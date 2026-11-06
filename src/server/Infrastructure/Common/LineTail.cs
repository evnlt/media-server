namespace Infrastructure.Common;

// Keeps the last lines of a process stream; lines arrive on another thread, hence the lock.
public sealed class LineTail
{
    private readonly int _capacity;
    private readonly Queue<string> _lines = new();

    public LineTail(int capacity)
    {
        _capacity = capacity;
    }

    public void Add(string? line)
    {
        if (line is null)
        {
            return;
        }

        lock (_lines)
        {
            _lines.Enqueue(line);
            while (_lines.Count > _capacity)
            {
                _lines.Dequeue();
            }
        }
    }

    public string GetText()
    {
        lock (_lines)
        {
            return string.Join(Environment.NewLine, _lines);
        }
    }
}
