using Newtonsoft.Json.Converters;

namespace NosAyudamos
{
    public class DateOnlyConverter : IsoDateTimeConverter
    {
        public DateOnlyConverter() => DateTimeFormat = "yyyy-MM-dd";
    }
}
