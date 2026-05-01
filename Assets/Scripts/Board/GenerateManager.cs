using System.Collections.Generic;
using UnityEngine;

public class GenerateManager : MonoBehaviour
{
    public static GenerateManager Instance { get; private set; }

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

    private void Shuffle<T>(IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = UnityEngine.Random.Range(0, n + 1); 
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
    public void PlaceRandomMatch(List<int> boardData, int[] numberCounts)
    {
        List<int> emptyIndices = new List<int>();
        for (int i = 0; i < boardData.Count; i++) if (boardData[i] == -1) emptyIndices.Add(i);
        Shuffle(emptyIndices);
        
        foreach (int firstIndex in emptyIndices)
        {
            var neighbors = MatchManager.Instance.GetNeighbors(firstIndex);
            foreach (int secondIndex in neighbors)
            {
                if (boardData[secondIndex] == -1)
                {
                    for (int v = 1; v <= Constants.COLUMNS; v++)
                    {
                        int value  = Random.Range(1, 10);
                        int option = Random.Range(0, 2);
                        if (option == 0) {
                            if (numberCounts[value] >= 1 && numberCounts[10 - value] >= 1)
                            {
                                boardData[firstIndex ] = value;
                                boardData[secondIndex] = 10 - value;
                                --numberCounts[value];
                                --numberCounts[10 - value];
                                return;
                            }    
                            else if (numberCounts[value] >= 2)
                            {
                                boardData[firstIndex ] = value;
                                boardData[secondIndex] = value;
                                numberCounts[value] -= 2;
                                return;
                            }
                        }
                        else
                        {
                            if (numberCounts[value] >= 2)
                            {
                                boardData[firstIndex ] = value;
                                boardData[secondIndex] = value;
                                numberCounts[value] -= 2;
                                return;
                            }
                            else if (numberCounts[value] >= 1 && numberCounts[10 - value] >= 1)
                            {
                                boardData[firstIndex ] = value;
                                boardData[secondIndex] = 10 - value;
                                --numberCounts[value];
                                --numberCounts[10 - value];
                                return;
                            }    
                        }
                    }
                }
            }
        }
    }
    private int CountFilledNeighbors(int index, List<int> boardData)
    {
        int count = 0;
        List<int> neighbors = MatchManager.Instance.GetNeighbors(index);
        foreach (int n in neighbors)
        {
            if (boardData[n] != -1) ++count;
        }
        return count;
    }
    private int GetMostOptimizedIndex(List<int> boardData)
    {
        int result = -1, neighborCountA = 0;
        for (int i = 0; i < boardData.Count; i++)
        {
            if (boardData[i] == -1) {
                if (result == -1) {
                    result = i;
                    neighborCountA = CountFilledNeighbors(i, boardData);
                }
                else
                {
                    int neighborCountB = CountFilledNeighbors(i, boardData);
                    if (neighborCountB > neighborCountA)
                    {
                        result = i;
                        neighborCountA = neighborCountB;
                    }
                }
            }
        }
        return result;
    }
    private int GetAvailableMask(int[] numberCounts, List<int> boardData, int index)
    {
        int forbiddenMask = 0;
        List<int> neighbors = MatchManager.Instance.GetNeighbors(index);

        foreach (int n in neighbors)
        {
            int val = boardData[n];
            if (val != -1) 
            {
                forbiddenMask |= (1 << val);
            }
        }

        int availableMask = 0;
        for (int i = 1; i <= Constants.COLUMNS; i++)
        {
            if (numberCounts[i] > 0 && 
                (forbiddenMask & (1 << i)) == 0 && 
                (forbiddenMask & (1 << (10 - i))) == 0)
            {
                availableMask |= (1 << i);
            }
        }
        return availableMask;
    }
    public bool FillRemainingCells(List<int> boardData, int[] numberCounts, int step, int finalStep, float endTime)
    {
        if (Time.realtimeSinceStartup > endTime) return false;

        if (step >= finalStep) return true;

        int currentIndex = GetMostOptimizedIndex(boardData);

        int availableMask = GetAvailableMask(numberCounts, boardData, currentIndex);
        if (availableMask == 0) return false;

        for (int number = 1; number <= Constants.COLUMNS; ++number) if ((availableMask & (1 << number)) != 0)
        {
            boardData[currentIndex] = number;
            --numberCounts[number];
            
            if (FillRemainingCells(boardData, numberCounts, step + 1, finalStep, endTime)) return true;

            ++numberCounts[number];
            boardData[currentIndex] = -1;
        } 

        return false;
    }
}