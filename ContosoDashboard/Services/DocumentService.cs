using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Data;
using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public class DocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorageService;
    private readonly INotificationService _notificationService;
    private readonly IUserService _userService;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".doc",
        ".docx",
        ".xls",
        ".xlsx",
        ".ppt",
        ".pptx",
        ".txt",
        ".jpg",
        ".jpeg",
        ".png"
    };

    private const long MaxFileSizeBytes = 25 * 1024 * 1024;

    public DocumentService(
        ApplicationDbContext context,
        IFileStorageService fileStorageService,
        INotificationService notificationService,
        IUserService userService)
    {
        _context = context;
        _fileStorageService = fileStorageService;
        _notificationService = notificationService;
        _userService = userService;
    }

    public async Task<List<Document>> GetUserDocumentsAsync(int userId)
    {
        return await _context.Documents
            .Include(d => d.Project)
            .Include(d => d.UploadedByUser)
            .Where(d => d.UploadedByUserId == userId || d.Shares.Any(s => s.UserId == userId) || (d.ProjectId.HasValue && d.Project!.ProjectMembers.Any(pm => pm.UserId == userId)))
            .OrderByDescending(d => d.UploadedDate)
            .ToListAsync();
    }

    public async Task<List<Document>> GetProjectDocumentsAsync(int projectId, int requestingUserId)
    {
        var project = await _context.Projects
            .Include(p => p.ProjectMembers)
            .FirstOrDefaultAsync(p => p.ProjectId == projectId);

        if (project == null)
        {
            return new List<Document>();
        }

        var isAuthorized = project.ProjectManagerId == requestingUserId ||
                           project.ProjectMembers.Any(pm => pm.UserId == requestingUserId);

        if (!isAuthorized)
        {
            return new List<Document>();
        }

        return await _context.Documents
            .Include(d => d.UploadedByUser)
            .Include(d => d.Project)
            .Where(d => d.ProjectId == projectId && d.Status == DocumentStatus.Approved)
            .OrderByDescending(d => d.UploadedDate)
            .ToListAsync();
    }

    public async Task<List<Document>> SearchDocumentsAsync(int userId, string searchTerm)
    {
        var normalized = searchTerm.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return await GetUserDocumentsAsync(userId);
        }

        var query = _context.Documents
            .Include(d => d.Project)
            .Include(d => d.UploadedByUser)
            .Where(d => d.Status == DocumentStatus.Approved)
            .Where(d => d.UploadedByUserId == userId || d.Shares.Any(s => s.UserId == userId) || (d.ProjectId.HasValue && d.Project!.ProjectMembers.Any(pm => pm.UserId == userId)));

        var lower = normalized.ToLower();
        query = query.Where(d =>
            d.Title.ToLower().Contains(lower) ||
            (d.Description != null && d.Description.ToLower().Contains(lower)) ||
            (d.Tags != null && d.Tags.ToLower().Contains(lower)) ||
            d.UploadedByUser.DisplayName.ToLower().Contains(lower) ||
            (d.Project != null && d.Project.Name.ToLower().Contains(lower)));

        return await query
            .OrderByDescending(d => d.UploadedDate)
            .ToListAsync();
    }

    public async Task<Document> UploadDocumentAsync(int userId, int? projectId, string title, string? description, string category, string? tags, Stream fileStream, string fileName)
    {
        var user = await _userService.GetUserByIdAsync(userId) ?? throw new InvalidOperationException("User not found.");

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Document title is required.");
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            throw new ArgumentException("Document category is required.");
        }

        var extension = Path.GetExtension(fileName);
        if (!AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Unsupported file type.");
        }

        if (fileStream.Length > MaxFileSizeBytes)
        {
            throw new InvalidOperationException("File exceeds the 25 MB limit.");
        }

        if (projectId.HasValue)
        {
            var project = await _context.Projects
                .Include(p => p.ProjectMembers)
                .FirstOrDefaultAsync(p => p.ProjectId == projectId.Value);

            if (project == null)
            {
                throw new InvalidOperationException("Project not found.");
            }

            var isAllowed = project.ProjectManagerId == userId || project.ProjectMembers.Any(pm => pm.UserId == userId);
            if (!isAllowed)
            {
                throw new UnauthorizedAccessException("User is not authorized to upload to this project.");
            }
        }

        var safeFileName = Guid.NewGuid() + extension;
        var relativeDirectory = projectId.HasValue ? $"{userId}/{projectId.Value}" : $"{userId}/personal";
        var relativePath = Path.Combine(relativeDirectory, safeFileName).Replace('\\', '/');

        await _fileStorageService.UploadAsync(fileStream, relativePath, "application/octet-stream");

        var document = new Document
        {
            Title = title,
            Description = description,
            Category = category,
            Tags = tags,
            ProjectId = projectId,
            UploadedByUserId = userId,
            FileName = fileName,
            StoredFileName = safeFileName,
            FilePath = relativePath,
            FileSizeBytes = fileStream.Length,
            MimeType = GetMimeType(extension),
            UploadedDate = DateTime.UtcNow,
            Status = DocumentStatus.PendingScan,
            UpdatedDate = DateTime.UtcNow
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        await LogActivityAsync(document.DocumentId, userId, "Upload", "Document uploaded and queued for scan");

        if (projectId.HasValue)
        {
            var members = await _context.ProjectMembers
                .Where(pm => pm.ProjectId == projectId.Value)
                .Select(pm => pm.UserId)
                .ToListAsync();

            foreach (var memberId in members.Where(id => id != userId))
            {
                await _notificationService.CreateNotificationAsync(new Notification
                {
                    UserId = memberId,
                    Title = "New Project Document",
                    Message = $"A new document '{title}' was added to the project.",
                    Type = NotificationType.ProjectUpdate,
                    Priority = NotificationPriority.Informational
                });
            }
        }

        return document;
    }

    public async Task<Document?> GetDocumentByIdAsync(int documentId, int requestingUserId)
    {
        var document = await _context.Documents
            .Include(d => d.Project)
            .ThenInclude(p => p!.ProjectMembers)
            .Include(d => d.UploadedByUser)
            .Include(d => d.Shares)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId);

        if (document == null)
        {
            return null;
        }

        var canAccess = document.UploadedByUserId == requestingUserId ||
                        document.Shares.Any(s => s.UserId == requestingUserId) ||
                        (document.ProjectId.HasValue && (document.Project!.ProjectManagerId == requestingUserId || document.Project.ProjectMembers.Any(pm => pm.UserId == requestingUserId)));

        return canAccess ? document : null;
    }

    public async Task<Stream?> GetDocumentStreamAsync(int documentId, int requestingUserId)
    {
        var document = await GetDocumentByIdAsync(documentId, requestingUserId);
        if (document == null)
        {
            return null;
        }

        var absolutePath = GetAbsoluteStoragePath(document.FilePath);
        if (!File.Exists(absolutePath))
        {
            return null;
        }

        return await _fileStorageService.DownloadAsync(absolutePath);
    }

    public async Task<bool> UpdateMetadataAsync(int documentId, int requestingUserId, string? title, string? description, string? category, string? tags)
    {
        var document = await _context.Documents.FirstOrDefaultAsync(d => d.DocumentId == documentId);
        if (document == null)
        {
            return false;
        }

        if (document.UploadedByUserId != requestingUserId)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(title))
        {
            document.Title = title;
        }

        document.Description = description;

        if (!string.IsNullOrWhiteSpace(category))
        {
            document.Category = category;
        }

        document.Tags = tags;
        document.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await LogActivityAsync(documentId, requestingUserId, "MetadataUpdated", "Document metadata updated");
        return true;
    }

    public async Task<bool> ShareDocumentAsync(int documentId, int ownerUserId, int targetUserId)
    {
        var document = await _context.Documents.FirstOrDefaultAsync(d => d.DocumentId == documentId);
        if (document == null || document.UploadedByUserId != ownerUserId)
        {
            return false;
        }

        var alreadyShared = await _context.DocumentShares
            .AnyAsync(ds => ds.DocumentId == documentId && ds.UserId == targetUserId);

        if (alreadyShared)
        {
            return false;
        }

        var share = new DocumentShare
        {
            DocumentId = documentId,
            UserId = targetUserId,
            SharedByUserId = ownerUserId,
            SharedDate = DateTime.UtcNow
        };

        _context.DocumentShares.Add(share);
        await _context.SaveChangesAsync();

        await _notificationService.CreateNotificationAsync(new Notification
        {
            UserId = targetUserId,
            Title = "Document Shared",
            Message = $"A document was shared with you: {document.Title}",
            Type = NotificationType.ProjectUpdate,
            Priority = NotificationPriority.Important
        });

        await LogActivityAsync(documentId, ownerUserId, "Share", "Document shared with another user");
        return true;
    }

    public async Task<bool> DeleteDocumentAsync(int documentId, int requestingUserId)
    {
        var document = await _context.Documents
            .Include(d => d.Shares)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId);

        if (document == null)
        {
            return false;
        }

        var isOwner = document.UploadedByUserId == requestingUserId;
        var isProjectManager = document.ProjectId.HasValue &&
                               await _context.Projects.AnyAsync(p => p.ProjectId == document.ProjectId && p.ProjectManagerId == requestingUserId);

        if (!isOwner && !isProjectManager)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(document.FilePath))
        {
            await _fileStorageService.DeleteAsync(GetAbsoluteStoragePath(document.FilePath));
        }

        _context.DocumentShares.RemoveRange(document.Shares);
        _context.Documents.Remove(document);
        await _context.SaveChangesAsync();

        await LogActivityAsync(documentId, requestingUserId, "Delete", "Document deleted");
        return true;
    }

    public async Task<List<Document>> GetSharedWithUserAsync(int userId)
    {
        return await _context.DocumentShares
            .Include(ds => ds.Document)
            .ThenInclude(d => d!.UploadedByUser)
            .Where(ds => ds.UserId == userId)
            .Select(ds => ds.Document)
            .OrderByDescending(d => d.UploadedDate)
            .ToListAsync();
    }

    public async Task LogActivityAsync(int documentId, int userId, string action, string? details = null)
    {
        _context.DocumentActivities.Add(new DocumentActivity
        {
            DocumentId = documentId,
            UserId = userId,
            Action = action,
            Details = details,
            ActionDate = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
    }

    public async Task SetScanResultAsync(int documentId, bool approved)
    {
        var document = await _context.Documents.FirstOrDefaultAsync(d => d.DocumentId == documentId);
        if (document == null)
        {
            return;
        }

        document.Status = approved ? DocumentStatus.Approved : DocumentStatus.Rejected;
        document.UpdatedDate = DateTime.UtcNow;

        if (!approved)
        {
            await _fileStorageService.DeleteAsync(GetAbsoluteStoragePath(document.FilePath));
        }

        await _context.SaveChangesAsync();
        await LogActivityAsync(documentId, document.UploadedByUserId, approved ? "ScanApproved" : "ScanRejected", approved ? "File passed scan" : "File failed scan and was quarantined");
    }

    private static string GetMimeType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".txt" => "text/plain",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };
    }

    private string GetAbsoluteStoragePath(string relativePath)
    {
        return Path.Combine(_fileStorageService.GetRootPath(), relativePath.TrimStart('/', '\\'));
    }
}
