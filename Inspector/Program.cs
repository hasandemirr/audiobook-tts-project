using System;
using System.Reflection;
using Google.GenAI.Types;
using Google.GenAI;

class Program
{
    static void Main()
    {
        var type = typeof(HttpOptions);
        Console.WriteLine($"Type: {type.FullName}");
        foreach (var prop in type.GetProperties())
        {
            Console.WriteLine($"  - {prop.Name} ({prop.PropertyType.Name})");
        }
    }
}
