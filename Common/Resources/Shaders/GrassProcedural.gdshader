shader_type canvas_item;

varying vec2 world_pos;

// All noise is computed in world-space, so tiles are seamless by construction:
// neighbouring tiles sample the same continuous field across their shared edge.
//
// All feature sizes are kept large (~10+ screen px at the game's 0.3 camera zoom)
// so nothing aliases/shimmer while the camera moves at fractional positions.

group_uniforms palette;
uniform vec4 soil_color : source_color = vec4(0.071, 0.208, 0.110, 1.0);
uniform vec4 moss_color : source_color = vec4(0.125, 0.298, 0.153, 1.0);
uniform vec4 grass_color : source_color = vec4(0.035, 0.416, 0.188, 1.0);
uniform vec4 blade_color : source_color = vec4(0.165, 0.518, 0.239, 1.0);
uniform vec4 tip_color : source_color = vec4(0.302, 0.583, 0.278, 1.0);

group_uniforms terrain;
uniform float patch_scale : hint_range(8.0, 512.0) = 40.0;
uniform float tuft_scale : hint_range(16.0, 512.0) = 72.0;
uniform float color_dither : hint_range(0.0, 0.2) = 0.02;

group_uniforms blades;
uniform float blade_density : hint_range(0.0, 1.0) = 0.5;
uniform float blade_streak_x : hint_range(0.005, 0.2) = 0.03;
uniform float blade_streak_y : hint_range(0.002, 0.1) = 0.012;
uniform float tuft_strength : hint_range(0.0, 1.0) = 0.65;

group_uniforms wind;
uniform float wind_speed : hint_range(0.0, 3.0) = 0.4;
uniform float wind_sway : hint_range(0.0, 16.0) = 4.0;
uniform float wind_brightness : hint_range(0.0, 0.35) = 0.07;

float hash21(vec2 p) {
    p = fract(p * vec2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return fract(p.x * p.y);
}

float vnoise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    float a = hash21(i);
    float b = hash21(i + vec2(1.0, 0.0));
    float c = hash21(i + vec2(0.0, 1.0));
    float d = hash21(i + vec2(1.0, 1.0));
    return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}

float fbm2(vec2 p) {
    return vnoise(p) * 0.65 + vnoise(p * 2.13 + 11.7) * 0.35;
}

void vertex() {
    world_pos = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
}

void fragment() {
    vec2 px = world_pos;

    float wind_field = vnoise(vec2(px.x * 0.02 - TIME * wind_speed, px.y * 0.02 + TIME * wind_speed * 0.35));
    float wind = wind_field - 0.5;
    float sway = wind * wind_sway;

    vec3 col = mix(soil_color.rgb, moss_color.rgb, fbm2(px / patch_scale));

    col = mix(col, grass_color.rgb, smoothstep(0.15, 0.85, vnoise(vec2(px.x * 0.013, px.y * 0.021))) * 0.55);

    col += (vnoise(px * 0.02 + 31.7) - 0.5) * color_dither;

    float shifted_x = px.x + sway * smoothstep(0.0, 1.0, fract(px.y * 0.03));
    float streak = vnoise(vec2(shifted_x * blade_streak_x, px.y * blade_streak_y));
    float blade = smoothstep(1.0 - blade_density, 1.0 - blade_density + 0.18, streak);

    float tuft = smoothstep(0.42, 0.78, fbm2(px / tuft_scale + vec2(sway * 0.02, 0.0)));
    blade *= tuft * tuft_strength + (1.0 - tuft) * 0.18;

    float height = vnoise(vec2(px.x * 0.03, px.y * 0.03) + wind_field);
    float tip = smoothstep(0.35, 0.85, height);

    col = mix(col, blade_color.rgb, blade * 0.85);
    col = mix(col, tip_color.rgb, blade * tip * 0.6);
    col *= 1.0 + wind * wind_brightness;

    COLOR = vec4(col, 1.0);
}