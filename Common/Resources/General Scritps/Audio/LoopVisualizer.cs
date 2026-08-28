using Godot;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                   LOOPVISUALIZER
*-----------------------------------------------------------------------------------------------------------------------**/

/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	*  
	**  1 - Same visual as AudioVisualizer, but NOT tied to the audio bus.
	**  2 - Bars are animated by a fixed, looping array of amplitude values
	**		instead of a real-time spectrum, so it works with zero audio.
	**  3 - The loop scrolls across the bars over time, wrapped around, and
	**		keeps the same attack/decay smoothing for the visualizer feel.
	*
*-----------------------------------------------------------------------------------------------------------------------**/

public partial class LoopVisualizer : Node2D
{
    //!---------------------------------------------------------------------------------------------------------
    #region Variables
    //!---------------------------------------------------------------------------------------------------------

    // The fixed pattern of amplitude values (0.0 to 1.0) the bars scroll through.
    // Bars sample this array with an offset that advances over time, wrapping around.
    // Ignored when RandomizedLoop is true.
    [Export] public float[] LoopValues = {
        0f, 0.05f, 1f, 0.8f, 0.55f, 0.35f, 0.2f, 0.05f, 0f, 0f,
        0.15f, 0.9f, 1f, 0.6f, 0.4f, 0.25f, 0.05f, 0f, 0f, 0f,
        0.45f, 0.3f, 0.75f, 0.4f, 0.1f, 0f, 1f, 0.85f, 0.5f, 0.3f,
        0.15f, 0.05f, 0.6f, 0.9f, 0.35f, 0.2f, 0.1f, 0f, 0.05f, 0f,
        0.7f, 0.55f, 0.25f, 1f, 0.8f, 0.4f, 0.1f, 0f
    };

    // When true, the loop is generated randomly at runtime instead of using LoopValues.
    // The random pattern is seamless: its first and last value are the same, so the
    // wrap-around after the whole array passes has no jump and no dip to zero.
    [Export] public bool RandomizedLoop = true;

    [Export] public int NumBars = 24;               // How many vertical bars to draw
    [Export] public float LoopSpeed = 6f;           // How many array indices the loop advances per second
    [Export] public float Amplitude = 1f;           // Global scale applied to every value in the loop
    [Export] public float MaxHeightRatio = 0.4f;    // Max bar height as a fraction of screen height (0.0 to 1.0)
    [Export] public float VisualizerWidth = 256f;   // Total width of the whole visualizer in pixels (both mirrored sides)
    [Export] public float BarWidth = 2.66f;         // Fixed thickness of each bar in pixels; never changes when resizing
    [Export] public Color BarColor = Colors.Cyan;   // The main color of the bars
    [Export] public float SmoothSpeed = 15f;        // How fast bars rise when values increase (attack speed)
    [Export] public float DecaySpeed = 4f;          // How fast bars fall when values decrease (release speed)
    [Export] public float PointRatio = 0.3f;        // How much of each bar is the pointed tip vs the rectangular body
    [Export] public bool GlowEnabled = true;        // Toggle the outer glow effect on/off
    [Export] public float GlowExpand = 4f;          // How many pixels the outer glow extends beyond the bar edges
    [Export] public Color GlowColor = new Color(0f, 1f, 1f, 0.3f);  // Outer glow color (cyan, semi-transparent)
    [Export] public float InnerGlowShrink = 2f;     // How many pixels the inner glow is inset from the bar edges
    [Export] public Color InnerGlowColor = new Color(1f, 1f, 1f, 0.6f);  // Inner highlight color (white, semi-transparent)
    [Export] public float InnerGlowHeightRatio = 0.5f;  // Inner glow height relative to the bar height (0.0 to 1.0)
    [Export] public bool CenterLineEnabled = true;  // Toggle the horizontal center line on/off
    [Export] public Color CenterLineColor = new Color(1f, 1f, 1f, 0.4f);  // Center line color
    [Export] public float CenterLineWidth = 1f;     // Center line thickness in pixels
    [Export] public bool CenterGlowEnabled = true;  // Toggle the thin glowing inner center line on/off
    [Export] public float CenterGlowWidth = 1f;     // Thickness of the thin glowing inner line
    [Export] public float CenterGlowExpand = 4f;    // How far the glow halo extends beyond the thin line
    [Export] public Color CenterGlowColor = new Color(1f, 1f, 1f, 0.9f);  // Thin inner line color (bright)
    [Export] public float CenterGap = 0f;           // Empty space between the center line and where bars start
    [Export] public bool FadeEnabled = true;        // Fade the whole visualizer in/out instead of snapping
    [Export] public float FadeInSpeed = 8f;         // How fast it fades in when shown (attack)
    [Export] public float FadeOutSpeed = 5f;        // How fast it fades out when hidden (release)

    // Stores the current smoothed height of each bar (values from 0.0 to 1.0).
    private float[] _currentAmplitudes;

    // Current time-based offset into the loop, in array indices.
    private float _loopOffset;

    // The actual pattern the bars read from (random or the exported LoopValues).
    private float[] _pattern;

    // Current fade alpha of the whole visualizer (0.0 transparent, 1.0 opaque)
    // and where it is heading. Driven by Show/HideVisualizer.
    private float _fadeAlpha;
    private float _fadeTarget;

    // Bar-height activity ramp: 0 while idle (bars at 0 height), 1 while hovered
    // (bars at their full loop values). Makes bars rise/fall with the hover.
    private float _activity;

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Initialization and Processes
    //!---------------------------------------------------------------------------------------------------------

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        EnsureArraySize();

        // Start fully hidden, ready for a fade-in on the first show.
        _fadeAlpha = 0f;
        _fadeTarget = 0f;
        _activity = 0f;
        SelfModulate = new Color(1f, 1f, 1f, 0f);
    }

    // Called every frame. 'delta' is the time elapsed since the last frame in seconds.
    public override void _Process(double delta)
    {
        // Skip all work while hidden or not in the tree (e.g. between hovers).
        if (!IsInsideTree() || !Visible) return;

        EnsureArraySize();
        EnsurePattern();

        float dt = (float)delta;

        // === FADE ===
        // Move the whole visualizer's transparency toward its target, using
        // different speeds for fade-in and fade-out. Once fully faded out it
        // stops working entirely (skipped by the Visible guard at the top).
        if (FadeEnabled)
        {
            if (_fadeTarget > _fadeAlpha)
                _fadeAlpha = Mathf.Lerp(_fadeAlpha, _fadeTarget, FadeInSpeed * dt);
            else
                _fadeAlpha = Mathf.Lerp(_fadeAlpha, _fadeTarget, FadeOutSpeed * dt);

            SelfModulate = new Color(1f, 1f, 1f, _fadeAlpha);

            if (_fadeTarget <= 0f && _fadeAlpha < 0.01f)
            {
                Visible = false;
                return;
            }
        }
        else
        {
            SelfModulate = new Color(1f, 1f, 1f, 1f);
        }

        // === BAR ACTIVITY ===
        // Ramp the bar heights toward the hover target on every frame, so the
        // bars grow from 0 when shown and sink back to 0 when hidden. Reuses the
        // fade speeds for the rise/fall cadence.
        if (_fadeTarget > _activity)
            _activity = Mathf.Lerp(_activity, _fadeTarget, FadeInSpeed * dt);
        else
            _activity = Mathf.Lerp(_activity, _fadeTarget, FadeOutSpeed * dt);

        int count = _pattern.Length;

        // Advance the loop as long as we have values to cycle through.
        if (count > 0)
            _loopOffset = (_loopOffset + LoopSpeed * dt) % count;

        for (int i = 0; i < NumBars; i++)
        {
            // Read the value at this bar's position in the loop, linearly
            // interpolating between array entries so the scroll is smooth.
            float target;
            if (count == 0)
            {
                target = 0f;
            }
            else
            {
                float pos = i + _loopOffset;
                int idx = (int)Mathf.Floor(pos) % count;
                int next = (idx + 1) % count;
                float frac = pos - Mathf.Floor(pos);
                target = Mathf.Lerp(_pattern[idx], _pattern[next], frac) * Amplitude * _activity;
            }

            // === SMOOTHING (Attack/Release envelope) ===
            // Same behaviour as AudioVisualizer: when the target is higher,
            // rise fast (attack); when lower, fall slowly (decay/trailing).
            if (target > _currentAmplitudes[i])
                _currentAmplitudes[i] = Mathf.Lerp(_currentAmplitudes[i], target, SmoothSpeed * dt);
            else
                _currentAmplitudes[i] = Mathf.Lerp(_currentAmplitudes[i], target, DecaySpeed * dt);
        }

        // Tell Godot to redraw the node (triggers the _Draw() method).
        QueueRedraw();
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Methods and Interfaces
    //!---------------------------------------------------------------------------------------------------------

    // Fades the visualizer in. Called by whatever owns it (e.g. hover controller).
    public void ShowVisualizer()
    {
        if (!IsInsideTree()) return;

        _fadeTarget = 1f;

        if (!FadeEnabled)
        {
            _fadeAlpha = 1f;
            SelfModulate = new Color(1f, 1f, 1f, 1f);
        }

        Visible = true;
    }

    // Fades the visualizer out. With FadeEnabled it stays visible until the
    // fade finishes, then disables itself automatically.
    public void HideVisualizer()
    {
        _fadeTarget = 0f;

        if (!FadeEnabled)
        {
            Visible = false;
            return;
        }
    }

    // Keeps the smoothing buffer in sync with the current NumBars, so it can
    // never be smaller than the loop bound (protects against out-of-range reads).
    private void EnsureArraySize()
    {
        if (_currentAmplitudes == null || _currentAmplitudes.Length != NumBars)
            _currentAmplitudes = new float[NumBars];
    }

    // Resolves which array drives the loop: the randomized pattern when
    // RandomizedLoop is on, otherwise the exported LoopValues.
    private void EnsurePattern()
    {
        if (RandomizedLoop)
        {
            if (_pattern == null || _pattern.Length != NumBars || NumBars == 0)
                GenerateRandomPattern(NumBars);
        }
        else
        {
            _pattern = LoopValues;
        }
    }

    // Builds a random, seamless loop of 'count' values.
    // The first and last entries are forced to the same value, so scrolling
    // through the whole array and wrapping back never snaps or dips to zero.
    private void GenerateRandomPattern(int count)
    {
        if (count <= 0)
        {
            _pattern = new float[0];
            return;
        }

        // Fill with random amplitudes, keeping them clear of the floor.
        var rng = new RandomNumberGenerator();
        rng.Randomize();

        float[] raw = new float[count];
        for (int i = 0; i < count; i++)
            raw[i] = rng.RandfRange(0.15f, 1.0f);

        // Circular 3-point low-pass so neighbouring bars read naturally instead
        // of flickering between unrelated values.
        float[] smooth = new float[count];
        for (int i = 0; i < count; i++)
        {
            int prev = (i - 1 + count) % count;
            int next = (i + 1) % count;
            smooth[i] = raw[prev] * 0.25f + raw[i] * 0.5f + raw[next] * 0.25f;
        }

        // Seal the loop: last value equals the first, so the wrap is seamless.
        smooth[count - 1] = smooth[0];

        _pattern = smooth;
    }

    // Called when QueueRedraw() is invoked.
    // This is where all the actual rendering happens.
    public override void _Draw()
    {
        EnsureArraySize();

        // Get the size of the viewport (the game window).
        var viewportSize = GetViewportRect().Size;

        // The drawing center is the node's local origin.
        Vector2 center = Vector2.Zero;

        // Endpoints of the center line, stored so the glow can reuse them.
        Vector2 lineLeft = Vector2.Zero;
        Vector2 lineRight = Vector2.Zero;

        // The whole visualizer is VisualizerWidth pixels wide, split in half for the
        // two mirrored sides (bars go out both ways from the center).
        float barAreaWidth = VisualizerWidth * 0.5f;

        // Bar thickness is fixed. Resizing the whole visualizer only widens or
        // narrows the gaps between bars; the bars themselves stay identical.
        float barWidth = BarWidth;
        float spaceForGaps = barAreaWidth - NumBars * barWidth;
        float barSpacing = Mathf.Max(0f, spaceForGaps / Mathf.Max(1, NumBars - 1));

        // Calculate the maximum height a bar can reach, based on screen height and the ratio setting.
        float maxHeight = viewportSize.Y * MaxHeightRatio;

        // Base Y positions for the bars, offset from the center line so an empty
        // strip remains in the middle (bars "come out" of that gap, not the line).
        float upBase = center.Y - CenterGap;
        float downBase = center.Y + CenterGap;

        // Draw the horizontal center line if enabled.
        if (CenterLineEnabled)
        {
            var transform = GetGlobalTransformWithCanvas();
            Vector2 globalCenter = transform * center;
            lineLeft = transform.AffineInverse() * new Vector2(0f, globalCenter.Y);
            lineRight = transform.AffineInverse() * new Vector2(viewportSize.X, globalCenter.Y);
            DrawLine(lineLeft, lineRight, CenterLineColor, CenterLineWidth);
        }

        // Draw the thin glowing inner line, centered inside the center line.
        if (CenterGlowEnabled && CenterGlowWidth > 0f && CenterLineEnabled)
        {
            DrawLine(lineLeft, lineRight, FadeAlpha(CenterGlowColor, 0.15f), CenterGlowWidth + CenterGlowExpand * 3f);
            DrawLine(lineLeft, lineRight, FadeAlpha(CenterGlowColor, 0.35f), CenterGlowWidth + CenterGlowExpand * 2f);
            DrawLine(lineLeft, lineRight, CenterGlowColor, CenterGlowWidth);
        }

        // === THREE-PASS RENDERING ===
        // Pass 0: outer glow (behind), Pass 1: main bar, Pass 2: inner glow.
        // All bars are done per pass to minimize draw call overhead.
        for (int pass = 0; pass < 3; pass++)
        {
            for (int i = 0; i < NumBars; i++)
            {
                float height = _currentAmplitudes[i] * maxHeight;

                float rightX = center.X + i * (barWidth + barSpacing);
                float leftX = center.X - (i + 1) * (barWidth + barSpacing);

                DrawBarPass(rightX, upBase, barWidth, height, 1f, pass);
                DrawBarPass(rightX, downBase, barWidth, height, -1f, pass);
                DrawBarPass(leftX, upBase, barWidth, height, 1f, pass);
                DrawBarPass(leftX, downBase, barWidth, height, -1f, pass);
            }
        }
    }

    // Handles drawing one pass (glow, main, or inner glow) for a single bar.
    private void DrawBarPass(float x, float baseY, float width, float height, float direction, int pass)
    {
        if (height < 1f) return;

        if (pass == 0 && GlowEnabled)
        {
            DrawBarShape(
                x - GlowExpand,
                baseY,
                width + GlowExpand * 2f,
                height + GlowExpand,
                direction,
                GlowColor
            );
        }
        else if (pass == 1)
        {
            DrawBarShape(x, baseY, width, height, direction, BarColor);
        }
        else if (pass == 2 && GlowEnabled && width > InnerGlowShrink * 2f)
        {
            float innerHeight = Mathf.Min(height * InnerGlowHeightRatio, height - InnerGlowShrink);

            if (innerHeight >= 4f)
                DrawBarShape(
                    x + InnerGlowShrink,
                    baseY,
                    width - InnerGlowShrink * 2f,
                    innerHeight,
                    direction,
                    InnerGlowColor
                );
        }
    }

    // Draws a single bar shape as a colored polygon (rectangular body + pointed tip).
    private void DrawBarShape(float x, float baseY, float width, float height, float direction, Color color)
    {
        float tipHeight = height * PointRatio;
        float bodyHeight = height - tipHeight;

        float tipX = x + width * 0.5f;

        Vector2[] points;

        if (bodyHeight < 1f)
        {
            points = new Vector2[]
            {
                new(x, baseY),
                new(tipX, baseY - height * direction),
                new(x + width, baseY),
            };
        }
        else
        {
            points = new Vector2[]
            {
                new(x, baseY),
                new(x, baseY - bodyHeight * direction),
                new(tipX, baseY - height * direction),
                new(x + width, baseY - bodyHeight * direction),
                new(x + width, baseY),
            };
        }

        DrawColoredPolygon(points, color);
    }

    // Returns a copy of 'color' with its alpha multiplied by 'multiplier'.
    private static Color FadeAlpha(Color color, float multiplier)
    {
        return new Color(color.R, color.G, color.B, color.A * multiplier);
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------
}