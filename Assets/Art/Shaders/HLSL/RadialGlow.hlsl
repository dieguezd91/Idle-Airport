#ifndef RADIAL_GLOW_INCLUDED
#define RADIAL_GLOW_INCLUDED

inline float radial_glow(
    const float2 uv,
    const float2 center,
    const float radius,
    const float intensity,
    const float spikes,
    const float spike_sharpness,
    const float mask_radius,
    const float mask_sharpness)
{
    const float2 d = uv - center;
    const float dist = length(d);
    const float angle = atan2(d.y, d.x);

    const float star = pow(abs(cos(angle * spikes)), spike_sharpness);
    const float mask = pow(1.0 - smoothstep(0.0, mask_radius, dist), mask_sharpness);
    const float glow = ((radius / (dist + 0.0001)) * lerp(1.0, star, 0.5)) * mask;

    return saturate(glow * intensity);
}

#endif