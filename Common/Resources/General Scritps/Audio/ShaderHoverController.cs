using Godot;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                   SHADER HOVER CONTROLLER
*-----------------------------------------------------------------------------------------------------------------------**/

/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	*
	**  1 - Companion controller for ShaderLoopVisualizer, mirroring
	**		MenuButtonHoverVisualizer so the shader version can be wired the same way.
	**  2 - Watches a list of Buttons; on hover it repositions the shader visualizer
	**		child to the hovered button's center and shows it, hiding it on leave.
	**
*-----------------------------------------------------------------------------------------------------------------------**/

public partial class ShaderHoverController : Node2D
{
    //!---------------------------------------------------------------------------------------------------------
    #region Variables
    //!---------------------------------------------------------------------------------------------------------

    // Buttons that trigger the visualizer when hovered.
    [Export] public Button[] Buttons { get; set; }

    // The ShaderLoopVisualizer that renders the effect (a child node).
    [Export] public ShaderLoopVisualizer Visualizer { get; set; }

    // The button currently hovered, if any.
    private Button _hovered;

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Initialization and Processes
    //!---------------------------------------------------------------------------------------------------------

    public override void _Ready()
    {
        base._Ready();

        // Hidden until the first button is hovered.
        if (Visualizer != null)
            Visualizer.HideVisualizer();

        if (Buttons == null) return;

        foreach (Button button in Buttons)
        {
            if (button == null) continue;

            button.MouseEntered += () => OnHoverEnter(button);
            button.MouseExited += () => OnHoverExit(button);
        }
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Methods
    //!---------------------------------------------------------------------------------------------------------

    private void OnHoverEnter(Button button)
    {
        _hovered = button;

        if (Visualizer == null || !IsInsideTree()) return;

        Visualizer.CenterOn(button.GetGlobalRect().GetCenter());
        Visualizer.ShowVisualizer();
    }

    private void OnHoverExit(Button button)
    {
        if (_hovered == button)
        {
            _hovered = null;

            if (Visualizer != null)
                Visualizer.HideVisualizer();
        }
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------
}