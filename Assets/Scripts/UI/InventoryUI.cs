using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Minecraft.Items;

namespace Minecraft.UI
{
    public class InventoryUI : MonoBehaviour
    {
        [Header("Prefabs & Parents")]
        public GameObject SlotPrefab;
        public Transform MainInventoryParent;
        public Transform CraftingGridParent;
        public Transform OutputSlotParent;
        public DragIcon DragIcon;

        [Header("Panel")]
        public GameObject InventoryPanel;

        [Header("Crafting")]
        public Recipe[] Recipes;

        private Inventory inventory;
        private ItemData[] craftingGrid = new ItemData[9];
        private int[] craftingCounts = new int[9];
        private Recipe currentRecipe;

        private List<ItemSlotUI> mainSlots = new List<ItemSlotUI>();
        private List<ItemSlotUI> craftingSlots = new List<ItemSlotUI>();
        private ItemSlotUI outputSlot;

        private bool isDragging;
        private ItemData draggedItem;
        private int draggedCount;
        private ItemSlotUI dragSourceSlot;
        private bool dragFromCrafting;

        private bool isOpen;

        private void Awake()
        {
            inventory = new Inventory();
            inventory.OnSlotChanged += OnInventorySlotChanged;
            InventoryPanel.SetActive(false);
        }

        private void Start()
        {
            CreateSlots();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                ToggleInventory();
            }

            if (isDragging)
            {
                DragIcon.UpdatePosition(Input.mousePosition);
            }
        }

        private void CreateSlots()
        {
            for (int i = 0; i < Inventory.SlotCount; i++)
            {
                GameObject slotObj = Instantiate(SlotPrefab, MainInventoryParent);
                ItemSlotUI slotUI = slotObj.GetComponent<ItemSlotUI>();
                slotUI.SlotIndex = i;
                slotUI.IsCraftingSlot = false;
                slotUI.IsOutputSlot = false;
                mainSlots.Add(slotUI);
            }

            for (int i = 0; i < 9; i++)
            {
                GameObject slotObj = Instantiate(SlotPrefab, CraftingGridParent);
                ItemSlotUI slotUI = slotObj.GetComponent<ItemSlotUI>();
                slotUI.SlotIndex = i;
                slotUI.IsCraftingSlot = true;
                slotUI.IsOutputSlot = false;
                craftingSlots.Add(slotUI);
            }

            GameObject outputObj = Instantiate(SlotPrefab, OutputSlotParent);
            outputSlot = outputObj.GetComponent<ItemSlotUI>();
            outputSlot.SlotIndex = 0;
            outputSlot.IsCraftingSlot = false;
            outputSlot.IsOutputSlot = true;
        }

        private void ToggleInventory()
        {
            isOpen = !isOpen;
            InventoryPanel.SetActive(isOpen);

            if (isOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                RefreshAllSlots();
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                ReturnDraggedItem();
            }
        }

        private void OnInventorySlotChanged(int index)
        {
            if (!isOpen) return;
            RefreshMainSlot(index);
        }

        private void RefreshAllSlots()
        {
            for (int i = 0; i < Inventory.SlotCount; i++)
                RefreshMainSlot(i);
            for (int i = 0; i < 9; i++)
                RefreshCraftingSlot(i);
            RefreshOutputSlot();
        }

        private void RefreshMainSlot(int index)
        {
            if (index < 0 || index >= mainSlots.Count) return;
            ItemStack stack = inventory.GetSlot(index);
            mainSlots[index].SetItem(stack.Item?.Icon, stack.Count);
        }

        private void RefreshCraftingSlot(int index)
        {
            if (index < 0 || index >= craftingSlots.Count) return;
            ItemData item = craftingGrid[index];
            int count = craftingCounts[index];
            craftingSlots[index].SetItem(item?.Icon, count);
        }

        private void RefreshOutputSlot()
        {
            if (currentRecipe != null)
                outputSlot.SetItem(currentRecipe.Output.Item?.Icon, currentRecipe.Output.Count);
            else
                outputSlot.SetItem(null, 0);
        }

        private void UpdateCrafting()
        {
            currentRecipe = CraftingMatcher.TryMatch(craftingGrid, Recipes);
            RefreshOutputSlot();
        }

        public void OnSlotLeftClick(ItemSlotUI slot)
        {
            if (slot.IsOutputSlot)
            {
                CraftOutput();
                return;
            }

            if (slot.IsCraftingSlot)
            {
                HandleCraftingSlotLeftClick(slot);
                return;
            }

            HandleMainSlotLeftClick(slot);
        }

        public void OnSlotRightClick(ItemSlotUI slot)
        {
            if (slot.IsOutputSlot) return;

            if (slot.IsCraftingSlot)
            {
                HandleCraftingSlotRightClick(slot);
                return;
            }

            HandleMainSlotRightClick(slot);
        }

        private void HandleMainSlotLeftClick(ItemSlotUI slot)
        {
            ItemStack slotStack = inventory.GetSlot(slot.SlotIndex);

            if (!isDragging)
            {
                if (!slotStack.IsEmpty)
                {
                    draggedItem = slotStack.Item;
                    draggedCount = slotStack.Count;
                    dragSourceSlot = slot;
                    dragFromCrafting = false;
                    isDragging = true;
                    inventory.SetSlot(slot.SlotIndex, ItemStack.Empty);
                    DragIcon.Show(draggedItem.Icon, draggedCount, Input.mousePosition);
                }
            }
            else
            {
                if (slotStack.IsEmpty)
                {
                    inventory.SetSlot(slot.SlotIndex, new ItemStack(draggedItem, draggedCount));
                    ClearDrag();
                }
                else if (slotStack.Item == draggedItem)
                {
                    int canAdd = draggedItem.MaxStackSize - slotStack.Count;
                    int toAdd = Mathf.Min(canAdd, draggedCount);
                    if (toAdd > 0)
                    {
                        inventory.SetSlot(slot.SlotIndex, new ItemStack(draggedItem, slotStack.Count + toAdd));
                        draggedCount -= toAdd;
                        if (draggedCount <= 0)
                            ClearDrag();
                        else
                            DragIcon.Show(draggedItem.Icon, draggedCount, Input.mousePosition);
                    }
                }
                else
                {
                    ItemStack temp = slotStack;
                    inventory.SetSlot(slot.SlotIndex, new ItemStack(draggedItem, draggedCount));
                    draggedItem = temp.Item;
                    draggedCount = temp.Count;
                    dragSourceSlot = slot;
                    dragFromCrafting = false;
                    DragIcon.Show(draggedItem?.Icon, draggedCount, Input.mousePosition);
                }
            }
        }

        private void HandleMainSlotRightClick(ItemSlotUI slot)
        {
            ItemStack slotStack = inventory.GetSlot(slot.SlotIndex);

            if (!isDragging)
            {
                if (!slotStack.IsEmpty)
                {
                    int half = Mathf.CeilToInt(slotStack.Count / 2f);
                    draggedItem = slotStack.Item;
                    draggedCount = half;
                    dragSourceSlot = slot;
                    dragFromCrafting = false;
                    isDragging = true;
                    inventory.SetSlot(slot.SlotIndex, new ItemStack(slotStack.Item, slotStack.Count - half));
                    DragIcon.Show(draggedItem.Icon, draggedCount, Input.mousePosition);
                }
            }
            else
            {
                if (slotStack.IsEmpty)
                {
                    inventory.SetSlot(slot.SlotIndex, new ItemStack(draggedItem, 1));
                    draggedCount--;
                    if (draggedCount <= 0)
                        ClearDrag();
                    else
                        DragIcon.Show(draggedItem.Icon, draggedCount, Input.mousePosition);
                }
                else if (slotStack.Item == draggedItem && slotStack.Count < draggedItem.MaxStackSize)
                {
                    inventory.SetSlot(slot.SlotIndex, new ItemStack(draggedItem, slotStack.Count + 1));
                    draggedCount--;
                    if (draggedCount <= 0)
                        ClearDrag();
                    else
                        DragIcon.Show(draggedItem.Icon, draggedCount, Input.mousePosition);
                }
            }
        }

        private void HandleCraftingSlotLeftClick(ItemSlotUI slot)
        {
            if (!isDragging)
            {
                if (craftingGrid[slot.SlotIndex] != null)
                {
                    draggedItem = craftingGrid[slot.SlotIndex];
                    draggedCount = craftingCounts[slot.SlotIndex];
                    dragSourceSlot = slot;
                    dragFromCrafting = true;
                    isDragging = true;
                    craftingGrid[slot.SlotIndex] = null;
                    craftingCounts[slot.SlotIndex] = 0;
                    RefreshCraftingSlot(slot.SlotIndex);
                    UpdateCrafting();
                    DragIcon.Show(draggedItem.Icon, draggedCount, Input.mousePosition);
                }
            }
            else
            {
                if (craftingGrid[slot.SlotIndex] == null)
                {
                    craftingGrid[slot.SlotIndex] = draggedItem;
                    craftingCounts[slot.SlotIndex] = draggedCount;
                    RefreshCraftingSlot(slot.SlotIndex);
                    UpdateCrafting();
                    ClearDrag();
                }
                else if (craftingGrid[slot.SlotIndex] == draggedItem)
                {
                    int canAdd = draggedItem.MaxStackSize - craftingCounts[slot.SlotIndex];
                    int toAdd = Mathf.Min(canAdd, draggedCount);
                    if (toAdd > 0)
                    {
                        craftingCounts[slot.SlotIndex] += toAdd;
                        draggedCount -= toAdd;
                        RefreshCraftingSlot(slot.SlotIndex);
                        UpdateCrafting();
                        if (draggedCount <= 0)
                            ClearDrag();
                        else
                            DragIcon.Show(draggedItem.Icon, draggedCount, Input.mousePosition);
                    }
                }
                else
                {
                    ItemData tempItem = craftingGrid[slot.SlotIndex];
                    int tempCount = craftingCounts[slot.SlotIndex];
                    craftingGrid[slot.SlotIndex] = draggedItem;
                    craftingCounts[slot.SlotIndex] = draggedCount;
                    draggedItem = tempItem;
                    draggedCount = tempCount;
                    RefreshCraftingSlot(slot.SlotIndex);
                    UpdateCrafting();
                    DragIcon.Show(draggedItem?.Icon, draggedCount, Input.mousePosition);
                }
            }
        }

        private void HandleCraftingSlotRightClick(ItemSlotUI slot)
        {
            if (!isDragging)
            {
                if (craftingGrid[slot.SlotIndex] != null)
                {
                    int half = Mathf.CeilToInt(craftingCounts[slot.SlotIndex] / 2f);
                    draggedItem = craftingGrid[slot.SlotIndex];
                    draggedCount = half;
                    dragSourceSlot = slot;
                    dragFromCrafting = true;
                    isDragging = true;
                    craftingCounts[slot.SlotIndex] -= half;
                    if (craftingCounts[slot.SlotIndex] <= 0)
                    {
                        craftingGrid[slot.SlotIndex] = null;
                        craftingCounts[slot.SlotIndex] = 0;
                    }
                    RefreshCraftingSlot(slot.SlotIndex);
                    UpdateCrafting();
                    DragIcon.Show(draggedItem.Icon, draggedCount, Input.mousePosition);
                }
            }
            else
            {
                if (craftingGrid[slot.SlotIndex] == null)
                {
                    craftingGrid[slot.SlotIndex] = draggedItem;
                    craftingCounts[slot.SlotIndex] = 1;
                    draggedCount--;
                    RefreshCraftingSlot(slot.SlotIndex);
                    UpdateCrafting();
                    if (draggedCount <= 0)
                        ClearDrag();
                    else
                        DragIcon.Show(draggedItem.Icon, draggedCount, Input.mousePosition);
                }
                else if (craftingGrid[slot.SlotIndex] == draggedItem)
                {
                    int canAdd = draggedItem.MaxStackSize - craftingCounts[slot.SlotIndex];
                    if (canAdd > 0)
                    {
                        craftingCounts[slot.SlotIndex]++;
                        draggedCount--;
                        RefreshCraftingSlot(slot.SlotIndex);
                        UpdateCrafting();
                        if (draggedCount <= 0)
                            ClearDrag();
                        else
                            DragIcon.Show(draggedItem.Icon, draggedCount, Input.mousePosition);
                    }
                }
            }
        }

        private void CraftOutput()
        {
            if (currentRecipe == null) return;

            ItemStack output = currentRecipe.Output;
            if (!inventory.AddItem(output)) return;

            for (int i = 0; i < 9; i++)
            {
                if (craftingGrid[i] != null)
                {
                    craftingCounts[i]--;
                    if (craftingCounts[i] <= 0)
                    {
                        craftingGrid[i] = null;
                        craftingCounts[i] = 0;
                    }
                    RefreshCraftingSlot(i);
                }
            }

            UpdateCrafting();
        }

        public void OnSlotBeginDrag(ItemSlotUI slot, PointerEventData eventData)
        {
        }

        public void OnSlotDrag(ItemSlotUI slot, PointerEventData eventData)
        {
        }

        public void OnSlotEndDrag(ItemSlotUI slot, PointerEventData eventData)
        {
        }

        private void ClearDrag()
        {
            isDragging = false;
            draggedItem = null;
            draggedCount = 0;
            dragSourceSlot = null;
            dragFromCrafting = false;
            DragIcon.Hide();
        }

        private void ReturnDraggedItem()
        {
            if (!isDragging) return;

            if (dragFromCrafting && dragSourceSlot != null)
            {
                craftingGrid[dragSourceSlot.SlotIndex] = draggedItem;
                craftingCounts[dragSourceSlot.SlotIndex] = draggedCount;
                RefreshCraftingSlot(dragSourceSlot.SlotIndex);
                UpdateCrafting();
            }
            else if (!dragFromCrafting && dragSourceSlot != null)
            {
                inventory.SetSlot(dragSourceSlot.SlotIndex, new ItemStack(draggedItem, draggedCount));
            }
            else
            {
                inventory.AddItem(new ItemStack(draggedItem, draggedCount));
            }

            ClearDrag();
        }

        public Inventory GetInventory()
        {
            return inventory;
        }
    }
}
