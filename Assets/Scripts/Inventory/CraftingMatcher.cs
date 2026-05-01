namespace Minecraft.Items
{
    public static class CraftingMatcher
    {
        public static Recipe TryMatch(ItemData[] grid, Recipe[] recipes)
        {
            if (grid == null || grid.Length != 9 || recipes == null) return null;

            foreach (var recipe in recipes)
            {
                if (recipe == null || recipe.Input == null) continue;
                if (TryMatchRecipe(grid, recipe)) return recipe;
            }
            return null;
        }

        private static bool TryMatchRecipe(ItemData[] grid, Recipe recipe)
        {
            int gridMinX = 3, gridMinY = 3, gridMaxX = -1, gridMaxY = -1;
            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 3; x++)
                {
                    if (grid[x + y * 3] != null)
                    {
                        if (x < gridMinX) gridMinX = x;
                        if (y < gridMinY) gridMinY = y;
                        if (x > gridMaxX) gridMaxX = x;
                        if (y > gridMaxY) gridMaxY = y;
                    }
                }
            }

            int recipeMinX = 3, recipeMinY = 3, recipeMaxX = -1, recipeMaxY = -1;
            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 3; x++)
                {
                    if (recipe.Input[x + y * 3] != null)
                    {
                        if (x < recipeMinX) recipeMinX = x;
                        if (y < recipeMinY) recipeMinY = y;
                        if (x > recipeMaxX) recipeMaxX = x;
                        if (y > recipeMaxY) recipeMaxY = y;
                    }
                }
            }

            if (recipeMinX > recipeMaxX) return false;

            int gridW = gridMaxX - gridMinX + 1;
            int gridH = gridMaxY - gridMinY + 1;
            int recipeW = recipeMaxX - recipeMinX + 1;
            int recipeH = recipeMaxY - recipeMinY + 1;

            if (gridW != recipeW || gridH != recipeH) return false;

            int offsetX = gridMinX - recipeMinX;
            int offsetY = gridMinY - recipeMinY;

            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 3; x++)
                {
                    ItemData recipeItem = recipe.Input[x + y * 3];
                    int gx = x + offsetX;
                    int gy = y + offsetY;

                    if (gx < 0 || gx >= 3 || gy < 0 || gy >= 3)
                    {
                        if (recipeItem != null) return false;
                        continue;
                    }

                    ItemData gridItem = grid[gx + gy * 3];
                    if (recipeItem != gridItem) return false;
                }
            }

            return true;
        }
    }
}
