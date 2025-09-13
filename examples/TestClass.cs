using System;

namespace Example
{
    public class TestClass
    {
        public string Name { get; set; }
        
        public int Count { get; set; }
        
        public TestClass(string name)
        {
            Name = name;
        }
        
        public void DoSomething()
        {
            Console.WriteLine($"Doing something with {Name}");
        }
        
        public string GetFormattedName()
        {
            return $"Name: {Name}";
        }
        
        public void SetCount(int value)
        {
            Count = value;
        }
        
        public bool CreateNewInstance()
        {
            return true;
        }
    }
}