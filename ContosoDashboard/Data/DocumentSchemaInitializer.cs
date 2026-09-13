using Microsoft.EntityFrameworkCore;

namespace ContosoDashboard.Data;

public static class DocumentSchemaInitializer
{
    public static async Task EnsureCreatedAsync(ApplicationDbContext context)
    {
        await context.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'[dbo].[Documents]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Documents](
                    [DocumentId] int NOT NULL IDENTITY,
                    [Title] nvarchar(255) NOT NULL,
                    [Description] nvarchar(2000) NULL,
                    [Category] nvarchar(255) NOT NULL,
                    [Tags] nvarchar(1000) NULL,
                    [ProjectId] int NULL,
                    [UploadedByUserId] int NOT NULL,
                    [FileName] nvarchar(255) NOT NULL,
                    [StoredFileName] nvarchar(255) NOT NULL,
                    [FilePath] nvarchar(500) NOT NULL,
                    [FileSizeBytes] bigint NOT NULL,
                    [MimeType] nvarchar(255) NOT NULL,
                    [UploadedDate] datetime2 NOT NULL,
                    [UpdatedDate] datetime2 NULL,
                    [Status] int NOT NULL,
                    CONSTRAINT [PK_Documents] PRIMARY KEY ([DocumentId]),
                    CONSTRAINT [FK_Documents_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [dbo].[Projects] ([ProjectId]),
                    CONSTRAINT [FK_Documents_Users_UploadedByUserId] FOREIGN KEY ([UploadedByUserId]) REFERENCES [dbo].[Users] ([UserId])
                );
            END
            """);

        await context.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'[dbo].[DocumentShares]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[DocumentShares](
                    [DocumentShareId] int NOT NULL IDENTITY,
                    [DocumentId] int NOT NULL,
                    [UserId] int NOT NULL,
                    [SharedByUserId] int NOT NULL,
                    [SharedDate] datetime2 NOT NULL,
                    CONSTRAINT [PK_DocumentShares] PRIMARY KEY ([DocumentShareId]),
                    CONSTRAINT [FK_DocumentShares_Documents_DocumentId] FOREIGN KEY ([DocumentId]) REFERENCES [dbo].[Documents] ([DocumentId]) ON DELETE CASCADE,
                    CONSTRAINT [FK_DocumentShares_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId]),
                    CONSTRAINT [FK_DocumentShares_Users_SharedByUserId] FOREIGN KEY ([SharedByUserId]) REFERENCES [dbo].[Users] ([UserId])
                );
            END
            """);

        await context.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'[dbo].[DocumentActivities]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[DocumentActivities](
                    [DocumentActivityId] int NOT NULL IDENTITY,
                    [DocumentId] int NOT NULL,
                    [UserId] int NOT NULL,
                    [Action] nvarchar(100) NOT NULL,
                    [Details] nvarchar(1000) NULL,
                    [ActionDate] datetime2 NOT NULL,
                    CONSTRAINT [PK_DocumentActivities] PRIMARY KEY ([DocumentActivityId]),
                    CONSTRAINT [FK_DocumentActivities_Documents_DocumentId] FOREIGN KEY ([DocumentId]) REFERENCES [dbo].[Documents] ([DocumentId]) ON DELETE CASCADE,
                    CONSTRAINT [FK_DocumentActivities_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId])
                );
            END
            """);

        await context.Database.ExecuteSqlRawAsync("""
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Documents_UploadedByUserId' AND object_id = OBJECT_ID(N'[dbo].[Documents]'))
                CREATE INDEX [IX_Documents_UploadedByUserId] ON [dbo].[Documents] ([UploadedByUserId]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Documents_ProjectId' AND object_id = OBJECT_ID(N'[dbo].[Documents]'))
                CREATE INDEX [IX_Documents_ProjectId] ON [dbo].[Documents] ([ProjectId]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Documents_Status' AND object_id = OBJECT_ID(N'[dbo].[Documents]'))
                CREATE INDEX [IX_Documents_Status] ON [dbo].[Documents] ([Status]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Documents_Category' AND object_id = OBJECT_ID(N'[dbo].[Documents]'))
                CREATE INDEX [IX_Documents_Category] ON [dbo].[Documents] ([Category]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DocumentShares_DocumentId_UserId' AND object_id = OBJECT_ID(N'[dbo].[DocumentShares]'))
                CREATE UNIQUE INDEX [IX_DocumentShares_DocumentId_UserId] ON [dbo].[DocumentShares] ([DocumentId], [UserId]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DocumentActivities_DocumentId_ActionDate' AND object_id = OBJECT_ID(N'[dbo].[DocumentActivities]'))
                CREATE INDEX [IX_DocumentActivities_DocumentId_ActionDate] ON [dbo].[DocumentActivities] ([DocumentId], [ActionDate]);
            """);
    }
}