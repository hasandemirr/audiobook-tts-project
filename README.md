# TtsPipeline: Sesli Kitap Oluşturma Aracı

Bu proje, uzun metin dosyalarını (örneğin kitap bölümlerini) akıllıca parçalara bölen ve Google Gemini API kullanarak yüksek kaliteli ses dosyalarına (TTS) dönüştüren bir .NET 8 konsol uygulamasıdır.

## Özellikler

*   **Akıllı Metin Parçalama:** Uzun metinleri cümle bütünlüğünü bozmadan, paragrafları koruyarak güvenli parçalara ayırır (maksimum 3500 karakter).
*   **Google Gemini Entegrasyonu:** Google'ın gelişmiş üretken yapay zeka modellerini kullanarak doğal ve etkileyici bir seslendirme yapar.
*   **Yönetmen Şablonu:** Seslendirme için "Efsanevi Anlatıcı" profili kullanır (derin bariton, otoriter, stüdyo kalitesi).
*   **Otomatik Kayıt:** Oluşturulan ses dosyalarını sıralı bir şekilde (`part_001.wav`, `part_002.wav`...) kaydeder.

## Gereksinimler

*   [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
*   **[FFmpeg](https://ffmpeg.org/download.html)** (Ses dosyalarını birleştirmek için gereklidir. Sistem PATH'ine ekli olmalıdır.)
*   Google API Key (Gemini API erişimi için - [Google AI Studio](https://aistudio.google.com/))

## Kurulum ve Hazırlık

1.  **Projeyi Derleyin:**
    Terminali açın ve proje dizinine gidin (`TtsPipeline` klasörü), ardından şu komutu çalıştırın:
    ```powershell
    dotnet build
    ```

2.  **Girdi Dosyası:**
    Seslendirmek istediğiniz metin dosyasını (örneğin `kitap.txt`) projenin `Input` klasörüne kopyalayın.
    *   **Not:** Program `Input` klasöründeki *ilk* `.txt` dosyasını otomatik olarak işleme alır.

## Kullanım

Projeyi çalıştırmak için terminalde şu komutu girin:

```powershell
dotnet run
```

1.  Uygulama başladığında sizden **Google API Key** isteyecektir.
2.  API Key'inizi yapıştırıp `Enter` tuşuna basın.
3.  Uygulama metni parçalara bölecek ve her bir parça için API isteği gönderecektir.
4.  İşlem durumu terminalde canlı olarak gösterilecektir (`[API İŞLEMİ] Parça 1/10 gönderiliyor...`).

## Çıktılar

Oluşturulan ses dosyaları projenin `Temp` klasöründe saklanır.

Örnek dosya isimleri:
*   `kitap_part_001.wav`
*   `kitap_part_002.wav`
*   ...

## Sorun Giderme

*   **API Hatası:** Eğer API hatası alırsanız, internet bağlantınızı ve API anahtarınızın kotasını/yetkisini kontrol edin.
*   **Dosya Bulunamadı:** `Input` klasörünün dolu olduğundan emin olun.

## Proje Yapısı

*   `Program.cs`: Ana uygulama akışı.
*   `TextChunker.cs`: Metin parçalama mantığı.
*   `GeminiTtsService.cs`: Google API ile iletişim kuran servis.
*   `MockGeminiTtsService.cs`: Test amaçlı sahte servis (artık kullanılmıyor ama kodda mevcut).
