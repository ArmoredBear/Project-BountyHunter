// Silent Hill style scene-fixed fog for 2D cenarios.
// Unlike Cenario_Fog.gdshader (the camera chase fog driven every frame by
// FogDriver.cs), this shader needs NO camera data and cannot interfere with it.
// It pins its field to the terrain: vertex() passes the polygon's local vertex
// position (== world, since the node sits at the scene origin with the polygon
// drawn in world coordinates) to fragment() via a varying. Fragment-space
// VERTEX is screen-space, so the world position MUST come from the vertex stage.
// The mush phases are zeroed, so the banks are riveted to the terrain and never
// drift; the only movement is a uniform global pulse that cannot read as
// following the camera.
// Place the Polygon2D at the scene origin, draw the polygon in world
// coordinates, and keep it OUT of the "fog_layer" group so FogDriver ignores it.
//
// LIT VARIANT. The blanket is a flat sheet of mist, so fragment() hands light()
// a flat camera-facing normal and light() tints the fog with the light instead
// of replacing it, mirroring GrassProcedural_PixelCrisp.gdshader: the unlit fog
// keeps exactly the color it had before, and the mist only picks up the color
// and falloff of the Light2Ds that reach it. Only the color is touched, so the
// density profile below is untouched and the fog keeps its tuned thickness.
// ``light_reaction`` scales the whole response, so a bright Light2D (the
// checkpoint lantern runs energy 7, the player's spots 8) can be dialled down
// without darkening the unlit mist.

shader_type canvas_item;
render_mode blend_mix;

//!-------------------------------------------- density
group_uniforms density;
uniform vec4 fog_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform float max_density : hint_range(0.0, 1.0) = 0.12;
uniform float near_base : hint_range(0.0, 1.0) = 0.0;

//!-------------------------------------------- motion
group_uniforms motion;
uniform float clump_scale : hint_range(0.0, 0.01) = 0.0016;
uniform float clump_speed : hint_range(0.0, 20.0) = 1.5;
uniform float clump_threshold : hint_range(0.0, 1.0) = 0.5;
uniform float clump_soft : hint_range(0.0, 0.4) = 0.16;
uniform vec2 wind_dir = vec2(-1.0, 0.0);
uniform float wind_speed : hint_range(0.0, 500.0) = 40.0;
uniform float noise_scale : hint_range(0.0, 0.05) = 0.006;
uniform float drift_speed : hint_range(0.0, 200.0) = 12.0;
uniform float warp_strength : hint_range(0.0, 2000.0) = 250.0;
uniform float warp_scale : hint_range(0.0, 0.01) = 0.003;

//!-------------------------------------------- lighting
group_uniforms lighting;
uniform float light_reaction : hint_range(0.0, 1.0) = 1.0;

// Layered sines of linear coordinates: seamless by construction (they wrap
// forever), so the fog shows no tiling seams while it moves. Returns 0..1.
float mushy(vec2 p, float t)
{
	float v = 0.0;
	v += sin(p.x * 1.0 + t * 0.7) * 0.23;
	v += sin(p.y * 1.37 + t * 1.1) * 0.21;
	v += sin((p.x + p.y) * 0.63 + t * -0.8) * 0.18;
	v += sin((p.x - p.y * 1.27) * 1.83 + t * 0.5 + 1.11) * 0.15;
	v += sin(dot(p, vec2(2.9, -1.7)) + t * -1.2) * 0.13;
	v += sin(dot(p, vec2(-2.2, 3.1)) + t * 0.3 + 2.2) * 0.10;
	return v * 0.5 + 0.5;
}

// World position of the fragment, computed in vertex() where VERTEX really is
// local space. This node sits at the scene origin with the polygon drawn in
// world coordinates, so local == world.
varying vec2 fog_world;

void vertex()
{
	fog_world = VERTEX;
}

// Coarse, wide field used to place separated fog banks. Few low-frequency
// sines keep it smooth and big, so a threshold turns it into a handful of
// soft-edged lobes instead of the even blanket. Returns 0..1.
float clump_field(vec2 p, float t)
{
	float v = 0.0;
	v += sin(p.x * 1.0 + t * 0.6);
	v += sin(p.y * 1.3 + t * -0.8);
	v += sin((p.x + p.y) * 0.7 + t * 0.4);
	v += sin((p.x - p.y) * 1.1 + t * -0.5);
	v += sin(dot(p, vec2(1.8, -1.2)) + t * 0.3);
	return v * 0.1 + 0.5;
}

void fragment()
{
	vec2 world = fog_world;

	// World-space translation: the wind offset is applied to the raw world
	// coordinate, so the whole clump (coarse shape AND interior detail) glides
	// across the map over time. It stays pinned to terrain coordinates, so it
	// never follows the camera; only TIME moves it.
	vec2 base = world + wind_dir * wind_speed * TIME;

	// The coarse field decides WHERE fog exists: inside a bank it is ~1,
	// in the gaps it drops to ~0, so there are clear holes between clumps.
	float clump_t = TIME * clump_speed * 0.05;
	// Warp the sample coordinate with the same rolling field before extracting
	// the bank shape, so each bank's edge gets curled and pulled like a cloud.
	vec2 warp;
	warp.x = (mushy(base * warp_scale + vec2(0.0, 3.7), clump_t) - 0.5) * warp_strength;
	warp.y = (mushy(base * warp_scale + vec2(5.1, 0.0), clump_t) - 0.5) * warp_strength;
	float coarse = clump_field((base + warp) * clump_scale, clump_t);
	float bank = smoothstep(clump_threshold - clump_soft, clump_threshold + clump_soft, coarse);

	// Fines ride along with the clump, roiling in place on top of the drift.
	float drift_t = TIME * drift_speed * 0.05;
	float interior = 0.35 + 0.65 * mushy(base * noise_scale, drift_t);

	float density = clamp(near_base + max_density * bank * interior, 0.0, 1.0);

	// Flat sheet of mist facing the camera: light() needs a valid normal to
	// shade against, and this is the only surface the fog has.
	NORMAL = vec3(0.0, 0.0, 1.0);

	COLOR = vec4(fog_color.rgb, density * fog_color.a);
}

// Tints the mist with the light that reaches it. The fog color is the base and
// the light only adds to it, so fog outside every light keeps the color the
// fragment() above produced. The density (COLOR.a) is deliberately left alone:
// the light changes how the fog looks, never how thick it is.
void light()
{
	vec3 n = normalize(NORMAL);
	vec3 l = normalize(LIGHT_DIRECTION);
	float ndotl = max(dot(n, l), 0.0);

	vec3 lit = LIGHT_COLOR.rgb * COLOR.rgb * LIGHT_ENERGY * ndotl * light_reaction;

	LIGHT = vec4(lit, LIGHT_COLOR.a);
}