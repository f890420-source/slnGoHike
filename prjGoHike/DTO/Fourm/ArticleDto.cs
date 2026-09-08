namespace prjGoHike.DTO.Fourm
{
    public class ArticleDto
    {
        public int ArticleId { get; set; }

        public long UserId { get; set; }

        public int CategoryId { get; set; }

        public string Title { get; set; } = null!;

        public string Content { get; set; } = null!;

        public DateTime CreatedDate { get; set; }

        public DateTime? UpdateDate { get; set; }

        public byte Status { get; set; }

        public string? CategoryName { get; set; }
    }
}
