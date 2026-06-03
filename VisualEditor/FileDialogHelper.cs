using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace VisualEditor;

public static class FileDialogHelper
{
    public static string OpenFile(string title, string filter)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            string safeTitle = title.Replace("'", "");
            string safeFilter = filter.Replace("'", "");
            return RunBashCommand($"zenity --file-selection --title='{safeTitle}' --file-filter='{safeFilter}'");
        }
        
        Console.WriteLine($"Enter file path to load ({filter}):");
        return Console.ReadLine() ?? string.Empty;
    }

    public static string SaveFile(string title, string filter, string defaultName)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            string safeTitle = title.Replace("'", "");
            string safeName = defaultName.Replace("'", "");
            string safeFilter = filter.Replace("'", "");
            
            return RunBashCommand($"zenity --file-selection --save --confirm-overwrite --title='{safeTitle}' --filename='{safeName}' --file-filter='{safeFilter}'");
        }

        Console.WriteLine($"Enter file path to save ({defaultName}):");
        return Console.ReadLine() ?? string.Empty;
    }

    private static string RunBashCommand(string command)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{command}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            
            process.Start();
            string output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            return process.ExitCode == 0 ? output : string.Empty;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Dialog error: {ex.Message}");
            return string.Empty;
        }
    }
}