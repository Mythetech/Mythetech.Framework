namespace Mythetech.Framework.Desktop.Services;

/// <summary>
/// Delivers a stream's lines to its handlers, holding the lines raised before the first handler subscribes, up to
/// <paramref name="capacity"/>, and giving them to that handler when it does.
/// </summary>
internal sealed class BufferedLineEvent(int capacity)
{
    // Handlers run under the lock, so a replay and a new line never reach a handler out of order or at once.
    private readonly Lock _lock = new();
    private Action<string>? _handlers;
    private List<string>? _pending = [];

    public void Add(Action<string>? handler)
    {
        if (handler is null)
        {
            return;
        }

        lock (_lock)
        {
            _handlers += handler;
            if (_pending is null)
            {
                return;
            }

            var pending = _pending;
            _pending = null;
            foreach (var line in pending)
            {
                handler(line);
            }
        }
    }

    public void Remove(Action<string>? handler)
    {
        lock (_lock)
        {
            _handlers -= handler;
        }
    }

    public void Raise(string line)
    {
        lock (_lock)
        {
            if (_pending is null)
            {
                _handlers?.Invoke(line);
            }
            else if (_pending.Count < capacity)
            {
                _pending.Add(line);
            }
        }
    }
}
