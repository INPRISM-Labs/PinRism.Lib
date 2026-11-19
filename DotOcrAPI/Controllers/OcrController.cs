using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PinRism.Lib;
using PinRism.Lib.Models.DTOs;

namespace DotOcrAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class OcrController(GeminiOcrService ocrService, ILogger<OcrController> logger) : ControllerBase
{
    private static readonly HashSet<string> SupportedImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/heic", "image/heif"
    };

    [HttpPost("extract-text")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<OcrResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<OcrResultDto>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<OcrResultDto>(StatusCodes.Status502BadGateway)]
    [ProducesResponseType<OcrResultDto>(StatusCodes.Status504GatewayTimeout)]
    [ProducesResponseType<OcrResultDto>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExtractText([FromForm] IFormFile? file, CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new OcrResultDto { Error = "No file uploaded or file is empty." });

        if (!SupportedImageTypes.Contains(file.ContentType))
            return BadRequest(new OcrResultDto { Error = "Unsupported file type. Please upload a JPEG, PNG, WebP, HEIC, or HEIF image." });

        try
        {
            using var image = new MemoryStream();
            await file.CopyToAsync(image, cancellationToken);
            var text = await ocrService.ExtractTextFromImageAsync(image.ToArray(), file.ContentType.ToLowerInvariant(), cancellationToken);

            if (string.IsNullOrWhiteSpace(text))
                return Ok(new OcrResultDto { Error = "No text found in the image." });

            return Ok(new OcrResultDto
            {
                Success = true,
                Data = OcrFields.Parse(text),
                Meta = new Meta { FileName = file.FileName, MimeType = file.ContentType, Length = file.Length }
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException ex)
        {
            logger.LogWarning(ex, "The OCR provider timed out.");
            return StatusCode(504, new OcrResultDto { Error = "The OCR provider timed out." });
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            logger.LogWarning(ex, "The OCR provider could not complete the request.");
            return StatusCode(502, new OcrResultDto { Error = "The OCR provider could not complete the request." });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during text extraction.");
            return StatusCode(500, new OcrResultDto { Error = "An error occurred during text extraction." });
        }
    }
}
