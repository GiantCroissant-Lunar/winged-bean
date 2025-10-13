using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Plate.CrossMilo.Contracts.Recorder.Services;

namespace WingedBean.Testing.Recorder;

/// <summary>
/// TextWriter that intercepts console output and feeds it to the recorder
/// </summary>
public class RecordingTextWriter : TextWriter
{
    private readonly TextWriter _originalWriter;
    private readonly IService _recorder;
    private readonly string _sessionId;
    private readonly StringBuilder _buffer = new();
    private readonly object _lock = new();

    public override Encoding Encoding => Encoding.UTF8;

    public RecordingTextWriter(
        TextWriter originalWriter,
        IService recorder,
        string sessionId)
    {
        _originalWriter = originalWriter;
        _recorder = recorder;
        _sessionId = sessionId;
    }

    public override void Write(char value)
    {
        _originalWriter.Write(value);

        lock (_lock)
        {
            _buffer.Append(value);

            // Flush on newline for better timing
            if (value == '\n')
            {
                FlushToRecorder();
            }
        }
    }

    public override void Write(string? value)
    {
        if (value == null) return;

        _originalWriter.Write(value);

        lock (_lock)
        {
            _buffer.Append(value);

            if (value.Contains('\n'))
            {
                FlushToRecorder();
            }
        }
    }

    public override void WriteLine(string? value)
    {
        _originalWriter.WriteLine(value);

        lock (_lock)
        {
            _buffer.AppendLine(value);
            FlushToRecorder();
        }
    }

    private void FlushToRecorder()
    {
        if (_buffer.Length == 0) return;

        try
        {
            var bytes = Encoding.UTF8.GetBytes(_buffer.ToString());
            _recorder.RecordDataAsync(_sessionId, bytes, DateTimeOffset.UtcNow).Wait();
            _buffer.Clear();
        }
        catch (Exception ex)
        {
            // Log but don't throw - recording shouldn't break output
            System.Console.Error.WriteLine($"[RecordingTextWriter] Error: {ex.Message}");
        }
    }

    public override void Flush()
    {
        _originalWriter.Flush();

        lock (_lock)
        {
            FlushToRecorder();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Flush();
        }
        base.Dispose(disposing);
    }
}
