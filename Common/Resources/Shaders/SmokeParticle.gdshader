shader_type canvas_item;

uniform sampler2D noise_texture : source_color, repeat_enable, filter_linear;
uniform float alpha_multiplier : hint_range(0.0, 2.0) = 1.0;
uniform float alpha_cutoff : hint_range(0.0, 1.0) = 0.1;

void fragment() {
    vec4 noise = texture(noise_texture, UV);
    float alpha = smoothstep(alpha_cutoff, 1.0, noise.r) * alpha_multiplier;
    COLOR = vec4(vec3(1.0), alpha);
}
