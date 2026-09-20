shader_type canvas_item;

// Scatters a single grass sprite texture as separate tufts on a forest
// ground. Mirrors Cenario_Rain_Shader.gdshader: the pattern runs in WORLD
// space, so resizing the sprite only reveals more ground while each tuft
// keeps its world-space size.
//
// The scatter forms a continuous uneven meadow: every cell spawns a small
// number of tufts (0-3, picked per cell from a low-frequency clumping field),
// each one randomly placed, scaled, flipped and shaded so neighbouring cells'
// grass overlaps and no grid, spacing or tile reconstruction is visible.
// ``clump_strength`` 0.0 flattens the field to a uniform density.
//
// LIT VARIANT. Each grass type carries its own white-silhouette diffuse and
// its own normal/specular map, read at the TUFT UV from explicit samplers, so
// a custom light() can shade every tuft while the meadow keeps reacting to
// Light2D like GrassProcedural_PixelCrisp.
//
// GRASS VARIETY. Extra tuft types can be blended into the meadow: each copy
// rolls a weighted pick over type B (grass_texture_b + its own normal/specular
// samplers + tuft_world_size_b), type C (the grass_texture_c set) and type A
// (the grass_texture_a set), all with the same layout of parameters. Mixing
// types with separate scenes would multiply the per-pixel fragment work; a few
// extra cached texture fetches in one pass stay nearly free instead.
//
// WIND & SWAY. Adapted from the CC0 shader "2D Pixel perfect wind & sway
// effect" (godotshaders.com, kaeylos). Each tuft copy shifts its sample UV
// horizontally: a quantized per-texel-row noise adds the wind, a sinusoid
// graded by tuft height adds the sway. The offsets are masked to the top of
// the tuft and normalized by the tuft's texel width so both grass kinds and
// all copy scales sway by a comparable on-screen amount. Pure UV distortion:
// no vertex deformation, so the world-space scatter/cells stay untouched.
// The final horizontal offset is snapped to whole screen pixels (via fwidth
// of the world position), so the blades step in discrete pixel jumps — a
// pixel-perfect look instead of sub-pixel shimmer.
// ``sway_mode`` selects between per-tuft random sway (each tuft moves on its
// own, seeded by its copy hash) and a travelling gust that sweeps a wave band
// across the meadow using each tuft's world position as the phase.

group_uniforms grass_variety;
uniform sampler2D grass_texture_a : repeat_enable, filter_nearest;
uniform sampler2D grass_normal_a : repeat_enable, filter_nearest;
uniform sampler2D grass_specular_a : repeat_enable, filter_nearest;
uniform sampler2D grass_texture_b : repeat_enable, filter_nearest;
uniform sampler2D grass_normal_b : repeat_enable, filter_nearest;
uniform sampler2D grass_specular_b : repeat_enable, filter_nearest;
uniform sampler2D grass_texture_c : repeat_enable, filter_nearest;
uniform sampler2D grass_normal_c : repeat_enable, filter_nearest;
uniform sampler2D grass_specular_c : repeat_enable, filter_nearest;
// World-space sizes of each tuft tile (their own texel aspect ratios).
uniform vec2 tuft_world_size_a = vec2(200.0, 105.0);
uniform vec2 tuft_world_size_b = vec2(22.0, 88.0);
uniform vec2 tuft_world_size_c = vec2(47.0, 47.0);
// Share of tufts of each type (the three chances are normalized, so they
// only compete against each other and always add up to every spawned tuft
// being one of the three types). Each type carries its own diffuse, normal
// and specular samplers.
uniform float grass_a_chance : hint_range(0.0, 1.0) = 1.0;
uniform float grass_b_chance : hint_range(0.0, 1.0) = 0.5;
uniform float grass_c_chance : hint_range(0.0, 1.0) = 0.0;
// Texel dimensions of each tuft texture: drive the wind UV quantization and
// the normalization that keeps the sway amount equal across the grass kinds.
uniform vec2 tuft_texels_a = vec2(40.0, 21.0);
uniform vec2 tuft_texels_b = vec2(18.0, 71.0);
uniform vec2 tuft_texels_c = vec2(38.0, 38.0);

group_uniforms grass_colors;
uniform vec4 grass_color_a : source_color = vec4(0.05, 0.30, 0.12, 1.0);
uniform vec4 grass_color_b : source_color = vec4(0.10, 0.45, 0.16, 1.0);
uniform vec4 grass_color_c : source_color = vec4(0.30, 0.55, 0.12, 1.0);
// How strongly each blade's tone spreads through the a->b->c ramp.
// 0.0 = every blade the same mid tone, 1.0 = full dark-to-light spread.
uniform float color_variation : hint_range(0.0, 1.0) = 0.8;

group_uniforms scatter;
uniform float spacing : hint_range(1.0, 16.0) = 4.0;
uniform float jitter : hint_range(0.0, 1.0) = 1.0;
uniform float size_variation : hint_range(0.0, 1.0) = 0.6;
uniform float flip_chance : hint_range(0.0, 1.0) = 0.5;

group_uniforms clumping;
uniform float clump_scale : hint_range(64.0, 4096.0) = 800.0;
uniform float clump_strength : hint_range(0.0, 1.0) = 0.7;
// Multiplier for the number of tufts each cell spawns (1.0 = current look).
uniform float density : hint_range(0.0, 12.0) = 1.0;
// Probability a cell is left bare (spawns no tufts), carving gaps between patches.
uniform float empty_chance : hint_range(0.0, 0.95) = 0.0;

group_uniforms lighting;
uniform float normal_strength : hint_range(0.0, 4.0) = 1.0;
uniform float specular_strength : hint_range(0.0, 2.0) = 0.3;
uniform float shininess : hint_range(1.0, 128.0) = 24.0;

group_uniforms wind;
uniform float wind_strength : hint_range(0.0, 2.0) = 1.0;
uniform float wind_scale : hint_range(0.0, 64.0) = 10.0;
uniform float wind_speed : hint_range(0.0, 2.0) = 0.1;
// Fraction of the tuft (measured from the bottom) that stays unaffected by wind.
uniform float wind_height_start : hint_range(0.0, 1.0) = 0.0;

group_uniforms sway;
// 0 = random: every tuft sways with its own phase and wind pattern.
// 1 = wave: a travelling gust sweeps across the meadow, tuft by tuft.
uniform int sway_mode : hint_range(0, 1) = 0;
uniform float sway_strength : hint_range(0.0, 10.0) = 0.5;
uniform float sway_speed : hint_range(0.0, 4.0) = 0.5;
// Fraction of the tuft (measured from the bottom) that stays unaffected by sway.
uniform float sway_height_start : hint_range(0.0, 1.0) = 0.0;
// Wave mode: spatial frequency of the travelling gust (phase per world unit)
// and how fast the gust sweeps across the field (TIME factor).
uniform float wave_scale : hint_range(0.001, 0.5) = 0.02;
uniform float wave_speed : hint_range(0.0, 4.0) = 0.8;

group_uniforms depth_split;
// 0 = off (this layer draws the whole meadow every frame).
// 1 = back layer: only tufts rooted above the split line (behind the player).
// 2 = front layer: only tufts rooted at or below the split line (in front).
uniform int depth_split_mode : hint_range(0, 2) = 0;
// World-space Y separating back from front grass (usually the player's feet).
uniform float depth_split_y = 0.0;

// Border detection. The meadow is a hand-drawn Polygon2D, and every grass
// copy must stay fully inside it: a tuft whose front/right edge crosses the
// polygon outline would be hard-sliced by the sprite's clip. So the C#
// splitter uploads the polygon outline as a world-space point list, the
// shader computes the tuft-to-edge distance, and any copy landing within
// ``border_radius`` of (or past) the outline is skipped entirely — nothing
// is ever drawn in the rim, so nothing ever gets cut.
//
// ``polygon_point_count`` is 0 when the shape hasn't been drawn yet, which
// disables the border check completely (grass renders everywhere as before).
uniform int polygon_point_count = 0;
// The drawn outline, in world space, decimated to at most 64 vertices by the
// C# splitter so the per-fragment edge tests stay bounded.
uniform vec2 polygon_points[64];
// Width of the empty rim along the outline (world units). Larger = a wider
// uncovered band so no blade sits close enough to the edge to be clipped.
uniform float border_radius : hint_range(0.0, 200.0) = 32.0;

varying vec2 world_pos;
varying float spec_amount;

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

vec2 wind_random(vec2 p) {
    p = vec2(dot(p, vec2(127.1, 311.7)), dot(p, vec2(269.5, 183.3)));
    return -1.0 + 2.0 * fract(sin(p) * 43758.5453123);
}

float wind_noise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    vec2 blur = smoothstep(0.0, 1.0, f);
    return mix(
        mix(dot(wind_random(i + vec2(0.0, 0.0)), f - vec2(0.0, 0.0)),
            dot(wind_random(i + vec2(1.0, 0.0)), f - vec2(1.0, 0.0)), blur.x),
        mix(dot(wind_random(i + vec2(0.0, 1.0)), f - vec2(0.0, 1.0)),
            dot(wind_random(i + vec2(1.0, 1.0)), f - vec2(1.0, 1.0)), blur.x), blur.y) + 0.5;
}

bool point_in_polygon(vec2 p) {
    // Classic even-odd ray cast over the decimated outline. Works for
    // concave (and self-intersection-free) hand-drawn shapes.
    if (polygon_point_count < 3) return true;
    bool inside = false;
    for (int i = 0, j = polygon_point_count - 1; i < polygon_point_count; j = i++) {
        vec2 a = polygon_points[i];
        vec2 b = polygon_points[j];
        bool crosses = (b.y > p.y) != (a.y > p.y);
        if (crosses && p.x < (a.x - b.x) * (p.y - b.y) / (a.y - b.y) + b.x) {
            inside = !inside;
        }
    }
    return inside;
}

// Squared distance from p to the segment ab.
float dist2_segment(vec2 p, vec2 a, vec2 b) {
    vec2 ab = b - a;
    vec2 ap = p - a;
    float t = dot(ap, ab) / max(dot(ab, ab), 1e-6);
    t = clamp(t, 0.0, 1.0);
    vec2 q = a + ab * t;
    vec2 d = p - q;
    return dot(d, d);
}

// Squared distance from p to the nearest outline edge.
float dist2_to_outline(vec2 p) {
    float best = 1e18;
    for (int i = 0; i < polygon_point_count; i++) {
        vec2 a = polygon_points[i];
        vec2 b = polygon_points[(i + 1) % polygon_point_count];
        best = min(best, dist2_segment(p, a, b));
    }
    return best;
}

// Squared distance from p to the axis-aligned rectangle whose bottom-right
// corner is origin + size (the tuft's full footprint).
float dist2_to_rect(vec2 p, vec2 origin, vec2 size) {
    vec2 q = clamp(p, origin, origin + size);
    vec2 d = p - q;
    return dot(d, d);
}

void vertex()
{
    world_pos = (MODEL_MATRIX * vec4(VERTEX.xy, 0.0, 1.0)).xy;
}

void fragment()
{
    // Incoming COLOR carries the sprite's Modulate / Self Modulate. The
    // texture is a white silhouette, so this color is what actually paints
    // the grass; it must be multiplied back into the final output.
    vec4 sprite_color = COLOR;

    vec2 cell_size = tuft_world_size_a * spacing;
    vec2 cell = floor(world_pos / cell_size);

    // Search the 3x3 window of cells around this fragment: any of them may
    // own a tuft that covers this pixel, because tufts are allowed to drift
    // past their own cell borders. The first tuft that covers the pixel wins.
    bool drawn = false;
    for (int dy = -1; dy <= 1; dy++) {
        for (int dx = -1; dx <= 1; dx++) {
            vec2 cc = cell + vec2(float(dx), float(dy));
            // Depth split fast path: skip whole cells that lie entirely on
            // the other layer's side of the split line (tuft origins always
            // stay inside their own cell), so each layer only does the clump
            // and copy math for cells that can belong to it.
            if (depth_split_mode == 1 && cc.y * cell_size.y >= depth_split_y) continue;
            if (depth_split_mode == 2 && (cc.y + 1.0) * cell_size.y < depth_split_y) continue;

            // Clumping field at this cell's centre, sampled per cell so
            // neighbouring fragments agree. Drives how many tufts the cell
            // spawns: dense meadow zones get more, sparse zones fewer. A 0
            // count simply leaves the cell bare, producing unevenness
            // without ever restoring a visible grid.
            float field = fbm2((cc + vec2(0.5)) * cell_size / clump_scale + 47.3);
            float density_field = mix(1.0, 0.4 + 2.3 * field, clump_strength) * density;

            float rh = hash21(cc + vec2(9.1, 4.4));
            int count = int(floor(rh + density_field));
            // Roll whether this cell is left bare: consistent per cell (same
            // hash in every fragment), so it just carves sparse gaps between
            // clumps instead of flickering.
            if (hash21(cc + vec2(3.3, 7.7)) < empty_chance) {
                count = 0;
            }

            for (int k = 0; k < count; k++) {
                // Per-copy seed: every tuft of every cell is independent.
                vec2 sk = cc * 17.31 + vec2(float(k) * 7.17, 3.1);

                // Per-copy type: a weighted roll picks type A / B / C by normalizing the
                // three chance uniforms, and the chosen type brings its own
                // size, diffuse, normal and specular.
                float type_roll = hash21(sk + vec2(5.9, 13.1));
                int kind;
                float chance_total = max(0.0001, grass_a_chance + grass_b_chance + grass_c_chance);
                float a_share = grass_a_chance / chance_total;
                float b_share = a_share + grass_b_chance / chance_total;
                if (type_roll < a_share) {
                    kind = 0;
                } else if (type_roll < b_share) {
                    kind = 1;
                } else {
                    kind = 2;
                }
                vec2 base_size = kind == 1 ? tuft_world_size_b : (kind == 2 ? tuft_world_size_c : tuft_world_size_a);

                // Random per-copy size. Capped so a scaled tuft never grows
                // larger than its cell, which keeps the 3x3 window enough to
                // catch every tuft that can reach this fragment.
                float scale = 1.0 + (hash21(sk + vec2(1.3, 8.8)) - 0.5) * 2.0 * size_variation;
                scale *= 1.0 + (field - 0.5) * clump_strength;
                float scale_cap = min(cell_size.x / base_size.x, cell_size.y / base_size.y);
                scale = clamp(scale, 0.3, scale_cap);
                vec2 tuft_size = base_size * scale;

                // Random placement: the origin may land anywhere across the
                // cell (jitter = 1.0), so copies from neighbouring cells can
                // overlap freely and the whole meadow blends together.
                float rx = hash21(sk + vec2(7.1, 5.3));
                float ry = hash21(sk + vec2(3.7, 1.9));
                vec2 place = vec2(0.5) + (vec2(rx, ry) - 0.5) * jitter;
                vec2 origin = cc * cell_size + cell_size * place;

                // Authoritative depth cull for cells straddling the split: a
                // tuft sits behind the player when rooted above the line, in
                // front when rooted at/below it. Each tuft is owned by exactly
                // one layer, so the halves never duplicate or pop as the line
                // follows the player.
                if (depth_split_mode == 1 && origin.y >= depth_split_y) continue;
                if (depth_split_mode == 2 && origin.y < depth_split_y) continue;

                // Cheap footprint test first: most pixels on the meadow lie
                // between tufts, and the border math below is the expensive
                // part (O(64) loops over the outline). Only fragments that
                // actually fall inside a copy's footprint pay for it — a
                // rejected border tuft is treated exactly like a missing one,
                // so moving this test earlier changes nothing visually.
                vec2 in_tuft = world_pos - origin;
                bool inside = in_tuft.x >= 0.0 && in_tuft.y >= 0.0 &&
                              in_tuft.x < tuft_size.x && in_tuft.y < tuft_size.y;
                if (!inside) {
                    continue;
                }

                // Border rejection ("outline rim"). The tuft only draws when
                // its whole footprint (all four corners) stays strictly inside
                // the drawn polygon, every corner sits at least
                // ``border_radius`` away from every outline edge, AND every
                // outline vertex keeps at least ``border_radius`` clearance
                // from the full footprint rectangle. Together those two tests
                // give the exact minimum distance between the tuft's
                // height-and-width box and the outline: a long tuft whose edge
                // midline slides past an outline vertex would otherwise still
                // be hard-sliced mid-tuft even though its corners are far. The
                // result is an empty rim of bare ground just inside the
                // shape's edge, so grass never pokes past the drawn meadow.
                // Border math is skipped entirely while the shape hasn't been
                // drawn (polygon_point_count == 0).
                if (polygon_point_count >= 3) {
                    vec2 c0 = origin;
                    vec2 c1 = origin + vec2(tuft_size.x, 0.0);
                    vec2 c2 = origin + vec2(0.0, tuft_size.y);
                    vec2 c3 = origin + tuft_size;
                    bool fully_inside = point_in_polygon(c0) && point_in_polygon(c1) &&
                                        point_in_polygon(c2) && point_in_polygon(c3);
                    if (!fully_inside) {
                        continue;
                    }
                    float r2 = border_radius * border_radius;
                    if (dist2_to_outline(c0) < r2 || dist2_to_outline(c1) < r2 ||
                        dist2_to_outline(c2) < r2 || dist2_to_outline(c3) < r2) {
                        continue;
                    }
                    bool vertex_near_footprint = false;
                    for (int i = 0; i < polygon_point_count; i++) {
                        if (dist2_to_rect(polygon_points[i], origin, tuft_size) < r2) {
                            vertex_near_footprint = true;
                            break;
                        }
                    }
                    if (vertex_near_footprint) {
                        continue;
                    }
                }

                vec2 uv = in_tuft / tuft_size;

                // Animated wind and sway (adapted from "2D Pixel perfect wind
                // & sway effect"). A horizontal UV offset, quantized per tuft
                // texel for pixel-stable motion: a noise field drifts through
                // the tuft as wind, a sinusoid graded by tuft height adds the
                // sway, the top of the tuft takes the strongest movement, and
                // it is normalized by the tuft's texel width so both grass
                // kinds and all copy scales sway by a comparable screen amount.
                //
                // sway_mode 0 (random): the per-copy seed nudges the sway phase
                // and the wind noise domain, so every tuft moves on its own.
                // sway_mode 1 (wave): the phase is driven by the tuft's world
                // position and time, so a gust band travels across the meadow
                // and each tuft leans as the band passes over it.
                vec2 texel_count = kind == 1 ? tuft_texels_b : (kind == 2 ? tuft_texels_c : tuft_texels_a);
                vec2 puv = floor(uv * texel_count) / texel_count;
                float copy_hash = hash21(sk + vec2(8.1, 2.7));
                float phase;
                vec2 wind_drift;
                if (sway_mode == 1) {
                    phase = (origin.x + origin.y) * wave_scale - TIME * wave_speed;
                    wind_drift = vec2(TIME * wind_speed * 2.0 + (origin.x + origin.y * 0.7) * 0.02, 0.0);
                } else {
                    phase = copy_hash * TAU;
                    wind_drift = vec2(TIME * wind_speed, copy_hash * 137.0);
                }
                float wind_n = wind_noise(puv * wind_scale + wind_drift) * 2.0 - 1.0;
                float sway_row = max(0.0, 1.0 - puv.y - sway_height_start) * sin(TIME * sway_speed + phase);
                float sway_uv = (sway_row * sway_strength + wind_n * step(puv.y, 1.0 - wind_height_start) * wind_strength) / texel_count.x;
                // Pixel-perfect: snap the horizontal shift to whole screen
                // pixels (fwidth gives the world-space size of a pixel at the
                // current zoom), so each texel row steps in discrete pixel
                // jumps instead of drifting sub-pixel and shimmering.
                float pixel_w = fwidth(world_pos).x;
                float sway_world = round(sway_uv * tuft_size.x / pixel_w) * pixel_w;
                sway_uv = sway_world / tuft_size.x;
                // Clamped so the swayed sample never wraps/bleeds over the
                // tuft borders (samplers use repeat, which would copy blades).
                vec2 uvw = clamp(uv + vec2(sway_uv, 0.0), 0.0, 1.0);

                // Random horizontal mirror per copy. The tuft texture samples
                // the mirrored UV, but the normal map is read from the
                // un-mirrored UV (the map is the shape of ONE tuft) and its X
                // is flipped, so a mirrored tuft lights like its mirror does.
                bool flipped = hash21(sk + vec2(2.3, 6.1)) < flip_chance;
                vec2 uv_tx = uvw;
                if (flipped) {
                    uv_tx.x = 1.0 - uvw.x;
                }

                // Diffuse + lighting maps for the chosen type. Each type carries its own
                // samplers; the normal is decoded with the engine's own 2D
                // conversion (Y down), sampled per-tuft so each tuft lights
                // from its own map.
                vec4 tex;
                vec3 nrm;
                if (kind == 2) {
                    tex = texture(grass_texture_c, uv_tx);
                    vec4 ntex = texture(grass_normal_c, uvw);
                    nrm.xy = ntex.xy * vec2(2.0, -2.0) - vec2(1.0, -1.0);
                    nrm.z = sqrt(max(0.001, 1.0 - dot(nrm.xy, nrm.xy)));
                    spec_amount = texture(grass_specular_c, uvw).r;
                } else if (kind == 1) {
                    tex = texture(grass_texture_b, uv_tx);
                    vec4 ntex = texture(grass_normal_b, uvw);
                    nrm.xy = ntex.xy * vec2(2.0, -2.0) - vec2(1.0, -1.0);
                    nrm.z = sqrt(max(0.001, 1.0 - dot(nrm.xy, nrm.xy)));
                    spec_amount = texture(grass_specular_b, uvw).r;
                } else {
                    tex = texture(grass_texture_a, uv_tx);
                    vec4 ntex = texture(grass_normal_a, uvw);
                    nrm.xy = ntex.xy * vec2(2.0, -2.0) - vec2(1.0, -1.0);
                    nrm.z = sqrt(max(0.001, 1.0 - dot(nrm.xy, nrm.xy)));
                    spec_amount = texture(grass_specular_a, uvw).r;
                }
                if (tex.a <= 0.001) {
                    continue;
                }
                if (flipped) {
                    nrm.x = -nrm.x;
                }
                NORMAL = normalize(vec3(nrm.xy * normal_strength, nrm.z));

                // Per-copy color: the texture is a white silhouette, so every blade of a
                // given grass copy gets the SAME solid tone. The tone is a
                // random spot in the a->b->c ramp, seeded per copy so each
                // tuft is one complete shade of green and different tufts
                // differ. color_variation pulls the tone from the shared mid
                // color toward its own spot (0.0 = all tufts mid green).
                float b = hash21(sk + vec2(13.7, 7.9));
                b = mix(0.5, b, color_variation);
                vec3 grad;
                if (b < 0.5) {
                    grad = mix(grass_color_a.rgb, grass_color_b.rgb, b * 2.0);
                } else {
                    grad = mix(grass_color_b.rgb, grass_color_c.rgb, (b - 0.5) * 2.0);
                }

                // The texture is pure white, so the blade is tinted directly
                // with its gram tone (multiplying by it would just wash out).
                COLOR = sprite_color * vec4(grad, tex.a);
                drawn = true;
                break;
            }
            if (drawn) {
                break;
            }
        }
        if (drawn) {
            break;
        }
    }

    // No tuft from any neighbouring cell covers this pixel.
    if (!drawn) {
        COLOR = vec4(0.0, 0.0, 0.0, 0.0);
    }
}

void light() {
    vec3 n = normalize(NORMAL);
    vec3 l = normalize(LIGHT_DIRECTION);
    float ndotl = max(dot(n, l), 0.0);

    vec3 diffuse = LIGHT_COLOR.rgb * COLOR.rgb * LIGHT_ENERGY * ndotl;

    vec3 half_vec = normalize(l + vec3(0.0, 0.0, 1.0));
    float ndoth = max(dot(n, half_vec), 0.0);
    float spec = pow(ndoth, shininess) * specular_strength * spec_amount;
    vec3 specular = LIGHT_COLOR.rgb * spec * LIGHT_ENERGY;

    LIGHT = vec4(diffuse + specular, LIGHT_COLOR.a);
}