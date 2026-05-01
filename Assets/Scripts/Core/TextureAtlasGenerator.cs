using UnityEngine;

namespace Minecraft.Core
{
    public static class TextureAtlasGenerator
    {
        private const int AtlasSize = 256;
        private const int CellSize = 16;
        private const int CellsPerRow = 16;

        public static Texture2D Generate()
        {
            Texture2D atlas = new Texture2D(AtlasSize, AtlasSize, TextureFormat.RGBA32, false);
            atlas.filterMode = FilterMode.Point;
            atlas.wrapMode = TextureWrapMode.Clamp;

            Color[] pixels = new Color[AtlasSize * AtlasSize];
            System.Random rng = new System.Random(42);

            for (int blockId = 0; blockId < 256; blockId++)
            {
                Color baseColor = GetBlockColor(blockId);
                int cellX = (blockId % CellsPerRow) * CellSize;
                int cellY = (blockId / CellsPerRow) * CellSize;

                for (int y = 0; y < CellSize; y++)
                {
                    for (int x = 0; x < CellSize; x++)
                    {
                        Color pixel = baseColor;

                        float edgeFactor = 1f;
                        if (x == 0 || x == CellSize - 1 || y == 0 || y == CellSize - 1)
                            edgeFactor = 0.92f;

                        float noise = (float)(rng.NextDouble() * 0.04f - 0.02f);

                        pixel.r = Mathf.Clamp01(pixel.r * edgeFactor + noise);
                        pixel.g = Mathf.Clamp01(pixel.g * edgeFactor + noise);
                        pixel.b = Mathf.Clamp01(pixel.b * edgeFactor + noise);

                        int px = cellX + x;
                        int py = cellY + y;
                        pixels[py * AtlasSize + px] = pixel;
                    }
                }
            }

            atlas.SetPixels(pixels);
            atlas.Apply();
            return atlas;
        }

        private static Color GetBlockColor(int blockId)
        {
            switch (blockId)
            {
                case BlockType.Stone:   return new Color(0.55f, 0.55f, 0.50f);
                case BlockType.Dirt:    return new Color(0.60f, 0.50f, 0.38f);
                case BlockType.Grass:   return new Color(0.45f, 0.55f, 0.30f);
                case BlockType.Bedrock: return new Color(0.22f, 0.22f, 0.22f);
                case BlockType.Sand:    return new Color(0.78f, 0.72f, 0.60f);
                case BlockType.Wood:    return new Color(0.55f, 0.42f, 0.28f);
                case BlockType.Leaves:  return new Color(0.30f, 0.48f, 0.22f);
                case BlockType.Water:   return new Color(0.25f, 0.40f, 0.55f);
                case BlockType.Plank:   return new Color(0.68f, 0.55f, 0.38f);
                case BlockType.Glass:   return new Color(0.78f, 0.85f, 0.90f);
                default:                return new Color(0.90f, 0.30f, 0.90f);
            }
        }
    }
}
