shader_type canvas_item;
render_mode unshaded, blend_add;

// Ground companion to Cenario_Rain_Shader.gdshader. One hashed splash per
// world-space cell, world-locked like the rain so it never swims with the
// camera. The ground plane is foreshortened (squash), rings are measured in
// world units with fwidth anti-aliasing so they never collapse into hairlines,
// and the ring radius stays clamped inside its own cell so a single cell
// lookup is enough.

group_uniforms splash;
uniform vec4 splash_color : source_color = vec4(0.72, 0.85, 1.0, 1.0);
uniform float opacity : hint_range(0.0, 1.0) = 0.45;

group_uniforms layout;
uniform float cell_size : hint_range(50.0, 1200.0) = 300.0;
uniform float splash_speed : hint_range(0.05, 5.0) = 1.6;
uniform float ring_size : hint_range(10.0, 300.0) = 55.0;
// 0 = every ring is exactly ring_size; 1 = rings range from 0 to 2x ring_size.
uniform float ring_size_variance : hint_range(0.0, 1.0) = 0.25;
// Ring band width in world units, not a fraction of the ring.
uniform float ring_thickness : hint_range(1.0, 40.0) = 8.0;
// Vertical foreshortening: 1.0 is a perfect circle, 2.0 presses it flat.
uniform float squash : hint_range(1.0, 4.0) = 2.0;

group_uniforms droplets;
uniform float droplet_chance : hint_range(0.0, 1.0) = 0.6;
// Droplet radius in world units.
uniform float droplet_size : hint_range(2.0, 60.0) = 8.0;

group_uniforms wind;
uniform float wind_drift : hint_range(-1.0, 1.0) = 0.0;

group_uniforms lightning;
uniform float flash_strength : hint_range(0.0, 1.0) = 0.0;
uniform float flash_glow : hint_range(0.0, 3.0) = 1.5;

float hash12(vec2 p)
{
    vec3 p3 = fract(vec3(p.xyx) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return fract((p3.x + p3.y) * p3.z);
}

vec2 hash22(vec2 p)
{
    vec3 p3 = fract(vec3(p.xyx) * vec3(0.1031, 0.1030, 0.0973));
    p3 += dot(p3, p3.yzx + 33.33);
    return fract((p3.xx + p3.yz) * p3.zy);
}

varying vec2 world_pos;

void vertex()
{
    world_pos = (MODEL_MATRIX * vec4(VERTEX.xy, 0.0, 1.0)).xy;
}

void fragment()
{
    vec2 cell = floor(world_pos / cell_size);
    vec2 h = hash22(cell);
    float phase = hash12(cell + vec2(7.31, 19.17));

    float t = fract(TIME * splash_speed * mix(0.7, 1.3, phase) + phase);

    float size_hash = hash12(cell + vec2(53.71, 7.19));
    float radius = min(ring_size * mix(1.0 - ring_size_variance, 1.0 + ring_size_variance, size_hash), cell_size * 0.45);

    vec2 center = (cell + 0.2 + 0.8 * h) * cell_size;
    center.x += wind_drift * t * radius;

    vec2 rel = world_pos - center;
    rel.y *= squash;

    float dist = length(rel);

    // Pixel size must come from the world varying, never from dist: the
    // splash center jumps at every cell border, so fwidth(dist) explodes
    // there and paints bright lines along the whole cell grid.
    float pixel_world = max(fwidth(world_pos.x), 0.001);
    float thickness = max(ring_thickness, pixel_world * 1.5);

    float ring = 1.0 - smoothstep(thickness - pixel_world * 0.5, thickness + pixel_world * 0.5, abs(dist - t * radius));

    float life = 1.0 - t;
    float fade = smoothstep(0.0, 0.15, t) * life * life;

    float dimple_size = max(thickness, radius * 0.12);
    float dimple = (1.0 - smoothstep(0.0, dimple_size, dist)) * life * life * life;

    float drops = 0.0;
    for (int i = 0; i < 3; i++)
    {
        float fi = float(i);
        vec2 dh = hash22(cell + vec2(fi * 37.13, fi * 91.77));
        float gate = 1.0 - step(droplet_chance, hash12(cell * 2.7 + fi * 5.31));

        // Each droplet gets its own travel distance, otherwise they sit on a
        // shared circle and read as a hard arc instead of scattered specks.
        float ang = dh.x * TAU;
        float travel = mix(0.2, 0.85, dh.y) * radius * t;
        vec2 offset = vec2(cos(ang) * travel, sin(ang) * travel / squash);

        float dd = distance(world_pos, center + offset) / max(droplet_size, 1.0);
        drops += (1.0 - smoothstep(0.0, 1.0, dd)) * gate;
    }
    drops *= fade;

    float alpha = clamp(ring + dimple * 0.8 + drops * 0.7, 0.0, 1.0) * opacity;

    vec3 col = splash_color.rgb * (1.0 + flash_strength * flash_glow);

    COLOR = vec4(col, alpha);
}
