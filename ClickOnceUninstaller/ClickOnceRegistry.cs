using System.Runtime.Versioning;
using Microsoft.Win32;
using System.Text;

namespace ClickOnceUninstaller;

[SupportedOSPlatform("windows")]
public class ClickOnceRegistry
{
    public const string ComponentsRegistryPath = @"Software\Classes\Software\Microsoft\Windows\CurrentVersion\Deployment\SideBySide\2.0\Components";
    public const string MarksRegistryPath = @"Software\Classes\Software\Microsoft\Windows\CurrentVersion\Deployment\SideBySide\2.0\Marks";

    public ClickOnceRegistry()
    {
        ReadComponents();
        ReadMarks();
    }

    private void ReadComponents()
    {
        Components = [];

        var components = Registry.CurrentUser.OpenSubKey(ComponentsRegistryPath);
        if (components == null)
        {
            return;
        }

        foreach (var keyName in components.GetSubKeyNames())
        {
            var componentKey = components.OpenSubKey(keyName);
            if (componentKey == null)
            {
                continue;
            }

            var component = new Component { Key = keyName };
            Components.Add(component);

            component.Dependencies = [];
            foreach (var dependencyName in componentKey.GetSubKeyNames().Where(n => n != "Files"))
            {
                component.Dependencies.Add(dependencyName);
            }
        }
    }

    private void ReadMarks()
    {
        Marks = [];

        var marks = Registry.CurrentUser.OpenSubKey(MarksRegistryPath);
        if (marks == null)
        {
            return;
        }

        foreach (var keyName in marks.GetSubKeyNames())
        {
            var markKey = marks.OpenSubKey(keyName);
            if (markKey == null)
            {
                continue;
            }

            var mark = new Mark { Key = keyName };
            Marks.Add(mark);

            if (markKey.GetValue("appid") is byte[] appid)
            {
                mark.AppId = Encoding.ASCII.GetString(appid);
            }

            if (markKey.GetValue("identity") is byte[] identity)
            {
                mark.Identity = Encoding.ASCII.GetString(identity);
            }

            mark.Implications = [];
            var implications = markKey.GetValueNames().Where(n => n.StartsWith("implication"));
            foreach (var implicationName in implications)
            {
                if (markKey.GetValue(implicationName) is byte[] implication)
                {
                    mark.Implications.Add(new Implication
                    {
                        Key = implicationName,
                        Name = implicationName.Substring(12),
                        Value = Encoding.ASCII.GetString(implication)
                    });
                }
            }
        }
    }

    public class RegistryKey
    {
        public string Key { get; set; }

        public override string ToString()
        {
            return Key ?? base.ToString();
        }
    }

    public class Component : RegistryKey
    {
        public List<string> Dependencies { get; set; }
    }

    public class Mark : RegistryKey
    {
        public string AppId { get; set; }

        public string Identity { get; set; }

        public List<Implication> Implications { get; set; }
    }

    public class Implication : RegistryKey
    {
        public string Name { get; set; }

        public string Value { get; set; }
    }

    public List<Component> Components { get; set; }

    public List<Mark> Marks { get; set; }
}