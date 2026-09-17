namespace prjGoHike.Services.forum
{
    public class CommentValidationService
    {
        public string? ValidateImages(
            List<IFormFile> images)
        {
            // =========================
            // 圖片數量驗證
            // =========================
            if (images.Count > 5)
            {
                return "一則留言最多只能上傳 5 張圖片";
            }

            // =========================
            // 圖片大小驗證
            // =========================
            foreach (var image in images)
            {
                if (image.Length > 5 * 1024 * 1024)
                {
                    return "單張圖片大小不能超過 5 MB";
                }
            }

            // =========================
            // 副檔名驗證
            // =========================
            var allowedExtensions = new[]
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp"
            };

            foreach (var image in images)
            {
                var extension = Path
                    .GetExtension(image.FileName)
                    .ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    return "只允許上傳 jpg、jpeg、png、webp 圖片";
                }
            }

            // =========================
            // MIME Type 驗證
            // =========================
            var allowedContentTypes = new[]
            {
                "image/jpeg",
                "image/png",
                "image/webp"
            };

            foreach (var image in images)
            {
                if (!allowedContentTypes.Contains(
                    image.ContentType))
                {
                    return "上傳檔案格式不正確";
                }
            }

            return null;
        }
    }
}
