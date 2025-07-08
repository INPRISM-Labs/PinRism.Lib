
# PinRism.OCR - A .NET OCR Library powerd by Google Gemini

`PinRism.OCR` is a lightweight and easy-to-use .NET 8 class library designed for Optical Character Recognition (OCR) by leveraging the power of Google's Gemini API. It allows you to extract text from images within your .NET applications with minimal setup.

## Features

-   **Google Gemini API Integration:** Utilizes `gemini-2.0-flash` for robust and accurate text extraction.
    
-   **Simple API:** Provides a clean `GeminiOcrService` for straightforward usage.
    
-   **Dependency Injection Friendly:** Built with .NET's standard dependency injection patterns, making it easy to integrate into ASP.NET Core Web APIs, Console Apps, and other modern .NET applications.
    
-   **Base64 Image Handling:** Automatically handles the conversion of image byte arrays to base64 for Gemini API requests.
    
-   **Error Logging:** Integrates with `Microsoft.Extensions.Logging` for comprehensive error and information logging.
    

## Installation

You can install `PinRism.OCR` via NuGet Package Manager or the .NET CLI.

**NuGet Package Manager:**

```
Install-Package PinRism.OCR -Version 1.0.0

```

**

.NET CLI:**

```
dotnet add package PinRism.OCR --version 1.0.0

```

## Getting Started

### 1. Obtain Your Google Gemini API Key

To use this library, you need a Google Gemini API key.

-   Visit [Google AI Studio](https://aistudio.google.com/app/apikey "null") to generate your API key.
    
-   Keep this key secure and do not expose it directly in your code.
    

### 2. Configure Your API Key

The library expects the Gemini API key to be available via `Microsoft.Extensions.Configuration` under the key `GeminiApiKey`.

**Recommended way (for Web APIs or Console Apps): Using `appsettings.json`**

Add the following to your `appsettings.json` file (or `appsettings.Development.json` for development):

```
{
  "GeminiApiKey": "YOUR_GEMINI_API_KEY_HERE",
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "DotOcrLib": "Debug" // Optional: To see detailed logs from the library
    }
  }
}

```

**Important:** Replace `"YOUR_GEMINI_API_KEY_HERE"` with your actual API key.

### 3. Integrate into Your Application

#### For ASP.NET Core Web APIs (`Program.cs`):

```
// Program.cs
using DotOcrLib; // Using your library's namespace
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer(); // Optional, if using minimal APIs or Swagger
// builder.Services.AddSwaggerGen(); // Optional, if using Swagger

// Configure HttpClient for GeminiOcrService
builder.Services.AddHttpClient<GeminiOcrService>();

// Register GeminiOcrService with dependency injection
// Use AddSingleton for GeminiOcrService as it depends on HttpClient, which is managed by DI
builder.Services.AddSingleton<GeminiOcrService>();

// Load configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // app.UseSwagger(); // Optional
    // app.UseSwaggerUI(); // Optional
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

```

#### For Console Applications (`Program.cs`):

```
// Program.cs
using DotOcrLib; // Using your library's namespace
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;

Console.WriteLine("Starting OCR process...");

// 1. Configure the application
var builder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

IConfiguration configuration = builder.Build();

// 2. Setup Dependency Injection (DI) container
var serviceCollection = new ServiceCollection();

// Add logging
serviceCollection.AddLogging(configure => configure.AddConsole())
                 .Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Debug);

// Add HttpClient for GeminiOcrService
serviceCollection.AddHttpClient<GeminiOcrService>();

// Register GeminiOcrService
serviceCollection.AddSingleton<GeminiOcrService>();

// Add configuration to DI
serviceCollection.AddSingleton(configuration);

var serviceProvider = serviceCollection.BuildServiceProvider();

// 3. Get the OCR Service instance
var ocrService = serviceProvider.GetRequiredService<GeminiOcrService>();
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

// 4. Prepare image data
string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "my_image_with_text.png"); // Make sure this image exists!
string mimeType = "image/png"; // Adjust based on your image type (e.g., "image/jpeg")

if (!File.Exists(imagePath))
{
    logger.LogError($"Error: Image file not found at {imagePath}. Please provide a valid image file.");
    return;
}

byte[] imageData = await File.ReadAllBytesAsync(imagePath);

// 5. Perform OCR
try
{
    logger.LogInformation("Attempting to extract text from image...");
    string extractedText = await ocrService.ExtractTextFromImageAsync(imageData, mimeType);

    if (!string.IsNullOrEmpty(extractedText))
    {
        Console.WriteLine("\n--- Extracted Text ---");
        Console.WriteLine(extractedText);
        Console.WriteLine("----------------------");
    }
    else
    {
        Console.WriteLine("\nNo text was extracted from the image or an error occurred during extraction.");
    }
}
catch (Exception ex)
{
    logger.LogError(ex, "An unhandled error occurred during OCR process.");
}

Console.WriteLine("OCR process finished.");

```

### 4. Use the `GeminiOcrService` in Your Code

Once injected, you can use `GeminiOcrService` in your controllers, services, or other classes:

```
// Example: In an ASP.NET Core Controller
using Microsoft.AspNetCore.Mvc;
using DotOcrLib;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

[ApiController]
[Route("api/[controller]")]
public class MyOcrController : ControllerBase
{
    private readonly GeminiOcrService _ocrService;
    private readonly ILogger<MyOcrController> _logger;

    public MyOcrController(GeminiOcrService ocrService, ILogger<MyOcrController> logger)
    {
        _ocrService = ocrService;
        _logger = logger;
    }

    [HttpPost("extract-from-upload")]
    [Consumes("multipart/form-data")]
    [Produces("text/plain")]
    public async Task<IActionResult> ExtractTextFromUpload([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded or file is empty.");
        }

        // Basic MIME type validation
        if (!file.ContentType.StartsWith("image/"))
        {
            return BadRequest("Unsupported file type. Please upload an image (e.g., JPEG, PNG).");
        }

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        byte[] imageData = memoryStream.ToArray();

        string extractedText = await _ocrService.ExtractTextFromImageAsync(imageData, file.ContentType);

        if (string.IsNullOrEmpty(extractedText))
        {
            return Ok("No text found in the image.");
        }

        return Ok(extractedText);
    }
}

```

## API Reference (Key Class)

### `GeminiOcrService`

The primary service for OCR operations.

-   `public GeminiOcrService(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiOcrService> logger)`
    
    -   Constructor for dependency injection. Requires `HttpClient`, `IConfiguration` (for `GeminiApiKey`), and `ILogger`.
        
-   `public Task<string> ExtractTextFromImageAsync(byte[] imageData, string mimeType)`
    
    -   **Parameters:**
        
        -   `imageData`: The image content as a byte array.
            
        -   `mimeType`: The MIME type of the image (e.g., `"image/png"`, `"image/jpeg"`).
            
    -   **Returns:** A `string` containing the extracted text. Returns `string.Empty` if no text is found or an error occurs.
        

## Error Handling

The `ExtractTextFromImageAsync` method is designed to return `string.Empty` on most errors (e.g., API key issues, network problems, no text found). Detailed error messages are logged using `Microsoft.Extensions.Logging`. Monitor your application's logs for more insights into any issues during OCR processing.

## Contributing

We welcome contributions! If you have suggestions, bug reports, or want to contribute code, please visit our GitHub repository:

[https://github.com/PinRism-Labs/PinRism.OCR](https://www.google.com/url?sa=E&source=gmail&q=https://github.com/PinRism-Labs/PinRism.OCR)

## License

This project is licensed under the [MIT License]

## Support

For questions or issues, please open an issue on our GitHub repository.
