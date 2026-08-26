shader_type canvas_item;
render_mode blend_add, unshaded;

//! Lens-flare light ball driven by Heart_Flare.cs. Attached to a FULLSCREEN ColorRect
//! with blend_add. Each frame the script feeds Flare_Pos (normalized screen UV) and
//! Flare_Strength; the fragment draws a bright ball plus horizontal/vertical cross
//! streaks there. Because output is additive and alpha==0 outside the flare, the
//! fullscreen rect never washes the rest of the HUD.

uniform vec2 Flare_Pos = vec2(0.5, 0.5);
uniform vec4 Flare_Color : source_color = vec4(1.0, 0.85, 0.95, 1.0);
uniform float Flare_Strength = 0.0;
uniform float Flare_Size : hint_range(0.001, 0.3) = 0.02;

void fragment() {
	vec2 d = SCREEN_UV - Flare_Pos;
	float dist = length(d);

	// Bright core ball (gaussian, soft halo).
	float ball = exp(-pow(dist / Flare_Size, 2.0));

	// Cross streaks (lens-flare spikes): thin across, long along.
	float streak_w = Flare_Size * 0.35;
	float streak_len = Flare_Size * 7.0;
	float h_streak = exp(-pow(d.y / streak_w, 2.0)) * exp(-pow(d.x / streak_len, 2.0));
	float v_streak = exp(-pow(d.x / streak_w, 2.0)) * exp(-pow(d.y / streak_len, 2.0));

	float intensity = (ball + h_streak + v_streak) * Flare_Strength;

	// blend_add -> screen += Flare_Color.rgb * intensity.
	COLOR.rgb = Flare_Color.rgb;
	COLOR.a = intensity;
}
