#!/usr/bin/env dotnet
// Refreshes the Material Symbols Rounded font embedded in Mythetech.Framework.
//
// Google only serves the woff2 build to browser-like clients, so the CSS is
// requested with a desktop Chrome user agent. The font file is downloaded over
// the embedded copy and the version tag recorded in mythetech.css is updated so
// the diff shows which Google Fonts release the package now ships.
//
// Run it as ./scripts/update-material-symbols.cs, or as
// dotnet scripts/update-material-symbols.cs on Windows, where shebangs don't apply.
using System.Text.RegularExpressions;

const string GoogleCssUrl = "https://fonts.googleapis.com/css2?family=Material+Symbols+Rounded";
const string UserAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

var scriptDirectory = AppContext.GetData("EntryPointFileDirectoryPath") as string
    ?? throw new InvalidOperationException("Run this file with dotnet directly; it finds the repository from its own path.");
var webRoot = Path.GetFullPath(Path.Combine(scriptDirectory, "..", "Mythetech.Framework", "wwwroot"));
var cssFile = Path.Combine(webRoot, "mythetech.css");
var fontFile = Path.Combine(webRoot, "fonts", "material-symbols-rounded.woff2");

using var http = new HttpClient();
http.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);

var googleCss = await http.GetStringAsync(GoogleCssUrl);

var fontUrl = Regex.Match(googleCss, @"https://fonts\.gstatic\.com/[^)]*\.woff2").Value;
if (fontUrl.Length == 0)
{
    Console.Error.WriteLine("Could not find a woff2 url in the Google Fonts response");
    return 1;
}

var newVersion = Regex.Match(fontUrl, @"/(v\d+)/").Groups[1].Value;
if (newVersion.Length == 0)
{
    Console.Error.WriteLine($"Could not find a version in the font url {fontUrl}");
    return 1;
}

var versionTag = new Regex(@"(?<=material-symbols-version: )v\d+");
var css = await File.ReadAllTextAsync(cssFile);
var currentMatch = versionTag.Match(css);
var currentVersion = currentMatch.Success ? currentMatch.Value : "none";

Console.WriteLine($"Current embedded version: {currentVersion}");
Console.WriteLine($"Latest Google Fonts version: {newVersion}");

if (currentVersion == newVersion)
{
    Console.WriteLine("Already up to date");
    return 0;
}

var font = await http.GetByteArrayAsync(fontUrl);
await File.WriteAllBytesAsync(fontFile, font);
await File.WriteAllTextAsync(cssFile, versionTag.Replace(css, newVersion));

Console.WriteLine($"Updated {fontFile} to {newVersion} ({font.Length} bytes)");
return 0;
