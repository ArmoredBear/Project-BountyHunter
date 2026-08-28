using Godot;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                   SHADER LOOP VISUALIZER
*-----------------------------------------------------------------------------------------------------------------------**/

/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	*
	**  1 - Full-shader alternative to LoopVisualizer: a single ColorRect with a
	**		canvas_item shader draws every bar, tip, glow, center line and the
	**		rise/fall envelope. There is no per-bar CPU work and no _Draw() polygons.
	**  2 - Being a Control (ColorRect) it slots cleanly into the menu's Control tree.
	**  3 - Practically identical exports and ShowVisualizer/HideVisualizer as
	**		LoopVisualizer, so it can be swapped in and compared.
	**  4 - Per frame the shell only pushes one time uniform (u_now) so the shader
	**		can animate the loop scroll and the rise/fall exponential envelope.
	*
*-----------------------------------------------------------------------------------------------------------------------**/

public partial class ShaderLoopVisualizer : ColorRect
{
    //!---------------------------------------------------------------------------------------------------------
    #region Variables
    //!---------------------------------------------------------------------------------------------------------

    private const string ShaderPath = "res://Common/Resources/General Scritps/Audio/ShaderLoopVisualizer.gdshader";

    private ShaderMaterial _material;
    private bool _hovered;

    [Export] public int NumBars = 64;                // Bars per mirrored side
    [Export] public float VisualizerWidth = 328f;    // Total width in px, always exact
    [Export] public float BarWidth = 4f;             // Thickness when there's room for it
    [Export] public float PointRatio = 1f;           // Tip fraction of each bar (1 = pure triangle)
    [Export] public float CenterGap = 6f;            // Empty strip around the center line
    [Export] public float LoopSpeed = 1f;            // Loop indices advanced per second
    [Export] public float Amplitude = 1f;            // Global bar-height scale
    [Export] public float MaxHeightRatio = 0.22f;    // Max bar height as a fraction of screen height
    [Export] public float Seed = 7f;                 // Seed for the seamless random loop
    [Export] public Color BarColor = new Color(0.898f, 0.839f, 0.612f, 0.588f);

    [Export] public bool GlowEnabled = false;        // Halo + inner overlay (matches LoopVisualizer gating)
    [Export] public float GlowExpand = 2f;
    [Export] public Color GlowColor = new Color(0.09f, 0.086f, 0.061f, 1f);
    [Export] public float InnerGlowShrink = 1f;
    [Export] public float InnerGlowHeightRatio = 0.3f;
    [Export] public Color InnerGlowColor = new Color(1f, 0.976f, 0.878f, 0.51f);

    [Export] public bool CenterLineEnabled = false;
    [Export] public float CenterLineWidth = 1.5f;
    [Export] public Color CenterLineColor = new Color(0.91f, 0.874f, 0.732f, 1f);

    [Export] public float FadeInSpeed = 10f;         // Bar rise cadence (activity attack)
    [Export] public float FadeOutSpeed = 10f;        // Bar fall cadence (activity release)

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Initialization and Processes
    //!---------------------------------------------------------------------------------------------------------

    public override void _Ready()
    {
        base._Ready();

        // Fully transparent base so nothing renders before the shader kicks in.
        Color = new Color(0f, 0f, 0f, 0f);

        // The visualizer must never swallow mouse input meant for the buttons.
        MouseFilter = Control.MouseFilterEnum.Ignore;

        _material = new ShaderMaterial
        {
            Shader = GD.Load<Shader>(ShaderPath)
        };
        Material = _material;

        GetViewport().SizeChanged += OnViewportSizeChanged;
        OnViewportSizeChanged();

        PushStaticUniforms();
        PushEmphasisUniforms(_hovered);
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        var viewport = GetViewport();
        if (viewport != null)
            viewport.SizeChanged -= OnViewportSizeChanged;
    }

    // Called every frame. Only work of the whole node: forward the current wall
    // clock so the shader can animate. Single scalar, no allocations.
    public override void _Process(double delta)
    {
        base._Process(delta);
        if (_material != null)
            _material.SetShaderParameter("u_now", CurrentTimeSeconds());
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Public API
    //!---------------------------------------------------------------------------------------------------------

    // Positions the rect so its center sits on 'globalCenter' (Control space).
    // ColorRect's position is its top-left corner, hence the half-size offset.
    public void CenterOn(Vector2 globalCenter)
    {
        GlobalPosition = globalCenter - Size * 0.5f;
    }

    // Fades the visualizer in (bars rise from 0). Called by the hover controller.
    public void ShowVisualizer()
    {
        _hovered = true;
        PushEmphasisUniforms(_hovered);
    }

    // Fades the visualizer out (bars sink to 0). The shader's activity envelope
    // handles the animation, so the node stays visible while alpha falls.
    public void HideVisualizer()
    {
        _hovered = false;
        PushEmphasisUniforms(_hovered);
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Internal
    //!---------------------------------------------------------------------------------------------------------

    private void OnViewportSizeChanged()
    {
        float viewportHeight = GetViewportRect().Size.Y;
        if (viewportHeight <= 1f) return;

        if (_material != null)
            _material.SetShaderParameter("u_viewport_height", viewportHeight);

        float wid = Mathf.Max(VisualizerWidth, 1f);
        float hgt = Mathf.Max(viewportHeight * MaxHeightRatio, 1f);
        Size = new Vector2(wid, hgt);
    }

    private void PushStaticUniforms()
    {
        if (_material == null) return;

        _material.SetShaderParameter("u_num_bars", NumBars);
        _material.SetShaderParameter("u_visualizer_width", Mathf.Max(VisualizerWidth, 1f));
        _material.SetShaderParameter("u_bar_width", Mathf.Max(BarWidth, 0.001f));
        _material.SetShaderParameter("u_point_ratio", PointRatio);
        _material.SetShaderParameter("u_center_gap", CenterGap);
        _material.SetShaderParameter("u_loop_speed", LoopSpeed);
        _material.SetShaderParameter("u_amplitude", Amplitude);
        _material.SetShaderParameter("u_max_height_ratio", MaxHeightRatio);
        _material.SetShaderParameter("u_seed", Seed);
        _material.SetShaderParameter("u_bar_color", BarColor);

        _material.SetShaderParameter("u_glow_enabled", GlowEnabled);
        _material.SetShaderParameter("u_glow_expand", GlowExpand);
        _material.SetShaderParameter("u_glow_color", GlowColor);
        _material.SetShaderParameter("u_inner_shrink", InnerGlowShrink);
        _material.SetShaderParameter("u_inner_ratio", InnerGlowHeightRatio);
        _material.SetShaderParameter("u_inner_color", InnerGlowColor);

        _material.SetShaderParameter("u_center_line_enabled", CenterLineEnabled);
        _material.SetShaderParameter("u_center_line_width", Mathf.Max(CenterLineWidth, 0.001f));
        _material.SetShaderParameter("u_center_line_color", CenterLineColor);

        _material.SetShaderParameter("u_rise_speed", FadeInSpeed);
        _material.SetShaderParameter("u_fall_speed", FadeOutSpeed);
    }

    private void PushEmphasisUniforms(bool hovered)
    {
        if (_material == null) return;

        float now = CurrentTimeSeconds();
        _material.SetShaderParameter("u_hovered", hovered ? 1f : 0f);
        _material.SetShaderParameter("u_change_time", now);
        _material.SetShaderParameter("u_now", now);
    }

    private static float CurrentTimeSeconds()
    {
        return Time.GetTicksMsec() * 0.001f;
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------
}