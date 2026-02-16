namespace TtsPipeline
{
    public class AppConfig
    {
        public string ApiKey { get; set; } = string.Empty;
        public string ModelName { get; set; } = "gemini-2.5-flash-preview-tts";
        public int MaxCharLimit { get; set; } = 800;
        public string DirectorPrompt { get; set; } = string.Empty;
    }
}
