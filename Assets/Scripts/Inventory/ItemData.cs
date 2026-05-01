using UnityEngine;

namespace Minecraft.Items
{
    [CreateAssetMenu(fileName = "NewItem", menuName = "Minecraft/Item Data")]
    public class ItemData : ScriptableObject
    {
        public ushort Id;
        public string ItemName;
        public Sprite Icon;
        public int MaxStackSize = 64;
    }
}
