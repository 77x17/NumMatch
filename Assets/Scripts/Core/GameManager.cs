using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private int addButtonCounter = Constants.MAX_ADD_TIME;
    private int currentStage = 1;
    public bool IsGameActive { get; set; } = true;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    public void StartGame()
    {
        currentStage = 0;
        GenerateNewStage();
    }

    public void Replay()
    {
        if (BoardManager.Instance.IsAnimating) return;
        
        BoardManager.Instance.ClearBoardForReplay();
        StartGame();
    }

    public void GenerateNewStage()
    {
        ++currentStage;
        UIManager.Instance.UpdateStageText(currentStage);

        addButtonCounter = Constants.MAX_ADD_TIME;
        UIManager.Instance.UpdateAddNumberText(addButtonCounter);

        int totalCells = Constants.START_ROWS * Constants.COLUMNS;
        do {
            List<int> tempBoard = new List<int>();
            for (int i = 0; i < totalCells; ++i) tempBoard.Add(-1);

            int[] numberCounts = new int[Constants.COLUMNS + 1];
            for (int i = 1; i <= Constants.COLUMNS; ++i) 
                numberCounts[i] = Constants.START_ROWS;

            int targetPairs = (currentStage == 1) ? 3 : ((currentStage == 2) ? 2 : 1);
            
            do {
                for (int p = 0; p < targetPairs; ++p)
                {
                    GenerateManager.Instance.PlaceRandomMatch(tempBoard, numberCounts);
                }
            }
            while (MatchManager.Instance.CountMatchPairs(tempBoard) != targetPairs);

            int finalStep = CalcFinalStep(tempBoard);   
            float endTime = Time.realtimeSinceStartup + 1.0f;
            if (GenerateManager.Instance.FillRemainingCells(tempBoard, numberCounts, 0, finalStep, endTime))
            {
                bool[] gemStatus = GemManager.Instance.GenerateGems(0, tempBoard);

                // Yêu cầu BoardManager hiển thị các ô mới lên màn hình
                BoardManager.Instance.RenderNewStage(tempBoard, gemStatus);
                
                Debug.Log($"[Generate New Stage] - Stage: {currentStage} with { MatchManager.Instance.CountMatchPairs(tempBoard) } pairs.");
                break;
            }
            else
            {
                Debug.Log($"[Generate New Stage] - Failed with { targetPairs } pairs.");
            }
        }
        while (true);
    }

    private int CalcFinalStep(List<int> board)
    {
        int count = 0;
        for (int i = 0; i < board.Count; i++)
        {
            if (board[i] == -1) ++count;
        }
        return count;
    }

    public void FinalizeTurnCheck(List<int> boardData)
    {
        if (CheckWin())
        {
            UIManager.Instance.ShowWinScreen();
        }
        else if (addButtonCounter == 0 && !CheckFinishedStage(boardData) && CheckLose(boardData))
        {
            UIManager.Instance.ShowLoseScreen();
        }
    }

    public bool CheckFinishedStage(List<int> boardData)
    {
        for (int index = 0; index < Constants.COLUMNS; ++index) 
            if (boardData[index] > 0) return false;
        return true;
    }

    private bool CheckLose(List<int> boardData)
    {
        for (int i = 0; i < boardData.Count; ++i) if (boardData[i] > 0)
        {
            for (int j = i + 1; j < boardData.Count; ++j) if (boardData[j] > 0) 
            {
                if (MatchManager.Instance.PreMatch(i, j, boardData) && MatchManager.Instance.CanMatch(i, j, boardData))
                {
                    return false;
                }
            }
        }
        return true;    
    }

    private bool CheckWin()
    {
        return GemManager.Instance.CheckWin();
    }

    public void HandleAddMoreNumbers(List<int> boardData)
    {
        AudioManager.Instance.PlaySound(AudioManager.AudioType.Pop);

        if (addButtonCounter <= 0) return;

        List<int> remaining = new List<int>();
        foreach (int value in boardData)
        {
            if (value > 0) remaining.Add(value);
        }

        if (remaining.Count == 0) return;

        int startIndex = 0;
        while (startIndex < boardData.Count && boardData[startIndex] != 0)
        {
            ++startIndex;
        }

        bool[] gemStatus = GemManager.Instance.GenerateGems(startIndex, remaining);

        --addButtonCounter;
        UIManager.Instance.UpdateAddNumberText(addButtonCounter);
        
        // Gọi BoardManager chạy hiệu ứng và sau khi chạy xong thì Callback lại để GameManager xử lý tiếp
        BoardManager.Instance.ExecuteAddNumbersVFX(remaining, gemStatus, startIndex);
    }

    // Hàm Callback được gọi sau khi Coroutine bên VFXManager chạy xong
    public void OnAddNumbersFinished(List<int> remaining)
    {
        Debug.Log($"[AddMoreNumbers Trigger] - Total valid numbers: { remaining.Count * 2 }");
        
        if (addButtonCounter == 0 && CheckLose(BoardManager.Instance.GetBoardData()))
        {
            UIManager.Instance.ShowLoseScreen();
        }
    }
}