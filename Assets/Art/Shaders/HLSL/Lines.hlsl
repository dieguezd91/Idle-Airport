#ifndef LINES_INCLUDED
#define LINES_INCLUDED

inline float scrolling_lines(
    float2 uv, 
    const float2 offset,
    const float spacing,
    const float thickness, 
    const float speed, 
    const float time)
{
    uv = uv - offset;
    uv.x += time * speed;
    const float dist = abs(frac(uv.x / spacing) - 0.5) * spacing;
    return 1.0 - smoothstep(0.0, thickness, dist);
}

#endif