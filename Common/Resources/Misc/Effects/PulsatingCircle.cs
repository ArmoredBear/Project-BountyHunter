using Godot;

/**-----------------------------------------------------------------------------------------------------------------------
*!                                                   PULSATINGCIRCLE
*-----------------------------------------------------------------------------------------------------------------------**/
/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	*  
	**  1 - Draws a set of expanding, fading concentric rings from the node's origin.
	**  2 - In music-reactive mode samples the Master audio bus and drives pulse speed and ring radius from the music.
	*
*-----------------------------------------------------------------------------------------------------------------------**/
public partial class PulsatingCircle : Node2D
{
    //!---------------------------------------------------------------------------------------------------------
    #region Variables
    //!---------------------------------------------------------------------------------------------------------

    // -- Timing -----------------------------------------------------------------
    // Seconds between the start of one pulse and the next.
    [ExportCategory("Timing")]
    [Export(PropertyHint.Range, "0.1,10")] public float PulseInterval = 2.0f;

    // How long a single pulse grows for before it is skipped/faded out.
    [Export(PropertyHint.Range, "0.1,10")] public float PulseDuration = 1.0f;

    // Global speed multiplier for the pulse timer. Ignored in audio-only mode.
    [Export(PropertyHint.Range, "0.0,10.0")] public float SpeedMultiplier = 1.0f;

    // -- Circles ----------------------------------------------------------------
    // Number of rings drawn per pulse (staggered by TimeOffset).
    [ExportCategory("Circles")]
    [Export(PropertyHint.Range, "1,12")] public int CircleCount = 3;

    // Stagger between consecutive rings so the circles trail one another.
    [Export(PropertyHint.Range, "0.0,5.0")] public float TimeOffset = 0.3f;

    // -- Appearance -------------------------------------------------------------
    // Smallest ring radius, before any audio scaling is applied.
    [ExportCategory("Appearance")]
    [Export(PropertyHint.Range, "1,1024")] public float MinRadius = 10f;

    // Largest ring radius a pulse reaches at the end of its growth.
    [Export(PropertyHint.Range, "1,2048")] public float MaxRadius = 200f;

    // Hard floor for the drawn radius so rings never fully disappear.
    [Export(PropertyHint.Range, "1,100")] public float MinVisibleRadius = 3f;

    // Stroke width of the rings in pixels.
    [Export(PropertyHint.Range, "0.1,50")] public float LineWidth = 2f;

    // Base ring color; only the alpha channel changes as a pulse fades.
    [Export] public Color CircleColor = Colors.Cyan;

    // -- Behavior ---------------------------------------------------------------
    // Restart the pulse when the timer passes PulseInterval (otherwise it stops).
    [ExportCategory("Behavior")]
    [Export] public bool Loop = true;

    // -- Music Reactive ---------------------------------------------------------
    // Master switch for all audio-driven behaviour.
    [ExportCategory("Music Reactive")]
    [Export] public bool EnableMusicReactive = true;

    // In non-audio-only mode: how strongly energy multiplies the pulse speed.
    [Export(PropertyHint.Range, "0.0,10.0")] public float BassBoost = 1.5f;

    // How strongly the smoothed audio energy scales the ring radius.
    [Export(PropertyHint.Range, "0.0,5.0")] public float RadiusBoost = 0.8f;

    // Divides the raw bass signal so a given level counts as 1.0 (higher = less sensitive).
    [Export(PropertyHint.Range, "0.0,5.0")] public float Sensitivity = 0.5f;

    // Weight of the bus peak-volume fallback signal when the analyzer is unavailable.
    [Export(PropertyHint.Range, "0.0,2.0")] public float PeakBoost = 0.3f;

    // Frequency band (Hz) sampled on the Master bus for bass detection.
    [Export(PropertyHint.Range, "20,500")] public float BassFreqMin = 20f;
    [Export(PropertyHint.Range, "100,1000")] public float BassFreqMax = 200f;

    // How fast the energy level rises when the music gets louder.
    [Export(PropertyHint.Range, "0.1,10.0")] public float SmoothAttack = 3.0f;

    // How fast the energy level settles back when the music drops.
    [Export(PropertyHint.Range, "0.1,10.0")] public float SmoothRelease = 2.0f;

    // Bass level a rising edge must cross to count as a beat/kick.
    [Export(PropertyHint.Range, "0.0,1.0")] public float BeatThreshold = 0.3f;

    // Impulse value applied on each detected beat.
    [Export(PropertyHint.Range, "0.0,5.0")] public float KickStrength = 1.0f;

    // How fast the beat impulse decays back to zero.
    [Export(PropertyHint.Range, "0.0,30.0")] public float KickDecay = 4.0f;

    // Minimum seconds between kicks, preventing rapid re-triggering.
    [Export(PropertyHint.Range, "0.0,1.0")] public float KickCooldown = 0.25f;

    // Smoothing rate for the pulse speed (higher = snappier).
    [Export(PropertyHint.Range, "0.1,30.0")] public float SpeedSmoothing = 10f;

    // Smoothing rate for the radius scaling (higher = snappier).
    [Export(PropertyHint.Range, "0.1,30.0")] public float ScaleSmoothing = 6f;

    // In audio-only mode: multiplier that turns audio drive into pulse speed.
    [Export(PropertyHint.Range, "0.1,10.0")] public float AudioPulseSpeed = 1.5f;

    // If true the pulse only advances when music drives it (no constant base pulse).
    // If false there is always a base pulse whose speed/radius are boosted by music.
    [Export] public bool ExpandWithAudioOnly = true;

    // Progress through the current pulse, in seconds.
    private float _timer = 0f;

    // Cached spectrum analyzer instance for the Master bus.
    private AudioEffectSpectrumAnalyzerInstance _analyzer;

    // Static guard so the analyzer effect is only added to the bus once.
    private static bool _analyzerAdded;

    // Smoothed music energy level, normalized to 0..1.
    private float _energy = 0f;

    // Decaying impulse applied on each detected beat.
    private float _kick = 0f;

    // Previous frame's bass level, used for rising-edge detection.
    private float _lastBass = 0f;

    // Countdown that prevents the kick from re-triggering too fast.
    private float _kickCooldown = 0f;

    // Smoothed pulse advance rate used by the timer.
    private float _smoothedSpeed = 0f;

    // Smoothed radius scale driven by the audio.
    private float _smoothedScale = 0f;

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Initialization and Processes
    //!---------------------------------------------------------------------------------------------------------

    public override void _Ready()
    {
        // Locate (or create) the spectrum analyzer on the Master bus.
        TrySetupAnalyzer();
    }

    /// <summary>
    /// Finds the spectrum analyzer on the Master bus and caches its instance.
    /// If none exists yet it adds one (guarded by a static flag so multiple
    /// audio-reactive nodes do not each add a duplicate effect).
    /// </summary>
    private void TrySetupAnalyzer()
    {
        int busIdx = AudioServer.GetBusIndex("Master");
        if (busIdx == -1) return;

        // Check whether the Master bus already carries a spectrum analyzer effect.
        bool hasAnalyzer = false;
        for (int i = 0; i < AudioServer.GetBusEffectCount(busIdx); i++)
        {
            if (AudioServer.GetBusEffect(busIdx, i) is AudioEffectSpectrumAnalyzer)
            {
                hasAnalyzer = true;
                break;
            }
        }

        // Add one at the front of the bus if it is missing.
        if (!hasAnalyzer && !_analyzerAdded)
        {
            var analyzer = new AudioEffectSpectrumAnalyzer
            {
                FftSize = AudioEffectSpectrumAnalyzer.FftSizeEnum.Size2048,
                BufferLength = 2.0f
            };
            AudioServer.AddBusEffect(busIdx, analyzer, 0);
            _analyzerAdded = true;
        }

        // Cache the analyzer instance so it can be sampled every frame.
        // The instance may not be ready the very first frame; it is retried in _Process.
        for (int i = 0; i < AudioServer.GetBusEffectCount(busIdx); i++)
        {
            if (AudioServer.GetBusEffect(busIdx, i) is AudioEffectSpectrumAnalyzer)
            {
                var instance = (AudioEffectSpectrumAnalyzerInstance)
                    AudioServer.GetBusEffectInstance(busIdx, i);
                if (instance != null)
                    _analyzer = instance;
                break;
            }
        }
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;

        if (EnableMusicReactive)
        {
            // Retry analyzer acquisition in case it was not ready in _Ready.
            if (_analyzer == null)
                TrySetupAnalyzer();

            // -- Bass level from the spectrum analyzer -------------------------
            float bassDb = 0f;
            if (_analyzer != null)
            {
                Vector2 mag = _analyzer.GetMagnitudeForFrequencyRange(BassFreqMin, BassFreqMax);
                float raw = (mag.X + mag.Y) * 0.5f;
                bassDb = AmplitudeToDb(raw);
                if (Sensitivity > 0f)
                    bassDb = Mathf.Clamp(bassDb / Sensitivity, 0f, 1f);
            }
            // -- Bus peak-volume fallback --------------------------------------
            // Read every channel (the Master bus is mono, so guard against
            // reading a right channel that does not exist).
            int busIdx = AudioServer.GetBusIndex("Master");
            int channels = Mathf.Max(1, AudioServer.GetBusChannels(busIdx));
            float peakDb = 0f;
            for (int c = 0; c < channels; c++)
                peakDb += AudioServer.GetBusPeakVolumeLeftDb(busIdx, c);
            peakDb /= channels;
            float peak = Mathf.Clamp((peakDb + 60f) / 60f, 0f, 1f);

            // Combine both sources, preferring whichever is stronger.
            float target = Mathf.Max(bassDb, peak * PeakBoost);

            // Smooth the energy so the effect reacts without jitter.
            float dampSpeed;
            if (target > _energy)
            {
                dampSpeed = SmoothAttack;
            }
            else
            {
                dampSpeed = SmoothRelease;
            }
            _energy = MathUtils.Damp(_energy, target, dampSpeed, dt);

            // -- Beat / kick detection -----------------------------------------
            // A kick fires only on the rising edge of the bass level, gated by
            // a cooldown so a single beat does not retrigger several times.
            if (_analyzer != null &&
                bassDb > BeatThreshold && _lastBass <= BeatThreshold &&
                _kickCooldown <= 0f)
            {
                _kick = KickStrength;
                _kickCooldown = KickCooldown;
            }
            _lastBass = bassDb;
            _kickCooldown = Mathf.Max(0f, _kickCooldown - dt);
            _kick = MathUtils.Damp(_kick, 0f, KickDecay, dt);

            // Combine smoothed energy with the beat impulse into one drive value.
            float audioDrive = _energy * 0.5f + _kick;

            // Map the drive onto smoothed pulse speed and radius scale.
            if (ExpandWithAudioOnly)
            {
                // Audio-only: the pulse is completely still when music is silent.
                _smoothedSpeed = MathUtils.Damp(_smoothedSpeed, audioDrive * AudioPulseSpeed, SpeedSmoothing, dt);
                _smoothedScale = MathUtils.Damp(_smoothedScale, audioDrive * RadiusBoost, ScaleSmoothing, dt);
            }
            else
            {
                // Base pulse always runs; the music boosts its speed and radius.
                float targetSpeed = 1f + audioDrive * BassBoost;
                float targetScale = 1f + audioDrive * RadiusBoost;
                _smoothedSpeed = MathUtils.Damp(_smoothedSpeed, targetSpeed, SpeedSmoothing, dt);
                _smoothedScale = MathUtils.Damp(_smoothedScale, targetScale, ScaleSmoothing, dt);
            }
        }
        else
        {
            // Music reactivity disabled: drain everything back to the idle state.
            _energy = MathUtils.Damp(_energy, 0f, SmoothRelease, dt);
            _kick = MathUtils.Damp(_kick, 0f, KickDecay, dt);
            float idle;
            if (ExpandWithAudioOnly)
            {
                idle = 0f;
            }
            else
            {
                idle = 1f;
            }
            _smoothedSpeed = MathUtils.Damp(_smoothedSpeed, idle, SpeedSmoothing, dt);
            _smoothedScale = MathUtils.Damp(_smoothedScale, idle, ScaleSmoothing, dt);
        }

        // Advance the pulse timer. In audio-only mode the raw smoothed speed is
        // used directly; otherwise the base SpeedMultiplier scales it.
        float timerSpeed;
        if (ExpandWithAudioOnly)
        {
            timerSpeed = _smoothedSpeed;
        }
        else
        {
            timerSpeed = SpeedMultiplier * _smoothedSpeed;
        }
        _timer += dt * timerSpeed;
        if (Loop && _timer >= PulseInterval)
            _timer -= PulseInterval;

        QueueRedraw();
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Methods and Interfaces
    //!---------------------------------------------------------------------------------------------------------

    public override void _Draw()
    {
        if (LineWidth <= 0f) return;

        // Audio level is the smoothed scale, clamped to 0..1 so rings never grow
        // past their full size.
        float audioLevel = Mathf.Clamp(_smoothedScale, 0f, 1f);

        for (int i = 0; i < CircleCount; i++)
        {
            // Each ring runs the pulse with a small stagger so they trail.
            float circleTimer = _timer + i * TimeOffset;
            if (circleTimer >= PulseInterval)
                circleTimer -= PulseInterval;

            float radius;
            float alpha;

            if (circleTimer <= PulseDuration)
            {
                // Growing phase: ease the radius up and fade alpha down.
                float progress = circleTimer / PulseDuration;
                float eased = MathUtils.SmoothStep(progress);
                // Scale the interpolated radius by the audio level, but never
                // let it drop below MinVisibleRadius.
                radius = Mathf.Max(MinVisibleRadius,
                    Mathf.Lerp(MinRadius, MaxRadius, eased) * audioLevel);
                alpha = 1f - progress;
            }
            else
            {
                // Outside the pulse window the ring is fully faded out.
                radius = MaxRadius;
                alpha = 0f;
            }

            // Skip rings that have nothing to draw this frame.
            if (radius <= 0f || alpha <= 0f) continue;

            Color drawColor = new Color(CircleColor.R, CircleColor.G, CircleColor.B, alpha);

            DrawCircle(Vector2.Zero, radius, drawColor, false, LineWidth);
        }
    }

    /// <summary>
    /// Converts a linear amplitude (0..1) into a normalized 0..1 level using a
    /// decibel scale, so quiet sounds get less energy than they would linearly.
    /// </summary>
    private static float AmplitudeToDb(float amplitude)
    {
        if (amplitude <= 0f) return 0f;
        float db = Mathf.LinearToDb(amplitude);
        return Mathf.Clamp((db + 80f) / 80f, 0f, 1f);
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------
}
