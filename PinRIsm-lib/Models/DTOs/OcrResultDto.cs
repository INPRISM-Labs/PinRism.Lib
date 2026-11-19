using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PinRism.Lib.Models.DTOs;

public class OcrResultDto
{
    public bool Success { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Data? Data { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Meta? Meta { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Error { get; set; }
}

public class Data
{
    public string? Name { get; set; }

    [JsonConverter(typeof(BirthDateJsonConverter))]
    public DateTime? BirthDate { get; set; }

    public string? Address { get; set; }
    public string? RawText { get; set; }
}

public class Meta
{
    public string? FileName { get; set; }
    public string? MimeType { get; set; }
    public long Length { get; set; }
}

internal sealed class BirthDateJsonConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.GetDateTime();

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        => writer.WriteStringValue(value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
}
