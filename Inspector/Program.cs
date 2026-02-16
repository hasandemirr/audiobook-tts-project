using System;
using System.Linq;
using System.Reflection;

class Program
{
    static void Main()
    {
        try
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name.StartsWith("Google.GenAI"));
            if (assembly == null) try { assembly = Assembly.Load("Google.GenAI"); } catch {}

            var type = assembly.GetType("Google.GenAI.Client");
            if (type != null)
            {
                Console.WriteLine($"Type: {type.FullName}");
                foreach (var ctor in type.GetConstructors())
                {
                    Console.WriteLine("Constructor:");
                    foreach (var p in ctor.GetParameters())
                    {
                        Console.WriteLine($"  - {p.Name} ({p.ParameterType.Name})");
                    }
                }
            }
            
            // Also check ApiClient
            type = assembly.GetType("Google.GenAI.ApiClient");
             if (type != null)
            {
                Console.WriteLine($"Type: {type.FullName}");
                 foreach (var ctor in type.GetConstructors())
                {
                    Console.WriteLine("Constructor:");
                    foreach (var p in ctor.GetParameters())
                    {
                        Console.WriteLine($"  - {p.Name} ({p.ParameterType.Name})");
                    }
                }
            }
        }
        catch (Exception ex) { Console.WriteLine(ex); }
    }
}
