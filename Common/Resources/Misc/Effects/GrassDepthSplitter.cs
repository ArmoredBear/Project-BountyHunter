using Godot;

/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   GRASS DEPTH SPLITTER
	*-----------------------------------------------------------------------------------------------------------------------**/

/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	*
	**  1 - Splits the procedural grass meadow into a back layer (drawn under
	**  the player) and a front layer (drawn over it), so tufts can occlude
	**  and be occluded by the player in 2D.
	**  2 - Each grass sprite receives its own runtime copy of the shared
	**  grass material locked to the matching depth_split_mode; every frame
	**  the split line follows the player's feet, so tufts cross layers as
	**  the player walks past them.
	*
*-----------------------------------------------------------------------------------------------------------------------**/

public partial class GrassDepthSplitter : Node
{
    //!-------------------------------------------------------------------------------------------------------------------
    #region Variables
    //!-------------------------------------------------------------------------------------------------------------------

    // The back layer sprite (drawn under the player).
    [ExportCategory("Depth Split")]
    [Export] public NodePath BackLayerPath;

    // The front layer sprite (drawn over the player).
    [Export] public NodePath FrontLayerPath;

    // Vertical offset added to the player's feet for the split line
    // (positive shifts the line downward, putting more grass in front).
    [Export(PropertyHint.Range, "-500,500")] public float SplitY_Offset = 0.0f;

    // Width of the empty rim kept inside the drawn outline (world units).
    // Tufts whose footprint lands within this distance of any polygon edge are
    // skipped entirely, so a hand-drawn grass shape gets a clean empty border
    // strip instead of hard-sliced tufts on the outline.
    [Export(PropertyHint.Range, "0,200")] public float BorderRadius = 32.0f;

    private Polygon2D _backLayer;
    private Polygon2D _frontLayer;
    private ShaderMaterial _backMaterial;
    private ShaderMaterial _frontMaterial;
    private CharacterBody2D _playerBody;

    #endregion
    //!-------------------------------------------------------------------------------------------------------------------

    //!-------------------------------------------------------------------------------------------------------------------
    #region Initialization and Processes
    //!-------------------------------------------------------------------------------------------------------------------

    public override void _Ready()
    {
        base._Ready();

        _backLayer = GetNodeOrNull<Polygon2D>(BackLayerPath);
        _frontLayer = GetNodeOrNull<Polygon2D>(FrontLayerPath);

        GD.Print("SPLIT ready: back=", _backLayer, " front=", _frontLayer,
            " backMat=", _backLayer?.Material, " frontMat=", _frontLayer?.Material,
            " backPath=", BackLayerPath, " frontPath=", FrontLayerPath);

        _backMaterial = SetupLayer(_backLayer, 1);
        _frontMaterial = SetupLayer(_frontLayer, 2);

        // Force the draw order so the back layer draws under the player
        // (player root z_index = 2) and the front layer draws over it.
        // The front sprite is a child of the back sprite, so both use
        // absolute z_index (z_as_relative = false) to avoid stacking.
        if (_backLayer != null)
        {
            _backLayer.ZIndex = 1;
            _backLayer.ZAsRelative = false;
        }
        if (_frontLayer != null)
        {
            _frontLayer.ZIndex = 3;
            _frontLayer.ZAsRelative = false;
        }

        GD.Print("SPLIT setup: backMatNull=", _backMaterial == null, " frontMatNull=", _frontMaterial == null);

        _playerBody = GetNodeOrNull<CharacterBody2D>("/root/Player/Player_Body");
        if (_playerBody == null)
        {
            Node playerRoot = GetNodeOrNull("/root/Player");
            if (playerRoot != null)
                _playerBody = playerRoot.GetNodeOrNull<CharacterBody2D>("Player_Body");
        }

        UploadOutline();

        UpdateSplitLine();
    }

    public override void _Process(double delta)
    {
        UpdateSplitLine();
    }

    #endregion
    //!-------------------------------------------------------------------------------------------------------------------

    //!-------------------------------------------------------------------------------------------------------------------
    #region Depth Split Logic
    //!-------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gives the sprite its own runtime copy of the shared grass material and
    /// locks it to the given depth_split_mode (1 = back, 2 = front).
    /// </summary>
    private ShaderMaterial SetupLayer(Polygon2D sprite, int mode)
    {
        if (sprite == null || !(sprite.Material is ShaderMaterial shared))
            return null;

        ShaderMaterial mat = (ShaderMaterial)shared.Duplicate();
        sprite.Material = mat;
        mat.SetShaderParameter("depth_split_mode", mode);
        return mat;
    }

    /// <summary>
    /// Converts the back layer's hand-drawn outline to world space, decimates
    /// it to at most 64 points to match the shader's fixed array, and uploads
    /// it (plus the border radius) into both grass materials. The shader then
    /// skips any tuft whose footprint crosses the outline or lands inside the
    /// border strip. With fewer than 3 points the count stays 0 and grass
    /// renders everywhere as before.
    /// </summary>
    private void UploadOutline()
    {
        if (_backLayer == null || _backLayer.Polygon == null || _backLayer.Polygon.Length < 3)
            return;

        // Snapshot the outline in world space: the polygon is stored in the
        // sprite's local coordinates, so each point is multiplied by the
        // sprite's global transform to reach world units (the space the
        // shader's world_pos and origin live in).
        Vector2[] local = _backLayer.Polygon;
        var world = new Vector2[local.Length];
        Transform2D xf = _backLayer.GlobalTransform;
        for (int i = 0; i < local.Length; i++)
            world[i] = xf * local[i];

        // Decimate to at most 64 points with a uniform stride, always keeping
        // the final point so the loop that treats it as closing back to point
        // 0 never sees a degenerate closing edge.
        const int MaxPoints = 64;
        var outline = new Godot.Collections.Array<Vector2>();
        if (world.Length > MaxPoints)
        {
            int stride = world.Length / MaxPoints + (world.Length % MaxPoints == 0 ? 0 : 1);
            for (int i = 0; i < world.Length; i += stride)
                outline.Add(world[i]);
            if (outline.Count < MaxPoints && outline[outline.Count - 1] != world[world.Length - 1])
                outline.Add(world[world.Length - 1]);
        }
        else
        {
            for (int i = 0; i < world.Length; i++)
                outline.Add(world[i]);
        }

        PushOutline(outline);
    }

    /// <summary>
    /// Pushes the same outline into both grass materials.
    /// </summary>
    private void PushOutline(Godot.Collections.Array<Vector2> outline)
    {
        int count = outline.Count;

        if (_backMaterial != null)
        {
            _backMaterial.SetShaderParameter("polygon_point_count", count);
            _backMaterial.SetShaderParameter("polygon_points", outline);
            _backMaterial.SetShaderParameter("border_radius", BorderRadius);
        }
        if (_frontMaterial != null)
        {
            _frontMaterial.SetShaderParameter("polygon_point_count", count);
            _frontMaterial.SetShaderParameter("polygon_points", outline);
            _frontMaterial.SetShaderParameter("border_radius", BorderRadius);
        }
    }

    /// <summary>
    /// Pushes the player's feet Y (plus the offset) into both grass materials
    /// so each tuft is drawn in the correct front/back layer.
    /// </summary>
    private void UpdateSplitLine()
    {
        if (_playerBody == null || !IsInstanceValid(_playerBody))
            return;

        float splitY = _playerBody.GlobalPosition.Y + SplitY_Offset;

        if (_backMaterial != null)
            _backMaterial.SetShaderParameter("depth_split_y", splitY);
        if (_frontMaterial != null)
            _frontMaterial.SetShaderParameter("depth_split_y", splitY);
    }

    #endregion
    //!-------------------------------------------------------------------------------------------------------------------
}
