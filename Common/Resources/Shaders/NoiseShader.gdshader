shader_type canvas_item;

group_uniforms texture_1;
uniform sampler2D noise_texture : repeat_enable, filter_nearest;
uniform float density : hint_range(0.0, 1.0) = 0.25;
uniform vec2 speed = vec2(0.02, 0.01);
uniform vec4 fog_color : source_color = vec4(0.8, 0.8, 0.85, 1.0);
uniform float uv_scale : hint_range(0.01, 2.0) = 0.2;

group_uniforms texture_2;
uniform sampler2D noise_texture_2 : repeat_enable, filter_nearest;
uniform float density_2 : hint_range(0.0, 1.0) = 0.25;
uniform vec2 speed_2 = vec2(-0.015, 0.008);
uniform vec4 fog_color_2 : source_color = vec4(0.8, 0.8, 0.85, 1.0);
uniform float uv_scale_2 : hint_range(0.01, 2.0) = 0.2;

group_uniforms fade;
uniform float fade_speed : hint_range(0.0, 2.0) = 0.3;
uniform float fade_variation : hint_range(0.0, 10.0) = 4.0;
uniform float fade_uv_scale : hint_range(0.01, 2.0) = 0.15;
uniform vec2 fade_speed_uv = vec2(0.01, -0.007);

group_uniforms blend;
uniform float blend_strength : hint_range(0.0, 1.0) = 0.5;

void fragment() {
    vec2 fade_uv = UV * fade_uv_scale + fade_speed_uv * TIME;
    float fade_mask = texture(noise_texture, fade_uv + 100.0).r;
    float fade_mask_2 = texture(noise_texture_2, fade_uv + 200.0).r;

    vec2 uv1 = UV * uv_scale + speed * TIME;
    float fog1 = clamp(texture(noise_texture, uv1).r * 2.0 - 1.0, 0.0, 1.0);
    float fade1 = sin(TIME * fade_speed + fade_mask * fade_variation) * 0.5 + 0.5;
    float alpha1 = fog1 * density * fade1;

    vec2 uv2 = UV * uv_scale_2 + speed_2 * TIME;
    float fog2 = clamp(texture(noise_texture_2, uv2).r * 2.0 - 1.0, 0.0, 1.0);
    float fade2 = sin(TIME * fade_speed * 0.7 + fade_mask_2 * fade_variation + 2.0) * 0.5 + 0.5;
    float alpha2 = fog2 * density_2 * fade2;

    vec3 color = mix(fog_color.rgb, fog_color_2.rgb, blend_strength);
    float alpha = mix(alpha1, alpha2, blend_strength);

    COLOR = vec4(color, alpha);
}
