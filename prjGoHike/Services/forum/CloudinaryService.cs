using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace prjGoHike.Services.forum
{
    public class CloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IConfiguration configuration)
        {
            var cloudName =
                configuration["Cloudinary:CloudName"];

            var apiKey =
                configuration["Cloudinary:ApiKey"];

            var apiSecret =
                configuration["Cloudinary:ApiSecret"];

            var account = new Account(
                cloudName,
                apiKey,
                apiSecret
            );

            _cloudinary = new Cloudinary(account);
        }


        public async Task<string> UploadImageAsync(
            IFormFile file,
            string folder)
        {
            await using var stream =
                file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(
                    file.FileName,
                    stream
                ),

                Folder = folder
            };

            var result =
                await _cloudinary.UploadAsync(uploadParams);

            if (result.Error != null)
            {
                throw new Exception(
                    result.Error.Message
                );
            }

            return result.SecureUrl.ToString();
        }

        public async Task DeleteImageAsync(string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return;
            }

            var uri = new Uri(imageUrl);

            var path = uri.AbsolutePath;

            var uploadIndex = path.IndexOf("/upload/");

            if (uploadIndex == -1)
            {
                return;
            }

            var publicIdWithExtension =
                path[(uploadIndex + "/upload/".Length)..];

            // Cloudinary URL 可能包含 v1234567890 版本號
            var parts = publicIdWithExtension.Split('/');

            if (
                parts.Length > 0 &&
                parts[0].StartsWith("v") &&
                parts[0].Substring(1).All(char.IsDigit)
            )
            {
                publicIdWithExtension =
                    string.Join("/", parts.Skip(1));
            }

            var publicId =
                Path.ChangeExtension(
                    publicIdWithExtension,
                    null
                );

            var deletionParams =
                new DeletionParams(publicId);

            await _cloudinary.DestroyAsync(
                deletionParams
            );
        }
    }
}
