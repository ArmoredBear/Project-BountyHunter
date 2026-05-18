shader_type canvas_item;

uniform float glitch_intensity : hint_range(0.0, 1.0) = 0.5;
uniform float speed : hint_range(0.1, 5.0) = 1.0;
uniform float time_offset : hint_range(0.0, 100.0) = 0.0;

float random(vec2 uv) {
    return fract(sin(dot(uv, vec2(12.9898, 78.233))) * 43758.5453);
}

uniform vec4 from_color : source_color = vec4(0.0, 0.0, 0.0, 1.0);
uniform vec4 to_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);

void fragment() {
    vec2 uv = UV;

    float rand_offset = random(uv);

    float glitch = random(vec2(floor(uv.y * 20.0), floor((TIME + time_offset) * speed * 10.0)));
    uv.x += (glitch - 0.5) * glitch_intensity * 0.1;

    vec4 tex_color = texture(TEXTURE, uv);

    float t = fract((TIME + time_offset + rand_offset * 5.0) * speed);
    vec3 color = mix(from_color.rgb, to_color.rgb, t);

    COLOR = tex_color * vec4(color, 1.0);
}