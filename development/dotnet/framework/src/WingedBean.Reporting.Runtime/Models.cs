using System.Text.Json.Serialization;

namespace WingedBean.Reporting.Runtime;

public sealed class ResourceBundleEntry
{
    [JsonPropertyName("path")] public string Path { get; set; } = string.Empty;
    [JsonPropertyName("sizeBytes")] public long SizeBytes { get; set; }
}

public sealed class ResourceBundleReport
{
    [JsonPropertyName("bundleName")] public string BundleName { get; set; } = string.Empty;
    [JsonPropertyName("bundleVersion")] public string? BundleVersion { get; set; }
    [JsonPropertyName("fileCount")] public int FileCount { get; set; }
    [JsonPropertyName("totalSizeBytes")] public long TotalSizeBytes { get; set; }
    [JsonPropertyName("entries")] public List<ResourceBundleEntry> Entries { get; set; } = new();
    [JsonPropertyName("timestampUtc")] public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}

public sealed class AsciinemaReport
{
    [JsonPropertyName("sessionId")] public string SessionId { get; set; } = Guid.NewGuid().ToString("N");
    [JsonPropertyName("durationSeconds")] public double DurationSeconds { get; set; }
    [JsonPropertyName("outputFiles")] public List<string> OutputFiles { get; set; } = new();
    [JsonPropertyName("timestampUtc")] public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}

public sealed class RuntimeMetricsReport
{
    [JsonPropertyName("process")] public string Process { get; set; } = string.Empty;
    [JsonPropertyName("cpuTimeSeconds")] public double CpuTimeSeconds { get; set; }
    [JsonPropertyName("memoryMB")] public double MemoryMB { get; set; }
    [JsonPropertyName("counters")] public Dictionary<string, double> Counters { get; set; } = new();
    [JsonPropertyName("timestampUtc")] public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}
