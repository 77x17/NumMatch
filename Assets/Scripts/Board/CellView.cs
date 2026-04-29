using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using UnityEngine.EventSystems;
using DG.Tweening;

public enum GemType { None, Pink, Orange, Purple };

public class CellView : MonoBehaviour, IPointerDownHandler {
    [Header("References")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_Text numberText;

    [Header("Visual Config")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.yellow;

    [Header("Gem Backgrounds")]
    [SerializeField] private Sprite[] gemBackgroundSprites;

    [Header("Components")]
    [SerializeField] private GameObject normalHighlightOverlay;
    // Kéo thả GameObject 'SelectionOverlay' bạn vừa tạo vào đây trong Inspector
    [SerializeField] private GameObject selectionOverlay;
    [SerializeField] private GameObject previewOverlay;
    [SerializeField] private GameObject hintOverlay;

    private Sprite defaultSprite;

    [Header("Text Color Config")]
    private Color normalTextColor = new Color(0.161f, 0.361f, 0.416f, 1f);
    private Color matchedTextColor = new Color(0.161f, 0.361f, 0.416f, 0.3f);

    public int Index { get; private set; }
    public int Value { get; private set; }

    // Event - BoardManager lắng nghe sự kiện này
    public event Action<int> OnCellClicked;

    public GemType GemType { get; private set; }
    private Coroutine activeAnimation;

    // Start is called before the first frame update
    public void Init(int index, int value)
    {
        Index = index;
        Value = value;

        transform.localScale = Vector3.one;

        if (defaultSprite == null) 
            defaultSprite = backgroundImage.sprite;

        if (value == 0)
        {
            numberText.text = "";
        }
        else
        {
            numberText.text = value.ToString();
            numberText.color = normalTextColor;
        }
        // SetHighlight(false);

        GemType = GemType.None;
        ApplyGemBackground(GemType);

        previewOverlay.transform.localScale = Vector3.zero;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        HandleClick();
    }

    private void HandleClick()
    {
        if (Value == 0) return;
        
        // Thêm một chút effect thu nhỏ nhẹ khi bấm để tăng cảm giác "vật lý"
        // (Nếu bạn đang dùng DOTween thì càng tốt, nếu không dùng Coroutine)
        transform.localScale = Vector3.one * 0.95f; 
        Invoke(nameof(ResetScale), 0.1f); // Reset scale sau 0.1s

        OnCellClicked?.Invoke(Index);
    }

    private void ResetScale()
    {
        transform.localScale = Vector3.one;
    }

    public void SetHighlight(bool isSelected)
    {
        if (GemType != GemType.None && selectionOverlay != null)
        {
            // Overlay chỉ xuất hiện khi ô được CHỌN và ô đó LÀ GEM
            bool showOverlay = isSelected && (GemType != GemType.None);
            selectionOverlay.SetActive(showOverlay);

            if (showOverlay)
            {
                selectionOverlay.transform.DOKill();
                selectionOverlay.transform.localScale = Vector3.one * 0.5f; // Bắt đầu từ nhỏ
                selectionOverlay.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack); // Phóng lớn nảy nhẹ

                numberText.color = Color.yellow;
            }
            else
            {
                numberText.color = Color.white;
            }
        }
        else 
        {
            if (normalHighlightOverlay != null)
            {
                normalHighlightOverlay.transform.DOKill();

                if (isSelected)
                {
                    normalHighlightOverlay.SetActive(true);
                    normalHighlightOverlay.transform.localScale = Vector3.zero;
                    normalHighlightOverlay.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutCubic);
                }
                else
                {
                    normalHighlightOverlay.transform.DOScale(Vector3.zero, 0.15f)
                        .SetEase(Ease.InCubic)
                        .OnComplete(() => normalHighlightOverlay.SetActive(false));
                }
            }
            
            // Background chính giữ nguyên màu normalColor để làm nền cho màu vàng phóng lên
            backgroundImage.color = normalColor;
        }

        // Hiệu ứng nhích nhẹ toàn bộ ô để tăng cảm giác vật lý
        float targetScale = isSelected ? 1.05f : 1.0f;
        transform.DOScale(targetScale, 0.2f).SetEase(Ease.OutQuad);
    }

    public void SetPreview(bool isPreview)
    {
        if (previewOverlay == null) return;
        
        previewOverlay.transform.DOKill();

        if (isPreview)
        {
            previewOverlay.SetActive(true);
            previewOverlay.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutCubic);
        }
        else
        {
            previewOverlay.transform.DOScale(Vector3.zero, 0.15f)
                          .SetEase(Ease.InCubic)
                          .OnComplete(() => previewOverlay.SetActive(false));
        }
    }

    // Gọi hàm này khi hai số đã Match
    public int SetCleared()
    {
        Value = 0;
        selectionOverlay.SetActive(false);

        int gemType = (int)GemType;

        GemType = GemType.None;
        ApplyGemBackground(GemType);

        SetHighlight(false);
        numberText.color = matchedTextColor;

        return gemType;
    }

    public void PlayClearAnimation(float duration)
    {
        // numberText.DOKill();
        if (numberText != null) {
            numberText.DOFade(0f, duration).SetEase(Ease.InQuad);
        }
        // Tạo chuỗi animation cho Scale
        Sequence s = DOTween.Sequence();
        
        // Thu nhỏ lại từ từ để lấy đà (70%)
        s.Append(transform.DOScale(Vector3.one * 0.7f, duration * 0.3f).SetEase(Ease.OutQuad));

        // Bật mạnh lại kích thước cũ thật nhanh (30%) 
        // Dùng Ease.OutBack để nó hơi nở quá 1.0 một chút rồi mới dừng lại, cực kỳ mượt!
        s.Append(transform.DOScale(Vector3.one, duration * 0.7f).SetEase(Ease.OutBack));
    }

    // Cập nhật lại index và vị trí mới
    public void UpdateIndex(int newIndex)
    {
        this.Index = newIndex;
    }

    public void UpdateValue(int value)
    {
        Value = value;

        transform.localScale = Vector3.one;

        if (value == 0)
        {
            numberText.text = "";
        }
        else
        {
            numberText.text = value.ToString();
            numberText.color = normalTextColor;
        }

        GemType = GemType.None;
        ApplyGemBackground(GemType);
    }

    // Gem
    public void SetGem(int type)
    {
        if (type == 1) GemType = GemType.Pink;
        else if (type == 2) GemType = GemType.Orange;
        else if (type == 3) GemType = GemType.Purple;

        ApplyGemBackground(GemType);
    }

    private void ApplyGemBackground(GemType gemType)
    {
        int index = (int)gemType;

        if (gemBackgroundSprites != null && index != 0 && index < gemBackgroundSprites.Length)
        {
            var sprite = gemBackgroundSprites[index];
            backgroundImage.sprite = sprite != null ? sprite : defaultSprite;
            numberText.color = Color.white;
        } 
        else
        {
            backgroundImage.sprite = defaultSprite;
            numberText.color = normalTextColor;
        }
    }

    public void Shake()
    {
        // Nếu ô đang chạy animation biến mất thì không rung
        if (Value == 0) return; 
        
        if (activeAnimation != null) StopCoroutine(activeAnimation);
        activeAnimation = StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        // Lưu lại vị trí gốc của Text (không phải của cả Cell)
        Vector3 originalTextPos = numberText.transform.localPosition;
        
        float elapsed = 0f;
        float duration = 0.25f;    // Thời gian rung
        float magnitude = 15f;    // Độ rộng của cú rung (pixel)
        float frequency = 40f;    // Tốc độ rung (càng cao rung càng nhanh)

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // Sử dụng hàm Sin để tạo chuyển động qua lại đồng nhất
            // Sin(elapsed * frequency) sẽ trả về giá trị từ -1 đến 1 theo chu kỳ
            float xOffset = Mathf.Sin(elapsed * frequency) * magnitude;

            // Chỉ áp dụng xOffset vào trục X của numberText
            numberText.transform.localPosition = new Vector3(
                originalTextPos.x + xOffset, 
                originalTextPos.y, 
                originalTextPos.z
            );

            yield return null;
        }

        // Trả Text về vị trí chính giữa ban đầu
        numberText.transform.localPosition = originalTextPos;
        activeAnimation = null;
    }

    public void SetHint()
    {
        if (hintOverlay == null) return;

        // 1. Reset trạng thái ban đầu
        hintOverlay.transform.DOKill(); // Dừng các tween cũ nếu có
        hintOverlay.SetActive(true);
        hintOverlay.transform.localScale = Vector3.zero;

        // 2. Tween xuất hiện (Scale từ 0 lên 1)
        hintOverlay.transform.DOScale(Vector3.one * 0.8f, 0.3f)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                // 3. Khi hiện xong, bắt đầu hiệu ứng phóng to thu nhỏ 10% liên tục
                // Scale từ 1 lên 1.1 rồi quay lại (Yoyo)
                hintOverlay.transform.DOScale(Vector3.one * 1f, 0.8f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo); // -1 là lặp vô tận
            });
    }

    public void ClearHint()
    {
        // Dừng hiệu ứng loop và thu nhỏ biến mất
        if (hintOverlay == null) return;

        hintOverlay.transform.DOKill(); 
        hintOverlay.transform.DOScale(Vector3.zero, 0.2f)
            .OnComplete(() => hintOverlay.SetActive(false));
    }
}
