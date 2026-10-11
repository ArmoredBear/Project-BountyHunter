using Godot;

/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   VISIBILITY CULLER
	*-----------------------------------------------------------------------------------------------------------------------**/

/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	*
	**  1 - Hides a large shader-driven canvas item (sprite / polygon) when its
	**  world bounds fall outside the camera's view, so off-screen procedural
	**  effects such as the rain overlay stop paying per-pixel shader cost.
	**  2 - These items are huge sprites/polygons whose AABBs defeat the
	**  renderer's own item culling, so the check runs here on the CPU each
	**  frame, mirroring the grass meadow culling in GrassDepthSplitter.
	**
	**  Attach one per effect node and point TargetPath at the item to hide
	**  (leave empty to hide the node's own children / itself).
	*
*-----------------------------------------------------------------------------------------------------------------------**/

public partial class VisibilityCuller : Node2D
{
    //!-------------------------------------------------------------------------------------------------------------------
    #region Variables
    //!-------------------------------------------------------------------------------------------------------------------

    [ExportCategory("Visibility Culling")]
    [Export] public bool Cull_When_Offscreen = true;

    [Export(PropertyHint.Range, "0,2000,50")] public float Cull_Margin = 400.0f;

    /// <summary>The node whose world bounds are culled; leave empty to cull this node itself.</summary>
    [Export] public NodePath TargetPath;

    private Node2D _target;
    private Camera2D _worldCamera;
    private Rect2 _worldRect;
    private bool _measured;
    private bool _hidden;

    #endregion
    //!-------------------------------------------------------------------------------------------------------------------

    //!-------------------------------------------------------------------------------------------------------------------
    #region Initialization
    //!-------------------------------------------------------------------------------------------------------------------

    public override void _Ready()
    {
        base._Ready();

        _target = this;
        if (TargetPath != null && !TargetPath.IsEmpty)
        {
            Node2D resolved = GetNodeOrNull<Node2D>(TargetPath);
            if (resolved != null)
            {
                _target = resolved;
            }
        }

        _worldCamera = GetNodeOrNull<Camera2D>("/root/Player/Player_Body/Camera2D");
        if (_worldCamera == null)
        {
            Node playerRoot = GetNodeOrNull("/root/Player");
            if (playerRoot != null)
            {
                _worldCamera = playerRoot.GetNodeOrNull<Camera2D>("Player_Body/Camera2D");
            }
        }
    }

    #endregion
    //!-------------------------------------------------------------------------------------------------------------------

    //!-------------------------------------------------------------------------------------------------------------------
    #region Processes
    //!-------------------------------------------------------------------------------------------------------------------

    public override void _Process(double delta)
    {
        base._Process(delta);
        UpdateVisibility();
    }

    #endregion
    //!-------------------------------------------------------------------------------------------------------------------

    //!-------------------------------------------------------------------------------------------------------------------
    #region Visibility Culling
    //!-------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Hides the target whenever its world bounds stop intersecting the
    /// camera's visible world rect. The bounds are measured lazily on the
    /// first frame so the instanced scene's hierarchy is fully settled.
    /// </summary>
    private void UpdateVisibility()
    {
        if (!Cull_When_Offscreen || _target == null || !IsInstanceValid(_target) || _worldCamera == null)
        {
            _hidden = false;
            ApplyVisibility();
            return;
        }

        if (!_measured)
        {
            _worldRect = MeasureWorldRect(_target);
            _measured = true;
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

        _hidden = !_worldRect.Grow(Cull_Margin).Intersects(world_view_rect);
        ApplyVisibility();
    }

    /// <summary>
    /// Mirrors the current hidden state onto the target item.
    /// </summary>
    private void ApplyVisibility()
    {
        if (_target == null || !IsInstanceValid(_target))
        {
            return;
        }

        if (_target.Visible == _hidden)
        {
            _target.Visible = !_hidden;
        }
    }

    /// <summary>
    /// Measures the item's world-space bounds from its local rect (sprites
    /// report the texture rect; polygons report their outline) transformed by
    /// the node's global transform.
    /// </summary>
    private Rect2 MeasureWorldRect(Node2D item)
    {
        Rect2 local = new Rect2(new Vector2(-1, -1), new Vector2(2, 2));

        if (item is Sprite2D sprite)
        {
            local = sprite.GetRect();
        }
        else if (item is Polygon2D polygon && polygon.Polygon != null && polygon.Polygon.Length > 0)
        {
            Vector2[] points = polygon.Polygon;
            Vector2 first = points[0];
            Vector2 min = first;
            Vector2 max = first;

            for (int i = 1; i < points.Length; i++)
            {
                min = new Vector2(Mathf.Min(min.X, points[i].X), Mathf.Min(min.Y, points[i].Y));
                max = new Vector2(Mathf.Max(max.X, points[i].X), Mathf.Max(max.Y, points[i].Y));
            }

            local = new Rect2(min, max - min);
        }

        Transform2D xf = item.GlobalTransform;
        Vector2 local_end = local.Position + local.Size;
        Vector2 world_start = xf * local.Position;
        Vector2 world_end = xf * local_end;

        Vector2 world_min = new Vector2(Mathf.Min(world_start.X, world_end.X), Mathf.Min(world_start.Y, world_end.Y));
        Vector2 world_max = new Vector2(Mathf.Max(world_start.X, world_end.X), Mathf.Max(world_start.Y, world_end.Y));

        return new Rect2(world_min, world_max - world_min);
    }

    #endregion
    //!-------------------------------------------------------------------------------------------------------------------
}