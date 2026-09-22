using System.Text;

namespace Mythetech.Framework.Test.Infrastructure.Shell;

/// <summary>
/// Splits a Windows argument string the way the MSVC runtime and CommandLineToArgvW do
/// for everything after the program name. Used to prove quoted values round-trip.
/// </summary>
internal static class WindowsCommandLine
{
    public static List<string> Parse(string commandLine)
    {
        var args = new List<string>();
        var i = 0;

        while (i < commandLine.Length)
        {
            while (i < commandLine.Length && commandLine[i] is ' ' or '\t')
                i++;

            if (i >= commandLine.Length)
                break;

            var current = new StringBuilder();
            var inQuotes = false;

            while (i < commandLine.Length)
            {
                var c = commandLine[i];

                if (!inQuotes && c is ' ' or '\t')
                    break;

                if (c == '\\')
                {
                    var start = i;
                    while (i < commandLine.Length && commandLine[i] == '\\')
                        i++;
                    var count = i - start;

                    if (i < commandLine.Length && commandLine[i] == '"')
                    {
                        current.Append('\\', count / 2);
                        if (count % 2 == 1)
                        {
                            current.Append('"');
                            i++;
                        }
                    }
                    else
                    {
                        current.Append('\\', count);
                    }

                    continue;
                }

                if (c == '"')
                {
                    if (inQuotes && i + 1 < commandLine.Length && commandLine[i + 1] == '"')
                    {
                        current.Append('"');
                        i += 2;
                        continue;
                    }

                    inQuotes = !inQuotes;
                    i++;
                    continue;
                }

                current.Append(c);
                i++;
            }

            args.Add(current.ToString());
        }

        return args;
    }
}
