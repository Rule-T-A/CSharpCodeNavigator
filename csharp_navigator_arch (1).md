# C# Code Navigator - Architecture & Implementation Guide

## Overview

This document describes the architecture and implementation plan for a C# code navigation and understanding tool that enables AI-powered code exploration through Claude Desktop. The system analyzes C# projects to extract method call relationships, stores them in a vector database, and exposes navigation capabilities through an MCP server.

### Core Capabilities

1. **Call Path Navigation**: Find all routes to reach a specific method or navigate from a method to see where code can go
2. **AI-Powered Documentation**: Generate and store semantic descriptions of code elements for natural language search
3. **Visual Diagrams**: Create Mermaid sequence diagrams and UML class diagrams from code structure
4. **Claude Desktop Integration**: Expose all capabilities as tools Claude can orchestrate

### Technology Stack

- **Analysis**: Roslyn (C# compiler platform) for semantic code analysis
- **Storage**: VectorStore (file-based vector database) for embedding-based storage and search
- **Integration**: MCP (Model Context Protocol) server for Claude Desktop
- **Language**: C# / .NET 9

---

## System Architecture

### Component Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    Indexing UI Application                   │
│  • Load C# projects                                          │
│  • Trigger analysis                                          │
│  • Configure settings                                        │
│  • Monitor progress                                          │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│              Roslyn Analyzer (Assembly)                      │
│  • Parse C# syntax trees                                     │
│  • Semantic analysis (method calls, symbols)                 │
│  • Extract relationships                                     │
│  • Output structured data                                    │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│          CSharpSimpleVector (Vector Database)                │
│  • Method call relationships                                 │
│  • Code element descriptions                                 │
│  • Semantic search capability                                │
│  • Metadata storage                                          │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│             MCP Server (Background Service)                  │
│                                                               │
│  ┌─────────────────────────────────────────┐                │
│  │         Call Index (In-Memory)          │                │
│  │  • Fast graph lookups                   │                │
│  │  • Built from vector store at startup   │                │
│  └─────────────────────────────────────────┘                │
│                                                               │
│  ┌─────────────────────────────────────────┐                │
│  │         Path Finding Logic              │                │
│  │  • BFS/DFS traversal                    │                │
│  │  • Find paths to/from methods           │                │
│  └─────────────────────────────────────────┘                │
│                                                               │
│  ┌─────────────────────────────────────────┐                │
│  │       Diagram Generation                │                │
│  │  • Mermaid sequence diagrams            │                │
│  │  • UML class diagrams                   │                │
│  └─────────────────────────────────────────┘                │
│                                                               │
│  ┌─────────────────────────────────────────┐                │
│  │          MCP Protocol Layer             │                │
│  │  • JSON-RPC over stdio                  │                │
│  │  • Tool registration and execution      │                │
│  └─────────────────────────────────────────┘                │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│                    Claude Desktop                            │
│  • Orchestrates queries                                      │
│  • Uses exposed MCP tools                                    │
│  • Presents results to user                                  │
└─────────────────────────────────────────────────────────────┘
```

### Data Flow

1. **Indexing Phase**: UI app triggers Roslyn analyzer → extracts call relationships → stores in vector database
2. **Server Startup**: MCP server loads vector store → builds in-memory call index → exposes tools
3. **Query Phase**: Claude Desktop calls MCP tools → server uses call index for navigation → returns results
4. **Search Phase**: Claude queries vector store semantically → finds relevant code elements

### Why This Architecture

- **Separate assemblies**: Roslyn analyzer can be reused, tested independently, versioned separately
- **Vector store as persistence**: Single source of truth, enables semantic search, no additional database needed
- **In-memory call index**: Fast O(1) lookups for path finding without full vector store traversal
- **UI separate from server**: Server runs as background service, can operate without UI, supports automation
- **MCP integration**: Standard protocol for AI tool integration, works with Claude Desktop and other MCP clients

---

## Data Model

### Vector Store Chunk Types

The vector database stores three types of chunks, distinguished by metadata:

#### 1. Method Call Relationships

**Purpose**: Enable path finding and call graph navigation

```
Content: "Method LoginController.Login in class LoginController calls method 
          UserService.ValidateUser in class UserService. This call happens in 
          file Controllers/LoginController.cs at line 42."

Metadata: {
  "type": "method_call",
  "caller": "MyApp.Controllers.LoginController.Login",
  "callee": "MyApp.Services.UserService.ValidateUser",
  "caller_class": "LoginController",
  "callee_class": "UserService",
  "caller_namespace": "MyApp.Controllers",
  "callee_namespace": "MyApp.Services",
  "file_path": "Controllers/LoginController.cs",
  "line_number": 42
}
```

#### 2. Method Descriptions

**Purpose**: Enable semantic search and AI-generated documentation

```
Content: "The ValidateUser method in UserService authenticates user credentials 
          by checking the provided username and password against stored values. 
          It returns true if credentials are valid, false otherwise. This method 
          is commonly called during login flows and API authentication."

Metadata: {
  "type": "method_description",
  "method": "MyApp.Services.UserService.ValidateUser",
  "class": "UserService",
  "namespace": "MyApp.Services",
  "return_type": "bool",
  "parameters": "string username, string password",
  "file_path": "Services/UserService.cs",
  "line_number": 15
}
```

#### 3. Code Element Information

**Purpose**: Support general code search and context

```
Content: "UserService class in namespace MyApp.Services. Contains methods for 
          user authentication, validation, and profile management. Key methods 
          include ValidateUser, CreateUser, UpdateProfile, and DeleteUser."

Metadata: {
  "type": "code_element",
  "element_type": "class",
  "name": "UserService",
  "namespace": "MyApp.Services",
  "file_path": "Services/UserService.cs",
  "method_count": 8,
  "is_public": true
}
```

### Metadata Schema Standards

- **Fully qualified names**: Always use namespace.class.method format for unambiguous identification
- **File paths**: Relative to project root for portability
- **Line numbers**: 1-based indexing matching editor conventions
- **Type field**: Always present, enables filtering without semantic search
- **Namespace hierarchy**: Preserve full namespace path for accurate resolution

---

## Phase 1: Roslyn Analysis & Indexing Pipeline

**Owner**: Katie (if she wants the challenge) or Dad

**Purpose**: Extract method call relationships from C# projects and populate the vector database with structured, searchable data.

**Prerequisites**: 
- CSharpSimpleVector library available
- .NET 8 SDK installed
- Understanding of C# project structure

**Deliverables**:
- `CodeAnalyzer.Roslyn` assembly
- Working analyzer that processes .csproj files
- Populated vector store with call relationships
- Console test harness for verification

### Technical Context: Understanding Roslyn

Roslyn is the .NET compiler platform that provides APIs for analyzing C# code. Key concepts:

**Syntax Trees**: Parse source code into hierarchical structures representing code elements (classes, methods, statements)

**Semantic Model**: Resolves symbols, types, and references across files. Required for:
- Determining what method is being called (not just method name as text)
- Resolving types through aliases, using statements, and fully qualified names
- Following references across files and assemblies

**Compilation Context**: Roslyn needs references to all assemblies the code depends on (mscorlib, System.*, NuGet packages) to perform semantic analysis.

### Roslyn Implementation Guide

#### Setting Up Compilation Context

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;

// Load a project with all references resolved
var workspace = MSBuildWorkspace.Create();
var project = await workspace.OpenProjectAsync(projectPath);

// Alternative: Manual compilation for more control
var syntaxTrees = new List<SyntaxTree>();
foreach (var file in Directory.GetFiles(sourcePath, "*.cs", SearchOption.AllDirectories))
{
    var code = await File.ReadAllTextAsync(file);
    syntaxTrees.Add(CSharpSyntaxTree.ParseText(code, path: file));
}

// Add required references (critical for semantic analysis)
var references = new List<MetadataReference>
{
    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),     // mscorlib
    MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),    // System.Console
    MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location), // System.Linq
    // Add more as needed based on project dependencies
};

var compilation = CSharpCompilation.Create(
    assemblyName: "AnalysisCompilation",
    syntaxTrees: syntaxTrees,
    references: references,
    options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
);
```

#### Extracting Method Calls

```csharp
using Microsoft.CodeAnalysis.CSharp.Syntax;

// For each syntax tree in compilation
foreach (var tree in compilation.SyntaxTrees)
{
    var semanticModel = compilation.GetSemanticModel(tree);
    var root = await tree.GetRootAsync();
    
    // Find all method invocations
    var invocations = root.DescendantNodes()
        .OfType<InvocationExpressionSyntax>();
    
    foreach (var invocation in invocations)
    {
        // Get the method being called (semantic analysis)
        var symbolInfo = semanticModel.GetSymbolInfo(invocation);
        if (symbolInfo.Symbol is not IMethodSymbol calledMethod)
            continue; // Skip if can't resolve
        
        // Find the containing method (what's making the call)
        var containingMethod = invocation.Ancestors()
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault();
        
        if (containingMethod == null)
            continue; // Skip calls outside methods (e.g., field initializers)
        
        var containingSymbol = semanticModel.GetDeclaredSymbol(containingMethod);
        if (containingSymbol == null)
            continue;
        
        // Extract fully qualified names
        var caller = GetFullyQualifiedName(containingSymbol);
        var callee = GetFullyQualifiedName(calledMethod);
        
        // Get location information
        var location = invocation.GetLocation();
        var lineSpan = location.GetLineSpan();
        
        // Store in vector database
        await StoreMethodCall(
            caller: caller,
            callee: callee,
            callerClass: containingSymbol.ContainingType.Name,
            calleeClass: calledMethod.ContainingType.Name,
            callerNamespace: containingSymbol.ContainingNamespace.ToDisplayString(),
            calleeNamespace: calledMethod.ContainingNamespace.ToDisplayString(),
            filePath: lineSpan.Path,
            lineNumber: lineSpan.StartLinePosition.Line + 1
        );
    }
}

string GetFullyQualifiedName(ISymbol symbol)
{
    // Format: Namespace.ClassName.MethodName
    var parts = new List<string>();
    
    // Add method name
    parts.Add(symbol.Name);
    
    // Add containing type
    if (symbol.ContainingType != null)
        parts.Insert(0, symbol.ContainingType.Name);
    
    // Add namespace
    if (symbol.ContainingNamespace != null && 
        !symbol.ContainingNamespace.IsGlobalNamespace)
    {
        parts.Insert(0, symbol.ContainingNamespace.ToDisplayString());
    }
    
    return string.Join(".", parts);
}
```

#### Storing in Vector Database

```csharp
async Task StoreMethodCall(
    string caller,
    string callee,
    string callerClass,
    string calleeClass,
    string callerNamespace,
    string calleeNamespace,
    string filePath,
    int lineNumber)
{
    var content = $"Method {caller} in class {callerClass} calls method {callee} " +
                  $"in class {calleeClass}. This call happens in file {filePath} " +
                  $"at line {lineNumber}.";
    
    var metadata = new Dictionary<string, object>
    {
        ["type"] = "method_call",
        ["caller"] = caller,
        ["callee"] = callee,
        ["caller_class"] = callerClass,
        ["callee_class"] = calleeClass,
        ["caller_namespace"] = callerNamespace,
        ["callee_namespace"] = calleeNamespace,
        ["file_path"] = filePath,
        ["line_number"] = lineNumber
    };
    
    await vectorStore.AddTextAsync(content, metadata);
}
```

#### Common Pitfalls and Solutions

**Pitfall 1**: Null symbol resolution
- **Cause**: Missing assembly references, code doesn't compile
- **Solution**: Check compilation.GetDiagnostics() for errors, ensure all references are added

**Pitfall 2**: Wrong method symbol for overloaded methods
- **Cause**: Multiple methods with same name
- **Solution**: SymbolInfo includes signature, use it for disambiguation

**Pitfall 3**: Generic methods show as open generics
- **Cause**: Generic type parameters not resolved
- **Solution**: Use `calledMethod.OriginalDefinition` or format with type arguments

**Pitfall 4**: Extension methods appear as static calls
- **Cause**: Extension methods are syntactic sugar for static calls
- **Solution**: Check `calledMethod.IsExtensionMethod` and record appropriately

### Success Criteria

- [ ] Analyzer can load a .csproj file and resolve all references
- [ ] Semantic model successfully resolves method calls across files
- [ ] Vector store contains method_call chunks with correct metadata
- [ ] Fully qualified names are accurate and unambiguous
- [ ] File paths and line numbers are correct
- [ ] Can analyze a project with 50+ files without errors
- [ ] Generated data matches the documented schema

### TDD Approach

1. **Write test with sample C# code**: Create test files with known method calls
2. **Define expected output**: Document what calls should be found
3. **Implement analyzer**: Use Roslyn to extract calls
4. **Verify against test**: Confirm all expected calls are found with correct metadata
5. **Add edge cases**: Test inheritance, interfaces, async methods, lambdas

### Checklist

- [ ] Create `CodeAnalyzer.Roslyn` project
- [ ] Add Roslyn NuGet packages (Microsoft.CodeAnalysis.CSharp, Microsoft.CodeAnalysis.CSharp.Workspaces)
- [ ] Add reference to CSharpSimpleVector
- [ ] Write tests for basic method call extraction
- [ ] Implement compilation context setup
- [ ] Implement method call extraction with semantic model
- [ ] Handle fully qualified name generation
- [ ] Store results in vector store with correct metadata schema
- [ ] Write tests for edge cases (overloads, generics, extension methods)
- [ ] Create console test harness
- [ ] Verify on real project (e.g., CSharpSimpleVector itself)
- [ ] Document any Roslyn quirks discovered

### AI Prompting Guidance

When working with an AI assistant on this phase:

**Provide**:
- The Roslyn code examples above
- Sample C# code to analyze
- Expected output (what calls should be found)
- The vector store metadata schema

**Ask for**:
- Explanation of any Roslyn concepts that are unclear
- Help debugging symbol resolution issues
- Suggestions for handling edge cases
- Code review of semantic analysis logic

**Example prompt**:
```
I'm using Roslyn to extract method calls from C# code. Here's my current 
implementation: [paste code]. It works for simple cases but fails when 
analyzing [specific scenario]. The error is [error message]. How should 
I handle this case?
```

### Verification

**Unit tests pass**: All test cases with known call relationships pass

**Real project analysis**: Successfully analyze a non-trivial project (50+ files, multiple assemblies)

**Data integrity**: Query vector store, confirm:
- All expected method calls are present
- Metadata is correctly structured
- Fully qualified names are accurate
- No duplicate entries (same call stored multiple times)

**Performance**: Complete analysis in reasonable time (< 30 seconds for 100 files)

---

## Phase 2: Call Index Builder

**Owner**: Katie

**Purpose**: Build an in-memory graph structure from vector store metadata for fast path-finding operations.

**Prerequisites**: 
- Phase 1 complete with populated vector store
- Understanding of dictionaries and graph data structures
- CSharpSimpleVector API familiarity

**Deliverables**:
- `CallIndex` class with fast lookup capabilities
- Ability to query "what does this call?" and "what calls this?"
- Unit tests verifying index correctness

### Technical Context

The vector database excels at semantic search but not at exact graph traversal. For path finding (goal #1 and #2), we need O(1) lookup of relationships:

**Forward index**: Given method X, what methods does X call?
**Reverse index**: Given method Y, what methods call Y?

The `CallIndex` loads all method_call chunks once at startup, extracts metadata, and builds dictionaries for fast lookups. This trades memory (storing relationships twice) for query speed.

### Interface Contract

```csharp
namespace CodeAnalyzer.Navigation;

/// <summary>
/// In-memory index of method call relationships built from vector store.
/// Provides O(1) lookups for path finding operations.
/// </summary>
public interface ICallIndex
{
    /// <summary>
    /// Total number of unique methods in the index
    /// </summary>
    int MethodCount { get; }
    
    /// <summary>
    /// Total number of call relationships
    /// </summary>
    int RelationshipCount { get; }
    
    /// <summary>
    /// Build index by loading all method_call chunks from vector store
    /// </summary>
    Task BuildFromVectorStoreAsync(FileVectorStore vectorStore);
    
    /// <summary>
    /// Get all methods that this method calls (outgoing edges)
    /// </summary>
    List<string> GetCallees(string methodName);
    
    /// <summary>
    /// Get all methods that call this method (incoming edges)
    /// </summary>
    List<string> GetCallers(string methodName);
    
    /// <summary>
    /// Check if a method exists in the index
    /// </summary>
    bool HasMethod(string methodName);
    
    /// <summary>
    /// Get all methods (for enumeration)
    /// </summary>
    IEnumerable<string> GetAllMethods();
}
```

### Data Structures

The implementation should maintain:

```csharp
// Forward index: method → methods it calls
private Dictionary<string, List<string>> _calls;

// Reverse index: method → methods that call it
private Dictionary<string, List<string>> _calledBy;

// Optional: Store method metadata for context
private Dictionary<string, MethodMetadata> _methodInfo;
```

### Success Criteria

- [ ] Index loads all method_call chunks from vector store
- [ ] GetCallees returns correct list of methods called
- [ ] GetCallers returns correct list of callers
- [ ] Empty list returned for methods with no calls (not null)
- [ ] MethodCount matches number of unique methods
- [ ] RelationshipCount matches number of stored calls
- [ ] Handles methods that exist only as caller or only as callee
- [ ] Build completes in < 5 seconds for 1000 method calls

### TDD Approach

1. **Write test with mock vector store**: Create test chunks with known relationships
2. **Define test graph**: A→B, B→C, A→C (clear expected structure)
3. **Implement BuildFromVectorStoreAsync**: Load and parse chunks
4. **Verify lookups**: Test GetCallees and GetCallers return correct results
5. **Test edge cases**: Empty results, circular references, missing methods

### Checklist

- [ ] Write test: Build index from vector store with known data
- [ ] Write test: GetCallees returns correct methods
- [ ] Write test: GetCallers returns correct methods
- [ ] Write test: HasMethod returns true/false appropriately
- [ ] Implement ICallIndex interface
- [ ] Implement BuildFromVectorStoreAsync:
  - [ ] Use vectorStore.GetAllIdsAsync()
  - [ ] For each ID, use vectorStore.GetAsync(id)
  - [ ] Filter for metadata["type"] == "method_call"
  - [ ] Extract caller and callee from metadata
  - [ ] Populate _calls dictionary (forward)
  - [ ] Populate _calledBy dictionary (reverse)
- [ ] Implement GetCallees (return empty list if not found)
- [ ] Implement GetCallers (return empty list if not found)
- [ ] Implement HasMethod
- [ ] Implement GetAllMethods
- [ ] Add null safety checks
- [ ] Test with real vector store from Phase 1
- [ ] Verify counts match expected values
- [ ] Document any performance considerations

### AI Prompting Guidance

**Provide**:
- The ICallIndex interface above
- Test cases with expected behavior
- Vector store API reference (GetAllIdsAsync, GetAsync)
- Sample metadata structure

**Ask for**:
- Implementation of BuildFromVectorStoreAsync
- Efficient dictionary population
- Null handling strategies
- Performance optimization if needed

**Example prompt**:
```
I need to implement ICallIndex.BuildFromVectorStoreAsync. The method should:
1. Load all documents from the vector store
2. Filter for type == "method_call"
3. Build two dictionaries: forward (_calls) and reverse (_calledBy)

Here's my test that needs to pass: [paste test]
Here's the interface: [paste interface]
Vector store API: [paste relevant methods]

Please implement BuildFromVectorStoreAsync.
```

### Verification

**Unit tests pass**: All test cases pass with 100% correctness

**Integration test**: Load index from Phase 1 vector store, verify:
- MethodCount > 0
- RelationshipCount > 0
- Sample lookups return expected results
- No exceptions during build

**Data integrity**: For random samples:
- If A→B in vector store, GetCallees("A") contains "B"
- If A→B in vector store, GetCallers("B") contains "A"

---

## Phase 3: Path Finding

**Owner**: Katie

**Purpose**: Implement graph traversal algorithms to find call paths between methods, supporting goals #1 (paths to) and #2 (paths from).

**Prerequisites**:
- Phase 2 complete with working CallIndex
- Understanding of breadth-first search (BFS) algorithm
- Understanding of recursion or iterative stack-based traversal

**Deliverables**:
- `PathFinder` class with path discovery capabilities
- Backward traversal (find all paths TO a method)
- Forward traversal (find all reachable methods FROM a method)
- Cycle detection and max depth handling

### Technical Context

Path finding on call graphs is graph traversal:

**Backward traversal** (goal #1): Start from target method, follow "who calls this" edges until reaching entry points (methods no one calls)

**Forward traversal** (goal #2): Start from entry method, follow "what does this call" edges up to max depth

**Challenges**:
- **Cycles**: Method A calls B, B calls A (infinite loop without cycle detection)
- **Multiple paths**: Same method may be reached through different routes
- **Depth control**: Deep call chains need max depth limit

**Algorithm choice**: Breadth-first search (BFS) finds shortest paths first and naturally handles depth limiting.

### Interface Contract

```csharp
namespace CodeAnalyzer.Navigation;

/// <summary>
/// Finds call paths through the method call graph
/// </summary>
public interface IPathFinder
{
    /// <summary>
    /// Find all paths that lead TO the target method (backward traversal).
    /// Returns paths from entry points to target.
    /// </summary>
    /// <param name="targetMethod">Method to find paths to</param>
    /// <param name="maxDepth">Maximum path length (default 10)</param>
    /// <returns>List of paths, each path is a list of method names</returns>
    List<List<string>> FindPathsTo(string targetMethod, int maxDepth = 10);
    
    /// <summary>
    /// Find all methods reachable FROM the start method (forward traversal).
    /// Returns methods grouped by distance from start.
    /// </summary>
    /// <param name="startMethod">Method to traverse from</param>
    /// <param name="maxDepth">Maximum traversal depth (default 5)</param>
    /// <returns>Dictionary of depth → list of methods at that depth</returns>
    Dictionary<int, List<string>> FindPathsFrom(string startMethod, int maxDepth = 5);
}
```

### Algorithm Notes

**For FindPathsTo (backward)**:
```
Start from target
For each caller of current method:
  - If caller hasn't been visited in this path (avoid cycles)
  - Add caller to path
  - If caller has no callers (entry point):
    - Save complete path
  - Else if depth < maxDepth:
    - Recursively find paths to caller
```

**For FindPathsFrom (forward)**:
```
Queue ← [startMethod at depth 0]
Visited ← empty set

While queue not empty and depth ≤ maxDepth:
  - Dequeue method and depth
  - If method in visited, skip (cycle)
  - Add method to visited
  - Add method to results at current depth
  - For each callee:
    - Enqueue callee at depth + 1
```

### Success Criteria

- [ ] FindPathsTo finds all paths to target method
- [ ] FindPathsTo stops at entry points (no callers)
- [ ] FindPathsTo respects maxDepth limit
- [ ] FindPathsTo handles cycles without infinite loops
- [ ] FindPathsFrom finds all reachable methods
- [ ] FindPathsFrom groups methods by depth correctly
- [ ] FindPathsFrom respects maxDepth limit
- [ ] FindPathsFrom handles cycles without infinite loops
- [ ] Empty input returns empty result (not null or exception)
- [ ] Non-existent methods return empty result

### TDD Approach

1. **Write test with simple linear path**: A→B→C, find paths to C, expect [A,B,C]
2. **Write test with branching**: A→B→D, A→C→D, find paths to D, expect two paths
3. **Write test with cycle**: A→B→C→A, verify no infinite loop
4. **Implement FindPathsTo** using BFS or DFS with visited tracking
5. **Write test for forward traversal**: Start at A, expect all reachable methods grouped by depth
6. **Implement FindPathsFrom** using BFS with depth tracking

### Checklist

- [ ] Write test: Linear path (A→B→C), find paths to C
- [ ] Write test: Multiple paths to target
- [ ] Write test: Cycle detection (A→B→A)
- [ ] Write test: Max depth limiting
- [ ] Write test: Entry point detection (no callers)
- [ ] Implement FindPathsTo:
  - [ ] Use CallIndex.GetCallers for backward traversal
  - [ ] Track visited methods per path (avoid cycles)
  - [ ] Detect entry points (GetCallers returns empty)
  - [ ] Respect maxDepth parameter
  - [ ] Return paths as List<List<string>>
- [ ] Write test: Forward traversal groups by depth
- [ ] Write test: Forward traversal respects max depth
- [ ] Write test: Forward traversal detects cycles
- [ ] Implement FindPathsFrom:
  - [ ] Use CallIndex.GetCallees for forward traversal
  - [ ] Use BFS with queue
  - [ ] Track global visited set (avoid cycles)
  - [ ] Group results by depth
  - [ ] Respect maxDepth parameter
- [ ] Add null/empty input handling
- [ ] Add tests for edge cases (non-existent methods)
- [ ] Verify performance (< 1 second for graphs with 1000 nodes)
- [ ] Document algorithm complexity

### AI Prompting Guidance

**Provide**:
- The IPathFinder interface
- Test cases with graph diagrams
- CallIndex interface (GetCallers, GetCallees methods)
- Algorithm notes above

**Ask for**:
- Implementation of BFS/DFS algorithms
- Cycle detection strategies
- Help with recursive vs iterative approaches
- Performance optimization suggestions

**Example prompt**:
```
I need to implement FindPathsTo using breadth-first search. The method should:
1. Start from the target method
2. Follow GetCallers edges backward
3. Detect when reaching entry points (no callers)
4. Handle cycles without infinite loops
5. Respect maxDepth

Here's my test: [paste test with diagram]
Here's the interface: [paste interface]
CallIndex provides: GetCallers(method) → List<string>

Please implement FindPathsTo with clear comments explaining the algorithm.
```

### Verification

**Unit tests pass**: All test cases pass including edge cases

**Integration test with real data**:
- Load call index from Phase 1
- Pick a known deep method (e.g., low-level utility)
- Run FindPathsTo, verify paths make sense
- Pick an entry point (e.g., controller action)
- Run FindPathsFrom, verify depth grouping is correct

**Manual verification**:
- Trace one path manually through source code
- Confirm each edge (A→B) exists in code
- Verify no phantom edges

---

## Phase 4: Console Interface

**Owner**: Katie

**Purpose**: Create an interactive command-line interface for testing and debugging before MCP integration.

**Prerequisites**:
- Phases 1-3 complete
- Understanding of console I/O and command parsing

**Deliverables**:
- Interactive console application
- Commands for path finding
- Commands for searching
- Help system
- User-friendly output formatting

### Technical Context

Before integrating with Claude Desktop through MCP, we need a way to manually test and verify all functionality. A console interface allows:
- Quick iteration during development
- Easy debugging of path finding logic
- Verification that the system works end-to-end
- Demonstration to stakeholders
- Documentation of expected behavior

The console app should be user-friendly, with clear commands and well-formatted output that makes relationships easy to understand visually.

### Command Requirements

**Minimum viable commands**:
- `to <method>` - Find all paths TO a method
- `from <method>` - Find all reachable methods FROM a method
- `search <query>` - Semantic search in vector store
- `info <method>` - Show method details
- `help` - Show available commands
- `exit` - Quit application

**Output should be**:
- Clearly formatted with visual hierarchy
- Easy to scan quickly
- Shows relevant context (file paths, line numbers)
- Uses symbols/emoji for visual distinction (optional but helpful)

### Success Criteria

- [ ] Application loads call index on startup
- [ ] All commands work correctly
- [ ] Output is clear and readable
- [ ] Error messages are helpful (not just exceptions)
- [ ] Help text explains each command
- [ ] Can handle invalid input gracefully
- [ ] Performance is responsive (< 1 second per query)
- [ ] Output fits in standard terminal width (80-120 chars)

### TDD Approach

1. **Write test for command parser**: Verify commands parse correctly
2. **Implement parser**: Split input into command and arguments
3. **Write test for each command handler**: Mock dependencies, verify output format
4. **Implement handlers**: Call PathFinder, CallIndex, VectorStore as needed
5. **Manual testing**: Use console app interactively to verify user experience

### Checklist

- [ ] Create console application project
- [ ] Add references to all previous assemblies
- [ ] Write test for command parsing
- [ ] Implement command parser
- [ ] Write startup sequence:
  - [ ] Load vector store
  - [ ] Build call index
  - [ ] Show ready message with statistics
- [ ] Implement `to` command:
  - [ ] Call PathFinder.FindPathsTo
  - [ ] Format paths clearly (A → B → C)
  - [ ] Show path count
  - [ ] Handle method not found
- [ ] Implement `from` command:
  - [ ] Call PathFinder.FindPathsFrom
  - [ ] Group by depth level
  - [ ] Show total reachable count
  - [ ] Handle method not found
- [ ] Implement `search` command:
  - [ ] Call vectorStore.SearchTextAsync
  - [ ] Show results with similarity scores
  - [ ] Display relevant metadata
  - [ ] Handle no results
- [ ] Implement `info` command:
  - [ ] Show method details from call index
  - [ ] List what it calls
  - [ ] List what calls it
  - [ ] Display file location
- [ ] Implement `help` command with usage examples
- [ ] Implement `exit` command
- [ ] Add error handling for invalid input
- [ ] Add error handling for missing methods
- [ ] Format output with visual hierarchy
- [ ] Test with real indexed project
- [ ] Verify user experience is smooth

### AI Prompting Guidance

**Provide**:
- Command requirements above
- PathFinder and CallIndex interfaces
- Sample output format you want
- Vector store API for search

**Ask for**:
- Implementation of command parser
- Console output formatting suggestions
- Error handling patterns
- User experience improvements

**Example prompt**:
```
I need a console application that provides an interactive interface for 
code navigation. Required commands:
- to <method> - find paths to method
- from <method> - find reachable methods
- search <query> - semantic search
- help - show commands

The app should:
1. Load vector store and build call index on startup
2. Display a prompt for commands
3. Parse and execute commands
4. Format output clearly with visual hierarchy

Here are the interfaces I'm working with: [paste interfaces]

Please implement the main program loop and command handlers.
```

### Verification

**Functional testing**:
- Run each command with valid input
- Verify output matches expected format
- Test with known methods from indexed project

**Error testing**:
- Invalid commands show helpful error
- Non-existent methods handled gracefully
- Empty search results explained clearly

**User experience**:
- Commands feel responsive
- Output is easy to read
- Help text is clear
- Can navigate indexed project effectively

---

## Phase 5: Diagram Generation

**Owner**: Katie

**Purpose**: Generate Mermaid diagram syntax from code structure to support goal #4 (visual diagrams).

**Prerequisites**:
- Phase 3 complete (PathFinder working)
- Understanding of Mermaid diagram syntax
- Familiarity with string formatting

**Deliverables**:
- `MermaidGenerator` class
- Sequence diagram generation from call paths
- Class diagram generation from code structure
- Valid Mermaid syntax output

### Technical Context

Mermaid is a text-based diagramming language that renders in markdown viewers and many tools (including Claude). By generating Mermaid syntax from code analysis, we enable visual understanding of code flow.

**Sequence diagrams** show method call flows over time (goal #4)
**Class diagrams** show class relationships and structure

The generation is pure string formatting - take path data and format it according to Mermaid syntax rules.

### Mermaid Syntax Reference

**Sequence Diagram Format**:
```
sequenceDiagram
    participant A as ClassA
    participant B as ClassB
    A->>B: methodCall()
    B->>C: anotherMethod()
    C-->>B: return
```

**Class Diagram Format**:
```
classDiagram
    class ClassName {
        +methodName()
        +anotherMethod()
    }
    ClassA --> ClassB : calls
```

### Interface Contract

```csharp
namespace CodeAnalyzer.Diagrams;

/// <summary>
/// Generates Mermaid diagram syntax from code structure
/// </summary>
public interface IMermaidGenerator
{
    /// <summary>
    /// Generate sequence diagram from call paths.
    /// Shows temporal flow of method calls.
    /// </summary>
    /// <param name="paths">List of call paths from PathFinder</param>
    /// <param name="title">Optional diagram title</param>
    /// <returns>Valid Mermaid sequence diagram syntax</returns>
    string GenerateSequenceDiagram(List<List<string>> paths, string title = null);
    
    /// <summary>
    /// Generate class diagram showing relationships between classes.
    /// </summary>
    /// <param name="methods">Methods to include (fully qualified names)</param>
    /// <returns>Valid Mermaid class diagram syntax</returns>
    string GenerateClassDiagram(IEnumerable<string> methods);
}
```

### Success Criteria

- [ ] GenerateSequenceDiagram produces valid Mermaid syntax
- [ ] Sequence diagrams render correctly in Mermaid viewers
- [ ] GenerateClassDiagram produces valid Mermaid syntax
- [ ] Class diagrams render correctly in Mermaid viewers
- [ ] Handles empty input gracefully (returns minimal valid diagram)
- [ ] Large diagrams (50+ nodes) remain readable
- [ ] Method names are properly escaped/formatted
- [ ] Participant names extracted correctly from fully qualified names

### TDD Approach

1. **Write test with simple path**: [A.Method1, B.Method2], verify Mermaid syntax
2. **Implement GenerateSequenceDiagram**: Format as Mermaid sequence diagram
3. **Test output in Mermaid viewer**: Paste generated syntax, verify it renders
4. **Write test for class diagram**: List of methods, verify class relationships
5. **Implement GenerateClassDiagram**: Extract classes and format
6. **Test edge cases**: Empty input, long names, special characters

### Checklist

- [ ] Research Mermaid sequence diagram syntax
- [ ] Write test: Simple path generates valid syntax
- [ ] Write test: Multiple paths in one diagram
- [ ] Write test: Handles fully qualified names (extract class name)
- [ ] Implement GenerateSequenceDiagram:
  - [ ] Start with "sequenceDiagram" header
  - [ ] Extract class names from fully qualified method names
  - [ ] Create participant declarations
  - [ ] Generate call arrows (A->>B)
  - [ ] Add optional title
  - [ ] Handle duplicate participants
- [ ] Verify sequence diagram renders in Mermaid viewer
- [ ] Research Mermaid class diagram syntax
- [ ] Write test: Generate class diagram from methods
- [ ] Write test: Show relationships between classes
- [ ] Implement GenerateClassDiagram:
  - [ ] Extract unique class names from methods
  - [ ] Group methods by class
  - [ ] Generate class blocks
  - [ ] Generate relationship arrows
  - [ ] Handle classes with many methods (limit for readability)
- [ ] Verify class diagram renders in Mermaid viewer
- [ ] Add tests for edge cases (empty, special chars)
- [ ] Handle escaping if needed (quotes, special chars)
- [ ] Add optional parameters (max depth, filtering)
- [ ] Document Mermaid syntax used

### AI Prompting Guidance

**Provide**:
- The IMermaidGenerator interface
- Mermaid syntax reference above
- Sample input data (paths from PathFinder)
- Expected output examples

**Ask for**:
- String formatting logic
- Help with Mermaid syntax details
- Suggestions for readability (large diagrams)
- Edge case handling

**Example prompt**:
```
I need to generate Mermaid sequence diagrams from method call paths.

Input: List<List<string>> where each list is a call path like:
  ["MyApp.Controllers.LoginController.Login", 
   "MyApp.Services.UserService.ValidateUser",
   "MyApp.Data.UserRepository.FindUser"]

Output: Valid Mermaid sequence diagram syntax showing these calls.

Mermaid format:
sequenceDiagram
    participant A as ClassName
    A->>B: methodCall()

Requirements:
1. Extract class name from fully qualified name
2. Create participants for each unique class
3. Generate call arrows for each step in path
4. Handle multiple paths in same diagram

Here's my test: [paste test]
Please implement GenerateSequenceDiagram.
```

### Verification

**Syntax validation**:
- Paste generated Mermaid into viewer (https://mermaid.live)
- Verify diagram renders without errors
- Check visual layout is sensible

**Content accuracy**:
- Verify all methods in path appear in diagram
- Verify call order is correct (temporal flow)
- Verify class names extracted correctly

**Edge cases**:
- Empty input produces minimal valid diagram
- Long method names don't break layout
- Special characters handled correctly

---

## Phase 6: MCP Integration

**Owner**: Dad (scaffolding) + Katie (tool implementations)

**Purpose**: Expose all functionality as MCP tools for Claude Desktop orchestration.

**Prerequisites**:
- All previous phases complete and tested
- Understanding of JSON serialization
- Familiarity with Claude Desktop configuration

**Deliverables**:
- MCP server running as background service
- Tool registration and discovery
- Tool execution handlers
- Claude Desktop integration working

### Technical Context

The Model Context Protocol (MCP) enables AI assistants to use external tools. An MCP server:
- Runs as a separate process
- Communicates via JSON-RPC over stdio
- Registers available tools with descriptions
- Executes tool calls and returns results

The MCP server loads the vector store and call index once at startup, then serves requests from Claude Desktop. It's a thin integration layer over the functionality built in previous phases.

### Architecture Notes

**Separation of concerns**:
- UI application: Triggers indexing, writes to vector store
- MCP server: Reads from vector store, serves queries (no UI)
- This allows server to run independently as a service

**Process model**:
- MCP server runs continuously
- Loads data once at startup (call index)
- Responds to tool calls from Claude Desktop
- Can be restarted to reload updated data

### MCP Tools to Implement

The following tools should be exposed to Claude:

**Tool: find_paths_to**
```
Description: Find all call paths that lead to a target method
Parameters:
  - target (string, required): Fully qualified method name
  - max_depth (integer, optional): Maximum path length (default 10)
Returns:
  - target: Method name
  - paths: Array of call paths
  - count: Number of paths found
```

**Tool: find_paths_from**
```
Description: Find all methods reachable from a starting method
Parameters:
  - start (string, required): Fully qualified method name
  - max_depth (integer, optional): Maximum traversal depth (default 5)
Returns:
  - start: Method name
  - reachable_by_depth: Methods grouped by depth level
  - total_methods: Count of reachable methods
```

**Tool: search_code**
```
Description: Semantic search for code elements
Parameters:
  - query (string, required): Natural language search query
  - limit (integer, optional): Maximum results (default 10)
Returns:
  - query: Original search query
  - results: Array of matching code elements with similarity scores
  - count: Number of results
```

**Tool: explain_method**
```
Description: Get detailed information about a method
Parameters:
  - method (string, required): Fully qualified method name
Returns:
  - method: Method name
  - description: AI-generated or extracted description
  - calls: Methods this calls
  - called_by: Methods that call this
  - file_path: Source file location
  - line_number: Line in source file
```

**Tool: generate_sequence_diagram**
```
Description: Generate Mermaid sequence diagram from call flow
Parameters:
  - entry_point (string, required): Starting method
  - max_depth (integer, optional): How deep to trace (default 5)
Returns:
  - entry_point: Starting method
  - mermaid: Mermaid diagram syntax
  - method_count: Number of methods in diagram
```

**Tool: generate_class_diagram**
```
Description: Generate Mermaid class diagram for namespace or class
Parameters:
  - scope (string, required): Namespace or class name
Returns:
  - scope: What was analyzed
  - mermaid: Mermaid diagram syntax
  - class_count: Number of classes in diagram
```

**Tool: index_project**
```
Description: Analyze and index a C# project (triggers Phase 1 analyzer)
Parameters:
  - project_path (string, required): Path to .csproj or directory
Returns:
  - project_path: Path analyzed
  - methods_indexed: Count of methods indexed
  - relationships_found: Count of call relationships
  - status: Success or error message
```

### Success Criteria

- [ ] MCP server starts successfully
- [ ] Server loads call index from vector store
- [ ] All tools registered and discoverable by Claude
- [ ] Each tool executes without errors
- [ ] Tool responses match expected schema
- [ ] Claude Desktop can invoke tools
- [ ] Server handles errors gracefully
- [ ] Server can run without UI
- [ ] Performance is acceptable (< 2 seconds per query)

### Checklist

**Dad's responsibilities**:
- [ ] Set up MCP protocol JSON-RPC handler
- [ ] Implement tool registration mechanism
- [ ] Create tool execution dispatcher
- [ ] Add error handling and logging
- [ ] Configure stdio communication
- [ ] Test MCP protocol compliance
- [ ] Create Claude Desktop configuration file
- [ ] Document MCP server setup

**Katie's responsibilities**:
- [ ] Implement find_paths_to tool handler
  - [ ] Parse parameters
  - [ ] Call PathFinder.FindPathsTo
  - [ ] Format response JSON
- [ ] Implement find_paths_from tool handler
  - [ ] Parse parameters
  - [ ] Call PathFinder.FindPathsFrom
  - [ ] Format response JSON
- [ ] Implement search_code tool handler
  - [ ] Parse parameters
  - [ ] Call vectorStore.SearchTextAsync
  - [ ] Format results with metadata
- [ ] Implement explain_method tool handler
  - [ ] Use CallIndex for relationships
  - [ ] Query vector store for description
  - [ ] Combine information
- [ ] Implement generate_sequence_diagram tool handler
  - [ ] Use PathFinder to get paths
  - [ ] Call MermaidGenerator
  - [ ] Return diagram syntax
- [ ] Implement generate_class_diagram tool handler
  - [ ] Extract scope information
  - [ ] Call MermaidGenerator
  - [ ] Return diagram syntax
- [ ] Implement index_project tool handler
  - [ ] Trigger Phase 1 analyzer
  - [ ] Report progress
  - [ ] Return statistics
- [ ] Add parameter validation for all tools
- [ ] Add error responses for invalid input
- [ ] Test each tool independently
- [ ] Integration test with Claude Desktop

### AI Prompting Guidance

**For Katie's tool implementations**:

**Provide**:
- Tool specifications above
- Interfaces from previous phases (PathFinder, MermaidGenerator, etc.)
- Example of expected JSON response format
- Dad's scaffolding (tool registration, dispatcher)

**Ask for**:
- Implementation of tool handler methods
- JSON serialization/deserialization
- Parameter validation logic
- Error handling strategies

**Example prompt**:
```
I need to implement the find_paths_to tool handler for the MCP server.

Tool specification:
[paste tool spec above]

Available interfaces:
- PathFinder.FindPathsTo(target, maxDepth) → List<List<string>>

The handler should:
1. Parse parameters from JSON
2. Validate target parameter exists
3. Call PathFinder with appropriate parameters
4. Format response as JSON with schema: { target, paths, count }
5. Handle errors (method not found, etc.)

Here's the scaffolding Dad provided: [paste dispatcher signature]

Please implement the find_paths_to tool handler.
```

### Verification

**Unit testing**:
- Mock MCP requests
- Verify each tool handler produces correct output
- Test parameter validation
- Test error cases

**Integration testing**:
- Configure Claude Desktop to use MCP server
- Verify tools appear in Claude's tool list
- Test each tool through Claude Desktop
- Verify responses are correct and well-formatted

**End-to-end testing**:
- Ask Claude to "find all paths to SaveUser method"
- Verify Claude calls find_paths_to tool
- Verify response is accurate
- Verify Claude presents results clearly

---

## Project Structure

```
CSharpCodeNavigator/
├── src/
│   ├── CodeAnalyzer.Roslyn/              # Phase 1
│   │   ├── CodeAnalyzer.Roslyn.csproj
│   │   ├── RoslynAnalyzer.cs
│   │   ├── MethodCallExtractor.cs
│   │   ├── VectorStoreWriter.cs
│   │   └── Models/
│   │       ├── MethodCallInfo.cs
│   │       └── AnalysisResult.cs
│   │
│   ├── CodeAnalyzer.Navigation/          # Phases 2-3
│   │   ├── CodeAnalyzer.Navigation.csproj
│   │   ├── CallIndex.cs
│   │   ├── PathFinder.cs
│   │   └── Models/
│   │       └── MethodMetadata.cs
│   │
│   ├── CodeAnalyzer.Diagrams/            # Phase 5 (planned)
│   │   ├── CodeAnalyzer.Diagrams.csproj
│   │   └── MermaidGenerator.cs
│   │
│   ├── CodeAnalyzer.Console/             # Phase 4
│   │   ├── CodeAnalyzer.Console.csproj
│   │   ├── Program.cs
│   │   └── CommandHandler.cs
│   │
│   ├── CodeAnalyzer.McpServer/           # Phase 6 (planned)
│   │   ├── CodeAnalyzer.McpServer.csproj
│   │   ├── Program.cs
│   │   ├── McpProtocol.cs
│   │   ├── ToolRegistry.cs
│   │   └── ToolHandlers/
│   │       ├── FindPathsToHandler.cs
│   │       ├── FindPathsFromHandler.cs
│   │       ├── SearchCodeHandler.cs
│   │       ├── ExplainMethodHandler.cs
│   │       ├── DiagramHandlers.cs
│   │       └── IndexProjectHandler.cs
│   │
│   └── CodeAnalyzer.IndexingUI/          # UI for triggering analysis (planned)
│       ├── CodeAnalyzer.IndexingUI.csproj
│       ├── Program.cs
│       └── MainWindow.xaml (or Console UI)
│
├── tests/
│   ├── CodeAnalyzer.Roslyn.Tests/
│   ├── CodeAnalyzer.Navigation.Tests/
│   ├── CodeAnalyzer.Diagrams.Tests/
│   └── CodeAnalyzer.McpServer.Tests/
│
├── docs/
│   ├── architecture.md (this document)
│   ├── mcp-setup.md
│   └── troubleshooting.md
│
└── examples/
    ├── sample-csharp-project/
    └── claude-desktop-config.json
```

Note: Projects marked as “planned” are not yet present in this repository.

---

## Appendix A: Vector Store API Reference

Quick reference for CSharpSimpleVector operations used throughout the project:

### Core Operations

```csharp
// Create or open vector store
var store = await FileVectorStore.CreateOrOpenAsync("./path");

// Add text with metadata
var docId = await store.AddTextAsync(
    content: "Description text",
    metadata: new Dictionary<string, object> { ["key"] = "value" }
);

// Semantic search
var results = await store.SearchTextAsync(query: "search terms", limit: 10);
// Returns: SearchResult[] with Document and Similarity properties

// Get all document IDs
var allIds = await store.GetAllIdsAsync();
// Returns: string[]

// Get specific document
var doc = await store.GetAsync(docId);
// Returns: VectorDocument? with Content, Metadata, Embedding

// Delete document
var deleted = await store.DeleteAsync(docId);
// Returns: bool
```

### Result Types

```csharp
public class SearchResult
{
    public VectorDocument Document { get; set; }
    public float Similarity { get; set; }
}

public class VectorDocument
{
    public string Content { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
    public float[] Embedding { get; set; }
}
```

---

## Appendix B: Roslyn Quick Reference

### Key Concepts

**SyntaxTree**: Parsed representation of source code
**SyntaxNode**: Element in syntax tree (class, method, statement)
**SemanticModel**: Type information and symbol resolution
**ISymbol**: Represents a declared entity (method, class, namespace)
**Compilation**: Collection of syntax trees with references

### Common Patterns

```csharp
// Parse single file
var tree = CSharpSyntaxTree.ParseText(sourceCode);
var root = await tree.GetRootAsync();

// Find nodes of specific type
var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

// Get semantic model
var compilation = /* create compilation */;
var semanticModel = compilation.GetSemanticModel(tree);

// Resolve symbol from syntax
var symbol = semanticModel.GetSymbolInfo(syntaxNode).Symbol;
var declaredSymbol = semanticModel.GetDeclaredSymbol(syntaxNode);

// Navigate symbol hierarchy
symbol.ContainingType      // Parent class
symbol.ContainingNamespace // Parent namespace
symbol.Name                // Simple name

// Fully qualified name
var fqn = symbol.ContainingNamespace.ToDisplayString() + "." +
          symbol.ContainingType.Name + "." +
          symbol.Name;
```

### Useful Syntax Types

- `ClassDeclarationSyntax` - class definitions
- `MethodDeclarationSyntax` - method definitions
- `InvocationExpressionSyntax` - method calls
- `IdentifierNameSyntax` - names/identifiers
- `ParameterListSyntax` - method parameters

### Useful Symbol Types

- `IMethodSymbol` - method information
- `INamedTypeSymbol` - class/struct/interface information
- `INamespaceSymbol` - namespace information
- `IParameterSymbol` - parameter information

---

## Appendix C: Testing Strategy

### Test Organization

**Unit Tests**: Test individual classes in isolation
- Mock dependencies
- Fast execution (< 1 second total)
- High coverage of logic paths

**Integration Tests**: Test component interactions
- Use real dependencies where practical
- Test data flow between phases
- Verify contracts between components

**End-to-End Tests**: Test complete workflows
- Index real project
- Query through console or MCP
- Verify accuracy of results

### TDD Workflow

1. **Write failing test** defining desired behavior
2. **Run test** to confirm it fails
3. **Implement** minimum code to pass
4. **Run test** to confirm it passes
5. **Refactor** if needed
6. **Commit** working code

### Test Naming Convention

```
MethodName_Scenario_ExpectedBehavior

Examples:
- FindPathsTo_LinearPath_ReturnsCompletePath
- BuildIndex_EmptyVectorStore_ReturnsZeroMethods
- GenerateSequenceDiagram_SimplePath_ProducesValidMermaid
```

### Coverage Goals

- Phase 1 (Roslyn): Focus on integration tests (real code samples)
- Phase 2-3: High unit test coverage (pure logic, no I/O)
- Phase 4: Manual testing (UI interaction)
- Phase 5: Output validation (Mermaid syntax correctness)
- Phase 6: Integration tests (MCP protocol compliance)

---

## Appendix D: Troubleshooting Guide

### Phase 1: Roslyn Issues

**Problem**: Compilation errors when creating semantic model
- Check: All required references added?
- Check: Code actually compiles in Visual Studio?
- Solution: Add missing references, fix compilation errors first

**Problem**: GetSymbolInfo returns null
- Check: Is semantic model from correct syntax tree?
- Check: Are you querying the right node type?
- Solution: Verify node type, ensure semantic model matches tree

**Problem**: Can't resolve method calls across files
- Check: Are all files added to compilation?
- Check: Do files reference each other correctly?
- Solution: Ensure all syntax trees in single compilation

### Phase 2: Call Index Issues

**Problem**: Index is empty after building
- Check: Are there method_call chunks in vector store?
- Check: Is metadata["type"] filter correct?
- Solution: Verify Phase 1 output, check metadata keys

**Problem**: GetCallers/GetCallees returns empty unexpectedly
- Check: Are fully qualified names consistent?
- Check: Case sensitivity?
- Solution: Verify name formatting matches between phases

### Phase 3: Path Finding Issues

**Problem**: FindPathsTo returns no paths for known method
- Check: Does method exist in call index?
- Check: Are there actually any callers?
- Solution: Use console app to inspect what's in index

**Problem**: Infinite loop or very slow
- Check: Is cycle detection working?
- Check: Is maxDepth being respected?
- Solution: Add logging, verify visited tracking

### Phase 4: Console App Issues

**Problem**: Commands not recognized
- Check: Command parser logic
- Check: Case sensitivity
- Solution: Add logging for parsed commands

### Phase 5: Diagram Issues

**Problem**: Mermaid won't render
- Check: Syntax errors (paste into mermaid.live)
- Check: Special characters in names?
- Solution: Escape or filter problematic characters

### Phase 6: MCP Integration Issues

**Problem**: Claude doesn't see tools
- Check: MCP server registered in Claude Desktop config
- Check: Server process running?
- Check: Tool registration JSON format
- Solution: Check Claude Desktop logs

**Problem**: Tool calls fail
- Check: Parameter names match specification
- Check: Response JSON structure
- Solution: Log requests/responses for debugging

---

## Summary and Next Steps

This architecture provides a complete path from C# code analysis through AI-powered navigation in Claude Desktop. The phased approach ensures:

- Each phase builds on previous work
- Testing validates correctness at each step
- Katie gains increasing confidence and capability
- Dad can provide support where needed (Roslyn, MCP)
- The system works end-to-end before final integration

### Getting Started

1. **Review this document together** - Ensure Katie understands overall vision
2. **Decide on Phase 1 ownership** - Katie tackles challenge or Dad provides foundation
3. **Set up development environment** - .NET 8, IDE, test runners
4. **Create repository structure** - Initialize git, create projects
5. **Begin Phase 1** - First tests, first implementation
6. **Iterate with confidence** - Each green test is progress

### Success Metrics

- Working MCP server integrated with Claude Desktop
- Can navigate call graphs in real C# projects
- Can search code semantically
- Can generate useful diagrams
- Katie has gained significant development experience
- System serves as portfolio-quality demonstration

---

**Document Version**: 1.0  
**Last Updated**: 2025  
**Authors**: Architecture by Dad & Claude, Implementation by Katie & AI Assistants