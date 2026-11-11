using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>Loads PDF pages as textures on Quest; uses Poppler for editor previews.</summary>
public sealed class PitchPdfSource
{
    private string location;
    private bool androidUri;
    public int PageCount { get; private set; }

    public void Open(string pathOrUri, bool isAndroidUri)
    {
        location = pathOrUri;
        androidUri = isAndroidUri;
#if UNITY_ANDROID && !UNITY_EDITOR
        using (var activity = GetActivity())
        using (var bridge = new AndroidJavaClass("com.pitchvr.PdfBridge"))
            PageCount = bridge.CallStatic<int>("pageCount", activity, location);
#else
        string pdfinfo = FindTool("pdfinfo");
        string output = Run(pdfinfo, "\"" + location + "\"");
        Match match = Regex.Match(output, @"(?m)^Pages:\s+(\d+)");
        if (!match.Success) throw new InvalidOperationException("Could not read PDF page count.");
        PageCount = int.Parse(match.Groups[1].Value);
#endif
        if (PageCount < 1 || PageCount > 100)
            throw new InvalidOperationException("Choose a PDF with 1 to 100 pages.");
    }

    public Texture2D RenderPage(int index)
    {
        if (index < 0 || index >= PageCount) throw new ArgumentOutOfRangeException(nameof(index));
        byte[] png;
#if UNITY_ANDROID && !UNITY_EDITOR
        using (var activity = GetActivity())
        using (var bridge = new AndroidJavaClass("com.pitchvr.PdfBridge"))
        {
            string base64 = bridge.CallStatic<string>("renderPage", activity, location, index, 1024);
            png = Convert.FromBase64String(base64);
        }
#else
        string prefix = Path.Combine(Application.temporaryCachePath, "pitch_page_" + index);
        string command = "-f " + (index + 1) + " -l " + (index + 1) +
            " -singlefile -png -scale-to 1024 \"" + location + "\" \"" + prefix + "\"";
        Run(FindTool("pdftoppm"), command);
        string imagePath = prefix + ".png";
        png = File.ReadAllBytes(imagePath);
        File.Delete(imagePath);
#endif
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(png, true)) throw new InvalidOperationException("Could not decode PDF page.");
        texture.filterMode = FilterMode.Bilinear;
        return texture;
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static AndroidJavaObject GetActivity()
    {
        using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            return player.GetStatic<AndroidJavaObject>("currentActivity");
    }
#else
    private static string FindTool(string name)
    {
        string[] paths = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator);
        foreach (string directory in paths)
        {
            string candidate = Path.Combine(directory, name);
            if (File.Exists(candidate)) return candidate;
        }
        string bundled = "/Users/davidedoh/.cache/codex-runtimes/codex-primary-runtime/dependencies/bin/override/" + name;
        if (File.Exists(bundled)) return bundled;
        throw new FileNotFoundException("PDF preview requires Poppler's " + name + " on PATH.");
    }

    private static string Run(string tool, string args)
    {
        var process = new Process();
        process.StartInfo = new ProcessStartInfo(tool, args) {
            UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true,
            CreateNoWindow = true
        };
        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0) throw new InvalidOperationException(error);
        return output;
    }
#endif
}
