using System.Globalization;
using PinRism.Lib.Models.DTOs;

namespace DotOcrAPI;

internal static class OcrFields
{
    public static Data Parse(string text)
    {
        var data = new Data { RawText = text };
        var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var line in lines)
        {
            var separator = line.IndexOf(':');
            if (separator < 0)
                continue;

            var label = line[..separator].Trim().ToLowerInvariant();
            var value = line[(separator + 1)..].Trim();
            if (value.Length == 0)
                continue;

            switch (label)
            {
                case "name":
                case "full name":
                    data.Name ??= value;
                    break;
                case "date of birth":
                case "birth date":
                case "birthdate":
                case "dob":
                case "d.o.b.":
                    if (DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                        data.BirthDate ??= date;
                    break;
                case "address":
                    data.Address ??= value;
                    break;
            }
        }

        // Support the sample ID layout with an unlabeled name above the birth date.
        if (data.Name is null && data.BirthDate.HasValue && data.Address is not null &&
            lines.Length >= 3 && !lines[0].Contains(':') && lines[0].Any(char.IsLetter))
        {
            var nextLabel = lines[1].Split(':', 2)[0].Trim().ToLowerInvariant();
            if (nextLabel is "date of birth" or "birth date" or "birthdate" or "dob" or "d.o.b.")
                data.Name = lines[0];
        }

        return data;
    }
}
