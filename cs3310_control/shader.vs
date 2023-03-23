#version 330 core

layout(location = 0) in vec3 aPos;

out vec2 screenCoord;

void main()
{
    // NDC坐标在[-1,1]之间，转换为[0,1]之间
    screenCoord = (vec2(aPos.x, aPos.y) + 1.0) / 2.0;
    gl_Position = vec4(aPos, 1.0f);
}