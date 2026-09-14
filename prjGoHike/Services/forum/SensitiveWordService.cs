namespace prjGoHike.Services.forum
{
    public class SensitiveWordService
    {
        // 敏感詞清單
        private readonly string[] _sensitiveWords =
        {
            "白癡",
            "智障",
            "媽的",
            "腦殘",
            "低能"
        };

        // 判斷文字是否包含敏感詞
        public bool ContainsSensitiveWord(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return false;
            }

            return _sensitiveWords.Any(word =>
                content.Contains(
                    word,
                    StringComparison.OrdinalIgnoreCase
                )
            );
        }
    }
}
