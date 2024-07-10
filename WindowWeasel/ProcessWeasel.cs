using System.Diagnostics;

namespace WindowWeasel;

public class ProcessWeasel
{
    public static IWeaselWindow LaunchProcess(string path) =>
        ProcessWeasel.LaunchProcess(path, string.Empty);

    public static IWeaselWindow LaunchProcess(string path, params string[] arguments) =>
        ProcessWeasel.LaunchProcess(path, string.Join(' ', arguments));

    public static IWeaselWindow LaunchProcess(string path, string arguments) =>
        ProcessWeasel.LaunchProcess(new ProcessStartInfo() { FileName = path, Arguments = arguments });

    public static IWeaselWindow LaunchProcess(ProcessStartInfo startInfo)
    {
        var process = Process.Start(startInfo)!;
        return new WWindow(process);
    }
}
