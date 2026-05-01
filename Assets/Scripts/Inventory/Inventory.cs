using System;

namespace Minecraft.Items
{
    public class Inventory
    {
        public const int SlotCount = 45;

        private ItemStack[] slots = new ItemStack[SlotCount];
        public event Action<int> OnSlotChanged;

        public ItemStack GetSlot(int index)
        {
            if (index < 0 || index >= SlotCount) return ItemStack.Empty;
            return slots[index];
        }

        public void SetSlot(int index, ItemStack stack)
        {
            if (index < 0 || index >= SlotCount) return;
            slots[index] = stack;
            OnSlotChanged?.Invoke(index);
        }

        public bool AddItem(ItemStack stack)
        {
            if (stack.IsEmpty) return true;

            int remaining = stack.Count;

            for (int i = 0; i < SlotCount && remaining > 0; i++)
            {
                if (slots[i].Item == stack.Item && slots[i].Count < slots[i].Item.MaxStackSize)
                {
                    int canAdd = stack.Item.MaxStackSize - slots[i].Count;
                    int toAdd = Math.Min(canAdd, remaining);
                    slots[i].Count += toAdd;
                    remaining -= toAdd;
                    OnSlotChanged?.Invoke(i);
                }
            }

            for (int i = 0; i < SlotCount && remaining > 0; i++)
            {
                if (slots[i].IsEmpty)
                {
                    int toAdd = Math.Min(stack.Item.MaxStackSize, remaining);
                    slots[i] = new ItemStack(stack.Item, toAdd);
                    remaining -= toAdd;
                    OnSlotChanged?.Invoke(i);
                }
            }

            return remaining == 0;
        }

        public ItemStack RemoveItem(int index, int count)
        {
            if (index < 0 || index >= SlotCount || slots[index].IsEmpty) return ItemStack.Empty;

            int toRemove = Math.Min(count, slots[index].Count);
            ItemStack removed = new ItemStack(slots[index].Item, toRemove);
            slots[index].Count -= toRemove;

            if (slots[index].Count <= 0)
                slots[index] = ItemStack.Empty;

            OnSlotChanged?.Invoke(index);
            return removed;
        }

        public bool HasItem(ItemData item, int count)
        {
            int found = 0;
            for (int i = 0; i < SlotCount; i++)
            {
                if (slots[i].Item == item)
                {
                    found += slots[i].Count;
                    if (found >= count) return true;
                }
            }
            return false;
        }

        public int GetItemCount(ItemData item)
        {
            int found = 0;
            for (int i = 0; i < SlotCount; i++)
            {
                if (slots[i].Item == item)
                    found += slots[i].Count;
            }
            return found;
        }

        public int FindEmptySlot()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (slots[i].IsEmpty) return i;
            }
            return -1;
        }

        public void SwapSlots(int a, int b)
        {
            if (a < 0 || a >= SlotCount || b < 0 || b >= SlotCount) return;
            ItemStack temp = slots[a];
            slots[a] = slots[b];
            slots[b] = temp;
            OnSlotChanged?.Invoke(a);
            OnSlotChanged?.Invoke(b);
        }
    }
}
