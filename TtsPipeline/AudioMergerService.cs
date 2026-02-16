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
        public async Task MergeWavFilesAsync(string tempDir, string outputFile, string filePrefix)
        {
            // Belirtilen öneke sahip .wav dosyalarını isme göre sırala
            var wavFiles = Directory.GetFiles(tempDir, $"{filePrefix}_part_*.wav")
                                    .OrderBy(f => f)
                                    .ToList();

            if (wavFiles.Count == 0)
            {
                throw new FileNotFoundException($"{filePrefix} ile başlayan birleştirilecek .wav dosyası bulunamadı.");
            }

            var filterInputs = new System.Text.StringBuilder();
            var processStartInfo = new ProcessStartInfo
            {
                FileName = @"C:\ffmpeg\bin\ffmpeg.exe",
                WorkingDirectory = tempDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // .NET 8 ile gelen ArgumentList dize kaçırma (escaping) sorunlarını önler.
            for (int i = 0; i < wavFiles.Count; i++)
            {
                string fileName = Path.GetFileName(wavFiles[i]);
                processStartInfo.ArgumentList.Add("-f");
                processStartInfo.ArgumentList.Add("s16le");
                processStartInfo.ArgumentList.Add("-ar");
                processStartInfo.ArgumentList.Add("24000");
                processStartInfo.ArgumentList.Add("-ac");
                processStartInfo.ArgumentList.Add("1");
                processStartInfo.ArgumentList.Add("-i");
                processStartInfo.ArgumentList.Add(fileName);
                
                filterInputs.Append($"[{i}:a]");
            }

            filterInputs.Append($"concat=n={wavFiles.Count}:v=0:a=1[out]");

            processStartInfo.ArgumentList.Add("-filter_complex");
            processStartInfo.ArgumentList.Add(filterInputs.ToString());
            processStartInfo.ArgumentList.Add("-map");
            processStartInfo.ArgumentList.Add("[out]");
            processStartInfo.ArgumentList.Add("-c:a");
            processStartInfo.ArgumentList.Add("libmp3lame");
            processStartInfo.ArgumentList.Add("-b:a");
            processStartInfo.ArgumentList.Add("128k");
            processStartInfo.ArgumentList.Add("-ac");
            processStartInfo.ArgumentList.Add("1");
            processStartInfo.ArgumentList.Add("-y");
            processStartInfo.ArgumentList.Add(outputFile);

            Console.WriteLine($"[DEBUG] FFmpeg {(wavFiles.Count)} parça için başlatılıyor...");
            
            using var process = new Process { StartInfo = processStartInfo };
            
            try
            {
                process.Start();
            }
            catch (System.ComponentModel.Win32Exception)
            {
                throw new Exception("FFmpeg ('ffmpeg.exe') sistemde bulunamadı. Lütfen yolu kontrol edin.");
            }

            // Deadlock'u önlemek için çıktıları paralel (asenkron) oku
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            string error = await errorTask;
            string output = await outputTask;

            if (process.ExitCode != 0)
            {
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
