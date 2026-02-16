using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace TtsPipeline
{
    public class MockGeminiTtsService
    {
        private readonly string _directorPrompt = @"
# AUDIO PROFILE: Legendary Narrator
## THE SCENE: High clarity, studio environment. No background echo or reverb.
### DIRECTOR'S NOTES
Voice Profile: Adult male, deep baritone, authoritative and legendary. No audible breath sounds.
Pacing: Above average speed (not fast, but serious). Avoid long dramatic pauses.
#### TRANSCRIPT
{0}";

        public async Task<bool> GenerateAndSaveAudioMockAsync(string chunkText, string outputPath)
        {
            try
            {
                // 1 saniyelik ağ gecikmesi simülasyonu
                await Task.Delay(1000);

                // Promptu oluştur (sırf şablonu kullanmış olmak için, şu an mock içinde işlevsel değil ama yapı hazır)
                string fullPrompt = string.Format(_directorPrompt, chunkText);

                // Promptu log dosyası olarak kaydet (.txt)
                string txtOutputPath = Path.ChangeExtension(outputPath, ".txt");
                await File.WriteAllTextAsync(txtOutputPath, fullPrompt);

                // Sahte ses dosyası içeriği
                string dummyContent = $"Bu bir test ses dosyasidir.\nPrompt Length: {fullPrompt.Length}\nOriginal Text: {chunkText.Substring(0, Math.Min(50, chunkText.Length))}...";
                byte[] dummyBytes = Encoding.UTF8.GetBytes(dummyContent);

                // Dosyayı kaydet
                await File.WriteAllBytesAsync(outputPath, dummyBytes);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MOCK ERROR] Dosya yazma hatası: {ex.Message}");
                return false;
            }
        }
    }
}
