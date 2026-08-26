shader_type canvas_item;

group_uniforms rain;
uniform vec4 rain_color : source_color = vec4(0.72, 0.8, 0.95, 1.0);
uniform float brightness : hint_range(0.1, 10.0) = 4.0;
uniform float speed : hint_range(0.01, 10.0) = 0.8;
uniform float wind : hint_range(-0.5, 0.5) = 0.15;

group_uniforms near_layer;
uniform float near_density : hint_range(1.0, 500.0) = 120.0;
uniform float near_compression : hint_range(0.1, 1.0) = 0.2;
uniform float near_trail : hint_range(5.0, 100.0) = 50.0;
uniform float near_opacity : hint_range(0.0, 1.0) = 0.55;

group_uniforms far_layer;
uniform float far_density : hint_range(1.0, 500.0) = 200.0;
uniform float far_compression : hint_range(0.1, 1.0) = 0.25;
uniform float far_trail : hint_range(5.0, 100.0) = 30.0;
uniform float far_opacity : hint_range(0.0, 1.0) = 0.3;
uniform float far_speed_scale : hint_range(0.1, 2.0) = 0.7;

group_uniforms lightning;
uniform float flash_strength : hint_range(0.0, 1.0) = 0.0;
uniform float flash_glow : hint_range(0.0, 3.0) = 1.5;

// Renders one rain layer and returns a 0..1 brightness value.
float rain_layer(vec2 uv, float t, float density, float compression, float trail, float speed_scale, float opacity)
{
    // Columns across the width; wind shears them along the slant.
    vec2 st = vec2(uv.x * density, uv.y);
    st.x -= wind * uv.y * density;

    vec2 duv = vec2(floor(st.x), st.y) * compression;
    float offset = sin(duv.x);

    // Per-column fall speed varies so drops are never all in sync.
    float fall = cos(duv.x * 30.0) * 0.5 + 0.5;
    float trail_len = mix(10.0, trail, fall);

    // Drop head scrolls downward and the streak trails behind it.
    float drop = fract(duv.y - t * fall * speed_scale + offset) * trail_len;
    drop = 1.0 / drop;
    drop = smoothstep(0.0, 1.0, drop * drop);
    drop = sin(drop * PI) * fall * brightness;

    // Soft horizontal falloff inside each column.
    float shape = sin(fract(st.x) * PI);
    drop *= shape * shape;

    return clamp(drop * opacity, 0.0, 1.0);
}

void fragment()
{
    vec2 uv = SCREEN_UV;
    float t = TIME * speed;

    float near = rain_layer(uv, t, near_density, near_compression, near_trail, 1.0, near_opacity);
    float far = rain_layer(uv, t, far_density, far_compression, far_trail, far_speed_scale, far_opacity);

    float alpha = clamp(near + far, 0.0, 1.0);

    // Lightning brightens the rain during a strike.
    vec3 col = rain_color.rgb * (1.0 + flash_strength * flash_glow);

    COLOR = vec4(col, alpha);
}
