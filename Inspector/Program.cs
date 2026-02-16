using System;
using System.Linq;
using System.Threading.Tasks;
using Google.GenAI;

class Program
{
    static async Task Main()
    {
        string apiKey = "PLACEHOLDER"; // The user will need to run this or I can try with a generic call if it allows listing without key, but usually it needs one.
        // Actually, let's just write a code that calls ListModels so the user can see.
        
        Console.WriteLine("Listing available models...");
        try 
        {
            // Enter API Key if you want to run this locally
            Console.Write("Enter API Key: ");
            string key = Console.ReadLine();
            
            using var client = new Client(apiKey: key);
            var models = await client.Models.ListAsync();
            
            foreach (var m in models)
            {
                Console.WriteLine($"Model: {m.Name}");
                Console.WriteLine($"  DisplayName: {m.DisplayName}");
                Console.WriteLine($"  SupportedMethods: {string.Join(", ", m.SupportedGenerationMethods)}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
