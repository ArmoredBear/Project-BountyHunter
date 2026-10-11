// A/B probe: texture-driven replacement for GrassProcedural_PixelCrisp.
// Same palette groups, but blades/dents come from the CanvasTexture's own
// diffuse/normal/spec maps instead of ~15 procedural vnoise passes.
shader_type canvas_item;

uniform vec4 soil_color : source_color = vec4(0.071, 0.208, 0.110, 1.0);
uniform vec4 grass_color : source_color = vec4(0.035, 0.416, 0.188, 1.0);
uniform vec4 tip_color : source_color = vec4(0.302, 0.583, 0.278, 1.0);
uniform float color_dither : hint_range(0.0, 0.2) = 0.02;
uniform float dither_scale : hint_range(8.0, 512.0) = 70.0;
uniform float normal_strength : hint_range(-4.0, 4.0) = 0.3;
uniform float specular_strength : hint_range(0.0, 2.0) = 0.15;
uniform float shininess : hint_range(1.0, 128.0) = 24.0;

uniform sampler2D nmap_tex : filter_linear;
uniform sampler2D spec_tex : filter_linear;

varying vec2 world_pos;

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

void vertex() {
    world_pos = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
}

void fragment() {
    vec4 tex = texture(TEXTURE, UV);
    vec3 col = tex.rgb;

    // One cheap two-octave field for organic patchiness instead of the old
    // 15-noise sync (this is the entire remaining procedural cost).
    float d = vnoise(world_pos / dither_scale + 29.7) * 0.65
            + vnoise(world_pos / (dither_scale * 2.13) + 11.7) * 0.35;
    col += (d - 0.5) * color_dither;
    col = mix(col, grass_color.rgb, d * 0.35);
    col = mix(col, tip_color.rgb, d * 0.12);

    // CanvasTexture normal map instead of the procedural gradient.
    vec4 nmap = texture(nmap_tex, UV);
    vec2 grad = (nmap.xy - 0.5) * 2.0 * normal_strength;
    NORMAL = normalize(vec3(grad, 1.0));

    COLOR = vec4(col, 1.0);
}

void light() {
    vec3 n = normalize(NORMAL);
    vec3 l = normalize(LIGHT_DIRECTION);
    float ndotl = max(dot(n, l), 0.0);
    vec3 diffuse = LIGHT_COLOR.rgb * COLOR.rgb * LIGHT_ENERGY * ndotl;

    vec3 half_vec = normalize(l + vec3(0.0, 0.0, 1.0));
    float ndoth = max(dot(n, half_vec), 0.0);
    float spec = pow(ndoth, shininess) * specular_strength;
    vec3 specular = LIGHT_COLOR.rgb * spec * LIGHT_ENERGY;

    LIGHT = vec4(diffuse + specular, LIGHT_COLOR.a);
}
