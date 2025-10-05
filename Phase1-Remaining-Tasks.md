## Phase 1 (1.1–1.7) Remaining Work – Execution Plan

This document tracks the remaining items from Phase 1 (excluding 1.8) and how we will complete them.

### 1.5 Inheritance Scenarios

- [x] Implement inherited method call resolution in `RoslynAnalyzer`:
  - [x] Use `IMethodSymbol.OverriddenMethod`, `IsOverride`, `IsVirtual`, `IsAbstract`, `ContainingType.BaseType`
  - [x] Normalize interface and virtual dispatch targets to declared interface/base method
  - [x] Handle `base.Method()` and `this.Method()` consistently (record on base symbol)
- [x] Tests:
  - [x] Override call path: base virtual → derived override (recorded on base)
  - [x] Abstract base + derived concrete override
  - [x] Interface call through variable typed as interface to concrete
  - [x] Explicit interface implementation
  - [x] Base qualifier `base.Foo()` vs `this.Foo()`

### 1.6 Vector Store Validation/Retrieval

- [ ] Add metadata validation/normalization before writes:
  - [ ] Required keys present: `type`, `caller`, `callee`, `caller_class`, `callee_class`, `caller_namespace`, `callee_namespace`, `file_path`, `line_number`
  - [ ] Normalize FQNs, trim whitespace, ensure relative paths, coerce `line_number` to int ≥ 1
  - [ ] Reject or log-and-skip invalid entries; append error to `AnalysisResult.Errors`
- [ ] Tests:
  - [ ] Unit tests for happy path, missing fields, bad types, whitespace normalization
- [ ] Real store integration test:
  - [ ] Use the existing `FileVectorStore` adapter in tests to write a small set of calls, then read back and assert content/metadata

### 1.7 Attribute/Initializer Calls

- [ ] Implement optional handling controlled by `AnalyzerOptions.AttributeInitializerCalls`:
  - [ ] Detect attribute constructor invocations and field/property initializer invocations
  - [ ] Attribute constructor: include as `method_call` with caller being the containing member or type as documented; gate by option
  - [ ] Initializers: include constructor-body initializer calls if option enabled; otherwise continue skipping
- [ ] Tests:
  - [ ] Attribute on class and method with constructor call recorded when enabled and absent when disabled
  - [ ] Field/property initializer invoking a method
  - [ ] Ensure local/lambda behavior remains consistent

### Documentation Updates

- [ ] After each subtask completes, check off the corresponding boxes in `PhaseOnePlan.md` for 1.5–1.7, and note any caveats (e.g., which attribute/initializer cases are included).


