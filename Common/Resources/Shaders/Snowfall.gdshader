shader_type canvas_item;

// Adapted from Lumine's "Multilayer Snowfall Shader" (CC0, godotshaders.com).
// Base color is transparent so only the falling snow shows over the menu.
// A lightning flash group brightens the snow during strikes, matching Rain.

group_uniforms snow_appearance;
uniform float spread : hint_range(0.0, 1.5) = 0.5;
uniform float size : hint_range(0.01, 5.0) = 0.5;
uniform vec4 snow_color : source_color = vec4(1.0);
uniform float snow_transparency : hint_range(-0.5, 1.0) = 0.2;

group_uniforms snow_movement;
uniform float speed : hint_range(0.0, 10.0) = 0.5;
uniform float wind : hint_range(-2.0, 2.0) = 0.0;
uniform int num_of_layers = 40;

group_uniforms lightning;
uniform float flash_strength : hint_range(0.0, 1.0) = 0.0;
uniform float flash_glow : hint_range(0.0, 3.0) = 2.0;

const mat3 NOISE_MATRIX = mat3(
	vec3(13.323122, 23.5112, 21.71123),
	vec3(21.1212, 28.7312, 11.9312),
	vec3(21.8112, 14.7212, 61.3934)
);

// Generates the snowflake pattern for a single depth layer.
vec3 generate_snowflake(vec2 coord, float layer_index, float time, float wind_strength) {
	// Scale coordinates based on layer depth.
	float layer_scale = 1.0 + layer_index * 0.5 / (max(size, 0.01) * 2.0);
	vec2 scaled_coord = coord * layer_scale;

	// Apply movement (falling + wind). Wind is negated for the preview look.
	vec2 movement = vec2(
		scaled_coord.y * (spread * mod(layer_index * 7.238917, 1.0) - spread * 0.5) +
		(-wind_strength) * speed * time * 0.5,
		-speed * time / (1.0 + layer_index * 0.5 * 0.03)
	);
	vec2 final_coord = scaled_coord + movement;

	// Generate the noise pattern for this layer's cell.
	vec3 noise_input = vec3(floor(final_coord), 31.189 + layer_index);
	vec3 noise_val = floor(noise_input) * 0.00001 + fract(noise_input);
	vec3 random = fract((31415.9 + noise_val) / fract(NOISE_MATRIX * noise_val));

	// Snowflake shape.
	vec2 shape = abs(mod(final_coord, 1.0) - 0.5 + 0.9 * random.xy - 0.45);
	shape += 0.01 * abs(2.0 * fract(10.0 * final_coord.yx) - 1.0);

	// Edge softness drifts with depth so flakes fade in and out of focus.
	float depth_offset = 5.0 * sin(time * 0.1);
	float edge_softness = 0.005 + 0.05 * min(0.5 * abs(layer_index - 5.0 - depth_offset), 1.0);

	float shape_value = 0.6 * max(shape.x - shape.y, shape.x + shape.y) + max(shape.x, shape.y) - 0.01;

	return vec3(smoothstep(edge_softness, -edge_softness, shape_value) *
				(random.x / (1.0 + 0.02 * layer_index * 0.5)));
}

void fragment() {
	vec3 snow_accumulation = vec3(0.0);

	// Accumulate every depth layer.
	for (int i = 0; i < num_of_layers; i++) {
		snow_accumulation += generate_snowflake(UV, float(i), TIME, wind);
	}

	float snow_intensity = clamp(length(snow_accumulation), 0.0, 1.0);
	vec4 base_color = vec4(0.0); // Transparent: only the snow is drawn.

	// Transparency applied to the snow color.
	vec4 transparency_color = vec4(snow_color.rgb * (1.0 + snow_transparency), snow_intensity);

	COLOR = mix(base_color, transparency_color, snow_intensity);

	// Lightning brightens the falling snow.
	if (flash_strength > 0.0)
		COLOR.rgb += snow_color.rgb * flash_glow * flash_strength * snow_intensity;
}