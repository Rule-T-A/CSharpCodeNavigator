using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace CodeAnalyzer.Roslyn.Tests
{
    public class RoslynAnalyzerInheritanceTests
    {
        [Fact]
        public async Task AbstractBase_DerivedOverride_CallRecordedOnAbstractBase()
        {
            var analyzer = new RoslynAnalyzer();
            var file = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData", "Inheritance", "AbstractOverride.cs");
            file = Path.GetFullPath(file);
            Assert.True(File.Exists(file));

            var compilation = await analyzer.CreateCompilationFromFilesAsync(file);
            var calls = new List<CodeAnalyzer.Roslyn.Models.MethodCallInfo>();
            foreach (var t in compilation.SyntaxTrees)
                calls.AddRange(analyzer.ExtractMethodCalls(t, compilation.GetSemanticModel(t)));

            Assert.Contains(calls, c => c.Caller.EndsWith("Inheritance.AbstractUse.Run") && c.Callee.EndsWith("Inheritance.AbstractBase.Do"));
        }

        [Fact]
        public async Task ExplicitInterfaceImplementation_CallRecordedOnInterfaceMethod()
        {
            var analyzer = new RoslynAnalyzer();
            var dir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData", "ExplicitInterface");
            dir = Path.GetFullPath(dir);
            var files = new[]
            {
                Path.Combine(dir, "IThing.cs"),
                Path.Combine(dir, "ThingImpl.cs"),
                Path.Combine(dir, "UseExplicit.cs")
            };
            foreach (var f in files) Assert.True(File.Exists(f), $"Missing: {f}");

            var compilation = await analyzer.CreateCompilationFromFilesAsync(files);
            var calls = new List<CodeAnalyzer.Roslyn.Models.MethodCallInfo>();
            foreach (var t in compilation.SyntaxTrees)
                calls.AddRange(analyzer.ExtractMethodCalls(t, compilation.GetSemanticModel(t)));

            Assert.Contains(calls, c => c.Caller.EndsWith("Explicit.Use.Run") && c.Callee.EndsWith("Explicit.Contract.IThing.Do"));
        }

        [Fact]
        public async Task BaseQualifier_Vs_ThisQualifier_BothResolveAndRecordOnBase()
        {
            var analyzer = new RoslynAnalyzer();
            var file = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData", "BaseQualifier", "BaseVsThis.cs");
            file = Path.GetFullPath(file);
            Assert.True(File.Exists(file));

            var compilation = await analyzer.CreateCompilationFromFilesAsync(file);
            var calls = new List<CodeAnalyzer.Roslyn.Models.MethodCallInfo>();
            foreach (var t in compilation.SyntaxTrees)
                calls.AddRange(analyzer.ExtractMethodCalls(t, compilation.GetSemanticModel(t)));

            // base.Foo()
            Assert.Contains(calls, c => c.Caller.EndsWith("BaseQualifier.Derived.DoBoth") && c.Callee.EndsWith("BaseQualifier.Base.Foo"));
            // this.Foo() also normalized to base symbol per policy
            Assert.Contains(calls, c => c.Caller.EndsWith("BaseQualifier.Derived.DoBoth") && c.Callee.EndsWith("BaseQualifier.Base.Foo"));
        }
    }
}


