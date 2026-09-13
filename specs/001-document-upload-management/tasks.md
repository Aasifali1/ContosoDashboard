# Tasks: Document Upload and Management

**Input**: Design documents from `/specs/001-document-upload-management/`
**Prerequisites**: plan.md (required), spec.md (required for user stories)

**Tests**: Not explicitly requested in the feature specification; therefore no dedicated test tasks are included in this task list.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish the local document storage and shared service contracts required by the feature.

- [ ] T001 Create the feature storage directory and upload layout under `ContosoDashboard/AppData/uploads/` for secure local document handling
- [X] T002 [P] Add the initial document service contracts in `ContosoDashboard/Services/IFileStorageService.cs`
- [X] T003 [P] Implement the local storage provider in `ContosoDashboard/Services/LocalFileStorageService.cs` with upload, delete, and download methods
- [X] T004 [P] Register the storage abstraction in `ContosoDashboard/Program.cs` and app configuration entries in `ContosoDashboard/appsettings.json`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core data model and authorization infrastructure that MUST be complete before user story work begins.

**Checkpoint**: Foundation ready - user story implementation can now begin.

- [X] T010 Add the base document entities in `ContosoDashboard/Models/Document.cs`, `ContosoDashboard/Models/DocumentShare.cs`, and `ContosoDashboard/Models/DocumentActivity.cs`
- [X] T011 [P] Extend `ContosoDashboard/Data/ApplicationDbContext.cs` with `DbSet<Document>`, `DbSet<DocumentShare>`, and `DbSet<DocumentActivity>` plus required indexes and relationship setup
- [X] T012 [P] Add support for document metadata fields and status tracking in `ContosoDashboard/Models/Document.cs`, including `DocumentId` as integer, category as text, and MIME type length support for Office documents
- [X] T013 Implement the shared `DocumentService` in `ContosoDashboard/Services/DocumentService.cs` with upload validation, file path generation, authorization checks, and pending scan status flows
- [ ] T014 [P] Update `ContosoDashboard/Services/NotificationService.cs` to support document-share and project-document notifications
- [X] T015 Add permission helpers and project membership checks in `ContosoDashboard/Services/DocumentService.cs` to enforce IDOR-safe document access rules
- [ ] T016 Add the background scan queue contract and message payload model in `ContosoDashboard/Services/DocumentScanQueueMessage.cs` for Azure Queue Storage integration
- [ ] T017 [P] Add quota and validation helpers for file size, extension whitelist, and duplicate file-path prevention in `ContosoDashboard/Services/DocumentService.cs`

---

## Phase 3: User Story 1 - Upload and organize work documents (Priority: P1) 🎯 MVP

**Goal**: Users can securely upload, categorize, and view their work documents.

**Independent Test**: A logged-in user can upload a valid document, provide metadata, and confirm it appears in their document list with the expected metadata and status.

### Implementation for User Story 1

- [X] T021 [P] [US1] Create the document management page shell in `ContosoDashboard/Pages/Documents.razor` for personal and project document browsing
- [X] T022 [US1] Implement the upload workflow in `ContosoDashboard/Services/DocumentService.cs` for file validation, storage, metadata persistence, and pending/approved scan states
- [X] T023 [US1] Add upload UI actions and progress/error messages in `ContosoDashboard/Pages/Documents.razor`
- [X] T024 [US1] Add personal document list and sorting/filtering in `ContosoDashboard/Pages/Documents.razor`
- [ ] T025 [US1] Wire project-associated document display into `ContosoDashboard/Pages/ProjectDetails.razor` so project members can view related files
- [X] T026 [US1] Ensure file metadata includes title, description, category, tags, uploader, upload date, size, and MIME type in `ContosoDashboard/Models/Document.cs`

**Checkpoint**: At this point, User Story 1 should be fully functional and testable independently.

---

## Phase 4: User Story 2 - Access and find project documents securely (Priority: P1)

**Goal**: Users can search and access only the documents they are authorized to view.

**Independent Test**: A user can search by title, tag, or uploader and only sees documents permitted for their role and project access.

### Implementation for User Story 2

- [ ] T031 [P] [US2] Implement document search and filtering logic in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T032 [US2] Enforce role- and project-based authorization in `ContosoDashboard/Services/DocumentService.cs` and prevent direct object access for unauthorized users
- [ ] T033 [US2] Add secure download and preview actions in `ContosoDashboard/Pages/Documents.razor` and `ContosoDashboard/Pages/ProjectDetails.razor`
- [ ] T034 [US2] Add audit logging for document access in `ContosoDashboard/Services/DocumentService.cs` and `ContosoDashboard/Models/DocumentActivity.cs`
- [ ] T035 [US2] Ensure project-document queries return only current user-authorized records and hide unshared files from search results

**Checkpoint**: At this point, User Stories 1 and 2 should both work independently.

---

## Phase 5: User Story 3 - Share and manage documents across teams (Priority: P2)

**Goal**: Document owners and managers can update metadata, replace files, share documents, and remove them safely.

**Independent Test**: A user can edit metadata, share a document with another user, and confirm the recipient can access the shared item in their shared documents view.

### Implementation for User Story 3

- [ ] T041 [P] [US3] Add metadata editing and replace-file flows in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T042 [US3] Implement share assignment and recipient visibility logic in `ContosoDashboard/Services/DocumentService.cs` and `ContosoDashboard/Models/DocumentShare.cs`
- [ ] T043 [US3] Add the “Shared with Me” view and document ownership actions in `ContosoDashboard/Pages/Documents.razor`
- [ ] T044 [US3] Add delete confirmation and permanent cleanup behavior in `ContosoDashboard/Services/DocumentService.cs` and `ContosoDashboard/Pages/Documents.razor`
- [ ] T045 [US3] Trigger in-app notification creation for shared documents and project updates in `ContosoDashboard/Services/NotificationService.cs`

**Checkpoint**: At this point, the document-sharing and management flow should be functional and independently testable.

---

## Phase 6: User Story 4 - Connect documents to tasks and dashboard (Priority: P2)

**Goal**: Users can access the right documents in the context of tasks and the dashboard.

**Independent Test**: A user can view task details and the dashboard summary and see relevant documents without leaving the workflow.

### Implementation for User Story 4

- [ ] T051 [P] [US4] Add the recent-documents widget and summary count to `ContosoDashboard/Pages/Index.razor`
- [ ] T052 [US4] Extend task-detail views in `ContosoDashboard/Pages/Tasks.razor` to display associated document links and upload actions
- [ ] T053 [US4] Add project-to-document association logic in `ContosoDashboard/Services/DocumentService.cs` for task-related uploads
- [ ] T054 [US4] Apply dashboard and task query updates in `ContosoDashboard/Services/DashboardService.cs` and `ContosoDashboard/Services/TaskService.cs`

**Checkpoint**: At this point, the main document feature is integrated into the dashboard and task workflows.

---

## Phase 7: Async Virus Scan Job (Background Processing)

**Purpose**: Perform non-blocking malware scanning for uploaded files after storage.

- [ ] T061 [P] Implement the Azure Function scan worker contract in `ContosoDashboard/Services/DocumentScanFunction.cs` with Queue Storage trigger bindings for the upload queue
- [ ] T062 [US1] Add the queue message processing flow in `ContosoDashboard/Services/DocumentScanWorker.cs` to fetch the pending file, invoke the scan, and update the document status
- [ ] T063 [US1] Update `ContosoDashboard/Services/DocumentService.cs` to mark a document as `PendingScan`, emit the queue message, and handle approval or rejection transitions
- [ ] T064 [P] Add quarantine or delete logic for rejected files in `ContosoDashboard/Services/DocumentService.cs` and `ContosoDashboard/Services/LocalFileStorageService.cs`

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Final quality, security review, and training-appropriate documentation.

- [ ] T071 [P] Review and tighten authorization checks across `ContosoDashboard/Services/DocumentService.cs` and relevant pages
- [ ] T072 [P] Update the developer documentation in `README.md` with upload, local storage, and migration notes for the document feature
- [ ] T073 [P] Run a final UX and security walkthrough for upload, sharing, preview, delete, and access-denied scenarios
- [ ] T074 [P] Validate the Azure-ready queue scan flow remains optional for offline local training and does not break baseline local execution

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-6)**: All depend on Foundational completion
- **Async Scan (Phase 7)**: Depends on foundational storage and upload status flow, but can be parallelized with the final story work after the upload path is in place
- **Polish (Phase 8)**: Depends on all desired story work being complete

### User Story Dependencies

- **User Story 1 (P1)**: Depends on Foundational phase; no dependency on other stories
- **User Story 2 (P1)**: Depends on Foundational phase and the upload metadata flow from US1
- **User Story 3 (P2)**: Depends on Foundational phase and the access model established in US1/US2
- **User Story 4 (P2)**: Depends on Foundational phase and project/task context from the existing dashboard model

### Parallel Opportunities

- Setup tasks T001-T004 can be implemented in parallel
- Foundational tasks T010-T017 can be worked in parallel where files are logically separate
- User Story 1 tasks T021-T026 can proceed in parallel after foundational completion
- User Story 2 tasks T031-T035 can proceed in parallel after US1 upload foundation is ready
- User Story 3 tasks T041-T045 can proceed in parallel after the document access model is in place
- User Story 4 tasks T051-T054 can proceed in parallel after the project/task model availability is confirmed
- Async scan tasks T061-T064 can run in parallel with the story completion work after queue contract creation

---

## Parallel Example: User Story 1

```bash
# Launch the upload foundation tasks together
Task: "Create the document management page shell in ContosoDashboard/Pages/Documents.razor"
Task: "Implement upload validation and metadata persistence in ContosoDashboard/Services/DocumentService.cs"
Task: "Add project-associated document display in ContosoDashboard/Pages/ProjectDetails.razor"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1
4. Validate the upload and list workflow in isolation
5. Stop and verify the feature works before expanding into access, sharing, and integration tasks

### Incremental Delivery

1. Setup + Foundational → stable document storage and authorization base
2. User Story 1 → upload, metadata, and personal/project document list
3. User Story 2 → secure access and search
4. User Story 3 → sharing and management
5. User Story 4 → task and dashboard integration
6. Async scan worker → security hardening and Azure-ready scanning
7. Polish → final validation and documentation

### Team Execution Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once the foundation is stable:
   - Developer A: User Story 1
   - Developer B: User Story 2
   - Developer C: User Story 3
   - Developer D: User Story 4 / scan worker integration
3. Final hardening and documentation run after all user stories complete

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps each task to a user story for traceability
- Each story is defined to be independently completable and testable
- No detailed test implementation tasks were added because the feature specification did not explicitly request TDD or contract test generation
- Keep each task narrow enough for one developer to complete without additional context
