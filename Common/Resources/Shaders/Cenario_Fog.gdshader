// Silent Hill style distance fog for 2D cenarios.
// Screen-space shader: each fragment computes its WORLD position from the
// camera (uniforms pushed every frame by FogDriver.cs), builds a density
// profile that is 0 inside a clear "pocket" around the camera and ramps to a
// thick wall in the distance, then roils that profile with three sine fields
// (fine drift + wide billow + slow swell) so the fog visibly moves.
// The noise is expressed in world space, so banks of mist stay pinned to the
// terrain while the density wall follows the camera.
// The scene-fixed, non-camera blanket fog lives in Cenario_SceneFog.gdshader.

shader_type canvas_item;
render_mode blend_mix, unshaded;

//!-------------------------------------------- camera (pushed by FogDriver)
group_uniforms camera;
uniform vec2 cam_pos = vec2(0.0, 0.0);
uniform float cam_zoom = 0.25;
uniform vec2 viewport_size = vec2(2560.0, 1080.0);

//!-------------------------------------------- density profile
group_uniforms density;
uniform vec4 fog_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform float max_density : hint_range(0.0, 1.0) = 1.0;
uniform float clear_radius : hint_range(0.0, 6000.0) = 2200.0;
uniform float ramp_distance : hint_range(0.0, 6000.0) = 2000.0;
uniform float near_base : hint_range(0.0, 1.0) = 0.0;

//!-------------------------------------------- motion
group_uniforms motion;
uniform vec2 wind_dir = vec2(-0.25, 0.1);
uniform float noise_scale : hint_range(0.0, 0.05) = 0.006;
uniform float drift_speed : hint_range(0.0, 200.0) = 12.0;
uniform float billow_scale : hint_range(0.0, 0.05) = 0.0015;
uniform float billow_speed : hint_range(0.0, 200.0) = 4.0;
uniform float swell_scale : hint_range(0.0, 0.01) = 0.0008;
uniform float swell_speed : hint_range(0.0, 20.0) = 1.5;

// Layered sines of linear coordinates: seamless by construction (they wrap
// forever), so the fog shows no tiling seams while it drifts. Returns 0..1.
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

void fragment()
{
	vec2 visible_world = viewport_size / vec2(max(cam_zoom, 0.001));
	vec2 world = cam_pos + (SCREEN_UV - vec2(0.5)) * visible_world;
	float dist = distance(world, cam_pos);

	float wall = smoothstep(clear_radius, clear_radius + ramp_distance, dist);

	float drift_t = TIME * drift_speed * 0.05;
	float billow_t = TIME * billow_speed * 0.02;
	float swell_t = TIME * swell_speed * 0.05;

	float drift = mushy(world * noise_scale + wind_dir * drift_t * 0.6, drift_t);
	float billow = mushy(world * billow_scale + wind_dir * billow_t * 0.4, billow_t);
	float swell = mushy(world * swell_scale - wind_dir * swell_t * 0.3, swell_t);

	float noise_field = clamp(0.72 + (drift - 0.5) * 0.55 + (billow - 0.5) * 0.42 + (swell - 0.5) * 0.3, 0.15, 1.2);

	float density = near_base + wall * max_density * noise_field;
	density = clamp(density, 0.0, 1.0);

	COLOR = vec4(fog_color.rgb, density * fog_color.a);
}