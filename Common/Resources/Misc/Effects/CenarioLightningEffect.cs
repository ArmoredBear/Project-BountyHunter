using Godot;
using System;
using System.Collections.Generic;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                          SCENARIO LIGHTNING EFFECT
*-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	*
**  1 - Forest-only variant of LightningEffect.cs. It drives the exact same
**      "flash_strength" uniform on the rain and the ground splash, so the main
**      menu's behaviour is untouched by this file.
**  2 - The bolts are drawn in WORLD space instead of screen space. Each one
**      is aimed from above the top of the view down onto the ground in front
**      of the camera, and every bolt width is expressed in world units, so the
**      bolts sit inside the scenario and scale with the camera zoom instead of
**      being pasted flat over the screen.
**  3 - There is no fullscreen flash quad. The light of the strike is a radial
**      glow drawn in world space where the bolts land, so it falls off across
**      the ground instead of washing the whole screen out.
**  4 - Each strike also blooms where it lands, so the bolt reads as hitting
**      something rather than fading out in mid air.
	**
*-----------------------------------------------------------------------------------------------------------------------**/

public partial class CenarioLightningEffect : Node2D
{
    //!---------------------------------------------------------------------------------------------------------
    #region Variables
    //!---------------------------------------------------------------------------------------------------------

    /// <summary>Minimum number of seconds between two strikes (picked randomly).</summary>
    [ExportCategory("Timing")]
    [Export(PropertyHint.Range, "0.1,30")] public float MinInterval = 8.0f;

    /// <summary>Maximum number of seconds between two strikes (picked randomly).</summary>
    [Export(PropertyHint.Range, "0.1,30")] public float MaxInterval = 18.0f;

    /// <summary>Total length of a single flash, from the first spark until fully dark.</summary>
    [Export(PropertyHint.Range, "0.2,3.0")] public float FlashDuration = 0.8f;

    /// <summary>Master switch for the world space flash glow. Turn off to keep only the bolts.</summary>
    [Export(PropertyHint.Range, "0.0,1.0")] public bool DrawWorldFlash = true;

    /// <summary>Radius of the world space flash glow, in world units. Falls off smoothly to nothing at the edge.</summary>
    [Export(PropertyHint.Range, "0,8000")] public float WorldFlashRadius = 2600.0f;

    /// <summary>Soft radial falloff used for the world space flash. A gradient texture is required, stacked hard circles read as visible rings.</summary>
    [Export] public Texture2D WorldFlashTexture;

    /// <summary>Peak brightness of the world space flash glow at its centre.</summary>
    [Export(PropertyHint.Range, "0.0,2.0")] public float WorldFlashIntensity = 0.5f;

    /// <summary>
    /// The strike light, configured in the inspector. This node is the source of truth: the
    /// extra lights needed for the remaining bolts are duplicated from it, so every value
    /// tuned here (height, blend mode, shadow, texture scale) applies to all of them.
    /// </summary>
    [Export] public PointLight2D ImpactLight;

    /// <summary>Extra ShaderMaterials that also receive the same "flash_strength" value (e.g. the rain and the ground splash).</summary>
    [ExportCategory("Flash")]
    [Export] public ShaderMaterial[] FlashExtraMaterials;

    /// <summary>GpuParticles2D nodes whose SelfModulate is brightened during a strike. Leave empty to disable.</summary>
    [Export] public GpuParticles2D[] FlashParticleNodes;

    /// <summary>How strongly the flash brightens the linked particle nodes (1 = white).</summary>
    [Export(PropertyHint.Range, "0.0,3.0")] public float ParticleFlashGlow = 1.5f;

    /// <summary>Master switch for the bolt polylines. Turn off to keep only the flash.</summary>
    [ExportCategory("Bolts")]
    [Export] public bool DrawBolts = true;

    /// <summary>How many main bolts are drawn per strike.</summary>
    [Export(PropertyHint.Range, "1,8")] public int BoltCount = 3;

    /// <summary>Stroke width of the bright core of each bolt, in world units.</summary>
    [Export(PropertyHint.Range, "1,200")] public float BoltWidth = 38.0f;

    /// <summary>Stroke width of the soft glow drawn underneath each bolt, in world units.</summary>
    [Export(PropertyHint.Range, "1,600")] public float BoltGlowWidth = 150.0f;

    /// <summary>Base alpha of the wide soft glow, scaled by the current flash strength.</summary>
    [Export(PropertyHint.Range, "0.0,1.0")] public float BoltGlowAlpha = 0.16f;

    /// <summary>Stroke width of the tight halo hugging the core, in world units. This is what sells the brightness.</summary>
    [Export(PropertyHint.Range, "1,400")] public float BoltHaloWidth = 95.0f;

    /// <summary>Base alpha of the tight halo.</summary>
    [Export(PropertyHint.Range, "0.0,1.0")] public float BoltHaloAlpha = 0.45f;

    /// <summary>Radius of the bloom where a bolt lands, in world units.</summary>
    [Export(PropertyHint.Range, "0,2000")] public float ImpactGlowRadius = 260.0f;

    /// <summary>Base alpha of the landing bloom.</summary>
    [Export(PropertyHint.Range, "0.0,1.0")] public float ImpactGlowAlpha = 0.15f;

    /// <summary>Fraction of the bolt length at each end that tapers to a sharp point.</summary>
    [Export(PropertyHint.Range, "0.02,0.5")] public float BoltTipTaper = 0.2f;

    /// <summary>Core color of the bolts.</summary>
    [Export] public Color BoltColor = new Color(0.8f, 0.9f, 1.0f);

    /// <summary>Color of the solid inner channel, kept brighter than the glow so the bolt has a hot centre.</summary>
    [Export] public Color BoltCoreColor = new Color(0.95f, 0.98f, 1.0f);

    /// <summary>Multiplier on the bolt colours that cancels out the scenario CanvasModulate (0.297), which dims everything drawn in the world canvas. Needs the HDR 2D buffer to have headroom above 1.0.</summary>
    [Export(PropertyHint.Range, "0.5,4.0")] public float BoltBrightness = 1.8f;

    // How fast a single flicker peak dies down (higher = sharper flash).
    private const float PulseDecay = 14.0f;

    private readonly List<List<Vector2>> _bolts = new List<List<Vector2>>();
    private readonly List<Vector2> _impacts = new List<Vector2>();
    private readonly List<Pulse> _pulses = new List<Pulse>();
    private readonly Random _rng = new Random();

    // Per strike variation for the light pool, so it never reads as one clean disc.
    private const int GlowLayers = 3;
    private readonly Vector2[] _glowOffset = new Vector2[GlowLayers];
    private readonly float[] _glowScale = new float[GlowLayers];
    private readonly float[] _glowSquash = new float[GlowLayers];

    // One real light per bolt, created on demand so BoltCount stays the only knob.
    private readonly List<PointLight2D> _impactLights = new List<PointLight2D>();

    // Energy read from the inspector node once, before _Process starts mutating it.
    private float _impactLightBaseEnergy = -1f;

    private bool _flashActive;
    private float _flashT;
    private float _timeUntilNextStrike = 1.0f;
    private float _fadeStart;

    private Camera2D _camera;
    private Vector2 _viewCenter;
    private Vector2 _viewSize = new Vector2(10240.0f, 4320.0f);

    #endregion
    //!---------------------------------------------------------------------------------------------------------
    #region Initialization and Processes
    //!---------------------------------------------------------------------------------------------------------

    public override void _Ready()
    {
        base._Ready();

        CacheView();
        _timeUntilNextStrike = RandRange(MinInterval, MaxInterval);
        EnsureImpactLights();
        SetFlashStrength(0f);
        SetImpactLight(0f);
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
                _impacts.Clear();
                SetFlashStrength(0f);
                SetImpactLight(0f);
            }
            else
            {
                float strength = ComputeStrength(_flashT);
                SetFlashStrength(strength);
                SetImpactLight(strength);
                QueueRedraw();
            }
        }
        else
        {
            _timeUntilNextStrike -= dt;
            if (_timeUntilNextStrike <= 0f)
            {
                TriggerStrike();
            }
        }
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------
    #region Strike Logic
    //!---------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Starts a new strike: builds the flicker pattern, regenerates the bolts
    /// against the camera's current position and schedules the next strike.
    /// </summary>
    private void TriggerStrike()
    {
        _flashActive = true;
        _flashT = 0f;

        CacheView();
        GenerateFlickerPattern();
        GenerateBolts();
        RandomizeGlow();
        EnsureImpactLights();

        for (int i = 0; i < _impactLights.Count && i < _impacts.Count; i++)
        {
            _impactLights[i].GlobalPosition = _impacts[i];
        }

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
        {
            _pulses.Add(new Pulse(RandRange(0.03f, 0.35f), RandRange(0.3f, 0.8f)));
        }

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
        if (t >= FlashDuration)
        {
            return 0f;
        }

        float s = 0f;
        for (int i = 0; i < _pulses.Count; i++)
        {
            Pulse p = _pulses[i];
            if (t >= p.Time)
            {
                s = Mathf.Max(s, p.Peak * Mathf.Exp(-PulseDecay * (t - p.Time)));
            }
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
        if (FlashExtraMaterials != null)
        {
            for (int i = 0; i < FlashExtraMaterials.Length; i++)
            {
                if (FlashExtraMaterials[i] != null)
                {
                    FlashExtraMaterials[i].SetShaderParameter("flash_strength", strength);
                }
            }
        }

        if (FlashParticleNodes != null)
        {
            float boost = 1f + strength * ParticleFlashGlow;
            Color c = new Color(boost, boost, boost, 1f);
            for (int i = 0; i < FlashParticleNodes.Length; i++)
            {
                if (FlashParticleNodes[i] != null)
                {
                    FlashParticleNodes[i].SelfModulate = c;
                }
            }
        }
    }

    /// <summary>
    /// Grows the strike light pool so there is one light per landing point. Light 0 is always
    /// the inspector configured ImpactLight node; the rest are duplicates of it, so nothing
    /// tuned on that node is lost. Lights are reused between strikes.
    /// </summary>
    private void EnsureImpactLights()
    {
        if (ImpactLight == null)
        {
            return;
        }

        int needed = Mathf.Max(_impacts.Count, 1);

        while (_impactLights.Count < needed)
        {
            if (_impactLights.Count == 0)
            {
                if (_impactLightBaseEnergy < 0f)
                {
                    _impactLightBaseEnergy = ImpactLight.Energy;
                }

                _impactLights.Add(ImpactLight);
            }
            else
            {
                PointLight2D copy = (PointLight2D)ImpactLight.Duplicate();
                copy.Name = ImpactLight.Name + _impactLights.Count;
                copy.Energy = 0f;
                AddChild(copy);
                _impactLights.Add(copy);
            }
        }

        for (int i = 0; i < _impactLights.Count; i++)
        {
            _impactLights[i].Visible = i < _impacts.Count;
        }
    }

    /// <summary>
    /// Drives every strike light so they pulse with the same curve as the flash and the bolts.
    /// One light per landing point, scaled from the energy set on the inspector node.
    /// </summary>
    private void SetImpactLight(float strength)
    {
        for (int i = 0; i < _impactLights.Count; i++)
        {
            if (_impactLights[i] != null)
            {
                _impactLights[i].Energy = i < _impacts.Count ? _impactLightBaseEnergy * strength : 0f;
            }
        }
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------
    #region View
    //!---------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Caches where the camera is looking and how much world space that covers,
    /// so every bolt can be aimed relative to what the player can actually see.
    /// </summary>
    private void CacheView()
    {
        _camera = GetViewport().GetCamera2D();

        Vector2 screen = GetViewportRect().Size;
        if (screen == Vector2.Zero)
        {
            screen = new Vector2(2560.0f, 1080.0f);
        }

        if (_camera == null)
        {
            _viewCenter = GlobalPosition;
            _viewSize = screen;
            return;
        }

        _viewCenter = _camera.GetScreenCenterPosition();
        _viewSize = screen / _camera.Zoom;
    }

    /// <summary>
    /// Scatters the layers of the light pool around the strike point with random
    /// sizes and a random vertical squash, so the glow has an uneven, organic
    /// edge instead of reading as a single perfect circle.
    /// </summary>
    private void RandomizeGlow()
    {
        for (int i = 0; i < GlowLayers; i++)
        {
            float angle = RandRange(0f, Mathf.Tau);
            float dist = RandRange(0f, WorldFlashRadius * 0.28f);
            _glowOffset[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
            _glowScale[i] = RandRange(0.55f, 1.0f);
            _glowSquash[i] = RandRange(0.55f, 0.92f);
        }
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------
    #region Bolt Generation
    //!---------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Regenerates all bolt polylines for the current strike, each main bolt
    /// getting one or two side branches, and remembers where each one lands.
    /// </summary>
    private void GenerateBolts()
    {
        _bolts.Clear();
        _impacts.Clear();

        for (int b = 0; b < BoltCount; b++)
        {
            List<Vector2> main = GenerateMainBolt();
            if (main == null || main.Count < 2)
            {
                continue;
            }

            _bolts.Add(main);
            _impacts.Add(main[main.Count - 1]);

            int branchCount = _rng.Next(1, 3);
            for (int i = 0; i < branchCount; i++)
            {
                int fromIdx = _rng.Next(2, main.Count - 2);
                List<Vector2> branch = GenerateBranchBolt(main, fromIdx);
                if (branch != null)
                {
                    _bolts.Add(branch);
                }
            }
        }
    }

    /// <summary>
    /// Builds one jagged main bolt in world space, starting from above the top
    /// of the view and landing on the ground inside the lower half of it.
    /// </summary>
    private List<Vector2> GenerateMainBolt()
    {
        float w = _viewSize.X;
        float h = _viewSize.Y;

        Vector2 end = _viewCenter + new Vector2(RandRange(-0.4f, 0.4f) * w, RandRange(0.12f, 0.42f) * h);
        Vector2 start = end + new Vector2(RandRange(-0.25f, 0.25f) * w, -h * RandRange(0.55f, 0.85f));

        int segments = 16;
        Vector2 step = (end - start) / segments;
        Vector2 normal = new Vector2(-step.Y, step.X).Normalized();
        float jitter = Mathf.Min(w, h) * 0.012f;

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

        float segmentLength = Mathf.Min(_viewSize.X, _viewSize.Y) * RandRange(0.01f, 0.02f);
        Vector2 step = dir * segmentLength;
        Vector2 normal = new Vector2(-step.Y, step.X).Normalized();
        int count = _rng.Next(4, 8);

        var points = new List<Vector2> { from };
        for (int i = 1; i < count; i++)
        {
            Vector2 basePoint = from + step * i;
            points.Add(basePoint + normal * RandRange(-0.3f, 0.3f) * BoltWidth);
        }
        return points;
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------
    #region Drawing
    //!---------------------------------------------------------------------------------------------------------

    public override void _Draw()
    {
        float alpha;
        if (_flashActive)
        {
            alpha = ComputeStrength(_flashT);
        }
        else
        {
            alpha = 0f;
        }

        if (alpha <= 0.01f)
        {
            return;
        }

        // World space flash: the light of the strike itself, centred where the
        // bolts land, instead of a fullscreen quad that washes out the scenario.
        if (DrawWorldFlash && _impacts.Count > 0)
        {
            DrawFlashPools(_impacts[0], WorldFlashIntensity * alpha);
        }

        if (!DrawBolts || _bolts.Count == 0)
        {
            return;
        }

        float a = Mathf.Min(1f, alpha);
        float k = BoltBrightness;
        Color wideGlow = new Color(BoltColor.R * k, BoltColor.G * k, BoltColor.B * k, BoltGlowAlpha * a);
        Color halo = new Color(BoltColor.R * k, BoltColor.G * k, BoltColor.B * k, BoltHaloAlpha * a);
        Color core = new Color(BoltCoreColor.R * k, BoltCoreColor.G * k, BoltCoreColor.B * k, a);
        Color hot = new Color(k, k, k, a);

        // Landing bloom first, so the bolt itself reads on top of it.
        for (int i = 0; i < _impacts.Count; i++)
        {
            DrawSoftPool(_impacts[i], ImpactGlowRadius, ImpactGlowAlpha * alpha);
        }

        for (int i = 0; i < _bolts.Count; i++)
        {
            Vector2[] points = _bolts[i].ToArray();

            // Four passes, from softest to hottest. The tight halo is what makes
            // the channel read as bright: without it the jump from the wide glow
            // straight to the core leaves the bolt looking thin and washed out.
            DrawColoredPolygon(BuildTaperedPolygon(points, BoltGlowWidth, BoltTipTaper), wideGlow);
            DrawColoredPolygon(BuildTaperedPolygon(points, BoltHaloWidth, BoltTipTaper), halo);
            DrawColoredPolygon(BuildTaperedPolygon(points, BoltWidth, BoltTipTaper), core);
            DrawPolyline(points, hot, Mathf.Max(1f, BoltWidth * 0.3f), true);
        }
    }

    /// <summary>
    /// Draws the light of the strike as a few overlapping soft radial pools.
    /// Each layer gets its own offset, size and squash from <see cref="RandomizeGlow"/>,
    /// plus a small per frame shimmer, so the edges stay irregular and the core
    /// still builds up where the layers overlap.
    /// </summary>
    private void DrawFlashPools(Vector2 center, float intensity)
    {
        if (WorldFlashTexture == null || WorldFlashRadius <= 0f || intensity <= 0f)
        {
            return;
        }

        for (int i = 0; i < GlowLayers; i++)
        {
            float shimmer = 0.88f + 0.12f * Mathf.Sin(_flashT * 47f + i * 2.1f);
            float w = WorldFlashRadius * 2f * _glowScale[i];
            float h = w * _glowSquash[i];
            Vector2 c = center + _glowOffset[i];
            float a = intensity * shimmer * (0.6f - 0.12f * i);

            DrawTextureRect(
                WorldFlashTexture,
                new Rect2(c.X - w * 0.5f, c.Y - h * 0.5f, w, h),
                false,
                new Color(BoltColor.R, BoltColor.G, BoltColor.B, a));
        }
    }

    /// <summary>
    /// Draws a single soft pool centred on a landing point, used for the small
    /// bloom right where a bolt meets the ground.
    /// </summary>
    private void DrawSoftPool(Vector2 center, float radius, float intensity)
    {
        if (WorldFlashTexture == null || radius <= 0f || intensity <= 0f)
        {
            return;
        }

        float d = radius * 2f;
        DrawTextureRect(
            WorldFlashTexture,
            new Rect2(center.X - radius, center.Y - radius, d, d),
            false,
            new Color(BoltColor.R, BoltColor.G, BoltColor.B, intensity));
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
        {
            polygon[i] = left[i];
        }
        for (int i = 0; i < n; i++)
        {
            polygon[n + i] = right[n - 1 - i];
        }
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
    #region Helpers
    //!---------------------------------------------------------------------------------------------------------

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
