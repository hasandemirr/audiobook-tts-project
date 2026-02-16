# TtsPipeline: Akıllı Sesli Kitap Oluşturucu 🎧📚

Bu proje, uzun metin dosyalarını (kitap bölümleri vb.) anlamlı parçalara bölen, Google Gemini API kullanarak yüksek kaliteli sese dönüştüren ve ardından bu parçaları tek bir MP3 dosyasında birleştiren profesyonel bir TTS (Text-to-Speech) pipeline uygulamasıdır.

## 🌟 Öne Çıkan Özellikler

*   **Model Seçimi:** Flash (hızlı) ve Pro (kalite) modelleri arasında geçiş yapabilme.
*   **Dinamik Parça Sınırı:** Kullanıcının belirlediği karakter limitine göre metin parçalama.
*   **Akıllı Metin Parçalama:** Uzun metinleri cümle bütünlüğünü bozmadan, paragrafları koruyarak güvenli parçalara ayırır (Varsayılan 800 karakter).
*   **Gemini Flash 2.5 TTS:** Google'ın en yeni modelleri ile doğal ve akıcı seslendirme.
*   **Dirençli Pipeline:** 
    *   **Retry:** Bağlantı hatalarında otomatik yeniden deneme (3 deneme).
    *   **Resume:** Kaldığı yerden devam etme (mevcut dosyaları atlar).
    *   **Timeout:** Uzun parçalar için 5 dakikalık zaman aşımı desteği.
*   **Otomatik Birleştirme:** Parçaları FFmpeg kullanarak tek bir MP3 dosyasında birleştirir.
*   **Metadata Gömme:** MP3 dosyasına tam metni (Lyrics) ve kapak resmini (ID3 tags) otomatik olarak ekler.
*   **Dosya Seçim Menüsü:** Birden fazla giriş dosyası arasından seçim yapabilme.

## 🛠️ Gereksinimler

1.  **[.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)** (Derleme ve çalıştırma için).
2.  **[FFmpeg](https://ffmpeg.org/download.html)** (Ses birleştirme için kritik).
    *   `ffmpeg.exe`'nin `C:\ffmpeg\bin\ffmpeg.exe` yolunda olması veya `AudioMergerService.cs` içindeki yolun güncellenmesi gerekir.
3.  **Google API Key:** [Google AI Studio](https://aistudio.google.com/) üzerinden ücretsiz alabilirsiniz.

## 🚀 Hızlı Başlangıç (GitHub Workflow)

### 1. Projeyi Klonlayın
```powershell
git clone https://github.com/hasandemirr/audiobook-tts-project.git
cd audiobook-tts-project/TtsPipeline
```

### 2. Bağımlılıkları Yükleyin ve Derleyin
```powershell
dotnet build
```

### 3. Klasör Yapısını Hazırlayın
Proje dizininde şu klasörlerin olduğundan emin olun (yoksa uygulama oluşturacaktır):
- `Input/`: Seslendirilecek `.txt` dosyalarını buraya atın. Kapak resmi için bir `.jpg` veya `.png` ekleyebilirsiniz.
- `Output/`: Final MP3 buraya kaydedilir.
- `Temp/`: Geçici ses parçaları burada tutulur (işlem sonunda temizlenir).

### 4. Uygulamayı Çalıştırın
```powershell
dotnet run
```

*   Uygulama başladığında **API Key** isteyecektir. Key'inizi yapıştırıp devam edin.
*   `Input` klasöründeki dosyalar listelenecek, işlemek istediğinizi seçin.

## 📂 Proje Yapısı

- `Program.cs`: Tüm süreci yöneten orkestrasyon katmanı.
- `GeminiTtsService.cs`: Google SDK entegrasyonu ve ses üretimi.
- `AudioMergerService.cs`: FFmpeg birleştirme ve TagLib# metadata işlemleri.
- `TextChunker.cs`: Metni akıllı parçalara bölen yardımcı sınıf.

## ⚠️ Dikkat Edilmesi Gerekenler

- **Maliyet:** Ücretsiz katman (Free Tier) kullanıyorsanız rate limitlere dikkat edin.
- **FFmpeg Yolu:** Eğer FFmpeg farklı bir klasördeyse `AudioMergerService.cs` içindeki `FileName = @"C:\ffmpeg\bin\ffmpeg.exe"` satırını kendi yolunuza göre güncelleyin.
- **Dil:** Uygulama şu an Türkçe metinler için optimize edilmiştir ancak model çok dilli destek sunmaktadır.

---
*Geliştirici: **hasandemirr***
