shader_type canvas_item;

group_uniforms layer_1;
uniform sampler2D noise_1 : repeat_enable, filter_linear;
uniform vec4 color_1 : source_color = vec4(0.6, 0.65, 0.75, 1.0);
uniform float alpha_1 : hint_range(0.0, 1.0) = 0.25;
uniform vec2 speed_1 = vec2(0.03, 0.005);
uniform float scale_1 : hint_range(0.1, 5.0) = 1.0;

group_uniforms layer_2;
uniform sampler2D noise_2 : repeat_enable, filter_linear;
uniform vec4 color_2 : source_color = vec4(0.55, 0.6, 0.7, 1.0);
uniform float alpha_2 : hint_range(0.0, 1.0) = 0.18;
uniform vec2 speed_2 = vec2(-0.02, 0.003);
uniform float scale_2 : hint_range(0.1, 5.0) = 1.5;

group_uniforms fade;
uniform float edge_fade : hint_range(0.0, 0.5) = 0.15;

group_uniforms lightning;
uniform float flash_strength : hint_range(0.0, 1.0) = 0.0;
uniform float flash_glow : hint_range(0.0, 3.0) = 1.5;

void fragment() {
    vec2 uv = UV;

    float fade_l = smoothstep(0.0, edge_fade, uv.x);
    float fade_r = smoothstep(1.0, 1.0 - edge_fade, uv.x);
    float fade_t = smoothstep(0.0, edge_fade, uv.y);
    float fade_b = smoothstep(1.0, 1.0 - edge_fade, uv.y);
    float fade = fade_l * fade_r * fade_t * fade_b;

    vec2 uv1 = uv * scale_1 + speed_1 * TIME;
    float n1 = texture(noise_1, uv1).r;
    n1 = smoothstep(0.3, 0.7, n1);

    vec2 uv2 = uv * scale_2 + speed_2 * TIME;
    float n2 = texture(noise_2, uv2).r;
    n2 = smoothstep(0.3, 0.7, n2);

    vec3 col = mix(color_1.rgb, color_2.rgb, n2);
    col *= (1.0 + flash_strength * flash_glow);
    float alpha = (n1 * alpha_1 + n2 * alpha_2) * fade;

    COLOR = vec4(col, alpha);
}
