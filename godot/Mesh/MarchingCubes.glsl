#[compute]
#version 450

layout(local_size_x = 10, local_size_y = 10, local_size_z = 10) in;

const int offsets[] = {0, 0, 3, 6, 12, 15, 21, 27, 36, 39, 45, 51, 60, 66, 75, 84, 90, 93, 99, 105, 114, 120, 129, 138, 150, 156, 165, 174, 186, 195, 207, 219, 228, 231, 237, 243, 252, 258, 267, 276, 288, 294, 303, 312, 324, 333, 345, 357, 366, 372, 381, 390, 396, 405, 417, 429, 438, 447, 459, 471, 480, 492, 507, 522, 528, 531, 537, 543, 552, 558, 567, 576, 588, 594, 603, 612, 624, 633, 645, 657, 666, 672, 681, 690, 702, 711, 723, 735, 750, 759, 771, 783, 798, 810, 825, 840, 852, 858, 867, 876, 888, 897, 909, 915, 924, 933, 945, 957, 972, 984, 999, 1008, 1014, 1023, 1035, 1047, 1056, 1068, 1083, 1092, 1098, 1110, 1125, 1140, 1152, 1167, 1173, 1185, 1188, 1191, 1197, 1203, 1212, 1218, 1227, 1236, 1248, 1254, 1263, 1272, 1284, 1293, 1305, 1317, 1326, 1332, 1341, 1350, 1362, 1371, 1383, 1395, 1410, 1419, 1425, 1437, 1446, 1458, 1467, 1482, 1488, 1494, 1503, 1512, 1524, 1533, 1545, 1557, 1572, 1581, 1593, 1605, 1620, 1632, 1647, 1662, 1674, 1683, 1695, 1707, 1716, 1728, 1743, 1758, 1770, 1782, 1791, 1806, 1812, 1827, 1839, 1845, 1848, 1854, 1863, 1872, 1884, 1893, 1905, 1917, 1932, 1941, 1953, 1965, 1980, 1986, 1995, 2004, 2010, 2019, 2031, 2043, 2058, 2070, 2085, 2100, 2106, 2118, 2127, 2142, 2154, 2163, 2169, 2181, 2184, 2193, 2205, 2217, 2232, 2244, 2259, 2268, 2280, 2292, 2307, 2322, 2328, 2337, 2349, 2355, 2358, 2364, 2373, 2382, 2388, 2397, 2409, 2415, 2418, 2427, 2433, 2445, 2448, 2454, 2457, 2460 };
const int lengths[] = {0, 3, 3, 6, 3, 6, 6, 9, 3, 6, 6, 9, 6, 9, 9, 6, 3, 6, 6, 9, 6, 9, 9, 12, 6, 9, 9, 12, 9, 12, 12, 9, 3, 6, 6, 9, 6, 9, 9, 12, 6, 9, 9, 12, 9, 12, 12, 9, 6, 9, 9, 6, 9, 12, 12, 9, 9, 12, 12, 9, 12, 15, 15, 6, 3, 6, 6, 9, 6, 9, 9, 12, 6, 9, 9, 12, 9, 12, 12, 9, 6, 9, 9, 12, 9, 12, 12, 15, 9, 12, 12, 15, 12, 15, 15, 12, 6, 9, 9, 12, 9, 12, 6, 9, 9, 12, 12, 15, 12, 15, 9, 6, 9, 12, 12, 9, 12, 15, 9, 6, 12, 15, 15, 12, 15, 6, 12, 3, 3, 6, 6, 9, 6, 9, 9, 12, 6, 9, 9, 12, 9, 12, 12, 9, 6, 9, 9, 12, 9, 12, 12, 15, 9, 6, 12, 9, 12, 9, 15, 6, 6, 9, 9, 12, 9, 12, 12, 15, 9, 12, 12, 15, 12, 15, 15, 12, 9, 12, 12, 9, 12, 15, 15, 12, 12, 9, 15, 6, 15, 12, 6, 3, 6, 9, 9, 12, 9, 12, 12, 15, 9, 12, 12, 15, 6, 9, 9, 6, 9, 12, 12, 15, 12, 15, 15, 6, 12, 9, 15, 12, 9, 6, 12, 3, 9, 12, 12, 15, 12, 15, 9, 12, 12, 15, 15, 6, 9, 12, 6, 3, 6, 9, 9, 6, 9, 12, 6, 3, 9, 6, 12, 3, 6, 3, 3, 0 };
const int cornerIndexAFromEdge[] = { 0, 1, 2, 3, 4, 5, 6, 7, 0, 1, 2, 3 };
const int cornerIndexBFromEdge[] = { 1, 2, 3, 0, 5, 6, 7, 4, 4, 5, 6, 7 };

struct Triangle {
    vec4 a, b, c;
    vec4 norm;
};

layout(set = 0, binding = 0, std430) restrict readonly buffer LUTData {
    int[] LUT;
};

layout(set = 0, binding = 1, std430) restrict buffer AreaData {
    int VoxelResX;
    int VoxelResY;
    int VoxelResZ;
    float VoxelSize;
    float LocalPositionX;
    float LocalPositionY;
    float LocalPositionZ;
};
layout(set = 0, binding = 2, std430) restrict buffer VoxelValueData {
    float VoxelValue[];
};

layout(set = 0, binding = 3, std430) coherent buffer Counter {
    uint TriangleCounter;
};

layout(set = 0, binding = 4, std430) restrict buffer MeshData {
    Triangle Triangles[];
};

const float iso_level = 0;

ivec3 getVoxelRes() { return ivec3(VoxelResX, VoxelResY, VoxelResZ); }
vec3 getLocalPosition() { return vec3(LocalPositionX, LocalPositionY, LocalPositionZ); }
vec3 getWorldSize() { return VoxelSize * vec3(getVoxelRes()); }
vec3 getCenterOffset() { return 0.5 * getWorldSize(); }

vec4 interpolate_verts(vec4 v1, vec4 v2) {
    float t = (iso_level - v1.w) / (v2.w - v1.w);
    return v1 + t * (v2 - v1);
}

vec3 world_of_voxel(ivec3 voxel) {
    vec3 offset =
        vec3((voxel.x + 0.5) * VoxelSize,
             (voxel.y + 0.5) * VoxelSize,
             (voxel.z + 0.5) * VoxelSize)
        - getCenterOffset();
    return getLocalPosition() + offset;
}

ivec3 voxel_of_world(vec3 world) {
    vec3 offset = world - getLocalPosition() + getCenterOffset();
    ivec3 result = ivec3(offset / VoxelSize - vec3(0.5, 0.5, 0.5));
    return result;
}

float get_voxel_value(ivec3 voxel) {
    ivec3 res = getVoxelRes();
    return VoxelValue[voxel.x * res.y * res.z + voxel.y * res.z + voxel.z];
}

vec4 evaluate(ivec3 voxel) {
    vec3 pos = world_of_voxel(voxel);
    float value = get_voxel_value(voxel);
    return vec4(pos, value);
}
vec4 evaluate(int x, int y, int z) {
    return evaluate(ivec3(x, y, z));
}
vec4 evaluate(uint x, uint y, uint z) {
    return evaluate(ivec3(x, y, z));
}

void ProcessVoxel(uvec3 coord) {
    ivec3 res = getVoxelRes();
    // Bounds check for over-dispatched invocations
    if (coord.x >= res.x || coord.y >= res.y || coord.z >= res.z) {
        return;
    }
    if (coord.x + 1 == res.x ||
        coord.y + 1 == res.y ||
        coord.z + 1 == res.z) {
        return;
    }
    vec4 cube_corners[8] = {
        evaluate(coord.x + 0, coord.y + 0, coord.z + 0),
        evaluate(coord.x + 1, coord.y + 0, coord.z + 0),
        evaluate(coord.x + 1, coord.y + 0, coord.z + 1),
        evaluate(coord.x + 0, coord.y + 0, coord.z + 1),
        evaluate(coord.x + 0, coord.y + 1, coord.z + 0),
        evaluate(coord.x + 1, coord.y + 1, coord.z + 0),
        evaluate(coord.x + 1, coord.y + 1, coord.z + 1),
        evaluate(coord.x + 0, coord.y + 1, coord.z + 1),
    };
    uint cube_index = 0;
    if (cube_corners[0].w < iso_level) cube_index |= 1;
    if (cube_corners[1].w < iso_level) cube_index |= 2;
    if (cube_corners[2].w < iso_level) cube_index |= 4;
    if (cube_corners[3].w < iso_level) cube_index |= 8;
    if (cube_corners[4].w < iso_level) cube_index |= 16;
    if (cube_corners[5].w < iso_level) cube_index |= 32;
    if (cube_corners[6].w < iso_level) cube_index |= 64;
    if (cube_corners[7].w < iso_level) cube_index |= 128;
    int num_indices = lengths[cube_index];
    int offset = offsets[cube_index];

    for (int i = 0; i < num_indices; i += 3) {
        // Get indices of corner points A and B for each of the three edges
        // of the cube that need to be joined to form the triangle.
        int v0 = LUT[offset + i];
        int v1 = LUT[offset + 1 + i];
        int v2 = LUT[offset + 2 + i];

        int a0 = cornerIndexAFromEdge[v0];
        int b0 = cornerIndexBFromEdge[v0];
        int a1 = cornerIndexAFromEdge[v1];
        int b1 = cornerIndexBFromEdge[v1];
        int a2 = cornerIndexAFromEdge[v2];
        int b2 = cornerIndexBFromEdge[v2];

        // Calculate vertex positions
        Triangle triangle;
        triangle.a = interpolate_verts(cube_corners[a0], cube_corners[b0]);
        triangle.b = interpolate_verts(cube_corners[a1], cube_corners[b1]);
        triangle.c = interpolate_verts(cube_corners[a2], cube_corners[b2]);

        vec3 ab = triangle.b.xyz - triangle.a.xyz;
        vec3 ac = triangle.c.xyz - triangle.a.xyz;
        vec3 normal = normalize(cross(ab, ac));
        triangle.norm = vec4(normal, 0);

        uint index = atomicAdd(TriangleCounter, 1);
        Triangles[index] = triangle;
    }
}


void main() {
  ProcessVoxel(gl_GlobalInvocationID);
}
