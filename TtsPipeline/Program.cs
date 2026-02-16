using TtsPipeline;
using System.IO;

Console.WriteLine("Sesli Kitap Pipeline Başlatılıyor...");

// API Key İste
Console.Write("Lütfen Google API Key giriniz: ");
string? apiKey = Console.ReadLine();

if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.WriteLine("API Key girilmedi. İşlem iptal edildi.");
    return;
}

// Input klasörü yolu
string inputDir = Path.Combine(Directory.GetCurrentDirectory(), "Input");
string tempDir = Path.Combine(Directory.GetCurrentDirectory(), "Temp");

if (!Directory.Exists(inputDir))
{
    Console.WriteLine($"Hata: '{inputDir}' klasörü bulunamadı.");
    return;
}

if (!Directory.Exists(tempDir))
{
    Directory.CreateDirectory(tempDir);
}

// İlk .txt dosyasını bul
var files = Directory.GetFiles(inputDir, "*.txt");
if (files.Length == 0)
{
    Console.WriteLine("Uyarı: Input klasöründe hiç .txt dosyası yok.");
    return;
}

string filePath = files[0];
string fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);

try
{
    // Metni oku
    string text = await File.ReadAllTextAsync(filePath);

    // Parçala
    var chunks = TextChunker.SplitTextIntoSafeChunks(text);
    Console.WriteLine($"[BİLGİ] '{Path.GetFileName(filePath)}' dosyası okundu ve {chunks.Count} parçaya bölündü.");

    // Gerçek Servisi Başlat
    var geminiService = new GeminiTtsService();

    // Döngü ile işle
    for (int i = 0; i < chunks.Count; i++)
    {
        string chunkText = chunks[i];
        
        // Dosya adı formatı: bolum_01_part_001.wav (WAV veya PCM dönebilir, genelde WAV container olmayabilir ama RAW ses verisi. 
        // SDK'nın döndürdüğü veri formatı Base64 encoded audio bytes. 
        // Genellikle WAV header içermeyebilir, ham PCM olabilir veya WAV olabilir. 
        // Şimdilik .wav uzantısı kullanalım, oynatıcılar bazen header olmasa da açabilir veya format bellidir.
        // Google GenAI audio çıkışı genelde WAV formatındadır.)
        
        string outputFileName = $"{fileNameWithoutExt}_part_{(i + 1):000}.wav";
        string outputPath = Path.Combine(tempDir, outputFileName);
        
        // Prompt loglama (Opsiyonel olarak burada da tutabiliriz ama istenmedi, 
        // ancak debug için iyi olabilir. Şimdilik sadece ses dosyası.)

        Console.Write($"[API İŞLEMİ] Parça {i + 1}/{chunks.Count} gönderiliyor... ");

        bool success = await geminiService.GenerateAndSaveAudioAsync(chunkText, outputPath, apiKey);

        if (success)
        {
            Console.WriteLine($"Tamamlandı. -> {outputFileName}");
        }
        else
        {
            Console.WriteLine("HATA!");
        }
    }

    Console.WriteLine("\nTüm işlemler tamamlandı.");
}
catch (Exception ex)
{
    Console.WriteLine($"Hata oluştu: {ex.Message}");
}
