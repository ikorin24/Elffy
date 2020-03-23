#version 300 es
precision highp float;

layout (std140) uniform material {
    vec4 base;
} color;

out vec4 outColor;

void main(){
    outColor = color.base;
}

/*
void main(void)
{
    gl_FragColor = vec4(0.8, 0.1, 0.1, 1.0);
}
*/
