using Godot;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                   AUDIOVISUALIZER
*-----------------------------------------------------------------------------------------------------------------------**/

/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	*  
	**  1 - Creates a real-time audio spectrum visualizer that listens to the Master audio bus.
	**  2 - Draws animated bars that react to music/sounds, mirrored from the center for a symmetrical display.
	*
*-----------------------------------------------------------------------------------------------------------------------**/

// This script creates a real-time audio spectrum visualizer.
// It listens to the Master audio bus and draws animated bars that react to music/sounds.
// The bars are mirrored from the center, creating a symmetrical display.
public partial class AudioVisualizer : Node2D
{
    //!---------------------------------------------------------------------------------------------------------
    #region Variables
    //!---------------------------------------------------------------------------------------------------------

    [Export] public int NumBars = 128;              // How many vertical bars to draw
    [Export] public float MaxHeightRatio = 0.4f;    // Max bar height as a fraction of screen height (0.0 to 1.0)
    [Export] public Color BarColor = Colors.Cyan;   // The main color of the bars
    [Export] public float BarSpacing = 2f;          // Pixels of empty space between each bar
    [Export] public float SmoothSpeed = 15f;        // How fast bars rise when volume increases (attack speed)
    [Export] public float DecaySpeed = 4f;          // How fast bars fall when volume decreases (release speed)
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

    // Reference to the spectrum analyzer instance on the Master audio bus.
    // This is what gives us real-time frequency magnitude data.
    private AudioEffectSpectrumAnalyzerInstance _analyzer;

    // Stores the current smoothed height of each bar (values from 0.0 to 1.0).
    // We smooth these over time so the bars don't jitter erratically.
    private float[] _currentAmplitudes;

    // Static flag that ensures we only add the spectrum analyzer effect to the audio bus ONCE,
    // even if multiple instances of this node exist in the scene.
    private static bool _analyzerAdded;

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Initialization and Processes
    //!---------------------------------------------------------------------------------------------------------

    // Called when the node enters the scene tree for the first time.
    // Sets up the spectrum analyzer and prepares the amplitude array.
    public override void _Ready()
    {
        // Create an array to hold the current height of each bar, initialized to 0.
        _currentAmplitudes = new float[NumBars];

        // Get the index of the "Master" audio bus (the main output bus).
        int busIdx = AudioServer.GetBusIndex("Master");

        // Only add the spectrum analyzer effect to the bus if it hasn't been added yet.
        if (!_analyzerAdded)
        {
            // Create the spectrum analyzer effect.
            var analyzer = new AudioEffectSpectrumAnalyzer();

            // FFT size determines the frequency resolution.
            // Size_2048 gives us 1024 frequency bins, which is good for a 128-bar visualizer.
            analyzer.FftSize = AudioEffectSpectrumAnalyzer.FftSizeEnum.Size2048;

            // How much audio history to keep (in seconds).
            // 2.0 seconds means the analyzer looks at the last 2 seconds of audio.
            analyzer.BufferLength = 2.0f;

            // Add the effect to position 0 on the Master bus.
            AudioServer.AddBusEffect(busIdx, analyzer, 0);

            // Mark that we've added it so no other instance duplicates it.
            _analyzerAdded = true;
        }

        // Loop through all effects on the Master bus to find our spectrum analyzer instance.
        for (int i = 0; i < AudioServer.GetBusEffectCount(busIdx); i++)
        {
            // Check if this effect is a spectrum analyzer.
            if (AudioServer.GetBusEffect(busIdx, i) is AudioEffectSpectrumAnalyzer)
            {
                // Get the runtime instance of the analyzer (needed to query magnitude data).
                _analyzer = (AudioEffectSpectrumAnalyzerInstance)
                    AudioServer.GetBusEffectInstance(busIdx, i);
                break;
            }
        }
    }

    // Called every frame. 'delta' is the time elapsed since the last frame in seconds.
    // This is where we read audio data and smooth the bar heights.
    public override void _Process(double delta)
    {
        float dt = (float)delta;

        // Loop through each bar and calculate its current amplitude.
        for (int i = 0; i < NumBars; i++)
        {
            // Calculate the frequency range for this bar.
            // We use logarithmic spacing because human hearing perceives pitch logarithmically.
            // The range goes from 20 Hz (low bass) to 20,000 Hz (high treble).
            // Lower bars get a narrower frequency range (more detail on bass),
            // while higher bars cover a wider range.
            float freqMin = 20f * Mathf.Pow(20000f / 20f, (float)i / NumBars);
            float freqMax = 20f * Mathf.Pow(20000f / 20f, (float)(i + 1) / NumBars);

            // Get the magnitude (loudness) of the audio in this frequency range.
            // Returns a Vector2 where X = left channel, Y = right channel.
            Vector2 mag = _analyzer.GetMagnitudeForFrequencyRange(freqMin, freqMax);

            // Average the left and right channels to get a single raw amplitude value.
            float rawAmplitude = (mag.X + mag.Y) * 0.5f;

            // Convert the raw amplitude to a normalized 0-to-1 value.
            float target = AmplitudeToDb(rawAmplitude);

            // === SMOOTHING (Attack/Release envelope) ===
            // If the new target is HIGHER than the current value, use fast "attack" smoothing.
            // If the new target is LOWER, use slower "decay" smoothing.
            // This creates the classic visualizer effect: bars jump up quickly to match the beat,
            // then fall slowly for a trailing/sweeping look.
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

    // Called when QueueRedraw() is invoked.
    // This is where all the actual rendering happens.
    public override void _Draw()
    {
        // Get the size of the viewport (the game window).
        var viewportSize = GetViewportRect().Size;

        // The drawing center is the node's local origin.
        Vector2 center = Vector2.Zero;

        // Endpoints of the center line, stored so the glow can reuse them.
        Vector2 lineLeft = Vector2.Zero;
        Vector2 lineRight = Vector2.Zero;

        // The bars occupy the left half of the screen width (and are mirrored to the right).
        float barAreaWidth = viewportSize.X * 0.5f;

        // Calculate the total width available after accounting for spacing between bars.
        float totalWidth = barAreaWidth - BarSpacing;

        // Calculate how wide each individual bar should be.
        float barWidth = totalWidth / NumBars;

        // Calculate the maximum height a bar can reach, based on screen height and the ratio setting.
        float maxHeight = viewportSize.Y * MaxHeightRatio;

        // Base Y positions for the bars, offset from the center line so an empty
        // strip remains in the middle (bars "come out" of that gap, not the line).
        float upBase = center.Y - CenterGap;
        float downBase = center.Y + CenterGap;

        // Draw the horizontal center line if enabled.
        // The screen's left/right edges are mapped into this node's local drawing
        // space (via the inverse canvas transform), so the line always spans the
        // full visible screen width regardless of where this node sits in the scene.
        if (CenterLineEnabled)
        {
            var transform = GetGlobalTransformWithCanvas();
            Vector2 globalCenter = transform * center;
            lineLeft = transform.AffineInverse() * new Vector2(0f, globalCenter.Y);
            lineRight = transform.AffineInverse() * new Vector2(viewportSize.X, globalCenter.Y);
            DrawLine(lineLeft, lineRight, CenterLineColor, CenterLineWidth);
        }

        // Draw the thin glowing inner line, centered inside the center line.
        // The glow is faked with a few expanding, increasingly transparent passes
        // drawn underneath a bright thin core line.
        if (CenterGlowEnabled && CenterGlowWidth > 0f && CenterLineEnabled)
        {
            // Widest, most transparent halo first, then a tighter, brighter one.
            DrawLine(lineLeft, lineRight, FadeAlpha(CenterGlowColor, 0.15f), CenterGlowWidth + CenterGlowExpand * 3f);
            DrawLine(lineLeft, lineRight, FadeAlpha(CenterGlowColor, 0.35f), CenterGlowWidth + CenterGlowExpand * 2f);
            // Thin bright core on top.
            DrawLine(lineLeft, lineRight, CenterGlowColor, CenterGlowWidth);
        }

        // === THREE-PASS RENDERING ===
        // Pass 0: Draw the outer glow (behind the main bar, slightly larger and semi-transparent).
        // Pass 1: Draw the main bar (the solid colored bar).
        // Pass 2: Draw the inner glow (a smaller white highlight near the top of the bar).
        //
        // We do all bars in each pass rather than all passes per bar to minimize draw call overhead.
        for (int pass = 0; pass < 3; pass++)
        {
            for (int i = 0; i < NumBars; i++)
            {
                // Calculate the height of this bar based on its current smoothed amplitude.
                float height = _currentAmplitudes[i] * maxHeight;

                // Calculate the X position for the bar on the RIGHT side of center.
                float rightX = center.X + i * (barWidth + BarSpacing);

                // Calculate the X position for the bar on the LEFT side of center (mirrored).
                float leftX = center.X - (i + 1) * (barWidth + BarSpacing);

                // Draw 4 bars per index: right-up, right-down, left-up, left-down.
                // direction=1 means the bar points UP from its base (above the gap).
                // direction=-1 means the bar points DOWN from its base (below the gap).
                DrawBarPass(rightX, upBase, barWidth, height, 1f, pass);
                DrawBarPass(rightX, downBase, barWidth, height, -1f, pass);
                DrawBarPass(leftX, upBase, barWidth, height, 1f, pass);
                DrawBarPass(leftX, downBase, barWidth, height, -1f, pass);
            }
        }
    }

    // Handles drawing one pass (glow, main, or inner glow) for a single bar.
    // 'x' = left edge of the bar, 'baseY' = center Y position.
    // 'width' = bar width, 'height' = bar height.
    // 'direction' = 1 for up, -1 for down.
    // 'pass' = 0 (outer glow), 1 (main bar), 2 (inner glow).
    private void DrawBarPass(float x, float baseY, float width, float height, float direction, int pass)
    {
        // Don't draw if the bar is too small to see.
        if (height < 1f) return;

        if (pass == 0 && GlowEnabled)
        {
            // === PASS 0: OUTER GLOW ===
            // Draw a larger, semi-transparent version of the bar behind the main bar.
            DrawBarShape(
                x - GlowExpand,                              // Shift left by glow expand amount
                baseY,                                       // Center line Y position
                width + GlowExpand * 2f,                     // Make wider by glow expand on both sides
                height + GlowExpand,                         // Make taller by glow expand
                direction,                                   // Same direction (up or down)
                GlowColor                                    // Semi-transparent glow color
            );
        }
        else if (pass == 1)
        {
            // === PASS 1: MAIN BAR ===
            // Draw the solid colored bar at its normal size.
            DrawBarShape(x, baseY, width, height, direction, BarColor);
        }
        else if (pass == 2 && GlowEnabled && width > InnerGlowShrink * 2f)
        {
            // === PASS 2: INNER GLOW ===
            // Draw a smaller white highlight near the top of the bar for a shiny effect.
            float innerHeight = Mathf.Min(height * InnerGlowHeightRatio, height - InnerGlowShrink);

            // Only draw if the inner glow will be tall enough to see.
            if (innerHeight >= 4f)
                DrawBarShape(
                    x + InnerGlowShrink,                     // Inset from left edge
                    baseY,                                   // Center line Y position
                    width - InnerGlowShrink * 2f,            // Narrower than the main bar
                    innerHeight,                             // Shorter, only at the top portion
                    direction,                               // Same direction (up or down)
                    InnerGlowColor                           // White semi-transparent color
                );
        }
    }

    // Draws a single bar shape as a colored polygon.
    // Each bar has a rectangular body with a pointed triangular tip at the top.
    //
    // Shape:
    //         ▲          ← tip point (triangle)
    //        █ █         ← pointed top
    //       █   █
    //      █     █       ← rectangular body
    //     █       █
    //    █         █
    //   ─────────────    ← base (at center line)
    //
    // 'x' = left edge, 'baseY' = center line Y.
    // 'direction' = 1 points upward, -1 points downward (below center line).
    private void DrawBarShape(float x, float baseY, float width, float height, float direction, Color color)
    {
        // Calculate how much of the bar height is the pointed tip vs the rectangular body.
        float tipHeight = height * PointRatio;
        float bodyHeight = height - tipHeight;

        // The tip is centered horizontally on the bar.
        float tipX = x + width * 0.5f;

        Vector2[] points;

        if (bodyHeight < 1f)
        {
            // If the bar is too short for a visible body, just draw a triangle.
            points = new Vector2[]
            {
                new(x, baseY),                          // Bottom-left corner
                new(tipX, baseY - height * direction),  // Tip point (top)
                new(x + width, baseY),                  // Bottom-right corner
            };
        }
        else
        {
            // Draw a full bar with rectangular body + triangular tip.
            // The polygon has 5 vertices going clockwise:
            points = new Vector2[]
            {
                new(x, baseY),                                   // 1. Bottom-left corner
                new(x, baseY - bodyHeight * direction),          // 2. Body top-left (where triangle starts)
                new(tipX, baseY - height * direction),           // 3. Tip point (top center)
                new(x + width, baseY - bodyHeight * direction),  // 4. Body top-right
                new(x + width, baseY),                           // 5. Bottom-right corner
            };
        }

        // Draw the filled polygon with the specified color.
        DrawColoredPolygon(points, color);
    }

    // Converts a raw linear amplitude value to a normalized 0-to-1 range.
    // Linear amplitude values are very small (e.g., 0.001 for quiet sounds).
    // Converting to decibels gives a more perceptually-linear scale.
    private static float AmplitudeToDb(float amplitude)
    {
        // If there's no sound, return 0.
        if (amplitude <= 0f) return 0f;

        // Convert linear amplitude to decibels.
        // Decibels are negative for quiet sounds (e.g., -60dB) and approach 0 for loud sounds.
        float db = Mathf.LinearToDb(amplitude);

        // Most audio content sits between -80dB and 0dB.
        // We add 80 to shift the range: -80dB → 0, 0dB → 80.
        // Then divide by 80 to normalize to 0.0–1.0.
        // Finally clamp to ensure we never go outside that range.
        return Mathf.Clamp((db + 80f) / 80f, 0f, 1f);
    }

    // Returns a copy of 'color' with its alpha multiplied by 'multiplier'.
    // Used to build the progressively fainter glow layers for the center line.
    private static Color FadeAlpha(Color color, float multiplier)
    {
        return new Color(color.R, color.G, color.B, color.A * multiplier);
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------
}
