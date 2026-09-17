shader_type canvas_item;

varying vec2 world_pos;
varying float blade_mask;

// All noise is computed in world-space, so tiles are seamless by construction:
// neighbouring tiles sample the same continuous field across their shared edge.
//
// PIXEL-CRISP VARIANT. Requires pixel-perfect rendering: camera zoom 0.25 and
// the camera snapped to the pixel grid (see CameraShake.cs). pixel_size is the
// world-space size of one screen pixel = 1.0 / zoom, and all pattern math below
// is done in whole screen pixels, so the grass stays crisp and never shimmers.
//
// LIT VARIANT. A procedural normal is derived from the same blade field and a
// custom light() adds a Blinn specular highlight on top of Lambert, so the
// grass reacts to Light2D (the engine still applies shadows around light()).

group_uniforms palette;
uniform vec4 soil_color : source_color = vec4(0.071, 0.208, 0.110, 1.0);
uniform vec4 moss_color : source_color = vec4(0.125, 0.298, 0.153, 1.0);
uniform vec4 grass_color : source_color = vec4(0.035, 0.416, 0.188, 1.0);
uniform vec4 blade_color : source_color = vec4(0.165, 0.518, 0.239, 1.0);
uniform vec4 tip_color : source_color = vec4(0.302, 0.583, 0.278, 1.0);

group_uniforms terrain;
uniform float pixel_size : hint_range(1.0, 16.0) = 4.0;
uniform float grass_scale : hint_range(0.25, 4.0) = 1.8;
uniform float patch_scale : hint_range(8.0, 512.0) = 24.0;
uniform float tuft_scale : hint_range(16.0, 512.0) = 38.0;
uniform float color_dither : hint_range(0.0, 0.2) = 0.02;

group_uniforms blades;
uniform float blade_density : hint_range(0.0, 1.0) = 0.5;
uniform float blade_streak_x : hint_range(0.02, 0.6) = 0.21;
uniform float blade_streak_y : hint_range(0.005, 0.3) = 0.06;
uniform float tuft_strength : hint_range(0.0, 1.0) = 0.65;

group_uniforms clearings;
uniform float bare_scale : hint_range(16.0, 512.0) = 110.0;
uniform float bare_amount : hint_range(0.0, 1.0) = 0.45;
uniform float bare_softness : hint_range(0.01, 0.4) = 0.12;
uniform float bare_soil_mix : hint_range(0.0, 1.0) = 0.5;

group_uniforms variety;
uniform float density_scale : hint_range(16.0, 512.0) = 70.0;
uniform float density_min : hint_range(0.0, 1.0) = 0.35;
uniform float macro_scale : hint_range(128.0, 2048.0) = 520.0;
uniform float macro_strength : hint_range(0.0, 1.0) = 0.18;

group_uniforms lighting;
uniform float normal_strength : hint_range(-4.0, 4.0) = 0.3;
uniform float specular_strength : hint_range(0.0, 2.0) = 0.15;
uniform float shininess : hint_range(1.0, 128.0) = 24.0;

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

float grass_height(vec2 p) {
    float gs = max(grass_scale, 0.001);
    return vnoise(vec2(p.x * blade_streak_x * gs, p.y * blade_streak_y * gs));
}

void vertex() {
    world_pos = (MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0)).xy;
}

void fragment() {
    vec2 px = floor(world_pos / pixel_size);
    float gs = max(grass_scale, 0.001);

    vec3 col = mix(soil_color.rgb, moss_color.rgb, fbm2(px * gs / patch_scale));

    col = mix(col, grass_color.rgb, smoothstep(0.15, 0.85, vnoise(vec2(px.x * 0.013 * gs, px.y * 0.021 * gs))) * 0.55);

    col += (vnoise(px * 0.04 * gs + 5.1) - 0.5) * color_dither;

    float streak = vnoise(vec2(px.x * blade_streak_x * gs, px.y * blade_streak_y * gs));
    float blade = smoothstep(1.0 - blade_density, 1.0 - blade_density + 0.18, streak);

    float tuft = smoothstep(0.35, 0.65, fbm2(px * gs / tuft_scale));
    blade *= tuft * tuft_strength + (1.0 - tuft) * 0.12;

    // Random organic bare patches: fbm carves irregular regions with no blades.
    float bare_field = fbm2(px * gs / bare_scale + 17.3);
    float bare_cut = 0.28 + bare_amount * 0.32;
    float bare_mask = smoothstep(bare_cut, bare_cut + bare_softness, bare_field);

    // Spatial density field: a softer medium-scale noise thins the grass
    // gradually so the field is not uniformly dense, instead of just on/off.
    float density_field = fbm2(px * gs / density_scale + 29.7);
    float density = mix(density_min, 1.0, smoothstep(0.2, 0.8, density_field));

    float coverage = bare_mask * density;
    blade *= coverage;

    float height = vnoise(vec2(px.x * 0.06 * gs, px.y * 0.06 * gs));
    float tip = smoothstep(0.35, 0.85, height);

    col = mix(col, blade_color.rgb, blade * 0.85);
    col = mix(col, tip_color.rgb, blade * tip * 0.6);

    // Macro variation: a very large-scale field tints whole regions warmer/
    // brighter or cooler/darker so the field is not one flat carpet of green.
    float macro = (fbm2(px * gs / macro_scale + 71.4) - 0.5) * 2.0;
    col *= 1.0 + macro * macro_strength;
    col.r *= 1.0 + macro * macro_strength * 0.25;
    col.b *= 1.0 - macro * macro_strength * 0.35;

    col = mix(col, soil_color.rgb, (1.0 - coverage) * bare_soil_mix);

    // Procedural normal from the blade height field. Written in canvas space
    // (Y down); negative gradient tilts each blade so it catches the light.
    // Sparse and bare areas scale the gradient to zero so the ground reads flat.
    float h0 = grass_height(px);
    float hx = grass_height(px + vec2(1.0, 0.0));
    float hy = grass_height(px + vec2(0.0, 1.0));
    vec2 grad = vec2(-(hx - h0), -(hy - h0)) * normal_strength * coverage;
    NORMAL = normalize(vec3(grad, 1.0));

    blade_mask = blade;
    COLOR = vec4(col, 1.0);
}

void light() {
    vec3 n = normalize(NORMAL);
    vec3 l = normalize(LIGHT_DIRECTION);
    float ndotl = max(dot(n, l), 0.0);

    vec3 diffuse = LIGHT_COLOR.rgb * COLOR.rgb * LIGHT_ENERGY * ndotl;

    vec3 half_vec = normalize(l + vec3(0.0, 0.0, 1.0));
    float ndoth = max(dot(n, half_vec), 0.0);
    float spec = pow(ndoth, shininess) * specular_strength * (0.4 + blade_mask);
    vec3 specular = LIGHT_COLOR.rgb * spec * LIGHT_ENERGY;

    LIGHT = vec4(diffuse + specular, LIGHT_COLOR.a);
}
