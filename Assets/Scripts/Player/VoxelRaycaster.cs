using System;
using Minecraft.Core;
using UnityEngine;

namespace Minecraft.Player
{
    public struct VoxelRaycastHit
    {
        public Vector3Int BlockPos;
        public Vector3Int FaceNormal;
        public Vector3 HitPoint;
        public float Distance;
        public bool HasHit;
    }

    public static class VoxelRaycaster
    {
        public static VoxelRaycastHit Raycast(Vector3 origin, Vector3 direction, float maxDistance,
            Func<int, int, int, Block> getBlock)
        {
            VoxelRaycastHit result = new VoxelRaycastHit { HasHit = false, Distance = maxDistance };

            int x = Mathf.FloorToInt(origin.x);
            int y = Mathf.FloorToInt(origin.y);
            int z = Mathf.FloorToInt(origin.z);

            int stepX = direction.x > 0f ? 1 : (direction.x < 0f ? -1 : 0);
            int stepY = direction.y > 0f ? 1 : (direction.y < 0f ? -1 : 0);
            int stepZ = direction.z > 0f ? 1 : (direction.z < 0f ? -1 : 0);

            float tDeltaX = stepX != 0 ? Mathf.Abs(1f / direction.x) : float.MaxValue;
            float tDeltaY = stepY != 0 ? Mathf.Abs(1f / direction.y) : float.MaxValue;
            float tDeltaZ = stepZ != 0 ? Mathf.Abs(1f / direction.z) : float.MaxValue;

            float tMaxX = stepX != 0
                ? (stepX > 0 ? (x + 1 - origin.x) : (origin.x - x)) * tDeltaX
                : float.MaxValue;
            float tMaxY = stepY != 0
                ? (stepY > 0 ? (y + 1 - origin.y) : (origin.y - y)) * tDeltaY
                : float.MaxValue;
            float tMaxZ = stepZ != 0
                ? (stepZ > 0 ? (z + 1 - origin.z) : (origin.z - z)) * tDeltaZ
                : float.MaxValue;

            float t = 0f;

            while (t < maxDistance)
            {
                Block block = getBlock(x, y, z);
                if (block.IsSolid)
                {
                    result.HasHit = true;
                    result.BlockPos = new Vector3Int(x, y, z);
                    result.Distance = t;

                    if (tMaxX < tMaxY && tMaxX < tMaxZ)
                        result.FaceNormal = new Vector3Int(-stepX, 0, 0);
                    else if (tMaxY < tMaxZ)
                        result.FaceNormal = new Vector3Int(0, -stepY, 0);
                    else
                        result.FaceNormal = new Vector3Int(0, 0, -stepZ);

                    result.HitPoint = origin + direction * t;
                    return result;
                }

                if (tMaxX < tMaxY)
                {
                    if (tMaxX < tMaxZ)
                    {
                        t = tMaxX;
                        tMaxX += tDeltaX;
                        x += stepX;
                    }
                    else
                    {
                        t = tMaxZ;
                        tMaxZ += tDeltaZ;
                        z += stepZ;
                    }
                }
                else
                {
                    if (tMaxY < tMaxZ)
                    {
                        t = tMaxY;
                        tMaxY += tDeltaY;
                        y += stepY;
                    }
                    else
                    {
                        t = tMaxZ;
                        tMaxZ += tDeltaZ;
                        z += stepZ;
                    }
                }
            }

            return result;
        }
    }
}
