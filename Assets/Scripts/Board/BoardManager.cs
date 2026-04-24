using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BoardManager : MonoBehaviour
{
    [Header("References")]
    // SerializeField cho phép chỉnh sửa trong Unity Editor thay vì public nên vẫn đảm bảo tính đóng gói.
    [SerializeField] private GameObject cellPrefab; // Cell.prefab
    [SerializeField] private Transform contentParent; // Content
    [SerializeField] private TextMeshProUGUI addButtonNumberText; // Add button
    [SerializeField] private TextMeshProUGUI stageText;
    [SerializeField] private GameObject losePanel;
    private const int MAX_ADD_TIME = 6;
    private int addButtonCounter = MAX_ADD_TIME;
    private int currentStage = 1;

    // Danh sách các số ở trên board theo mảng 1 chiều
    private List<int> boardData = new List<int>();
    private List<CellView> cellViews = new List<CellView>();

    private int firstSelected = -1;
    private int secondSelected = -1;

    private const int START_ROWS = 3;
    private const int COLUMNS = 9;

    // Start is called before the first frame update
    void Start()
    {
        boardData.Clear();
        for (int i = 0; i < 9 * COLUMNS; ++i)
        {
            boardData.Add(0);
        }

        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        cellViews.Clear();
        foreach(int x in boardData) {
            AddCell(x);
        }

        currentStage = 0;

        GenerateNewStage();
    }

    private void AddCell(int x)
    {
        GameObject go = Instantiate(cellPrefab, contentParent);
        CellView cell = go.GetComponent<CellView>();

        cell.Init(cellViews.Count, x);
        // Đăng ký lắng nghe OnCellClicked bằng hàm HandleCellClicked
        cell.OnCellClicked += HandleCellClicked;

        cellViews.Add(cell);
    }

    private void GenerateNewStage()
    {
        Debug.Log("[Generate New Stage]");

        addButtonCounter         = MAX_ADD_TIME;
        addButtonNumberText.text = addButtonCounter.ToString();

        for (int index = 0; index < START_ROWS * COLUMNS; ++index)
        {
            boardData[index] = Random.Range(1, 10);
            cellViews[index].UpdateValue(boardData[index]);
        }

        ++currentStage;
        stageText.text = $"Stage: {currentStage}";
    }

    private void HandleCellClicked(int index)
    {
        if (index == firstSelected)
        {
            Deselect(firstSelected);
            firstSelected = -1;
            return;
        }

        if (firstSelected == -1)
        {
            firstSelected = index;
            cellViews[index].SetHighlight(true);
            return;
        }

        secondSelected = index;
        cellViews[index].SetHighlight(true);

        if (PreMatch(firstSelected, secondSelected))
        {
            EvaluateMatch();
        }
        else
        {
            Deselect(firstSelected);
            firstSelected  = secondSelected;
            secondSelected = -1;
        }
    }

    private bool PreMatch(int a, int b)
    {
        bool isSame     = boardData[a] == boardData[b];
        bool isSumOfTen = (boardData[a] + boardData[b]) == 10;
        return isSame || isSumOfTen;
    }

    private bool CanMatch(int a, int b)
    {
        // Chắc chắn rằng a nhỏ hơn b
        if (a > b) return CanMatch(b, a);

        int[] x = { a / COLUMNS, b / COLUMNS };
        int[] y = { a % COLUMNS, b % COLUMNS };

        // Kiểm tra theo chiều ngang
        if (x[0] == x[1]) 
        {
            bool matchFound = true;
            for (int i = a + 1; i < b; ++i) if (boardData[i] > 0) 
            {
                matchFound = false;
                break;
            }
            if (matchFound) return true;
        }

        // Kiểm tra theo chiều dọc
        if (y[0] == y[1])
        {
            bool matchFound = true;
            for (int i = a + COLUMNS; i < b; i += COLUMNS) if (boardData[i] > 0) 
            {
                matchFound = false;
                break;
            }
            if (matchFound) return true;
        }

        // Kiểm tra đường chéo chính
        if (x[0] - y[0] == x[1] - y[1])
        {
            bool matchFound = true;
            int i = x[0] + 1, j = y[0] + 1;
            while (i != x[1] && j != y[1])
            {
                if (boardData[i * COLUMNS + j] > 0) {
                    matchFound = false;
                    break;
                }
                ++i; ++j;
            }
            if (matchFound) return true;
        }

        // Kiểm tra đường chéo phụ
        if (x[0] + y[0] == x[1] + y[1])
        {
            bool matchFound = true;
            int i = x[0] + 1, j = y[0] - 1;
            while (i != x[1] && j != y[1])
            {
                if (boardData[i * COLUMNS + j] > 0)
                {
                    matchFound = false;
                    break;
                }
                ++i; --j;
            }
            if (matchFound) return true;
        }

        // Kiểm tra đường ngang tăng dần
        {
            bool matchFound = true;
            for (int i = a + 1; i < b; ++i) if (boardData[i] > 0)
            {
                matchFound = false;
                break;
            }
            if (matchFound) return true;
        }

        return false;
    }

    private void EvaluateMatch()
    {
        bool matched = CanMatch(firstSelected, secondSelected);

        if (matched)
        {
            if (firstSelected < secondSelected)
            {
                ProcessMatch(secondSelected, firstSelected);
            }
            else {
                ProcessMatch(firstSelected, secondSelected);
            }

            if (addButtonCounter == 0)
            {
                if (CheckLose())
                {
                    ShowLoseScreen();
                }
            }
        }
        else
        {
            Deselect(firstSelected);
            Deselect(secondSelected);
        }

        firstSelected  = -1;
        secondSelected = -1;
    }

    private bool CheckLose()
    {
        for (int i = 0; i < boardData.Count; ++i) if (boardData[i] > 0)
        {
            for (int j = i + 1; j < boardData.Count; ++j) if (boardData[j] > 0) 
            {
                if (PreMatch(i, j) && CanMatch(i, j))
                {
                    return false;
                }
            }
        }

        return true;    
    }

    private void ShowLoseScreen()
    {
        losePanel.SetActive(true);
        Time.timeScale = 0.0f;
    }

    public void OnReplayButton()
    {
        Time.timeScale = 1.0f;
        losePanel.SetActive(false);

        Start();
    }

    private void Deselect(int index)
    {
        if (index >= 0 && index < cellViews.Count)
        {
            cellViews[index].SetHighlight(false);
        }
    }

    private void ProcessMatch(int a, int b)
    {
        Debug.Log($"Matched: [{ a }] = { boardData[a] } & [{ b }] = { boardData[b] }");
        
        cellViews[a].SetCleared();
        cellViews[b].SetCleared();
        
        boardData[a] = -1;
        boardData[b] = -1;

        bool clearLineA = ProcessClearLine(a);
        bool clearLineB = ProcessClearLine(b);

        if (clearLineA || clearLineB)
        {
            while (ShouldAddNewLine())
            {
                CreateNewLine();
            }
        }
        
        if (clearLineA && clearLineB && ((a < COLUMNS) || (b < COLUMNS))) 
        {
            if (CheckFinishedStage())
            {
                GenerateNewStage();
            }
        }
    }

    private bool ShouldAddNewLine()
    {
        if (boardData.Count < 9 * COLUMNS) return true;

        return false;
    }

    private bool CheckFinishedStage()
    {
        for (int index = 0; index < COLUMNS; ++index) if (boardData[index] > 0) return false;

        return true;
    }

    private bool ProcessClearLine(int a)
    {
        int x = a / COLUMNS;
        int startIndex = x * COLUMNS;

        if (startIndex < 0 || startIndex >= boardData.Count) return false;

        // Check có phải là hàng ngang trống
        int endIndex = System.Math.Min(startIndex + COLUMNS, boardData.Count);
        for (int i = startIndex; i < endIndex; ++i) 
            if (boardData[i] > 0) return false;
        
        for (int i = startIndex; i < endIndex; ++i)
            if (cellViews[i] != null) Destroy(cellViews[i].gameObject);
        
        int actualCountToDelete = endIndex - startIndex;
        if (actualCountToDelete > 0) {
            boardData.RemoveRange(startIndex, actualCountToDelete);
            cellViews.RemoveRange(startIndex, actualCountToDelete);
        }

        Debug.Log($"[Clear line]: [{ x }]: boardData.Count { boardData.Count}; cellViews.Count { cellViews.Count }");

        UpdateCellsIndex(a);

        return true;
    }

    private void UpdateCellsIndex(int a)
    {
        int startIndex = a / COLUMNS * COLUMNS;
        for (int i = startIndex; i < cellViews.Count; ++i)
        {
            cellViews[i].UpdateIndex(i);
        }
    }

    private void CreateNewLine()
    {
        for (int i = 0; i < COLUMNS; ++i)
        {
            boardData.Add(0);

            AddCell(0);
        }
    }

    public void AddMoreNumbers()
    {
        if (addButtonCounter <= 0)
        {
            return;
        }

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

        for (int i = 0; i < remaining.Count; ++i)
        {
            if (startIndex + i < boardData.Count) {
                boardData[startIndex + i] = remaining[i];
                cellViews[startIndex + i].UpdateValue(remaining[i]);
            }
            else
            {
                boardData.Add(remaining[i]);
                AddCell(remaining[i]);
            }
        }

        --addButtonCounter;
        addButtonNumberText.text = addButtonCounter.ToString();

        Debug.Log($"[AddMoreNumbers Trigger] - Total valid numbers: { remaining.Count * 2 }");

        if (addButtonCounter == 0)
        {
            if (CheckLose())
            {
                ShowLoseScreen();   
            }
        }
    }
}
