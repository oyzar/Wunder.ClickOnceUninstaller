An uninstaller for ClickOnce applications
===========================

### Why?

Apparently, ClickOnce installations can't be removed silently. The ClickOnce uninstaller always shows a "Maintainance" dialog, requiring user interaction. 

### What?

The Wunder.ClickOnceUninstaller uninstaller imitates the actions performed by the ClickOnce uninstaller, removing files, registry entries, start menu and desktop links for a given application. 

It automatically resolves dependencies between installed components and removes all of the applications's components which are not required by other installed ClickOnce applications.

The uninstaller can be used programmatically as .NET library, through a command line interface. 

### How?

##### .NET

    ILogger logger;
    var uninstallInfo = UninstallInfo.Find("Application Name");
    if (uninstallInfo == null)
    {
        var uninstaller = new Uninstaller(logger, "App.exe");
        uninstaller.Uninstall(uninstallInfo);
    }

##### Command-line

    ClickOnceUninstaller.exe "Application Name" App.exe

## License

The source code is available under the [MIT license](http://opensource.org/licenses/mit-license.php).
