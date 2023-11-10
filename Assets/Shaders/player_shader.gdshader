shader_type canvas_item;

uniform vec4 player_color : source_color;

void fragment() {
	vec4 texture_color = texture(TEXTURE, UV);

    COLOR = player_color * texture_color;
}
