# Phase 1: Roslyn Analysis & Indexing Pipeline - Implementation Plan

## Overview
**Goal**: Extract method call relationships from C# projects and populate the vector database with structured, searchable data.

**Owner**: Katie (with AI assistance)  
**Prerequisites**: CSharpSimpleVector library available, .NET 9 SDK installed  
**Deliverables**: `CodeAnalyzer.Roslyn` assembly, working analyzer, populated vector store, console test harness

---

## Phase 1 Breakdown: Small, Incremental Steps

**Progress**: 1/8 steps completed (12.5%) - Step 1.1 ✅ COMPLETED

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

### **Step 1.2: Basic Roslyn Compilation Setup** ⏱️ *45 minutes*
**Goal**: Set up Roslyn compilation context and verify we can parse C# code

- [ ] Add Roslyn NuGet packages to project (if not already present):
  - [ ] `Microsoft.CodeAnalysis.CSharp`
  - [ ] `Microsoft.CodeAnalysis.CSharp.Workspaces`
  - [ ] `Microsoft.Build.Locator`

- [ ] Implement basic compilation setup in `RoslynAnalyzer`:
  - [ ] `CreateCompilationAsync(string projectPath)` method
  - [ ] Load project using `MSBuildWorkspace`
  - [ ] Get all syntax trees from project
  - [ ] Create `CSharpCompilation` with references

- [ ] Add reference resolution:
  - [ ] Add `mscorlib` reference
  - [ ] Add `System.Console` reference
  - [ ] Add `System.Linq` reference
  - [ ] Add project-specific references

- [ ] Create test C# file for testing:
  - [ ] Simple class with one method
  - [ ] Place in `tests/TestData/` folder

- [ ] Write tests:
  - [ ] Test compilation creation with test file
  - [ ] Test syntax tree extraction
  - [ ] Test semantic model creation
  - [ ] Verify no compilation errors

- [ ] Verify we can get semantic model for test file

---

### **Step 1.3: Method Declaration Extraction** ⏱️ *30 minutes*
**Goal**: Extract method declarations and generate fully qualified names

- [ ] Implement method extraction in `RoslynAnalyzer`:
  - [ ] `ExtractMethodDeclarations(SyntaxTree tree, SemanticModel model)` method
  - [ ] Find all `MethodDeclarationSyntax` nodes
  - [ ] Get `IMethodSymbol` from semantic model
  - [ ] Generate fully qualified names

- [ ] Create `GetFullyQualifiedName(ISymbol symbol)` helper method:
  - [ ] Format: `Namespace.ClassName.MethodName`
  - [ ] Handle global namespace
  - [ ] Handle nested types

- [ ] Create test C# file with multiple methods:
  - [ ] Class with 3-4 methods
  - [ ] Methods in different namespaces
  - [ ] Static and instance methods

- [ ] Write tests:
  - [ ] Test method extraction from single file
  - [ ] Test fully qualified name generation
  - [ ] Test namespace handling
  - [ ] Verify all expected methods are found

- [ ] Verify method extraction works correctly

---

### **Step 1.4: Method Call Detection (Basic)** ⏱️ *45 minutes*
**Goal**: Find method invocations and extract caller/callee relationships

- [ ] Implement basic method call detection:
  - [ ] `ExtractMethodCalls(SyntaxTree tree, SemanticModel model)` method
  - [ ] Find all `InvocationExpressionSyntax` nodes
  - [ ] Get containing method (caller)
  - [ ] Get called method symbol (callee)

- [ ] Handle basic cases:
  - [ ] Direct method calls (`obj.Method()`)
  - [ ] Static method calls (`Class.Method()`)
  - [ ] Method calls within same class

- [ ] Create test C# file with method calls:
  - [ ] Class with methods that call each other
  - [ ] Mix of instance and static calls
  - [ ] Calls to external methods (Console.WriteLine)

- [ ] Write tests:
  - [ ] Test method call extraction
  - [ ] Test caller/callee identification
  - [ ] Test line number extraction
  - [ ] Verify call relationships are correct

- [ ] Verify basic method call detection works

---

### **Step 1.5: Semantic Analysis Integration** ⏱️ *60 minutes*
**Goal**: Use semantic model to resolve method symbols across files

- [ ] Enhance method call detection with semantic analysis:
  - [ ] Use `semanticModel.GetSymbolInfo(invocation)` to resolve symbols
  - [ ] Handle cross-file method calls
  - [ ] Handle method overloads
  - [ ] Handle generic methods

- [ ] Implement symbol resolution:
  - [ ] Check for null symbols (unresolved calls)
  - [ ] Handle extension methods
  - [ ] Handle method calls through interfaces

- [ ] Create multi-file test scenario:
  - [ ] Two classes in different files
  - [ ] Methods calling across files
  - [ ] Interface implementations
  - [ ] Inheritance scenarios

- [ ] Write tests:
  - [ ] Test cross-file method resolution
  - [ ] Test interface method calls
  - [ ] Test inherited method calls
  - [ ] Test unresolved symbol handling

- [ ] Verify semantic analysis works across files

---

### **Step 1.6: Vector Store Integration** ⏱️ *45 minutes*
**Goal**: Store method call relationships in vector database

- [ ] Create `VectorStoreWriter` class:
  - [ ] Constructor taking `FileVectorStore` parameter
  - [ ] `StoreMethodCallAsync(MethodCallInfo callInfo)` method
  - [ ] Format content and metadata according to schema

- [ ] Implement metadata schema compliance:
  - [ ] `type: "method_call"`
  - [ ] All required metadata fields
  - [ ] Proper content formatting

- [ ] Integrate with `RoslynAnalyzer`:
  - [ ] Add `VectorStoreWriter` to analyzer
  - [ ] Store each discovered method call
  - [ ] Handle storage errors gracefully

- [ ] Create test vector store:
  - [ ] Use temporary directory
  - [ ] Store test method calls
  - [ ] Verify data can be retrieved

- [ ] Write tests:
  - [ ] Test method call storage
  - [ ] Test metadata correctness
  - [ ] Test vector store retrieval
  - [ ] Test error handling

- [ ] Verify method calls are stored correctly

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
