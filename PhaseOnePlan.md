# Phase 1: Roslyn Analysis & Indexing Pipeline - Implementation Plan

## Overview
**Goal**: Extract method call relationships from C# projects and populate the vector database with structured, searchable data.

**Owner**: Katie (with AI assistance)  
**Prerequisites**: CSharpSimpleVector library available, .NET 9 SDK installed  
**Deliverables**: `CodeAnalyzer.Roslyn` assembly, working analyzer, populated vector store, console test harness

---

## Phase 1 Breakdown: Small, Incremental Steps

**Progress**: 6/8 steps completed (75%) - Steps 1.1–1.6 ✅ COMPLETED

### **Step 1.1: Project Setup & Basic Models** ⏱️ *30 minutes* ✅ **COMPLETED**
**Goal**: Create foundation data structures and verify project setup

- [x] Create `MethodCallInfo` class with properties:
  - [x] `Caller` (string) - fully qualified method name making the call
  - [x] `Callee` (string) - fully qualified method name being called
  - [x] `CallerClass` (string) - class containing the caller
  - [x] `CalleeClass` (string) - class containing the callee
  - [x] `CallerNamespace` (string) - namespace of caller
  - [x] `CalleeNamespace` (string) - namespace of callee
  - [x] `FilePath` (string) - source file path
  - [x] `LineNumber` (int) - line number of the call

- [x] Create `AnalysisResult` class with properties:
  - [x] `MethodCalls` (List<MethodCallInfo>) - all discovered method calls
  - [x] `MethodsAnalyzed` (int) - count of methods processed
  - [x] `FilesProcessed` (int) - count of files analyzed
  - [x] `Errors` (List<string>) - any analysis errors

- [x] Create `RoslynAnalyzer` class skeleton:
  - [x] Empty constructor
  - [x] Placeholder `AnalyzeProjectAsync(string projectPath)` method
  - [x] Placeholder `AnalyzeFileAsync(string filePath)` method

- [x] Write unit tests:
  - [x] Test `MethodCallInfo` property assignment
  - [x] Test `AnalysisResult` initialization
  - [x] Test `RoslynAnalyzer` instantiation

- [x] Verify project builds and tests pass

**✅ Results**: 20 tests passing, all models working correctly, ready for Step 1.2

**📁 Files Created**:
- `src/CodeAnalyzer.Roslyn/Models/MethodCallInfo.cs` - Method call relationship data model
- `src/CodeAnalyzer.Roslyn/Models/AnalysisResult.cs` - Analysis results container
- `src/CodeAnalyzer.Roslyn/RoslynAnalyzer.cs` - Main analyzer class skeleton
- `tests/CodeAnalyzer.Roslyn.Tests/Models/MethodCallInfoTests.cs` - 4 unit tests
- `tests/CodeAnalyzer.Roslyn.Tests/Models/AnalysisResultTests.cs` - 15 unit tests  
- `tests/CodeAnalyzer.Roslyn.Tests/RoslynAnalyzerTests.cs` - 1 unit test

**🔧 Technical Details**:
- All classes follow the architecture document specifications
- Comprehensive unit test coverage (20 tests total)
- Proper error handling and null safety
- Clean code with XML documentation
- Ready for Roslyn integration in Step 1.2

---

### **Step 1.2: Basic Roslyn Compilation Setup** ⏱️ *45 minutes* ✅ **COMPLETED**
**Goal**: Set up Roslyn compilation context and verify we can parse C# code

- [x] Add Roslyn NuGet packages to project (if not already present):
  - [x] `Microsoft.CodeAnalysis.CSharp`
  - [x] `Microsoft.CodeAnalysis.CSharp.Workspaces`
  - [x] `Microsoft.Build.Locator`
  - [x] `Microsoft.CodeAnalysis.Workspaces.MSBuild`

- [x] Implement basic compilation setup in `RoslynAnalyzer`:
  - [x] `CreateCompilationAsync(string projectPath)` method
  - [x] Load project using `MSBuildWorkspace`
  - [x] Get all syntax trees from project
  - [x] Create `CSharpCompilation` with references

- [x] Add reference resolution:
  - [x] Add `mscorlib` reference
  - [x] Add `System.Console` reference
  - [x] Add `System.Linq` reference
  - [x] Add project-specific references

- [x] Create test C# file for testing:
  - [x] Simple class with one method
  - [x] Place in `tests/TestData/` folder

- [x] Write tests:
  - [x] Test compilation creation with test file
  - [x] Test syntax tree extraction
  - [x] Test semantic model creation
  - [x] Verify no compilation errors

- [x] Verify we can get semantic model for test file

**Note on build warnings**: During Step 1.2, methods `AnalyzeProjectAsync` and `AnalyzeFileAsync` remain placeholders. The compiler emits CS1998 warnings because they are marked `async` without `await` yet. These will resolve naturally in Steps 1.3–1.5 when asynchronous analysis is implemented. Temporary alternatives include removing `async` and returning `Task.FromException` or inserting `await Task.CompletedTask;` before the `NotImplementedException`.

---

### **Step 1.3: Method Declaration Extraction** ⏱️ *30 minutes* ✅ **COMPLETED**
**Goal**: Extract method declarations and generate fully qualified names

- [x] Implement method extraction in `RoslynAnalyzer`:
  - [x] `ExtractMethodDeclarations(SyntaxTree tree, SemanticModel model)` method
  - [x] Find all `MethodDeclarationSyntax` nodes
  - [x] Get `IMethodSymbol` from semantic model
  - [x] Generate fully qualified names

- [x] Create `GetFullyQualifiedName(ISymbol symbol)` helper method:
  - [x] Format: `Namespace.ClassName.MethodName`
  - [x] Handle global namespace
  - [x] Handle nested types

- [x] Create test C# file with multiple methods:
  - [x] Class with 3-4 methods
  - [x] Methods in different namespaces
  - [x] Static and instance methods

- [x] Write tests:
  - [x] Test method extraction from single file
  - [x] Test fully qualified name generation
  - [x] Test namespace handling
  - [x] Verify all expected methods are found

- [x] Verify method extraction works correctly

**Notes**: Added multi-namespace test data (`MultiNamespace.cs`) and tests confirming extraction and FQN formatting for static and instance methods across namespaces.

---

### **Step 1.4: Method Call Detection (Basic)** ⏱️ *45 minutes* ✅ **COMPLETED**
**Goal**: Find method invocations and extract caller/callee relationships

- [x] Implement basic method call detection:
  - [x] `ExtractMethodCalls(SyntaxTree tree, SemanticModel model)` method
  - [x] Find all `InvocationExpressionSyntax` nodes
  - [x] Get containing method (caller)
  - [x] Get called method symbol (callee)

- [x] Handle basic cases:
  - [x] Direct method calls (`obj.Method()`)
  - [x] Static method calls (`Class.Method()`)
  - [x] Method calls within same class

- [x] Create test C# file with method calls:
  - [x] Class with methods that call each other
  - [x] Mix of instance and static calls
  - [x] Calls to external methods (Console.WriteLine)

- [x] Write tests:
  - [x] Test method call extraction
  - [x] Test caller/callee identification
  - [x] Test line number extraction
  - [x] Verify call relationships are correct

- [x] Verify basic method call detection works

**Notes**: Implemented `GetContainingMethodSymbol` for robust caller resolution; `AnalyzeFileAsync` now returns `AnalysisResult` with methods analyzed and call list. Unresolved symbols are skipped here and will be addressed in Step 1.5.

---

### **Step 1.5: Semantic Analysis Integration** ⏱️ *60 minutes* ✅ **COMPLETED**
**Goal**: Use semantic model to resolve method symbols across files

- [x] Enhance method call detection with semantic analysis:
  - [x] Use `semanticModel.GetSymbolInfo(invocation)` to resolve symbols
  - [x] Handle cross-file method calls
  - [x] Handle method overloads
  - [x] Handle generic methods (normalized via `OriginalDefinition` where applicable)

- [x] Implement symbol resolution:
  - [x] Check for null symbols (unresolved calls)
  - [x] Handle extension methods (normalize via `ReducedFrom`)
  - [x] Handle method calls through interfaces (interface dispatch captured)

- [x] Create multi-file test scenario:
  - [x] Two classes in different files
  - [x] Methods calling across files
  - [x] Interface implementations
  - [ ] Inheritance scenarios (deferred to 1.7 edge cases if needed)

- [x] Write tests:
  - [x] Test cross-file method resolution
  - [x] Test interface method calls
  - [ ] Test inherited method calls (deferred)
  - [x] Test unresolved symbol handling (skipped entries, no crash)

- [x] Verify semantic analysis works across files

**Notes**: Implemented `AnalyzeProjectAsync` to iterate all syntax trees with a single compilation for semantic consistency. Enhanced call extraction to select candidates when `Symbol` is null, normalize extension methods via `ReducedFrom`, and record interface-dispatch targets at the interface method symbol level for stability.

---

### **Step 1.6: Vector Store Integration** ⏱️ *45 minutes* ✅ **COMPLETED**
**Goal**: Store method call relationships in vector database

- [x] Add `IVectorStoreWriter` abstraction:
  - [x] `AddTextAsync(content, metadata)`
  - [x] Library remains decoupled from external VectorStore package

- [ ] Implement metadata schema compliance:
  - [ ] `type: "method_call"`
  - [ ] All required metadata fields
  - [ ] Proper content formatting

- [x] Integrate with `RoslynAnalyzer`:
  - [x] Optional writer via constructor DI
  - [x] Store each discovered method call
  - [x] Handle storage errors gracefully (errors appended to `AnalysisResult.Errors`)

- [x] Tests (using `FakeVectorStoreWriter`):
  - [x] Verify `AnalyzeFileAsync` persists `method_call` documents
  - [x] Validate metadata schema keys and types

- [x] Write tests:
  - [x] Test method call storage
  - [x] Test metadata correctness
  - [ ] Retrieval via real store (optional, can add later)
  - [x] Error handling captured on write failures

- [x] Verify method calls are stored correctly

**Notes**: Implemented storage via an interface to avoid a hard dependency on the VectorStore package in the main library. Tests use a fake writer to validate content and metadata without requiring model downloads. A thin adapter can be created in integration layers when wiring a real `FileVectorStore`.

---

### **Step 1.7: Error Handling & Edge Cases** ⏱️ *30 minutes*
**Goal**: Handle compilation errors and edge cases gracefully

- [ ] Implement error handling:
  - [ ] Check `compilation.GetDiagnostics()` for errors
  - [ ] Log compilation errors
  - [ ] Continue analysis despite errors where possible

- [ ] Handle edge cases:
  - [ ] Unresolved symbols (external dependencies)
  - [ ] Incomplete code (syntax errors)
  - [ ] Missing references
  - [ ] Generic type parameters

- [ ] Create problematic test files:
  - [ ] File with syntax errors
  - [ ] File with missing references
  - [ ] File with incomplete methods

- [ ] Write tests:
  - [ ] Test error handling
  - [ ] Test graceful degradation
  - [ ] Test error reporting
  - [ ] Test partial analysis success

- [ ] Verify robust error handling

---

### **Step 1.8: Console Test Harness** ⏱️ *30 minutes*
**Goal**: Create simple console app to test the analyzer end-to-end

- [ ] Update `Program.cs` in console project:
  - [ ] Add project reference to `CodeAnalyzer.Roslyn`
  - [ ] Create simple test harness
  - [ ] Load test project and run analysis
  - [ ] Display results

- [ ] Implement basic console interface:
  - [ ] Load project path from command line or prompt
  - [ ] Run analysis
  - [ ] Display method call count
  - [ ] Display sample method calls
  - [ ] Show any errors

- [ ] Create test project for analysis:
  - [ ] Small C# project with multiple files
  - [ ] Various method call patterns
  - [ ] Known expected results

- [ ] Test end-to-end:
  - [ ] Run console app
  - [ ] Analyze test project
  - [ ] Verify results match expectations
  - [ ] Check vector store contents

- [ ] Verify complete end-to-end functionality

---

## Success Criteria for Phase 1

- [ ] **Analyzer can load a .csproj file** and resolve all references
- [ ] **Semantic model successfully resolves** method calls across files
- [ ] **Vector store contains method_call chunks** with correct metadata
- [ ] **Fully qualified names are accurate** and unambiguous
- [ ] **File paths and line numbers are correct**
- [ ] **Can analyze a project with 50+ files** without errors
- [ ] **Generated data matches the documented schema**

## Testing Strategy

- [ ] **Unit Tests**: Test each component in isolation
- [ ] **Integration Tests**: Test component interactions
- [ ] **End-to-End Tests**: Test complete analysis workflow
- [ ] **Error Tests**: Test error handling and edge cases
- [ ] **Performance Tests**: Verify reasonable analysis time

## Estimated Total Time: ~5 hours

**Next Phase**: Phase 2 - Call Index Builder (builds on Phase 1 results)

---

*This plan follows the TDD approach outlined in the architecture document. Each step builds incrementally on the previous steps, ensuring we always have working code.*
