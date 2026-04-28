using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

public enum GemType { None, Pink, Orange, Purple };

public class CellView : MonoBehaviour {
    [Header("References")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_Text numberText;
    [SerializeField] private Button button;

    [Header("Visual Config")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.yellow;
    [SerializeField] private Color clearedColor = new Color(1.0f, 0, 0, 0);

    [Header("Gem Backgrounds")]
    [SerializeField] private Sprite[] gemBackgroundSprites;

    [Header("Clear Animation")]
    [SerializeField] private float clearAnimDuration = 0.35f;
    [SerializeField] private float revealAnimDuration = 0.2f;

    private Sprite defaultSprite;

    [Header("Text Color Config")]
    [SerializeField] private Color normalTextColor = new Color(41f/255f, 92f/255f, 106f/255f, 1f);
    [SerializeField] private Color matchedTextColor = new Color(0.5f, 0.5f, 0.5f, 1.0f);
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
            button.interactable = false;
        }
        else
        {
            numberText.text = value.ToString();
            numberText.color = normalTextColor;
            button.interactable = true;
        }
        SetHighlight(false);

        button.onClick.RemoveAllListeners();
        // Đăng ký lắng nghe HandleClick vào Button
        button.onClick.AddListener(HandleClick);

        GemType = GemType.None;
        ApplyGemBackground(GemType);
    }

    private void HandleClick()
    {
        if (Value == 0) return;
        // Chỉ gọi nếu có người đăng ký lắng nghe
        OnCellClicked?.Invoke(Index);
    }

    public void SetHighlight(bool isSelected)
    {
        backgroundImage.color = isSelected ? selectedColor : normalColor;
    }

    // Gọi hàm này khi hai số đã Match
    public int SetCleared()
    {
        Value = 0;
        backgroundImage.color = normalColor;
        backgroundImage.raycastTarget = false;
        // button.interactable = false;

        int gemType = (int)GemType;

        GemType = GemType.None;
        ApplyGemBackground(GemType);

        // Dừng coroutine cũ nếu đang chạy, rồi bắt đầu animation mới
        if (activeAnimation != null) StopCoroutine(activeAnimation);
        activeAnimation = StartCoroutine(ClearAnimation());

        return gemType;
    }

    private IEnumerator ClearAnimation()
    {
        // ── PHASE 1: Thu nhỏ + mờ dần ──────────────────────────────
        float elapsed = 0f;
        Color bgColor  = backgroundImage.color;
        Color txtColor = numberText.color;

        while (elapsed < clearAnimDuration)
        {
            elapsed += Time.deltaTime;
            float t     = Mathf.Clamp01(elapsed / clearAnimDuration);
            float eased = 1f - Mathf.Pow(1f - t, 2f); // ease-out quad

            transform.localScale   = Vector3.one * Mathf.Lerp(1f, 0f, eased);
            backgroundImage.color  = new Color(bgColor.r,  bgColor.g,  bgColor.b,  1f - eased);
            numberText.color       = new Color(txtColor.r, txtColor.g, txtColor.b, 1f - eased);

            yield return null;
        }

        // Khoá chắc trạng thái giữa 2 phase
        transform.localScale  = Vector3.zero;
        backgroundImage.color = new Color(bgColor.r,  bgColor.g,  bgColor.b,  0f);
        numberText.color      = new Color(txtColor.r, txtColor.g, txtColor.b, 0f);

        // ── Đổi sang khung nền "cleared" ở đây (scale = 0, alpha = 0) ──
        backgroundImage.sprite = defaultSprite;          // hoặc sprite "cleared" tuỳ ý
        backgroundImage.color  = new Color(normalColor.r, normalColor.g, normalColor.b, 0f);
        numberText.color       = new Color(matchedTextColor.r, matchedTextColor.g, matchedTextColor.b, 0f);

        // ── PHASE 2: Phóng to + hiện lại ───────────────────────────
        elapsed = 0f;
        while (elapsed < revealAnimDuration)
        {
            elapsed += Time.deltaTime;
            float t     = Mathf.Clamp01(elapsed / revealAnimDuration);
            float eased = 1f - Mathf.Pow(1f - t, 2f); // ease-out quad

            transform.localScale  = Vector3.one * Mathf.Lerp(0f, 1f, eased);
            backgroundImage.color = new Color(normalColor.r, normalColor.g, normalColor.b, eased);
            numberText.color      = new Color(matchedTextColor.r, matchedTextColor.g, matchedTextColor.b, eased);

            yield return null;
        }

        // Khoá chắc trạng thái cuối
        button.interactable = false;
        backgroundImage.raycastTarget = true;
        transform.localScale  = Vector3.one;
        backgroundImage.color = normalColor;
        numberText.color      = matchedTextColor;

        activeAnimation = null;
    }

    // Chỉ fade out, không fade in - dùng cho clear line
    public IEnumerator FadeOutOnly(float duration)
    {
        // Dừng ClearAnimation (hoặc bất kỳ animation nào) trước khi chạy
        if (activeAnimation != null)
        {
            StopCoroutine(activeAnimation);
            activeAnimation = null;
        }

        float elapsed = 0f;
        Color bgColor  = backgroundImage.color;
        Color txtColor = numberText.color;
        float startScale = transform.localScale.x;

        while (elapsed < duration)
        {
            // Dừng ngay nếu object đã bị destroy
            if (this == null || gameObject == null) yield break;

            elapsed += Time.deltaTime;
            float t     = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 2f);

            transform.localScale  = Vector3.one * Mathf.Lerp(startScale, 0f, eased);
            backgroundImage.color = new Color(bgColor.r,  bgColor.g,  bgColor.b,  Mathf.Lerp(1,  0f, eased));
            numberText.color      = new Color(txtColor.r, txtColor.g, txtColor.b, Mathf.Lerp(txtColor.a, 0f, eased));

            yield return null;
        }

        if (this == null || gameObject == null) yield break;

        transform.localScale  = Vector3.zero;
        backgroundImage.color = new Color(bgColor.r, bgColor.g, bgColor.b, 0f);
        numberText.color      = new Color(txtColor.r, txtColor.g, txtColor.b, 0f);
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
            button.interactable = false;
        }
        else
        {
            numberText.text = value.ToString();
            numberText.color = normalTextColor;
            button.interactable = true;
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
}
