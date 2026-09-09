using Microsoft.AspNetCore.Http;

namespace prjGoHike.Dtos
{
    public class CreateArticleDto
    {
        public int CategoryId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        // 文章圖片，可一次上傳多張
        public List<IFormFile> Images { get; set; } = new();
    }
}
