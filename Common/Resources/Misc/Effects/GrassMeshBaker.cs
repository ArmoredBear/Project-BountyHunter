using Godot;
using System;
using System.Collections.Generic;

/**-----------------------------------------------------------------------------------------------------------------------
	**                                                    GRASS MESH BAKER
	*-----------------------------------------------------------------------------------------------------------------------**/

/**-----------------------------------------------------------------------------------------------------------------------
	**                                                    PURPOSE
	*
	**  Bakes a hand-drawn grass meadow into real tuft geometry and swaps the
	**  heavy procedural scatter (GrassRepeat.gdshader) for cheap quads:
	**
	**  1 - Replicates the scatter shader's deterministic tuft generation in
	**  world space: the same cell grid, clumping field, density roll, type /
	**  size / placement / flip hashing and colour ramp. Every tuft becomes a
	**  quad (two triangles) with its tone in the vertex COLOR and its origin /
	**  size / type / flip packed into CUSTOM0 / CUSTOM1.
	**
	**  2 - Applies the border rim in C#: a tuft is only kept when its whole
	**  footprint sits strictly inside the drawn polygon, every corner clears
	**  the outline by `BorderRadius`, and no outline vertex approaches the
	**  footprint by less than `BorderRadius` (the exact same three tests the
	**  shader ran per pixel, run once per tuft).
	**
	**  3 - Partitions the tufts into square tile meshes (world-space). The
	**  renderer's own per-item culling therefore skips whole grass tiles when
	**  they leave the camera - the same "disable rendering off-screen" logic
	**  TileMapLayers enjoy - with no script work.
	**
	**  4 - Spawns one mesh instance per tile in two passes sharing the same
	**  mesh: a back pass (drawn under the player) and a front pass (drawn over
	**  it). GrassBaked.gdshader clips each pass per tuft (origin vs the split
	**  line) so tufts change layer as the player walks past them, costing only
	**  two uniforms per frame and never rebuilding a mesh.
	**
	*-----------------------------------------------------------------------------------------------------------------------**/

public partial class GrassMeshBaker : Node
{
    //!-------------------------------------------------------------------------------------------------------------------
    #region Variables
    //!-------------------------------------------------------------------------------------------------------------------

    // The existing grass material: its shader parameters (textures, sizes,
    // chances, density, sway...) are copied onto the baked material so the
    // baked meadow mirrors the current look.
    [ExportCategory("Bake Source")]
    [Export] public Material SourceMaterial;

    // The Polygon2D whose hand-drawn polygon is the meadow outline (used for
    // the world-space border rim) and whose transform gives the world bounds.
    [Export] public NodePath OutlinePolygonPath;

    // Container that receives the generated MeshInstance2D tiles.
    [Export] public NodePath TilesContainerPath;

    // Width of the empty rim kept inside the drawn outline (world units).
    [Export(PropertyHint.Range, "0,200")] public float BorderRadius = 32.0f;

    // World size of one baked tile (the culling granularity). Smaller tiles
    // cull off-screen grass tighter but add more items.
    [Export(PropertyHint.Range, "200,4000")] public float TileSize = 1200.0f;

    [ExportCategory("Depth Split")]
    // Node whose global Y (plus the offset) is the split line; when null the
    // line stays fixed at SplitYOffset.
    [Export] public NodePath SplitSourcePath;

    // Vertical offset added to the split source Y (positive shifts the line
    // downward, putting more grass in front).
    [Export(PropertyHint.Range, "-500,500")] public float SplitY_Offset = 0.0f;

    private Polygon2D _outline;
    private Node2D _tilesContainer;
    private Node2D _splitSource;

    private ShaderMaterial _bakedBackMaterial;
    private ShaderMaterial _bakedFrontMaterial;

    // Shader parameter snapshot from the source material (doubles as the
    // algorithm's tuning, so the bake follows the material the designer set).
    private float _spacing;
    private float _jitter;
    private float _sizeVariation;
    private float _flipChance;
    private float _clumpScale;
    private float _clumpStrength;
    private float _density;
    private float _emptyChance;
    private float _colorVariation;
    private Vector2 _sizeA;
    private Vector2 _sizeB;
    private Vector2 _sizeC;
    private float _chanceA;
    private float _chanceB;
    private float _chanceC;
    private Color _colorA;
    private Color _colorB;
    private Color _colorC;

    private Vector2[] _outlineWorld;
    private Rect2 _meadowRect;

    #endregion
    //!-------------------------------------------------------------------------------------------------------------------

    //!-------------------------------------------------------------------------------------------------------------------
    #region Initialization and Processes
    //!-------------------------------------------------------------------------------------------------------------------

    public override void _Ready()
    {
        base._Ready();

        _outline = GetNodeOrNull<Polygon2D>(OutlinePolygonPath);
        _splitSource = GetNodeOrNull<Node2D>(SplitSourcePath);

        _tilesContainer = GetNodeOrNull<Node2D>(TilesContainerPath);
        if (_tilesContainer == null)
        {
            _tilesContainer = new Node2D { Name = "BakedGrass" };
            AddChild(_tilesContainer);
        }

        if (_outline == null)
        {
            GD.PrintErr("GRASS BAKE FAIL: no outline polygon at ", OutlinePolygonPath);
            return;
        }

        ReadBakeParameters();
        BuildMaterials();

        System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
        BakeMeadow();
        watch.Stop();

        GD.Print("GRASS BAKE OK tufts=", LastTuftCount, " tiles=", LastTileCount,
            " ms=", watch.ElapsedMilliseconds, " cells=", LastCellCount,
            " rect=", _meadowRect,
            " dens=", _density, " spacing=", _spacing, " clump=", _clumpStrength,
            " sizevar=", _sizeVariation, " cols=", _colorVariation);
    }

    public override void _Process(double delta)
    {
        UpdateSplitLine();
    }

    #endregion
    //!-------------------------------------------------------------------------------------------------------------------

    // Public stats, read after _Ready for verification scripts.
    public int LastTuftCount { get; private set; }
    public int LastTileCount { get; private set; }
    public int LastCellCount { get; private set; }

    //!-------------------------------------------------------------------------------------------------------------------
    #region Material and Parameter Setup
    //!-------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Snapshots every baking-relevant parameter from the source material. A
    /// missing spin on a name falls back to the procedural shader's defaults,
    /// so the bake still resolves when the material renames a parameter.
    /// </summary>
    private void ReadBakeParameters()
    {
        _sizeA = ReadVec2("tuft_world_size_a", new Vector2(50.0f, 26.0f));
        _sizeB = ReadVec2("tuft_world_size_b", new Vector2(22.0f, 88.0f));
        _sizeC = ReadVec2("tuft_world_size_c", new Vector2(47.0f, 47.0f));
        _chanceA = ReadFloat("grass_a_chance", 1.0f);
        _chanceB = ReadFloat("grass_b_chance", 0.5f);
        _chanceC = ReadFloat("grass_c_chance", 0.0f);
        _colorA = ReadColor("grass_color_a", new Color(0.05f, 0.30f, 0.12f));
        _colorB = ReadColor("grass_color_b", new Color(0.10f, 0.45f, 0.16f));
        _colorC = ReadColor("grass_color_c", new Color(0.30f, 0.55f, 0.12f));
        _colorVariation = ReadFloat("color_variation", 0.8f);
        _spacing = ReadFloat("spacing", 4.0f);
        _jitter = ReadFloat("jitter", 1.0f);
        _sizeVariation = ReadFloat("size_variation", 0.6f);
        _flipChance = ReadFloat("flip_chance", 0.5f);
        _clumpScale = ReadFloat("clump_scale", 800.0f);
        _clumpStrength = ReadFloat("clump_strength", 0.7f);
        _density = ReadFloat("density", 1.0f);
        _emptyChance = ReadFloat("empty_chance", 0.0f);
    }

    private float ReadFloat(StringName name, float fallback)
    {
        if (SourceMaterial is ShaderMaterial sm && sm.GetShaderParameter(name).VariantType != Godot.Variant.Type.Nil)
            return sm.GetShaderParameter(name).AsSingle();
        return fallback;
    }

    private Vector2 ReadVec2(StringName name, Vector2 fallback)
    {
        if (SourceMaterial is ShaderMaterial sm && sm.GetShaderParameter(name).VariantType != Godot.Variant.Type.Nil)
            return sm.GetShaderParameter(name).AsVector2();
        return fallback;
    }

    private Color ReadColor(StringName name, Color fallback)
    {
        if (SourceMaterial is ShaderMaterial sm && sm.GetShaderParameter(name).VariantType != Godot.Variant.Type.Nil)
            return sm.GetShaderParameter(name).AsColor();
        return fallback;
    }

    /// <summary>
    /// Builds the two shared materials (back and front passes) by duplicating
    /// the source grass material and swapping in the baked grass shader, so
    /// every texture and tuning value carries over. Only the two materials are
    /// ever updated at runtime (the split uniform), no matter how many tiles.
    /// </summary>
    private void BuildMaterials()
    {
        if (!(SourceMaterial is ShaderMaterial shared))
            return;

        Shader bakedShader = GD.Load<Shader>("res://Common/Resources/Shaders/GrassBaked.gdshader");
        if (bakedShader == null)
        {
            GD.PrintErr("GRASS BAKE FAIL: GrassBaked.gdshader not found");
            return;
        }

        _bakedBackMaterial = (ShaderMaterial)shared.Duplicate();
        _bakedBackMaterial.Shader = bakedShader;
        _bakedBackMaterial.SetShaderParameter("depth_split_mode", 1);

        _bakedFrontMaterial = (ShaderMaterial)shared.Duplicate();
        _bakedFrontMaterial.Shader = bakedShader;
        _bakedFrontMaterial.SetShaderParameter("depth_split_mode", 2);
    }

    #endregion
    //!-------------------------------------------------------------------------------------------------------------------

    //!-------------------------------------------------------------------------------------------------------------------
    #region Meadow Outline
    //!-------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Snapshot of the drawn outline in world space (the polygon is stored in
    /// the sprite's local coordinates), plus its world-space AABB that bounds
    /// which cells to visit.
    /// </summary>
    private void SnapshotOutline()
    {
        Vector2[] local = _outline.Polygon;
        if (local == null || local.Length < 3)
        {
            _outlineWorld = null;
            _meadowRect = new Rect2(_outline.GlobalPosition, Vector2.One);
            return;
        }

        Transform2D xf = _outline.GlobalTransform;
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
        _meadowRect = new Rect2(minX, minY, maxX - minX, maxY - minY);
    }

    #endregion
    //!-------------------------------------------------------------------------------------------------------------------

    //!-------------------------------------------------------------------------------------------------------------------
    #region Bake
    //!-------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Replicates the scatter shader's world-space generation (same cell grid,
    /// clumping field, density roll, per-copy type/size/placement hashing and
    /// colour ramp), folds in the border rim, and lays the accepted tufts out
    /// over tile meshes.
    /// </summary>
    private void BakeMeadow()
    {
        SnapshotOutline();
        if (_outlineWorld == null)
            return;

        Vector2 cellSize = _sizeA * _spacing;
        if (cellSize.X <= 0.01f || cellSize.Y <= 0.01f)
        {
            GD.PrintErr("GRASS BAKE FAIL: invalid cell size ", cellSize);
            return;
        }

        float totalChance = Mathf.Max(0.0001f, _chanceA + _chanceB + _chanceC);
        float shareA = _chanceA / totalChance;
        float shareB = shareA + _chanceB / totalChance;

        // Visit the cells that can reach the meadow (plus one cell of slack
        // for tufts that drift past their own cell borders).
        int minCX = Mathf.FloorToInt((_meadowRect.Position.X - cellSize.X - 300.0f) / cellSize.X);
        int maxCX = Mathf.CeilToInt((_meadowRect.End.X + cellSize.X + 300.0f) / cellSize.X);
        int minCY = Mathf.FloorToInt((_meadowRect.Position.Y - cellSize.Y - 300.0f) / cellSize.Y);
        int maxCY = Mathf.CeilToInt((_meadowRect.End.Y + cellSize.Y + 300.0f) / cellSize.Y);

        var tiles = new Dictionary<long, List<Tuft>>();
        int tuftCount = 0;
        int cellCount = 0;
        float r2 = BorderRadius * BorderRadius;

        for (int cy = minCY; cy <= maxCY; cy++)
        {
            for (int cx = minCX; cx <= maxCX; cx++)
            {
                cellCount++;
                Vector2 cc = new Vector2(cx, cy);

                float field = Fbm2((cc + new Vector2(0.5f, 0.5f)) * cellSize / _clumpScale + new Vector2(47.3f, 47.3f));
                float densityField = Mathf.Lerp(1.0f, 0.4f + 2.3f * field, _clumpStrength) * _density;

                float rh = Hash21(cc + new Vector2(9.1f, 4.4f));
                int count = Mathf.FloorToInt(rh + densityField);
                if (Hash21(cc + new Vector2(3.3f, 7.7f)) < _emptyChance)
                    count = 0;

                for (int k = 0; k < count; k++)
                {
                    Vector2 sk = cc * 17.31f + new Vector2(k * 7.17f, 3.1f);

                    // Type: weighted roll across A / B / C.
                    float typeRoll = Hash21(sk + new Vector2(5.9f, 13.1f));
                    int type;
                    if (typeRoll < shareA)
                        type = 0;
                    else if (typeRoll < shareB)
                        type = 1;
                    else
                        type = 2;
                    Vector2 baseSize = type == 1 ? _sizeB : (type == 2 ? _sizeC : _sizeA);

                    // Size (capped so a tuft never outgrows its cell).
                    float scale = 1.0f + (Hash21(sk + new Vector2(1.3f, 8.8f)) - 0.5f) * 2.0f * _sizeVariation;
                    scale *= 1.0f + (field - 0.5f) * _clumpStrength;
                    float scaleCap = Mathf.Min(cellSize.X / baseSize.X, cellSize.Y / baseSize.Y);
                    scale = Mathf.Clamp(scale, 0.3f, scaleCap);
                    Vector2 size = baseSize * scale;

                    // Placement anywhere across the cell.
                    float rx = Hash21(sk + new Vector2(7.1f, 5.3f));
                    float ry = Hash21(sk + new Vector2(3.7f, 1.9f));
                    Vector2 place = new Vector2(0.5f, 0.5f) + (new Vector2(rx, ry) - new Vector2(0.5f, 0.5f)) * _jitter;
                    Vector2 origin = cc * cellSize + cellSize * place;

                    // Border rim: reject any tuft whose footprint touches the
                    // outline or the empty rim around it.
                    if (!IsTuftAllowed(origin, size, r2))
                        continue;

                    bool flop = Hash21(sk + new Vector2(2.3f, 6.1f)) < _flipChance;

                    // Per-tuft tone from the a->b->c colour ramp.
                    float b = Hash21(sk + new Vector2(13.7f, 7.9f));
                    b = Mathf.Lerp(0.5f, b, _colorVariation);
                    Color grad;
                    if (b < 0.5f)
                        grad = _colorA.Lerp(_colorB, b * 2.0f);
                    else
                        grad = _colorB.Lerp(_colorC, (b - 0.5f) * 2.0f);

                    long tileKey = TileKey(origin);
                    if (!tiles.TryGetValue(tileKey, out List<Tuft> list))
                    {
                        list = new List<Tuft>();
                        tiles[tileKey] = list;
                    }
                    list.Add(new Tuft(origin, size, type, flop, grad));
                    tuftCount++;
                }
            }
        }

        LastTuftCount = tuftCount;
        LastCellCount = cellCount;
        LastTileCount = tiles.Count;
        BuildTiles(tiles);
    }

    /// <summary>
    /// The scatter shader's three border tests, run once per tuft instead of
    /// per pixel: all four corners strictly inside the polygon, every corner
    /// at least BorderRadius away from every outline edge, and no outline
    /// vertex closer than BorderRadius to the footprint rectangle.
    /// </summary>
    private bool IsTuftAllowed(Vector2 origin, Vector2 size, float r2)
    {
        Vector2 c0 = origin;
        Vector2 c1 = origin + new Vector2(size.X, 0.0f);
        Vector2 c2 = origin + new Vector2(0.0f, size.Y);
        Vector2 c3 = origin + size;

        if (!Geometry2D.IsPointInPolygon(c0, _outlineWorld) ||
            !Geometry2D.IsPointInPolygon(c1, _outlineWorld) ||
            !Geometry2D.IsPointInPolygon(c2, _outlineWorld) ||
            !Geometry2D.IsPointInPolygon(c3, _outlineWorld))
            return false;

        if (Dist2ToOutline(c0) < r2 || Dist2ToOutline(c1) < r2 ||
            Dist2ToOutline(c2) < r2 || Dist2ToOutline(c3) < r2)
            return false;

        int n = _outlineWorld.Length;
        for (int i = 0; i < n; i++)
        {
            if (Dist2ToRect(_outlineWorld[i], origin, size) < r2)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Squared distance from the point to the nearest outline edge.
    /// </summary>
    private float Dist2ToOutline(Vector2 p)
    {
        float best = float.MaxValue;
        int n = _outlineWorld.Length;
        for (int i = 0; i < n; i++)
            best = Mathf.Min(best, Dist2Segment(p, _outlineWorld[i], _outlineWorld[(i + 1) % n]));
        return best;
    }

    /// <summary>
    /// Squared distance from the point to the line segment [a, b].
    /// </summary>
    private float Dist2Segment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        Vector2 ap = p - a;
        float t = ap.Dot(ab) / Mathf.Max(ab.Dot(ab), 1e-6f);
        t = Mathf.Clamp(t, 0.0f, 1.0f);
        Vector2 q = a + ab * t;
        float d = (p - q).LengthSquared();
        return d;
    }

    /// <summary>
    /// Squared distance from the point to the axis-aligned rectangle whose
    /// bottom-right corner is origin + size.
    /// </summary>
    private float Dist2ToRect(Vector2 p, Vector2 origin, Vector2 size)
    {
        Vector2 q = new Vector2(
            Mathf.Clamp(p.X, origin.X, origin.X + size.X),
            Mathf.Clamp(p.Y, origin.Y, origin.Y + size.Y));
        float dx = p.X - q.X;
        float dy = p.Y - q.Y;
        return dx * dx + dy * dy;
    }

    #endregion
    //!-------------------------------------------------------------------------------------------------------------------

    //!-------------------------------------------------------------------------------------------------------------------
    #region Tile Meshes
    //!-------------------------------------------------------------------------------------------------------------------

    private long TileKey(Vector2 origin)
    {
        int tx = Mathf.FloorToInt(origin.X / TileSize);
        int ty = Mathf.FloorToInt(origin.Y / TileSize);
        return ((long)tx << 32) ^ ((long)ty & 0xFFFFFFFFL);
    }

    /// <summary>
    /// Turns the per-tile tuft lists into world-space quad meshes and spawns
    /// the two mesh instances per tile (shared mesh, back/front materials).
    /// </summary>
    private void BuildTiles(Dictionary<long, List<Tuft>> tiles)
    {
        foreach (var pair in tiles)
        {
            int tx = (int)(pair.Key >> 32);
            int ty = (int)(pair.Key & 0xFFFFFFFFL);
            List<Tuft> list = pair.Value;

            var mesh = new ArrayMesh();
            var tool = new SurfaceTool();
            tool.Begin(Mesh.PrimitiveType.Triangles);
            tool.SetCustomFormat(0, SurfaceTool.CustomFormat.RgbaFloat);
            tool.SetCustomFormat(1, SurfaceTool.CustomFormat.RgbaFloat);

            foreach (Tuft tuft in list)
            {
                Vector2 o = tuft.Origin;
                Vector2 s = tuft.Size;
                Vector2[] corners = { o, o + new Vector2(s.X, 0.0f), o + s, o + new Vector2(0.0f, s.Y) };
                Vector2[] uvs = { new Vector2(0.0f, 0.0f), new Vector2(1.0f, 0.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 1.0f) };
                for (int v = 0; v < 4; v++)
                {
                    tool.SetUV(uvs[v]);
                    tool.SetColor(new Color(tuft.Tone.R, tuft.Tone.G, tuft.Tone.B, 1.0f));
                    tool.SetCustom(0, new Color(o.X, o.Y, s.X, s.Y));
                    tool.SetCustom(1, new Color(tuft.Type, tuft.Flop ? 1.0f : 0.0f, 0.0f, 0.0f));
                    tool.AddVertex(new Vector3(corners[v].X, corners[v].Y, 0.0f));
                }
            }

            tool.Commit(mesh);

            SpawnTile(tx, ty, mesh, 1, _bakedBackMaterial);
            SpawnTile(tx, ty, mesh, 2, _bakedFrontMaterial);
        }
    }

    /// <summary>
    /// One mesh instance per pass, sharing the tile mesh: back (drawn under a
    /// player) and front (drawn over it). The per-tuft front/back clip lives
    /// in the shader's vertex stage, so nothing here changes per frame.
    /// </summary>
    private void SpawnTile(int tx, int ty, ArrayMesh mesh, int pass, ShaderMaterial material)
    {
        var instance = new MeshInstance2D
        {
            Name = $"Tile_{tx}_{ty}_{(pass == 1 ? "back" : "front")}",
            Mesh = mesh,
            Material = material,
            ZIndex = pass == 1 ? 1 : 3,
            ZAsRelative = false,
        };
        _tilesContainer.AddChild(instance);
    }

    #endregion
    //!-------------------------------------------------------------------------------------------------------------------

    //!-------------------------------------------------------------------------------------------------------------------
    #region Runtime
    //!-------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Follows the split source's feet Y (plus the offset) into both shared
    /// materials; the vertex shader resolves which tufts belong to which pass.
    /// </summary>
    private void UpdateSplitLine()
    {
        if (_bakedBackMaterial == null || _bakedFrontMaterial == null)
            return;

        float splitY = SplitY_Offset;
        if (_splitSource != null && IsInstanceValid(_splitSource))
            splitY += _splitSource.GlobalPosition.Y;

        _bakedBackMaterial.SetShaderParameter("depth_split_y", splitY);
        _bakedFrontMaterial.SetShaderParameter("depth_split_y", splitY);
    }

    #endregion
    //!-------------------------------------------------------------------------------------------------------------------

    //!-------------------------------------------------------------------------------------------------------------------
    #region Deterministic Hash (shader-port)
    //!-------------------------------------------------------------------------------------------------------------------

    // These replicate GrassRepeat.gdshader's hash21 / vnoise / fbm2 exactly
    // (32-bit float math), so the baked meadow uses the same clumping and
    // per-tuft randomness the procedural scatter produced.

    private static float Fract(float x)
    {
        return x - Mathf.Floor(x);
    }

    private static Vector2 Fract(Vector2 p)
    {
        return new Vector2(Fract(p.X), Fract(p.Y));
    }

    /// <summary>
    /// The shader's hash21: GLSL fract(p * vec2(123.34, 456.21)); p += dot(p, p + 45.32);
    /// return fract(p.x * p.y). The scalar dot result is added to both components.
    /// </summary>
    private static float Hash21(Vector2 p)
    {
        p = Fract(p * new Vector2(123.34f, 456.21f));
        float dotValue = p.Dot(p + new Vector2(45.32f, 45.32f));
        p += new Vector2(dotValue, dotValue);
        return Fract(p.X * p.Y);
    }

    /// <summary>
    /// The shader's vnoise: smooth-step interpolated value noise over the 2x2
    /// cell neighbourhood of p.
    /// </summary>
    private static float Vnoise(Vector2 p)
    {
        Vector2 i = new Vector2(Mathf.Floor(p.X), Mathf.Floor(p.Y));
        Vector2 f = Fract(p);
        f = f * f * (new Vector2(3.0f, 3.0f) - 2.0f * f);
        float a = Hash21(i);
        float b = Hash21(i + new Vector2(1.0f, 0.0f));
        float c = Hash21(i + new Vector2(0.0f, 1.0f));
        float d = Hash21(i + new Vector2(1.0f, 1.0f));
        return Mathf.Lerp(Mathf.Lerp(a, b, f.X), Mathf.Lerp(c, d, f.X), f.Y);
    }

    /// <summary>
    /// The shader's fbm2: two octaves of value noise.
    /// </summary>
    private static float Fbm2(Vector2 p)
    {
        return Vnoise(p) * 0.65f + Vnoise(p * 2.13f + new Vector2(11.7f, 11.7f)) * 0.35f;
    }

    //!-------------------------------------------------------------------------------------------------------------------
    #endregion
    //!-------------------------------------------------------------------------------------------------------------------
}

/// <summary>
/// One generated tuft: its world footprint, type, mirror flag and tone.
/// </summary>
public struct Tuft
{
    public Vector2 Origin;
    public Vector2 Size;
    public int Type;
    public bool Flop;
    public Color Tone;

    public Tuft(Vector2 origin, Vector2 size, int type, bool flop, Color tone)
    {
        Origin = origin;
        Size = size;
        Type = type;
        Flop = flop;
        Tone = tone;
    }
}