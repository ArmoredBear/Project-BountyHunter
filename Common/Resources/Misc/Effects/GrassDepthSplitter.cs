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
	**  3 - Culls an entire meadow (both layers) on the CPU when its world
	**  bounds fall outside the camera's view, so off-screen grass never
	**  pays for the heavy per-pixel scatter shader. The meadows are huge
	**  polygons whose AABBs defeat the renderer's own item culling, so this
	**  check must be done in script.
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

    [ExportCategory("Visibility Culling")]
    [Export] public bool Cull_When_Offscreen = true;
    [Export(PropertyHint.Range, "0,2000,50")] public float Cull_Margin = 400.0f;

    private Polygon2D _backLayer;
    private Polygon2D _frontLayer;
    private ShaderMaterial _backMaterial;
    private ShaderMaterial _frontMaterial;
    private CharacterBody2D _playerBody;
    private Camera2D _worldCamera;
    private Rect2 _meadowWorldRect;
    private bool _meadowMeasured;
    private bool _meadowHidden;

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

        _worldCamera = GetNodeOrNull<Camera2D>("/root/Player/Player_Body/Camera2D");
        if (_worldCamera == null)
        {
            Node playerRoot = GetNodeOrNull("/root/Player");
            if (playerRoot != null)
                _worldCamera = playerRoot.GetNodeOrNull<Camera2D>("Player_Body/Camera2D");
        }

        UploadOutline();

        UpdateVisibility();
        UpdateSplitLine();
    }

    public override void _Process(double delta)
    {
        UpdateVisibility();

        if (!_meadowHidden)
        {
            UpdateSplitLine();
        }
    }

    #endregion
    //!-------------------------------------------------------------------------------------------------------------------

    //!-------------------------------------------------------------------------------------------------------------------
    #region Visibility Culling
    //!-------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Hides this meadow's layers whenever its world bounds stop intersecting
    /// the camera's visible world rect. The meadow rect is measured lazily on
    /// the first frame so the instanced scene's hierarchy is fully settled.
    /// </summary>
    private void UpdateVisibility()
    {
        if (!Cull_When_Offscreen || _backLayer == null || _worldCamera == null)
        {
            _meadowHidden = false;
            ApplyMeadowVisibility();
            return;
        }

        if (!_meadowMeasured)
        {
            _meadowWorldRect = ComputeMeadowWorldRect(_backLayer);
            _meadowMeasured = true;
        }

        Vector2 zoom = _worldCamera.Zoom;
        if (zoom.X <= 0.0001f || zoom.Y <= 0.0001f)
        {
            return;
        }

        Rect2 viewport_rect = _worldCamera.GetViewportRect();
        Vector2 world_view_size = viewport_rect.Size / zoom;
        Vector2 world_view_center = _worldCamera.GetScreenCenterPosition();
        Rect2 world_view_rect = new Rect2(world_view_center - world_view_size * 0.5f, world_view_size);

        _meadowHidden = !_meadowWorldRect.Grow(Cull_Margin).Intersects(world_view_rect);
        ApplyMeadowVisibility();
    }

    /// <summary>
    /// Mirrors the current hidden state onto both grass layers.
    /// </summary>
    private void ApplyMeadowVisibility()
    {
        if (_backLayer != null && _backLayer.Visible == _meadowHidden)
        {
            _backLayer.Visible = !_meadowHidden;
        }
        if (_frontLayer != null && _frontLayer.Visible == _meadowHidden)
        {
            _frontLayer.Visible = !_meadowHidden;
        }
    }

    /// <summary>
    /// Measures the meadow's world-space bounds from the back layer's polygon
    /// (the front layer shares the same outline and lives inside it).
    /// </summary>
    private Rect2 ComputeMeadowWorldRect(Polygon2D back)
    {
        Vector2[] polygon = back.Polygon;
        if (polygon == null || polygon.Length == 0)
        {
            return new Rect2(back.GlobalPosition, Vector2.One);
        }

        Transform2D xf = back.GlobalTransform;
        Vector2 first = xf * polygon[0];
        float min_x = first.X;
        float max_x = first.X;
        float min_y = first.Y;
        float max_y = first.Y;

        for (int i = 1; i < polygon.Length; i++)
        {
            Vector2 p = xf * polygon[i];
            min_x = Mathf.Min(min_x, p.X);
            max_x = Mathf.Max(max_x, p.X);
            min_y = Mathf.Min(min_y, p.Y);
            max_y = Mathf.Max(max_y, p.Y);
        }

        return new Rect2(min_x, min_y, max_x - min_x, max_y - min_y);
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
