using Microsoft.AspNetCore.Mvc;
using PinRism.Lib;
using PinRism.Lib.Models.DTOs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = _ =>
        new BadRequestObjectResult(new OcrResultDto { Error = "Invalid multipart upload. Supply an image in the file field." });
});
builder.Services.AddHttpClient<GeminiOcrService>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
