shader_type canvas_item;

// The exact colors currently on your sprite sheet
uniform vec4 color_1_original : source_color;
uniform vec4 color_1_replace : source_color;

uniform vec4 color_2_original : source_color;
uniform vec4 color_2_replace : source_color;

uniform vec4 color_3_original : source_color;
uniform vec4 color_3_replace : source_color;

// A tiny tolerance handles slight blending/compression artifacts
uniform float tolerance : hint_range(0.0, 1.0) = 0.01;

void fragment() {
    vec4 current_color = texture(TEXTURE, UV);

    // Swap Color 1 (Now applies the new Alpha value as well)
    if (distance(current_color.rgb, color_1_original.rgb) < tolerance && current_color.a > 0.0) {
        COLOR = color_1_replace;
    }
    // Swap Color 2 (Now applies the new Alpha value as well)
    else if (distance(current_color.rgb, color_2_original.rgb) < tolerance && current_color.a > 0.0) {
        COLOR = color_2_replace;
    }
	// Swap Color 2 (Now applies the new Alpha value as well)
    else if (distance(current_color.rgb, color_3_original.rgb) < tolerance && current_color.a > 0.0) {
        COLOR = color_3_replace;
    }
    // Keep everything else the same
    else {
        COLOR = current_color;
    }
}