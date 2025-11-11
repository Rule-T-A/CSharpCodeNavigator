# C# Code Navigator REST API Specification

## Overview

This REST API provides agent-friendly access to C# code analysis and navigation capabilities. The API is designed to be MCP-compatible, allowing it to be bridged to the Model Context Protocol for use with Claude Desktop and other AI agents.

**Version**: 1.0  
**Base URL**: `/api`  
**Protocol**: HTTP/HTTPS  
**Content Type**: `application/json`

---

## Design Philosophy

This API is designed for **agent composition** rather than pre-built question answering:

- **Composable Primitives**: Low-level operations that agents can combine to answer complex questions
- **Graph Traversal**: Tools for walking up and down call dependency trees
- **Discovery**: Enumeration endpoints to discover what code elements exist
- **Flexibility**: Agents control traversal depth, filtering, and query strategies
- **MCP-Compatible**: Structured to map cleanly to MCP tools and resources

---

## Authentication

*Note: Authentication is not currently implemented but may be added in future versions.*

---

## Error Handling

All endpoints use consistent error response format:

```json
{
  "error": {
    "code": "ERROR_CODE",
    "message": "Human-readable error message",
    "details": {
      // Additional context-specific error details
    }
  }
}
```

### HTTP Status Codes

- `200 OK` - Request succeeded
- `202 Accepted` - Async operation started (check status via resource)
- `400 Bad Request` - Invalid request parameters
- `404 Not Found` - Resource not found (project, method, class, etc.)
- `500 Internal Server Error` - Server error
- `503 Service Unavailable` - Service temporarily unavailable

### Common Error Codes

- `PROJECT_NOT_FOUND` - Specified project ID does not exist
- `METHOD_NOT_FOUND` - Specified method does not exist in project
- `CLASS_NOT_FOUND` - Specified class does not exist in project
- `INVALID_PARAMETER` - Request parameter is invalid or missing
- `ANALYSIS_IN_PROGRESS` - Project analysis is still in progress
- `ANALYSIS_FAILED` - Project analysis failed

---

## Tools

All tools are accessed via `POST /api/tools/{tool_name}`. Tools represent actions that agents can invoke.

### Tool: `index_project`

Analyze and index a C# codebase.

**Endpoint**: `POST /api/tools/index_project`

**Request Body**:
```json
{
  "project_path": "/path/to/codebase",
  "project_name": "MyProject"  // optional, defaults to folder name
}
```

**Response**:
```json
{
  "project_id": "uuid-here",
  "status": "analyzing|ready|error",
  "job_id": "job-uuid",
  "message": "Analysis started successfully"
}
```

**Notes**:
- Returns immediately with `status: "analyzing"` for long-running operations
- Use `get_project_status` tool to check progress
- `project_path` can be a directory or `.csproj` file

---

### Tool: `list_projects`

List all indexed projects.

**Endpoint**: `POST /api/tools/list_projects`

**Request Body**:
```json
{}
```

**Response**:
```json
{
  "projects": [
    {
      "project_id": "uuid",
      "name": "MyProject",
      "status": "ready|analyzing|error",
      "indexed_at": "2025-01-15T10:30:00Z",
      "path": "/path/to/codebase"
    }
  ],
  "total_count": 1
}
```

---

### Tool: `list_classes`

Enumerate all classes in a project.

**Endpoint**: `POST /api/tools/list_classes`

**Request Body**:
```json
{
  "project_id": "uuid",
  "namespace": "MyApp.Services",  // optional: filter by namespace
  "limit": 100,  // optional: pagination limit (default: 100)
  "offset": 0    // optional: pagination offset (default: 0)
}
```

**Response**:
```json
{
  "classes": [
    {
      "fully_qualified_name": "MyApp.Services.UserService",
      "name": "UserService",
      "namespace": "MyApp.Services",
      "file_path": "Services/UserService.cs",
      "line_number": 10
    }
  ],
  "total_count": 120,
  "has_more": false,
  "limit": 100,
  "offset": 0
}
```

**Notes**:
- Use pagination (`limit`/`offset`) for large projects
- `namespace` filter supports partial matching
- `has_more` indicates if more results are available

---

### Tool: `list_methods`

Enumerate all methods in a project.

**Endpoint**: `POST /api/tools/list_methods`

**Request Body**:
```json
{
  "project_id": "uuid",
  "class_name": "UserService",  // optional: filter by class name
  "namespace": "MyApp.Services",  // optional: filter by namespace
  "limit": 100,  // optional: pagination limit (default: 100)
  "offset": 0    // optional: pagination offset (default: 0)
}
```

**Response**:
```json
{
  "methods": [
    {
      "fully_qualified_name": "MyApp.Services.UserService.ValidateUser",
      "name": "ValidateUser",
      "class_name": "UserService",
      "namespace": "MyApp.Services",
      "file_path": "Services/UserService.cs",
      "line_number": 42
    }
  ],
  "total_count": 450,
  "has_more": false,
  "limit": 100,
  "offset": 0
}
```

**Notes**:
- Filters can be combined (e.g., class + namespace)
- Fully qualified names are in format: `Namespace.ClassName.MethodName`

---

### Tool: `list_entry_points`

Enumerate entry points (Main methods, controllers, API endpoints, etc.).

**Endpoint**: `POST /api/tools/list_entry_points`

**Request Body**:
```json
{
  "project_id": "uuid",
  "type": "all"  // optional: "controllers" | "main" | "api" | "all" (default: "all")
}
```

**Response**:
```json
{
  "entry_points": [
    {
      "type": "controller",
      "method": "MyApp.Controllers.LoginController.Login",
      "file_path": "Controllers/LoginController.cs",
      "line_number": 15,
      "route": "/api/login",  // if detectable, null otherwise
      "http_method": "POST"   // if detectable, null otherwise
    },
    {
      "type": "main",
      "method": "MyApp.Program.Main",
      "file_path": "Program.cs",
      "line_number": 10,
      "route": null,
      "http_method": null
    }
  ],
  "total_count": 12
}
```

**Entry Point Types**:
- `controller` - ASP.NET controllers (methods with `[HttpPost]`, `[HttpGet]`, etc.)
- `main` - Program entry points (`Main` methods)
- `api` - API endpoints (may overlap with controllers)
- `all` - All entry point types

---

### Tool: `get_method`

Get detailed information about a specific method.

**Endpoint**: `POST /api/tools/get_method`

**Request Body**:
```json
{
  "project_id": "uuid",
  "method": "MyApp.Services.UserService.ValidateUser"
}
```

**Response**:
```json
{
  "method": {
    "fully_qualified_name": "MyApp.Services.UserService.ValidateUser",
    "name": "ValidateUser",
    "class_name": "UserService",
    "namespace": "MyApp.Services",
    "return_type": "bool",
    "parameters": ["string username", "string password"],
    "access_modifier": "public",
    "is_static": false,
    "is_virtual": false,
    "is_abstract": false,
    "file_path": "Services/UserService.cs",
    "line_number": 42
  }
}
```

**Error**: Returns `404` with `METHOD_NOT_FOUND` if method doesn't exist.

---

### Tool: `get_class`

Get detailed information about a specific class.

**Endpoint**: `POST /api/tools/get_class`

**Request Body**:
```json
{
  "project_id": "uuid",
  "class": "MyApp.Services.UserService"
}
```

**Response**:
```json
{
  "class": {
    "fully_qualified_name": "MyApp.Services.UserService",
    "name": "UserService",
    "namespace": "MyApp.Services",
    "access_modifier": "public",
    "is_static": false,
    "is_abstract": false,
    "is_sealed": false,
    "base_class": null,  // fully qualified name, or null
    "interfaces": ["IDisposable"],  // list of interface names
    "file_path": "Services/UserService.cs",
    "line_number": 10,
    "method_count": 8,
    "property_count": 3,
    "field_count": 2
  }
}
```

**Error**: Returns `404` with `CLASS_NOT_FOUND` if class doesn't exist.

---

### Tool: `get_callers`

Get methods that call a target method (walk UP the call tree).

**Endpoint**: `POST /api/tools/get_callers`

**Request Body**:
```json
{
  "project_id": "uuid",
  "method": "MyApp.Services.UserService.ValidateUser",
  "depth": 1,  // optional: 1 = direct callers only, 2 = callers of callers, etc. (default: 1)
  "include_self": false  // optional: include the method itself in results (default: false)
}
```

**Response**:
```json
{
  "target_method": "MyApp.Services.UserService.ValidateUser",
  "callers": [
    {
      "method": "MyApp.Controllers.LoginController.Login",
      "file_path": "Controllers/LoginController.cs",
      "line_number": 15,
      "depth": 1  // distance from target (1 = direct caller)
    },
    {
      "method": "MyApp.Controllers.AdminController.VerifyAdmin",
      "file_path": "Controllers/AdminController.cs",
      "line_number": 22,
      "depth": 1
    }
  ],
  "total_count": 2,
  "max_depth_reached": false  // true if depth limit was hit
}
```

**Notes**:
- `depth: 1` returns only direct callers
- For deeper traversal, agent can recursively call with each caller
- `max_depth_reached` indicates if there are more callers beyond the requested depth

---

### Tool: `get_callees`

Get methods called by a method (walk DOWN the call tree).

**Endpoint**: `POST /api/tools/get_callees`

**Request Body**:
```json
{
  "project_id": "uuid",
  "method": "MyApp.Services.UserService.ValidateUser",
  "depth": 1,  // optional: 1 = direct callees only, 2 = callees of callees, etc. (default: 1)
  "include_self": false  // optional: include the method itself in results (default: false)
}
```

**Response**:
```json
{
  "source_method": "MyApp.Services.UserService.ValidateUser",
  "callees": [
    {
      "method": "MyApp.Data.UserRepository.FindUser",
      "file_path": "Services/UserService.cs",
      "line_number": 45,
      "depth": 1  // distance from source (1 = direct callee)
    },
    {
      "method": "MyApp.Services.PasswordHasher.VerifyHash",
      "file_path": "Services/UserService.cs",
      "line_number": 46,
      "depth": 1
    }
  ],
  "total_count": 2,
  "max_depth_reached": false
}
```

**Notes**:
- `depth: 1` returns only direct callees
- For deeper traversal, agent can recursively call with each callee
- `max_depth_reached` indicates if there are more callees beyond the requested depth

---

### Tool: `get_class_methods`

Get all methods in a class.

**Endpoint**: `POST /api/tools/get_class_methods`

**Request Body**:
```json
{
  "project_id": "uuid",
  "class": "MyApp.Services.UserService"
}
```

**Response**:
```json
{
  "class": "MyApp.Services.UserService",
  "methods": [
    {
      "fully_qualified_name": "MyApp.Services.UserService.ValidateUser",
      "name": "ValidateUser",
      "return_type": "bool",
      "access_modifier": "public",
      "is_static": false,
      "file_path": "Services/UserService.cs",
      "line_number": 42
    }
  ],
  "total_count": 8
}
```

---

### Tool: `get_class_references`

Get classes that reference/use this class.

**Endpoint**: `POST /api/tools/get_class_references`

**Request Body**:
```json
{
  "project_id": "uuid",
  "class": "MyApp.Services.UserService",
  "relationship_type": "all"  // optional: "calls_methods" | "inherits" | "implements" | "all" (default: "all")
}
```

**Response**:
```json
{
  "class": "MyApp.Services.UserService",
  "referenced_by": [
    {
      "class": "MyApp.Controllers.LoginController",
      "relationship": "calls_methods",
      "methods_called": [
        "MyApp.Services.UserService.ValidateUser",
        "MyApp.Services.UserService.CreateUser"
      ]
    },
    {
      "class": "MyApp.Services.UserServiceExtended",
      "relationship": "inherits",
      "methods_called": []
    }
  ],
  "total_count": 5
}
```

**Relationship Types**:
- `calls_methods` - Classes that call methods on this class
- `inherits` - Classes that inherit from this class
- `implements` - Classes that implement this class (if it's an interface)
- `all` - All relationship types

---

### Tool: `search_code`

Semantic search for code elements using natural language.

**Endpoint**: `POST /api/tools/search_code`

**Request Body**:
```json
{
  "project_id": "uuid",
  "query": "authentication methods that validate user credentials",
  "limit": 10,  // optional: max results (default: 10)
  "types": ["methods", "classes"],  // optional: filter by element type (default: all)
  "min_similarity": 0.7  // optional: minimum similarity score 0.0-1.0 (default: 0.0)
}
```

**Response**:
```json
{
  "query": "authentication methods that validate user credentials",
  "results": [
    {
      "element": {
        "type": "method",
        "fully_qualified_name": "MyApp.Services.UserService.ValidateUser",
        "name": "ValidateUser",
        "class_name": "UserService",
        "namespace": "MyApp.Services",
        "file_path": "Services/UserService.cs",
        "line_number": 42
      },
      "similarity": 0.92,  // similarity score 0.0-1.0
      "snippet": "Method ValidateUser in class UserService validates user credentials..."
    }
  ],
  "total_results": 5,
  "search_time_ms": 45
}
```

**Notes**:
- Uses vector similarity search
- `types` can include: `"methods"`, `"classes"`, `"method_calls"`
- Results are sorted by similarity (highest first)

---

### Tool: `get_project_status`

Get project analysis status and statistics.

**Endpoint**: `POST /api/tools/get_project_status`

**Request Body**:
```json
{
  "project_id": "uuid"
}
```

**Response**:
```json
{
  "project_id": "uuid",
  "status": "ready|analyzing|error",
  "progress": {
    "files_processed": 45,
    "total_files": 85,
    "percentage": 52.9
  },
  "statistics": {
    "total_files": 85,
    "total_methods": 450,
    "total_classes": 120,
    "total_method_calls": 1250,
    "total_namespaces": 15
  },
  "errors": []  // populated if status is "error"
}
```

**Status Values**:
- `ready` - Analysis complete, project is queryable
- `analyzing` - Analysis in progress (check `progress` for details)
- `error` - Analysis failed (check `errors` for details)

---

## Resources

Resources provide read-only access to data and can be accessed via `GET /api/resources/{resource_uri}`. Resources are useful for caching and direct access without tool invocation overhead.

### Resource: `project://{project_id}`

Get project information.

**Endpoint**: `GET /api/resources/project/{project_id}`

**Response**: Same as `get_project_status` tool response.

---

### Resource: `method://{project_id}/{method_fqn}`

Get method details.

**Endpoint**: `GET /api/resources/method/{project_id}/{method_fqn}`

**Response**: Same as `get_method` tool response.

**Note**: Method FQN in URL should be URL-encoded (e.g., `MyApp.Services.UserService.ValidateUser` → `MyApp.Services.UserService.ValidateUser`).

---

### Resource: `class://{project_id}/{class_fqn}`

Get class details.

**Endpoint**: `GET /api/resources/class/{project_id}/{class_fqn}`

**Response**: Same as `get_class` tool response.

**Note**: Class FQN in URL should be URL-encoded.

---

## Tool Discovery

### Endpoint: `GET /api/tools`

List all available tools with their schemas (for MCP tool registration).

**Response**:
```json
{
  "tools": [
    {
      "name": "index_project",
      "description": "Analyze and index a C# project",
      "input_schema": {
        "type": "object",
        "properties": {
          "project_path": {
            "type": "string",
            "description": "Path to .csproj file or directory"
          },
          "project_name": {
            "type": "string",
            "description": "Optional project name (defaults to folder name)"
          }
        },
        "required": ["project_path"]
      }
    },
    {
      "name": "get_callers",
      "description": "Get methods that call a target method (walk UP the call tree)",
      "input_schema": {
        "type": "object",
        "properties": {
          "project_id": {
            "type": "string",
            "description": "Project identifier"
          },
          "method": {
            "type": "string",
            "description": "Fully qualified method name"
          },
          "depth": {
            "type": "integer",
            "description": "Traversal depth (1 = direct callers only)",
            "default": 1
          },
          "include_self": {
            "type": "boolean",
            "description": "Include the method itself in results",
            "default": false
          }
        },
        "required": ["project_id", "method"]
      }
    }
    // ... all other tools
  ]
}
```

---

## Agent Workflow Examples

### Example 1: "Who calls this method and who does it call?"

**Agent Workflow**:
1. Call `get_callers` with `depth: 1` to get direct callers
2. Call `get_callees` with `depth: 1` to get direct callees
3. Compose answer from both results

**API Calls**:
```json
POST /api/tools/get_callers
{
  "project_id": "abc123",
  "method": "MyApp.Services.UserService.ValidateUser",
  "depth": 1
}

POST /api/tools/get_callees
{
  "project_id": "abc123",
  "method": "MyApp.Services.UserService.ValidateUser",
  "depth": 1
}
```

---

### Example 2: "Where in the UI does a method get called?"

**Agent Workflow**:
1. Call `list_entry_points` to get all UI entry points
2. For each entry point, recursively walk down using `get_callees`
3. Check if target method appears in any call tree
4. If found, trace back the path

**API Calls**:
```json
POST /api/tools/list_entry_points
{
  "project_id": "abc123",
  "type": "all"
}

POST /api/tools/get_callees
{
  "project_id": "abc123",
  "method": "MyApp.Controllers.LoginController.Login",
  "depth": 5
}
// Repeat for each entry point, checking if target method appears
```

---

### Example 3: "Is this used in the code, or is it orphaned?"

**Agent Workflow**:
1. Call `get_callers` with `depth: 1`
2. If empty, check if it's an entry point
3. If not an entry point and has no callers, it's orphaned

**API Calls**:
```json
POST /api/tools/get_callers
{
  "project_id": "abc123",
  "method": "MyApp.Services.OldService.DeprecatedMethod",
  "depth": 1
}

POST /api/tools/list_entry_points
{
  "project_id": "abc123"
}
// Check if method is in entry points list
```

---

### Example 4: "Find all orphaned pieces of code"

**Agent Workflow**:
1. Call `list_methods` to get all methods (with pagination)
2. For each method:
   - Call `get_callers` with `depth: 1`
   - Call `list_entry_points` to check if it's an entry point
   - If no callers and not an entry point, mark as orphaned
3. Similar process for classes using `list_classes` and `get_class_references`

**API Calls**:
```json
POST /api/tools/list_methods
{
  "project_id": "abc123",
  "limit": 1000
}

POST /api/tools/get_callers
{
  "project_id": "abc123",
  "method": "{each_method}",
  "depth": 1
}
// Repeat for all methods
```

---

### Example 5: "Walk up the entire call tree to find all entry points"

**Agent Workflow**:
1. Start with target method
2. Call `get_callers` with `depth: 1`
3. For each caller, recursively call `get_callers`
4. Stop when `get_callers` returns empty (found entry point)
5. Collect all paths

**API Calls**:
```json
POST /api/tools/get_callers
{
  "project_id": "abc123",
  "method": "MyApp.Services.UserService.ValidateUser",
  "depth": 1
}
// For each result, recursively call get_callers
```

---

## Performance Considerations

1. **Pagination**: Use `limit` and `offset` for enumeration endpoints on large projects
2. **Depth Control**: Use `depth: 1` for initial queries, then recursively traverse as needed
3. **Caching**: Resources can be cached by agents/clients
4. **Batch Operations**: Consider implementing batch endpoints for common multi-query patterns
5. **Async Operations**: Long-running `index_project` operations return immediately; poll status

---

## Future Enhancements

Potential additions for future versions:

- **Batch Operations**: Endpoints to query multiple methods/classes at once
- **WebSocket Support**: Real-time updates for analysis progress
- **GraphQL Endpoint**: Alternative query interface
- **Filtering Enhancements**: More sophisticated filtering options
- **Relationship Queries**: Direct queries for specific relationship types
- **Source Code Access**: Endpoints to retrieve actual source code snippets
- **Change Tracking**: Track when code elements were last modified
- **Dependency Analysis**: Analyze NuGet package dependencies

---

## Version History

- **1.0** (2025-01-15): Initial API specification

---

## Appendix: Fully Qualified Name Format

All fully qualified names (FQNs) follow this format:

- **Methods**: `Namespace.ClassName.MethodName`
- **Classes**: `Namespace.ClassName`
- **Namespaces**: `Namespace` or `Namespace.SubNamespace`

Examples:
- Method: `MyApp.Services.UserService.ValidateUser`
- Class: `MyApp.Services.UserService`
- Namespace: `MyApp.Services`

---

## Appendix: MCP Mapping

This API maps to MCP concepts as follows:

- **MCP Tools** → `POST /api/tools/{tool_name}`
- **MCP Resources** → `GET /api/resources/{resource_uri}`
- **MCP Prompts** → (Not yet implemented, may be added in future)

For MCP tool registration, use the `GET /api/tools` endpoint to retrieve tool schemas.

