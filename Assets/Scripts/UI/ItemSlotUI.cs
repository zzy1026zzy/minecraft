using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Minecraft.UI
{
    public class ItemSlotUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public Image IconImage;
        public TMP_Text CountText;
        public Image SlotBackground;
        public int SlotIndex;
        public bool IsCraftingSlot;
        public bool IsOutputSlot;

        private InventoryUI inventoryUI;

        private void Awake()
        {
            inventoryUI = GetComponentInParent<InventoryUI>();
        }

        public void SetItem(Sprite icon, int count)
        {
            if (icon != null && count > 0)
            {
                IconImage.sprite = icon;
                IconImage.enabled = true;
                CountText.text = count > 1 ? count.ToString() : "";
            }
            else
            {
                IconImage.sprite = null;
                IconImage.enabled = false;
                CountText.text = "";
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (inventoryUI == null) return;

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                inventoryUI.OnSlotLeftClick(this);
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                inventoryUI.OnSlotRightClick(this);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (inventoryUI == null) return;
            inventoryUI.OnSlotBeginDrag(this, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (inventoryUI == null) return;
            inventoryUI.OnSlotDrag(this, eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (inventoryUI == null) return;
            inventoryUI.OnSlotEndDrag(this, eventData);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
        }

        public void OnPointerExit(PointerEventData eventData)
        {
        }
    }
}
