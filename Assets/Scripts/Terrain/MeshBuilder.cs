using System;
using System.Collections.Generic;
using Minecraft.Core;
using UnityEngine;

namespace Minecraft.Terrain
{
    public struct ChunkMeshData
    {
        public Vector3[] Vertices;
        public int[] Triangles;
        public Vector2[] UVs;
        public Vector3[] Normals;
    }

    public static class MeshBuilder
    {
        private static readonly Vector3Int[] FaceNormals =
        {
            new Vector3Int(1, 0, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(0, 0, 1),
        };

        private const float AtlasCellSize = 1f / 16f;

        public static ChunkMeshData BuildGreedyMesh(Chunk chunk, Func<int, int, int, Block> getWorldBlock)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector2> uvs = new List<Vector2>();
            List<Vector3> normals = new List<Vector3>();

            int[] axisSize = { Chunk.Width, Chunk.Height, Chunk.Depth };

            for (int d = 0; d < 3; d++)
            {
                int u = (d + 1) % 3;
                int v = (d + 2) % 3;

                int sizeD = axisSize[d];
                int sizeU = axisSize[u];
                int sizeV = axisSize[v];

                int[] mask = new int[sizeU * sizeV];
                int[] worldPos = new int[3];
                Vector3 chunkOrigin = chunk.WorldOrigin;

                for (int layer = -1; layer < sizeD; layer++)
                {
                    int n = 0;
                    worldPos[d] = layer;

                    for (int j = 0; j < sizeV; j++)
                    {
                        for (int i = 0; i < sizeU; i++)
                        {
                            worldPos[u] = i;
                            worldPos[v] = j;

                            Block blockHere = GetBlockAt(chunk, chunkOrigin, worldPos, getWorldBlock);

                            worldPos[d] = layer + 1;
                            Block blockNext = GetBlockAt(chunk, chunkOrigin, worldPos, getWorldBlock);
                            worldPos[d] = layer;

                            if (blockHere.IsSolid && !blockNext.IsSolid)
                                mask[n] = 1;
                            else if (!blockHere.IsSolid && blockNext.IsSolid)
                                mask[n] = 2;
                            else
                                mask[n] = 0;

                            n++;
                        }
                    }

                    for (int j = 0; j < sizeV; j++)
                    {
                        for (int i = 0; i < sizeU;)
                        {
                            int maskVal = mask[j * sizeU + i];
                            if (maskVal == 0)
                            {
                                i++;
                                continue;
                            }

                            int w;
                            for (w = 1; i + w < sizeU && mask[j * sizeU + i + w] == maskVal; w++) ;

                            int h;
                            bool done = false;
                            for (h = 1; j + h < sizeV && !done; h++)
                            {
                                for (int k = 0; k < w; k++)
                                {
                                    if (mask[(j + h) * sizeU + i + k] != maskVal)
                                    {
                                        done = true;
                                        break;
                                    }
                                }
                            }
                            if (done) h--;

                            worldPos[d] = layer + 1;
                            worldPos[u] = i;
                            worldPos[v] = j;
                            ushort blockId = GetBlockAt(chunk, chunkOrigin, worldPos, getWorldBlock).id;

                            AddQuad(vertices, triangles, uvs, normals,
                                d, maskVal == 2, layer + 1, i, j, w, h, blockId);

                            for (int l = 0; l < h; l++)
                                for (int k = 0; k < w; k++)
                                    mask[(j + l) * sizeU + i + k] = 0;

                            i += w;
                        }
                    }
                }
            }

            return new ChunkMeshData
            {
                Vertices = vertices.ToArray(),
                Triangles = triangles.ToArray(),
                UVs = uvs.ToArray(),
                Normals = normals.ToArray(),
            };
        }

        private static Block GetBlockAt(Chunk chunk, Vector3 chunkOrigin, int[] worldPos, Func<int, int, int, Block> getWorldBlock)
        {
            int wx = (int)chunkOrigin.x + worldPos[0];
            int wy = worldPos[1];
            int wz = (int)chunkOrigin.z + worldPos[2];
            return getWorldBlock(wx, wy, wz);
        }

        private static void AddQuad(
            List<Vector3> vertices, List<int> triangles, List<Vector2> uvs, List<Vector3> normals,
            int d, bool backFace, int faceLayer, int i, int j, int w, int h, ushort blockId)
        {
            int u = (d + 1) % 3;
            int v = (d + 2) % 3;

            int[] pos = new int[3];
            pos[d] = faceLayer;
            pos[u] = i;
            pos[v] = j;
            Vector3 v0 = new Vector3(pos[0], pos[1], pos[2]);

            pos[u] = i + w;
            Vector3 v1 = new Vector3(pos[0], pos[1], pos[2]);

            pos[u] = i;
            pos[v] = j + h;
            Vector3 v2 = new Vector3(pos[0], pos[1], pos[2]);

            pos[u] = i + w;
            Vector3 v3 = new Vector3(pos[0], pos[1], pos[2]);

            Vector3 normal = FaceNormals[d];
            if (backFace) normal = -normal;

            int vertStart = vertices.Count;
            vertices.Add(v0);
            vertices.Add(v1);
            vertices.Add(v2);
            vertices.Add(v3);

            for (int k = 0; k < 4; k++)
                normals.Add(normal);

            if (backFace)
            {
                triangles.Add(vertStart);
                triangles.Add(vertStart + 2);
                triangles.Add(vertStart + 1);
                triangles.Add(vertStart + 1);
                triangles.Add(vertStart + 2);
                triangles.Add(vertStart + 3);
            }
            else
            {
                triangles.Add(vertStart);
                triangles.Add(vertStart + 1);
                triangles.Add(vertStart + 2);
                triangles.Add(vertStart + 1);
                triangles.Add(vertStart + 3);
                triangles.Add(vertStart + 2);
            }

            float uBase = (blockId % 16) * AtlasCellSize;
            float vBase = (blockId / 16) * AtlasCellSize;

            uvs.Add(new Vector2(uBase, vBase));
            uvs.Add(new Vector2(uBase + w * AtlasCellSize, vBase));
            uvs.Add(new Vector2(uBase, vBase + h * AtlasCellSize));
            uvs.Add(new Vector2(uBase + w * AtlasCellSize, vBase + h * AtlasCellSize));
        }
    }
}
