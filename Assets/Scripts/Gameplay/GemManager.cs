using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GemManager : MonoBehaviour
{
    public static GemManager Instance { get; private set; }

    private const int TARGET_PINK_GEMS   = 5;
    private const int TARGET_ORANGE_GEMS = 5;
    private const int TARGET_PURPLE_GEMS = 5;

    private int currentPinkGems   = 0;
    private int currentOrangeGems = 0;
    private int currentPurpleGems = 0;

    [Header("UI Reference")]
    public TextMeshProUGUI pinkGemsText, orangeGemsText, purpleGemsText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;   
        }
        Instance = this;
    }

    public void Init()
    {
        currentPinkGems   = 0;
        currentOrangeGems = 0;
        currentPurpleGems = 0;

        pinkGemsText.text   = Mathf.Max(TARGET_PINK_GEMS   - currentPinkGems  , 0).ToString();
        orangeGemsText.text = Mathf.Max(TARGET_ORANGE_GEMS - currentOrangeGems, 0).ToString();
        purpleGemsText.text = Mathf.Max(TARGET_PURPLE_GEMS - currentPurpleGems, 0).ToString();
    }

    public void UpdateStatus(int gemType)
    {
        if (gemType != 0)
        {
            if (gemType == 1) ++currentPinkGems;
            else if (gemType == 2) ++currentOrangeGems;
            else if (gemType == 3) ++currentPurpleGems;
        }

        pinkGemsText.text   = Mathf.Max(TARGET_PINK_GEMS   - currentPinkGems  , 0).ToString();
        orangeGemsText.text = Mathf.Max(TARGET_ORANGE_GEMS - currentOrangeGems, 0).ToString();
        purpleGemsText.text = Mathf.Max(TARGET_PURPLE_GEMS - currentPurpleGems, 0).ToString();
    }

    public int GetValidGemType()
    {
        List<int> gemsType = new List<int>();
        if (TARGET_PINK_GEMS   - currentPinkGems   > 0) gemsType.Add(1);
        if (TARGET_ORANGE_GEMS - currentOrangeGems > 0) gemsType.Add(2);
        if (TARGET_PURPLE_GEMS - currentPurpleGems > 0) gemsType.Add(3);
        
        if (gemsType.Count == 0) return 0;

        return gemsType[Random.Range(0, gemsType.Count)];
    }

    public bool CheckWin()
    {
        if (currentPinkGems   >= TARGET_PINK_GEMS   && 
            currentOrangeGems >= TARGET_ORANGE_GEMS && 
            currentPurpleGems >= TARGET_PURPLE_GEMS)
        {
            return true;
        }

        return false;
    }

    public bool[] GenerateGems(int startIndex, List<int> addNumbers)
    {
        bool[] gemStatus = new bool[addNumbers.Count];

        int X = Random.Range(5, 8);
        int Y = Mathf.CeilToInt((addNumbers.Count + 1) / 2.0f);
        int Z = Mathf.Max(TARGET_PINK_GEMS - currentPinkGems, 0) + 
                Mathf.Max(TARGET_ORANGE_GEMS - currentOrangeGems, 0) + 
                Mathf.Max(TARGET_PURPLE_GEMS - currentPurpleGems, 0);

        if (Z <= 0) return gemStatus;
        
        int gemCount = 0;
        int countSinceLastGem = 0;

        for (int i = 0; i < addNumbers.Count; ++i)
        {
            if (gemCount >= Z) break;

            ++countSinceLastGem;
            bool isLastChanceInWindow = countSinceLastGem >= Y;

            bool rolledGem = Random.Range(0, 100) < X;
            if (rolledGem || isLastChanceInWindow)
            {
                int candidateIdx = FindValidGemCandidate(
                    addNumbers,
                    i,
                    startIndex,
                    gemStatus
                );

                if (candidateIdx >= 0)
                {
                    gemStatus[candidateIdx] = true;
                    ++gemCount;
                    countSinceLastGem = 0;
                }
            }
        }

        return gemStatus;
    }

    public int FindValidGemCandidate(
        List<int> addNumbers,
        int currentIndex,
        int startIndex,
        bool[] gemStatus)
    {
        startIndex %= 9;
        List<int> neighbors = MatchManager.Instance.GetNeighborIndices(startIndex + currentIndex, startIndex + addNumbers.Count);
        foreach (int n in neighbors) if (0 <= n - startIndex && n - startIndex < addNumbers.Count && gemStatus[n - startIndex] == true)
        {
            if (MatchManager.Instance.PreMatch(currentIndex, n - startIndex, addNumbers) && 
                MatchManager.Instance.CanMatch(currentIndex, n - startIndex, addNumbers))
            {
                return -1;                
            }
        }
        return currentIndex;
    }
}