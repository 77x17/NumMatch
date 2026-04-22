using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class CellView : MonoBehaviour {
    [Header("References")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_Text numberText;
    [SerializeField] private Button button;

    [Header("Visual Config")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.yellow;
    [SerializeField] private Color clearedColor = new Color(1.0f, 0, 0, 0);

    private Color normalTextColor = Color.black;
    private Color matchedTextColor = new Color(0.5f, 0.5f, 0.5f, 1.0f);

    public int Index { get; private set; }
    public int Value { get; private set; }

    // Event - BoardManager lắng nghe sự kiện này
    public event Action<int> OnCellClicked;

    // Start is called before the first frame update
    public void Init(int index, int value)
    {
        Index = index;
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
        SetHighlight(false);

        button.onClick.RemoveAllListeners();
        // Đăng ký lắng nghe HandleClick vào Button
        button.onClick.AddListener(HandleClick);
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
    public void SetCleared()
    {
        Value = 0;
        backgroundImage.color = normalColor;
        numberText.color = matchedTextColor;
        button.interactable = false;
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
    }
}
