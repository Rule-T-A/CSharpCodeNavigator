using System;

namespace Test.Initializers
{
    public class InitializerTest
    {
        private string _name = GetDefaultName(); // Field initializer calling method
        
        private static string GetDefaultName() => "Default";
        
        public string Name { get; set; } = GetDefaultName(); // Property initializer
        
        private int _count = CalculateCount(); // Field initializer calling method
        
        private static int CalculateCount() => 42;
        
        public string Description { get; set; } = CreateDescription("Test"); // Property initializer with parameters
        
        private static string CreateDescription(string prefix) => $"{prefix} Description";
    }
}
