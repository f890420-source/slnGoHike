namespace prjGoHike.Services.forum
{
    public class SensitiveWordService
    {
        #region 敏感詞清單
        private readonly string[] _sensitiveWords =
{
    // 常見辱罵
    "白癡",
    "白痴",
    "智障",
    "腦殘",
    "低能",
    "弱智",
    "智缺",
    "腦袋有洞",
    "沒腦",
    "有病",
    "神經病",
    "垃圾",
    "廢物",
    "人渣",
    "敗類",
    "畜生",
    "禽獸",
    "狗東西",
    "王八蛋",
    "混蛋",

    // 常見髒話
    "媽的",
    "他媽的",
    "他媽",
    "幹你娘",
    "幹你媽",
    "操你媽",
    "靠北",
    "靠杯",
    "靠邀",
    "機掰",
    "雞掰",
    "雞巴",
    "懶叫",
    "三小",
    "啥小",
    "洨",

    // 英文常見辱罵
    "fuck",
    "fucking",
    "shit",
    "bitch",
    "asshole",
    "idiot",
    "moron",
    "stupid"
};
        #endregion
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
