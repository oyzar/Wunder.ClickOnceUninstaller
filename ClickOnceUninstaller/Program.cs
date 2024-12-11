using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;

namespace ClickOnceUninstaller;

[SupportedOSPlatform("windows")]
class Program
{

    static void Main(string[] args)
    {
        if (args.Length != 2 || string.IsNullOrEmpty(args[0]) || string.IsNullOrEmpty(args[1]))
        {
            Console.WriteLine("Usage:\nClickOnceUninstaller displayName appName.exe");
            return;
        }

        var appName = args[0];

        var uninstallInfo = UninstallInfo.Find(appName);
        if (uninstallInfo == null)
        {
            Console.WriteLine("Could not find application \"{0}\"", appName);
            return;
        }

        Console.WriteLine("Uninstalling application \"{0}\"", appName);
        //replace with a non-dummy logger
        var logger = LoggerFactory.Create(_ => {}).CreateLogger("Uninstaller");
        var mainProcessName = args[0];
        var uninstaller = new Uninstaller(logger, mainProcessName);
        uninstaller.Uninstall(uninstallInfo);

        Console.WriteLine("Uninstall complete");
    }
}