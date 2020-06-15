#version 440

layout (location = 0) in vec3 pos;
layout (location = 1) in vec3 normal;
layout (location = 2) in vec4 color;
layout (location = 3) in vec2 texcoord;

out vec4 vertColor;

layout (location = 4) uniform mat4 modelView;
layout (location = 5) uniform mat4 projection;

void main()
{
    gl_Position = projection * modelView * vec4(pos, 1f);
    vertColor = color;
}
