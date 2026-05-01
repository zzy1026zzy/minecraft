using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Minecraft.Terrain;
using UnityEngine;

namespace Minecraft.Core
{
    public class World : MonoBehaviour
    {
        public static World Instance { get; private set; }

        [Header("Chunk Settings")]
        public Material ChunkMaterial;
        public int ViewDistance = 8;
        public int Seed = 12345;

        [Header("Player")]
        public Transform PlayerTransform;

        private Dictionary<Vector2Int, Chunk> chunks = new Dictionary<Vector2Int, Chunk>();
        private Queue<Action> mainThreadActions = new Queue<Action>();
        private readonly object actionLock = new object();
        private readonly object chunksLock = new object();

        private Vector2Int lastPlayerChunkCoord = new Vector2Int(int.MinValue, int.MinValue);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (ChunkMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                if (shader != null) ChunkMaterial = new Material(shader);
            }

            if (ChunkMaterial != null && ChunkMaterial.mainTexture == null)
            {
                ChunkMaterial.mainTexture = TextureAtlasGenerator.Generate();
            }
        }

        private void Update()
        {
            ProcessMainThreadActions();
            UpdateChunkLoading();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void ProcessMainThreadActions()
        {
            lock (actionLock)
            {
                while (mainThreadActions.Count > 0)
                {
                    mainThreadActions.Dequeue()?.Invoke();
                }
            }
        }

        public void EnqueueMainThreadAction(Action action)
        {
            lock (actionLock)
            {
                mainThreadActions.Enqueue(action);
            }
        }

        private void UpdateChunkLoading()
        {
            if (PlayerTransform == null) return;

            Vector3 playerPos = PlayerTransform.position;
            int cx = Mathf.FloorToInt(playerPos.x / Chunk.Width);
            int cz = Mathf.FloorToInt(playerPos.z / Chunk.Depth);
            Vector2Int playerCoord = new Vector2Int(cx, cz);

            if (playerCoord == lastPlayerChunkCoord) return;
            lastPlayerChunkCoord = playerCoord;

            for (int x = -ViewDistance; x <= ViewDistance; x++)
            {
                for (int z = -ViewDistance; z <= ViewDistance; z++)
                {
                    Vector2Int coord = new Vector2Int(cx + x, cz + z);
                    if (!ChunkExists(coord))
                    {
                        LoadChunkAsync(coord);
                    }
                }
            }

            List<Vector2Int> toRemove = new List<Vector2Int>();
            lock (chunksLock)
            {
                foreach (var kvp in chunks)
                {
                    int dx = Mathf.Abs(kvp.Key.x - cx);
                    int dz = Mathf.Abs(kvp.Key.y - cz);
                    if (dx > ViewDistance + 2 || dz > ViewDistance + 2)
                    {
                        toRemove.Add(kvp.Key);
                    }
                }
            }

            foreach (var coord in toRemove)
            {
                UnloadChunk(coord);
            }
        }

        private bool ChunkExists(Vector2Int coord)
        {
            lock (chunksLock)
            {
                return chunks.ContainsKey(coord);
            }
        }

        public Chunk GetChunk(Vector2Int coord)
        {
            lock (chunksLock)
            {
                chunks.TryGetValue(coord, out Chunk chunk);
                return chunk;
            }
        }

        private void LoadChunkAsync(Vector2Int coord)
        {
            Chunk chunk = new Chunk(coord);

            lock (chunksLock)
            {
                chunks[coord] = chunk;
            }

            GameObject go = new GameObject($"Chunk_{coord.x}_{coord.y}");
            go.transform.SetParent(transform);
            go.transform.position = chunk.WorldOrigin;
            go.transform.localScale = Vector3.one;

            chunk.GameObject = go;
            chunk.MeshFilter = go.AddComponent<MeshFilter>();
            chunk.MeshRenderer = go.AddComponent<MeshRenderer>();
            chunk.MeshRenderer.material = ChunkMaterial;
            chunk.MeshCollider = go.AddComponent<MeshCollider>();

            Task.Run(() =>
            {
                try
                {
                    TerrainGenerator.GenerateChunkData(chunk, Seed);
                    chunk.IsGenerated = true;

                    ChunkMeshData meshData = MeshBuilder.BuildGreedyMesh(chunk, GetBlockWorld);

                    EnqueueMainThreadAction(() =>
                    {
                        if (chunk.GameObject == null) return;
                        ApplyMeshData(chunk, meshData);
                        chunk.IsDirty = false;
                    });
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Chunk generation failed for {coord}: {ex}");
                }
            });
        }

        private void UnloadChunk(Vector2Int coord)
        {
            Chunk chunk;
            lock (chunksLock)
            {
                if (!chunks.TryGetValue(coord, out chunk)) return;
                chunks.Remove(coord);
            }

            if (chunk.GameObject != null)
            {
                if (Application.isPlaying)
                    Destroy(chunk.GameObject);
                else
                    DestroyImmediate(chunk.GameObject);
            }
        }

        public Block GetBlockWorld(int x, int y, int z)
        {
            if (y < 0 || y >= Chunk.Height) return new Block(BlockType.Air);

            int cx = Mathf.FloorToInt((float)x / Chunk.Width);
            int cz = Mathf.FloorToInt((float)z / Chunk.Depth);

            Chunk chunk = GetChunk(new Vector2Int(cx, cz));
            if (chunk == null || !chunk.IsGenerated) return new Block(BlockType.Air);

            int lx = x - cx * Chunk.Width;
            int lz = z - cz * Chunk.Depth;

            return chunk.GetBlock(lx, y, lz);
        }

        public void SetBlockWorld(int x, int y, int z, Block block)
        {
            if (y < 0 || y >= Chunk.Height) return;

            int cx = Mathf.FloorToInt((float)x / Chunk.Width);
            int cz = Mathf.FloorToInt((float)z / Chunk.Depth);

            Chunk chunk = GetChunk(new Vector2Int(cx, cz));
            if (chunk == null || !chunk.IsGenerated) return;

            int lx = x - cx * Chunk.Width;
            int lz = z - cz * Chunk.Depth;

            chunk.SetBlock(lx, y, lz, block);

            RebuildChunkMesh(chunk);

            if (lx == 0) RebuildNeighborChunk(cx - 1, cz);
            if (lx == Chunk.Width - 1) RebuildNeighborChunk(cx + 1, cz);
            if (lz == 0) RebuildNeighborChunk(cx, cz - 1);
            if (lz == Chunk.Depth - 1) RebuildNeighborChunk(cx, cz + 1);
        }

        private void RebuildNeighborChunk(int cx, int cz)
        {
            Chunk neighbor = GetChunk(new Vector2Int(cx, cz));
            if (neighbor != null && neighbor.IsGenerated)
            {
                RebuildChunkMesh(neighbor);
            }
        }

        public void RebuildChunkMesh(Chunk chunk)
        {
            chunk.IsDirty = true;
            Task.Run(() =>
            {
                try
                {
                    ChunkMeshData meshData = MeshBuilder.BuildGreedyMesh(chunk, GetBlockWorld);
                    EnqueueMainThreadAction(() =>
                    {
                        if (chunk.GameObject == null) return;
                        ApplyMeshData(chunk, meshData);
                        chunk.IsDirty = false;
                    });
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Mesh rebuild failed for {chunk.Coord}: {ex}");
                }
            });
        }

        private void ApplyMeshData(Chunk chunk, ChunkMeshData data)
        {
            Mesh mesh = chunk.MeshFilter.sharedMesh;
            if (mesh == null)
            {
                mesh = new Mesh();
                mesh.name = $"ChunkMesh_{chunk.Coord.x}_{chunk.Coord.y}";
            }
            else
            {
                mesh.Clear();
            }

            mesh.indexFormat = data.Vertices.Length > 65535
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;

            mesh.SetVertices(data.Vertices);
            mesh.SetTriangles(data.Triangles, 0);
            mesh.SetUVs(0, data.UVs);
            mesh.SetNormals(data.Normals);
            mesh.RecalculateBounds();

            chunk.MeshFilter.sharedMesh = mesh;
            chunk.MeshCollider.sharedMesh = mesh;
        }
    }
}
