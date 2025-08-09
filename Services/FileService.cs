using FileServices.Api.Models;

namespace FileServices.Api.Services
{
    public interface IFileService
    {
        Task<UploadResponse> UploadFileAsync(IFormFile file);
        Task<(byte[] fileBytes, string contentType, string fileName)?> DownloadFileAsync(string fileName);
        Task<List<Models.FileInfo>> GetFilesAsync();
        Task<bool> DeleteFileAsync(string fileName);
    }

    public class FileService : IFileService
    {
        private readonly string _uploadsPath;
        private readonly ILogger<FileService> _logger;

        public FileService(ILogger<FileService> logger)
        {
            _logger = logger;
            _uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
            
            // Ensure uploads directory exists
            if (!Directory.Exists(_uploadsPath))
            {
                Directory.CreateDirectory(_uploadsPath);
            }
        }

        public async Task<UploadResponse> UploadFileAsync(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return new UploadResponse
                    {
                        Success = false,
                        Message = "No file uploaded or file is empty"
                    };
                }

                // Validate file size (100MB limit)
                if (file.Length > 100 * 1024 * 1024)
                {
                    return new UploadResponse
                    {
                        Success = false,
                        Message = "File size exceeds 100MB limit"
                    };
                }

                // Generate unique filename to prevent conflicts
                var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                var filePath = Path.Combine(_uploadsPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                _logger.LogInformation($"File uploaded successfully: {fileName}");

                return new UploadResponse
                {
                    Success = true,
                    Message = "File uploaded successfully",
                    FileName = fileName,
                    FileSize = file.Length
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                return new UploadResponse
                {
                    Success = false,
                    Message = $"Error uploading file: {ex.Message}"
                };
            }
        }

        public async Task<(byte[] fileBytes, string contentType, string fileName)?> DownloadFileAsync(string fileName)
        {
            try
            {
                var filePath = Path.Combine(_uploadsPath, fileName);
                
                if (!File.Exists(filePath))
                {
                    return null;
                }

                var fileBytes = await File.ReadAllBytesAsync(filePath);
                var contentType = GetContentType(fileName);

                return (fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error downloading file: {fileName}");
                return null;
            }
        }

        public Task<List<Models.FileInfo>> GetFilesAsync()
        {
            try
            {
                var files = new List<Models.FileInfo>();
                var directoryInfo = new DirectoryInfo(_uploadsPath);
                
                if (!directoryInfo.Exists)
                {
                    return Task.FromResult(files);
                }

                foreach (var file in directoryInfo.GetFiles())
                {
                    files.Add(new Models.FileInfo
                    {
                        FileName = file.Name,
                        Size = file.Length,
                        CreatedDate = file.CreationTime,
                        ContentType = GetContentType(file.Name)
                    });
                }

                return Task.FromResult(files.OrderByDescending(f => f.CreatedDate).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting files list");
                return Task.FromResult(new List<Models.FileInfo>());
            }
        }

        public Task<bool> DeleteFileAsync(string fileName)
        {
            try
            {
                var filePath = Path.Combine(_uploadsPath, fileName);
                
                if (!File.Exists(filePath))
                {
                    return Task.FromResult(false);
                }

                File.Delete(filePath);
                _logger.LogInformation($"File deleted successfully: {fileName}");
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting file: {fileName}");
                return Task.FromResult(false);
            }
        }

        private static string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".txt" => "text/plain",
                ".zip" => "application/zip",
                ".rar" => "application/x-rar-compressed",
                _ => "application/octet-stream"
            };
        }
    }
}
