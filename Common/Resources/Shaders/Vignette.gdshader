shader_type canvas_item;

group_uniforms vignette;
uniform vec4 vignette_color : source_color = vec4(0.0, 0.0, 0.0, 1.0);
uniform float vignette_intensity : hint_range(0.0, 2.0) = 1.0;
uniform float vignette_radius : hint_range(0.0, 1.0) = 0.55;
uniform float vignette_softness : hint_range(0.0, 1.0) = 0.35;
uniform float vignette_roundness : hint_range(0.0, 1.0) = 1.0;

void fragment() {
    vec2 centered = UV - 0.5;
    float euc = length(centered) / 0.70710678;
    float cheb = max(abs(centered.x), abs(centered.y)) / 0.5;
    float dist = mix(cheb, euc, vignette_roundness);

    float mask = smoothstep(vignette_radius, min(1.0, vignette_radius + vignette_softness), dist);
    float alpha = clamp(mask * vignette_intensity, 0.0, 1.0) * vignette_color.a;

    COLOR = vec4(vignette_color.rgb, alpha);
}
