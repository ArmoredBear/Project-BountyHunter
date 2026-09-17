using System.Collections.Generic;
using Godot;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                              CHAIN SHADER CONTROLLER
*-----------------------------------------------------------------------------------------------------------------------**/

/**-----------------------------------------------------------------------------------------------------------------------
	**                                                  PURPOSE
	*
	**  1 - Drive the chain shader's scroll offset from the visible inventory tab.
	**  2 - Scroll down shifts the chain up (inverted) and only while scrolling.
	**
*-----------------------------------------------------------------------------------------------------------------------**/

public partial class ChainShaderController : Control
{
    //!---------------------------------------------------------------------------------------------------------
    #region Variables
    //!---------------------------------------------------------------------------------------------------------

    private ShaderMaterial _material;
    private VScrollBar[] _scrollBars;
    private float _currentScroll;

    #endregion
    //!---------------------------------------------------------------------------------------------------------
    //!---------------------------------------------------------------------------------------------------------
    #region Initialization and Processes
    //!---------------------------------------------------------------------------------------------------------

    public override void _Ready()
    {
        _material = new ShaderMaterial();
        Material materialOverride = Material;
        if (materialOverride is ShaderMaterial sm)
        {
            _material = (ShaderMaterial)sm.Duplicate();
            Material = _material;
        }

        _scrollBars = FindScrollBars();
    }

    public override void _Process(double delta)
    {
        VScrollBar active = GetActiveScrollBar();
        float target = 0.0f;

        if (active != null)
        {
            float range = Mathf.Max((float)active.MaxValue, 1.0f);
            float normalized = (float)active.Value / range;
            target = 1.0f - normalized;
        }

        _currentScroll = Mathf.Lerp(_currentScroll, target, 10.0f * (float)delta);
        _material.SetShaderParameter("Scroll_Pos", _currentScroll);
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------
    //!---------------------------------------------------------------------------------------------------------
    #region Methods
    //!---------------------------------------------------------------------------------------------------------

    private VScrollBar GetActiveScrollBar()
    {
        foreach (VScrollBar sb in _scrollBars)
        {
            if (sb.GetParent<Control>().Visible)
            {
                return sb;
            }
        }
        return null;
    }

    private VScrollBar[] FindScrollBars()
    {
        List<VScrollBar> list = new List<VScrollBar>();
        Control panel = GetParent<Control>().GetParent<Control>();

        foreach (Node child in panel.GetChildren())
        {
            if (child is Control c && c.Name.ToString().StartsWith("ScrollContainer"))
            {
                VScrollBar sb = c.GetNodeOrNull<VScrollBar>("VScrollBar");
                if (sb != null)
                {
                    list.Add(sb);
                }
            }
        }
        return list.ToArray();
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------
}