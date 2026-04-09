shader_type canvas_item;

uniform vec4 top_color : source_color = vec4(0.157, 0.333, 0.627, 1.0);
uniform vec4 bottom_color : source_color = vec4(0.310, 0.702, 0.961, 1.0);

void fragment() {
    COLOR = mix(bottom_color, top_color, UV.y);
}
