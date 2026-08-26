shader_type canvas_item;
render_mode blend_add, unshaded;

//! Metal-glint version of the heart monitor line, matching the Armor Bar:
//! a soft band periodically sweeps RIGHT -> LEFT across the line (fully
//! entering and exiting off-screen each cycle, so the reset never snaps).
//! Replaces the old scrolling sine fade chain and the wrapping pulse head.
//! The >1.0 bloom boost is kept; tremor and flash are unchanged. Colors are
//! pushed well past 1.0 (HDR) under the glint so the scene's WorldEnvironment
//! glow (bloom) catches them and makes the line blow.

// Full-line crossings per second. Driven by C#: faster at low health.
uniform float Glint_Speed : hint_range(0.0, 4.0) = 0.35;

// Horizontal span of the trace in local pixels (set by C# every frame).
uniform float Line_Length : hint_range(1.0, 4096.0) = 792.0;

// Seconds between glint passes (a pass always finishes first).
uniform float Glint_Interval : hint_range(0.5, 10.0) = 3.0;

// Peak brightness of the glint band above the base line.
uniform float Glint_Intensity : hint_range(0.0, 2.0) = 1.0;

// Brightness of the line outside the glint, so it never fully vanishes.
uniform float Base_Brightness : hint_range(0.0, 1.0) = 0.25;
uniform float Glow_Strength : hint_range(0.0, 20.0) = 4.0;
uniform float Bloom_Threshold : hint_range(0.0, 1.0) = 0.35;

// Tremor effect driven by C# on damage.
uniform float Tremor_Amount : hint_range(0.0, 20.0) = 0.0;

// White flash driven by C# on damage.
uniform float Flash_Strength : hint_range(0.0, 5.0) = 0.0;
uniform vec4 Flash_Color : source_color = vec4(1.0, 0.4, 0.7, 1.0);

void fragment() {
	// Tremor shake on the local-space position (pixel units).
	vec2 v = VERTEX;
	if (Tremor_Amount > 0.0) {
		v.x += sin(TIME * 137.0) * Tremor_Amount * 0.003 * Line_Length;
		v.y += cos(TIME * 113.0) * Tremor_Amount * 0.003 * Line_Length;
	}

	// Normalized horizontal position 0..1 along the trace. Computed from the
	// local-space X instead of UV, because Line2D UVs follow arc length — the
	// scrolling ECG spikes would make a UV-based glint stall and slide back.
	float xn = clamp(v.x / max(Line_Length, 1e-5), -0.5, 1.5);

	// The band sweeps across the line once per Glint_Interval seconds. It
	// starts fully off the right edge and exits fully past the left edge
	// before the cycle resets, so the jump happens entirely off-screen.
	float span = 1.4;
	float travel = span / max(Glint_Speed, 1e-3);
	float interval = max(Glint_Interval, travel);
	float cycle = mod(TIME, interval);

	// Right -> left: center slides from +1.2 down to -0.2.
	float center = 1.2 - cycle * Glint_Speed;

	// Same Gaussian band shape as the Armor Bar's glint.
	float sweep = xn - center;
	float band = exp(-pow(sweep * 2.0, 2.0) * 40.0);

	float fade = Base_Brightness + band * Glint_Intensity;

	// Only the bright parts of the glint blow past 1.0 (cubed curve, so the
	// peaks pop extremely hard while the base of the line stays near 1.0 /
	// under the WorldEnvironment's glow_hdr_threshold). This makes it bloom.
	float blow = clamp((fade - Bloom_Threshold) / max(1.0 - Bloom_Threshold, 1e-5), 0.0, 1.0);
	blow = blow * blow * blow;

	// HDR boost -> bloom.
	COLOR.rgb *= 1.0 + Glow_Strength * blow;

	// White flash on damage.
	COLOR.rgb += Flash_Color.rgb * Flash_Strength;

	// Boost alpha during flash so the white is fully visible.
	float flash_alpha = clamp(Flash_Strength, 0.0, 1.0);
	COLOR.a = mix(clamp(fade, 0.0, 1.0), 1.0, flash_alpha);
}
