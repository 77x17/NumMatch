using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    [Header("References")]
    // SerializeField cho phép chỉnh sửa trong Unity Editor thay vì public nên vẫn đảm bảo tính đóng gói.
    [SerializeField] private GameObject cellPrefab; // Cell.prefab
    [SerializeField] private Transform contentParent; // Content

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
        for (int i = 0; i < START_ROWS * COLUMNS; ++i) {
            boardData.Add(Random.Range(1, 10));
        }
        for (int i = 0; i < (9 - START_ROWS) * COLUMNS; ++i)
        {
            boardData.Add(0);
        }

        RenderBoard();
    }

    void RenderBoard()
    {
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        cellViews.Clear();

        for (int i = 0; i < boardData.Count; ++i)
        {
            GameObject go = Instantiate(cellPrefab, contentParent);
            CellView cell = go.GetComponent<CellView>();
            
            cell.Init(i, boardData[i]);
            // Đăng ký lắng nghe OnCellClicked bằng hàm HandleCellClicked
            cell.OnCellClicked += HandleCellClicked;

            cellViews.Add(cell);
        }
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
            for (int i = a + 1; i < b; ++i) if (boardData[i] != 0) 
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
            for (int i = a + COLUMNS; i < b; i += COLUMNS) if (boardData[i] != 0) 
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
                if (boardData[i * COLUMNS + j] != 0) {
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
                if (boardData[i * COLUMNS + j] != 0)
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
            for (int i = a + 1; i < b; ++i) if (boardData[i] != 0)
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
        }
        else
        {
            Deselect(firstSelected);
            Deselect(secondSelected);
        }

        firstSelected  = -1;
        secondSelected = -1;
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
        
        boardData[a] = 0;
        boardData[b] = 0;

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
        if (boardData.Count <= 9 * COLUMNS) return true;

        return false;
    }

    private bool CheckFinishedStage()
    {
        for (int index = 0; index < COLUMNS; ++index) if (boardData[index] != 0) return false;

        return true;
    }

    private void GenerateNewStage()
    {
        Debug.Log("[Generate New Stage]");
        for (int index = 0; index < START_ROWS * COLUMNS; ++index)
        {
            boardData[index] = Random.Range(1, 10);
            cellViews[index].UpdateValue(boardData[index]);
        }
    }

    private bool ProcessClearLine(int a)
    {
        int x = a / COLUMNS;

        // Check có phải là hàng ngang trống
        for (int i = 0; i < COLUMNS; ++i) if (boardData[x * COLUMNS + i] != 0) return false;
        
        int startIndex = x * COLUMNS;
        for (int i = startIndex; i < startIndex + COLUMNS; ++i)
        {
            if (cellViews[i] != null) Destroy(cellViews[i].gameObject);
        }
        
        boardData.RemoveRange(startIndex, COLUMNS);
        cellViews.RemoveRange(startIndex, COLUMNS);

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

            GameObject go = Instantiate(cellPrefab, contentParent);
            CellView cell = go.GetComponent<CellView>();
            
            cell.Init(i, 0);
            // Đăng ký lắng nghe OnCellClicked bằng hàm HandleCellClicked
            cell.OnCellClicked += HandleCellClicked;

            cellViews.Add(cell);
        }
    }

    public void AddMoreNumbers()
    {
        Debug.Log("[AddMoreNumbers Trigger]");
        List<int> remaining = new List<int>();
        foreach (int value in boardData)
        {
            if (value != 0) remaining.Add(value);
        }

        if (remaining.Count == 0) return;

        int startIndex = 0, validNumberCount = 0;
        while (validNumberCount < remaining.Count)
        {
            if (boardData[startIndex] != 0)
            {
                ++validNumberCount;
            }
            ++startIndex;
        }

        for (int i = 0; i < remaining.Count; ++i)
        {
            boardData[startIndex + i] = remaining[i];
            cellViews[startIndex + i].UpdateValue(remaining[i]);
        }
    }
}
