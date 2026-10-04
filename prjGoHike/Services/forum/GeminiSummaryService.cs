using System.Text;
using System.Text.Json;

namespace prjGoHike.Services.forum
{
    public class GeminiSummaryService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public GeminiSummaryService(
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<string> GenerateSummaryAsync(
            string title,
            string content)
        {
            var apiKey =
                _configuration["Gemini:ApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new Exception(
                    "Gemini API Key 尚未設定"
                );
            }

            var prompt = $"""
你是登山討論平台 GoHike 的文章整理助手。

請根據以下文章的「標題」與「內容」整理文章重點。

規則：
1. 使用繁體中文。
2. 只能根據原文提供的資訊進行整理。
3. 不得自行加入原文沒有提到的登山資訊。
4. 不得自行推測路線、天氣、裝備或安全資訊。
5. 摘要內容簡潔易讀。
6. 請整理文章的主要重點。
7. 如果文章有提到地點、路線、裝備或注意事項，可以整理出來。
8. 如果原文沒有相關資訊，不需要強行產生該項目。
9. 不要加入 Markdown 標題符號，例如 #、##、###。

文章標題：
{title}

文章內容：
{content}
""";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = prompt
                            }
                        }
                    }
                }
            };

            var json =
                JsonSerializer.Serialize(requestBody);

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    "https://generativelanguage.googleapis.com/v1beta/models/gemini-3.1-flash-lite:generateContent"
                );

            request.Headers.Add(
                "x-goog-api-key",
                apiKey
            );

            request.Content =
                new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json"
                );

            var response =
                await _httpClient.SendAsync(request);

            response.EnsureSuccessStatusCode();

            var responseJson =
                await response.Content.ReadAsStringAsync();

            using var document =
                JsonDocument.Parse(responseJson);

            var summary =
                document.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

            if (string.IsNullOrWhiteSpace(summary))
            {
                throw new Exception(
                    "Gemini 未產生文章摘要"
                );
            }

            Console.WriteLine(
                $"Gemini 文章摘要：{summary}"
            );

            return summary.Trim();
        }
    }
}
