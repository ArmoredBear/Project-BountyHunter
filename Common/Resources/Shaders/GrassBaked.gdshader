shader_type canvas_item;

// BAKED GRASS VARIANT. Replaces the procedural scatter of
// GrassRepeat.gdshader with pre-baked tuft quads: a C# baker once generates
// every tuft (same clumping/density/type/size/placement math as the scatter),
// spells them out as world-space quads on a MeshInstance2D and this shader
// just draws them. The result keeps the exact look of the procedural meadow
// (sizes, colour ramp, border rim, lighting through Light2D) while removing
// the expensive per-pixel 3x3 cell search and the per-tuft O(64) border tests.
//
// VERTEX ATTRIBUTES FROM THE BAKER
//   VERTEX   world-space quad corners (mesh lives at identity, local == world)
//   UV       0..1 inside one tuft sprite
//   COLOR    per-tuft tone (the a->b->c colour ramp); alpha is unused
//   CUSTOM0  vec4(tuft_world_origin.xy, tuft_world_size.xy)
//   CUSTOM1  vec4(type, flipped, 0.0, 0.0)
//
// DEPTH SPLIT. The meadow keeps occluding the player in 2D exactly like the
// procedural version: two passes render the SAME static mesh, one locked to
// depth_split_mode 1 (draws tufts rooted behind the split line, i.e. behind
// the player) and one to mode 2 (in front). The vertex shader clips, per tuft
// and per pass, any tuft that belongs to the other layer by pushing it far
// off-screen, so back/front membership can follow the player for free and no
// mesh is ever rebuilt.
//
// TILEMAP-STYLE CULLING. The baker partitions the meadow into tile meshes, so
// the renderer's own item culling (the same mechanism TileMapLayers rely on)
// skips entire tiles the moment they leave the camera view. No per-frame
// script culling is needed.
//
// SWAY. Kept as a real geometric lean in the vertex shader (cheap): the part
// of the tuft above `sway_height_start` leans horizontally with a sine sweep.
// `vertex_sway_scale` converts the abstract strength into world units.

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

group_uniforms lighting;
uniform float normal_strength : hint_range(0.0, 4.0) = 1.0;
uniform float specular_strength : hint_range(0.0, 2.0) = 0.3;
uniform float shininess : hint_range(1.0, 128.0) = 24.0;

group_uniforms sway;
// 0 = random: every tuft sways with its own phase (seeded by its origin).
// 1 = wave: a travelling gust sweeps across the meadow, tuft by tuft.
uniform int sway_mode : hint_range(0, 1) = 0;
uniform float sway_strength : hint_range(0.0, 10.0) = 0.5;
uniform float sway_speed : hint_range(0.0, 4.0) = 0.5;
uniform float sway_height_start : hint_range(0.0, 1.0) = 0.0;
// Wave mode: spatial frequency of the travelling gust (phase per world unit)
// and how fast the gust sweeps across the field (TIME factor).
uniform float wave_scale : hint_range(0.001, 0.5) = 0.02;
uniform float wave_speed : hint_range(0.0, 4.0) = 0.8;
// Scales the geometric lean into world units; the procedural version
// normalized by the tuft's texel width, here it is a direct world offset.
uniform float vertex_sway_scale : hint_range(0.0, 64.0) = 8.0;

group_uniforms depth_split;
// 0 = off (this pass draws every tuft of the meadow).
// 1 = back layer: tufts rooted above the split line only (behind the player).
// 2 = front layer: tufts rooted at or below the split line only (in front).
uniform int depth_split_mode : hint_range(0, 2) = 0;
// World-space Y separating back from front grass (usually the player's feet).
uniform float depth_split_y = 0.0;

varying float tuft_type;
varying float tuft_flip;
varying float spec_amount;

float hash21(vec2 p) {
    p = fract(p * vec2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return fract(p.x * p.y);
}

void vertex()
{
    vec2 origin = CUSTOM0.xy;

    // Depth split clip: this pass only draws the tufts that belong to it.
    // Pushing vertices far off-screen discards the whole triangle without a
    // rebuild, keeping the membership change frame-cheap while the split line
    // follows the player. Early returns are illegal in processor functions,
    // so the push factor is accumulated instead.
    float push = 0.0;
    if (depth_split_mode == 1) {
        push = step(depth_split_y, origin.y); // 1.0 when the tuft is above the line
    }
    if (depth_split_mode == 2) {
        push = 1.0 - step(depth_split_y, origin.y); // 1.0 when the tuft is below the line
    }
    VERTEX += vec2(1000000.0, 1000000.0) * push;

    tuft_type = CUSTOM1.x;
    tuft_flip = CUSTOM1.y;

    // Geometric sway: the part of the tuft above `sway_height_start` leans.
    float phase = (sway_mode == 1) ?
        (origin.x + origin.y) * wave_scale - TIME * wave_speed :
        hash21(origin * 1.731 + vec2(3.1, 8.8)) * TAU;
    float row = max(0.0, 1.0 - UV.y - sway_height_start);
    float wave = sin(TIME * sway_speed + phase);
    VERTEX.x += row * wave * sway_strength * vertex_sway_scale;
}

void fragment()
{
    // The mirrored (flipped) tufts sample their diffuse mirrored, exactly like
    // the procedural scatter; the normal/spec maps stay unmirrored because
    // they describe a single tuft shape, and the normal X is flipped below so
    // a mirrored tuft lights the way its mirror should.
    vec2 uvs = UV;
    if (tuft_flip > 0.5) {
        uvs.x = 1.0 - uvs.x;
    }

    vec4 tex;
    vec3 nrm;
    if (tuft_type > 1.5) {
        tex = texture(grass_texture_c, uvs);
        vec4 ntex = texture(grass_normal_c, UV);
        nrm.xy = ntex.xy * vec2(2.0, -2.0) - vec2(1.0, -1.0);
        nrm.z = sqrt(max(0.001, 1.0 - dot(nrm.xy, nrm.xy)));
        spec_amount = texture(grass_specular_c, UV).r;
    } else if (tuft_type > 0.5) {
        tex = texture(grass_texture_b, uvs);
        vec4 ntex = texture(grass_normal_b, UV);
        nrm.xy = ntex.xy * vec2(2.0, -2.0) - vec2(1.0, -1.0);
        nrm.z = sqrt(max(0.001, 1.0 - dot(nrm.xy, nrm.xy)));
        spec_amount = texture(grass_specular_b, UV).r;
    } else {
        tex = texture(grass_texture_a, uvs);
        vec4 ntex = texture(grass_normal_a, UV);
        nrm.xy = ntex.xy * vec2(2.0, -2.0) - vec2(1.0, -1.0);
        nrm.z = sqrt(max(0.001, 1.0 - dot(nrm.xy, nrm.xy)));
        spec_amount = texture(grass_specular_a, UV).r;
    }
    if (tex.a <= 0.001) {
        discard;
    }

    if (tuft_flip > 0.5) {
        nrm.x = -nrm.x;
    }
    NORMAL = normalize(vec3(nrm.xy * normal_strength, nrm.z));

    // The textures are white silhouettes; the per-tuft tone rides in the
    // vertex COLOR (the a->b->c ramp is baked, so every blade of one tuft
    // shares one tone while different tufts differ, same as the scatter).
    // The texture RGB is deliberately ignored (it is white) so the tone is
    // used verbatim, matching the procedural shader.
    COLOR = vec4(COLOR.rgb, tex.a);
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