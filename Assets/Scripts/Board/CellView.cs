using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

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

    private Sprite defaultSprite;

    [Header("Text Color Config")]
    [SerializeField] private Color normalTextColor = new Color(41f/255f, 92f/255f, 106f/255f, 1f);
    [SerializeField] private Color matchedTextColor = new Color(0.5f, 0.5f, 0.5f, 1.0f);
    public int Index { get; private set; }
    public int Value { get; private set; }

    // Event - BoardManager lắng nghe sự kiện này
    public event Action<int> OnCellClicked;

    public GemType GemType { get; private set; }

    // Start is called before the first frame update
    public void Init(int index, int value)
    {
        Index = index;
        Value = value;

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

        button.interactable = false;

        int gemType = (int)GemType;

        GemType = GemType.None;
        ApplyGemBackground(GemType);

        numberText.color = matchedTextColor;

        return gemType;
    }

    // Cập nhật lại index và vị trí mới
    public void UpdateIndex(int newIndex)
    {
        this.Index = newIndex;
    }

    public void UpdateValue(int value)
    {
        Value = value;

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
