namespace PeekPin;

public sealed class FileLogger
{
    public const int MaxBytes = 1024 * 1024;
    private readonly string _path;
    private readonly object _gate = new();

    public FileLogger(string directory)
    {
        Directory.CreateDirectory(directory);
        _path = Path.Combine(directory, "peekpin.log");
    }

    public string FilePath => _path;

    public void Info(string message) => Write("INFO", message);

    public void Error(string message) => Write("ERROR", message);

    private void Write(string level, string message)
    {
        try
        {
            lock (_gate)
            {
                if (File.Exists(_path) && new FileInfo(_path).Length > MaxBytes)
                {
                    File.Delete(_path);
                }

                File.AppendAllText(_path, $"{DateTime.Now:O} {level} {message}{Environment.NewLine}");
            }
        }
        catch (IOException)
        {
        }
    }
}
