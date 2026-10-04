using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace prjGoHike.Services.forum
{
    public class GeminiModerationService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public GeminiModerationService(
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<bool> IsContentSafeAsync(
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
請判斷下面這段討論區留言是否包含：
- 辱罵
- 人身攻擊
- 仇恨言論
- 性騷擾或明顯不雅內容

只回答 SAFE 或 UNSAFE，不要回答其他內容。

留言：
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

            var resultText =
                document.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();
            Console.WriteLine(
    $"Gemini 判斷結果：{resultText}"
);
            return string.Equals(
                resultText?.Trim(),
                "SAFE",
                StringComparison.OrdinalIgnoreCase
            );
        }
    }
}
//gemini罵人判斷  你到底有沒有腦袋啊？每次講話都完全不經思考，什麼都不懂還一直裝得自己很厲害，看你發言真的讓人覺得很可笑。
