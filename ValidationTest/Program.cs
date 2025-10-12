using System;
using CodeAnalyzer.Roslyn;
using CodeAnalyzer.Roslyn.Models;

namespace ValidationTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var analyzer = new RoslynAnalyzer();
            
            // Test 1: Valid data should pass
            var validCall = new MethodCallInfo
            {
                Caller = "Namespace.Class.Method",
                Callee = "OtherNamespace.OtherClass.OtherMethod",
                CallerClass = "Class",
                CalleeClass = "OtherClass",
                CallerNamespace = "Namespace",
                CalleeNamespace = "OtherNamespace",
                FilePath = "C:\\path\\to\\file.cs",
                LineNumber = 42
            };
            
            var result1 = analyzer.ValidateAndNormalizeMetadata(validCall);
            Console.WriteLine($"Test 1 - Valid data: {(result1.IsValid ? "PASS" : "FAIL")}");
            if (!result1.IsValid)
            {
                Console.WriteLine($"  Errors: {string.Join(", ", result1.Errors)}");
            }
            
            // Test 2: Missing caller should fail
            var invalidCall = new MethodCallInfo
            {
                Caller = "", // Missing caller
                Callee = "OtherNamespace.OtherClass.OtherMethod",
                CallerClass = "Class",
                CalleeClass = "OtherClass",
                CallerNamespace = "Namespace",
                CalleeNamespace = "OtherNamespace",
                FilePath = "C:\\path\\to\\file.cs",
                LineNumber = 42
            };
            
            var result2 = analyzer.ValidateAndNormalizeMetadata(invalidCall);
            Console.WriteLine($"Test 2 - Missing caller: {(!result2.IsValid ? "PASS" : "FAIL")}");
            if (result2.IsValid)
            {
                Console.WriteLine("  ERROR: Should have failed but didn't!");
            }
            else
            {
                Console.WriteLine($"  Errors: {string.Join(", ", result2.Errors)}");
            }
            
            // Test 3: Whitespace trimming
            var whitespaceCall = new MethodCallInfo
            {
                Caller = "  Namespace.Class.Method  ",
                Callee = "OtherNamespace.OtherClass.OtherMethod",
                CallerClass = " Class ",
                CalleeClass = "OtherClass",
                CallerNamespace = " Namespace ",
                CalleeNamespace = "OtherNamespace",
                FilePath = "C:\\path\\to\\file.cs",
                LineNumber = 42
            };
            
            var result3 = analyzer.ValidateAndNormalizeMetadata(whitespaceCall);
            Console.WriteLine($"Test 3 - Whitespace trimming: {(result3.IsValid ? "PASS" : "FAIL")}");
            if (result3.IsValid)
            {
                Console.WriteLine($"  Trimmed caller: '{result3.NormalizedCall.Caller}'");
                Console.WriteLine($"  Trimmed caller class: '{result3.NormalizedCall.CallerClass}'");
            }
            else
            {
                Console.WriteLine($"  Errors: {string.Join(", ", result3.Errors)}");
            }
        }
    }
}
