using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SuggestManager : MonoBehaviour
{
    public static SuggestManager Instance { get; private set; }

    private int[] matchableWithSelected;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        
        // Nếu muốn tồn tại xuyên suốt các Scene:
        // DontDestroyOnLoad(this.gameObject); 

        matchableWithSelected = new int[8];
        for (int i = 0; i < 8; ++i) matchableWithSelected[i] = -1;
    }

    private bool IsAlreadyAdded(int index, int currentCount)
    {
        for (int i = 0; i < currentCount; i++)
        {
            if (matchableWithSelected[i] == index) return true;
        }
        return false;
    }
    private void GetMatchableWithSelected(int firstSelected, List<int> boardData)
    {
        if (firstSelected == -1) return;

        int count = 0;
        int totalCells = boardData.Count;
        int r = firstSelected / Constants.COLUMNS, c = firstSelected % Constants.COLUMNS;
        
        // Index
        for (int index = firstSelected - 1; index >= 0; --index)
        {
            if (boardData[index] > 0)
            {
                matchableWithSelected[count++] = index;
                break;
            }
        }

        for (int index = firstSelected + 1; index < totalCells; ++index)
        {
            if (boardData[index] > 0)
            {
                matchableWithSelected[count++] = index;
                break;
            }
        }

        // Ngang
        int anchor = r * 9;
        for (int index = firstSelected - 1; index >= anchor; --index)
        {
            if (boardData[index] > 0) 
            {
                if (!IsAlreadyAdded(index, count)) matchableWithSelected[count++] = index;
                break;
            }
        }
        anchor = r * 9 + 8;
        for (int index = firstSelected + 1; index <= anchor; ++index)
        {
            if (boardData[index] > 0) 
            {
                if (!IsAlreadyAdded(index, count)) matchableWithSelected[count++] = index;
                break;
            }
        }

        // Dọc
        anchor = 0;
        for (int index = firstSelected - 9; index >= anchor; index -= 9)
        {
            if (boardData[index] > 0) 
            {
                matchableWithSelected[count++] = index;
                break;
            }
        }
        anchor = totalCells - 1;
        for (int index = firstSelected + 9; index <= anchor; index += 9)
        {
            if (boardData[index] > 0) 
            {
                matchableWithSelected[count++] = index;
                break;
            }
        }
        
        // Đường chéo
        int[,] directions = { {-1, -1}, {-1, 1}, {1, -1}, {1, 1} };
        for (int d = 0; d < 4; d++)
        {
            int currR = r;
            int currC = c;
            while (true)
            {
                currR += directions[d, 0];
                currC += directions[d, 1];
                
                if (currR < 0 || currC < 0 || currC >= 9 || currR * 9 + currC >= totalCells) break;

                int index = currR * 9 + currC;
                if (boardData[index] > 0)
                {
                    matchableWithSelected[count++] = index;
                    break;
                }
            }
        }
    }
    public void ShowMatchableWithSelected(int firstSelected, List<int> boardData, List<CellView> cellViews)
    {
        if (firstSelected == -1) return;
        
        GetMatchableWithSelected(firstSelected, boardData);

        for (int i = 0; i < 8; ++i)
        {
            if (matchableWithSelected[i] == -1) continue;

            if (!nearFirstSelected(matchableWithSelected[i], firstSelected)) {
                cellViews[matchableWithSelected[i]].SetPreview(true);
            }
        }
    }
    private bool nearFirstSelected(int index, int firstSelected)
    {
        if (firstSelected == -1) return true;

        if (index == firstSelected - 1 || index == firstSelected + 1 || index == firstSelected - 9  || index == firstSelected + 9 || 
            index == firstSelected - 8 || index == firstSelected + 8 || index == firstSelected - 10 || index == firstSelected + 10)
        {
            return true;
        }
        return false;
    }
    public void ResetMatchableWithSelected(List<CellView> cellViews)
    {
        for (int i = 0; i < 8; ++i)
        {
            if (matchableWithSelected[i] == -1) continue;

            cellViews[matchableWithSelected[i]].SetPreview(false);

            matchableWithSelected[i] = -1;
        }
    }
}
