using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Texts")]
    public TextMeshProUGUI addButtonNumberText;
    public TextMeshProUGUI stageText;
    public TextMeshProUGUI pairsText;
    
    [Header("Panels")]
    public GameObject losePanel, winPanel, homePanel, settingPanel;
    public Transform rootCanvas;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        
        // Nếu muốn tồn tại xuyên suốt các Scene:
        // DontDestroyOnLoad(this.gameObject); 
    }
    public void UpdateStageText(int stage)
    {
        stageText.text = $"Stage: {stage}";
    }
    public void UpdatePairsText(int pairsLeft)
    {
        pairsText.text = $"Pairs: {pairsLeft}";
    }
    public void UpdateAddNumberText(int count)
    {
        addButtonNumberText.text = count.ToString();
    }
    public void ShowLoseScreen()
    {
        GameManager.Instance.IsGameActive = false;
        losePanel.SetActive(true);
        // Time.timeScale = 0.0f;
    }
    public void ShowWinScreen()
    {
        GameManager.Instance.IsGameActive = false;
        winPanel.SetActive(true);
        // Time.timeScale = 0.0f;
    }
    
    // Buttons (Hook in Inspector)
    public void HandleSettingButton()
    {
        if (!BoardManager.Instance.CanInteract()) return;

        GameManager.Instance.IsGameActive = false;

        AudioManager.Instance.PlaySound(AudioManager.AudioType.Pop);

        settingPanel.SetActive(true);
        // Time.timeScale = 0.0f;
    }

    public void HandleHomeButton()
    {
        if (!BoardManager.Instance.CanInteract()) return;

        GameManager.Instance.IsGameActive = false;
        
        AudioManager.Instance.PlaySound(AudioManager.AudioType.Pop);

        homePanel.SetActive(true);
        // Time.timeScale = 0.0f;
    }

    public void HandleReplayButton()
    {
        if (!BoardManager.Instance.CanInteract()) return;
        
        GameManager.Instance.IsGameActive = true;

        AudioManager.Instance.PlaySound(AudioManager.AudioType.Pop);

        losePanel.SetActive(false);
        winPanel.SetActive(false);
        homePanel.SetActive(false);
        settingPanel.SetActive(false);

        BoardManager.Instance.Replay();
        
        // Time.timeScale = 1.0f;
    }

    public void HandleBackButton()
    {
        if (!BoardManager.Instance.CanInteract()) return;

        GameManager.Instance.IsGameActive = true;

        AudioManager.Instance.PlaySound(AudioManager.AudioType.Pop);

        homePanel.SetActive(false);
        settingPanel.SetActive(false);

        // Time.timeScale = 1.0f;
    }
}