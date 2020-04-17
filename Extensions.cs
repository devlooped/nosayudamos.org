using System;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace NosAyudamos
{
    public static class Extensions
    {
        public static void Log<T>(this ILogger<T> logger, LogLevel level, object? value)
        {
            logger.Log(level, 
                "```" + 
                JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true }) + 
                "```", Array.Empty<object>());
        }
    }
}
