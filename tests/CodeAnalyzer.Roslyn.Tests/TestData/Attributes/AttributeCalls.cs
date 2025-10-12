using System;

namespace Test.Attributes
{
    [Serializable] // Attribute constructor call
    public class TestClass
    {
        [Obsolete("Use NewMethod instead")] // Attribute with constructor parameters
        public void OldMethod() { }
        
        [TestMethod] // Custom attribute
        public void TestMethod() { }
        
        [TestMethodWithParams("test", 42)] // Custom attribute with parameters
        public void TestMethodWithParams() { }
    }
    
    public class TestMethodAttribute : Attribute
    {
        public TestMethodAttribute() { } // Constructor that gets called
    }
    
    public class TestMethodWithParamsAttribute : Attribute
    {
        public TestMethodWithParamsAttribute(string name, int value) { } // Constructor with parameters
    }
}
