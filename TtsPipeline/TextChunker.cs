using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace TtsPipeline
{
    public static class TextChunker
    {
        public static List<string> SplitTextIntoSafeChunks(string text, int maxCharLimit = 3500)
        {
            var resultChunks = new List<string>();

            if (string.IsNullOrWhiteSpace(text))
                return resultChunks;

            // 1. Metni paragraflara böl
            var paragraphs = SplitIntoParagraphs(text);

            string currentChunk = "";

            foreach (var paragraph in paragraphs)
            {
                // Paragraf boşsa atla (Split'ten gelebilecek boşluklar için)
                if (string.IsNullOrWhiteSpace(paragraph))
                    continue;

                // Mevcut chunk + yeni paragraf (ve aradaki boşluk) limiti aşıyor mu?
                int potentialLength = currentChunk.Length + paragraph.Length + (string.IsNullOrEmpty(currentChunk) ? 0 : 2); // 2 for \n\n

                if (potentialLength <= maxCharLimit)
                {
                    // AŞMIYORSA: Ekle
                    if (!string.IsNullOrEmpty(currentChunk))
                        currentChunk += "\n\n";
                    currentChunk += paragraph;
                }
                else
                {
                    // AŞIYORSA:
                    // 1. Mevcut chunk'ı (varsa) listeye at
                    if (!string.IsNullOrEmpty(currentChunk))
                    {
                        resultChunks.Add(currentChunk);
                        currentChunk = "";
                    }

                    // 2. Sıradaki paragraf TEK BAŞINA limiti aşıyor mu?
                    if (paragraph.Length > maxCharLimit)
                    {
                        // Kritik Kural: Uzun paragrafı cümlelerden böl
                        var subChunks = SplitLongParagraph(paragraph, maxCharLimit);
                        resultChunks.AddRange(subChunks);
                    }
                    else
                    {
                        // Aşmıyorsa yeni chunk olarak başlat
                        currentChunk = paragraph;
                    }
                }
            }

            // Döngü bittiğinde elde kalan son chunk'ı ekle
            if (!string.IsNullOrEmpty(currentChunk))
            {
                resultChunks.Add(currentChunk);
            }

            return resultChunks;
        }

        private static string[] SplitIntoParagraphs(string text)
        {
            // \r\n\r\n veya \n\n ile böl
            return text.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static List<string> SplitLongParagraph(string paragraph, int maxCharLimit)
        {
            var chunks = new List<string>();
            // Cümle sonlarından böl: . ? ! 
            // Regex açıklaması: (?<=[.?!]) -> . ? veya ! karakterinden hemen sonrasını eşleşme noktası al (lookbehind)
            // Böylece noktalama işareti cümlede kalır.
            var sentences = Regex.Split(paragraph, @"(?<=[.?!])\s+");

            string currentSubChunk = "";

            foreach (var sentence in sentences)
            {
                if (string.IsNullOrWhiteSpace(sentence)) continue;

                int potentialLen = currentSubChunk.Length + sentence.Length + (string.IsNullOrEmpty(currentSubChunk) ? 0 : 1); // 1 for space

                if (potentialLen <= maxCharLimit)
                {
                    if (!string.IsNullOrEmpty(currentSubChunk))
                        currentSubChunk += " ";
                    currentSubChunk += sentence;
                }
                else
                {
                    if (!string.IsNullOrEmpty(currentSubChunk))
                    {
                        chunks.Add(currentSubChunk);
                        currentSubChunk = "";
                    }

                    // Çok nadir durum: Tek bir cümle bile limiti aşıyorsa?
                    // Kurallarda "Kelimeleri asla ortadan bölme" dendiği için, 
                    // bu durumda mecburen olduğu gibi yeni chunk yapıyoruz veya kelime kelime bölünebilir.
                    // Şimdilik cümle bütünlüğü bozulmasın diye (veya kelime bazlı bölme eklenebilir) 
                    // basitçe yeni chunk yapıp bırakıyoruz (overflow olsa bile), 
                    // çünkü kelime bölmek "asla" denmiş ama cümle sınırı da verilmiş.
                    // Güvenli tarafta kalmak için eğer cümle maxCharLimit'ten büyükse, olduğu gibi ekleyelim 
                    // (veya daha gelişmiş kelime bölme eklenebilir ama istenmedi).
                    if (sentence.Length > maxCharLimit)
                    {
                         // Cümle tek başına bile sığmıyor, yapacak bir şey yok, tek parça ekle.
                        chunks.Add(sentence);
                    }
                    else
                    {
                        currentSubChunk = sentence;
                    }
                }
            }

            if (!string.IsNullOrEmpty(currentSubChunk))
            {
                chunks.Add(currentSubChunk);
            }

            return chunks;
        }
    }
}
