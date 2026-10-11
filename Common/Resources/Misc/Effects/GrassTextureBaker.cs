using Godot;
using System;
using System.Collections.Generic;

/**-----------------------------------------------------------------------------------------------------------------------
	**                                                    GRASS TEXTURE BAKER
	*-----------------------------------------------------------------------------------------------------------------------**/

/**-----------------------------------------------------------------------------------------------------------------------
	**                                                    PURPOSE
	*
	**  Bakes the whole meadow once into a single texture by rendering the REAL
	**  GrassRepeat.gdshader into an offscreen SubViewport at startup, then
	**  swapping the captured frame onto a plain Sprite2D. From then on the
	**  meadow costs exactly one textured draw call per frame instead of the
	**  procedural per-pixel scatter (3x3 cell search + per-tuft O(64) border).
	**  The look is pixel-identical to the procedural shader by construction,
	**  because the bake is the shader — no C# re-implementation of the noise,
	**  tinting or border math that could drift.
	**
	**  The capture repeats the production setup: the meadow polygon drawn with
	**  a duplicate of the source grass material, its border outline decimated
	**  and uploaded (polygon_points / polygon_point_count / border_radius), and
	**  a warm PointLight2D (radial falloff generated at runtime) so the tufts
	**  come out lit exactly like the forest meadow. The depth split is skipped
	**  in this prototype on purpose; it can return later as per-texel attribute
	**  channels if the baked approach proves out.
	**
	**  TEXEL DENSITY. Capture scale is TexelsPerWorld texels per world unit.
	**  Higher = crisper blades but heavier VRAM; ~0.7 keeps the tufts sharp at
	**  play zoom while staying well under a hundred MB for the whole meadow.
	*-----------------------------------------------------------------------------------------------------------------------**/

public partial class GrassTextureBaker : Node2D
{
    /*- Public exports ------------------------------------------------------------------*/

    /// <summary>
    /// The Polygon2D that defines the meadow outline (world-space placement).
    /// Its polygon is captured and also drives the baked border rim.
    /// </summary>
    [Export]
    public NodePath OutlinePolygonPath { get; set; }

    /// <summary>
    /// The Sprite2D that receives the baked texture; after the bake it draws
    /// the whole meadow at the exact world rect captured up front.
    /// </summary>
    [Export]
    public NodePath GrassSpritePath { get; set; }

    /// <summary>
    /// The procedural grass material (Grass_Repeat_Material.tres) whose shader
    /// and parameters are captured.
    /// </summary>
    [Export]
    public ShaderMaterial SourceMaterial { get; set; }

    /// <summary>
    /// Empty rim left bare between the outline and the nearest tuft, same
    /// semantic as the splitter's border_radius upload.
    /// </summary>
    [Export]
    public float BorderRadius { get; set; } = 32.0f;

    /// <summary>
    /// Bake resolution: texels per world-space unit. Larger is sharper.
    /// </summary>
    [Export(PropertyHint.Range, "0.2,2.0,0.05")]
    public float TexelsPerWorld { get; set; } = 0.7f;

    /*- Baker state ---------------------------------------------------------------------*/

    /// <summary>
    /// Snapshot of the outline in world space (the polygon is stored in the
    /// node's local coordinates) and its decimated form for the border.
    /// </summary>
    private Vector2[] _outlineWorld;
    private Godot.Collections.Array<Vector2> _outlineDecimated;

    private Rect2 _meadowRect;

    // Public stats, read after the bake for verification scripts.
    public Vector2I LastTextureSize { get; private set; }
    public bool HasBaked { get; private set; }

    //!-------------------------------------------------------------------------------------------------------------------
    #region Bake
    //!-------------------------------------------------------------------------------------------------------------------

    public override async void _Ready()
    {
        SnapshotOutline();
        if (_outlineWorld == null || _outlineWorld.Length < 3)
        {
            GD.PrintErr("GRASS TEX BAKE FAIL: no outline polygon at ", OutlinePolygonPath);
            return;
        }

        System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();

        Vector2 texSize = _meadowRect.Size * TexelsPerWorld;
        Vector2I texPx = new Vector2I(Mathf.Max(1, Mathf.CeilToInt(texSize.X)),
                                      Mathf.Max(1, Mathf.CeilToInt(texSize.Y)));

        SubViewport viewport = new SubViewport
        {
            Size = texPx,
            TransparentBg = true,
            Disable3D = true,
        };
        viewport.Set("render_target_update_mode", 3); // UpdateAlways
        AddChild(viewport);

        // The capture polygon is the outline shifted so its AABB min corner
        // lands on viewport pixel (0, 0); the SubViewport maps canvas world
        // pixels 1:1 (origin top-left), so this fills the texture exactly.
        Vector2 shift = _meadowRect.Position;
        var capturePoly = new Vector2[_outlineWorld.Length];
        for (int i = 0; i < _outlineWorld.Length; i++)
            capturePoly[i] = _outlineWorld[i] - shift;

        ShaderMaterial captureMaterial = (ShaderMaterial)SourceMaterial.Duplicate();
        captureMaterial.SetShaderParameter("depth_split_mode", 0);
        captureMaterial.SetShaderParameter("polygon_point_count", _outlineDecimated.Count);
        captureMaterial.SetShaderParameter("polygon_points", _outlineDecimated == null ? null : ShiftOutline(_outlineDecimated, shift));
        captureMaterial.SetShaderParameter("border_radius", BorderRadius);

        var meadowPoly = new Polygon2D
        {
            Polygon = capturePoly,
            Material = captureMaterial,
        };
        viewport.AddChild(meadowPoly);

        // A warm light so the baked tufts come out shaded like the forest
        // meadow. Its radial falloff is generated at runtime; a light with no
        // texture would not render anything.
        var light = new PointLight2D
        {
            Position = new Vector2(texSize.X * 0.5f, texSize.Y * 0.5f),
            Texture = BuildRadialLightTexture(),
            TextureScale = 2.5f,
            Energy = 1.4f,
            Color = new Color(1.0f, 0.92f, 0.78f),
        };
        viewport.AddChild(light);

        // Let the viewport draw a frame, then lift its pixels.
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Image image = viewport.GetTexture().GetImage();
        if (image == null || image.GetSize().X == 0)
        {
            GD.PrintErr("GRASS TEX BAKE FAIL: empty capture");
            viewport.QueueFree();
            return;
        }

        ImageTexture baked = ImageTexture.CreateFromImage(image);
        viewport.QueueFree();

        Sprite2D sprite = GetNodeOrNull<Sprite2D>(GrassSpritePath);
        if (sprite == null)
        {
            GD.PrintErr("GRASS TEX BAKE FAIL: no sprite at ", GrassSpritePath);
            return;
        }
        sprite.Texture = baked;
        sprite.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
        sprite.Centered = false;
        sprite.Position = _meadowRect.Position;
        sprite.Scale = _meadowRect.Size / new Vector2(image.GetWidth(), image.GetHeight());
        sprite.ZIndex = 1;
        sprite.ZAsRelative = false;

        watch.Stop();
        LastTextureSize = new Vector2I(image.GetWidth(), image.GetHeight());
        HasBaked = true;

        GD.Print("GRASS TEX BAKE OK px=", image.GetWidth(), "x", image.GetHeight(),
            " world=", _meadowRect.Size, " tex_per_world=", TexelsPerWorld,
            " ms=", watch.ElapsedMilliseconds);
    }

    /// <summary>
    /// Shifts the decimated outline by the capture offset so the material's
    /// border math runs in the same space as the captured polygon.
    /// </summary>
    private Godot.Collections.Array<Vector2> ShiftOutline(Godot.Collections.Array<Vector2> outline, Vector2 shift)
    {
        var shifted = new Godot.Collections.Array<Vector2>();
        for (int i = 0; i < outline.Count; i++)
            shifted.Add(outline[i] - shift);
        return shifted;
    }

    /// <summary>
    /// A white radial gradient, transparent at the rim, for the bake light.
    /// </summary>
    private static ImageTexture BuildRadialLightTexture()
    {
        const int side = 128;
        int radius = side / 2;
        var img = Image.CreateEmpty(side, side, false, Image.Format.Rgba8);
        for (int y = 0; y < side; y++)
        {
            for (int x = 0; x < side; x++)
            {
                float dx = x - radius + 0.5f;
                float dy = y - radius + 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) / radius;
                float a = Mathf.Clamp(1.0f - d, 0.0f, 1.0f);
                a *= a;
                img.SetPixel(x, y, new Color(1.0f, 1.0f, 1.0f, a));
            }
        }
        return ImageTexture.CreateFromImage(img);
    }

    //!-------------------------------------------------------------------------------------------------------------------
    #endregion
    //!-------------------------------------------------------------------------------------------------------------------

    //!-------------------------------------------------------------------------------------------------------------------
    #region Outline
    //!-------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Snapshots the outline polygon in world space and decimates it to at
    /// most 64 points, mirroring GrassDepthSplitter.UploadOutline.
    /// </summary>
    private void SnapshotOutline()
    {
        Polygon2D outlineNode = GetNodeOrNull<Polygon2D>(OutlinePolygonPath);
        Vector2[] local = outlineNode?.Polygon;
        if (local == null || local.Length < 3)
        {
            _outlineWorld = null;
            return;
        }

        Transform2D xf = outlineNode.GlobalTransform;
        _outlineWorld = new Vector2[local.Length];
        for (int i = 0; i < local.Length; i++)
            _outlineWorld[i] = xf * local[i];

        Vector2 first = _outlineWorld[0];
        float minX = first.X, maxX = first.X, minY = first.Y, maxY = first.Y;
        for (int i = 1; i < _outlineWorld.Length; i++)
        {
            minX = Mathf.Min(minX, _outlineWorld[i].X);
            maxX = Mathf.Max(maxX, _outlineWorld[i].X);
            minY = Mathf.Min(minY, _outlineWorld[i].Y);
            maxY = Mathf.Max(maxY, _outlineWorld[i].Y);
        }
        _meadowRect = new Rect2(new Vector2(minX, minY), new Vector2(maxX - minX, maxY - minY));

        // Decimate to at most 64 points with a uniform stride, always keeping
        // the final point so the closing edge back to point 0 stays valid.
        const int MaxPoints = 64;
        _outlineDecimated = new Godot.Collections.Array<Vector2>();
        if (_outlineWorld.Length > MaxPoints)
        {
            int stride = _outlineWorld.Length / MaxPoints + (_outlineWorld.Length % MaxPoints == 0 ? 0 : 1);
            for (int i = 0; i < _outlineWorld.Length; i += stride)
                _outlineDecimated.Add(_outlineWorld[i]);
            Vector2 last = _outlineWorld[_outlineWorld.Length - 1];
            if (_outlineDecimated.Count < MaxPoints && _outlineDecimated[_outlineDecimated.Count - 1] != last)
                _outlineDecimated.Add(last);
        }
        else
        {
            for (int i = 0; i < _outlineWorld.Length; i++)
                _outlineDecimated.Add(_outlineWorld[i]);
        }
    }

    //!-------------------------------------------------------------------------------------------------------------------
    #endregion
    //!-------------------------------------------------------------------------------------------------------------------
}