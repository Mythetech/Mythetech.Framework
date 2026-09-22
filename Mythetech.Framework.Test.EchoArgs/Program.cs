using System.Text;

// Base64 keeps each argument on one line regardless of newlines or console encoding,
// so tests can compare exactly what the process received.
foreach (var arg in args)
{
    Console.WriteLine("arg:" + Convert.ToBase64String(Encoding.UTF8.GetBytes(arg)));
}
