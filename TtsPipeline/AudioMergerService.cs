using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;

namespace TtsPipeline
{
    public class AudioMergerService
    {
        public async Task MergeWavFilesAsync(string tempDir, string outputFile)
        {
            // .wav dosyalarını isme göre sırala (part_001, part_002...)
            var wavFiles = Directory.GetFiles(tempDir, "*.wav")
                                    .OrderBy(f => f)
                                    .ToList();

            if (wavFiles.Count == 0)
            {
                throw new FileNotFoundException("Birleştirilecek .wav dosyası bulunamadı.");
            }

            string listFilePath = Path.Combine(tempDir, "file_list.txt");
            
            // FFmpeg concat listesi oluştur
            using (var sw = new StreamWriter(listFilePath))
            {
                foreach (var file in wavFiles)
                {
                    // FFmpeg concat için 'file' formatı ve tek tırnak/kaçış karakterleri
                    sw.WriteLine($"file '{file.Replace("'", "'\\''")}'");
                }
            }

            // FFmpeg Komutu:
            // -f concat: Dosyaları birleştir
            // -safe 0: Yerel dosya yollarına izin ver
            // -i: Input listesi
            // -c:a libmp3lame: MP3 formatına dönüştür
            // -b:a 128k: Bit hızı
            // -ac 1: Mono (genelde sesli kitap için yeterli ve yer tasarrufu sağlar)
            string arguments = $"-f concat -safe 0 -i \"{listFilePath}\" -c:a libmp3lame -b:a 128k -ac 1 \"{outputFile}\" -y";

            var processStartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processStartInfo };
            
            bool started = process.Start();
            if (!started)
            {
                throw new Exception("FFmpeg başlatılamadı. Sistemde yüklü olduğundan emin olun.");
            }

            await process.WaitForExitAsync();

            // Dosya listesini temizle
            if (File.Exists(listFilePath))
            {
                File.Delete(listFilePath);
            }

            if (process.ExitCode != 0)
            {
                string error = await process.StandardError.ReadToEndAsync();
                throw new Exception($"FFmpeg hatası (ExitCode: {process.ExitCode}): {error}");
            }
        }

        public void EmbedMetadata(string mp3FilePath, string fullText, string coverImagePath)
        {
            using (var file = TagLib.File.Create(mp3FilePath))
            {
                // Şarkı sözleri (Lyrics) kısmına tam metni gömüyoruz
                file.Tag.Lyrics = fullText;

                // Kapak görseli ekleme
                if (!string.IsNullOrEmpty(coverImagePath) && File.Exists(coverImagePath))
                {
                    var picture = new TagLib.Picture(coverImagePath);
                    file.Tag.Pictures = new TagLib.IPicture[] { picture };
                }

                file.Save();
            }
        }
    }
}
