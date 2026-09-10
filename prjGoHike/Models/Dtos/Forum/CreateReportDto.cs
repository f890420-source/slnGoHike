namespace prjGoHike.Models.Dtos.Forum
{
    public class CreateReportDto
    {
        public int ArticleId { get; set; }

        public string Reason { get; set; } = string.Empty;
    }
}
