using System.Collections.Generic;
using UnityEngine;

public class HintManager : MonoBehaviour
{
    public static HintManager Instance { get; private set; }

    private float hintDelay = 10f;
    private float idleTimer = 0f;
    private int[] hintIndex = { -1, -1 };

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
    public void ShowHint(List<int> boardData, List<CellView> cellViews)
    {
        (hintIndex[0], hintIndex[1]) = MatchManager.Instance.FindMatchablePair(boardData);

        if (hintIndex[0] == -1 || hintIndex[1] == -1) return;

        Debug.Log($"[Show hint] - {hintIndex[0]} and {hintIndex[1]}.");

        if (hintIndex[0] < cellViews.Count) {
            cellViews[hintIndex[0]].SetHint();
        }
        if (hintIndex[1] < cellViews.Count) {
            cellViews[hintIndex[1]].SetHint();
        }
    }
    public void ClearHint(List<CellView> cellViews)
    {
        idleTimer = 0f;

        if (hintIndex[0] == -1 || hintIndex[1] == -1) return;

        if (hintIndex[0] < cellViews.Count)
        {
            cellViews[hintIndex[0]].ClearHint();
        }
        if (hintIndex[1] < cellViews.Count) {
            cellViews[hintIndex[1]].ClearHint();
        }

        hintIndex[0] = -1;
        hintIndex[1] = -1;
    }
    public void HandleIdleTimer(List<int> boardData, List<CellView> cellViews)
    {
        idleTimer += Time.deltaTime;
        if (idleTimer >= hintDelay)
        {
            ShowHint(boardData, cellViews);
            idleTimer = 0f;
        }
    }
}