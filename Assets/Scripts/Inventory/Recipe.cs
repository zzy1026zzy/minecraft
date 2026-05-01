using UnityEngine;

namespace Minecraft.Items
{
    [CreateAssetMenu(fileName = "NewRecipe", menuName = "Minecraft/Recipe")]
    public class Recipe : ScriptableObject
    {
        public ItemData[] Input = new ItemData[9];
        public ItemStack Output;
    }
}
