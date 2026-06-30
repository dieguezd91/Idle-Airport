#ifndef TILED_GRID_INCLUDED
#define TILED_GRID_INCLUDED

inline float4 tiled_grid(
    const float2 uv,
    const float tile_scale,
    const float3 color_a,
    const float3 color_b,
    const float3 border_color,
    const float border_thickness)
{
    const float2 scaled_uv = uv * tile_scale;
    const float2 tile_id = floor(scaled_uv);
    const float2 local_uv = frac(scaled_uv);

    // Hash the tile ID to get a random value
    const float hash = frac(sin(dot(tile_id, float2(127.1, 311.7))) * 43758.5453);

    const float3 tile_color = lerp(color_a, color_b, hash);

    // Border, its the dist from edge in the local uv
    const float2 border_dist = min(local_uv, 1.0 - local_uv);
    const float min_dist = min(border_dist.x, border_dist.y);

    const float border_mask = step(min_dist, border_thickness);
    const float3 result = lerp(tile_color, border_color, border_mask);

    return float4(result.x, result.y, result.z, 1);
}

#endif