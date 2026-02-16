using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Google.GenAI;
using Google.GenAI.Types;

namespace TtsPipeline
{
    public class GeminiTtsService
    {
        private readonly string _directorPrompt = @"Voice Profile: Adult male, deep baritone, authoritative and legendary.
Constraints: No background echo or reverb. No audible breath sounds. High clarity, studio environment.
Pace: above average speed (not fast, but serious). Avoid long dramatic pauses.
{0}";

        public async Task<bool> GenerateAndSaveAudioAsync(string chunkText, string outputPath, string apiKey)
        {
            try
            {
                // Client oluştur
                using var client = new Client(apiKey: apiKey);

                // Prompt hazırla
                string fullPrompt = string.Format(_directorPrompt, chunkText);

                // Content nesnesini manuel oluştur (List initialization)
                var content = new Content
                {
                    Parts = new List<Part> { new Part { Text = fullPrompt } }
                };

                // Config ayarla
                var config = new GenerateContentConfig
                {
                    ResponseModalities = new List<string> { "audio" },
                    SpeechConfig = new SpeechConfig
                    {
                        VoiceConfig = new VoiceConfig
                        {
                            PrebuiltVoiceConfig = new PrebuiltVoiceConfig
                            {
                                VoiceName = "Algieba"
                            }
                        }
                    }
                };

                // Dosya akışını aç (Create modunda)
                using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);

                var responseStream = client.Models.GenerateContentStreamAsync(
                    model: "gemini-2.0-flash", 
                    contents: new List<Content> { content },
                    config: config
                );

                await foreach (var response in responseStream)
                {
                    if (response.Candidates != null && response.Candidates.Count > 0)
                    {
                        foreach (var candidate in response.Candidates)
                        {
                            if (candidate.Content != null && candidate.Content.Parts != null)
                            {
                                foreach (var part in candidate.Content.Parts)
                                {
                                    if (part.InlineData != null && part.InlineData.Data != null)
                                    {
                                        // Byte dizisini dosyaya yaz
                                        var dataBuffer = part.InlineData.Data; 
                                        await fileStream.WriteAsync(dataBuffer, 0, dataBuffer.Length); 
                                    }
                                }
                            }
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API ERROR] Hata: {ex.Message}");
                return false;
            }
        }
    }
}
