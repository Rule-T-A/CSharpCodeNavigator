# C# Code Navigator - Proof of Concept

A proof of concept implementation of phases 1-4 of the C# Code Navigator system for AI-powered code exploration through Claude Desktop.

## 🎯 Project Goals

This project implements a sophisticated C# code analysis and navigation tool that:
- **Analyzes C# code** using Roslyn to extract method call relationships
- **Stores relationships** in a vector database for semantic search
- **Finds call paths** between methods (both "what calls this" and "what does this call")
- **Provides console interface** for interactive code exploration
- **Integrates with Claude Desktop** via MCP (Model Context Protocol) - future phases

## Project Structure

```
CSharpCodeNavigator/
├── src/
│   ├── CodeAnalyzer.Roslyn/              # Phase 1: Roslyn Analysis & Indexing
│   │   ├── Models/                       # Data models for analysis results
│   │   └── CodeAnalyzer.Roslyn.csproj
│   │
│   ├── CodeAnalyzer.Navigation/          # Phases 2-3: Call Index & Path Finding
│   │   ├── Models/                       # Data models for navigation
│   │   └── CodeAnalyzer.Navigation.csproj
│   │
│   └── CodeAnalyzer.Console/             # Phase 4: Console Interface
│       └── CodeAnalyzer.Console.csproj
│
├── tests/
│   ├── CodeAnalyzer.Roslyn.Tests/        # Tests for Phase 1
│   └── CodeAnalyzer.Navigation.Tests/    # Tests for Phases 2-3
│
└── CSharpCodeNavigator.sln              # Solution file
```

## Dependencies

- **VectorStore**: Local vector database for storing method call relationships
- **Microsoft.CodeAnalysis.CSharp**: Roslyn compiler platform for C# analysis
- **Microsoft.CodeAnalysis.CSharp.Workspaces**: Workspace support for Roslyn
- **Microsoft.Build.Locator**: MSBuild integration for project loading
- **xUnit**: Testing framework

## Getting Started

1. **Restore packages**:
   ```bash
   dotnet restore
   ```

2. **Build the solution**:
   ```bash
   dotnet build
   ```

3. **Run tests**:
   ```bash
   dotnet test
   ```

4. **Run the console application**:
   ```bash
   dotnet run --project src/CodeAnalyzer.Console
   ```

## Phase Implementation Status

- [ ] **Phase 1**: Roslyn Analysis & Indexing Pipeline (Steps 1.1–1.3 complete)
- [ ] **Phase 2**: Call Index Builder  
- [ ] **Phase 3**: Path Finding Algorithms
- [ ] **Phase 4**: Console Interface

## Next Steps

The project is now set up and ready for implementation. Each phase can be developed independently with proper test coverage.

See `csharp_navigator_arch (1).md` for detailed implementation guidance for each phase.
