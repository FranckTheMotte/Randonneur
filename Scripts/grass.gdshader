shader_type canvas_item;

uniform sampler2D grass_texture : source_color;
uniform float wind_speed = 1.5;
uniform float wind_amplitude = 1.0;

void vertex()
{
    float wind = sin(TIME * wind_speed + INSTANCE_CUSTOM.r * 10.0);
    VERTEX.x += wind * wind_amplitude * UV.y;
}

void fragment()
{
    COLOR = texture(grass_texture, UV);
}