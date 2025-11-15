# Versioning Implementation Plan

## Overview

This plan implements versioning for the codebase analysis database, allowing snapshots of the codebase at different points in time and comparison between versions.

**Strategy**: Option B - Version Metadata in Documents (storage-efficient approach)

**Goal**: Enable versioning with minimal storage overhead while maintaining backward compatibility.

---

## Architecture Decisions

### Storage Strategy
- ✅ **Version metadata in documents**: Add `version` and `version_created_at` fields to document metadata
- ✅ **Version manifests**: Store version information in separate JSON manifest files
- ✅ **Backward compatible**: Existing documents default to `"version": "v0-unversioned"`

### Version Identification
- ✅ **Primary**: Timestamp-based auto-versioning (e.g., `v1-2025-01-15T10:30:00Z`)
- ✅ **Optional**: Git commit hash linking (if available)
- ✅ **Optional**: User-provided version IDs (e.g., `v1.0`, `release-2.3`)

### Storage Location
```
vector-store/
  versions/
    v1-2025-01-15T10-30-00Z.json  (version manifest)
    v2-2025-01-16T14-20-00Z.json
    current.json  (pointer to current version)
  documents/
    {id}.json  (with version metadata)
  vector_index.bin  (current index)
```

---

## Phase 1: Core Version Infrastructure

### 1.1 Data Models
- [ ] Create `VersionInfo` class in `src/CodeAnalyzer.Roslyn/Models/`
  - [ ] Properties: `VersionId`, `CreatedAt`, `GitCommit`, `Description`, `Statistics`
  - [ ] JSON serialization support
- [ ] Create `VersionStatistics` class
  - [ ] Properties: `MethodCalls`, `MethodDefinitions`, `ClassDefinitions`, `TotalDocuments`
- [ ] Create `VersionDiff` class
  - [ ] Properties: `FromVersion`, `ToVersion`, `Differences` (dictionary by element type)
- [ ] Create `ElementDiff` class
  - [ ] Properties: `Missing`, `Extra`, `Changed` (lists of items)

### 1.2 Version Manifest System
- [ ] Create `VersionManifest` class to manage version metadata
- [ ] Implement `SaveVersionManifestAsync(versionInfo, storePath)` method
- [ ] Implement `LoadVersionManifestAsync(versionId, storePath)` method
- [ ] Implement `ListVersionManifestsAsync(storePath)` method
- [ ] Implement `GetCurrentVersionAsync(storePath)` method
- [ ] Implement `SetCurrentVersionAsync(versionId, storePath)` method
- [ ] Create `versions/` directory structure in vector store

### 1.3 Version Metadata in Documents
- [ ] Update `StoreMethodCallAsync` to include version metadata
  - [ ] Add `version` field to metadata dictionary
  - [ ] Add `version_created_at` field to metadata dictionary
- [ ] Update `StoreMethodDefinitionAsync` to include version metadata
- [ ] Update `StoreClassDefinitionAsync` to include version metadata
- [ ] Ensure backward compatibility (existing documents work without version)

---

## Phase 2: Version Management Interface

### 2.1 Extend IVectorStoreWriter Interface
- [ ] Create new `IVersionedVectorStore` interface extending `IVectorStoreWriter`
- [ ] Add `CreateVersionAsync(versionId?, description?, gitCommit?)` method
- [ ] Add `ListVersionsAsync()` method
- [ ] Add `GetVersionAsync(versionId)` method
- [ ] Add `DeleteVersionAsync(versionId)` method
- [ ] Add `SetCurrentVersionAsync(versionId)` method

### 2.2 Version-Aware Query Methods
- [ ] Add optional `versionId` parameter to `GetAsync(id, versionId?)`
- [ ] Add optional `versionId` parameter to `SearchTextAsync(query, versionId?, limit?)`
- [ ] Add optional `versionId` parameter to `GetAllIdsAsync(versionId?)`
- [ ] Implement version filtering logic (filter documents by version metadata)
- [ ] Default to "current" version if not specified (backward compatible)

### 2.3 Version Creation Logic
- [ ] Implement `CreateVersionAsync` in analyzer
  - [ ] Generate version ID (timestamp-based if not provided)
  - [ ] Capture git commit hash if available (optional)
  - [ ] Tag all documents created during analysis with version
  - [ ] Create version manifest with statistics
  - [ ] Save manifest to `versions/` directory
  - [ ] Update `current.json` pointer

### 2.4 Version Statistics Collection
- [ ] Implement statistics collection during analysis
  - [ ] Count method calls, method definitions, class definitions
  - [ ] Store in `VersionStatistics` object
  - [ ] Include in version manifest

---

## Phase 3: Version Comparison

### 3.1 Version Diff Implementation
- [ ] Create `DiffVersionsAsync(fromVersionId, toVersionId)` method
  - [ ] Load documents from both versions
  - [ ] Compare method calls (find missing, extra, changed)
  - [ ] Compare method definitions
  - [ ] Compare class definitions
  - [ ] Return structured `VersionDiff` object
- [ ] Create `DiffVersionWithCodebaseAsync(versionId, projectPath)` method
  - [ ] Re-analyze current codebase (ground truth)
  - [ ] Load documents from specified version
  - [ ] Compare using existing diff logic
  - [ ] Return structured diff

### 3.2 Diff Algorithms
- [ ] Implement method call comparison
  - [ ] Use key: `{caller}->{callee}@{filePath}:{lineNumber}`
  - [ ] Identify missing (in toVersion, not in fromVersion)
  - [ ] Identify extra (in fromVersion, not in toVersion)
- [ ] Implement method definition comparison
  - [ ] Use key: `FullyQualifiedName`
  - [ ] Compare properties (parameters, return type, modifiers)
  - [ ] Identify changed methods (same name, different properties)
- [ ] Implement class definition comparison
  - [ ] Use key: `FullyQualifiedName`
  - [ ] Compare properties (base class, interfaces, member counts)
  - [ ] Identify changed classes

### 3.3 Diff Output Formatting
- [ ] Format diff results for console output
- [ ] Format diff results for JSON API response
- [ ] Include summary statistics (counts)
- [ ] Include detailed item lists (with `include_details` flag)

---

## Phase 4: Console Commands

### 4.1 Version Management Commands
- [ ] Add `create-version [version-id] [--description <text>] [--git-commit <hash>]` command
  - [ ] Create new version snapshot of current analysis
  - [ ] Generate auto-version ID if not provided
  - [ ] Capture optional git commit hash
- [ ] Add `list-versions` command
  - [ ] Display all versions with creation date, description, statistics
  - [ ] Highlight current version
- [ ] Add `get-version <version-id>` command
  - [ ] Display detailed version information
  - [ ] Show statistics and metadata
- [ ] Add `delete-version <version-id>` command
  - [ ] Delete version manifest
  - [ ] Optionally delete associated documents (with confirmation)
- [ ] Add `set-current-version <version-id>` command
  - [ ] Update current version pointer
  - [ ] Set default version for queries

### 4.2 Version Query Commands
- [ ] Update `list-classes` to support `--version <version-id>` flag
- [ ] Update `list-methods` to support `--version <version-id>` flag
- [ ] Update `get-method` to support `--version <version-id>` flag
- [ ] Update `get-class` to support `--version <version-id>` flag
- [ ] Update `get-callers` to support `--version <version-id>` flag
- [ ] Update `get-callees` to support `--version <version-id>` flag
- [ ] Update `search` to support `--version <version-id>` flag

### 4.3 Version Diff Commands
- [ ] Add `diff-versions <from-version-id> <to-version-id>` command
  - [ ] Compare two versions
  - [ ] Display summary and detailed differences
- [ ] Add `diff-version <version-id> <project-path>` command
  - [ ] Compare version with current codebase
  - [ ] Re-uses existing `diff_project` logic

---

## Phase 5: API Endpoints

### 5.1 Version Management Endpoints
- [ ] Add `POST /api/tools/create_version` endpoint
  - [ ] Request: `{ "project_id": "uuid", "version_id": "optional", "description": "optional", "git_commit": "optional" }`
  - [ ] Response: `{ "version_id": "...", "created_at": "...", "statistics": {...} }`
- [ ] Add `POST /api/tools/list_versions` endpoint
  - [ ] Request: `{ "project_id": "uuid" }`
  - [ ] Response: `{ "versions": [...], "current_version": "..." }`
- [ ] Add `POST /api/tools/get_version` endpoint
  - [ ] Request: `{ "project_id": "uuid", "version_id": "..." }`
  - [ ] Response: `{ "version": {...} }`
- [ ] Add `POST /api/tools/delete_version` endpoint
  - [ ] Request: `{ "project_id": "uuid", "version_id": "..." }`
  - [ ] Response: `{ "success": true }`
- [ ] Add `POST /api/tools/set_current_version` endpoint
  - [ ] Request: `{ "project_id": "uuid", "version_id": "..." }`
  - [ ] Response: `{ "success": true, "current_version": "..." }`

### 5.2 Version-Aware Query Endpoints
- [ ] Update `list_classes` to accept optional `version_id` parameter
- [ ] Update `list_methods` to accept optional `version_id` parameter
- [ ] Update `get_method` to accept optional `version_id` parameter
- [ ] Update `get_class` to accept optional `version_id` parameter
- [ ] Update `get_callers` to accept optional `version_id` parameter
- [ ] Update `get_callees` to accept optional `version_id` parameter
- [ ] Update `search_code` to accept optional `version_id` parameter

### 5.3 Version Comparison Endpoints
- [ ] Add `POST /api/tools/diff_versions` endpoint
  - [ ] Request: `{ "project_id": "uuid", "from_version": "...", "to_version": "...", "include_details": true }`
  - [ ] Response: `{ "from_version": "...", "to_version": "...", "summary": {...}, "differences": {...} }`
- [ ] Update `diff_project` to accept optional `version_id` parameter
  - [ ] Compare specified version with current codebase
  - [ ] If no version_id, compare current version with codebase

---

## Phase 6: Git Integration (Optional Enhancement)

### 6.1 Git Commit Detection
- [ ] Add method to detect current git commit hash
  - [ ] Use `git rev-parse HEAD` command
  - [ ] Handle cases where not in git repo gracefully
- [ ] Auto-capture git commit when creating version
- [ ] Store git commit in version manifest

### 6.2 Git-Based Versioning
- [ ] Add `create-version-from-git <commit-hash>` command
  - [ ] Analyze codebase at specific git commit
  - [ ] Create version with commit hash as identifier
- [ ] Add `list-versions --git` flag
  - [ ] Show git commit information for versions
- [ ] Add git commit to version display/output

---

## Phase 7: Testing

### 7.1 Unit Tests
- [ ] Test `VersionInfo` serialization/deserialization
- [ ] Test `VersionManifest` save/load operations
- [ ] Test version metadata addition to documents
- [ ] Test version filtering in queries
- [ ] Test version diff algorithms
- [ ] Test backward compatibility (documents without version)

### 7.2 Integration Tests
- [ ] Test version creation workflow
  - [ ] Create version, verify documents tagged
  - [ ] Verify manifest created correctly
  - [ ] Verify statistics accurate
- [ ] Test version queries
  - [ ] Query specific version
  - [ ] Query current version (default)
  - [ ] Verify correct documents returned
- [ ] Test version comparison
  - [ ] Create two versions with known differences
  - [ ] Compare versions, verify diff accuracy
  - [ ] Compare version with codebase
- [ ] Test version deletion
  - [ ] Delete version, verify manifest removed
  - [ ] Verify documents still accessible (if not deleted)

### 7.3 Console Command Tests
- [ ] Test `create-version` command
- [ ] Test `list-versions` command
- [ ] Test `get-version` command
- [ ] Test `delete-version` command
- [ ] Test `diff-versions` command
- [ ] Test version-aware query commands

### 7.4 API Endpoint Tests
- [ ] Test all version management endpoints
- [ ] Test version-aware query endpoints
- [ ] Test version comparison endpoints
- [ ] Test error handling (invalid version IDs, etc.)

---

## Phase 8: Documentation

### 8.1 API Documentation
- [ ] Update `API_SPEC.md` with version management endpoints
- [ ] Document version-aware query parameters
- [ ] Add version comparison endpoint documentation
- [ ] Add examples for version workflows

### 8.2 User Documentation
- [ ] Document versioning concepts
- [ ] Document console commands for versioning
- [ ] Add examples of version workflows
- [ ] Document best practices (when to create versions, retention policies)

### 8.3 Code Documentation
- [ ] Add XML documentation comments to all new classes/methods
- [ ] Document version metadata schema
- [ ] Document version manifest format
- [ ] Document version diff algorithm

---

## Phase 9: Migration & Backward Compatibility

### 9.1 Migration Script
- [ ] Create migration utility to tag existing documents
  - [ ] Scan all existing documents
  - [ ] Add `"version": "v0-unversioned"` to metadata
  - [ ] Create initial version manifest for unversioned documents
- [ ] Test migration on existing vector stores
- [ ] Verify no data loss during migration

### 9.2 Backward Compatibility
- [ ] Ensure all existing queries work without version parameter
- [ ] Default to "current" version when not specified
- [ ] Handle documents without version metadata gracefully
- [ ] Maintain existing API contract (optional version parameters)

---

## Phase 10: Performance & Optimization

### 10.1 Query Performance
- [ ] Benchmark version-filtered queries vs. unfiltered
- [ ] Optimize version filtering (index version metadata if needed)
- [ ] Cache version manifests for faster access
- [ ] Optimize version diff algorithms for large codebases

### 10.2 Storage Optimization
- [ ] Implement document deduplication across versions (future enhancement)
- [ ] Add compression for old version indexes (future enhancement)
- [ ] Implement retention policies (auto-delete old versions)
- [ ] Monitor storage growth with multiple versions

---

## Phase 11: API Infrastructure Enhancements

This phase implements the remaining API endpoints that require additional infrastructure beyond the current single-vector-store architecture.

### 11.1 Project Management System

**Goal**: Enable multiple projects with unique IDs, supporting the `list_projects` API endpoint.

#### 11.1.1 Data Models
- [ ] Create `ProjectInfo` class in `src/CodeAnalyzer.Roslyn/Models/`
  - [ ] Properties: `ProjectId` (UUID), `Name`, `Path`, `Status` (ready|analyzing|error), `IndexedAt`, `LastModified`
  - [ ] JSON serialization support
- [ ] Create `ProjectStatus` class
  - [ ] Properties: `ProjectId`, `Status`, `Progress` (optional), `Statistics`, `Errors` (optional)
  - [ ] Progress tracking: `FilesProcessed`, `TotalFiles`, `Percentage`
  - [ ] Statistics: `TotalFiles`, `TotalMethods`, `TotalClasses`, `TotalMethodCalls`, `TotalNamespaces`
- [ ] Create `ProjectMetadata` class for storage
  - [ ] Store project metadata in `projects/` directory as JSON files
  - [ ] File naming: `{project-id}.json`

#### 11.1.2 Project Storage Strategy
- [ ] Create `IProjectManager` interface
  - [ ] `Task<string> CreateProjectAsync(string projectPath, string? projectName)` - Returns project ID
  - [ ] `Task<ProjectInfo?> GetProjectAsync(string projectId)`
  - [ ] `Task<List<ProjectInfo>> ListProjectsAsync()`
  - [ ] `Task<bool> UpdateProjectStatusAsync(string projectId, ProjectStatus status)`
  - [ ] `Task<bool> DeleteProjectAsync(string projectId)`
- [ ] Implement `ProjectManager` class
  - [ ] Store project metadata in `projects/` directory (JSON files)
  - [ ] Generate UUIDs for new projects
  - [ ] Map project IDs to vector store paths (either separate stores or namespaced within single store)
  - [ ] Handle project lifecycle (creation, updates, deletion)
- [ ] Choose storage approach:
  - [ ] **Option A**: Separate vector store per project (`vector-store/{project-id}/`)
  - [ ] **Option B**: Single vector store with project ID in document metadata
  - [ ] **Recommendation**: Option A for better isolation, Option B for simpler management

#### 11.1.3 Vector Store Integration
- [ ] Update `FileVectorStoreAdapter` to support project-scoped stores
  - [ ] Accept optional `projectId` parameter in constructor
  - [ ] Use project-specific store path if provided
- [ ] Update `RoslynAnalyzer` to accept project context
  - [ ] Tag all documents with `project_id` metadata
  - [ ] Store in project-specific vector store location
- [ ] Implement project store initialization
  - [ ] Create project store directory on project creation
  - [ ] Initialize vector store for new project

#### 11.1.4 Console Commands
- [ ] Update `analyze` command to support project management
  - [ ] `analyze <project-path> [--project-name <name>] [--project-id <id>]`
  - [ ] Create or update project metadata
  - [ ] Store analysis in project-specific vector store
- [ ] Update `list-projects` command
  - [ ] Display all projects with full metadata
  - [ ] Show statistics for each project
  - [ ] Highlight current/default project
- [ ] Add `create-project <project-path> [--name <name>]` command
  - [ ] Create project metadata without analyzing
  - [ ] Return project ID
- [ ] Add `delete-project <project-id>` command
  - [ ] Delete project metadata and associated vector store
  - [ ] Confirm before deletion

#### 11.1.5 API Endpoints
- [ ] Update `POST /api/tools/index_project` endpoint
  - [ ] Accept `project_path` and optional `project_name`
  - [ ] Create or retrieve project ID
  - [ ] Return `project_id` in response
  - [ ] Store analysis in project-specific location
- [ ] Implement `POST /api/tools/list_projects` endpoint
  - [ ] Return list of all projects with full metadata
  - [ ] Include status, statistics, and timestamps
  - [ ] Support filtering by status (optional)
- [ ] Update `get_project_status` endpoint
  - [ ] Accept `project_id` parameter
  - [ ] Return project status and statistics
  - [ ] Include progress if analysis in progress

---

### 11.2 Enhanced Attribute Extraction

**Goal**: Extract route and HTTP method information from ASP.NET controller attributes for `list_entry_points` API.

#### 11.2.1 Roslyn Attribute Analysis
- [ ] Create `AttributeExtractor` helper class in `src/CodeAnalyzer.Roslyn/`
  - [ ] `ExtractRouteAttribute(SyntaxNode, SemanticModel)` method
  - [ ] `ExtractHttpMethodAttributes(SyntaxNode, SemanticModel)` method
  - [ ] `ExtractControllerRoute(SyntaxNode, SemanticModel)` method
- [ ] Implement route attribute detection
  - [ ] Detect `[Route("...")]` attributes on classes and methods
  - [ ] Detect `[HttpGet]`, `[HttpPost]`, `[HttpPut]`, `[HttpDelete]`, etc.
  - [ ] Detect `[HttpGet("...")]` with route templates
  - [ ] Combine class-level and method-level routes
- [ ] Implement HTTP method detection
  - [ ] Extract from `[HttpGet]`, `[HttpPost]`, `[HttpPut]`, `[HttpDelete]`, `[HttpPatch]`
  - [ ] Extract from `[AcceptVerbs]` attribute
  - [ ] Extract from method name conventions (Get, Post, Put, Delete) as fallback

#### 11.2.2 Metadata Extension
- [ ] Update `MethodDefinitionInfo` model
  - [ ] Add `Route` property (nullable string)
  - [ ] Add `HttpMethod` property (nullable string)
  - [ ] Add `IsControllerMethod` property (bool)
- [ ] Update `ExtractMethodDefinitions` in `RoslynAnalyzer`
  - [ ] Check if method is in a controller class (class name ends with "Controller" or has `[ApiController]`)
  - [ ] Extract route and HTTP method attributes
  - [ ] Populate new properties in `MethodDefinitionInfo`
- [ ] Update vector store metadata
  - [ ] Add `route` field to method definition documents
  - [ ] Add `http_method` field to method definition documents
  - [ ] Add `is_controller_method` field to method definition documents

#### 11.2.3 Console Command Updates
- [ ] Update `list-entry-points` command
  - [ ] Display route information when available
  - [ ] Display HTTP method information
  - [ ] Improve controller detection (check for `[ApiController]` attribute)
- [ ] Update `get-method` command
  - [ ] Display route and HTTP method for controller methods

#### 11.2.4 API Endpoint Updates
- [ ] Update `POST /api/tools/list_entry_points` response
  - [ ] Include `route` field (populated from attributes, not null)
  - [ ] Include `http_method` field (populated from attributes, not null)
  - [ ] Improve controller detection logic
- [ ] Update `POST /api/tools/get_method` response
  - [ ] Include route and HTTP method for controller methods

---

### 11.3 Async Job Tracking

**Goal**: Implement asynchronous job tracking for long-running `index_project` operations with status polling.

#### 11.3.1 Job Management Infrastructure
- [ ] Create `JobInfo` class in `src/CodeAnalyzer.Roslyn/Models/`
  - [ ] Properties: `JobId` (UUID), `ProjectId`, `Type` (index|analyze|other), `Status` (pending|running|completed|failed), `CreatedAt`, `StartedAt`, `CompletedAt`, `Progress`, `Error`
  - [ ] JSON serialization support
- [ ] Create `JobProgress` class
  - [ ] Properties: `FilesProcessed`, `TotalFiles`, `Percentage`, `CurrentFile` (optional)
- [ ] Create `IJobManager` interface
  - [ ] `Task<string> CreateJobAsync(string projectId, string jobType)` - Returns job ID
  - [ ] `Task<JobInfo?> GetJobAsync(string jobId)`
  - [ ] `Task<bool> UpdateJobProgressAsync(string jobId, JobProgress progress)`
  - [ ] `Task<bool> CompleteJobAsync(string jobId, bool success, string? error = null)`
  - [ ] `Task<List<JobInfo>> ListJobsAsync(string? projectId = null)`
  - [ ] `Task<bool> CancelJobAsync(string jobId)`

#### 11.3.2 Job Storage
- [ ] Implement `JobManager` class
  - [ ] Store job metadata in `jobs/` directory as JSON files
  - [ ] File naming: `{job-id}.json`
  - [ ] Support job status updates (atomic file writes)
  - [ ] Implement job cleanup (delete completed jobs older than N days)
- [ ] Create job persistence layer
  - [ ] Save job state to disk for recovery
  - [ ] Load job state on startup
  - [ ] Handle job state corruption gracefully

#### 11.3.3 Background Processing
- [ ] Create `BackgroundJobProcessor` class
  - [ ] Queue analysis jobs for background processing
  - [ ] Process jobs asynchronously using `Task.Run` or background service
  - [ ] Update job progress during processing
  - [ ] Handle job cancellation
  - [ ] Handle job failures with error reporting
- [ ] Implement progress reporting
  - [ ] Report file processing progress during analysis
  - [ ] Update job progress at regular intervals (every N files or N seconds)
  - [ ] Store progress in job metadata
- [ ] Implement job cancellation
  - [ ] Support cancellation tokens
  - [ ] Gracefully stop analysis when cancelled
  - [ ] Update job status to "cancelled"

#### 11.3.4 Analysis Integration
- [ ] Update `RoslynAnalyzer.AnalyzeProjectAsync` to support progress callbacks
  - [ ] Add optional `IProgress<JobProgress>` parameter
  - [ ] Report progress during file processing
  - [ ] Support cancellation token
- [ ] Create `IndexProjectJob` wrapper class
  - [ ] Manages job lifecycle (create, update, complete)
  - [ ] Wraps `RoslynAnalyzer.AnalyzeProjectAsync` call
  - [ ] Handles errors and updates job status
  - [ ] Reports progress to job manager

#### 11.3.5 Console Commands
- [ ] Update `analyze` command to support async mode
  - [ ] `analyze <project-path> [--async]` flag
  - [ ] If `--async`, create job and return job ID immediately
  - [ ] If not async, run synchronously (current behavior)
- [ ] Add `get-job <job-id>` command
  - [ ] Display job status and progress
  - [ ] Show current file being processed (if available)
- [ ] Add `list-jobs [--project-id <id>]` command
  - [ ] List all jobs or jobs for specific project
  - [ ] Show status, progress, and timestamps
- [ ] Add `cancel-job <job-id>` command
  - [ ] Cancel running job
  - [ ] Update job status

#### 11.3.6 API Endpoints
- [ ] Update `POST /api/tools/index_project` endpoint
  - [ ] Create job immediately and return `job_id`
  - [ ] Return `status: "analyzing"` immediately
  - [ ] Start background processing
  - [ ] Response: `{ "project_id": "...", "status": "analyzing", "job_id": "...", "message": "..." }`
- [ ] Implement `POST /api/tools/get_project_status` endpoint
  - [ ] Accept `project_id` parameter
  - [ ] Return project status and current job status if analyzing
  - [ ] Include progress information
  - [ ] Response matches API spec format
- [ ] Add `POST /api/tools/get_job_status` endpoint (optional)
  - [ ] Accept `job_id` parameter
  - [ ] Return detailed job status and progress
  - [ ] Useful for polling job completion
- [ ] Add `POST /api/tools/cancel_job` endpoint (optional)
  - [ ] Accept `job_id` parameter
  - [ ] Cancel running job
  - [ ] Return success status

---

### 11.4 Testing

#### 11.4.1 Project Management Tests
- [ ] Test project creation and metadata storage
- [ ] Test project listing and filtering
- [ ] Test project deletion
- [ ] Test project-scoped vector store isolation
- [ ] Test project status updates

#### 11.4.2 Attribute Extraction Tests
- [ ] Test route attribute extraction from various ASP.NET patterns
- [ ] Test HTTP method attribute extraction
- [ ] Test controller detection (class name vs `[ApiController]`)
- [ ] Test route combination (class-level + method-level)
- [ ] Test edge cases (no attributes, multiple attributes)

#### 11.4.3 Job Tracking Tests
- [ ] Test job creation and status updates
- [ ] Test progress reporting during analysis
- [ ] Test job completion and error handling
- [ ] Test job cancellation
- [ ] Test job persistence and recovery
- [ ] Test concurrent job processing

---

### 11.5 Documentation

#### 11.5.1 API Documentation
- [ ] Update `API_SPEC.md` with project management details
- [ ] Document async job workflow for `index_project`
- [ ] Document route and HTTP method fields in entry points
- [ ] Add examples for project management workflows

#### 11.5.2 User Documentation
- [ ] Document project management concepts
- [ ] Document async analysis workflow
- [ ] Document route detection capabilities
- [ ] Add examples and best practices

---

## Implementation Order

1. **Phase 1**: Core infrastructure (data models, manifest system)
2. **Phase 2**: Version management interface
3. **Phase 3**: Version comparison logic
4. **Phase 4**: Console commands (for testing)
5. **Phase 5**: API endpoints
6. **Phase 6**: Git integration (optional, can be done later)
7. **Phase 7**: Testing (throughout, but comprehensive at end)
8. **Phase 8**: Documentation
9. **Phase 9**: Migration & compatibility
10. **Phase 10**: Performance optimization
11. **Phase 11**: API Infrastructure Enhancements (Project Management, Attribute Extraction, Async Jobs)

---

## Success Criteria

### Versioning Features
- [ ] Can create version snapshots of codebase analysis
- [ ] Can list and query different versions
- [ ] Can compare versions and see differences
- [ ] All existing functionality works without version parameters (backward compatible)
- [ ] Version queries are performant (< 2x overhead vs. non-versioned)
- [ ] Storage overhead is reasonable (< 10% per additional version)

### API Infrastructure Features
- [ ] Can manage multiple projects with unique IDs
- [ ] `list_projects` API returns all projects with full metadata
- [ ] Route and HTTP method information extracted from ASP.NET attributes
- [ ] `list_entry_points` API returns accurate route and HTTP method data
- [ ] `index_project` API supports async job tracking with status polling
- [ ] Job progress can be queried and monitored
- [ ] All tests pass
- [ ] Documentation is complete

---

## Future Enhancements (Post-MVP)

- [ ] Delta storage (only store changes between versions)
- [ ] Version compression (compress old versions)
- [ ] Automatic versioning on git commits (hooks)
- [ ] Version tagging (semantic versioning support)
- [ ] Version rollback (restore codebase to previous version state)
- [ ] Version merging (combine changes from multiple versions)
- [ ] Version visualization (UI for browsing version history)

---

## Notes

### Versioning
- **Storage Estimate**: With 1250 documents × 3KB = ~3.7MB per version. 10 versions = ~37MB (acceptable)
- **Performance Impact**: Version filtering adds minimal overhead (metadata lookup)
- **Breaking Changes**: None - all version features are additive and optional
- **Dependencies**: No new external dependencies required

### API Infrastructure
- **Project Management**: Option A (separate stores) provides better isolation but uses more disk space. Option B (namespaced) is simpler but requires careful metadata filtering.
- **Attribute Extraction**: Requires Roslyn semantic analysis of attributes, which is already available in the codebase. No new dependencies needed.
- **Async Jobs**: Background processing can use `Task.Run` or .NET's `BackgroundService`. Job storage is file-based (JSON) for simplicity. Consider using a proper job queue (Hangfire, Quartz.NET) for production scale.
- **Breaking Changes**: None - all features are additive. Existing single-project workflows continue to work.

