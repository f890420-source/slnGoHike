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
    }
}
