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
        public async Task<bool> GenerateAndSaveAudioAsync(string chunkText, string outputPath, string apiKey, string modelName, string directorPrompt)
        {
            try
            {
                // Client oluştur (Timeout süresini 5 dakikaya çıkar: 300,000 ms)
                var httpOptions = new HttpOptions { Timeout = 300000 };
                using var client = new Client(apiKey: apiKey, httpOptions: httpOptions);

                // Prompt hazırlığı: Eğer directorPrompt varsa ekle, yoksa sadece metni gönder.
                string finalInputText = string.IsNullOrWhiteSpace(directorPrompt) 
                    ? chunkText 
                    : $"{directorPrompt}\n\n{chunkText}";

                // Content nesnesini manuel oluştur (List initialization)
                var content = new Content
                {
                    Role = "user",
                    Parts = new List<Part> { new Part { Text = finalInputText } }
                };

                // Config ayarla
                var config = new GenerateContentConfig
                {
                    Temperature = 1,
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
                    model: modelName, 
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
