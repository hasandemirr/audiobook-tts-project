using TtsPipeline;
using System.IO;
using System.Text.Json;

Console.WriteLine("Sesli Kitap Pipeline Başlatılıyor...");

// --- KONFİGÜRASYON SİSTEMİ ---
string configPath = Path.Combine(Directory.GetCurrentDirectory(), "config.json");
AppConfig config = new AppConfig();

if (File.Exists(configPath))
{
    try
    {
        string json = File.ReadAllText(configPath);
        config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();

        Console.WriteLine("\n[SİSTEM] Kayıtlı ön ayarlar bulundu:");
        string maskedKey = string.IsNullOrWhiteSpace(config.ApiKey) 
            ? "YOK" 
            : new string('*', Math.Max(0, config.ApiKey.Length - 4)) + config.ApiKey[^4..];
        
        Console.WriteLine($"- API Key: {maskedKey}");
        Console.WriteLine($"- Model: {config.ModelName}");
        Console.WriteLine($"- Karakter Limiti: {config.MaxCharLimit}");
        Console.WriteLine($"- Yönetmen Talimatı: {(string.IsNullOrWhiteSpace(config.DirectorPrompt) ? "YOK" : config.DirectorPrompt)}");

        Console.Write("\n[SİSTEM] Bu ayarlar kullanılsın mı? (E/H) [Varsayılan: E]: ");
        string? useConfig = Console.ReadLine();

        if (useConfig?.Trim().ToUpper() == "H")
        {
            config = RunSetupWizard();
            File.WriteAllText(configPath, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[UYARI] Konfigürasyon okunurken hata oluştu, sihirbaz başlatılıyor: {ex.Message}");
        config = RunSetupWizard();
        File.WriteAllText(configPath, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
    }
}
else
{
    config = RunSetupWizard();
    File.WriteAllText(configPath, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
}

// Kolay erişim için değişkenleri ayarla
string apiKey = config.ApiKey;
string modelName = config.ModelName;
int maxCharLimit = config.MaxCharLimit;
string directorPrompt = config.DirectorPrompt;

if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.WriteLine("[HATA] API Key bulunamadı. Lütfen config.json dosyasını kontrol edin veya uygulamayı yeniden başlatıp yeni anahtar girin.");
    return;
}

// --- KURULUM SİHİRBAZI ---
static AppConfig RunSetupWizard()
{
    AppConfig newConfig = new AppConfig();

    Console.WriteLine("\n=== YENİ KONFİGÜRASYON KURULUMU ===");
    
    Console.Write("Lütfen Google API Key giriniz: ");
    newConfig.ApiKey = Console.ReadLine() ?? "";

    Console.WriteLine("\nLütfen kullanılacak Gemini modelini seçin:");
    Console.WriteLine("[1] gemini-2.5-flash-preview-tts (Hızlı & Ekonomik)");
    Console.WriteLine("[2] gemini-2.5-pro-preview-tts (Yüksek Kalite)");
    Console.WriteLine("[3] Diğer (Manuel Giriş)");
    Console.Write("Seçiminiz (1-3): ");
    string? choice = Console.ReadLine();
    
    if (choice == "2") newConfig.ModelName = "gemini-2.5-pro-preview-tts";
    else if (choice == "3")
    {
        Console.Write("Lütfen model adını tam olarak yazın: ");
        newConfig.ModelName = Console.ReadLine() ?? newConfig.ModelName;
    }

    Console.Write("\nLütfen parça başına limit girin (Varsayılan: 800): ");
    string? limitInput = Console.ReadLine();
    if (int.TryParse(limitInput, out int limit)) newConfig.MaxCharLimit = limit;
    else newConfig.MaxCharLimit = 800;

    Console.Write("\nLütfen yönetmen talimatını girin (Boş bırakmak için Enter'a basın): ");
    newConfig.DirectorPrompt = Console.ReadLine() ?? "";

    Console.WriteLine("[TAMAM] Ayarlar kaydedildi.");
    return newConfig;
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

    // Parçala (Kullanıcı limiti ile)
    var chunks = TextChunker.SplitTextIntoSafeChunks(text, maxCharLimit);
    Console.WriteLine($"[BİLGİ] '{Path.GetFileName(filePath)}' dosyası okundu ve {chunks.Count} parçaya bölündü.");

    // Gerçek Servisi Başlat
    var geminiService = new GeminiTtsService();

    // Döngü ile işle
    for (int i = 0; i < chunks.Count; i++)
    {
        string chunkText = chunks[i];
        
        string baseFileName = fileNameWithoutExt; // Defined baseFileName
        string fileName = $"{baseFileName}_part_{i + 1:D3}.wav";
        string outputPath = Path.Combine(tempDir, fileName);
        
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
            
            isSuccess = await geminiService.GenerateAndSaveAudioAsync(chunks[i], outputPath, apiKey, modelName, directorPrompt);

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
