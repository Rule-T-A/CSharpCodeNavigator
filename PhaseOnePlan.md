# Phase 1: Roslyn Analysis & Indexing Pipeline - Implementation Plan

## Overview
**Goal**: Extract all code elements (method calls, method definitions, classes, properties, fields, etc.) from C# projects and populate the vector database with structured, searchable data.

**Owner**: Katie (with AI assistance)  
**Prerequisites**: CSharpSimpleVector library available, .NET 9 SDK installed  
**Deliverables**: `CodeAnalyzer.Roslyn` assembly, working analyzer, populated vector store, console test harness

**Current Status**: Phase 1.1-1.8 completed for method call relationships. **Phase 1A-1F (All Code Elements) COMPLETED**. **Phase 1.9 (REST API) IN PROGRESS** - Steps 1.9.1-1.9.3 completed.

---

## Phase 1 Breakdown: Small, Incremental Steps

**Progress**: 8/8 steps completed (100%) - Steps 1.1–1.8 ✅ COMPLETED

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

### **Step 1.8: Console Test Harness** ⏱️ *30 minutes* ✅ **COMPLETED**
**Goal**: Create simple console app to test the analyzer end-to-end

- [x] Update `Program.cs` in console project:
  - [x] Add project reference to `CodeAnalyzer.Roslyn`
  - [x] Create simple test harness
  - [x] Load test project and run analysis
  - [x] Display results

- [x] Implement basic console interface:
  - [x] Load project path from command line or prompt
  - [x] Run analysis
  - [x] Display method call count
  - [x] Display sample method calls
  - [x] Show any errors

- [x] Create test project for analysis:
  - [x] Small C# project with multiple files
  - [x] Various method call patterns
  - [x] Known expected results

- [x] Test end-to-end:
  - [x] Run console app
  - [x] Analyze test project
  - [x] Verify results match expectations
  - [x] Check vector store contents

- [x] Verify complete end-to-end functionality

**✅ Results**: Console application successfully created with REPL interface. Supports project analysis, method call extraction, and vector store integration.

**📁 Files Updated**:
- `src/CodeAnalyzer.Console/Program.cs` - Complete REPL console application
- `tests/CodeAnalyzer.Roslyn.Tests/FileVectorStoreAdapter.cs` - Real vector store integration
- Console application includes help system, analysis commands, and error handling

**🔧 Technical Details**:
- REPL interface with commands: `help`, `analyze`, `clear`, `exit`
- Project analysis with progress reporting
- Method call and definition extraction
- Vector store integration with metadata validation
- Comprehensive error handling and user feedback

---

## Phase 1 Expansion: Support All Code Elements

**Current Limitation**: The system only stores method call relationships, making it impossible to search for method definitions, classes, properties, fields, etc.

**Required Expansion**: Extend the system to extract and store all C# code elements, not just method calls.

### **Phase 1A: Method Definitions** ⏱️ *2 hours* ✅ **COMPLETED**
**Goal**: Add support for method definitions to enable searching for method declarations

- [x] Create `MethodDefinitionInfo` model with properties:
  - [x] `MethodName` (string) - method name
  - [x] `ClassName` (string) - containing class
  - [x] `Namespace` (string) - namespace
  - [x] `ReturnType` (string) - return type
  - [x] `Parameters` (List<string>) - parameter types
  - [x] `AccessModifier` (string) - public, private, etc.
  - [x] `IsStatic` (bool) - static method flag
  - [x] `FilePath` (string) - source file path
  - [x] `LineNumber` (int) - line number of definition

- [x] Add `ExtractMethodDefinitions(SyntaxTree tree, SemanticModel model)` method
- [x] Update `AnalysisResult` to include `MethodDefinitions` list
- [x] Update vector store to store method definitions with content:
  ```
  "Method {methodName} in class {className} defined in namespace {namespace}. 
   This method returns {returnType} and is defined in file {filePath} at line {lineNumber}."
  ```
- [x] Update REPL to show method definitions in analysis results
- [x] Add tests for method definition extraction

**✅ Results**: Method definitions successfully extracted and stored. 101 tests passing, including comprehensive method definition extraction tests.

**📁 Files Created/Updated**:
- `src/CodeAnalyzer.Roslyn/Models/MethodDefinitionInfo.cs` - Method definition data model
- `src/CodeAnalyzer.Roslyn/RoslynAnalyzer.cs` - Added `ExtractMethodDefinitions` method
- `src/CodeAnalyzer.Roslyn/Models/AnalysisResult.cs` - Added method definitions support
- `tests/CodeAnalyzer.Roslyn.Tests/Models/MethodDefinitionInfoTests.cs` - 5 unit tests
- `tests/CodeAnalyzer.Roslyn.Tests/RoslynAnalyzerMethodDefinitionsTests.cs` - 8 extraction tests
- `tests/CodeAnalyzer.Roslyn.Tests/VectorStoreMethodDefinitionsTests.cs` - 6 integration tests

### **Phase 1B: Class Definitions** ⏱️ *2 hours* ✅ **COMPLETED**
**Goal**: Add support for class definitions to enable searching for class declarations

- [x] Create `ClassDefinitionInfo` model with properties:
  - [x] `ClassName` (string) - class name
  - [x] `Namespace` (string) - namespace
  - [x] `FullyQualifiedName` (string) - Namespace.ClassName
  - [x] `AccessModifier` (string) - public, private, etc.
  - [x] `IsStatic` (bool) - static class flag
  - [x] `IsAbstract` (bool) - abstract class flag
  - [x] `IsSealed` (bool) - sealed class flag
  - [x] `BaseClass` (string) - base class name (if any)
  - [x] `Interfaces` (List<string>) - implemented interfaces
  - [x] `FilePath` (string) - source file path
  - [x] `LineNumber` (int) - line number of definition
  - [x] `MethodCount` (int) - number of methods in class
  - [x] `PropertyCount` (int) - number of properties in class
  - [x] `FieldCount` (int) - number of fields in class

- [x] Add `ExtractClassDefinitions(SyntaxTree tree, SemanticModel model)` method
- [x] Update `AnalysisResult` to include `ClassDefinitions` list
- [x] Update vector store to store class definitions with content:
  ```
  "Class {className} defined in namespace {namespace}. 
   This is a {accessModifier} class with {methodCount} methods, {propertyCount} properties, and {fieldCount} fields.
   Defined in file {filePath} at line {lineNumber}."
  ```
- [x] Update REPL to show class definitions in analysis results
- [x] Add comprehensive tests for class definition extraction

**✅ Results**: Class definitions successfully extracted and stored. 101 tests passing, including comprehensive class definition extraction tests. Console application displays class definitions with inheritance information.

**📁 Files Created/Updated**:
- `src/CodeAnalyzer.Roslyn/Models/ClassDefinitionInfo.cs` - Class definition data model
- `src/CodeAnalyzer.Roslyn/RoslynAnalyzer.cs` - Added `ExtractClassDefinitions` method
- `src/CodeAnalyzer.Roslyn/Models/AnalysisResult.cs` - Added class definitions support
- `tests/CodeAnalyzer.Roslyn.Tests/Models/ClassDefinitionInfoTests.cs` - 5 unit tests
- `tests/CodeAnalyzer.Roslyn.Tests/RoslynAnalyzerClassDefinitionsTests.cs` - 9 extraction tests
- `tests/CodeAnalyzer.Roslyn.Tests/VectorStoreClassDefinitionsTests.cs` - 6 integration tests
- Console application updated to display class definitions with modifiers and inheritance

### **Phase 1C: Properties & Fields** ⏱️ *1.5 hours* ✅ **COMPLETED**
**Goal**: Add support for properties and fields

- [x] Create `PropertyDefinitionInfo` and `FieldDefinitionInfo` models
- [x] Add extraction methods
- [x] Store in vector store
- [x] Add tests

**✅ Results**: Properties and fields successfully extracted and stored. All tests passing (41 new tests added).

**📁 Files Created/Updated**:
- `src/CodeAnalyzer.Roslyn/Models/PropertyDefinitionInfo.cs` - Property definition data model
- `src/CodeAnalyzer.Roslyn/Models/FieldDefinitionInfo.cs` - Field definition data model
- `src/CodeAnalyzer.Roslyn/RoslynAnalyzer.cs` - Added `ExtractPropertyDefinitions` and `ExtractFieldDefinitions` methods
- `src/CodeAnalyzer.Roslyn/Models/AnalysisResult.cs` - Added property and field definitions support
- `tests/CodeAnalyzer.Roslyn.Tests/Models/PropertyDefinitionInfoTests.cs` - 5 unit tests
- `tests/CodeAnalyzer.Roslyn.Tests/Models/FieldDefinitionInfoTests.cs` - 6 unit tests
- `tests/CodeAnalyzer.Roslyn.Tests/RoslynAnalyzerPropertyDefinitionsTests.cs` - 8 extraction tests
- `tests/CodeAnalyzer.Roslyn.Tests/RoslynAnalyzerFieldDefinitionsTests.cs` - 9 extraction tests
- `tests/CodeAnalyzer.Roslyn.Tests/VectorStorePropertyDefinitionsTests.cs` - 6 integration tests
- `tests/CodeAnalyzer.Roslyn.Tests/VectorStoreFieldDefinitionsTests.cs` - 7 integration tests

### **Phase 1D: Enum Definitions** ⏱️ *1.5 hours* ✅ **COMPLETED**
**Goal**: Add support for enum definitions to enable searching for enum declarations

- [x] Create `EnumDefinitionInfo` model with properties:
  - [x] `EnumName` (string) - enum name
  - [x] `Namespace` (string) - namespace
  - [x] `FullyQualifiedName` (string) - Namespace.EnumName
  - [x] `AccessModifier` (string) - public, private, etc.
  - [x] `UnderlyingType` (string) - underlying type (int, byte, etc.)
  - [x] `Values` (List<EnumValueInfo>) - enum values
  - [x] `FilePath` (string) - source file path
  - [x] `LineNumber` (int) - line number of definition

- [x] Create `EnumValueInfo` model with properties:
  - [x] `ValueName` (string) - enum value name
  - [x] `Value` (object) - actual value
  - [x] `LineNumber` (int) - line number of value

- [x] Add `ExtractEnumDefinitions(SyntaxTree tree, SemanticModel model)` method
- [x] Update `AnalysisResult` to include `EnumDefinitions` list
- [x] Update vector store to store enum definitions with content:
  ```
  "Enum {enumName} defined in namespace {namespace} with underlying type {underlyingType}.
   Contains {valueCount} values: {valueList}.
   Defined in file {filePath} at line {lineNumber}."
  ```
- [ ] Update REPL to show enum definitions in analysis results (can be done later)
- [x] Add comprehensive tests for enum definition extraction

**✅ Results**: Enum definitions successfully extracted and stored. All tests passing (26 new tests added).

**📁 Files Created/Updated**:
- `src/CodeAnalyzer.Roslyn/Models/EnumDefinitionInfo.cs` - Enum definition data model
- `src/CodeAnalyzer.Roslyn/Models/EnumValueInfo.cs` - Enum value data model
- `src/CodeAnalyzer.Roslyn/RoslynAnalyzer.cs` - Added `ExtractEnumDefinitions` method
- `src/CodeAnalyzer.Roslyn/Models/AnalysisResult.cs` - Added enum definitions support
- `tests/CodeAnalyzer.Roslyn.Tests/Models/EnumDefinitionInfoTests.cs` - 5 unit tests
- `tests/CodeAnalyzer.Roslyn.Tests/Models/EnumValueInfoTests.cs` - 5 unit tests
- `tests/CodeAnalyzer.Roslyn.Tests/RoslynAnalyzerEnumDefinitionsTests.cs` - 7 extraction tests
- `tests/CodeAnalyzer.Roslyn.Tests/VectorStoreEnumDefinitionsTests.cs` - 9 integration tests

### **Phase 1E: Interfaces & Structs** ⏱️ *2 hours* ✅ **COMPLETED**
**Goal**: Complete support for interfaces and structs

- [x] Create `InterfaceDefinitionInfo` model with properties:
  - [x] `InterfaceName` (string) - interface name
  - [x] `Namespace` (string) - namespace
  - [x] `FullyQualifiedName` (string) - Namespace.InterfaceName
  - [x] `AccessModifier` (string) - public, private, etc.
  - [x] `BaseInterfaces` (List<string>) - inherited interfaces
  - [x] `MethodCount` (int) - number of methods in interface
  - [x] `PropertyCount` (int) - number of properties in interface
  - [x] `FilePath` (string) - source file path
  - [x] `LineNumber` (int) - line number of definition

- [x] Create `StructDefinitionInfo` model with properties:
  - [x] `StructName` (string) - struct name
  - [x] `Namespace` (string) - namespace
  - [x] `FullyQualifiedName` (string) - Namespace.StructName
  - [x] `AccessModifier` (string) - public, private, etc.
  - [x] `IsReadOnly` (bool) - readonly struct flag
  - [x] `IsRef` (bool) - ref struct flag
  - [x] `Interfaces` (List<string>) - implemented interfaces
  - [x] `MethodCount` (int) - number of methods in struct
  - [x] `PropertyCount` (int) - number of properties in struct
  - [x] `FieldCount` (int) - number of fields in struct
  - [x] `FilePath` (string) - source file path
  - [x] `LineNumber` (int) - line number of definition

- [x] Add extraction methods for interfaces and structs
- [x] Store in vector store with appropriate content
- [x] Add comprehensive tests

**✅ Results**: 
- Created `InterfaceDefinitionInfo` and `StructDefinitionInfo` models
- Implemented `ExtractInterfaceDefinitions` and `ExtractStructDefinitions` methods
- Added `StoreInterfaceDefinitionAsync` and `StoreStructDefinitionAsync` methods
- Updated `AnalysisResult` to include interface and struct definitions
- Added comprehensive unit tests (10 tests for models)
- Added extraction tests (11 tests)
- Added vector store integration tests (10 tests)
- **Total: 31 new tests added, all passing**

### **Phase 1F: Integration & Testing** ⏱️ *3 hours* ✅ **COMPLETED**
**Goal**: Integrate all code element types and ensure comprehensive testing

- [x] Update `AnalysisResult` to include all element types:
  - [x] `ClassDefinitions` (List<ClassDefinitionInfo>)
  - [x] `PropertyDefinitions` (List<PropertyDefinitionInfo>)
  - [x] `FieldDefinitions` (List<FieldDefinitionInfo>)
  - [x] `InterfaceDefinitions` (List<InterfaceDefinitionInfo>)
  - [x] `StructDefinitions` (List<StructDefinitionInfo>)
  - [x] `EnumDefinitions` (List<EnumDefinitionInfo>)

- [x] Update `RoslynAnalyzer` to extract all element types:
  - [x] Integrate all extraction methods in `AnalyzeProjectAsync` and `AnalyzeFileAsync`
  - [x] Ensure consistent error handling across all element types
  - [x] Optimize performance for large codebases

- [x] Update vector store integration:
  - [x] Ensure all element types are stored with consistent metadata schema
  - [x] Add validation for metadata completeness
  - [x] Test vector store retrieval for all element types

- [x] Update console application:
  - [x] Display counts for all element types
  - [x] Show sample definitions for each element type
  - [x] Add filtering options (e.g., show only classes, only enums)

- [x] Comprehensive testing:
  - [x] Integration tests for all element types together
  - [x] Created `ComprehensiveIntegrationTests` with 4 tests verifying all element types work together
  - [x] Updated console application to display all element types in search results
  - [x] Verified all 206 tests pass
  - [ ] Performance tests with large codebases (future enhancement)
  - [ ] Edge case testing (nested types, partial classes, etc.) (future enhancement)
  - [ ] End-to-end testing with real projects (future enhancement)

**✅ Results**:
- All element types integrated into `AnalysisResult` and `RoslynAnalyzer`
- Console application updated to display all element types in both summary and search results
- Comprehensive integration tests created (4 tests)
- All element types stored in vector store with consistent metadata schema
- **Total: 206 tests passing (100% success rate)**

**📁 Files Created/Updated**:
- `src/CodeAnalyzer.Roslyn/Models/InterfaceDefinitionInfo.cs` - Interface model
- `src/CodeAnalyzer.Roslyn/Models/StructDefinitionInfo.cs` - Struct model
- `tests/CodeAnalyzer.Roslyn.Tests/Models/InterfaceDefinitionInfoTests.cs` - 5 unit tests
- `tests/CodeAnalyzer.Roslyn.Tests/Models/StructDefinitionInfoTests.cs` - 7 unit tests
- `tests/CodeAnalyzer.Roslyn.Tests/RoslynAnalyzerInterfaceDefinitionsTests.cs` - 6 extraction tests
- `tests/CodeAnalyzer.Roslyn.Tests/RoslynAnalyzerStructDefinitionsTests.cs` - 7 extraction tests
- `tests/CodeAnalyzer.Roslyn.Tests/VectorStoreInterfaceDefinitionsTests.cs` - 5 integration tests
- `tests/CodeAnalyzer.Roslyn.Tests/VectorStoreStructDefinitionsTests.cs` - 7 integration tests
- `tests/CodeAnalyzer.Roslyn.Tests/ComprehensiveIntegrationTests.cs` - 4 comprehensive integration tests
- `src/CodeAnalyzer.Console/Program.cs` - Updated to display all element types

- [ ] Documentation updates:
  - [ ] Update architecture documentation
  - [ ] Add examples for each element type
  - [ ] Document metadata schema for all element types

### **Updated Success Criteria for Full Phase 1**

- [x] **Method call relationships** extracted and stored ✅
- [x] **Method definitions** extracted and stored ✅
- [x] **Class definitions** extracted and stored ✅
- [x] **Property definitions** extracted and stored ✅
- [x] **Field definitions** extracted and stored ✅
- [x] **Enum definitions** extracted and stored ✅
- [x] **Interface definitions** extracted and stored ✅
- [x] **Struct definitions** extracted and stored ✅
- [x] **Can search for any code element** by name or functionality ✅
- [x] **Vector store contains all element types** with correct metadata ✅

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

## Estimated Total Time: ~12 hours

**Phase 1A (Method Definitions)**: 2 hours ✅ **COMPLETED**
**Phase 1B (Class Definitions)**: 2 hours ✅ **COMPLETED**
**Phase 1C (Properties & Fields)**: 1.5 hours ✅ **COMPLETED**
**Phase 1D (Enum Definitions)**: 1.5 hours ✅ **COMPLETED**
**Phase 1E (Interfaces & Structs)**: 2 hours ✅ **COMPLETED**
**Phase 1F (Integration & Testing)**: 3 hours ✅ **COMPLETED**

**Total Phase 1 Expansion Time**: ~12.5 hours ✅ **COMPLETED**

## Current Status Summary

**✅ Completed Phases:**
- **Phase 1.1-1.8**: Complete method call relationship extraction and console application
- **Phase 1A**: Method definitions extraction and storage
- **Phase 1B**: Class definitions extraction and storage
- **Phase 1C**: Properties & Fields extraction and storage
- **Phase 1D**: Enum definitions extraction and storage
- **Phase 1E**: Interfaces & Structs extraction and storage
- **Phase 1F**: Integration & Testing
- **Phase 1.9.1**: REST API project setup and dependencies
- **Phase 1.9.2**: Project Management Service
- **Phase 1.9.3**: Enumeration Tools
- **Phase 1.9.4**: Code Element Detail Tools

**📊 Test Coverage:**
- **248 tests passing** (100% success rate)
  - 206 tests from Phase 1.1-1.8 and expansions
  - 2 tests from API project setup
  - 14 tests from Project Management Service
  - 11 tests from Enumeration Service
  - 15 tests from Code Element Detail Service
- Comprehensive unit tests for all models
- Integration tests for extraction methods
- Vector store integration tests
- Console application end-to-end testing
- API service layer testing

**🔧 Technical Achievements:**
- Comprehensive error handling
- REPL console interface with help system
- Real vector store integration via adapter pattern
- REST API project with ASP.NET Core and Swagger
- Project management service with async indexing
- Status tracking and vector store management
- Enumeration service for discovering code elements

**Next Phase**: Phase 1.9.5 - Relationship Traversal Tools, then remaining API steps

---

## Phase 1.9: REST API Implementation

**Owner**: AI Agent (with Katie's review)  
**Prerequisites**: Phase 1.1-1.8 completed (method calls, method definitions, class definitions extraction working)  
**Deliverables**: REST API server, tool endpoints, resource endpoints, comprehensive test suite  
**Estimated Time**: 8-10 hours

**Goal**: Create a REST API that exposes code analysis capabilities for agent use. The API should be MCP-compatible and provide composable primitives for agents to answer questions about codebases.

**Current Status**: 🔄 **IN PROGRESS** - Steps 1.9.1-1.9.4 completed

---

### **Step 1.9.1: Project Setup & Dependencies** ⏱️ *30 minutes* ✅ **COMPLETED**

**Goal**: Create API project and add required dependencies

- [x] Create new project `CodeAnalyzer.Api`:
  - [x] Add to solution
  - [x] Target .NET 9
  - [x] Add project reference to `CodeAnalyzer.Roslyn`
  - [x] Add project reference to `CodeAnalyzer.Navigation`

- [x] Add NuGet packages:
  - [x] `Microsoft.AspNetCore.OpenApi` (for OpenAPI/Swagger)
  - [x] `Swashbuckle.AspNetCore` (for Swagger UI)
  - [x] `VectorStore` (for vector store integration)
  - [x] `Microsoft.Extensions.Logging.Abstractions` (for logging)

- [x] Create basic project structure:
  - [x] `Controllers/` folder
  - [x] `Models/` folder (for API request/response models)
  - [x] `Services/` folder (for business logic)
  - [x] `Program.cs` with minimal setup

- [x] Write initial tests:
  - [x] Test that API project builds
  - [x] Test that server can start (minimal health check)

- [x] Verify project builds and basic server starts

**✅ Results**: API project successfully created with ASP.NET Core setup. 2 tests passing, Swagger UI configured.

**📁 Files Created**:
- `src/CodeAnalyzer.Api/CodeAnalyzer.Api.csproj`
- `src/CodeAnalyzer.Api/Program.cs`
- `tests/CodeAnalyzer.Api.Tests/CodeAnalyzer.Api.Tests.csproj`
- `tests/CodeAnalyzer.Api.Tests/ProjectSetupTests.cs`

---

### **Step 1.9.2: Project Management Service** ⏱️ *1 hour* ✅ **COMPLETED**

**Goal**: Create service layer for managing projects and their vector stores

- [x] Create `IProjectManager` interface:
  - [x] `Task<string> IndexProjectAsync(string projectPath, string? projectName)`
  - [x] `Task<ProjectStatus> GetProjectStatusAsync(string projectId)`
  - [x] `Task<List<ProjectInfo>> ListProjectsAsync()`
  - [x] `Task<bool> DeleteProjectAsync(string projectId)`

- [x] Create `ProjectManager` implementation:
  - [x] Store project metadata (in-memory dictionary for now, can persist later)
  - [x] Manage vector store paths per project
  - [x] Track analysis jobs and status
  - [x] Integrate with `RoslynAnalyzer` for indexing

- [x] Create models:
  - [x] `ProjectInfo` - project metadata
  - [x] `ProjectStatus` - analysis status and progress (with `IndexingStatus` enum)
  - [x] `IndexProjectRequest` - request model
  - [x] `IndexProjectResponse` - response model

- [x] Write tests:
  - [x] Test project indexing starts successfully
  - [x] Test project status retrieval
  - [x] Test project listing
  - [x] Test project deletion
  - [x] Test error handling (invalid paths, etc.)
  - [x] Test duplicate project handling
  - [x] Test status updates during indexing

- [x] Verify all tests pass

**✅ Results**: Project management service successfully implemented with async indexing, status tracking, and vector store integration. 14 tests passing (100% success rate).

**📁 Files Created**:
- `src/CodeAnalyzer.Api/Services/IProjectManager.cs`
- `src/CodeAnalyzer.Api/Services/ProjectManager.cs`
- `src/CodeAnalyzer.Api/Models/ProjectInfo.cs`
- `src/CodeAnalyzer.Api/Models/ProjectStatus.cs`
- `src/CodeAnalyzer.Api/Models/IndexProjectRequest.cs`
- `src/CodeAnalyzer.Api/Models/IndexProjectResponse.cs`
- `tests/CodeAnalyzer.Api.Tests/Services/ProjectManagerTests.cs`

**🔧 Technical Details**:
- In-memory project storage with dictionary-based tracking
- Async background indexing using `Task.Run`
- Per-project vector store management
- Status tracking with `IndexingStatus` enum (Queued, Indexing, Completed, Failed, NotIndexed)
- Comprehensive error handling and logging support
- Project ID generation based on project path hash

---

### **Step 1.9.3: Enumeration Tools** ⏱️ *1.5 hours* ✅ **COMPLETED**

**Goal**: Implement tools for discovering code elements (list_classes, list_methods, list_entry_points)

- [x] Create `IEnumerationService` interface:
  - [x] `Task<ClassListResponse> ListClassesAsync(string projectId, string? namespace, int limit, int offset)`
  - [x] `Task<MethodListResponse> ListMethodsAsync(string projectId, string? className, string? namespace, int limit, int offset)`
  - [x] `Task<EntryPointListResponse> ListEntryPointsAsync(string projectId, string? type)`

- [x] Create `EnumerationService` implementation:
  - [x] Load vector store for project
  - [x] Query vector store for class definitions (metadata type: "class_definition")
  - [x] Query vector store for method definitions (metadata type: "method_definition")
  - [x] Filter and paginate results
  - [x] Detect entry points (Main methods, controllers with [HttpPost]/[HttpGet] attributes)

- [x] Create response models:
  - [x] `ClassListResponse` with pagination
  - [x] `MethodListResponse` with pagination
  - [x] `EntryPointListResponse`
  - [x] `ClassInfo`, `MethodInfo`, `EntryPointInfo`

- [x] Write tests:
  - [x] Test listing classes with no filters
  - [x] Test listing classes with namespace filter
  - [x] Test listing classes with pagination
  - [x] Test listing methods with class filter
  - [x] Test listing methods with namespace filter
  - [x] Test listing entry points
  - [x] Test error handling (project not found, etc.)

- [x] Verify all tests pass

**✅ Results**: Enumeration service successfully implemented with vector store querying, filtering, pagination, and entry point detection. 11 tests passing (100% success rate).

**📁 Files Created**:
- `src/CodeAnalyzer.Api/Services/IEnumerationService.cs`
- `src/CodeAnalyzer.Api/Services/EnumerationService.cs`
- `src/CodeAnalyzer.Api/Models/ClassInfo.cs`
- `src/CodeAnalyzer.Api/Models/MethodInfo.cs`
- `src/CodeAnalyzer.Api/Models/EntryPointInfo.cs`
- `src/CodeAnalyzer.Api/Models/ClassListResponse.cs`
- `src/CodeAnalyzer.Api/Models/MethodListResponse.cs`
- `src/CodeAnalyzer.Api/Models/EntryPointListResponse.cs`
- `tests/CodeAnalyzer.Api.Tests/Services/EnumerationServiceTests.cs`

**🔧 Technical Details**:
- Vector store querying via `FileVectorStoreAdapter` with proper disposal
- Metadata-based filtering (type: "class_definition", "method_definition")
- Namespace filtering for classes and methods
- Class name filtering for methods
- Pagination support with limit and offset
- Entry point detection for Main methods and Controller classes
- Integration with `IProjectManager` for project access
- Comprehensive error handling for invalid project IDs

---

### **Step 1.9.4: Code Element Detail Tools** ⏱️ *1 hour* ✅ **COMPLETED**

**Goal**: Implement tools for getting detailed information about methods and classes

- [x] Create `ICodeElementService` interface:
  - [x] `Task<MethodDetailResponse> GetMethodAsync(string projectId, string methodFqn)`
  - [x] `Task<ClassDetailResponse> GetClassAsync(string projectId, string classFqn)`
  - [x] `Task<ClassMethodsResponse> GetClassMethodsAsync(string projectId, string classFqn)`

- [x] Create `CodeElementService` implementation:
  - [x] Load vector store for project
  - [x] Query vector store for method/class definitions by FQN
  - [x] Extract metadata and format response
  - [x] Handle not found cases

- [x] Create response models:
  - [x] `MethodDetailResponse`
  - [x] `ClassDetailResponse`
  - [x] `ClassMethodsResponse`

- [x] Write tests:
  - [x] Test getting method by FQN
  - [x] Test getting class by FQN
  - [x] Test getting class methods
  - [x] Test error handling (method not found, class not found)

- [x] Verify all tests pass

**✅ Results**: Code element detail service successfully implemented with vector store querying, metadata extraction, and comprehensive error handling. 15 tests passing (100% success rate).

**📁 Files Created**:
- `src/CodeAnalyzer.Api/Services/ICodeElementService.cs` - Service interface
- `src/CodeAnalyzer.Api/Services/CodeElementService.cs` - Service implementation
- `src/CodeAnalyzer.Api/Models/MethodDetailResponse.cs` - Method detail response model
- `src/CodeAnalyzer.Api/Models/ClassDetailResponse.cs` - Class detail response model
- `src/CodeAnalyzer.Api/Models/ClassMethodsResponse.cs` - Class methods response model
- `tests/CodeAnalyzer.Api.Tests/Services/CodeElementServiceTests.cs` - 15 comprehensive tests

**🔧 Technical Details**:
- Vector store querying via `FileVectorStoreAdapter` with proper disposal
- FQN-based lookup for methods and classes
- Complete metadata extraction including modifiers, parameters, inheritance
- Comprehensive error handling for invalid project IDs, empty FQNs, and not found cases
- Integration with `IProjectManager` for project access

---

### **Step 1.9.5: Relationship Traversal Tools** ⏱️ *2 hours*

**Goal**: Implement tools for walking call graphs (get_callers, get_callees, get_class_references)

- [ ] Create `IRelationshipService` interface:
  - [ ] `Task<CallersResponse> GetCallersAsync(string projectId, string methodFqn, int depth, bool includeSelf)`
  - [ ] `Task<CalleesResponse> GetCalleesAsync(string projectId, string methodFqn, int depth, bool includeSelf)`
  - [ ] `Task<ClassReferencesResponse> GetClassReferencesAsync(string projectId, string classFqn, string? relationshipType)`

- [ ] Create `RelationshipService` implementation:
  - [ ] Load vector store for project
  - [ ] Query method_call metadata for callers (where callee = methodFqn)
  - [ ] Query method_call metadata for callees (where caller = methodFqn)
  - [ ] Implement depth traversal (recursive or iterative)
  - [ ] For class references: find classes that call methods in target class
  - [ ] Format results with depth information

- [ ] Create response models:
  - [ ] `CallersResponse` with caller list and depth info
  - [ ] `CalleesResponse` with callee list and depth info
  - [ ] `ClassReferencesResponse` with reference list
  - [ ] `CallerInfo`, `CalleeInfo`, `ClassReferenceInfo`

- [ ] Write tests:
  - [ ] Test getting direct callers (depth=1)
  - [ ] Test getting direct callees (depth=1)
  - [ ] Test depth traversal (depth=2, depth=3)
  - [ ] Test include_self flag
  - [ ] Test getting class references
  - [ ] Test error handling (method not found, circular references)

- [ ] Verify all tests pass

**📁 Files Created**:
- `src/CodeAnalyzer.Api/Services/IRelationshipService.cs`
- `src/CodeAnalyzer.Api/Services/RelationshipService.cs`
- `src/CodeAnalyzer.Api/Models/CallersResponse.cs`
- `src/CodeAnalyzer.Api/Models/CalleesResponse.cs`
- `src/CodeAnalyzer.Api/Models/ClassReferencesResponse.cs`
- `tests/CodeAnalyzer.Api.Tests/Services/RelationshipServiceTests.cs`

**Note**: This step may need to be updated once Phase 2 (Call Index) is implemented for better performance.

---

### **Step 1.9.6: Search Tool** ⏱️ *1 hour*

**Goal**: Implement semantic search capability

- [ ] Create `ISearchService` interface:
  - [ ] `Task<SearchResponse> SearchCodeAsync(string projectId, string query, int limit, string[]? types, double? minSimilarity)`

- [ ] Create `SearchService` implementation:
  - [ ] Load vector store for project
  - [ ] Use `vectorStore.SearchTextAsync(query, limit)`
  - [ ] Filter by element types if specified
  - [ ] Filter by similarity threshold
  - [ ] Format results with similarity scores

- [ ] Create response models:
  - [ ] `SearchResponse` with results list
  - [ ] `SearchResult` with element info and similarity

- [ ] Write tests:
  - [ ] Test basic semantic search
  - [ ] Test search with type filter
  - [ ] Test search with similarity threshold
  - [ ] Test search with limit
  - [ ] Test empty results

- [ ] Verify all tests pass

**📁 Files Created**:
- `src/CodeAnalyzer.Api/Services/ISearchService.cs`
- `src/CodeAnalyzer.Api/Services/SearchService.cs`
- `src/CodeAnalyzer.Api/Models/SearchResponse.cs`
- `tests/CodeAnalyzer.Api.Tests/Services/SearchServiceTests.cs`

---

### **Step 1.9.7: Tool Controllers** ⏱️ *1.5 hours*

**Goal**: Create ASP.NET Core controllers for all tool endpoints

- [ ] Create `ToolsController`:
  - [ ] `POST /api/tools/index_project` → calls ProjectManager
  - [ ] `POST /api/tools/list_projects` → calls ProjectManager
  - [ ] `POST /api/tools/list_classes` → calls EnumerationService
  - [ ] `POST /api/tools/list_methods` → calls EnumerationService
  - [ ] `POST /api/tools/list_entry_points` → calls EnumerationService
  - [ ] `POST /api/tools/get_method` → calls CodeElementService
  - [ ] `POST /api/tools/get_class` → calls CodeElementService
  - [ ] `POST /api/tools/get_class_methods` → calls CodeElementService
  - [ ] `POST /api/tools/get_callers` → calls RelationshipService
  - [ ] `POST /api/tools/get_callees` → calls RelationshipService
  - [ ] `POST /api/tools/get_class_references` → calls RelationshipService
  - [ ] `POST /api/tools/search_code` → calls SearchService
  - [ ] `POST /api/tools/get_project_status` → calls ProjectManager

- [ ] Implement request validation:
  - [ ] Validate required parameters
  - [ ] Validate project_id exists
  - [ ] Return appropriate error responses

- [ ] Implement error handling:
  - [ ] Catch exceptions and return proper error responses
  - [ ] Use consistent error format from API spec

- [ ] Write integration tests:
  - [ ] Test each endpoint with valid requests
  - [ ] Test each endpoint with invalid requests
  - [ ] Test error responses
  - [ ] Test request validation

- [ ] Verify all tests pass

**📁 Files Created**:
- `src/CodeAnalyzer.Api/Controllers/ToolsController.cs`
- `tests/CodeAnalyzer.Api.Tests/Controllers/ToolsControllerTests.cs`

---

### **Step 1.9.8: Resource Endpoints** ⏱️ *30 minutes*

**Goal**: Create GET endpoints for resources (read-only data access)

- [ ] Create `ResourcesController`:
  - [ ] `GET /api/resources/project/{projectId}` → returns project status
  - [ ] `GET /api/resources/method/{projectId}/{methodFqn}` → returns method details
  - [ ] `GET /api/resources/class/{projectId}/{classFqn}` → returns class details

- [ ] Implement URL decoding for FQNs in routes
- [ ] Add caching headers (optional, for future optimization)

- [ ] Write tests:
  - [ ] Test each resource endpoint
  - [ ] Test URL encoding/decoding
  - [ ] Test 404 responses

- [ ] Verify all tests pass

**📁 Files Created**:
- `src/CodeAnalyzer.Api/Controllers/ResourcesController.cs`
- `tests/CodeAnalyzer.Api.Tests/Controllers/ResourcesControllerTests.cs`

---

### **Step 1.9.9: Tool Discovery & OpenAPI** ⏱️ *1 hour*

**Goal**: Add tool discovery endpoint and OpenAPI/Swagger documentation

- [ ] Create `GET /api/tools` endpoint:
  - [ ] Returns list of all available tools
  - [ ] Includes tool name, description, and input schema
  - [ ] Matches MCP tool registration format

- [ ] Configure Swagger/OpenAPI:
  - [ ] Add Swagger UI
  - [ ] Configure API documentation
  - [ ] Add XML comments to controllers/models

- [ ] Write tests:
  - [ ] Test tool discovery endpoint returns all tools
  - [ ] Test tool schemas are valid JSON Schema

- [ ] Verify Swagger UI works and shows all endpoints

**📁 Files Created**:
- `src/CodeAnalyzer.Api/Controllers/ToolsController.cs` (add discovery endpoint)
- `src/CodeAnalyzer.Api/Models/ToolDefinition.cs`
- Swagger configuration in `Program.cs`

---

### **Step 1.9.10: Error Handling & Validation** ⏱️ *1 hour*

**Goal**: Implement comprehensive error handling and request validation

- [ ] Create global error handler middleware:
  - [ ] Catch unhandled exceptions
  - [ ] Format errors according to API spec
  - [ ] Log errors appropriately

- [ ] Create custom exception types:
  - [ ] `ProjectNotFoundException`
  - [ ] `MethodNotFoundException`
  - [ ] `ClassNotFoundException`
  - [ ] `InvalidParameterException`

- [ ] Add request validation:
  - [ ] Use data annotations on request models
  - [ ] Validate FQN formats
  - [ ] Validate depth parameters (must be > 0)
  - [ ] Validate limit/offset (must be >= 0)

- [ ] Write tests:
  - [ ] Test error responses match API spec format
  - [ ] Test all error codes
  - [ ] Test validation errors

- [ ] Verify all error cases handled correctly

**📁 Files Created**:
- `src/CodeAnalyzer.Api/Middleware/ErrorHandlingMiddleware.cs`
- `src/CodeAnalyzer.Api/Exceptions/ProjectNotFoundException.cs`
- `src/CodeAnalyzer.Api/Exceptions/MethodNotFoundException.cs`
- `src/CodeAnalyzer.Api/Exceptions/ClassNotFoundException.cs`
- `src/CodeAnalyzer.Api/Exceptions/InvalidParameterException.cs`
- `tests/CodeAnalyzer.Api.Tests/Middleware/ErrorHandlingMiddlewareTests.cs`

---

### **Step 1.9.11: Integration Testing** ⏱️ *1 hour*

**Goal**: Create end-to-end integration tests

- [ ] Create test project setup:
  - [ ] Sample C# project for testing
  - [ ] Test vector store setup
  - [ ] Test data preparation

- [ ] Write integration tests:
  - [ ] Test full workflow: index project → query methods → get relationships
  - [ ] Test multiple projects
  - [ ] Test concurrent requests
  - [ ] Test error scenarios end-to-end

- [ ] Performance testing:
  - [ ] Test response times are acceptable (< 2 seconds for most operations)
  - [ ] Test pagination works for large result sets

- [ ] Verify all integration tests pass

**📁 Files Created**:
- `tests/CodeAnalyzer.Api.Tests/Integration/ApiIntegrationTests.cs`
- `tests/CodeAnalyzer.Api.Tests/TestData/` (sample projects)

---

### **Step 1.9.12: Documentation & Finalization** ⏱️ *30 minutes*

**Goal**: Finalize API implementation and documentation

- [ ] Update API_SPEC.md with any deviations or clarifications
- [ ] Add XML documentation comments to all public APIs
- [ ] Verify all endpoints match API spec
- [ ] Test API with sample agent workflow (manual testing)
- [ ] Update README.md with API usage instructions
- [ ] Create example requests/responses

- [ ] Final verification:
  - [ ] All tests pass
  - [ ] API builds without warnings
  - [ ] Swagger UI shows all endpoints correctly
  - [ ] Error responses match spec

**📁 Files Updated**:
- `API_SPEC.md` (if needed)
- `README.md`
- `src/CodeAnalyzer.Api/` (XML comments)

---

## Success Criteria for Phase 1.9

- [ ] All tool endpoints implemented and working
- [ ] All resource endpoints implemented and working
- [ ] Tool discovery endpoint returns complete tool list
- [ ] Error handling matches API spec
- [ ] All tests pass (unit + integration)
- [ ] Swagger UI functional and complete
- [ ] API can be used by agents to answer questions about codebases
- [ ] Performance is acceptable (< 2 seconds for most operations)

---

## Dependencies & Notes

**Dependencies**:
- Phase 1.1-1.8 must be complete (method calls, method definitions, class definitions)
- Vector store must be working
- RoslynAnalyzer must be functional

**Future Enhancements** (can be added later):
- Phase 2 (Call Index) integration for better performance on relationship queries
- Authentication/authorization
- Rate limiting
- Caching layer
- WebSocket support for real-time updates
- Batch operation endpoints

**Performance Considerations**:
- Initial implementation uses vector store queries directly
- Once Phase 2 (Call Index) is complete, can optimize relationship queries
- Pagination is important for large projects
- Consider caching frequently accessed resources

---

**Next Phase**: Phase 2 - Call Index Builder (can be done in parallel or after API)

---

*This plan follows the TDD approach outlined in the architecture document. Each step builds incrementally on the previous steps, ensuring we always have working code.*
