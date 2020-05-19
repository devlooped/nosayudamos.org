using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace NosAyudamos
{
    public partial class VisionResult
    {
        public VisionResult(string status) => Status = status;

        [JsonProperty("status")]
        public string Status { get; }

        [JsonProperty("createdDateTime")]
        public DateTimeOffset Created { get; set; }

        [JsonProperty("lastUpdatedDateTime")]
        public DateTimeOffset Updated { get; set; }

        [JsonProperty("analyzeResult")]
        public AnalyzeResult AnalyzeResult { get; set; } = new AnalyzeResult();
    }

    public partial class AnalyzeResult
    {
        [JsonProperty("version")]
        public string? Version { get; set; }

        [JsonProperty("readResults")]
        public List<ReadResult> ReadResults { get; } = new List<ReadResult>();
    }

    public partial class ReadResult
    {
        [JsonProperty("page")]
        public long Page { get; set; }

        [JsonProperty("language")]
        public string? Language { get; set; }

        [JsonProperty("angle")]
        public double Angle { get; set; }

        [JsonProperty("width")]
        public long Width { get; set; }

        [JsonProperty("height")]
        public long Height { get; set; }

        [JsonProperty("unit")]
        public string? Unit { get; set; }

        [JsonProperty("lines")]
        public List<Line> Lines { get; } = new List<Line>();

        public override string ToString() => string.Join(System.Environment.NewLine, Lines.Select(x => x.ToString()));
    }

    public partial class Line
    {
        public Line(string text) => Text = text;

        [JsonProperty("boundingBox")]
        public List<long> BoundingBox { get; } = new List<long>();

        [JsonProperty("text")]
        public string Text { get; }

        [JsonProperty("words")]
        public List<Word> Words { get; } = new List<Word>();

        public override string ToString() => Text;
    }

    public partial class Word
    {
        public Word(string text) => Text = text;

        [JsonProperty("boundingBox")]
        public List<long> BoundingBox { get; } = new List<long>();

        [JsonProperty("text")]
        public string Text { get; }

        [JsonProperty("confidence")]
        public double Confidence { get; set; }

        public override string ToString() => Text;
    }
}
