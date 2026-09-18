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
// LIT VARIANT. The sprite's texture is a CanvasTexture whose diffuse is an
// opaque white placeholder (so the sprite texture can never mask the shader
// output) and whose normal/specular maps are given per-tuft. That map is read
// at the TUFT UV through the engine built-ins NORMAL_TEXTURE and
// SPECULAR_SHININESS_TEXTURE, so a custom light() can shade every tuft while
// the meadow keeps reacting to Light2D like GrassProcedural_PixelCrisp.

uniform sampler2D grass_texture : repeat_enable, filter_nearest;

// World-space size of one grass tuft texture tile (40x21 texels at the draw scale).
uniform vec2 tuft_world_size = vec2(200.0, 105.0);

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

group_uniforms lighting;
uniform float normal_strength : hint_range(0.0, 4.0) = 1.0;
uniform float specular_strength : hint_range(0.0, 2.0) = 0.3;
uniform float shininess : hint_range(1.0, 128.0) = 24.0;

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

    vec2 cell_size = tuft_world_size * spacing;
    vec2 cell = floor(world_pos / cell_size);

    // Search the 3x3 window of cells around this fragment: any of them may
    // own a tuft that covers this pixel, because tufts are allowed to drift
    // past their own cell borders. The first tuft that covers the pixel wins.
    bool drawn = false;
    for (int dy = -1; dy <= 1; dy++) {
        for (int dx = -1; dx <= 1; dx++) {
            vec2 cc = cell + vec2(float(dx), float(dy));

            // Clumping field at this cell's centre, sampled per cell so
            // neighbouring fragments agree. Drives how many tufts the cell
            // spawns: dense meadow zones get more, sparse zones fewer. A 0
            // count simply leaves the cell bare, producing unevenness
            // without ever restoring a visible grid.
            float field = fbm2((cc + vec2(0.5)) * cell_size / clump_scale + 47.3);
            float density = mix(1.0, 0.4 + 2.3 * field, clump_strength);

            float rh = hash21(cc + vec2(9.1, 4.4));
            int count = int(floor(rh + density));

            for (int k = 0; k < count; k++) {
                // Per-copy seed: every tuft of every cell is independent.
                vec2 sk = cc * 17.31 + vec2(float(k) * 7.17, 3.1);

                // Random per-copy size. Capped so a scaled tuft never grows
                // larger than its cell, which keeps the 3x3 window enough to
                // catch every tuft that can reach this fragment.
                float scale = 1.0 + (hash21(sk + vec2(1.3, 8.8)) - 0.5) * 2.0 * size_variation;
                scale *= 1.0 + (field - 0.5) * clump_strength;
                float scale_cap = min(cell_size.x / tuft_world_size.x, cell_size.y / tuft_world_size.y);
                scale = clamp(scale, 0.3, scale_cap);
                vec2 tuft_size = tuft_world_size * scale;

                // Random placement: the origin may land anywhere across the
                // cell (jitter = 1.0), so copies from neighbouring cells can
                // overlap freely and the whole meadow blends together.
                float rx = hash21(sk + vec2(7.1, 5.3));
                float ry = hash21(sk + vec2(3.7, 1.9));
                vec2 place = vec2(0.5) + (vec2(rx, ry) - 0.5) * jitter;
                vec2 origin = cc * cell_size + cell_size * place;

                vec2 in_tuft = world_pos - origin;
                bool inside = in_tuft.x >= 0.0 && in_tuft.y >= 0.0 &&
                              in_tuft.x < tuft_size.x && in_tuft.y < tuft_size.y;
                if (!inside) {
                    continue;
                }

                vec2 uv = in_tuft / tuft_size;

                // Random horizontal mirror per copy. The tuft texture samples
                // the mirrored UV, but the normal map is read from the
                // un-mirrored UV (the map is the shape of ONE tuft) and its X
                // is flipped, so a mirrored tuft lights like its mirror does.
                bool flipped = hash21(sk + vec2(2.3, 6.1)) < flip_chance;
                vec2 uv_tx = uv;
                if (flipped) {
                    uv_tx.x = 1.0 - uv.x;
                }

                vec4 tex = texture(grass_texture, uv_tx);
                if (tex.a <= 0.001) {
                    continue;
                }

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

                // Per-tuft lighting from the sprite's CanvasTexture component
                // maps. NORMAL_TEXTURE / SPECULAR_SHININESS_TEXTURE are engine
                // built-ins fed by the CanvasTexture on the sprite. The normal
                // is decoded with the engine's own 2D conversion (Y down);
                // the per-copy mirror flips its X.
                vec4 ntex = texture(NORMAL_TEXTURE, uv);
                vec3 nrm;
                nrm.xy = ntex.xy * vec2(2.0, -2.0) - vec2(1.0, -1.0);
                nrm.z = sqrt(max(0.001, 1.0 - dot(nrm.xy, nrm.xy)));
                if (flipped) {
                    nrm.x = -nrm.x;
                }
                NORMAL = normalize(vec3(nrm.xy * normal_strength, nrm.z));
                spec_amount = texture(SPECULAR_SHININESS_TEXTURE, uv).r;

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