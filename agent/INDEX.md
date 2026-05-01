### 项目文件结构
```
Assets/Scripts/
├── Core/
│   ├── Block.cs              ← 体素结构体 (ushort id, 纯值类型)
│   ├── BlockType.cs          ← 方块ID常量定义
│   ├── Chunk.cs              ← 区块类 (16×256×16, 1D展平数组)
│   ├── World.cs              ← 世界管理器 (单例, 异步加载/卸载)
│   └── TextureAtlasGenerator.cs ← 程序化图集生成 (256×256, 16×16格, 自然色)
├── Terrain/
│   ├── NoiseGenerator.cs     ← 2D Perlin噪声 (多倍频叠加)
│   ├── TerrainGenerator.cs   ← 地形生成 (高度图→石头/泥土/草地)
│   └── MeshBuilder.cs        ← 贪婪网格构建器 (Greedy Meshing)
├── Player/
│   ├── VoxelRaycaster.cs     ← 3D DDA射线检测
│   └── FirstPersonController.cs ← 第一人称控制器 (AABB碰撞)
├── Inventory/
│   ├── ItemData.cs           ← 物品数据 (ScriptableObject, ID/Name/Icon/MaxStackSize)
│   ├── ItemStack.cs          ← 物品堆叠结构体 (ItemData + Count)
│   ├── Inventory.cs          ← 背包数据层 (45槽, AddItem/RemoveItem/HasItem, 事件驱动)
│   ├── Recipe.cs             ← 合成配方 (ScriptableObject, 3×3输入矩阵 + 输出)
│   └── CraftingMatcher.cs    ← 合成匹配器 (空间平移匹配算法)
└── UI/
    ├── InventoryUI.cs        ← 背包UI主控 (E键开关, 拖拽状态机, 合成集成)
    ├── ItemSlotUI.cs         ← 槽位UI组件 (左键拿起/放下, 右键拆分)
    └── DragIcon.cs           ← 拖拽图标 (跟随鼠标渲染)
```

### 🔑 核心设计要点

| 模块 | 关键决策 | 复杂度/开销 |
|------|---------|------------|
| Block | ushort id 纯值类型，零GC | 2 bytes/block |
| Chunk | index = x + y*16 + z*4096，连续内存 | 16×256×16 = 128KB/Chunk |
| World | Dictionary<Vector2Int, Chunk> + 双锁 | O(1) 查找 |
| 地形生成 | 6 octaves Perlin，persistence=0.5 | Task.Run 异步 |
| 贪婪网格 | 6面方向分别扫描合并，纹理平铺UV | 顶点数降低 80%+ |
| DDA射线 | 沿体素网格步进，无GC分配 | O(N) N=穿过体素数 |
| AABB碰撞 | 分轴独立检测+解析，贴墙无滑动 | O(K) K=重叠方块数 |
| ItemData | ScriptableObject 数据容器，零运行开销 | 1 asset/item |
| Inventory | ItemStack[] + Action<int> 事件驱动刷新 | O(1) 单槽 / O(N) 遍历 |
| 合成匹配 | 包围盒对齐 + 空间平移，支持任意位置摆放 | O(R×9) R=配方数 |

### 🎮 场景搭建步骤

1. **移除 PlasticSCM 包（重要！）：** Unity菜单 → Window → Package Manager → 搜索 "Version Control" → 选中 `com.unity.collab-proxy` → Remove
2. **创建 World 对象：** 场景中新建空 GameObject → 挂载 `World` 脚本 → 设置 Seed/ViewDistance
3. **创建 Player 对象：** 新建空 GameObject → 挂载 `Camera` + `FirstPersonController` → 将 Player 拖入 World 的 `PlayerTransform` 字段
4. **创建 UI Canvas：**
   - 场景右键 → UI → Canvas → 命名为 `GameCanvas`
   - Canvas 下创建空子对象 → 命名为 `InventoryPanel` → 挂载 `InventoryUI` 脚本
   - InventoryPanel 下创建三个空子对象：`MainSlots` / `CraftingGrid` / `OutputSlot`（设置 GridLayoutGroup 自动排列）
   - Canvas 下创建空子对象 → 命名为 `DragIcon` → 挂载 `DragIcon` 脚本 → 子对象放 Image + Text - TextMeshPro
5. **创建 Slot 预制体：**
   - 场景右键 → UI → Image → 命名为 `SlotPrefab` → 挂载 `ItemSlotUI` 脚本
   - SlotPrefab 下创建子 Image（命名 Icon）+ 子 GameObject → UI → Text - TextMeshPro（命名 Count）
   - 拖入 Project 窗口 `Assets/Prefabs/` 生成预制体，然后删除场景中的实例
6. **配置 InventoryUI：** 将 SlotPrefab 拖入 InventoryUI 的 SlotPrefab 字段，MainSlots/CraftingGrid/OutputSlot 分别拖入对应 Transform 字段，DragIcon 拖入 DragIcon 字段
7. **创建物品数据：** Project 右键 → Create → Minecraft → Item Data → 配置 Id/Name/Icon/MaxStackSize
8. **创建合成配方：** Project 右键 → Create → Minecraft → Recipe → 配置 3×3 Input 和 Output → 拖入 InventoryUI 的 Recipes 数组
9. **运行：** WASD移动，E键打开背包，鼠标左键拿起/放下物品，右键拆分，3×3合成区自动匹配配方

### ⚠️ 注意事项

- 首次运行需要约0.5-2秒生成初始区块（异步，不会卡顿），期间玩家被冻结在空中，区块就绪后自动解锁
- 背包打开时自动禁用玩家移动/视角旋转（通过 `Cursor.visible` 判断）
- 关闭背包时如果正在拖拽物品，自动归还到原槽位或背包空位
- 合成匹配算法通过计算玩家摆放的包围盒与配方包围盒对齐，支持配方在3×3网格内任意平移
- 如果使用 **URP**，Shader 会自动选择 `Universal Render Pipeline/Lit`；**Built-in** 则使用 `Standard`
- 纹理图集由 [TextureAtlasGenerator.cs](file:///d:/unityproject/minecraft/Assets/Scripts/Core/TextureAtlasGenerator.cs) 启动时自动生成，无需手动导入贴图。如需自定义颜色，修改其中的 `GetBlockColor()` 方法
- 如需调整地形，修改 [TerrainGenerator.cs](file:///d:/unityproject/minecraft/Assets/Scripts/Terrain/TerrainGenerator.cs) 中的 `SeaLevel` / `TerrainAmplitude` / `DirtThickness` 常量

### 🔧 已修复的Bug记录

| 日期 | 问题 | 根因 | 修复方案 |
|------|------|------|---------|
| 2026-05-01 | 进入Play模式无画面/无功能 | ①鼠标旋转互相覆盖 ②World.Instance空引用 ③玩家出生y=0 | ①合并Quaternion.Euler ②isWorldReady等待 ③强制y=100 |
| 2026-05-01 | World实例化但区块不加载 | lastPlayerChunkCoord默认(0,0)等于出生坐标 | 初始化为(int.MinValue,int.MinValue) |
| 2026-05-01 | 方块只渲染一个面 + 巨大紫色正方体包裹地图 | ①FaceNormals 6元素只用前3个→缺X轴面 ②三角形绕序反了→背面剔除掉正确面 ③面位置off-by-one | ①FaceNormals改为3元素(X+/Y+/Z+) ②前后三角形绕序互换 ③面位置统一为layer+1 |
