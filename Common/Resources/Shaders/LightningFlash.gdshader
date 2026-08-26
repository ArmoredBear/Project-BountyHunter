shader_type canvas_item;

group_uniforms flash;
uniform vec4 flash_color : source_color = vec4(0.55, 0.72, 1.0, 1.0);
uniform float flash_strength : hint_range(0.0, 1.0) = 0.0;
uniform float white_amount : hint_range(0.0, 1.0) = 0.35;
uniform float flicker_amount : hint_range(0.0, 1.0) = 0.4;
uniform float flicker_speed : hint_range(1.0, 120.0) = 55.0;

uniform sampler2D screen_texture : hint_screen_texture, filter_linear_mipmap;

void fragment() {
    vec4 scene = texture(screen_texture, SCREEN_UV);

    float frame = floor(TIME * flicker_speed);
    float noise = fract(sin(frame * 12.9898) * 43758.5453);
    float flicker = mix(1.0, noise, flicker_amount);
    float str = clamp(flash_strength * flicker, 0.0, 1.0);

    vec3 flash_col = mix(vec3(1.0), flash_color.rgb, white_amount);
    vec3 col = mix(scene.rgb, flash_col, str);
    col = mix(col, vec3(1.0), str * white_amount * 0.5);

    COLOR = vec4(col, 1.0);
}
