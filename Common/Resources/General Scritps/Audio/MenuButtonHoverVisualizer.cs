using Godot;
using System;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                   MENU BUTTON HOVER VISUALIZER
*-----------------------------------------------------------------------------------------------------------------------**/

/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	*  
	**  1 - Shared visualizer behind the main menu buttons.
	**  2 - Watches a list of Buttons; on hover it repositions the LoopVisualizer
	**		child to the hovered button's center and shows it, hiding it on leave.
	**  3 - A single instance serves every button, so no script is added to the
	**		buttons themselves (they already have their own scripts).
	*
*-----------------------------------------------------------------------------------------------------------------------**/

public partial class MenuButtonHoverVisualizer : Node2D
{
    //!---------------------------------------------------------------------------------------------------------
    #region Variables
    //!---------------------------------------------------------------------------------------------------------

    // Buttons that trigger the visualizer when hovered.
    [Export] public Button[] Buttons { get; set; }

    // The LoopVisualizer that does the actual drawing (a child node).
    [Export] public LoopVisualizer Visualizer { get; set; }

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

        // Wire every button's hover signals. Buttons keep their own scripts;
        // we only listen from here.
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

        // Center the visualizer on the hovered button (global/canvas coordinates,
        // matching the node's own canvas space).
        Visualizer.GlobalPosition = button.GetGlobalRect().GetCenter();
        Visualizer.ShowVisualizer();
    }

    private void OnHoverExit(Button button)
    {
        // Only hide when the button that owns the hover leaves.
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