shader_type canvas_item;

// Metal glint
uniform float Glint_Speed : hint_range(0.0, 4.0) = 0.35;
uniform float Glint_Intensity : hint_range(0.0, 1.0) = 0.10;
uniform float Glint_Angle : hint_range(0.0, 360.0) = 17.0;
uniform float Glint_Interval : hint_range(0.5, 10.0) = 3.0;
uniform vec4 Glint_Color : source_color = vec4(0.85, 0.9, 1.0, 1.0);
uniform float Steel_Glow : hint_range(0.0, 1.0) = 0.06;
uniform float Glint_Enabled : hint_range(0.0, 1.0) = 1.0;

// Surface detail
uniform float Scratch_Amount : hint_range(0.0, 1.0) = 0.4;
uniform float Edge_Wear : hint_range(0.0, 1.0) = 0.5;

// Damage flash driven by the armor bar script.
uniform vec4 Flash_Color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform float Flash_Intensity : hint_range(0.0, 2.0) = 0.6;
uniform float Flash = 0.0;

float hash1(float n)
{
	return fract(sin(n * 127.1) * 43758.5453123);
}

// Faint horizontal scratches worn into the plate.
float Scratch_Mask(vec2 p, float aspect, float seed)
{
	float s = 0.0;

	for (int k = 0; k < 7; k++)
	{
		float y = hash1(seed * 7.1 + float(k) * 1.7);
		float x0 = hash1(seed * 3.3 + float(k) * 2.9 + 0.5);
		float len = 0.06 + hash1(seed * 9.7 + float(k) * 3.7) * 0.3;

		float dY = abs(p.y - y * aspect);
		float along = clamp((p.x - x0) / max(len, 1e-5), 0.0, 1.0);

		s = max(s, (1.0 - smoothstep(0.0, 0.0035, dY)) * along);
	}

	return s;
}

void fragment()
{
	// ---------- base plate ----------

	// COLOR already includes the texture multiplied by the CanvasItem's Modulate and
	// Self Modulate (MODULATE itself was removed in Godot 4), so starting from it keeps
	// those Inspector values working.
	vec4 base = COLOR;
	COLOR = base;

	// ---------- scratches + edge wear ----------

	float scratch = Scratch_Mask(UV, 1.0, 4.0);
	COLOR.rgb *= 1.0 - scratch * Scratch_Amount * 0.35;
	COLOR.rgb += vec3(0.12) * scratch * Scratch_Amount;

	vec2 px = UV / TEXTURE_PIXEL_SIZE;
	float edge_dist = min(min(px.x, px.y), min(1.0 / TEXTURE_PIXEL_SIZE.x - px.x, 1.0 / TEXTURE_PIXEL_SIZE.y - px.y));
	float wear = (1.0 - smoothstep(0.0, 4.0, edge_dist)) * Edge_Wear;
	COLOR.rgb *= 1.0 - wear * 0.35;
	COLOR.rgb += vec3(0.30, 0.33, 0.42) * (1.0 - smoothstep(0.0, 1.2, edge_dist)) * Edge_Wear * 0.4;

	// ---------- metal glint / steel glow ----------

	float ang = radians(Glint_Angle);
	vec2 glint_dir = vec2(cos(ang), sin(ang)) * 0.5;

	// The band sweeps across the bar once per Glint_Interval seconds. It
	// fades in at the left edge, crosses, and FULLY exits past the far edge
	// (dot_max) before the cycle ends — so the reset jump happens entirely
	// off-screen and nothing is ever cut or snapped on screen.
	float dot_max = abs(glint_dir.x) + abs(glint_dir.y);
	float span = dot_max + 0.4;
	float travel = span / max(Glint_Speed, 1e-3);
	float interval = max(Glint_Interval, travel);
	float cycle = mod(TIME, interval);
	float center = cycle * Glint_Speed - 0.2;
	float sweep = dot(UV, glint_dir) - center;
	float band = exp(-pow(sweep * 2.0, 2.0) * 40.0);
	float sheen = band * Glint_Intensity;

	float glow = exp(-pow((UV.y - 0.5) * 4.0, 2.0)) * Steel_Glow;

	COLOR.rgb += Glint_Color.rgb * (sheen + glow) * Glint_Enabled;

	// ---------- damage flash ----------

	COLOR.rgb += Flash_Color.rgb * Flash * Flash_Intensity;
}
