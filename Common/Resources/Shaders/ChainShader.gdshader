shader_type canvas_item;

// The chain sprite sampled by the shader (Chain.png).
uniform sampler2D Chain_Texture : source_color;

// Region cropped from the sprite by the stylebox.
uniform vec2 Region_Offset = vec2(488.0, 0.0);
uniform vec2 Region_Size = vec2(2082.0, 350.0);
uniform vec2 Texture_Size = vec2(2917.0, 350.0);

// Tint applied by the stylebox modulate_color.
uniform vec4 Chain_Tint : source_color = vec4(0.6328125, 0.5331808, 0.4734017, 1.0);

// Inverted scroll position of the active inventory list (0..1), driven by the chain controller.
uniform float Scroll_Pos : hint_range(0.0, 1.0) = 0.0;

// How far the chain travels along the strip for a full scroll.
uniform float Scroll_Distance : hint_range(0.0, 2.0) = 0.9;

void fragment()
{
	vec2 region = Region_Size / Texture_Size;
	vec2 offset = Region_Offset / Texture_Size;

	// Tile the region across the strip and wrap it, so the motion is endless.
	float tiles = 1.0 / region.x;
	float x = fract(UV.x * tiles - Scroll_Pos * Scroll_Distance);

	vec2 chain_uv = vec2(x * region.x + offset.x, UV.y * region.y + offset.y);

	COLOR = texture(Chain_Texture, chain_uv) * Chain_Tint;
}