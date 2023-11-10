shader_type canvas_item;

uniform vec4 tick_color : source_color;
uniform float tick_amount : hint_range(0.0, 1.0, 0.1);

void fragment() {
	vec4 texture_color = texture(TEXTURE, UV);
	
    COLOR.rgb = (texture_color * (1.0 - tick_amount) + tick_color * tick_amount).rgb;
	COLOR.a = texture_color.a;
}
