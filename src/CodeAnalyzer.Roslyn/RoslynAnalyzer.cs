using CodeAnalyzer.Roslyn.Models;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeAnalyzer.Roslyn;

/// <summary>
/// Analyzes C# projects using Roslyn to extract method call relationships.
/// This is the main entry point for Phase 1 of the C# Code Navigator.
/// </summary>
public class RoslynAnalyzer
{
    private readonly IVectorStoreWriter? _vectorStore;
    private readonly AnalyzerOptions _options = new();
    /// <summary>
    /// Creates a new instance of RoslynAnalyzer
    /// </summary>
    public RoslynAnalyzer()
    {
    }

    /// <summary>
    /// Optional constructor to enable persistence to a vector store
    /// </summary>
    public RoslynAnalyzer(IVectorStoreWriter vectorStore)
    {
        _vectorStore = vectorStore;
    }

    public RoslynAnalyzer WithOptions(AnalyzerOptions options)
    {
        if (options != null)
        {
            _options.IncludeWarningsInErrors = options.IncludeWarningsInErrors;
            _options.RecordExternalCalls = options.RecordExternalCalls;
            _options.AttributeInitializerCalls = options.AttributeInitializerCalls;
        }
        return this;
    }

    /// <summary>
    /// Analyzes a C# project and extracts all method call relationships.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file or project directory</param>
    /// <returns>AnalysisResult containing all discovered method calls and metadata</returns>
    public async Task<AnalysisResult> AnalyzeProjectAsync(string projectPath)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
            throw new ArgumentException("Project path is required", nameof(projectPath));

        var result = new AnalysisResult
        {
            FilesProcessed = 0,
            MethodsAnalyzed = 0,
            MethodCalls = new List<MethodCallInfo>(),
            MethodDefinitions = new List<MethodDefinitionInfo>(),
            ClassDefinitions = new List<ClassDefinitionInfo>(),
            PropertyDefinitions = new List<PropertyDefinitionInfo>(),
            FieldDefinitions = new List<FieldDefinitionInfo>(),
            EnumDefinitions = new List<EnumDefinitionInfo>(),
            InterfaceDefinitions = new List<InterfaceDefinitionInfo>(),
            StructDefinitions = new List<StructDefinitionInfo>(),
            Errors = new List<string>()
        };

        try
        {
            var compilation = await CreateCompilationAsync(projectPath).ConfigureAwait(false);

            // Collect diagnostics (non-fatal)
            var diagnostics = compilation.GetDiagnostics();
            foreach (var d in diagnostics)
            {
                if (d.Severity == DiagnosticSeverity.Error || (_options.IncludeWarningsInErrors && d.Severity == DiagnosticSeverity.Warning))
                    result.Errors.Add(d.ToString());
            }

            foreach (var tree in compilation.SyntaxTrees)
            {
                var model = compilation.GetSemanticModel(tree);
                var methods = ExtractMethodDeclarations(tree, model);
                result.MethodsAnalyzed += methods.Count;

                var methodDefinitions = ExtractMethodDefinitions(tree, model);
                result.MethodDefinitions.AddRange(methodDefinitions);

                var classDefinitions = ExtractClassDefinitions(tree, model);
                result.ClassDefinitions.AddRange(classDefinitions);

                var propertyDefinitions = ExtractPropertyDefinitions(tree, model);
                result.PropertyDefinitions.AddRange(propertyDefinitions);

                var fieldDefinitions = ExtractFieldDefinitions(tree, model);
                result.FieldDefinitions.AddRange(fieldDefinitions);

                var enumDefinitions = ExtractEnumDefinitions(tree, model);
                result.EnumDefinitions.AddRange(enumDefinitions);

                var interfaceDefinitions = ExtractInterfaceDefinitions(tree, model);
                result.InterfaceDefinitions.AddRange(interfaceDefinitions);

                var structDefinitions = ExtractStructDefinitions(tree, model);
                result.StructDefinitions.AddRange(structDefinitions);

                var calls = ExtractMethodCalls(tree, model);
                result.MethodCalls.AddRange(calls);
                result.FilesProcessed += 1;

                if (_vectorStore != null)
                {
                    // Store method definitions
                    foreach (var methodDef in methodDefinitions)
                    {
                        try
                        {
                            await StoreMethodDefinitionAsync(methodDef).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            result.Errors.Add($"VectorStore write failed for method definition {methodDef.FullyQualifiedName}: {ex.Message}");
                        }
                    }

                    // Store class definitions
                    foreach (var classDef in classDefinitions)
                    {
                        try
                        {
                            await StoreClassDefinitionAsync(classDef).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            result.Errors.Add($"VectorStore write failed for class definition {classDef.FullyQualifiedName}: {ex.Message}");
                        }
                    }

                    // Store property definitions
                    foreach (var propertyDef in propertyDefinitions)
                    {
                        try
                        {
                            await StorePropertyDefinitionAsync(propertyDef).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            result.Errors.Add($"VectorStore write failed for property definition {propertyDef.FullyQualifiedName}: {ex.Message}");
                        }
                    }

                    // Store field definitions
                    foreach (var fieldDef in fieldDefinitions)
                    {
                        try
                        {
                            await StoreFieldDefinitionAsync(fieldDef).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            result.Errors.Add($"VectorStore write failed for field definition {fieldDef.FullyQualifiedName}: {ex.Message}");
                        }
                    }

                    // Store enum definitions
                    foreach (var enumDef in enumDefinitions)
                    {
                        try
                        {
                            await StoreEnumDefinitionAsync(enumDef).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            result.Errors.Add($"VectorStore write failed for enum definition {enumDef.FullyQualifiedName}: {ex.Message}");
                        }
                    }

                    // Store interface definitions
                    foreach (var interfaceDef in interfaceDefinitions)
                    {
                        try
                        {
                            await StoreInterfaceDefinitionAsync(interfaceDef).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            result.Errors.Add($"VectorStore write failed for interface definition {interfaceDef.FullyQualifiedName}: {ex.Message}");
                        }
                    }

                    // Store struct definitions
                    foreach (var structDef in structDefinitions)
                    {
                        try
                        {
                            await StoreStructDefinitionAsync(structDef).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            result.Errors.Add($"VectorStore write failed for struct definition {structDef.FullyQualifiedName}: {ex.Message}");
                        }
                    }

                    // Store method calls
                    foreach (var call in calls)
                    {
                        try
                        {
                            await StoreMethodCallAsync(call).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            result.Errors.Add($"VectorStore write failed for call {call.Caller} -> {call.Callee}: {ex.Message}");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Project analysis failed for '{projectPath}': {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Analyzes a single C# file and extracts method call relationships.
    /// </summary>
    /// <param name="filePath">Path to the .cs file</param>
    /// <returns>AnalysisResult containing method calls found in the file</returns>
    public async Task<AnalysisResult> AnalyzeFileAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path is required", nameof(filePath));

        var compilation = await CreateCompilationFromFilesAsync(filePath).ConfigureAwait(false);
        var tree = compilation.SyntaxTrees.First();
        var model = compilation.GetSemanticModel(tree);

        var result = new AnalysisResult
        {
            FilesProcessed = 1,
            MethodsAnalyzed = 0,
            MethodCalls = new List<MethodCallInfo>(),
            MethodDefinitions = new List<MethodDefinitionInfo>(),
            ClassDefinitions = new List<ClassDefinitionInfo>(),
            PropertyDefinitions = new List<PropertyDefinitionInfo>(),
            FieldDefinitions = new List<FieldDefinitionInfo>(),
            EnumDefinitions = new List<EnumDefinitionInfo>(),
            InterfaceDefinitions = new List<InterfaceDefinitionInfo>(),
            StructDefinitions = new List<StructDefinitionInfo>(),
            Errors = new List<string>()
        };

        try
        {
            var methods = ExtractMethodDeclarations(tree, model);
            result.MethodsAnalyzed = methods.Count;

            var methodDefinitions = ExtractMethodDefinitions(tree, model);
            result.MethodDefinitions.AddRange(methodDefinitions);

            var classDefinitions = ExtractClassDefinitions(tree, model);
            result.ClassDefinitions.AddRange(classDefinitions);

            var propertyDefinitions = ExtractPropertyDefinitions(tree, model);
            result.PropertyDefinitions.AddRange(propertyDefinitions);

            var fieldDefinitions = ExtractFieldDefinitions(tree, model);
            result.FieldDefinitions.AddRange(fieldDefinitions);

            var enumDefinitions = ExtractEnumDefinitions(tree, model);
            result.EnumDefinitions.AddRange(enumDefinitions);

            var interfaceDefinitions = ExtractInterfaceDefinitions(tree, model);
            result.InterfaceDefinitions.AddRange(interfaceDefinitions);

            var structDefinitions = ExtractStructDefinitions(tree, model);
            result.StructDefinitions.AddRange(structDefinitions);

            var calls = ExtractMethodCalls(tree, model);
            result.MethodCalls.AddRange(calls);

            // Collect file-specific diagnostics if requested in file mode
            var diags = model.Compilation.GetDiagnostics();
            foreach (var d in diags)
            {
                if (d.Severity == DiagnosticSeverity.Error || (_options.IncludeWarningsInErrors && d.Severity == DiagnosticSeverity.Warning))
                    result.Errors.Add(d.ToString());
            }

            if (_vectorStore != null)
            {
                // Store method definitions
                foreach (var methodDef in methodDefinitions)
                {
                    try
                    {
                        await StoreMethodDefinitionAsync(methodDef).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"VectorStore write failed for method definition {methodDef.FullyQualifiedName}: {ex.Message}");
                    }
                }

                // Store class definitions
                foreach (var classDef in classDefinitions)
                {
                    try
                    {
                        await StoreClassDefinitionAsync(classDef).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"VectorStore write failed for class definition {classDef.FullyQualifiedName}: {ex.Message}");
                    }
                }

                // Store property definitions
                foreach (var propertyDef in propertyDefinitions)
                {
                    try
                    {
                        await StorePropertyDefinitionAsync(propertyDef).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"VectorStore write failed for property definition {propertyDef.FullyQualifiedName}: {ex.Message}");
                    }
                }

                // Store field definitions
                foreach (var fieldDef in fieldDefinitions)
                {
                    try
                    {
                        await StoreFieldDefinitionAsync(fieldDef).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"VectorStore write failed for field definition {fieldDef.FullyQualifiedName}: {ex.Message}");
                    }
                }

                // Store enum definitions
                foreach (var enumDef in enumDefinitions)
                {
                    try
                    {
                        await StoreEnumDefinitionAsync(enumDef).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"VectorStore write failed for enum definition {enumDef.FullyQualifiedName}: {ex.Message}");
                    }
                }

                // Store interface definitions
                foreach (var interfaceDef in interfaceDefinitions)
                {
                    try
                    {
                        await StoreInterfaceDefinitionAsync(interfaceDef).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"VectorStore write failed for interface definition {interfaceDef.FullyQualifiedName}: {ex.Message}");
                    }
                }

                // Store struct definitions
                foreach (var structDef in structDefinitions)
                {
                    try
                    {
                        await StoreStructDefinitionAsync(structDef).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"VectorStore write failed for struct definition {structDef.FullyQualifiedName}: {ex.Message}");
                    }
                }

                // Store method calls
                foreach (var call in calls)
                {
                    try
                    {
                        await StoreMethodCallAsync(call).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"VectorStore write failed for call {call.Caller} -> {call.Callee}: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"File analysis failed for '{filePath}': {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Gets the version of the analyzer for debugging purposes
    /// </summary>
    public string Version => "1.0.0-phase1";

    /// <summary>
    /// Create a Roslyn Compilation for a C# project using MSBuildWorkspace.
    /// </summary>
    /// <param name="projectPath">Path to a .csproj file or directory containing a .csproj file</param>
    /// <returns>Compilation with all syntax trees and references loaded</returns>
    public async Task<Compilation> CreateCompilationAsync(string projectPath)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
            throw new ArgumentException("Project path is required", nameof(projectPath));

        // Ensure MSBuild is registered once per process
        if (!MSBuildLocator.IsRegistered)
        {
            MSBuildLocator.RegisterDefaults();
        }

        // Handle directory paths by finding the .csproj file
        string actualProjectPath = projectPath;
        if (Directory.Exists(projectPath))
        {
            var csprojFiles = Directory.GetFiles(projectPath, "*.csproj");
            if (csprojFiles.Length == 0)
            {
                throw new FileNotFoundException($"No .csproj file found in directory: {projectPath}");
            }
            if (csprojFiles.Length > 1)
            {
                throw new InvalidOperationException($"Multiple .csproj files found in directory: {projectPath}. Please specify the exact project file.");
            }
            actualProjectPath = csprojFiles[0];
        }
        else if (!File.Exists(projectPath))
        {
            throw new FileNotFoundException($"Project file not found: {projectPath}");
        }

        using var workspace = MSBuildWorkspace.Create();
        var project = await workspace.OpenProjectAsync(actualProjectPath).ConfigureAwait(false);
        var compilation = await project.GetCompilationAsync().ConfigureAwait(false);
        if (compilation == null)
            throw new InvalidOperationException("Failed to create compilation for project");

        return compilation;
    }

    /// <summary>
    /// Create a Roslyn Compilation from one or more C# source files without a project file.
    /// </summary>
    /// <param name="filePaths">Paths to .cs files</param>
    /// <returns>Compilation suitable for basic semantic analysis</returns>
    public async Task<Compilation> CreateCompilationFromFilesAsync(params string[] filePaths)
    {
        if (filePaths == null || filePaths.Length == 0)
            throw new ArgumentException("At least one C# file path is required", nameof(filePaths));

        var syntaxTrees = new List<SyntaxTree>();
        foreach (var path in filePaths)
        {
            var source = await File.ReadAllTextAsync(path).ConfigureAwait(false);
            var tree = CSharpSyntaxTree.ParseText(source, path: path);
            syntaxTrees.Add(tree);
        }

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location)
        };

        var compilation = CSharpCompilation.Create(
            assemblyName: "AnalysisCompilation",
            syntaxTrees: syntaxTrees,
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        return compilation;
    }

    /// <summary>
    /// Extract fully qualified method names declared in the given syntax tree.
    /// </summary>
    /// <param name="tree">Syntax tree to scan</param>
    /// <param name="model">Semantic model associated with the tree</param>
    /// <returns>List of fully qualified method names</returns>
    public List<string> ExtractMethodDeclarations(SyntaxTree tree, SemanticModel model)
    {
        if (tree == null) throw new ArgumentNullException(nameof(tree));
        if (model == null) throw new ArgumentNullException(nameof(model));

        var root = tree.GetRoot();
        var results = new List<string>();

        foreach (var methodDecl in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            var symbol = model.GetDeclaredSymbol(methodDecl);
            if (symbol == null)
                continue;

            var fqn = GetFullyQualifiedName(symbol);
            if (!string.IsNullOrWhiteSpace(fqn))
                results.Add(fqn);
        }

        return results;
    }

    /// <summary>
    /// Extract method definitions from the given syntax tree using the provided semantic model.
    /// </summary>
    /// <param name="tree">Syntax tree to scan</param>
    /// <param name="model">Semantic model associated with the tree</param>
    /// <returns>List of method definition information</returns>
    public List<MethodDefinitionInfo> ExtractMethodDefinitions(SyntaxTree tree, SemanticModel model)
    {
        if (tree == null) throw new ArgumentNullException(nameof(tree));
        if (model == null) throw new ArgumentNullException(nameof(model));

        var root = tree.GetRoot();
        var results = new List<MethodDefinitionInfo>();

        foreach (var methodDecl in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            var symbol = model.GetDeclaredSymbol(methodDecl);
            if (symbol == null)
                continue;

            var location = methodDecl.GetLocation().GetLineSpan();
            var parameters = new List<string>();

            // Extract parameter types
            foreach (var param in methodDecl.ParameterList.Parameters)
            {
                var paramType = param.Type?.ToString() ?? "object";
                parameters.Add(paramType);
            }

            // Determine access modifier
            var accessModifier = GetAccessModifier(methodDecl.Modifiers);

            var methodDef = new MethodDefinitionInfo(
                methodName: symbol.Name,
                className: symbol.ContainingType?.Name ?? string.Empty,
                namespaceName: symbol.ContainingNamespace?.ToDisplayString() ?? string.Empty,
                fullyQualifiedName: GetFullyQualifiedName(symbol),
                returnType: methodDecl.ReturnType?.ToString() ?? "void",
                parameters: parameters,
                accessModifier: accessModifier,
                isStatic: symbol.IsStatic,
                isVirtual: symbol.IsVirtual,
                isAbstract: symbol.IsAbstract,
                isOverride: symbol.IsOverride,
                filePath: location.Path,
                lineNumber: location.StartLinePosition.Line + 1
            );

            results.Add(methodDef);
        }

        return results;
    }

    /// <summary>
    /// Extract class definitions from the given syntax tree using the provided semantic model.
    /// </summary>
    /// <param name="tree">Syntax tree to scan</param>
    /// <param name="model">Semantic model associated with the tree</param>
    /// <returns>List of class definition information</returns>
    public List<ClassDefinitionInfo> ExtractClassDefinitions(SyntaxTree tree, SemanticModel model)
    {
        if (tree == null) throw new ArgumentNullException(nameof(tree));
        if (model == null) throw new ArgumentNullException(nameof(model));

        var root = tree.GetRoot();
        var results = new List<ClassDefinitionInfo>();

        foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
        {
            var symbol = model.GetDeclaredSymbol(classDecl);
            if (symbol == null)
                continue;

            var location = classDecl.GetLocation().GetLineSpan();
            
            // Extract base class and interfaces
            var baseClass = symbol.BaseType?.ToDisplayString() ?? string.Empty;
            // Don't include System.Object as base class (it's implicit)
            if (baseClass == "object" || baseClass == "System.Object")
                baseClass = string.Empty;
            var interfaces = symbol.Interfaces.Select(i => i.ToDisplayString()).ToList();
            
            // Count members (only user-defined methods, not constructors or property accessors)
            var methodCount = symbol.GetMembers().OfType<IMethodSymbol>()
                .Where(m => m.MethodKind == MethodKind.Ordinary).Count();
            var propertyCount = symbol.GetMembers().OfType<IPropertySymbol>().Count();
            var fieldCount = symbol.GetMembers().OfType<IFieldSymbol>()
                .Where(f => !f.IsImplicitlyDeclared).Count();

            // Determine access modifier
            var accessModifier = GetAccessModifier(classDecl.Modifiers);

            var classDef = new ClassDefinitionInfo(
                className: symbol.Name,
                namespaceName: symbol.ContainingNamespace?.ToDisplayString() ?? string.Empty,
                fullyQualifiedName: GetFullyQualifiedName(symbol),
                accessModifier: accessModifier,
                isStatic: symbol.IsStatic,
                isAbstract: symbol.IsAbstract,
                isSealed: symbol.IsSealed,
                baseClass: baseClass,
                interfaces: interfaces,
                filePath: location.Path,
                lineNumber: location.StartLinePosition.Line + 1,
                methodCount: methodCount,
                propertyCount: propertyCount,
                fieldCount: fieldCount
            );

            results.Add(classDef);
        }

        return results;
    }

    /// <summary>
    /// Extract property definitions from the given syntax tree using the provided semantic model.
    /// </summary>
    /// <param name="tree">Syntax tree to scan</param>
    /// <param name="model">Semantic model associated with the tree</param>
    /// <returns>List of property definition information</returns>
    public List<PropertyDefinitionInfo> ExtractPropertyDefinitions(SyntaxTree tree, SemanticModel model)
    {
        if (tree == null) throw new ArgumentNullException(nameof(tree));
        if (model == null) throw new ArgumentNullException(nameof(model));

        var root = tree.GetRoot();
        var results = new List<PropertyDefinitionInfo>();

        foreach (var propertyDecl in root.DescendantNodes().OfType<PropertyDeclarationSyntax>())
        {
            var symbol = model.GetDeclaredSymbol(propertyDecl);
            if (symbol == null)
                continue;

            var location = propertyDecl.GetLocation().GetLineSpan();

            // Determine access modifier
            var accessModifier = GetAccessModifier(propertyDecl.Modifiers);

            // Check for getter and setter
            var hasGetter = propertyDecl.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.GetAccessorDeclaration)) ?? false;
            var hasSetter = propertyDecl.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.SetAccessorDeclaration)) ?? false;
            
            // Check if auto-property (no accessor body)
            var isAutoProperty = propertyDecl.AccessorList?.Accessors.All(a => a.Body == null && a.ExpressionBody == null) ?? false;

            var propertyDef = new PropertyDefinitionInfo(
                propertyName: symbol.Name,
                className: symbol.ContainingType?.Name ?? string.Empty,
                namespaceName: symbol.ContainingNamespace?.ToDisplayString() ?? string.Empty,
                fullyQualifiedName: GetFullyQualifiedName(symbol),
                propertyType: propertyDecl.Type?.ToString() ?? "object",
                accessModifier: accessModifier,
                isStatic: symbol.IsStatic,
                isVirtual: symbol.IsVirtual,
                isAbstract: symbol.IsAbstract,
                isOverride: symbol.IsOverride,
                hasGetter: hasGetter,
                hasSetter: hasSetter,
                isAutoProperty: isAutoProperty,
                filePath: location.Path,
                lineNumber: location.StartLinePosition.Line + 1
            );

            results.Add(propertyDef);
        }

        return results;
    }

    /// <summary>
    /// Extract field definitions from the given syntax tree using the provided semantic model.
    /// </summary>
    /// <param name="tree">Syntax tree to scan</param>
    /// <param name="model">Semantic model associated with the tree</param>
    /// <returns>List of field definition information</returns>
    public List<FieldDefinitionInfo> ExtractFieldDefinitions(SyntaxTree tree, SemanticModel model)
    {
        if (tree == null) throw new ArgumentNullException(nameof(tree));
        if (model == null) throw new ArgumentNullException(nameof(model));

        var root = tree.GetRoot();
        var results = new List<FieldDefinitionInfo>();

        foreach (var fieldDecl in root.DescendantNodes().OfType<FieldDeclarationSyntax>())
        {
            var location = fieldDecl.GetLocation().GetLineSpan();
            
            // Determine access modifier
            var accessModifier = GetAccessModifier(fieldDecl.Modifiers);

            // Check modifiers
            var isStatic = fieldDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword));
            var isReadOnly = fieldDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.ReadOnlyKeyword));
            var isConst = fieldDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.ConstKeyword));
            var isVolatile = fieldDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.VolatileKeyword));

            // Process each variable in the field declaration (e.g., "int x, y, z;")
            foreach (var variable in fieldDecl.Declaration?.Variables ?? Enumerable.Empty<VariableDeclaratorSyntax>())
            {
                var symbol = model.GetDeclaredSymbol(variable);
                if (symbol == null)
                    continue;

                var fieldDef = new FieldDefinitionInfo(
                    fieldName: symbol.Name,
                    className: symbol.ContainingType?.Name ?? string.Empty,
                    namespaceName: symbol.ContainingNamespace?.ToDisplayString() ?? string.Empty,
                    fullyQualifiedName: GetFullyQualifiedName(symbol),
                    fieldType: fieldDecl.Declaration?.Type?.ToString() ?? "object",
                    accessModifier: accessModifier,
                    isStatic: isStatic,
                    isReadOnly: isReadOnly,
                    isConst: isConst,
                    isVolatile: isVolatile,
                    filePath: location.Path,
                    lineNumber: location.StartLinePosition.Line + 1
                );

                results.Add(fieldDef);
            }
        }

        return results;
    }

    /// <summary>
    /// Extract enum definitions from the given syntax tree using the provided semantic model.
    /// </summary>
    /// <param name="tree">Syntax tree to scan</param>
    /// <param name="model">Semantic model associated with the tree</param>
    /// <returns>List of enum definition information</returns>
    public List<EnumDefinitionInfo> ExtractEnumDefinitions(SyntaxTree tree, SemanticModel model)
    {
        if (tree == null) throw new ArgumentNullException(nameof(tree));
        if (model == null) throw new ArgumentNullException(nameof(model));

        var root = tree.GetRoot();
        var results = new List<EnumDefinitionInfo>();

        foreach (var enumDecl in root.DescendantNodes().OfType<EnumDeclarationSyntax>())
        {
            var symbol = model.GetDeclaredSymbol(enumDecl);
            if (symbol == null)
                continue;

            var location = enumDecl.GetLocation().GetLineSpan();

            // Determine access modifier
            var accessModifier = GetAccessModifier(enumDecl.Modifiers);

            // Get underlying type
            var underlyingType = symbol.EnumUnderlyingType?.ToDisplayString() ?? "int";

            // Extract enum values
            var enumValues = new List<EnumValueInfo>();
            foreach (var member in enumDecl.Members)
            {
                var memberSymbol = model.GetDeclaredSymbol(member);
                if (memberSymbol == null)
                    continue;

                var memberLocation = member.GetLocation().GetLineSpan();
                var value = memberSymbol.ConstantValue; // Can be null if not explicitly set

                var enumValue = new EnumValueInfo(
                    valueName: memberSymbol.Name,
                    value: value,
                    lineNumber: memberLocation.StartLinePosition.Line + 1
                );

                enumValues.Add(enumValue);
            }

            var enumDef = new EnumDefinitionInfo(
                enumName: symbol.Name,
                namespaceName: symbol.ContainingNamespace?.ToDisplayString() ?? string.Empty,
                fullyQualifiedName: GetFullyQualifiedName(symbol),
                accessModifier: accessModifier,
                underlyingType: underlyingType,
                values: enumValues,
                filePath: location.Path,
                lineNumber: location.StartLinePosition.Line + 1
            );

            results.Add(enumDef);
        }

        return results;
    }

    /// <summary>
    /// Extract interface definitions from the given syntax tree using the provided semantic model.
    /// </summary>
    /// <param name="tree">Syntax tree to scan</param>
    /// <param name="model">Semantic model associated with the tree</param>
    /// <returns>List of interface definition information</returns>
    public List<InterfaceDefinitionInfo> ExtractInterfaceDefinitions(SyntaxTree tree, SemanticModel model)
    {
        if (tree == null) throw new ArgumentNullException(nameof(tree));
        if (model == null) throw new ArgumentNullException(nameof(model));

        var root = tree.GetRoot();
        var results = new List<InterfaceDefinitionInfo>();

        foreach (var interfaceDecl in root.DescendantNodes().OfType<InterfaceDeclarationSyntax>())
        {
            var symbol = model.GetDeclaredSymbol(interfaceDecl);
            if (symbol == null)
                continue;

            var location = interfaceDecl.GetLocation().GetLineSpan();

            // Determine access modifier
            var accessModifier = GetAccessModifier(interfaceDecl.Modifiers);

            // Get base interfaces
            var baseInterfaces = new List<string>();
            if (interfaceDecl.BaseList != null)
            {
                foreach (var baseType in interfaceDecl.BaseList.Types)
                {
                    var baseTypeSymbol = model.GetSymbolInfo(baseType.Type).Symbol as INamedTypeSymbol;
                    if (baseTypeSymbol != null)
                    {
                        baseInterfaces.Add(GetFullyQualifiedName(baseTypeSymbol));
                    }
                }
            }

            // Count methods and properties
            int methodCount = 0;
            int propertyCount = 0;

            foreach (var member in interfaceDecl.Members)
            {
                if (member is MethodDeclarationSyntax)
                {
                    methodCount++;
                }
                else if (member is PropertyDeclarationSyntax)
                {
                    propertyCount++;
                }
            }

            var interfaceDef = new InterfaceDefinitionInfo(
                interfaceName: symbol.Name,
                namespaceName: symbol.ContainingNamespace?.ToDisplayString() ?? string.Empty,
                fullyQualifiedName: GetFullyQualifiedName(symbol),
                accessModifier: accessModifier,
                baseInterfaces: baseInterfaces,
                methodCount: methodCount,
                propertyCount: propertyCount,
                filePath: location.Path,
                lineNumber: location.StartLinePosition.Line + 1
            );

            results.Add(interfaceDef);
        }

        return results;
    }

    /// <summary>
    /// Extract struct definitions from the given syntax tree using the provided semantic model.
    /// </summary>
    /// <param name="tree">Syntax tree to scan</param>
    /// <param name="model">Semantic model associated with the tree</param>
    /// <returns>List of struct definition information</returns>
    public List<StructDefinitionInfo> ExtractStructDefinitions(SyntaxTree tree, SemanticModel model)
    {
        if (tree == null) throw new ArgumentNullException(nameof(tree));
        if (model == null) throw new ArgumentNullException(nameof(model));

        var root = tree.GetRoot();
        var results = new List<StructDefinitionInfo>();

        foreach (var structDecl in root.DescendantNodes().OfType<StructDeclarationSyntax>())
        {
            var symbol = model.GetDeclaredSymbol(structDecl);
            if (symbol == null)
                continue;

            var location = structDecl.GetLocation().GetLineSpan();

            // Determine access modifier
            var accessModifier = GetAccessModifier(structDecl.Modifiers);

            // Check for readonly and ref modifiers
            bool isReadOnly = structDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.ReadOnlyKeyword));
            bool isRef = structDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.RefKeyword));

            // Get implemented interfaces
            var interfaces = new List<string>();
            if (structDecl.BaseList != null)
            {
                foreach (var baseType in structDecl.BaseList.Types)
                {
                    var baseTypeSymbol = model.GetSymbolInfo(baseType.Type).Symbol as INamedTypeSymbol;
                    if (baseTypeSymbol != null)
                    {
                        interfaces.Add(GetFullyQualifiedName(baseTypeSymbol));
                    }
                }
            }

            // Count methods, properties, and fields
            int methodCount = 0;
            int propertyCount = 0;
            int fieldCount = 0;

            foreach (var member in structDecl.Members)
            {
                if (member is MethodDeclarationSyntax)
                {
                    methodCount++;
                }
                else if (member is PropertyDeclarationSyntax)
                {
                    propertyCount++;
                }
                else if (member is FieldDeclarationSyntax)
                {
                    var fieldDecl = (FieldDeclarationSyntax)member;
                    fieldCount += fieldDecl.Declaration?.Variables.Count ?? 0;
                }
            }

            var structDef = new StructDefinitionInfo(
                structName: symbol.Name,
                namespaceName: symbol.ContainingNamespace?.ToDisplayString() ?? string.Empty,
                fullyQualifiedName: GetFullyQualifiedName(symbol),
                accessModifier: accessModifier,
                isReadOnly: isReadOnly,
                isRef: isRef,
                interfaces: interfaces,
                methodCount: methodCount,
                propertyCount: propertyCount,
                fieldCount: fieldCount,
                filePath: location.Path,
                lineNumber: location.StartLinePosition.Line + 1
            );

            results.Add(structDef);
        }

        return results;
    }

    /// <summary>
    /// Generate fully qualified name for a symbol as Namespace.Class.Method
    /// </summary>
    /// <param name="symbol">Symbol to format</param>
    /// <returns>Fully qualified name</returns>
    public string GetFullyQualifiedName(ISymbol symbol)
    {
        if (symbol == null) return string.Empty;

        var parts = new List<string>();
        parts.Add(symbol.Name);

        if (symbol.ContainingType != null)
            parts.Insert(0, symbol.ContainingType.Name);

        var ns = symbol.ContainingNamespace;
        if (ns != null && !ns.IsGlobalNamespace)
            parts.Insert(0, ns.ToDisplayString());

        return string.Join('.', parts);
    }

    /// <summary>
    /// Extract access modifier from method declaration syntax modifiers.
    /// </summary>
    /// <param name="modifiers">Syntax token list containing modifiers</param>
    /// <returns>Access modifier string (public, private, protected, internal, or private)</returns>
    private string GetAccessModifier(SyntaxTokenList modifiers)
    {
        if (modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)))
            return "public";
        if (modifiers.Any(m => m.IsKind(SyntaxKind.ProtectedKeyword)))
            return "protected";
        if (modifiers.Any(m => m.IsKind(SyntaxKind.InternalKeyword)))
            return "internal";
        if (modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword)))
            return "private";
        
        // Default to private if no access modifier is specified
        return "private";
    }

    /// <summary>
    /// Extract method call relationships from a syntax tree using the provided semantic model.
    /// </summary>
    public List<MethodCallInfo> ExtractMethodCalls(SyntaxTree tree, SemanticModel model)
    {
        if (tree == null) throw new ArgumentNullException(nameof(tree));
        if (model == null) throw new ArgumentNullException(nameof(model));

        var root = tree.GetRoot();
        var results = new List<MethodCallInfo>();

        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var callerSymbol = GetContainingMethodSymbol(model, invocation);
            if (callerSymbol == null)
                continue; // skip invocations outside methods

            var symbolInfo = model.GetSymbolInfo(invocation);
            IMethodSymbol? calleeSymbol = symbolInfo.Symbol as IMethodSymbol;
            if (calleeSymbol == null && symbolInfo.CandidateSymbols.Length > 0)
            {
                calleeSymbol = symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();
            }
            if (calleeSymbol == null)
                continue; // unresolved; skip

            // Normalize extension method to the defining static method
            if (calleeSymbol.ReducedFrom is IMethodSymbol reduced)
            {
                calleeSymbol = reduced;
            }

            // Normalize inheritance/interface dispatch targets per 1.5
            calleeSymbol = NormalizeCalleeSymbol(calleeSymbol);

            var location = invocation.GetLocation().GetLineSpan();

            var call = new MethodCallInfo
            {
                Caller = GetFullyQualifiedName(callerSymbol),
                Callee = GetFullyQualifiedName(calleeSymbol),
                CallerClass = callerSymbol.ContainingType?.Name ?? string.Empty,
                CalleeClass = calleeSymbol.ContainingType?.Name ?? string.Empty,
                CallerNamespace = callerSymbol.ContainingNamespace?.ToDisplayString() ?? string.Empty,
                CalleeNamespace = calleeSymbol.ContainingNamespace?.ToDisplayString() ?? string.Empty,
                FilePath = location.Path,
                LineNumber = location.StartLinePosition.Line + 1
            };

            results.Add(call);
        }

        // Process attribute constructor calls and initializer calls if enabled
        if (_options.AttributeInitializerCalls)
        {
            // Process attribute constructor calls
            var attributeCalls = ExtractAttributeConstructorCalls(tree, model);
            results.AddRange(attributeCalls);
            
            // Process field/property initializer calls  
            var initializerCalls = ExtractInitializerCalls(tree, model);
            results.AddRange(initializerCalls);
        }

        return results;
    }

    /// <summary>
    /// Find the IMethodSymbol that contains the given syntax node.
    /// </summary>
    public IMethodSymbol? GetContainingMethodSymbol(SemanticModel model, SyntaxNode node)
    {
        var methodDecl = node.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (methodDecl == null) return null;
        return model.GetDeclaredSymbol(methodDecl) as IMethodSymbol;
    }

    private IMethodSymbol NormalizeCalleeSymbol(IMethodSymbol callee)
    {
        // Explicit interface implementation: map to the interface method
        if (callee.ExplicitInterfaceImplementations != null && callee.ExplicitInterfaceImplementations.Length > 0)
        {
            return callee.ExplicitInterfaceImplementations[0];
        }

        // Interface calls: already an interface symbol; keep as-is
        if (callee.ContainingType?.TypeKind == TypeKind.Interface)
        {
            return callee;
        }

        // Overrides/virtual: normalize to the top-most overridden base method
        var current = callee;
        while (current.OverriddenMethod != null)
        {
            current = current.OverriddenMethod;
        }
        return current;
    }

    /// <summary>
    /// Extract method calls from attribute constructor invocations.
    /// </summary>
    private List<MethodCallInfo> ExtractAttributeConstructorCalls(SyntaxTree tree, SemanticModel model)
    {
        var results = new List<MethodCallInfo>();
        var root = tree.GetRoot();

        foreach (var attributeList in root.DescendantNodes().OfType<AttributeListSyntax>())
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var symbolInfo = model.GetSymbolInfo(attribute);
                var constructorSymbol = symbolInfo.Symbol as IMethodSymbol;
                if (constructorSymbol == null && symbolInfo.CandidateSymbols.Length > 0)
                {
                    constructorSymbol = symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();
                }
                if (constructorSymbol == null)
                    continue; // unresolved; skip

                // Determine the caller (containing member or type)
                var callerSymbol = GetContainingMemberOrTypeSymbol(model, attribute);
                if (callerSymbol == null)
                    continue;

                var location = attribute.GetLocation().GetLineSpan();

                var call = new MethodCallInfo
                {
                    Caller = GetFullyQualifiedName(callerSymbol),
                    Callee = GetFullyQualifiedName(constructorSymbol),
                    CallerClass = callerSymbol.ContainingType?.Name ?? string.Empty,
                    CalleeClass = constructorSymbol.ContainingType?.Name ?? string.Empty,
                    CallerNamespace = callerSymbol.ContainingNamespace?.ToDisplayString() ?? string.Empty,
                    CalleeNamespace = constructorSymbol.ContainingNamespace?.ToDisplayString() ?? string.Empty,
                    FilePath = location.Path,
                    LineNumber = location.StartLinePosition.Line + 1
                };

                results.Add(call);
            }
        }

        return results;
    }

    /// <summary>
    /// Extract method calls from field and property initializers.
    /// </summary>
    private List<MethodCallInfo> ExtractInitializerCalls(SyntaxTree tree, SemanticModel model)
    {
        var results = new List<MethodCallInfo>();
        var root = tree.GetRoot();

        // Process field initializers
        foreach (var field in root.DescendantNodes().OfType<FieldDeclarationSyntax>())
        {
            if (field.Declaration?.Variables != null)
            {
                foreach (var variable in field.Declaration.Variables)
                {
                    if (variable.Initializer?.Value != null)
                    {
                        var initializerCalls = ExtractMethodCallsFromExpression(variable.Initializer.Value, model, field.Declaration.Type);
                        results.AddRange(initializerCalls);
                    }
                }
            }
        }

        // Process property initializers
        foreach (var property in root.DescendantNodes().OfType<PropertyDeclarationSyntax>())
        {
            if (property.Initializer?.Value != null)
            {
                var initializerCalls = ExtractMethodCallsFromExpression(property.Initializer.Value, model, property.Type);
                results.AddRange(initializerCalls);
            }
        }

        return results;
    }

    /// <summary>
    /// Extract method calls from an expression (used for initializers).
    /// </summary>
    private List<MethodCallInfo> ExtractMethodCallsFromExpression(ExpressionSyntax expression, SemanticModel model, TypeSyntax containingType)
    {
        var results = new List<MethodCallInfo>();

        foreach (var invocation in expression.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
        {
            var symbolInfo = model.GetSymbolInfo(invocation);
            var calleeSymbol = symbolInfo.Symbol as IMethodSymbol;
            if (calleeSymbol == null && symbolInfo.CandidateSymbols.Length > 0)
            {
                calleeSymbol = symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();
            }
            if (calleeSymbol == null)
                continue; // unresolved; skip

            // Normalize extension method to the defining static method
            if (calleeSymbol.ReducedFrom is IMethodSymbol reduced)
            {
                calleeSymbol = reduced;
            }

            // Normalize inheritance/interface dispatch targets per 1.5
            calleeSymbol = NormalizeCalleeSymbol(calleeSymbol);

            // For initializers, the caller is the containing type
            var callerSymbol = GetContainingTypeSymbol(model, containingType);
            if (callerSymbol == null)
                continue;

            var location = invocation.GetLocation().GetLineSpan();

            var call = new MethodCallInfo
            {
                Caller = GetFullyQualifiedName(callerSymbol),
                Callee = GetFullyQualifiedName(calleeSymbol),
                CallerClass = callerSymbol.Name,
                CalleeClass = calleeSymbol.ContainingType?.Name ?? string.Empty,
                CallerNamespace = callerSymbol.ContainingNamespace?.ToDisplayString() ?? string.Empty,
                CalleeNamespace = calleeSymbol.ContainingNamespace?.ToDisplayString() ?? string.Empty,
                FilePath = location.Path,
                LineNumber = location.StartLinePosition.Line + 1
            };

            results.Add(call);
        }

        return results;
    }

    /// <summary>
    /// Find the containing member (method, property, field) or type symbol for an attribute.
    /// </summary>
    private ISymbol? GetContainingMemberOrTypeSymbol(SemanticModel model, AttributeSyntax attribute)
    {
        // Walk up the syntax tree to find the containing declaration
        var containingNode = attribute.Ancestors().FirstOrDefault(node => 
            node is MethodDeclarationSyntax || 
            node is PropertyDeclarationSyntax || 
            node is FieldDeclarationSyntax || 
            node is ClassDeclarationSyntax || 
            node is StructDeclarationSyntax ||
            node is InterfaceDeclarationSyntax);

        if (containingNode == null)
            return null;

        return model.GetDeclaredSymbol(containingNode);
    }

    /// <summary>
    /// Find the containing type symbol for a type syntax.
    /// </summary>
    private INamedTypeSymbol? GetContainingTypeSymbol(SemanticModel model, TypeSyntax typeSyntax)
    {
        // Walk up the syntax tree to find the containing type declaration
        var containingNode = typeSyntax.Ancestors().FirstOrDefault(node => 
            node is ClassDeclarationSyntax || 
            node is StructDeclarationSyntax ||
            node is InterfaceDeclarationSyntax);

        if (containingNode == null)
            return null;

        return model.GetDeclaredSymbol(containingNode) as INamedTypeSymbol;
    }

    private async Task StoreMethodCallAsync(MethodCallInfo call)
    {
        if (_vectorStore == null)
            return;

        // Validate and normalize metadata before storing
        var validationResult = ValidateAndNormalizeMetadata(call);
        if (!validationResult.IsValid)
        {
            throw new InvalidOperationException($"Invalid method call metadata: {string.Join(", ", validationResult.Errors)}");
        }

        var content = $"Method {validationResult.NormalizedCall.Caller} in class {validationResult.NormalizedCall.CallerClass} calls method {validationResult.NormalizedCall.Callee} in class {validationResult.NormalizedCall.CalleeClass}. This call happens in file {validationResult.NormalizedCall.FilePath} at line {validationResult.NormalizedCall.LineNumber}.";

        var metadata = new Dictionary<string, object>
        {
            ["type"] = "method_call",
            ["caller"] = validationResult.NormalizedCall.Caller,
            ["callee"] = validationResult.NormalizedCall.Callee,
            ["caller_class"] = validationResult.NormalizedCall.CallerClass,
            ["callee_class"] = validationResult.NormalizedCall.CalleeClass,
            ["caller_namespace"] = validationResult.NormalizedCall.CallerNamespace,
            ["callee_namespace"] = validationResult.NormalizedCall.CalleeNamespace,
            ["file_path"] = validationResult.NormalizedCall.FilePath,
            ["line_number"] = validationResult.NormalizedCall.LineNumber
        };

        await _vectorStore.AddTextAsync(content, metadata).ConfigureAwait(false);
    }

    private async Task StoreMethodDefinitionAsync(MethodDefinitionInfo methodDef)
    {
        if (_vectorStore == null)
            return;

        var parametersStr = string.Join(", ", methodDef.Parameters);
        var staticStr = methodDef.IsStatic ? "static " : "";
        var virtualStr = methodDef.IsVirtual ? "virtual " : "";
        var abstractStr = methodDef.IsAbstract ? "abstract " : "";
        var overrideStr = methodDef.IsOverride ? "override " : "";

        var content = $"Method {methodDef.MethodName} in class {methodDef.ClassName} defined in namespace {methodDef.Namespace}. " +
                     $"This method returns {methodDef.ReturnType} and is defined in file {methodDef.FilePath} at line {methodDef.LineNumber}. " +
                     $"Access modifier: {methodDef.AccessModifier}, Parameters: ({parametersStr}), " +
                     $"Modifiers: {staticStr}{virtualStr}{abstractStr}{overrideStr}".TrimEnd();

        var metadata = new Dictionary<string, object>
        {
            ["type"] = "method_definition",
            ["method"] = methodDef.FullyQualifiedName,
            ["method_name"] = methodDef.MethodName,
            ["class"] = methodDef.ClassName,
            ["namespace"] = methodDef.Namespace,
            ["return_type"] = methodDef.ReturnType,
            ["parameters"] = parametersStr,
            ["access_modifier"] = methodDef.AccessModifier,
            ["is_static"] = methodDef.IsStatic,
            ["is_virtual"] = methodDef.IsVirtual,
            ["is_abstract"] = methodDef.IsAbstract,
            ["is_override"] = methodDef.IsOverride,
            ["file_path"] = methodDef.FilePath,
            ["line_number"] = methodDef.LineNumber
        };

        await _vectorStore.AddTextAsync(content, metadata).ConfigureAwait(false);
    }

    private async Task StoreClassDefinitionAsync(ClassDefinitionInfo classDef)
    {
        if (_vectorStore == null)
            return;

        var content = $"Class {classDef.ClassName} defined in namespace {classDef.Namespace}. " +
                     $"This is a {classDef.AccessModifier} class with {classDef.MethodCount} methods, " +
                     $"{classDef.PropertyCount} properties, and {classDef.FieldCount} fields. " +
                     $"Defined in file {classDef.FilePath} at line {classDef.LineNumber}.";

        var metadata = new Dictionary<string, object>
        {
            ["type"] = "class_definition",
            ["class"] = classDef.FullyQualifiedName,
            ["class_name"] = classDef.ClassName,
            ["namespace"] = classDef.Namespace,
            ["access_modifier"] = classDef.AccessModifier,
            ["is_static"] = classDef.IsStatic,
            ["is_abstract"] = classDef.IsAbstract,
            ["is_sealed"] = classDef.IsSealed,
            ["base_class"] = classDef.BaseClass,
            ["interfaces"] = string.Join(", ", classDef.Interfaces),
            ["method_count"] = classDef.MethodCount,
            ["property_count"] = classDef.PropertyCount,
            ["field_count"] = classDef.FieldCount,
            ["file_path"] = classDef.FilePath,
            ["line_number"] = classDef.LineNumber
        };

        await _vectorStore.AddTextAsync(content, metadata).ConfigureAwait(false);
    }

    private async Task StorePropertyDefinitionAsync(PropertyDefinitionInfo propertyDef)
    {
        if (_vectorStore == null)
            return;

        var accessors = "";
        if (propertyDef.HasGetter && propertyDef.HasSetter)
            accessors = "get and set";
        else if (propertyDef.HasGetter)
            accessors = "get";
        else if (propertyDef.HasSetter)
            accessors = "set";
        else
            accessors = "none";

        var modifiers = new List<string>();
        if (propertyDef.IsStatic) modifiers.Add("static");
        if (propertyDef.IsVirtual) modifiers.Add("virtual");
        if (propertyDef.IsAbstract) modifiers.Add("abstract");
        if (propertyDef.IsOverride) modifiers.Add("override");
        var modifiersStr = modifiers.Count > 0 ? string.Join(", ", modifiers) : "none";

        var content = $"Property {propertyDef.PropertyName} in class {propertyDef.ClassName} defined in namespace {propertyDef.Namespace}. " +
                     $"This is a {propertyDef.AccessModifier} property of type {propertyDef.PropertyType} with {accessors} accessor(s). " +
                     $"Modifiers: {modifiersStr}. " +
                     $"Defined in file {propertyDef.FilePath} at line {propertyDef.LineNumber}.";

        var metadata = new Dictionary<string, object>
        {
            ["type"] = "property_definition",
            ["property"] = propertyDef.FullyQualifiedName,
            ["property_name"] = propertyDef.PropertyName,
            ["class"] = propertyDef.ClassName,
            ["namespace"] = propertyDef.Namespace,
            ["property_type"] = propertyDef.PropertyType,
            ["access_modifier"] = propertyDef.AccessModifier,
            ["is_static"] = propertyDef.IsStatic,
            ["is_virtual"] = propertyDef.IsVirtual,
            ["is_abstract"] = propertyDef.IsAbstract,
            ["is_override"] = propertyDef.IsOverride,
            ["has_getter"] = propertyDef.HasGetter,
            ["has_setter"] = propertyDef.HasSetter,
            ["is_auto_property"] = propertyDef.IsAutoProperty,
            ["file_path"] = propertyDef.FilePath,
            ["line_number"] = propertyDef.LineNumber
        };

        await _vectorStore.AddTextAsync(content, metadata).ConfigureAwait(false);
    }

    private async Task StoreFieldDefinitionAsync(FieldDefinitionInfo fieldDef)
    {
        if (_vectorStore == null)
            return;

        var modifiers = new List<string>();
        if (fieldDef.IsStatic) modifiers.Add("static");
        if (fieldDef.IsReadOnly) modifiers.Add("readonly");
        if (fieldDef.IsConst) modifiers.Add("const");
        if (fieldDef.IsVolatile) modifiers.Add("volatile");
        var modifiersStr = modifiers.Count > 0 ? string.Join(", ", modifiers) : "none";

        var content = $"Field {fieldDef.FieldName} in class {fieldDef.ClassName} defined in namespace {fieldDef.Namespace}. " +
                     $"This is a {fieldDef.AccessModifier} field of type {fieldDef.FieldType}. " +
                     $"Modifiers: {modifiersStr}. " +
                     $"Defined in file {fieldDef.FilePath} at line {fieldDef.LineNumber}.";

        var metadata = new Dictionary<string, object>
        {
            ["type"] = "field_definition",
            ["field"] = fieldDef.FullyQualifiedName,
            ["field_name"] = fieldDef.FieldName,
            ["class"] = fieldDef.ClassName,
            ["namespace"] = fieldDef.Namespace,
            ["field_type"] = fieldDef.FieldType,
            ["access_modifier"] = fieldDef.AccessModifier,
            ["is_static"] = fieldDef.IsStatic,
            ["is_readonly"] = fieldDef.IsReadOnly,
            ["is_const"] = fieldDef.IsConst,
            ["is_volatile"] = fieldDef.IsVolatile,
            ["file_path"] = fieldDef.FilePath,
            ["line_number"] = fieldDef.LineNumber
        };

        await _vectorStore.AddTextAsync(content, metadata).ConfigureAwait(false);
    }

    private async Task StoreEnumDefinitionAsync(EnumDefinitionInfo enumDef)
    {
        if (_vectorStore == null)
            return;

        var valuesList = string.Join(", ", enumDef.Values.Select(v => v.ToString()));
        var valueCount = enumDef.Values.Count;

        var content = $"Enum {enumDef.EnumName} defined in namespace {enumDef.Namespace} with underlying type {enumDef.UnderlyingType}. " +
                     $"Contains {valueCount} value(s): {valuesList}. " +
                     $"Defined in file {enumDef.FilePath} at line {enumDef.LineNumber}.";

        var metadata = new Dictionary<string, object>
        {
            ["type"] = "enum_definition",
            ["enum"] = enumDef.FullyQualifiedName,
            ["enum_name"] = enumDef.EnumName,
            ["namespace"] = enumDef.Namespace,
            ["access_modifier"] = enumDef.AccessModifier,
            ["underlying_type"] = enumDef.UnderlyingType,
            ["value_count"] = valueCount,
            ["values"] = valuesList,
            ["file_path"] = enumDef.FilePath,
            ["line_number"] = enumDef.LineNumber
        };

        await _vectorStore.AddTextAsync(content, metadata).ConfigureAwait(false);
    }

    private async Task StoreInterfaceDefinitionAsync(InterfaceDefinitionInfo interfaceDef)
    {
        if (_vectorStore == null)
            return;

        var baseInterfacesList = interfaceDef.BaseInterfaces.Count > 0 
            ? string.Join(", ", interfaceDef.BaseInterfaces) 
            : "none";

        var content = $"Interface {interfaceDef.InterfaceName} defined in namespace {interfaceDef.Namespace}. " +
                     $"Access modifier: {interfaceDef.AccessModifier}. " +
                     $"Base interfaces: {baseInterfacesList}. " +
                     $"Contains {interfaceDef.MethodCount} method(s) and {interfaceDef.PropertyCount} property/properties. " +
                     $"Defined in file {interfaceDef.FilePath} at line {interfaceDef.LineNumber}.";

        var metadata = new Dictionary<string, object>
        {
            ["type"] = "interface_definition",
            ["interface"] = interfaceDef.FullyQualifiedName,
            ["interface_name"] = interfaceDef.InterfaceName,
            ["namespace"] = interfaceDef.Namespace,
            ["access_modifier"] = interfaceDef.AccessModifier,
            ["base_interfaces"] = baseInterfacesList,
            ["method_count"] = interfaceDef.MethodCount,
            ["property_count"] = interfaceDef.PropertyCount,
            ["file_path"] = interfaceDef.FilePath,
            ["line_number"] = interfaceDef.LineNumber
        };

        await _vectorStore.AddTextAsync(content, metadata).ConfigureAwait(false);
    }

    private async Task StoreStructDefinitionAsync(StructDefinitionInfo structDef)
    {
        if (_vectorStore == null)
            return;

        var modifiers = "";
        if (structDef.IsReadOnly) modifiers += "readonly ";
        if (structDef.IsRef) modifiers += "ref ";

        var interfacesList = structDef.Interfaces.Count > 0 
            ? string.Join(", ", structDef.Interfaces) 
            : "none";

        var content = $"Struct {structDef.StructName} defined in namespace {structDef.Namespace}. " +
                     $"Access modifier: {structDef.AccessModifier}. " +
                     $"Modifiers: {(string.IsNullOrWhiteSpace(modifiers) ? "none" : modifiers.Trim())}. " +
                     $"Implemented interfaces: {interfacesList}. " +
                     $"Contains {structDef.MethodCount} method(s), {structDef.PropertyCount} property/properties, and {structDef.FieldCount} field(s). " +
                     $"Defined in file {structDef.FilePath} at line {structDef.LineNumber}.";

        var metadata = new Dictionary<string, object>
        {
            ["type"] = "struct_definition",
            ["struct"] = structDef.FullyQualifiedName,
            ["struct_name"] = structDef.StructName,
            ["namespace"] = structDef.Namespace,
            ["access_modifier"] = structDef.AccessModifier,
            ["is_readonly"] = structDef.IsReadOnly,
            ["is_ref"] = structDef.IsRef,
            ["interfaces"] = interfacesList,
            ["method_count"] = structDef.MethodCount,
            ["property_count"] = structDef.PropertyCount,
            ["field_count"] = structDef.FieldCount,
            ["file_path"] = structDef.FilePath,
            ["line_number"] = structDef.LineNumber
        };

        await _vectorStore.AddTextAsync(content, metadata).ConfigureAwait(false);
    }

    /// <summary>
    /// Validates and normalizes method call metadata according to the required schema.
    /// </summary>
    /// <param name="call">The method call to validate</param>
    /// <returns>Validation result with normalized data or errors</returns>
    public MetadataValidationResult ValidateAndNormalizeMetadata(MethodCallInfo call)
    {
        var errors = new List<string>();
        var normalizedCall = new MethodCallInfo();

        // Required field validation and normalization
        normalizedCall.Caller = NormalizeFullyQualifiedName(call.Caller, "caller", errors);
        normalizedCall.Callee = NormalizeFullyQualifiedName(call.Callee, "callee", errors);
        normalizedCall.CallerClass = NormalizeClassName(call.CallerClass, "caller_class", errors);
        normalizedCall.CalleeClass = NormalizeClassName(call.CalleeClass, "callee_class", errors);
        normalizedCall.CallerNamespace = NormalizeNamespace(call.CallerNamespace, "caller_namespace", errors);
        normalizedCall.CalleeNamespace = NormalizeNamespace(call.CalleeNamespace, "callee_namespace", errors);
        normalizedCall.FilePath = NormalizeFilePath(call.FilePath, "file_path", errors);
        normalizedCall.LineNumber = NormalizeLineNumber(call.LineNumber, "line_number", errors);

        return new MetadataValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            NormalizedCall = normalizedCall
        };
    }

    private string NormalizeFullyQualifiedName(string? value, string fieldName, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"Required field '{fieldName}' is missing or empty");
            return string.Empty;
        }

        var normalized = value.Trim();
        if (string.IsNullOrEmpty(normalized))
        {
            errors.Add($"Required field '{fieldName}' is empty after trimming whitespace");
            return string.Empty;
        }

        return normalized;
    }

    private string NormalizeClassName(string? value, string fieldName, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"Required field '{fieldName}' is missing or empty");
            return string.Empty;
        }

        var normalized = value.Trim();
        if (string.IsNullOrEmpty(normalized))
        {
            errors.Add($"Required field '{fieldName}' is empty after trimming whitespace");
            return string.Empty;
        }

        return normalized;
    }

    private string NormalizeNamespace(string? value, string fieldName, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            // Namespace can be empty for global namespace
            return string.Empty;
        }

        var normalized = value.Trim();
        return normalized;
    }

    private string NormalizeFilePath(string? value, string fieldName, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"Required field '{fieldName}' is missing or empty");
            return string.Empty;
        }

        var normalized = value.Trim();
        if (string.IsNullOrEmpty(normalized))
        {
            errors.Add($"Required field '{fieldName}' is empty after trimming whitespace");
            return string.Empty;
        }

        // Convert to relative path if absolute
        try
        {
            if (Path.IsPathRooted(normalized))
            {
                // For now, keep absolute paths as-is, but could normalize to relative
                // normalized = Path.GetRelativePath(Directory.GetCurrentDirectory(), normalized);
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Invalid file path '{normalized}': {ex.Message}");
            return string.Empty;
        }

        return normalized;
    }

    private int NormalizeLineNumber(int value, string fieldName, List<string> errors)
    {
        if (value < 1)
        {
            errors.Add($"Required field '{fieldName}' must be >= 1, got {value}");
            return 1; // Default to line 1
        }

        return value;
    }
}
