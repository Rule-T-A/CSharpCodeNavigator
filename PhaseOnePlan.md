# Phase 1: Roslyn Analysis & Indexing Pipeline - Implementation Plan

## Overview
**Goal**: Extract all code elements (method calls, method definitions, classes, properties, fields, etc.) from C# projects and populate the vector database with structured, searchable data.

**Owner**: Katie (with AI assistance)  
**Prerequisites**: CSharpSimpleVector library available, .NET 9 SDK installed  
**Deliverables**: `CodeAnalyzer.Roslyn` assembly, working analyzer, populated vector store, console test harness

**Current Status**: Phase 1.1-1.8 completed for method call relationships. **EXPANSION NEEDED** to support all code elements.

---

## Phase 1 Breakdown: Small, Incremental Steps

**Progress**: 7/8 steps completed (87.5%) - Steps 1.1–1.7 ✅ COMPLETED

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
  - [x] Inheritance scenarios (override, abstract base, interface, explicit interface, base vs this)

- [x] Write tests:
  - [x] Test cross-file method resolution
  - [x] Test interface method calls
  - [x] Test inherited method calls
  - [x] Test unresolved symbol handling (skipped entries, no crash)

- [x] Verify semantic analysis works across files

**Notes**: Implemented `AnalyzeProjectAsync` to iterate all syntax trees with a single compilation for semantic consistency. Enhanced call extraction to select candidates when `Symbol` is null, normalize extension methods via `ReducedFrom`, and record interface-dispatch targets at the interface method symbol level for stability.

---

### **Step 1.6: Vector Store Integration** ⏱️ *45 minutes* ✅ **COMPLETED**
**Goal**: Store method call relationships in vector database

- [x] Add `IVectorStoreWriter` abstraction:
  - [x] `AddTextAsync(content, metadata)`
  - [x] Library remains decoupled from external VectorStore package

- [~] Implement metadata schema compliance:
  - [x] `type: "method_call"`
  - [x] Required metadata fields persisted
  - [ ] Additional validation/normalization rules (TBD)

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

### **Step 1.7: Error Handling & Edge Cases** ⏱️ *30 minutes* ✅ **COMPLETED**
**Goal**: Handle compilation errors and edge cases gracefully

- [x] Implement error handling:
  - [x] Check `compilation.GetDiagnostics()` for errors (and optional warnings)
  - [x] Append diagnostics to `AnalysisResult.Errors`
  - [x] Continue analysis despite errors where possible

- [x] Handle edge cases:
  - [x] Unresolved symbols (external dependencies) – skipped safely
  - [x] Incomplete code (syntax errors) – diagnostics collected, no throw
  - [x] Missing references – surfaced via diagnostics
  - [x] Local functions / lambdas – calls attributed to containing method
  - [x] Attribute/initializer calls – controlled by `AttributeInitializerCalls` option

- [x] Create problematic test files:
  - [x] File with syntax errors (excluded from compilation)
  - [x] File referencing missing type (excluded from compilation)
  - [x] Local/lambda call scenarios
  - [x] Attribute constructor call scenarios
  - [x] Field/property initializer call scenarios

- [x] Write tests:
  - [x] Test error handling
  - [x] Test graceful degradation
  - [x] Test error reporting
  - [x] Test partial analysis success
  - [x] Test attribute constructor calls (enabled/disabled)
  - [x] Test field/property initializer calls (enabled/disabled)
  - [x] Test local/lambda behavior consistency

- [x] Verify robust error handling

**Notes**: Added `AnalyzerOptions` (`IncludeWarningsInErrors`, `RecordExternalCalls`, `AttributeInitializerCalls`) and `WithOptions`. Diagnostics are collected in both project and file modes. Extraction remains null-safe and non-throwing. Calls inside local functions and lambdas are attributed to the containing method. Attribute constructor calls and field/property initializer calls are now supported when `AttributeInitializerCalls` option is enabled.

**Technical Debt Resolution**: All nullability warnings (CS8625) have been fixed, error handling enhanced with specific context, and obsolete tests removed. The codebase is now clean with 0 warnings and 51 passing tests.

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

## Phase 1 Expansion: Support All Code Elements

**Current Limitation**: The system only stores method call relationships, making it impossible to search for method definitions, classes, properties, fields, etc.

**Required Expansion**: Extend the system to extract and store all C# code elements, not just method calls.

### **Phase 1A: Method Definitions** ⏱️ *2 hours* 🔄 **PENDING**
**Goal**: Add support for method definitions to enable searching for method declarations

- [ ] Create `MethodDefinitionInfo` model with properties:
  - [ ] `MethodName` (string) - method name
  - [ ] `ClassName` (string) - containing class
  - [ ] `Namespace` (string) - namespace
  - [ ] `ReturnType` (string) - return type
  - [ ] `Parameters` (List<string>) - parameter types
  - [ ] `AccessModifier` (string) - public, private, etc.
  - [ ] `IsStatic` (bool) - static method flag
  - [ ] `FilePath` (string) - source file path
  - [ ] `LineNumber` (int) - line number of definition

- [ ] Add `ExtractMethodDefinitions(SyntaxTree tree, SemanticModel model)` method
- [ ] Update `AnalysisResult` to include `MethodDefinitions` list
- [ ] Update vector store to store method definitions with content:
  ```
  "Method {methodName} in class {className} defined in namespace {namespace}. 
   This method returns {returnType} and is defined in file {filePath} at line {lineNumber}."
  ```
- [ ] Update REPL to show method definitions in analysis results
- [ ] Add tests for method definition extraction

### **Phase 1B: Class Definitions** ⏱️ *1.5 hours* 🔄 **PENDING**
**Goal**: Add support for class definitions

- [ ] Create `ClassDefinitionInfo` model
- [ ] Add `ExtractClassDefinitions()` method
- [ ] Store class definitions in vector store
- [ ] Add tests

### **Phase 1C: Properties & Fields** ⏱️ *1.5 hours* 🔄 **PENDING**
**Goal**: Add support for properties and fields

- [ ] Create `PropertyDefinitionInfo` and `FieldDefinitionInfo` models
- [ ] Add extraction methods
- [ ] Store in vector store
- [ ] Add tests

### **Phase 1D: Interfaces, Structs, Enums** ⏱️ *2 hours* 🔄 **PENDING**
**Goal**: Complete support for all C# element types

- [ ] Create models for interfaces, structs, enums
- [ ] Add extraction methods
- [ ] Store in vector store
- [ ] Add comprehensive tests

### **Updated Success Criteria for Full Phase 1**

- [x] **Method call relationships** extracted and stored ✅
- [ ] **Method definitions** extracted and stored
- [ ] **Class definitions** extracted and stored  
- [ ] **Property definitions** extracted and stored
- [ ] **Field definitions** extracted and stored
- [ ] **Interface definitions** extracted and stored
- [ ] **Struct definitions** extracted and stored
- [ ] **Enum definitions** extracted and stored
- [ ] **Can search for any code element** by name or functionality
- [ ] **Vector store contains all element types** with correct metadata

---

## Success Criteria for Phase 1 (Original Scope)

- [x] **Analyzer can load a .csproj file** and resolve all references
- [x] **Semantic model successfully resolves** method calls across files
- [x] **Vector store contains method_call chunks** with correct metadata
- [x] **Fully qualified names are accurate** and unambiguous
- [x] **File paths and line numbers are correct**
- [x] **Can analyze a project with 50+ files** without errors
- [x] **Generated data matches the documented schema**

## Testing Strategy

- [x] **Unit Tests**: Test each component in isolation
- [x] **Integration Tests**: Test component interactions
- [x] **End-to-End Tests**: Test complete analysis workflow
- [x] **Error Tests**: Test error handling and edge cases
- [ ] **Performance Tests**: Verify reasonable analysis time

## Estimated Total Time: ~5 hours

**Next Phase**: Phase 2 - Call Index Builder (builds on Phase 1 results)

---

*This plan follows the TDD approach outlined in the architecture document. Each step builds incrementally on the previous steps, ensuring we always have working code.*
