using System.Runtime.CompilerServices;

namespace Mythetech.Framework.Utilities;

/// <summary>
/// Static utility for merging and deduplicating CSS class strings.
/// </summary>
public static class Css
{
    /// <summary>
    /// Merges CSS class strings, deduplicating and normalizing whitespace.
    /// Accepts any number of class strings, each of which may contain multiple space-separated classes.
    /// </summary>
    public static string Merge(params string?[] classes)
    {
        if (classes.Length == 0)
            return string.Empty;

        if (classes.Length == 1)
            return NormalizeSingle(classes[0]);

        return MergeCore(classes);
    }

    /// <summary>
    /// Conditionally includes classes based on boolean flags, then merges and deduplicates.
    /// </summary>
    public static string MergeIf(params (string? className, bool when)[] conditionals)
    {
        if (conditionals.Length == 0)
            return string.Empty;

        var active = new string?[conditionals.Length];
        var count = 0;
        foreach (var (className, when) in conditionals)
        {
            if (when)
                active[count++] = className;
        }

        if (count == 0)
            return string.Empty;

        return MergeCore(active.AsSpan(0, count));
    }

    private static string NormalizeSingle(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        if (!value.Contains(' '))
            return value;

        return MergeCore([value]);
    }

    private static string MergeCore(ReadOnlySpan<string?> classes)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var handler = new DefaultInterpolatedStringHandler();
        var first = true;

        foreach (var entry in classes)
        {
            if (string.IsNullOrWhiteSpace(entry))
                continue;

            foreach (var token in entry.AsSpan().Tokenize(' '))
            {
                if (token.IsEmpty || token.IsWhiteSpace())
                    continue;

                var cls = token.ToString();
                if (!seen.Add(cls))
                    continue;

                if (!first)
                    handler.AppendLiteral(" ");

                handler.AppendLiteral(cls);
                first = false;
            }
        }

        return handler.ToStringAndClear();
    }
}

internal static class SpanExtensions
{
    public static SpanTokenEnumerator Tokenize(this ReadOnlySpan<char> span, char separator)
        => new(span, separator);

    internal ref struct SpanTokenEnumerator
    {
        private ReadOnlySpan<char> _remaining;
        private readonly char _separator;
        private bool _started;

        public SpanTokenEnumerator(ReadOnlySpan<char> span, char separator)
        {
            _remaining = span;
            _separator = separator;
            Current = default;
            _started = false;
        }

        public ReadOnlySpan<char> Current { get; private set; }

        public SpanTokenEnumerator GetEnumerator() => this;

        public bool MoveNext()
        {
            if (!_started)
            {
                _started = true;
            }

            if (_remaining.IsEmpty)
            {
                Current = default;
                return false;
            }

            var idx = _remaining.IndexOf(_separator);
            if (idx < 0)
            {
                Current = _remaining;
                _remaining = default;
                return true;
            }

            Current = _remaining[..idx];
            _remaining = _remaining[(idx + 1)..];
            return true;
        }
    }
}
