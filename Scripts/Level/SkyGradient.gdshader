shader_type canvas_item;

uniform vec4 color_top : source_color = vec4(0.3, 0.5, 0.8, 1.0);
uniform vec4 color_bottom : source_color = vec4(0.7, 0.85, 0.95, 1.0);

void fragment() {
	COLOR = mix(color_bottom, color_top, UV.y);
}
