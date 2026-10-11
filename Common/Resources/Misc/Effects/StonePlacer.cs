using Godot;
using System;
using System.Collections.Generic;

/**-----------------------------------------------------------------------------------------------------------------------
	**                                                        STONE PLACER
	*-----------------------------------------------------------------------------------------------------------------------**/

/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	**
	**  1 - Works out how far the forest floor actually extends by walking the
	**  scene for every TileMapLayer and unioning the area its painted cells
	**  cover, in world space. Nothing is generated outside that area, so stones
	**  never end up floating in empty space past the edge of the map.
	**
	**  2 - Scatters Vector2 positions inside that area and creates one Sprite2D
	**  per position, using three stone textures the user supplies. Each of the
	**  three stone types carries its own count, scale range, rotation range,
	**  flip chance and tint, so a big rock type and a small pebble type can
	**  share one run.
	**
	**  3 - The generated Sprite2D nodes are real nodes with the edited scene
	**  root as their Owner, so they are written into the scene file and stay
	**  editable by hand afterwards. This only lays stones out; nothing here runs
	**  at game time, and it touches nothing the grass bakers produce.
	**
	**  USAGE
	**
	**  1 - Add this node to the scene, drop the three stone CanvasTextures into
	**  StoneA/B/C_Texture, and point BoundsRootPath at the node that holds the
	**  ground TileMapLayers (the scene root works).
	**  2 - Set TargetContainerPath at the node the stones should go under.
	**  3 - Tick Place. The stones appear and the scene is marked unsaved, so
	**  save it with Ctrl+S.
	**  4 - Change Seed for a different layout and run again. Each run clears
	**  the previous stones first, so one is always a fresh replacement.
	**
	*-----------------------------------------------------------------------------------------------------------------------**/

/// <summary>
/// One stone type's numbers, gathered from the flat inspector fields just
/// before a run. This is a plain C# object, not a Godot Resource, so it is
/// never written into the scene file and can never fail to load back.
/// </summary>
public class StoneVariant
{
    //!-------------------------------------------------------------------------------------------------------------------
    #region Variables
    //!-------------------------------------------------------------------------------------------------------------------

    /// <summary>How many stones of this type to scatter.</summary>
    public int Count;

    //!-------------------------------------------------------------------------------------------------------------------

    /// <summary>Smallest random scale for this stone type.</summary>
    public Vector2 MinScale;

    //!-------------------------------------------------------------------------------------------------------------------

    /// <summary>Largest random scale for this stone type.</summary>
    public Vector2 MaxScale;

    //!-------------------------------------------------------------------------------------------------------------------

    /// <summary>Smallest random rotation in degrees.</summary>
    public float MinRotation;

    //!-------------------------------------------------------------------------------------------------------------------

    /// <summary>Largest random rotation in degrees.</summary>
    public float MaxRotation;

    //!-------------------------------------------------------------------------------------------------------------------

    /// <summary>Chance of a stone being drawn mirrored, from 0 to 1.</summary>
    public float FlipChance;

    //!-------------------------------------------------------------------------------------------------------------------

    /// <summary>Flat tint applied to every stone of this type.</summary>
    public Color Modulate;

    //!-------------------------------------------------------------------------------------------------------------------

    /// <summary>When true the stone stands on its position instead of hovering centred on it.</summary>
    public bool AnchorToBase;

    //!-------------------------------------------------------------------------------------------------------------------

    /// <summary>Extra vertical nudge in world units.</summary>
    public float BaseOffsetY;

    #endregion
    //!-------------------------------------------------------------------------------------------------------------------
}

[Tool]
public partial class StonePlacer : Node
{
    /// <summary>Name prefix stamped on every stone this tool creates, so a re-run can recognise its own work and clear only that.</summary>
    private const string StoneNamePrefix = "Stone_";

    //!-------------------------------------------------------------------------------------------------------------------
    #region Variables
    //!-------------------------------------------------------------------------------------------------------------------

    /// <summary>Node to search for ground TileMapLayers under. Leave empty to use the edited scene root.</summary>
    [ExportCategory("Bounds")]
    [Export] public NodePath BoundsRootPath;

    /// <summary>Extra world units grown onto the tilemap bounds on every side before scattering.</summary>
    [Export(PropertyHint.Range, "0,2000,10")] public float BoundsPadding = 0.0f;

    /// <summary>Fallback bounds used only when the scene has no TileMapLayer with painted cells.</summary>
    [Export] public Rect2 FallbackBounds = new Rect2(0, 0, 20000, 8000);

    /// <summary>Node the generated Sprite2D stones are added under.</summary>
    [ExportCategory("Output")]
    [Export] public NodePath TargetContainerPath;

    /// <summary>Absolute draw layer for every stone. The forest runs grass back at 1, player at 2 and grass front at 3, so 1 puts stones above the back grass and under the player.</summary>
    [Export(PropertyHint.Range, "-100,100,1")] public int StoneZIndex = 1;

    /// <summary>Colour texture for the first stone type. Drop a CanvasTexture in here.</summary>
    [ExportCategory("Stone Textures")]
    [Export] public Texture2D StoneA_Texture;

    /// <summary>Colour texture for the second stone type. Drop a CanvasTexture in here.</summary>
    [Export] public Texture2D StoneB_Texture;

    /// <summary>Colour texture for the third stone type. Drop a CanvasTexture in here.</summary>
    [Export] public Texture2D StoneC_Texture;

    /// <summary>How many Stone A rocks to scatter.</summary>
    [ExportGroup("Stone A")]
    [Export(PropertyHint.Range, "0,2000,1")] public int StoneA_Count = 400;

    /// <summary>Smallest random scale for Stone A.</summary>
    [Export] public Vector2 StoneA_MinScale = Vector2.One;

    /// <summary>Largest random scale for Stone A.</summary>
    [Export] public Vector2 StoneA_MaxScale = Vector2.One;

    /// <summary>Smallest random rotation in degrees for Stone A.</summary>
    [Export] public float StoneA_MinRotation = 0.0f;

    /// <summary>Largest random rotation in degrees for Stone A.</summary>
    [Export] public float StoneA_MaxRotation = 0.0f;

    /// <summary>Chance of a Stone A rock being mirrored, from 0 to 1.</summary>
    [Export] public float StoneA_FlipChance = 0.5f;

    /// <summary>Flat tint for every Stone A rock. White means no tint.</summary>
    [Export] public Color StoneA_Modulate = Colors.White;

    /// <summary>When true Stone A rocks stand on their position instead of hovering centred over it.</summary>
    [Export] public bool StoneA_AnchorToBase = true;

    /// <summary>Extra vertical nudge for Stone A, for sinking them into the ground.</summary>
    [Export(PropertyHint.Range, "-200,200,1")] public float StoneA_BaseOffsetY = 0.0f;

    /// <summary>How many Stone B rocks to scatter.</summary>
    [ExportGroup("Stone B")]
    [Export(PropertyHint.Range, "0,2000,1")] public int StoneB_Count = 400;

    /// <summary>Smallest random scale for Stone B.</summary>
    [Export] public Vector2 StoneB_MinScale = Vector2.One;

    /// <summary>Largest random scale for Stone B.</summary>
    [Export] public Vector2 StoneB_MaxScale = Vector2.One;

    /// <summary>Smallest random rotation in degrees for Stone B.</summary>
    [Export] public float StoneB_MinRotation = 0.0f;

    /// <summary>Largest random rotation in degrees for Stone B.</summary>
    [Export] public float StoneB_MaxRotation = 0.0f;

    /// <summary>Chance of a Stone B rock being mirrored, from 0 to 1.</summary>
    [Export] public float StoneB_FlipChance = 0.5f;

    /// <summary>Flat tint for every Stone B rock. White means no tint.</summary>
    [Export] public Color StoneB_Modulate = Colors.White;

    /// <summary>When true Stone B rocks stand on their position instead of hovering centred over it.</summary>
    [Export] public bool StoneB_AnchorToBase = true;

    /// <summary>Extra vertical nudge for Stone B, for sinking them into the ground.</summary>
    [Export(PropertyHint.Range, "-200,200,1")] public float StoneB_BaseOffsetY = 0.0f;

    /// <summary>How many Stone C rocks to scatter.</summary>
    [ExportGroup("Stone C")]
    [Export(PropertyHint.Range, "0,2000,1")] public int StoneC_Count = 400;

    /// <summary>Smallest random scale for Stone C.</summary>
    [Export] public Vector2 StoneC_MinScale = Vector2.One;

    /// <summary>Largest random scale for Stone C.</summary>
    [Export] public Vector2 StoneC_MaxScale = Vector2.One;

    /// <summary>Smallest random rotation in degrees for Stone C.</summary>
    [Export] public float StoneC_MinRotation = 0.0f;

    /// <summary>Largest random rotation in degrees for Stone C.</summary>
    [Export] public float StoneC_MaxRotation = 0.0f;

    /// <summary>Chance of a Stone C rock being mirrored, from 0 to 1.</summary>
    [Export] public float StoneC_FlipChance = 0.5f;

    /// <summary>Flat tint for every Stone C rock. White means no tint.</summary>
    [Export] public Color StoneC_Modulate = Colors.White;

    /// <summary>When true Stone C rocks stand on their position instead of hovering centred over it.</summary>
    [Export] public bool StoneC_AnchorToBase = true;

    /// <summary>Extra vertical nudge for Stone C, for sinking them into the ground.</summary>
    [Export(PropertyHint.Range, "-200,200,1")] public float StoneC_BaseOffsetY = 0.0f;

    /// <summary>Master seed. Change it for a different scatter. Each stone type offsets from this, so types never repeat each other.</summary>
    [ExportCategory("Run")]
    [Export] public int Seed = 20260925;

    /// <summary>Tick to scatter. The script clears the tick itself, so one tick is one run.</summary>
    [Export] public bool Place = false;

    /// <summary>The positions used by the last run, in world space, for inspecting the scatter.</summary>
    public Godot.Collections.Array<Vector2> LastPositions { get; private set; } = new Godot.Collections.Array<Vector2>();

    /// <summary>The world bounds the last run worked out from the tilemaps.</summary>
    public Rect2 LastBounds { get; private set; }

    #endregion
    //!-------------------------------------------------------------------------------------------------------------------

    //!-------------------------------------------------------------------------------------------------------------------
    #region Processes
    //!-------------------------------------------------------------------------------------------------------------------

    public override void _Process(double delta)
    {
        if (!Place)
        {
            return;
        }

        // One tick means one run, so the checkbox goes back off by itself.
        Place = false;

        if (!Engine.IsEditorHint())
        {
            GD.PrintErr("STONE PLACER SKIP: this tool only builds nodes while the editor is open.");
            return;
        }

        // Do the real work outside _Process. Adding and freeing editor-owned
        // nodes while the editor is still walking its own tree leaves its
        // internal child array out of step with the live tree, which is what
        // trips the local_vector.h out-of-bounds assert.
        Callable.From(PlaceStones).CallDeferred();
    }

    #endregion
    //!-------------------------------------------------------------------------------------------------------------------

    //!-------------------------------------------------------------------------------------------------------------------
    #region Bounds
    //!-------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Adds up the world area covered by every painted TileMapLayer under the
    /// bounds root, growing the result by the padding. Falls back to the
    /// inspector rect when the scene has no usable tilemap, so the tool still
    /// does something obvious instead of silently doing nothing.
    /// </summary>
    private Rect2 ResolveBounds()
    {
        Node root = ResolveNode(BoundsRootPath);
        if (root == null)
        {
            GD.Print("STONE PLACER: no bounds root, using the fallback rect.");
            return FallbackBounds;
        }

        var layers = new List<TileMapLayer>();
        CollectTileMapLayers(root, layers);

        bool found = false;
        float minX = 0.0f;
        float minY = 0.0f;
        float maxX = 0.0f;
        float maxY = 0.0f;

        for (int i = 0; i < layers.Count; i++)
        {
            TileMapLayer layer = layers[i];

            Rect2I used = layer.GetUsedRect();
            if (used.Size.X <= 0 || used.Size.Y <= 0)
            {
                // An empty layer (the ground background layer is disabled and
                // has no cells) contributes nothing.
                continue;
            }

            Vector2I tileSize = new Vector2I(16, 16);
            if (layer.TileSet != null)
            {
                tileSize = layer.TileSet.TileSize;
            }
            if (tileSize.X <= 0 || tileSize.Y <= 0)
            {
                tileSize = new Vector2I(16, 16);
            }

            // Used cells are in grid space; scale by the tile size for pixels,
            // then push through the layer transform to reach world space.
            Rect2 cellRect = new Rect2(
                used.Position * tileSize,
                used.Size * tileSize);
            Rect2 worldRect = TransformRect(cellRect, layer.GlobalTransform);

            if (!found)
            {
                minX = worldRect.Position.X;
                minY = worldRect.Position.Y;
                maxX = worldRect.End.X;
                maxY = worldRect.End.Y;
                found = true;
            }
            else
            {
                minX = Mathf.Min(minX, worldRect.Position.X);
                minY = Mathf.Min(minY, worldRect.Position.Y);
                maxX = Mathf.Max(maxX, worldRect.End.X);
                maxY = Mathf.Max(maxY, worldRect.End.Y);
            }
        }

        if (!found)
        {
            GD.Print("STONE PLACER: no painted TileMapLayer found, using the fallback rect.");
            return FallbackBounds;
        }

        var bounds = new Rect2(
            new Vector2(minX, minY),
            new Vector2(maxX - minX, maxY - minY));
        bounds = bounds.Grow(BoundsPadding);
        return bounds;
    }

    /// <summary>
    /// Walks the tree under a node and gathers every TileMapLayer it finds.
    /// The forest spreads its grass over thirty separate layers, so all of them
    /// have to be unioned rather than picking just one.
    /// </summary>
    private static void CollectTileMapLayers(Node node, List<TileMapLayer> output)
    {
        if (node is TileMapLayer layer)
        {
            output.Add(layer);
        }

        // Snapshot the child count, then re-check it on every step. The editor
        // can be rebuilding the tree while a tool script runs, and asking for a
        // child index that no longer exists is what trips the out-of-bounds
        // assert, so back out instead of pressing on.
        int childCount = node.GetChildCount();
        for (int i = 0; i < childCount; i++)
        {
            if (i >= node.GetChildCount())
            {
                break;
            }

            CollectTileMapLayers(node.GetChild(i), output);
        }
    }

    /// <summary>
    /// Pushes a rect through a transform and returns the axis aligned box that
    /// contains the result, so a rotated layer still yields usable bounds.
    /// </summary>
    private static Rect2 TransformRect(Rect2 rect, Transform2D xf)
    {
        Vector2 p0 = xf * rect.Position;
        Vector2 p1 = xf * new Vector2(rect.End.X, rect.Position.Y);
        Vector2 p2 = xf * new Vector2(rect.Position.X, rect.End.Y);
        Vector2 p3 = xf * rect.End;

        float minX = Mathf.Min(Mathf.Min(p0.X, p1.X), Mathf.Min(p2.X, p3.X));
        float minY = Mathf.Min(Mathf.Min(p0.Y, p1.Y), Mathf.Min(p2.Y, p3.Y));
        float maxX = Mathf.Max(Mathf.Max(p0.X, p1.X), Mathf.Max(p2.X, p3.X));
        float maxY = Mathf.Max(Mathf.Max(p0.Y, p1.Y), Mathf.Max(p2.Y, p3.Y));

        return new Rect2(new Vector2(minX, minY), new Vector2(maxX - minX, maxY - minY));
    }

    #endregion
    //!-------------------------------------------------------------------------------------------------------------------

    //!-------------------------------------------------------------------------------------------------------------------
    #region Placement
    //!-------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Gathers the flat Stone A inspector fields into one holder for a run.
    /// </summary>
    private StoneVariant BuildStoneA()
    {
        StoneVariant variant = new StoneVariant();
        variant.Count = StoneA_Count;
        variant.MinScale = StoneA_MinScale;
        variant.MaxScale = StoneA_MaxScale;
        variant.MinRotation = StoneA_MinRotation;
        variant.MaxRotation = StoneA_MaxRotation;
        variant.FlipChance = StoneA_FlipChance;
        variant.Modulate = StoneA_Modulate;
        variant.AnchorToBase = StoneA_AnchorToBase;
        variant.BaseOffsetY = StoneA_BaseOffsetY;
        return variant;
    }

    /// <summary>
    /// Gathers the flat Stone B inspector fields into one holder for a run.
    /// </summary>
    private StoneVariant BuildStoneB()
    {
        StoneVariant variant = new StoneVariant();
        variant.Count = StoneB_Count;
        variant.MinScale = StoneB_MinScale;
        variant.MaxScale = StoneB_MaxScale;
        variant.MinRotation = StoneB_MinRotation;
        variant.MaxRotation = StoneB_MaxRotation;
        variant.FlipChance = StoneB_FlipChance;
        variant.Modulate = StoneB_Modulate;
        variant.AnchorToBase = StoneB_AnchorToBase;
        variant.BaseOffsetY = StoneB_BaseOffsetY;
        return variant;
    }

    /// <summary>
    /// Gathers the flat Stone C inspector fields into one holder for a run.
    /// </summary>
    private StoneVariant BuildStoneC()
    {
        StoneVariant variant = new StoneVariant();
        variant.Count = StoneC_Count;
        variant.MinScale = StoneC_MinScale;
        variant.MaxScale = StoneC_MaxScale;
        variant.MinRotation = StoneC_MinRotation;
        variant.MaxRotation = StoneC_MaxRotation;
        variant.FlipChance = StoneC_FlipChance;
        variant.Modulate = StoneC_Modulate;
        variant.AnchorToBase = StoneC_AnchorToBase;
        variant.BaseOffsetY = StoneC_BaseOffsetY;
        return variant;
    }

    /// <summary>
    /// Works out the ground bounds, then scatters each of the three stone
    /// types inside them and creates a Sprite2D per position. The scene is
    /// flagged unsaved at the end so the new nodes land in the scene file.
    /// </summary>
    private void PlaceStones()
    {
        // An empty TargetContainerPath falls back to the scene root, and
        // clearing that would delete the whole scene, so refuse to guess.
        if (TargetContainerPath == null || TargetContainerPath.IsEmpty)
        {
            GD.PrintErr("STONE PLACER FAIL: TargetContainerPath is empty. An empty path resolves to the scene ", 
                "root, so point it at a dedicated container node before placing.");
            return;
        }

        Node container = ResolveNode(TargetContainerPath);
        if (container == null)
        {
            GD.PrintErr("STONE PLACER FAIL: TargetContainerPath does not point at a node.");
            return;
        }

        // If the container is this node or one of its parents, clearing it would
        // free the tool that is currently running.
        if (IsSelfOrAncestor(container))
        {
            GD.PrintErr("STONE PLACER FAIL: the target container is the placer itself or one of its parents. ", 
                "Point it at a separate container, or the tool would delete itself when clearing.");
            return;
        }

        if (StoneA_Texture == null && StoneB_Texture == null && StoneC_Texture == null)
        {
            GD.PrintErr("STONE PLACER FAIL: all three stone textures are empty.");
            return;
        }

        Rect2 bounds = ResolveBounds();
        LastBounds = bounds;
        LastPositions.Clear();
        GD.Print("STONE PLACER bounds=", bounds, " from the tilemaps.");

        // Always replace, so a second run can never stack a second scatter on
        // top of the first one. ClearStones only touches Stone_ sprites, so
        // anything else under the container is left alone.
        int cleared = ClearStones(container);
        GD.Print("STONE PLACER cleared=", cleared, " previous stones.");

        // Each type walks the random stream from a different point so the three
        // types never line up on the same positions.
        PlaceVariant(container, StoneA_Texture, BuildStoneA(), Seed);
        PlaceVariant(container, StoneB_Texture, BuildStoneB(), Seed + 104729);
        PlaceVariant(container, StoneC_Texture, BuildStoneC(), Seed + 209759);

        GD.Print("STONE PLACER OK stones=", LastPositions.Count,
            " bounds=", bounds, " seed=", Seed);

        EditorInterface.Singleton.MarkSceneAsUnsaved();
    }

    /// <summary>
    /// Scatters one stone type: a random world position per stone inside the
    /// bounds, with a random scale, rotation, optional mirror and the type's
    /// tint, then a real Sprite2D node carrying them.
    /// </summary>
    private void PlaceVariant(Node container, Texture2D texture, StoneVariant settings, int seed)
    {
        if (texture == null)
        {
            return;
        }

        if (settings.Count <= 0)
        {
            return;
        }

        var rng = new Random(seed);
        Vector2 boundsSize = LastBounds.Size;

        for (int i = 0; i < settings.Count; i++)
        {
            float x = LastBounds.Position.X + NextFloat(rng) * boundsSize.X;
            float y = LastBounds.Position.Y + NextFloat(rng) * boundsSize.Y;
            var position = new Vector2(x, y);

            Vector2 scale = new Vector2(
                Mathf.Lerp(settings.MinScale.X, settings.MaxScale.X, NextFloat(rng)),
                Mathf.Lerp(settings.MinScale.Y, settings.MaxScale.Y, NextFloat(rng)));

            float rotation = Mathf.Lerp(
                Mathf.DegToRad(settings.MinRotation),
                Mathf.DegToRad(settings.MaxRotation),
                NextFloat(rng));

            var stone = new Sprite2D();
            stone.Name = StoneNamePrefix + LastPositions.Count.ToString("00");
            stone.Texture = texture;
            stone.Position = position;
            stone.Rotation = rotation;
            stone.Scale = scale;
            stone.Modulate = settings.Modulate;
            stone.ZAsRelative = false;
            stone.ZIndex = StoneZIndex;

            bool flip = NextFloat(rng) < settings.FlipChance;
            if (flip)
            {
                stone.FlipH = true;
            }

            if (settings.AnchorToBase)
            {
                // Put the origin at the bottom middle of the stone so it stands
                // on its position rather than hovering centred over it.
                stone.Centered = false;
                Vector2 texSize = texture.GetSize();
                stone.Offset = new Vector2(-texSize.X * 0.5f, -texSize.Y);
            }

            if (settings.BaseOffsetY != 0.0f)
            {
                stone.Position = new Vector2(position.X, position.Y + settings.BaseOffsetY);
            }

            // Setting the Owner is what makes the node get written into the
            // scene file instead of floating unattached. It has to happen after
            // AddChild, because the owner has to be an ancestor in the tree.
            container.AddChild(stone);
            stone.Owner = GetTree().EditedSceneRoot;

            LastPositions.Add(position);
        }
    }

    /// <summary>
    /// Removes only the stones this tool created, matched by name, so a re-run
    /// replaces the previous scatter without touching anything else that might
    /// be sitting under the same container. Returns how many were removed.
    /// </summary>
    private static int ClearStones(Node container)
    {
        int cleared = 0;

        for (int i = container.GetChildCount() - 1; i >= 0; i--)
        {
            Node child = container.GetChild(i);

            if (!(child is Sprite2D))
            {
                continue;
            }

            if (!child.Name.ToString().StartsWith(StoneNamePrefix))
            {
                continue;
            }

            // Detach first, then let the editor destroy the node at the end of
            // the frame. Freeing it here instead would pull the node out from
            // under the editor's own bookkeeping mid-walk.
            container.RemoveChild(child);
            child.QueueFree();
            cleared++;
        }

        return cleared;
    }

    #endregion
    //!-------------------------------------------------------------------------------------------------------------------

    //!-------------------------------------------------------------------------------------------------------------------
    #region Helpers
    //!-------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Walks up from this node to see whether the given node is the tool itself
    /// or one of its ancestors. Used to refuse a target container that clearing
    /// would empty out from under the running tool.
    /// </summary>
    private bool IsSelfOrAncestor(Node node)
    {
        Node current = this;
        while (current != null)
        {
            if (current == node)
            {
                return true;
            }

            current = current.GetParent();
        }

        return false;
    }

    /// <summary>
    /// Resolves an exported node path. An empty path falls back to the edited
    /// scene root, which is what the bounds search wants.
    /// </summary>
    private Node ResolveNode(NodePath path)
    {
        if (path != null && !path.IsEmpty)
        {
            Node found = GetNodeOrNull(path);
            if (found != null)
            {
                return found;
            }
            GD.PrintErr("STONE PLACER: path ", path, " did not resolve.");
            return null;
        }

        return GetTree().EditedSceneRoot;
    }

    /// <summary>
    /// A 0 to 1 random value from the shared generator. The explicit cast
    /// avoids NextSingle's float precision, which is plenty here anyway.
    /// </summary>
    private static float NextFloat(Random rng)
    {
        return (float)rng.NextDouble();
    }

    #endregion
    //!-------------------------------------------------------------------------------------------------------------------
}
