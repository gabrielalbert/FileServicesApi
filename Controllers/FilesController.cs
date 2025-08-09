using Microsoft.AspNetCore.Mvc;
using FileServices.Api.Services;
using FileServices.Api.Models;

namespace FileServices.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FilesController : ControllerBase
    {
        private readonly IFileService _fileService;
        private readonly ILogger<FilesController> _logger;

        public FilesController(IFileService fileService, ILogger<FilesController> logger)
        {
            _fileService = fileService;
            _logger = logger;
        }

        /// <summary>
        /// Upload a file
        /// </summary>
        /// <param name="file">The file to upload</param>
        /// <returns>Upload response</returns>
        [HttpPost("upload")]
        public async Task<ActionResult<UploadResponse>> UploadFile(IFormFile file)
        {
            var result = await _fileService.UploadFileAsync(file);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }

        /// <summary>
        /// Download a file by filename
        /// </summary>
        /// <param name="fileName">The name of the file to download</param>
        /// <returns>File content</returns>
        [HttpGet("download/{fileName}")]
        public async Task<IActionResult> DownloadFile(string fileName)
        {
            var result = await _fileService.DownloadFileAsync(fileName);
            
            if (result == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "File not found"
                });
            }

            return File(result.Value.fileBytes, result.Value.contentType, result.Value.fileName);
        }

        /// <summary>
        /// Get list of all uploaded files
        /// </summary>
        /// <returns>List of file information</returns>
        [HttpGet("list")]
        public async Task<ActionResult<ApiResponse<List<Models.FileInfo>>>> GetFiles()
        {
            var files = await _fileService.GetFilesAsync();
            
            return Ok(new ApiResponse<List<Models.FileInfo>>
            {
                Success = true,
                Message = "Files retrieved successfully",
                Data = files
            });
        }

        /// <summary>
        /// Delete a file by filename
        /// </summary>
        /// <param name="fileName">The name of the file to delete</param>
        /// <returns>Delete response</returns>
        [HttpDelete("delete/{fileName}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteFile(string fileName)
        {
            var result = await _fileService.DeleteFileAsync(fileName);
            
            if (result)
            {
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "File deleted successfully"
                });
            }
            
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "File not found or could not be deleted"
            });
        }

        /// <summary>
        /// Health check endpoint
        /// </summary>
        /// <returns>API status</returns>
        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "File Service API is running",
                Data = new { Timestamp = DateTime.UtcNow }
            });
        }
    }
}
