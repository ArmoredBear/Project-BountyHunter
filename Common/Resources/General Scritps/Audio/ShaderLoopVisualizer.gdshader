// FULL-SHADER VISUALIZER
// The whole visualizer (bars, pointed tips, mirrored sides, glow, center line,
// rise/fall activity and the seamless looping pattern) is computed per-pixel on
// a single quad. There is no per-bar CPU work and no _Draw() polygon rendering.
//
// The quad is a Sprite2D sized to (u_visualizer_width x viewport_height*ratio).
// The node origin is the visualizer center; bars mirror around it. Coordinates
// are derived from UV: px_x in [-W/2, W/2], px_y in [-H/2, H/2] (y up).
//
// Time input comes from C# as u_now (wall-clock seconds) on every frame, so the
// exponential rise/fall and the loop scroll animate in the shader.

shader_type canvas_item;
render_mode unshaded;

uniform int u_num_bars = 64;
uniform float u_visualizer_width = 328.0;
uniform float u_bar_width = 4.0;
uniform float u_point_ratio = 1.0;
uniform float u_center_gap = 6.0;
uniform float u_loop_speed = 1.0;
uniform float u_amplitude = 1.0;
uniform float u_max_height_ratio = 0.22;
uniform float u_seed = 7.0;
uniform vec4 u_bar_color = vec4(0.898, 0.839, 0.612, 0.588);

uniform bool u_glow_enabled = false;
uniform float u_glow_expand = 2.0;
uniform vec4 u_glow_color = vec4(0.09, 0.086, 0.061, 1.0);

uniform float u_inner_shrink = 1.0;
uniform float u_inner_ratio = 0.3;
uniform vec4 u_inner_color = vec4(1.0, 0.976, 0.878, 0.51);

uniform bool u_center_line_enabled = false;
uniform float u_center_line_width = 1.5;
uniform vec4 u_center_line_color = vec4(0.91, 0.874, 0.732, 1.0);

uniform float u_viewport_height = 1080.0;

uniform float u_hovered = 0.0;
uniform float u_now = 0.0;
uniform float u_change_time = 0.0;
uniform float u_rise_speed = 10.0;
uniform float u_fall_speed = 10.0;

// Deterministic hash in [0,1) for the loop pattern.
float hash(float n)
{
    return fract(sin(n * 127.1 + u_seed * 311.7) * 43758.5453);
}

// Raw amplitude for a loop index, wrapped mod count and kept off the floor.
// Mirrors the C# version: values land in [0.15, 1.0].
float rawVal(float idx)
{
    float n = max(float(u_num_bars), 1.0);
    float k = mod(idx, n);
    return 0.15 + 0.85 * hash(k + 1.0);
}

// Circular 3-point low-pass (0.25/0.5/0.25) with wrapped neighbours, matching
// the C# random-loop smoothing so the scroll has no dip when it wraps.
float smoothVal(float idx)
{
    return rawVal(idx - 1.0) * 0.25 + rawVal(idx) * 0.5 + rawVal(idx + 1.0) * 0.25;
}

// Sample the seamless loop at continuous index t, linear-interpolating between
// wrapped entries (equivalent to C# Mathf.Lerp(_pattern[idx], _pattern[next], f)).
float loopSample(float t)
{
    float ti = floor(t);
    return mix(smoothVal(ti), smoothVal(ti + 1.0), t - ti);
}

// Solid bar coverage in bar-local space:
// b.x horizontal position within the cell (0 = left edge, bw = right edge),
// b.y vertical distance from the bar base toward its tip (>= 0, up to height).
vec4 barSolid(vec2 b, float bw, float height, vec4 colr)
{
    if (b.x < 0.0 || b.x > bw) return vec4(0.0);
    if (b.y < 0.0 || b.y > height) return vec4(0.0);

    float bodyH = height * (1.0 - u_point_ratio);
    float tipH = height * u_point_ratio;

    if (b.y <= bodyH) return vec4(colr.rgb, colr.a);

    float tf = (b.y - bodyH) / max(tipH, 0.0001);
    float halfW = (bw * 0.5) * (1.0 - tf);
    if (abs(b.x - bw * 0.5) <= halfW) return vec4(colr.rgb, colr.a);

    return vec4(0.0);
}

// Soft additive halo around a bar, visible only when u_glow_enabled.
vec4 barHalo(vec2 b, float bw, float height)
{
    if (!u_glow_enabled) return vec4(0.0);

    float e = max(u_glow_expand, 0.001);
    float bodyH = height * (1.0 - u_point_ratio);
    float tipH = height * u_point_ratio;

    float bcx = b.x - bw * 0.5;
    float hwAt = bw * 0.5;
    if (b.y > bodyH)
        hwAt = (bw * 0.5) * (1.0 - (b.y - bodyH) / max(tipH, 0.0001));
    hwAt = max(hwAt, 0.0);

    float dx = max(abs(bcx) - hwAt, 0.0);
    float dy = max(b.y - height, 0.0);
    float d = dx + dy;

    if (d > e) return vec4(0.0);

    float s = 1.0 - d / e;
    return vec4(u_glow_color.rgb, u_glow_color.a * s * s);
}

void fragment()
{
    float W = max(u_visualizer_width, 4.0);
    float H = max(u_viewport_height * u_max_height_ratio, 4.0);

    // Local pixel coordinates, origin = visualizer center, y up.
    float px_x = UV.x * W - W * 0.5;
    float px_y = (0.5 - UV.y) * H;

    // Rise/fall activity, analytic exponential between hover states.
    float dt = max(u_now - u_change_time, 0.0);
    float act;
    if (u_hovered > 0.5)
        act = 1.0 - exp(-u_rise_speed * dt);
    else
        act = exp(-u_fall_speed * dt);
    act = clamp(act, 0.0, 1.0);

    // Per-side geometry with an EXACT visualizer width: if the requested width
    // can fit the bars plus gaps, gaps grow; otherwise bars shrink to always
    // cover exactly u_visualizer_width.
    float n = max(float(u_num_bars), 1.0);
    float half = W * 0.5;
    float need = n * u_bar_width;
    float bw;
    float step;
    if (need <= half)
    {
        bw = u_bar_width;
        step = bw + (half - need) / max(n - 1.0, 1.0);
    }
    else
    {
        bw = half / n;
        step = bw;
    }
    step = max(step, 0.001);

    float dist = abs(px_x);
    float idxf = dist / step;

    if (idxf > n - 0.5)
    {
        discard;
    }

    int i = int(floor(idxf));
    float cellX = dist - float(i) * step;

    // Scrolling seamless loop, sampled at this bar's index + time offset.
    float value = loopSample(float(i) + u_now * u_loop_speed) * u_amplitude * act;
    float height = min(value * H, H * 0.5);

    if (height < 1.0)
    {
        discard;
    }

    // Bars exist on both sides (mirrored) and both above and below the center.
    vec2 bUp = vec2(cellX, -u_center_gap - px_y);
    vec2 bDn = vec2(cellX, px_y - u_center_gap);

    vec4 coreUp = barSolid(bUp, bw, height, u_bar_color);
    vec4 coreDn = barSolid(bDn, bw, height, u_bar_color);

    vec3 col = coreUp.rgb + coreDn.rgb;
    float alpha = coreUp.a + coreDn.a;

    if (u_glow_enabled)
    {
        vec4 hUp = barHalo(bUp, bw, height);
        vec4 hDn = barHalo(bDn, bw, height);
        col += hUp.rgb * hUp.a + hDn.rgb * hDn.a;
        alpha += hUp.a + hDn.a;

        // Thin bright inner overlay (mirrors the C# inner-glow pass).
        float sBW = max(bw - u_inner_shrink * 2.0, 0.2);
        float sHT = min(height * u_inner_ratio, height - u_inner_shrink);
        float off = bw * 0.5 - sBW * 0.5;
        vec4 inUp = barSolid(vec2(bUp.x - off, bUp.y), sBW, sHT, u_inner_color);
        vec4 inDn = barSolid(vec2(bDn.x - off, bDn.y), sBW, sHT, u_inner_color);
        col += inUp.rgb * inUp.a + inDn.rgb * inDn.a;
        alpha += inUp.a + inDn.a;
    }

    // Optional center line (spans the node width, not the full viewport).
    if (u_center_line_enabled && abs(px_y) <= u_center_line_width * 0.5)
    {
        col += u_center_line_color.rgb;
        alpha += u_center_line_color.a;
    }

    COLOR = vec4(col, clamp(alpha, 0.0, 1.0));
}