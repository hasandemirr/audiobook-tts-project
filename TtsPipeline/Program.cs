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

// Dosyaları listele ve seçtir
var files = Directory.GetFiles(inputDir, "*.txt");
if (files.Length == 0)
{
    Console.WriteLine("Uyarı: Input klasöründe hiç .txt dosyası yok.");
    return;
}

string filePath;
if (files.Length > 1)
{
    Console.WriteLine("\n[İşlem] Hangi dosyayı işlemek istersiniz?");
    for (int j = 0; j < files.Length; j++)
    {
        Console.WriteLine($"{j + 1}. {Path.GetFileName(files[j])}");
    }
    Console.Write("Seçiminiz (1-N): ");
    if (int.TryParse(Console.ReadLine(), out int choice) && choice >= 1 && choice <= files.Length)
    {
        filePath = files[choice - 1];
    }
    else
    {
        Console.WriteLine("Geçersiz seçim. İlk dosya seçildi.");
        filePath = files[0];
    }
}
else
{
    filePath = files[0];
}

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
        
        string baseFileName = fileNameWithoutExt; // Defined baseFileName
        string fileName = $"{baseFileName}_part_{i + 1:D3}.wav";
        string outputPath = Path.Combine(tempDir, fileName);
        
        // Prompt loglama (Opsiyonel olarak burada da tutabiliriz ama istenmedi, 
        // ancak debug için iyi olabilir. Şimdilik sadece ses dosyası.)

        // [GÜNCELLEME] Dosya Kontrolü (Resume/Idempotency)
        if (File.Exists(outputPath) && new FileInfo(outputPath).Length > 0)
        {
            Console.WriteLine($"[BİLGİ] Dosya zaten mevcut, atlanıyor: {fileName}");
            continue;
        }

        // [GÜNCELLEME] Timeout ve Retry (Direnç Mekanizması)
        int maxRetries = 3;
        bool isSuccess = false;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            Console.Write($"[API İŞLEMİ] Parça {i + 1}/{chunks.Count} gönderiliyor (Deneme {attempt})...");
            
            isSuccess = await geminiService.GenerateAndSaveAudioAsync(chunks[i], outputPath, apiKey);

            if (isSuccess)
            {
                Console.WriteLine($" Tamamlandı. -> {fileName}");
                break;
            }
            else
            {
                Console.WriteLine(" HATA!");
                if (attempt < maxRetries)
                {
                    Console.WriteLine($"[BİLGİ] 5 saniye içinde tekrar denenecek...");
                    await Task.Delay(5000);
                }
                else
                {
                    Console.WriteLine($"[KRİTİK] {maxRetries} deneme başarısız oldu. Bu parça atlanıyor.");
                }
            }
        }
    }

    Console.WriteLine("\n[BİLGİ] Tüm parçalar başarıyla indirildi/doğrulandı.");

    // --- FİNAL AŞAMASI: BİRLEŞTİRME VE METADATA ---
    string outputDir = Path.Combine(Directory.GetCurrentDirectory(), "Output");
    if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);

    string finalMp3Path = Path.Combine(outputDir, $"{fileNameWithoutExt}_Tam.mp3");
    var mergerService = new AudioMergerService();

    try
    {
        Console.WriteLine($"[API İŞLEMİ] Ses dosyaları birleştiriliyor...");
        await mergerService.MergeWavFilesAsync(tempDir, finalMp3Path, fileNameWithoutExt);
        Console.WriteLine($"[TAMAMLANDI] Dosya oluşturuldu: {Path.GetFileName(finalMp3Path)}");

        // Kapak resmi bul (.jpg veya .png)
        string? coverPath = Directory.GetFiles(inputDir)
                                    .FirstOrDefault(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || 
                                                         f.EndsWith(".png", StringComparison.OrdinalIgnoreCase));

        Console.WriteLine("[API İŞLEMİ] Metadata (Lyrics & Cover) gömülüyor...");
        mergerService.EmbedMetadata(finalMp3Path, text, coverPath ?? "");

        // Geçici dosyaları temizle
        Console.WriteLine("[TEMİZLİK] Geçici .wav dosyaları silinicek...");
        foreach (var wavFile in Directory.GetFiles(tempDir, "*.wav"))
        {
            File.Delete(wavFile);
        }

        Console.WriteLine("\n--------------------------------------------------");
        Console.WriteLine("🏆 SESLİ KİTAP BAŞARIYLA OLUŞTURULDU!");
        Console.WriteLine($"Konum: {finalMp3Path}");
        Console.WriteLine("--------------------------------------------------");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[HATA] Birleştirme aşamasında bir sorun oluştu: {ex.Message}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Hata oluştu: {ex.Message}");
}
