using Godot;
using System;
using System.Collections.Generic;


/**-----------------------------------------------------------------------------------------------------------------------
*!                                                   PLAYER HEALTHBAR UI
*-----------------------------------------------------------------------------------------------------------------------**/
/**-----------------------------------------------------------------------------------------------------------------------
	**                                                   PURPOSE
	*  
	**  1 - Displays the player's health on the catch-up "Lines" TextureProgressBar.
	**  2 - Drives the ECG-style heartbeat waveform on the Monitor_Line (Line2D).
	**  3 - The line beats faster as health drops and flatlines at zero health.
	**  4 - Recolors using the same green -> yellow -> orange -> red bands as the Lines bar.
	*
*-----------------------------------------------------------------------------------------------------------------------**/
public partial class Player_Healthbar_UI : Control
{
    //!---------------------------------------------------------------------------------------------------------
    #region Variables
    //!---------------------------------------------------------------------------------------------------------

    private TextureProgressBar _lines;
    private TextureProgressBar _lines_catchup;
    private Line2D _monitor_line;

    // ECG waveform state
    private float _scroll;
    private Vector2[] _points = Array.Empty<Vector2>();

    // Scratch list the waveform is assembled into before publishing.
    private readonly List<Vector2> _point_build = new List<Vector2>(1100);

    // ECG waveform parameters
    private int _point_count = 990;
    private float _spacing = 0.8f;
    private float _beat_width_px = 320f;
    private float _beat_interval_normal = 0.9f;
    private float _beat_interval_low = 0.35f;
    private float _glint_speed_normal = 0.5f;
    private float _glint_speed_low = 2.5f;
    private float _glint_interval_full = 3.0f;
    private float _glint_interval_high = 2.2f;
    private float _glint_interval_mid = 1.6f;
    private float _glint_interval_low = 1.0f;
    private float _glint_interval_critical = 0.6f;
    private float _amplitude = 75f;
    private float _debug_health_ratio = -1f;
    private bool _auto_center = false;
    private float _baseline_y = 0f;
    private float _erratic_intensity = 12f;
    private float _variation_amount = 2f;

    // Last-frame flatline state so Heart_Flare can reuse it.
    private bool _last_flatline;

    // Tremor on damage
    private int _last_health;
    private float _tremor_amount;
    private Control _heart_monitor;
    private Vector2 _heart_monitor_original_pos;
    private ShaderMaterial _line_mat;
    private const float Tremor_Initial = 100.0f;
    private const float Tremor_Decay = 4.0f;
    private float _flash_strength;
    private const float Flash_Initial = 1.5f;
    private const float Flash_Decay = 6.0f;

    // Health lines border rects
    private ReferenceRect[] _line_rects;

    // Catch-up bar state
    private float _catchup_delay = 3.0f;
    private float _catchup_duration = 0.8f;
    private bool _catchup_active = false;
    private double _catchup_timer = 0.0;
    private double _catchup_start_value;
    private double _catchup_target;
    private bool _catchup_draining = false;

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Properties
    //!---------------------------------------------------------------------------------------------------------

    public TextureProgressBar Lines
    {
        get
        {
            return _lines;
        }

        set
        {
            _lines = value;
        }
    }

    public TextureProgressBar Lines_CatchUp
    {
        get
        {
            return _lines_catchup;
        }

        set
        {
            _lines_catchup = value;
        }
    }

    //! The Line2D node the ECG waveform is drawn onto.
    [Export]
    public Line2D Monitor_Line
    {
        get
        {
            return _monitor_line;
        }

        set
        {
            _monitor_line = value;
        }
    }

    //! How many points make up the line (more = smoother, costs more per frame).
    [Export]
    public int PointCount
    {
        get
        {
            return _point_count;
        }

        set
        {
            _point_count = value;
        }
    }

    //! Horizontal distance (pixels) between consecutive points.
    [Export]
    public float Spacing
    {
        get
        {
            return _spacing;
        }

        set
        {
            _spacing = value;
        }
    }

    //! Horizontal width (pixels) that a single heartbeat occupies on screen.
    [Export]
    public float Beat_Width_Px
    {
        get
        {
            return _beat_width_px;
        }

        set
        {
            _beat_width_px = value;
        }
    }

    //! Seconds per heartbeat at full health (calm pace).
    [Export]
    public float Beat_Interval_Normal
    {
        get
        {
            return _beat_interval_normal;
        }

        set
        {
            _beat_interval_normal = value;
        }
    }

    //! Seconds per heartbeat at zero health (frantic pace).
    [Export]
    public float Beat_Interval_Low
    {
        get
        {
            return _beat_interval_low;
        }

        set
        {
            _beat_interval_low = value;
        }
    }

    //! Shader glint sweep speed (line crossings per second) at full health.
    [Export]
    public float Glint_Speed_Normal
    {
        get
        {
            return _glint_speed_normal;
        }

        set
        {
            _glint_speed_normal = value;
        }
    }

    //! Shader glint sweep speed at zero health (frantic pace).
    [Export]
    public float Glint_Speed_Low
    {
        get
        {
            return _glint_speed_low;
        }

        set
        {
            _glint_speed_low = value;
        }
    }

    //! Seconds between glints at full health (green band, ratio >= 1.0).
    [Export]
    public float Glint_Interval_Full
    {
        get
        {
            return _glint_interval_full;
        }

        set
        {
            _glint_interval_full = value;
        }
    }

    //! Seconds between glints in the yellow band (ratio >= 0.75).
    [Export]
    public float Glint_Interval_High
    {
        get
        {
            return _glint_interval_high;
        }

        set
        {
            _glint_interval_high = value;
        }
    }

    //! Seconds between glints in the orange band (ratio >= 0.50).
    [Export]
    public float Glint_Interval_Mid
    {
        get
        {
            return _glint_interval_mid;
        }

        set
        {
            _glint_interval_mid = value;
        }
    }

    //! Seconds between glints in the deep-orange band (ratio >= 0.25).
    [Export]
    public float Glint_Interval_Low
    {
        get
        {
            return _glint_interval_low;
        }

        set
        {
            _glint_interval_low = value;
        }
    }

    //! Seconds between glints in the red band (ratio < 0.25) - most frantic.
    [Export]
    public float Glint_Interval_Critical
    {
        get
        {
            return _glint_interval_critical;
        }

        set
        {
            _glint_interval_critical = value;
        }
    }

    //! Vertical height of the ECG spikes, in pixels.
    [Export]
    public float Amplitude
    {
        get
        {
            return _amplitude;
        }

        set
        {
            _amplitude = value;
        }
    }

    //! Manual health override for testing. Set to 0.0 - 1.0 to preview;
    //! leave at -1 to use the real player health.
    [Export]
    public float Debug_Health_Ratio
    {
        get
        {
            return _debug_health_ratio;
        }

        set
        {
            _debug_health_ratio = value;
        }
    }

    //! If true, the script re-centers the line vertically inside the parent Control
    //! (overwriting the node's transform). If false, you position the line freely in the editor.
    [Export]
    public bool Auto_Center
    {
        get
        {
            return _auto_center;
        }

        set
        {
            _auto_center = value;
        }
    }

    //! Extra vertical offset applied when Auto_Center is enabled.
    [Export]
    public float Baseline_Y
    {
        get
        {
            return _baseline_y;
        }

        set
        {
            _baseline_y = value;
        }
    }

    //! How strong the baseline wobble is at low health (0 = disabled).
    [Export]
    public float Erratic_Intensity
    {
        get
        {
            return _erratic_intensity;
        }

        set
        {
            _erratic_intensity = value;
        }
    }

    //! How far each heartbeat's values stray from the base design (0 = identical, 1 = full).
    //! Amplitudes are randomized inside safe ranges so beats never get too tall or too short.
    [Export]
    public float Variation_Amount
    {
        get
        {
            return _variation_amount;
        }

        set
        {
            _variation_amount = value;
        }
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Initialization and Processes
    //!---------------------------------------------------------------------------------------------------------

    public override void _Ready()
    {
        Lines = GetNode<TextureProgressBar>("%TextureProgressBar - Health_Lines");
        Lines_CatchUp = GetNodeOrNull<TextureProgressBar>("%TextureProgressBar - Health_Lines_CatchUp");

        if (Monitor_Line == null)
        {
            Monitor_Line = GetNodeOrNull<Line2D>("%Control - Heart_Monitor/%Line2D - Monitor_Line");
        }

        if (Monitor_Line == null)
        {
            GD.PushWarning("Player_Healthbar_UI: no Line2D assigned. Assign Monitor_Line in the Inspector.");
        }
        else if (Auto_Center && GetParent() is Control parent)
        {
            Monitor_Line.Position = new Vector2(Monitor_Line.Position.X, parent.Size.Y * 0.5f + Baseline_Y);
        }

        Lines.Value = Get_Player_Health();
        if (Lines_CatchUp != null) Lines_CatchUp.Value = Lines.Value;
        _last_health = Player_Data_Autoload.Data.CURRENT_Health;

        // Cache Heart_Monitor for position tremor.
        _heart_monitor = GetNodeOrNull<Control>("%Control - Heart_Monitor");
        if (_heart_monitor != null)
        {
            _heart_monitor_original_pos = _heart_monitor.Position;
        }

        // Cache Monitor_Line material for shader effects (tremor + flash).
        if (Monitor_Line != null && Monitor_Line.Material is ShaderMaterial mat)
        {
            _line_mat = mat;
        }

        // Cache ReferenceRect border nodes under Lines.
        _line_rects = new ReferenceRect[]
        {
            GetNodeOrNull<ReferenceRect>("%ReferenceRect - Health_LinesUI_1"),
            GetNodeOrNull<ReferenceRect>("%ReferenceRect - Health_LinesUI_2"),
            GetNodeOrNull<ReferenceRect>("%ReferenceRect - Health_LinesUI_3"),
            GetNodeOrNull<ReferenceRect>("%ReferenceRect - Health_LinesUI_4"),
            GetNodeOrNull<ReferenceRect>("%ReferenceRect - Health_LinesUI_5"),
        };
    }

    public override void _Process(double delta)
    {
        Update_CatchUp(delta);
        Change_Color();
        Draw_ECG_Line(delta);
        Update_Tremor(delta);
    }

    private void Update_CatchUp(double delta)
    {
        if (!_catchup_active || Lines_CatchUp == null) return;

        _catchup_timer += delta;

        if (!_catchup_draining)
        {
            if (_catchup_timer >= _catchup_delay)
            {
                _catchup_draining = true;
                _catchup_timer = 0.0;
                _catchup_start_value = Lines_CatchUp.Value;
            }
        }
        else
        {
            float t = Mathf.Clamp((float)(_catchup_timer / _catchup_duration), 0f, 1f);
            // Cubic ease-out
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            Lines_CatchUp.Value = Mathf.Lerp((float)_catchup_start_value, (float)_catchup_target, eased);

            if (t >= 1f)
            {
                Lines_CatchUp.Value = (float)_catchup_target;
                _catchup_active = false;
            }
        }
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    //!---------------------------------------------------------------------------------------------------------
    #region Methods
    //!---------------------------------------------------------------------------------------------------------

    public int Get_Player_Health()
    {
        return Player_Data_Autoload.Data.CURRENT_Health;
    }

    public void Change_Health(double _damage)
    {
        Catch_Up_Change(Player_Data_Autoload.Data.CURRENT_Health, _damage);
    }

    public void Catch_Up_Change(double value_to_change, double change_value)
    {
        double new_value = Player_Data_Autoload.Data.CURRENT_Health;

        // Main bar tweens to current health.
        var main_tween = CreateTween();
        main_tween.TweenProperty(Lines, "value", new_value, 0.5);

        if (Lines_CatchUp == null) return;

        // If a catch-up cycle is already running, reset the delay but update the target.
        if (_catchup_active)
        {
            _catchup_target = new_value;
            _catchup_timer = 0.0;
            _catchup_draining = false;
            return;
        }

        // Start a new catch-up cycle.
        _catchup_start_value = Lines_CatchUp.Value;
        _catchup_target = new_value;
        _catchup_timer = 0.0;
        _catchup_draining = false;
        _catchup_active = true;
    }

    public bool Check_Full_Health()
    {
        if (Lines.Value == Lines.MaxValue)
        {
            return true;
        }
        else

        return false;
    }

    public void Change_Color()
    {
        float ratio = (float)Lines.Value / (float)Lines.MaxValue;
        Color line_color = Get_Health_Bar_Color(ratio);

        if (Lines.Value < 0)
        {
            line_color = Lines.TintProgress;
        }

        // The Under texture is the "empty" part of the bar, so it must tint with
        // the same damage color as the fill or it stays stale green while empty.
        Lines.TintProgress = line_color;
        Lines.TintUnder = line_color;

        // Update ReferenceRect border colors to match.
        if (_line_rects != null)
        {
            foreach (var rect in _line_rects)
            {
                if (rect != null)
                {
                    rect.BorderColor = line_color;
                }
            }
        }
    }

    /// <summary>
    /// Detects health drops and triggers a tremor effect on the Heart_Monitor shader.
    /// The tremor decays exponentially over time.
    /// </summary>
    private void Update_Tremor(double delta)
    {
        int current_health = Player_Data_Autoload.Data.CURRENT_Health;

        if (current_health < _last_health)
        {
            int damage = _last_health - current_health;
            _tremor_amount = Tremor_Initial * (float)damage / (float)Player_Data_Autoload.Data.MAX_Health;
            _flash_strength = Flash_Initial;
        }

        _last_health = current_health;

        if (_tremor_amount > 0.01f)
        {
            _tremor_amount = MathUtils.Damp(_tremor_amount, 0f, Tremor_Decay, (float)delta);
        }
        else
        {
            _tremor_amount = 0f;
        }

        // Shader tremor + flash on Monitor_Line material
        if (_line_mat != null)
        {
            _line_mat.SetShaderParameter("Tremor_Amount", _tremor_amount);
        }

        // White flash decay
        if (_flash_strength > 0.01f)
        {
            _flash_strength = MathUtils.Damp(_flash_strength, 0f, Flash_Decay, (float)delta);
        }
        else
        {
            _flash_strength = 0f;
        }

        if (_line_mat != null)
        {
            _line_mat.SetShaderParameter("Flash_Strength", _flash_strength);
        }

        // Position tremor (whole monitor shakes)
        if (_heart_monitor != null)
        {
            if (_tremor_amount > 0.01f)
            {
                float shake_x = RandomSignedFloat() * _tremor_amount;
                float shake_y = RandomSignedFloat() * _tremor_amount;
                _heart_monitor.Position = _heart_monitor_original_pos + new Vector2(shake_x, shake_y);
            }
            else
            {
                _heart_monitor.Position = _heart_monitor_original_pos;
            }
        }
    }

    /**------------------------------------------------------------------------------------------------
     *!                                        ECG WAVEFORM
    *------------------------------------------------------------------------------------------------**/

    /// <summary>
    /// Draws the scrolling ECG-style waveform onto the Monitor_Line. The trace follows the
    /// "Lines" bar so it compresses and recolors with it, beats faster as health drops,
    /// and flatlines when the player is dead.
    /// </summary>
    private void Draw_ECG_Line(double delta)
    {
        if (Monitor_Line == null)
        {
            return;
        }

        // 0.0 = dead, 1.0 = full health
        float health_ratio = Get_Health_Ratio();

        // When the "Lines" bar is available, follow its (tweened) fill so spacing and
        // color move exactly with it. Otherwise fall back to the raw health ratio.
        float bar_ratio = health_ratio;
        if (Lines != null && Lines.MaxValue > 0f)
        {
            bar_ratio = Mathf.Clamp((float)(Lines.Value / Lines.MaxValue), 0f, 1f);
        }

        // Flatline when dead: the trace becomes a straight line.
        bool flatline = health_ratio <= 0f;

        // Scale the whole trace proportionally to the bar so it tracks the bar's fill edge
        // exactly: both the point spacing and the beat widths shrink by bar_ratio. The tiny
        // floor only keeps the beat-mapping division safe near empty. On flatline it stays
        // full so the straight line stretches across the screen instead of stacking into a dot.
        float bar_scale;
        if (flatline || Spacing <= 0f)
        {
            bar_scale = 1f;
        }
        else
        {
            bar_scale = Mathf.Max(bar_ratio, 0.001f);
        }
        float spacing = Spacing * bar_scale;

        // Cache for Get_Beat_Flash_Point() (Heart_Flare).
        _last_flatline = flatline;

        // Lower health -> shorter beat interval -> faster heart.
        float beat_interval = Mathf.Lerp(Beat_Interval_Low, Beat_Interval_Normal, health_ratio);

        // Lower health -> faster glint sweep.
        float glint_speed = Mathf.Lerp(Glint_Speed_Low, Glint_Speed_Normal, health_ratio);
        if (_line_mat != null)
        {
            _line_mat.SetShaderParameter("Glint_Speed", glint_speed);

            // Glint cadence steps down with the same health bands as the
            // trace color (Get_Health_Bar_Color), so each hue shift also makes
            // the glint pass more often. The shader clamps the interval to the
            // crossing time, so short intervals become back-to-back passes.
            float glint_interval = health_ratio switch
            {
                >= 1.0f => Glint_Interval_Full,
                >= 0.75f => Glint_Interval_High,
                >= 0.5f => Glint_Interval_Mid,
                >= 0.25f => Glint_Interval_Low,
                _ => Glint_Interval_Critical,
            };
            _line_mat.SetShaderParameter("Glint_Interval", glint_interval);

            // Keep the shader's normalized-X mapping exact as the trace
            // shortens with the bar (spacing already includes bar_scale).
            _line_mat.SetShaderParameter("Line_Length", spacing * (PointCount - 1));
        }

        // Scroll speed is tied to beat rate so the trace never stretches or bunches up.
        _scroll += (float)(delta * Beat_Width_Px / beat_interval);

        // Local-pixel length of the uniform sampling grid.
        float length_px = spacing * (PointCount - 1);

        _point_build.Clear();

        if (flatline)
        {
            // Dead: a straight line across the full trace length.
            for (int i = 0; i < PointCount; i++)
            {
                _point_build.Add(new Vector2(i * spacing, 0f));
            }
        }
        else
        {
            // ---- collect pinned feature peaks ---------------------------------
            // The QRS tents are only a few pixels wide, so pure grid sampling
            // lets their apexes slip between points and pop as the trace
            // scrolls. Every P/Q/R/S/T center gets its own vertex, evaluated
            // exactly at its apex, so spike heights can never be missed.
            //
            // Visible wave-space range:
            float w_start = _scroll;
            float w_end = _scroll + length_px / bar_scale;
            int beat_first = (int)MathF.Floor(w_start / Beat_Width_Px) - 1;
            int beat_last = (int)MathF.Floor(w_end / Beat_Width_Px) + 1;

            // (screen x, y) pairs, appended in ascending x order: beats ascend
            // and the five centers inside one shape are ascending too.
            List<Vector2> pins = new List<Vector2>(16);
            for (int b = beat_first; b <= beat_last; b++)
            {
                Beat_Shape shape = Random_Beat_Shape(b);

                Span<float> centers = stackalloc float[]
                {
                    0.08f + shape.P_Shift,
                    0.27f + shape.QRS_Shift,
                    0.30f + shape.QRS_Shift,
                    0.33f + shape.QRS_Shift,
                    0.52f + shape.T_Shift,
                };

                foreach (float c in centers)
                {
                    if (c <= 0f || c >= 1f)
                    {
                        continue;
                    }

                    float u_c = (b + c) * Beat_Width_Px;
                    float x_c = (u_c - _scroll) * bar_scale;

                    // Keep pins strictly inside the trace so the tip stays the
                    // final uniform sample (Heart_Flare depends on that).
                    if (x_c < 0f || x_c > length_px - spacing)
                    {
                        continue;
                    }

                    // Negated because +Y points down in Godot's 2D coords.
                    float y_c = -Get_ECG(c, shape) * Amplitude;
                    pins.Add(new Vector2(x_c, y_c));
                }
            }

            // ---- merge uniform grid + pinned peaks in ascending x -------------
            bool erratic = Erratic_Intensity > 0f && health_ratio > 0f && health_ratio < 0.3f;
            float wobble_scale = erratic ? Erratic_Intensity * (1f - health_ratio / 0.3f) : 0f;
            int pin_i = 0;

            for (int i = 0; i < PointCount; i++)
            {
                float x = i * spacing;

                // Emit any peaks left of this grid point first.
                while (pin_i < pins.Count && pins[pin_i].X <= x)
                {
                    Emit_Point(_point_build, pins[pin_i], wobble_scale);
                    pin_i++;
                }

                // Map this pixel to an unscaled beat coordinate (bar scale removed)
                // so the on-screen beat width follows the bar while phases and beat
                // boundaries stay consistent.
                float w = x / bar_scale + _scroll;

                // Normalize the current pixel into a 0..1 beat phase.
                float phase = (w % Beat_Width_Px) / Beat_Width_Px;

                // The beat this pixel belongs to decides that beat's randomized shape.
                int beat = (int)(w / Beat_Width_Px);

                // Negated because +Y points down in Godot's 2D coords.
                float y = -Get_ECG(phase, Random_Beat_Shape(beat)) * Amplitude;

                Emit_Point(_point_build, new Vector2(x, y), wobble_scale);
            }

            // Trailing peaks beyond the last grid point.
            while (pin_i < pins.Count)
            {
                Emit_Point(_point_build, pins[pin_i], wobble_scale);
                pin_i++;
            }
        }

        // Publish into an exactly-sized buffer: Line2D renders the entire
        // array, and Heart_Flare treats the last entry as the trace tip.
        int n = _point_build.Count;
        if (_points.Length != n)
        {
            _points = new Vector2[n];
        }
        _point_build.CopyTo(_points);

        Monitor_Line.Points = _points;
        Monitor_Line.DefaultColor = Get_Health_Bar_Color(bar_ratio);
    }

    /// <summary>
    /// Appends one waveform vertex, applying the low-health erratic wobble.
    /// Points closer than 0.05px to the previous one are skipped so pinned
    /// peaks and grid samples never create degenerate zero-length segments.
    /// </summary>
    private void Emit_Point(List<Vector2> list, Vector2 p, float wobble_scale)
    {
        if (wobble_scale > 0f)
        {
            p.Y += Mathf.Sin(p.X * 0.013f + _scroll * 0.7f) * wobble_scale;
        }

        if (list.Count > 0 && MathF.Abs(list[list.Count - 1].X - p.X) < 0.05f)
        {
            return;
        }

        list.Add(p);
    }

    /// <summary>
    /// Returns the global (canvas-layer) position of the trace's TIP — the last point
    /// of the Line2D (right edge) — with constant full strength (no fade), plus the
    /// health-band color so the flare matches the Lines bar / monitor. The flare
    /// follows the tip continuously, so it never teleports. Returns false when the
    /// line is missing or flatlined.
    /// </summary>
    public bool Get_Beat_Flash_Point(out Vector2 global_pos, out float strength, out Color color)
    {
        global_pos = Vector2.Zero;
        strength = 0f;
        color = Colors.White;

        if (Monitor_Line == null || _last_flatline || _points.Length == 0)
        {
            return false;
        }

        // Same health-band color the Lines bar / ECG trace use.
        float ratio;
        if (Lines != null && Lines.MaxValue > 0f)
        {
            ratio = Mathf.Clamp((float)(Lines.Value / Lines.MaxValue), 0f, 1f);
        }
        else
        {
            ratio = Get_Health_Ratio();
        }

        color = Get_Health_Bar_Color(ratio);

        // The tip is the newest point, at the right edge of the trace.
        Vector2 tip = _points[_points.Length - 1];

        Vector2 local = new Vector2(tip.X, tip.Y);
        global_pos = Monitor_Line.GlobalPosition + local * Monitor_Line.Scale;

        strength = 1f;

        return true;
    }

    /// <summary>
    /// Returns the player's current health as a 0.0 (dead) to 1.0 (full) ratio.
    /// Uses the Debug_Health_Ratio export instead when it is set to 0.0 or above.
    /// </summary>
    private float Get_Health_Ratio()
    {
        if (Debug_Health_Ratio >= 0f)
        {
            return Mathf.Clamp(Debug_Health_Ratio, 0f, 1f);
        }

        int max_health = Player_Data_Autoload.Data.MAX_Health;
        if (max_health <= 0)
        {
            return 1f;
        }

        return Mathf.Clamp((float)Player_Data_Autoload.Data.CURRENT_Health / max_health, 0f, 1f);
    }

    /// <summary>
    /// Returns the same color band the health bar "Lines" uses for a given fill ratio:
    /// full -> green, >=75% yellow, >=50% orange, >=25% orange-red, below -> red.
    /// </summary>
    private Color Get_Health_Bar_Color(float ratio)
    {
        if (ratio >= 1.0f) return new Color("00ff80");
        if (ratio >= 0.75f) return new Color("ffff00");
        if (ratio >= 0.50f) return new Color("ffaf3e");
        if (ratio >= 0.25f) return new Color("ff6400");
        return new Color("ff0000");
    }

    /// <summary>
    /// Returns a random float between -1.0 and 1.0.
    /// </summary>
    private static float RandomSignedFloat()
    {
        return (float)(GD.Randf() * 2.0 - 1.0);
    }

    /// <summary>
    /// Builds one heartbeat shape from the beat's randomized values. The QRS complex is
    /// made of sharp pointy tents for an aggressive look; P and T are softer bumps.
    /// phase must be in the 0..1 range.
    /// </summary>
    private float Get_ECG(float phase, Beat_Shape shape)
    {
        float p = phase;

        // Small rounded P wave.
        float p_wave = shape.P_Amp * Mathf.Exp(-Mathf.Pow((p - (0.08f + shape.P_Shift)) / 0.030f, 2f));

        // Sharp, deep QRS complex - tight cluster of pointy tents.
        float q_dip = shape.Q_Amp * Tent(p, 0.27f + shape.QRS_Shift, shape.Q_Width);
        float r_spike = shape.R_Amp * Tent(p, 0.30f + shape.QRS_Shift, shape.R_Width);
        float s_dip = shape.S_Amp * Tent(p, 0.33f + shape.QRS_Shift, shape.S_Width);

        // Pointy T wave, softer than the QRS but still sharp.
        float t_wave = shape.T_Amp * Tent(p, 0.52f + shape.T_Shift, shape.T_Width);

        return p_wave + q_dip + r_spike + s_dip + t_wave;
    }

    /// <summary>
    /// All the per-beat values that make up one heartbeat's shape.
    /// </summary>
    private struct Beat_Shape
    {
        public float P_Amp;
        public float Q_Amp;
        public float R_Amp;
        public float S_Amp;
        public float T_Amp;
        public float QRS_Shift;
        public float P_Shift;
        public float T_Shift;
        public float Q_Width;
        public float R_Width;
        public float S_Width;
        public float T_Width;
    }

    /// <summary>
    /// Randomizes the beat's values around the base design, scaled by Variation_Amount.
    /// Deterministic per beat (seeded by the beat index) so the trace never flickers.
    /// Every amplitude stays inside a safe range, so beats can't get too tall or too short.
    /// </summary>
    private Beat_Shape Random_Beat_Shape(int beat)
    {
        ulong s = (ulong)beat * 0x9E3779B97F4A7C15ul;

        Beat_Shape shape;
        shape.P_Amp = Mathf.Lerp(0.18f - 0.06f * Variation_Amount, 0.18f + 0.06f * Variation_Amount, Rand01(ref s));
        shape.Q_Amp = Mathf.Lerp(-0.60f - 0.15f * Variation_Amount, -0.60f + 0.15f * Variation_Amount, Rand01(ref s));
        shape.R_Amp = Mathf.Lerp(1.30f - 0.10f * Variation_Amount, 1.30f + 0.10f * Variation_Amount, Rand01(ref s));
        shape.S_Amp = Mathf.Lerp(-0.50f - 0.15f * Variation_Amount, -0.50f + 0.15f * Variation_Amount, Rand01(ref s));
        shape.T_Amp = Mathf.Lerp(0.60f - 0.20f * Variation_Amount, 0.60f + 0.20f * Variation_Amount, Rand01(ref s));
        shape.QRS_Shift = Mathf.Lerp(-0.008f * Variation_Amount, 0.008f * Variation_Amount, Rand01(ref s));
        shape.P_Shift = Mathf.Lerp(-0.010f * Variation_Amount, 0.010f * Variation_Amount, Rand01(ref s));
        shape.T_Shift = Mathf.Lerp(-0.014f * Variation_Amount, 0.014f * Variation_Amount, Rand01(ref s));
        // Width floors are kept high enough that every tent spans several
        // sample steps (Spacing), otherwise spikes get under-sampled and their
        // rendered height pops frame-to-frame as the trace scrolls.
        shape.Q_Width = Mathf.Lerp(0.018f - 0.004f * Variation_Amount, 0.018f + 0.004f * Variation_Amount, Rand01(ref s));
        shape.R_Width = Mathf.Lerp(0.020f - 0.005f * Variation_Amount, 0.020f + 0.005f * Variation_Amount, Rand01(ref s));
        shape.S_Width = Mathf.Lerp(0.018f - 0.004f * Variation_Amount, 0.018f + 0.004f * Variation_Amount, Rand01(ref s));
        shape.T_Width = Mathf.Lerp(0.040f - 0.012f * Variation_Amount, 0.040f + 0.012f * Variation_Amount, Rand01(ref s));
        return shape;
    }

    /// <summary>
    /// Deterministic 0..1 random value from a seed. GD.RandFromSeed returns a raw integer,
    /// so it's normalized here before being used as an interpolation factor.
    /// </summary>
    private float Rand01(ref ulong seed)
    {
        uint raw = (uint)GD.RandFromSeed(ref seed);
        return raw / (float)uint.MaxValue;
    }

    /// <summary>
    /// A sharp triangular "tent" pulse: 1 at the center, falling linearly to 0 at the width.
    /// </summary>
    private float Tent(float phase, float center, float width)
    {
        return Mathf.Max(0f, 1f - Mathf.Abs(phase - center) / width);
    }

    /**------------------------------------------------------------------------------------------------
     *!                                        TEMPORARY
    *------------------------------------------------------------------------------------------------**/

    public override void _PhysicsProcess(double delta)
    {
        if (Input.IsActionJustReleased("Damage"))
        {
            Change_Health(30);
        }

        if (Input.IsActionJustReleased("Heal"))
        {
            Change_Health(-50);
        }
    }

    #endregion
    //!---------------------------------------------------------------------------------------------------------

    

}
