using Microsoft.AspNetCore.Http;

namespace prjGoHike.Dtos
{
    public class CreateCommentDto
    {
        public int ArticleId { get; set; }

        public string Content { get; set; } = string.Empty;

        public int? ParentCommentId { get; set; }

        public long? ReplyToUserId { get; set; }

        public List<IFormFile> Images { get; set; } = new();
    }
}
