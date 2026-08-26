#if TOOLS
using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace CSharpInspectorTooltips.Editor;

[Tool]
public partial class CSharpDocumentationInspectorPlugin : EditorInspectorPlugin
{
    private readonly Dictionary<string, Dictionary<string, string>> _documentation = new();
    private readonly Dictionary<string, string> _currentTooltips = new();
    private readonly string _xmlPath;
    private DateTime _lastWriteTimeUtc;

    public CSharpDocumentationInspectorPlugin()
    {
        string assemblyName = Assembly.GetExecutingAssembly().GetName().Name ?? string.Empty;
        _xmlPath = ProjectSettings.GlobalizePath(
            $"res://.godot/mono/temp/bin/Debug/{assemblyName}.xml");
        ReloadDocumentationIfChanged();
    }

    public bool ReloadDocumentationIfChanged()
    {
        if (!File.Exists(_xmlPath))
        {
            return false;
        }

        DateTime writeTimeUtc = File.GetLastWriteTimeUtc(_xmlPath);
        if (writeTimeUtc == _lastWriteTimeUtc)
        {
            return false;
        }

        return TryLoadDocumentation(writeTimeUtc);
    }

    public override bool _CanHandle(GodotObject @object)
    {
        return TryGetCSharpClassName(@object, out string className) &&
               _documentation.ContainsKey(className);
    }

    public override bool _ParseProperty(
        GodotObject @object,
        Variant.Type type,
        string name,
        PropertyHint hintType,
        string hintString,
        PropertyUsageFlags usageFlags,
        bool wide)
    {
        if (TryGetCSharpClassName(@object, out string className) &&
            TryGetDescription(className, name, out string description))
        {
            _currentTooltips[NormalizeName(name)] = description;
        }

        return false;
    }

    public override void _ParseBegin(GodotObject @object)
    {
        _currentTooltips.Clear();
    }

    public override void _ParseEnd(GodotObject @object)
    {
        if (_currentTooltips.Count == 0)
        {
            return;
        }

        var tooltips = new Dictionary<string, string>(_currentTooltips);
        Callable.From(() =>
            Callable.From(() => ApplyTooltips(tooltips)).CallDeferred()
        ).CallDeferred();
    }

    private void ApplyTooltips(Dictionary<string, string> tooltips)
    {
        EditorInspector inspector = EditorInterface.Singleton.GetInspector();
        ApplyTooltipsRecursive(inspector, tooltips);
    }

    private static void ApplyTooltipsRecursive(
        Node node,
        Dictionary<string, string> tooltips)
    {
        if (node is EditorProperty editorProperty)
        {
            string propertyName = editorProperty.GetEditedProperty().ToString();

            if (tooltips.TryGetValue(NormalizeName(propertyName), out string? description))
            {
                SetTooltipRecursive(editorProperty, description);
            }
        }

        foreach (Node child in node.GetChildren())
        {
            ApplyTooltipsRecursive(child, tooltips);
        }
    }

    private static void SetTooltipRecursive(Node node, string description)
    {
        if (node is Control control)
        {
            control.TooltipText = description;
        }

        foreach (Node child in node.GetChildren())
        {
            SetTooltipRecursive(child, description);
        }
    }

    private bool TryGetDescription(string className, string propertyName, out string description)
    {
        description = string.Empty;

        if (!_documentation.TryGetValue(className, out Dictionary<string, string>? properties) ||
            !properties.TryGetValue(NormalizeName(propertyName), out string? foundDescription) ||
            foundDescription == null)
        {
            return false;
        }

        description = foundDescription;
        return true;
    }

    private static bool TryGetCSharpClassName(GodotObject @object, out string className)
    {
        className = string.Empty;
        Variant scriptVariant = @object.GetScript();

        if (scriptVariant.VariantType != Variant.Type.Object)
        {
            return false;
        }

        Script? script = scriptVariant.AsGodotObject() as Script;
        string scriptPath = script?.ResourcePath ?? string.Empty;

        if (!scriptPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        className = Path.GetFileNameWithoutExtension(scriptPath);
        return !string.IsNullOrWhiteSpace(className);
    }

    private bool TryLoadDocumentation(DateTime writeTimeUtc)
    {
        try
        {
            XDocument document = XDocument.Load(_xmlPath);
            var loadedDocumentation = new Dictionary<string, Dictionary<string, string>>();

            foreach (XElement member in document.Descendants("member"))
            {
                string? memberId = member.Attribute("name")?.Value;
                XElement? summary = member.Element("summary");

                if (string.IsNullOrWhiteSpace(memberId) || summary == null)
                {
                    continue;
                }

                if (!memberId.StartsWith("F:", StringComparison.Ordinal) &&
                    !memberId.StartsWith("P:", StringComparison.Ordinal))
                {
                    continue;
                }

                if (!TrySplitMemberId(memberId[2..], out string className, out string propertyName))
                {
                    continue;
                }

                string description = NormalizeDescription(summary.Value);
                if (description.Length == 0)
                {
                    continue;
                }

                if (!loadedDocumentation.TryGetValue(className, out Dictionary<string, string>? properties))
                {
                    properties = new Dictionary<string, string>();
                    loadedDocumentation[className] = properties;
                }

                properties[NormalizeName(propertyName)] = description;
            }

            _documentation.Clear();
            foreach ((string className, Dictionary<string, string> properties) in loadedDocumentation)
            {
                _documentation[className] = properties;
            }

            _lastWriteTimeUtc = writeTimeUtc;
            return true;
        }
        catch (Exception exception)
        {
            GD.PushError($"C# Inspector Tooltips: failed to read XML documentation: {exception.Message}");
            return false;
        }
    }

    private static bool TrySplitMemberId(
        string memberId,
        out string className,
        out string propertyName)
    {
        className = string.Empty;
        propertyName = string.Empty;

        int propertySeparator = memberId.LastIndexOf('.');
        if (propertySeparator <= 0 || propertySeparator == memberId.Length - 1)
        {
            return false;
        }

        propertyName = memberId[(propertySeparator + 1)..];
        string typeName = memberId[..propertySeparator];
        int namespaceSeparator = typeName.LastIndexOf('.');
        className = namespaceSeparator >= 0
            ? typeName[(namespaceSeparator + 1)..]
            : typeName;

        return className.Length > 0 && propertyName.Length > 0;
    }

    private static string NormalizeDescription(string description)
    {
        string[] lines = description
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n')
            .Select(NormalizeLine)
            .ToArray();

        int firstContentLine = Array.FindIndex(lines, line => line.Length > 0);
        if (firstContentLine < 0)
        {
            return string.Empty;
        }

        int lastContentLine = Array.FindLastIndex(lines, line => line.Length > 0);
        return string.Join("\n", lines[firstContentLine..(lastContentLine + 1)]);
    }

    private static string NormalizeLine(string line)
    {
        return string.Join(
            " ",
            line.Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private static string NormalizeName(string value)
    {
        return new string(value
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());
    }
}
#endif
