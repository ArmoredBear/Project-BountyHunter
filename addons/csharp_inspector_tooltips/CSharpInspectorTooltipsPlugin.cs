#if TOOLS
using Godot;

namespace CSharpInspectorTooltips.Editor;

[Tool]
public partial class CSharpInspectorTooltipsPlugin : EditorPlugin
{
    private const double DocumentationCheckIntervalSeconds = 1.0;

    private CSharpDocumentationInspectorPlugin? _inspectorPlugin;
    private double _secondsUntilDocumentationCheck;

    public override void _EnterTree()
    {
        _inspectorPlugin = new CSharpDocumentationInspectorPlugin();
        AddInspectorPlugin(_inspectorPlugin);
    }

    public override void _Process(double delta)
    {
        _secondsUntilDocumentationCheck -= delta;
        if (_secondsUntilDocumentationCheck > 0 || _inspectorPlugin == null)
        {
            return;
        }

        _secondsUntilDocumentationCheck = DocumentationCheckIntervalSeconds;

        if (_inspectorPlugin.ReloadDocumentationIfChanged())
        {
            GodotObject? editedObject = EditorInterface.Singleton.GetInspector().GetEditedObject();
            if (editedObject != null)
            {
                EditorInterface.Singleton.InspectObject(editedObject);
            }
        }
    }

    public override void _ExitTree()
    {
        if (_inspectorPlugin == null)
        {
            return;
        }

        RemoveInspectorPlugin(_inspectorPlugin);
        _inspectorPlugin = null;
    }
}
#endif
