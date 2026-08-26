using Godot;
using System;
using System.Collections.Generic;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                   LIGHTNINGEFFECT
*-----------------------------------------------------------------------------------------------------------------------**/
/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	*  
	**  1 - Randomly fires lightning strikes on the main menu at random intervals.
	**  2 - Drives a bright blue-white fullscreen flash through a ShaderMaterial (only the strength envelope is set here).
	**  3 - Draws a jagged bolt with a few branches on this node via _Draw.
	*
*-----------------------------------------------------------------------------------------------------------------------**/
public partial class LightningEffect : Node2D
{
    //!---------------------------------------------------------------------------------------------------------
    #region Variables
    //!---------------------------------------------------------------------------------------------------------

    // -- Timing -----------------------------------------------------------------
    // Minimum and maximum seconds between two strikes (picked randomly).
    [ExportCategory("Timing")]
    [Export(PropertyHint.Range, "0.1,30")] public float MinInterval = 5.0f;

    [Export(PropertyHint.Range, "0.1,30")] public float MaxInterval = 10.0f;

    // Total length of a single flash, from the first spark until fully dark.
    [Export(PropertyHint.Range, "0.2,3.0")] public float FlashDuration = 0.8f;

    // -- Flash ------------------------------------------------------------------
    // Fullscreen ColorRect carrying the LightningFlash shader. Its
    // "flash_strength" uniform is driven by this script every frame.
    [ExportCategory("Flash")]
    [Export] public ColorRect FlashRect;

    // Extra ShaderMaterials that also receive the same "flash_strength"
    // value (e.g. the rain material, so it lights up during a strike).
    // Leave empty to disable.
    [Export] public ShaderMaterial[] FlashExtraMaterials;

    // GpuParticles2D nodes (e.g. the snow) whose SelfModulate is brightened
    // during a strike. Leave empty to disable.
    [Export] public GpuParticles2D[] FlashParticleNodes;

    // How strongly the flash brightens the linked particle nodes (1 = white).
    [Export(PropertyHint.Range, "0.0,3.0")] public float ParticleFlashGlow = 1.5f;

    // -- Bolts ------------------------------------------------------------------
    // Master switch for the bolt polylines. Turn off to keep only the flash.
    [ExportCategory("Bolts")]
    [Export] public bool DrawBolts = true;

    // How many main bolts are drawn per strike.
    [Export(PropertyHint.Range, "1,8")] public int BoltCount = 3;

    // Stroke width of the bright core of each bolt.
    [Export(PropertyHint.Range, "1,10")] public float BoltWidth = 2.5f;

    // Stroke width of the soft glow drawn underneath each bolt.
    [Export(PropertyHint.Range, "1,60")] public float BoltGlowWidth = 14.0f;

    // Base alpha of the glow layer, scaled by the current flash strength.
    [Export(PropertyHint.Range, "0.0,1.0")] public float BoltGlowAlpha = 0.3f;

    // Fraction of the bolt length at each end that tapers to a sharp point.
    [Export(PropertyHint.Range, "0.02,0.5")] public float BoltTipTaper = 0.2f;

    // Core color of the bolts (usually a pale blue/white).
    [Export] public Color BoltColor = new Color(0.8f, 0.9f, 1.0f);

    // How fast a single flicker peak dies down (higher = sharper flash).
    private const float PulseDecay = 14.0f;

    // -- Runtime state ----------------------------------------------------------
    // Individual flickers (time + peak) that make up the current flash.
    private readonly List<Pulse> _pulses = new List<Pulse>();

    // Bolt polylines currently on screen.
    private readonly List<List<Vector2>> _bolts = new List<List<Vector2>>();

    // Scene size used to lay out the bolts (matches the stretched viewport).
    private Vector2 _visibleSize = new Vector2(2560, 1080);

    // True while a strike is running.
    private bool _flashActive;

    // Seconds elapsed inside the current flash.
    private float _flashT;

    // Time after which the flash starts its final fade-out.
    private float _fadeStart;

    // Countdown until the next random strike.
    private float _timeUntilNextStrike = 1.0f;

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Initialization and Processes
    //!---------------------------------------------------------------------------------------------------------

    public override void _Ready()
    {
        base._Ready();
        _visibleSize = GetViewportRect().Size;
        if (_visibleSize == Vector2.Zero)
            _visibleSize = new Vector2(2560, 1080);

        _timeUntilNextStrike = RandRange(MinInterval, MaxInterval);
        SetFlashStrength(0f);
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;

        if (_flashActive)
        {
            _flashT += dt;
            if (_flashT >= FlashDuration)
            {
                _flashActive = false;
                _bolts.Clear();
                SetFlashStrength(0f);
            }
            else
            {
                float strength = ComputeStrength(_flashT);
                SetFlashStrength(strength);
                QueueRedraw();
            }
        }
        else
        {
            _timeUntilNextStrike -= dt;
            if (_timeUntilNextStrike <= 0f)
                TriggerStrike();
        }
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Strike Logic
    //!---------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Starts a new strike: builds the flicker pattern, regenerates the bolts
    /// and schedules the next strike.
    /// </summary>
    private void TriggerStrike()
    {
        _flashActive = true;
        _flashT = 0f;

        GenerateFlickerPattern();

        if (DrawBolts)
            GenerateBolts();
        else
            _bolts.Clear();

        SetFlashStrength(1f);
        QueueRedraw();

        _timeUntilNextStrike = RandRange(MinInterval, MaxInterval);
    }

    /// <summary>
    /// Generates the random set of flickers for this strike. The first pulse
    /// always hits at t = 0 with full strength; 1 to 3 re-strikes follow.
    /// </summary>
    private void GenerateFlickerPattern()
    {
        _pulses.Clear();
        _pulses.Add(new Pulse(0f, 1f));

        int extra = _rng.Next(1, 4);
        for (int i = 0; i < extra; i++)
            _pulses.Add(new Pulse(RandRange(0.03f, 0.35f), RandRange(0.3f, 0.8f)));

        _pulses.Sort((a, b) => a.Time.CompareTo(b.Time));

        float last = _pulses[_pulses.Count - 1].Time;
        _fadeStart = Mathf.Min(last + 0.08f, FlashDuration * 0.6f);
    }

    /// <summary>
    /// Returns the flash strength (0..1) at time <paramref name="t"/>, produced
    /// by summing the decaying flicker peaks and applying the final fade-out.
    /// </summary>
    private float ComputeStrength(float t)
    {
        if (t >= FlashDuration) return 0f;

        float s = 0f;
        for (int i = 0; i < _pulses.Count; i++)
        {
            var p = _pulses[i];
            if (t >= p.Time)
                s = Mathf.Max(s, p.Peak * Mathf.Exp(-PulseDecay * (t - p.Time)));
        }

        float fade = 1f;
        if (t > _fadeStart)
        {
            float span = Mathf.Max(0.001f, FlashDuration - _fadeStart);
            fade = Mathf.Clamp(1f - (t - _fadeStart) / span, 0f, 1f);
        }

        return Mathf.Clamp(s * fade, 0f, 1f);
    }

    /// <summary>
    /// Pushes the current strength into the flash shader's uniform, into any
    /// linked extra materials, and into the SelfModulate of any linked particle
    /// nodes, so everything reacts to the strike.
    /// </summary>
    private void SetFlashStrength(float strength)
    {
        if (FlashRect != null && FlashRect.Material is ShaderMaterial material)
            material.SetShaderParameter("flash_strength", strength);

        if (FlashExtraMaterials != null)
        {
            for (int i = 0; i < FlashExtraMaterials.Length; i++)
            {
                if (FlashExtraMaterials[i] != null)
                    FlashExtraMaterials[i].SetShaderParameter("flash_strength", strength);
            }
        }

        if (FlashParticleNodes != null)
        {
            float boost = 1f + strength * ParticleFlashGlow;
            Color c = new Color(boost, boost, boost, 1f);
            for (int i = 0; i < FlashParticleNodes.Length; i++)
            {
                if (FlashParticleNodes[i] != null)
                    FlashParticleNodes[i].SelfModulate = c;
            }
        }
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Bolt Generation
    //!---------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Regenerates all bolt polylines for the current strike, each main bolt
    /// getting one or two side branches.
    /// </summary>
    private void GenerateBolts()
    {
        _bolts.Clear();
        for (int b = 0; b < BoltCount; b++)
        {
            var main = GenerateMainBolt();
            if (main == null || main.Count < 2) continue;
            _bolts.Add(main);

            int branchCount = _rng.Next(1, 3);
            for (int i = 0; i < branchCount; i++)
            {
                int fromIdx = _rng.Next(2, main.Count - 2);
                var branch = GenerateBranchBolt(main, fromIdx);
                if (branch != null)
                    _bolts.Add(branch);
            }
        }
    }

    /// <summary>
    /// Builds one jagged main bolt from above the screen down into the scene,
    /// with random horizontal displacement growing toward the bottom.
    /// </summary>
    private List<Vector2> GenerateMainBolt()
    {
        float w = _visibleSize.X;
        float h = _visibleSize.Y;

        Vector2 start = new Vector2(RandRange(w * 0.05f, w * 0.95f), -20f);
        Vector2 end = new Vector2(start.X + RandRange(-w * 0.3f, w * 0.3f), RandRange(h * 0.3f, h * 0.75f));

        int segments = 16;
        Vector2 step = (end - start) / segments;
        Vector2 normal = new Vector2(-step.Y, step.X).Normalized();
        float jitter = Mathf.Min(w, h) * 0.02f;

        var points = new List<Vector2> { start };
        for (int i = 1; i < segments; i++)
        {
            Vector2 basePoint = start + step * i;
            float spread = jitter * (0.4f + 0.6f * (i / (float)segments));
            points.Add(basePoint + normal * RandRange(-spread, spread));
        }
        points.Add(end);
        return points;
    }

    /// <summary>
    /// Builds a shorter branch that splits off a point of an existing bolt,
    /// angled away from the main direction.
    /// </summary>
    private List<Vector2> GenerateBranchBolt(List<Vector2> main, int fromIdx)
    {
        Vector2 from = main[fromIdx];
        Vector2 dir = (main[fromIdx + 1] - main[fromIdx]).Normalized();
        dir = dir.Rotated(RandRange(-1.1f, 0.5f));

        float segmentLength = _visibleSize.Y * RandRange(0.015f, 0.03f);
        Vector2 step = dir * segmentLength;
        Vector2 normal = new Vector2(-step.Y, step.X).Normalized();
        int count = _rng.Next(4, 8);

        var points = new List<Vector2> { from };
        for (int i = 1; i < count; i++)
        {
            Vector2 basePoint = from + step * i;
            points.Add(basePoint + normal * RandRange(-4f, 4f));
        }
        return points;
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Drawing
    //!---------------------------------------------------------------------------------------------------------

    public override void _Draw()
    {
        if (!DrawBolts || _bolts.Count == 0) return;

        float alpha;
        if (_flashActive)
        {
            alpha = ComputeStrength(_flashT);
        }
        else
        {
            alpha = 0f;
        }
        if (alpha <= 0.01f) return;

        Color glow = new Color(BoltColor.R, BoltColor.G, BoltColor.B, BoltGlowAlpha * alpha);
        Color core = new Color(BoltColor.R, BoltColor.G, BoltColor.B, Mathf.Min(1f, alpha));

        for (int i = 0; i < _bolts.Count; i++)
        {
            Vector2[] points = _bolts[i].ToArray();

            DrawColoredPolygon(BuildTaperedPolygon(points, BoltGlowWidth, BoltTipTaper), glow);
            DrawColoredPolygon(BuildTaperedPolygon(points, BoltWidth, BoltTipTaper), core);
            DrawPolyline(points, new Color(1f, 1f, 1f, Mathf.Min(1f, alpha) * 0.8f), 1f, true);
        }
    }

    /// <summary>
    /// Builds a closed ribbon around a bolt path whose width shrinks to zero at
    /// both ends, producing a sharp, pointy tip instead of a flat line cap.
    /// <paramref name="maxWidth"/> is the widest point (the middle of the bolt)
    /// and <paramref name="tipRatio"/> controls how much of each end tapers.
    /// </summary>
    private static Vector2[] BuildTaperedPolygon(Vector2[] path, float maxWidth, float tipRatio)
    {
        int n = path.Length;
        var left = new Vector2[n];
        var right = new Vector2[n];

        for (int i = 0; i < n; i++)
        {
            Vector2 dir = DirectionAt(path, i);
            Vector2 normal = new Vector2(-dir.Y, dir.X);

            float fromStart = i / (float)(n - 1);
            float fromEnd = 1f - fromStart;
            float taper = Mathf.Min(fromStart, fromEnd) / tipRatio;
            taper = Mathf.Clamp(taper, 0f, 1f);
            taper = MathUtils.SmoothStep(taper);

            float half = maxWidth * 0.5f * taper;
            left[i] = path[i] + normal * half;
            right[i] = path[i] - normal * half;
        }

        var polygon = new Vector2[n * 2];
        for (int i = 0; i < n; i++)
            polygon[i] = left[i];
        for (int i = 0; i < n; i++)
            polygon[n + i] = right[n - 1 - i];
        return polygon;
    }

    /// <summary>
    /// Returns a normalized direction at index <paramref name="i"/> based on its
    /// neighbours, falling back to the adjacent segment at the endpoints.
    /// </summary>
    private static Vector2 DirectionAt(Vector2[] path, int i)
    {
        Vector2 prev = path[Mathf.Max(0, i - 1)];
        Vector2 next = path[Mathf.Min(path.Length - 1, i + 1)];
        return (next - prev).Normalized();
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Helpers
    //!---------------------------------------------------------------------------------------------------------

    private readonly Random _rng = new Random();

    /// <summary>
    /// Returns a random float between <paramref name="min"/> and
    /// <paramref name="max"/>, inclusive.
    /// </summary>
    private static float RandRange(float min, float max) => (float)GD.RandRange(min, max);

    /// <summary>
    /// A single flicker: the peak strength at a given offset time.
    /// </summary>
    private struct Pulse
    {
        public float Time;
        public float Peak;

        public Pulse(float time, float peak)
        {
            Time = time;
            Peak = peak;
        }
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------
}
