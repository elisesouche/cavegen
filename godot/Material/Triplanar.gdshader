shader_type spatial;

uniform sampler2D albedo[3];
uniform sampler2D normal_map[3];
uniform sampler2D arm[3];

varying vec2 tex_position_x;
varying vec2 tex_position_y;
varying vec2 tex_position_z;

void vertex() {
    tex_position_x = VERTEX.zy;
    tex_position_y = VERTEX.xz;
    tex_position_z = VERTEX.xy;
    if (NORMAL.x <  0.) tex_position_x.x *= -1.;
    if (NORMAL.y <  0.) tex_position_y.x *= -1.;
    if (NORMAL.z >= 0.) tex_position_z.x *= -1.;
    tex_position_x.y += 0.5;
    tex_position_z.x += 0.5;
}

vec3 get_projection_weights(in vec3 normal) {
    vec3 norm = abs(normal);
    return norm / (norm.x + norm.y + norm.z);
}

vec4 sample_triplanar(in sampler2D tex[3], in vec3 normal) {
    vec4 x = texture(tex[0], tex_position_x);
    vec4 y = texture(tex[1], tex_position_y);
    vec4 z = texture(tex[2], tex_position_z);
    vec3 weights = get_projection_weights(normal);
    return x * weights.x
         + y * weights.y
         + z * weights.z;
}

void fragment() {
    ALBEDO = sample_triplanar(albedo, NORMAL).rgb;
    NORMAL_MAP = sample_triplanar(normal_map, NORMAL).xyz;
    vec4 arm_ = sample_triplanar(arm, NORMAL);
    AO = arm_.r;
    ROUGHNESS = 1. - arm_.b;
    METALLIC = arm_.g;
}
