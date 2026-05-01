using UnityEngine;

namespace Minecraft.Core
{
    public class Chunk
    {
        public const int Width = 16;
        public const int Height = 256;
        public const int Depth = 16;
        public const int TotalBlocks = Width * Height * Depth;

        public Vector2Int Coord;
        public Block[] Blocks;
        public GameObject GameObject;
        public MeshFilter MeshFilter;
        public MeshRenderer MeshRenderer;
        public MeshCollider MeshCollider;
        public bool IsDirty;
        public bool IsGenerated;

        public Chunk(Vector2Int coord)
        {
            Coord = coord;
            Blocks = new Block[TotalBlocks];
            IsDirty = true;
            IsGenerated = false;
        }

        public static int Index3DTo1D(int x, int y, int z)
        {
            return x + y * Width + z * Width * Height;
        }

        public Block GetBlock(int x, int y, int z)
        {
            return Blocks[Index3DTo1D(x, y, z)];
        }

        public void SetBlock(int x, int y, int z, Block block)
        {
            Blocks[Index3DTo1D(x, y, z)] = block;
            IsDirty = true;
        }

        public Vector3 WorldOrigin
        {
            get { return new Vector3(Coord.x * Width, 0, Coord.y * Depth); }
        }
    }
}
