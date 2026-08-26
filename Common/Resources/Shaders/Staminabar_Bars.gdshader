shader_type canvas_item;

// Bar configuration
uniform int Bar_Count : hint_range(1, 200) = 25;
uniform float Gap_Width : hint_range(0.001, 0.05) = 0.005;
uniform float Outer_Gap_X : hint_range(0.0, 0.1) = 0.01;
uniform float Outer_Gap_Y : hint_range(0.0, 0.5) = 0.2;

// Fog
uniform sampler2D Fog_Texture : filter_linear_mipmap, repeat_enable;
uniform float Fog_Scale : hint_range(1.0, 10.0) = 6.0;
uniform float Fog_Speed : hint_range(0.0, 1.0) = 0.15;
uniform float Fog_Intensity : hint_range(0.0, 5.0) = 1.0;

// Glow
uniform float Glow_Intensity : hint_range(0.0, 1.0) = 0.3;

// Colors
uniform vec4 Full_Color : source_color = vec4(0.94, 1.0, 0.0, 1.0);
uniform vec4 Empty_Color : source_color = vec4(0.16, 0.16, 0.16, 1.0);
uniform vec4 Glow_Color : source_color = vec4(1.0, 1.0, 0.8, 1.0);

// Progress driven by C# (0.0 - 1.0)
uniform float Progress : hint_range(0.0, 1.0) = 1.0;

void fragment() {
	bool in_gap = UV.x < Outer_Gap_X || UV.x > 1.0 - Outer_Gap_X;
	in_gap = in_gap || UV.y < Outer_Gap_Y || UV.y > 1.0 - Outer_Gap_Y;

	// Remap UV.x into the padded region
	float padded_x = (UV.x - Outer_Gap_X) / (1.0 - Outer_Gap_X * 2.0);
	padded_x = clamp(padded_x, 0.0, 1.0);

	float segment_width = 1.0 / float(Bar_Count);
	float half_gap = Gap_Width * 0.5;

	// Which segment is this pixel in?
	float seg_f = floor(padded_x / segment_width);
	float local_x = padded_x - seg_f * segment_width;

	// In the horizontal gap between bars?
	in_gap = in_gap || local_x < half_gap || local_x >= segment_width - half_gap;

	// Bar-local X normalized to 0..1 (excluding gap)
	float bar_x = (local_x - half_gap) / (segment_width - Gap_Width);

	// Should this bar be filled?
	float threshold = (seg_f + 1.0) / float(Bar_Count);
	bool filled = Progress >= threshold;

	// Fog texture with horizontal scroll
	vec2 fog_uv = vec2(padded_x * Fog_Scale + TIME * Fog_Speed, UV.y * Fog_Scale);
	float fog = texture(Fog_Texture, fog_uv).r;

	vec3 color;
	float alpha = 1.0;

	if (in_gap) {
		alpha = 0.0;
	} else if (filled) {
		color = Full_Color.rgb;
		float fog_mod = Fog_Intensity <= 1.0 ? mix(1.0, fog, Fog_Intensity) : pow(fog, Fog_Intensity);
		color *= fog_mod;
		float glow = exp(-pow((bar_x - 0.5) * 3.0, 2.0)) * Glow_Intensity;
		color += Glow_Color.rgb * glow;
	} else {
		color = Empty_Color.rgb;
	}

	COLOR = vec4(color, alpha);
}
