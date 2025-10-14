using Microsoft.Extensions.Logging.Abstractions;
using WingedBean.Plugins.AsciinemaRecorder;
using WingedBean.Plugins.Resource;
using WingedBean.Reporting.Runtime;
using RecorderSessionMetadata = Plate.CrossMilo.Contracts.Recorder.Services.SessionMetadata;

// Demo: emit Winged runtime reports using existing plugins

Console.WriteLine("Winged Reporting Demo: Emitting bundle and asciinema reports...");

// 1) Create a tiny resource bundle and emit a report
var tmpRoot = Path.Combine(Directory.GetCurrentDirectory(), "build", "_temp", "winged-demo");
Directory.CreateDirectory(tmpRoot);
var sampleFile = Path.Combine(tmpRoot, "hello.txt");
File.WriteAllText(sampleFile, "Hello, Winged reporting!\n");

var bundleOut = Path.Combine(tmpRoot, "demo-bundle.wbundle");
var builder = new ResourceBundleBuilder("demo-bundle", "1.0.0");
builder.AddResource(sampleFile, "hello.txt");
await builder.BuildAsync(bundleOut);
Console.WriteLine($"Resource bundle created: {bundleOut}");

// 2) Use URF recorder plugin to simulate a minimal asciinema session and emit a report
var logger = NullLogger<URFRecorderService>.Instance;
var recorder = new URFRecorderService(logger);
var sessionId = $"demo-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
var meta = new RecorderSessionMetadata
{
    Width = 80,
    Height = 24,
    Environment = new Dictionary<string, string> { ["DEMO"] = "1" }
};
await recorder.StartRecordingAsync(sessionId, meta);
await recorder.RecordDataAsync(sessionId, System.Text.Encoding.UTF8.GetBytes("Hello from URF demo!\r\n"), DateTimeOffset.UtcNow);
await Task.Delay(100);
var urfPath = await recorder.StopRecordingAsync(sessionId);
Console.WriteLine($"URF recording file: {urfPath}");

Console.WriteLine("Done. Now run WingedIngestionDemo against build/_artifacts/<version>/winged to see aggregates.");
