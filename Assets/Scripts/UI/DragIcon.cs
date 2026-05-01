using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Minecraft.UI
{
    public class DragIcon : MonoBehaviour
    {
        public Image IconImage;
        public TMP_Text CountText;

        private Canvas canvas;
        private RectTransform rectTransform;

        private void Awake()
        {
            canvas = GetComponentInParent<Canvas>();
            rectTransform = GetComponent<RectTransform>();
            Hide();
        }

        public void Show(Sprite icon, int count, Vector2 screenPosition)
        {
            IconImage.sprite = icon;
            IconImage.enabled = true;
            CountText.text = count > 1 ? count.ToString() : "";
            gameObject.SetActive(true);
            UpdatePosition(screenPosition);
        }

        public void UpdatePosition(Vector2 screenPosition)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform, screenPosition, canvas.worldCamera, out Vector2 localPoint);
            rectTransform.localPosition = localPoint;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
