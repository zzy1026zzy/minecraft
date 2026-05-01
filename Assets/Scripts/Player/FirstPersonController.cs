using Minecraft.Core;
using UnityEngine;

namespace Minecraft.Player
{
    [RequireComponent(typeof(Camera))]
    public class FirstPersonController : MonoBehaviour
    {
        [Header("Movement")]
        public float WalkSpeed = 5f;
        public float SprintSpeed = 8f;
        public float JumpForce = 8f;
        public float Gravity = 20f;

        [Header("Mouse Look")]
        public float MouseSensitivity = 2f;
        public float VerticalClamp = 89f;

        [Header("Player Dimensions")]
        public float PlayerWidth = 0.6f;
        public float PlayerHeight = 1.8f;
        public float EyeHeight = 1.6f;

        [Header("Interaction")]
        public float ReachDistance = 8f;
        public ushort PlaceBlockId = BlockType.Plank;

        private Camera playerCamera;
        private float horizontalRotation;
        private float verticalRotation;
        private float verticalVelocity;
        private bool isGrounded;
        private bool isWorldReady;

        private float HalfWidth => PlayerWidth * 0.5f;

        private void Awake()
        {
            playerCamera = GetComponent<Camera>();
            horizontalRotation = transform.eulerAngles.y;
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (transform.position.y < 80f)
            {
                transform.position = new Vector3(transform.position.x, 100f, transform.position.z);
            }
        }

        private void Update()
        {
            if (World.Instance == null) return;

            if (Cursor.visible) return;

            if (!isWorldReady)
            {
                TryRegisterWithWorld();
                return;
            }

            HandleMouseLook();
            HandleMovement();
            HandleBlockInteraction();
        }

        private void TryRegisterWithWorld()
        {
            if (World.Instance.PlayerTransform == null)
            {
                World.Instance.PlayerTransform = transform;
            }

            int cx = Mathf.FloorToInt(transform.position.x / Chunk.Width);
            int cz = Mathf.FloorToInt(transform.position.z / Chunk.Depth);
            Chunk chunk = World.Instance.GetChunk(new Vector2Int(cx, cz));

            if (chunk != null && chunk.IsGenerated)
            {
                isWorldReady = true;
            }
        }

        private void HandleMouseLook()
        {
            float mouseX = Input.GetAxis("Mouse X") * MouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * MouseSensitivity;

            horizontalRotation += mouseX;
            verticalRotation -= mouseY;
            verticalRotation = Mathf.Clamp(verticalRotation, -VerticalClamp, VerticalClamp);

            transform.rotation = Quaternion.Euler(verticalRotation, horizontalRotation, 0f);
        }

        private void HandleMovement()
        {
            float speed = Input.GetKey(KeyCode.LeftShift) ? SprintSpeed : WalkSpeed;

            Vector3 input = Vector3.zero;
            if (Input.GetKey(KeyCode.W)) input += transform.forward;
            if (Input.GetKey(KeyCode.S)) input -= transform.forward;
            if (Input.GetKey(KeyCode.A)) input -= transform.right;
            if (Input.GetKey(KeyCode.D)) input += transform.right;

            if (input.magnitude > 1f) input.Normalize();

            Vector3 horizontalVelocity = input * speed;

            isGrounded = CheckGrounded();

            if (isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;

            if (isGrounded && Input.GetKeyDown(KeyCode.Space))
                verticalVelocity = JumpForce;

            verticalVelocity -= Gravity * Time.deltaTime;

            Vector3 totalVelocity = horizontalVelocity + Vector3.up * verticalVelocity;
            Vector3 delta = totalVelocity * Time.deltaTime;

            Vector3 newPos = transform.position;

            newPos.x = MoveAxis(transform.position, delta, 0);
            newPos.y = MoveAxis(transform.position, delta, 1);
            if (newPos.y != transform.position.y + delta.y)
                verticalVelocity = 0f;
            newPos.z = MoveAxis(transform.position, delta, 2);

            transform.position = newPos;
        }

        private bool CheckGrounded()
        {
            if (World.Instance == null) return false;

            Vector3 footPos = transform.position;
            Vector3 checkMin = footPos - new Vector3(HalfWidth, 0.01f, HalfWidth);
            Vector3 checkMax = footPos + new Vector3(HalfWidth, 0f, HalfWidth);

            int minX = Mathf.FloorToInt(checkMin.x);
            int minY = Mathf.FloorToInt(checkMin.y);
            int minZ = Mathf.FloorToInt(checkMin.z);
            int maxX = Mathf.FloorToInt(checkMax.x - 0.001f);
            int maxZ = Mathf.FloorToInt(checkMax.z - 0.001f);

            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    if (World.Instance.GetBlockWorld(x, minY, z).IsSolid)
                        return true;
                }
            }

            return false;
        }

        private float MoveAxis(Vector3 currentPos, Vector3 delta, int axis)
        {
            float target = currentPos[axis] + delta[axis];
            if (Mathf.Approximately(delta[axis], 0f)) return currentPos[axis];

            Vector3 testPos = currentPos;
            testPos[axis] = target;

            Vector3 aabbMin = testPos - new Vector3(HalfWidth, 0f, HalfWidth);
            Vector3 aabbMax = testPos + new Vector3(HalfWidth, PlayerHeight, HalfWidth);

            int minX = Mathf.FloorToInt(aabbMin.x);
            int minY = Mathf.FloorToInt(aabbMin.y);
            int minZ = Mathf.FloorToInt(aabbMin.z);
            int maxX = Mathf.FloorToInt(aabbMax.x - 0.001f);
            int maxY = Mathf.FloorToInt(aabbMax.y - 0.001f);
            int maxZ = Mathf.FloorToInt(aabbMax.z - 0.001f);

            bool collided = false;
            float resolvePos = target;

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        if (World.Instance.GetBlockWorld(x, y, z).IsSolid)
                        {
                            collided = true;
                            if (axis == 0)
                            {
                                if (delta[axis] > 0f)
                                    resolvePos = Mathf.Min(resolvePos, x - HalfWidth - 0.001f);
                                else
                                    resolvePos = Mathf.Max(resolvePos, x + 1f + HalfWidth + 0.001f);
                            }
                            else if (axis == 1)
                            {
                                if (delta[axis] > 0f)
                                    resolvePos = Mathf.Min(resolvePos, y - PlayerHeight - 0.001f);
                                else
                                    resolvePos = Mathf.Max(resolvePos, y + 1f + 0.001f);
                            }
                            else
                            {
                                if (delta[axis] > 0f)
                                    resolvePos = Mathf.Min(resolvePos, z - HalfWidth - 0.001f);
                                else
                                    resolvePos = Mathf.Max(resolvePos, z + 1f + HalfWidth + 0.001f);
                            }
                        }
                    }
                }
            }

            return collided ? resolvePos : target;
        }

        private void HandleBlockInteraction()
        {
            Vector3 eyePos = transform.position + Vector3.up * EyeHeight;
            Vector3 rayDir = playerCamera.transform.forward;

            VoxelRaycastHit hit = VoxelRaycaster.Raycast(eyePos, rayDir, ReachDistance,
                (x, y, z) => World.Instance.GetBlockWorld(x, y, z));

            if (!hit.HasHit) return;

            if (Input.GetMouseButtonDown(0))
            {
                World.Instance.SetBlockWorld(hit.BlockPos.x, hit.BlockPos.y, hit.BlockPos.z,
                    new Block(BlockType.Air));
            }

            if (Input.GetMouseButtonDown(1))
            {
                Vector3Int placePos = hit.BlockPos + hit.FaceNormal;
                World.Instance.SetBlockWorld(placePos.x, placePos.y, placePos.z,
                    new Block(PlaceBlockId));
            }
        }

        private void OnDestroy()
        {
            if (World.Instance != null && World.Instance.PlayerTransform == transform)
            {
                World.Instance.PlayerTransform = null;
            }
        }
    }
}
