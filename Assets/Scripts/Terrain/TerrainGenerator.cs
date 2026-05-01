using Minecraft.Core;
using UnityEngine;

namespace Minecraft.Terrain
{
    public static class TerrainGenerator
    {
        private const int SeaLevel = 64;
        private const int TerrainAmplitude = 48;
        private const int DirtThickness = 4;
        private const int BedrockMaxY = 4;

        public static void GenerateChunkData(Chunk chunk, int seed)
        {
            Vector3 origin = chunk.WorldOrigin;

            for (int z = 0; z < Chunk.Depth; z++)
            {
                for (int x = 0; x < Chunk.Width; x++)
                {
                    float worldX = origin.x + x;
                    float worldZ = origin.z + z;

                    float noiseVal = NoiseGenerator.OctavePerlin(worldX, worldZ, seed, 6, 0.5f, 0.004f);
                    int terrainHeight = Mathf.FloorToInt(noiseVal * TerrainAmplitude + SeaLevel);

                    for (int y = 0; y < Chunk.Height; y++)
                    {
                        int index = Chunk.Index3DTo1D(x, y, z);

                        if (y == 0)
                        {
                            chunk.Blocks[index] = new Block(BlockType.Bedrock);
                        }
                        else if (y <= BedrockMaxY)
                        {
                            chunk.Blocks[index] = new Block(BlockType.Stone);
                        }
                        else if (y < terrainHeight - DirtThickness)
                        {
                            chunk.Blocks[index] = new Block(BlockType.Stone);
                        }
                        else if (y < terrainHeight)
                        {
                            chunk.Blocks[index] = new Block(BlockType.Dirt);
                        }
                        else if (y == terrainHeight)
                        {
                            chunk.Blocks[index] = new Block(BlockType.Grass);
                        }
                        else
                        {
                            chunk.Blocks[index] = new Block(BlockType.Air);
                        }
                    }
                }
            }
        }
    }
}
