using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private GameObject cellPrefab; 
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform contentParent; 
    [SerializeField] private RectTransform viewportRect; 
    [SerializeField] private GridLayoutGroup gridLayout;  

    private List<int> boardData = new List<int>();
    private List<CellView> cellViews = new List<CellView>();

    private int firstSelected = -1;
    private int secondSelected = -1;

    public bool IsAnimating { get; private set; } = false;

    // Cho phép GameManager đọc boardData
    public List<int> GetBoardData() => boardData;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    void Update()
    {
        if (GameManager.Instance.IsGameActive && !IsAnimating)
        {
            HintManager.Instance.HandleIdleTimer(boardData, cellViews);
        }
    }

    void Start()
    {
        StartCoroutine(InitAfterLayout());
    }

    public void Replay()
    {
        ClearBoardForReplay();
        GameManager.Instance.StartGame();
    }

    IEnumerator InitAfterLayout()
    {
        yield return null;
        yield return null;
        yield return new WaitForEndOfFrame();

        SetupGridCellSize();

        // Giao lại quyền StartGame cho GameManager
        ClearBoardForReplay();
        GameManager.Instance.StartGame();
    }

    private void SetupGridCellSize()
    {
        Canvas.ForceUpdateCanvases();
        
        float viewportWidth = viewportRect.rect.width;
        float spacing = gridLayout.spacing.x;
        
        float cellSize = (viewportWidth - spacing * (Constants.COLUMNS - 1) - 20) / Constants.COLUMNS;
        gridLayout.cellSize = new Vector2(cellSize, cellSize);

        viewportRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, viewportWidth);
    }

    // Hàm gọi từ nút UI hoặc từ GameManager để dọn dẹp bảng
    public void ClearBoardForReplay()
    {
        boardData.Clear();
        for (int i = 0; i < 9 * Constants.COLUMNS; ++i)
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

        GemManager.Instance.Init();

        firstSelected  = -1;
        secondSelected = -1;

        HintManager.Instance.ClearHint(cellViews);
    }

    // GameManager gọi hàm này để kích hoạt coroutine hiển thị stage mới
    public void RenderNewStage(List<int> tempBoard, bool[] gemStatus)
    {
        StartCoroutine(VFXManager.Instance.DisplayCellsSequentially(tempBoard, gemStatus, 0, cellViews));
    }

    // Hàm này kết nối Coroutine sinh UI với logic kết thúc của GameManager
    public void ExecuteAddNumbersVFX(List<int> remaining, bool[] gemStatus, int startIndex)
    {
        StartCoroutine(VFXManager.Instance.DisplayCellsSequentially(remaining, gemStatus, startIndex, cellViews, () => 
        {
            GameManager.Instance.OnAddNumbersFinished(remaining);
        }));
    }

    public void UpdateBoard()
    {
        int lastValue = -1;
        for (int i = boardData.Count - 1; i >= 0; --i)
        {
            if (boardData[i] != 0) {
                lastValue = i;
                break;
            }
        }

        while (boardData.Count < 9 * Constants.COLUMNS || (lastValue != -1 && boardData.Count - lastValue - 1 < 3 * Constants.COLUMNS))
        {
            CreateNewLine();
        }

        while (boardData.Count % 9 != 0)
        {
            boardData.Add(0);
            AddCell(0);
        }

        UpdateScrollState(lastValue, boardData.Count);
        UIManager.Instance.UpdatePairsText(MatchManager.Instance.CountMatchPairs(boardData));
    }

    public void UpdateScrollState(int lastValue, int boardSize)
    {
        if (scrollRect == null) return;

        if (boardSize > 81 && lastValue > 81)
        {
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
        }
        else
        {
            scrollRect.verticalNormalizedPosition = 1f;
            scrollRect.vertical = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
        }
    }

    private void AddCell(int x)
    {
        GameObject go = Instantiate(cellPrefab, contentParent);
        CellView cell = go.GetComponent<CellView>();
        cell.Init(cellViews.Count, x);
        cell.OnCellClicked += HandleCellClicked;

        cellViews.Add(cell);
    }

    public void UpdateOrAddCell(int currentIndex, int value, bool isGem)
    {
        if (currentIndex < cellViews.Count)
        {
            boardData[currentIndex] = value;
            cellViews[currentIndex].UpdateValue(value);
        }
        else
        {
            boardData.Add(value);
            AddCell(value); 
        }

        if (isGem)
        {
            cellViews[currentIndex].SetGem(GemManager.Instance.GetValidGemType());
        }
    }

    private void HandleCellClicked(int index)
    {
        HintManager.Instance.ClearHint(cellViews);

        if (IsAnimating) return; 

        AudioManager.Instance.PlaySound(AudioManager.AudioType.ChooseNumber);

        if (index == firstSelected)
        {
            Deselect(firstSelected);
            firstSelected = -1;
            SuggestManager.Instance.ResetMatchableWithSelected(cellViews);
            return;
        }

        if (firstSelected == -1)
        {
            firstSelected = index;
            cellViews[index].SetHighlight(true);
            SuggestManager.Instance.ShowMatchableWithSelected(firstSelected, boardData, cellViews);
            return;
        }

        secondSelected = index;
        cellViews[index].SetHighlight(true);

        if (MatchManager.Instance.PreMatch(firstSelected, secondSelected, boardData))
        {
            MatchManager.Instance.EvaluateMatch(ref firstSelected, ref secondSelected, boardData, cellViews);
        }
        else
        {
            Deselect(firstSelected);
            firstSelected = secondSelected;
            SuggestManager.Instance.ResetMatchableWithSelected(cellViews);
            SuggestManager.Instance.ShowMatchableWithSelected(firstSelected, boardData, cellViews);
            secondSelected = -1;
        }
    }

    public void Deselect(int index)
    {
        if (index >= 0 && index < cellViews.Count)
        {
            cellViews[index].SetHighlight(false);
        }
    }

    public void HandleDataAfterClearAnimation(int a, int b, int startA, int startB, bool clearLineA, bool clearLineB)
    {
        int endA = Mathf.Min(startA + Constants.COLUMNS, cellViews.Count);
        int endB = Mathf.Min(startB + Constants.COLUMNS, cellViews.Count);

        if (clearLineA && clearLineB && startA != startB)
        {
            int firstStart  = Mathf.Min(startA, startB);
            int secondStart = Mathf.Max(startA, startB);
            DeleteLineData(secondStart, secondStart + Constants.COLUMNS);
            DeleteLineData(firstStart , firstStart  + Constants.COLUMNS);
        }
        else if (clearLineA) DeleteLineData(startA, endA);
        else if (clearLineB) DeleteLineData(startB, endB);

        PostClearProcess(a, b, clearLineA, clearLineB);
    }

    private void DeleteLineData(int startIndex, int endIndex)
    {
        for (int i = startIndex; i < endIndex; ++i)
            if (cellViews[i] != null) Destroy(cellViews[i].gameObject);

        int count = endIndex - startIndex;
        if (count > 0)
        {
            boardData.RemoveRange(startIndex, count);
            cellViews.RemoveRange(startIndex, count);
        }

        Debug.Log($"[Clear line]: [{startIndex / Constants.COLUMNS}]: boardData.Count {boardData.Count}; cellViews.Count {cellViews.Count}");

        UpdateCellsIndex(startIndex);
    }

    public void PostClearProcess(int a, int b, bool clearLineA, bool clearLineB)
    {
        if (clearLineA || clearLineB)
        {
            UpdateBoard();
        }

        if (clearLineA && clearLineB && ((a < Constants.COLUMNS) || (b < Constants.COLUMNS)))
        {
            // Ủy quyền cho GameManager kiểm tra qua màn
            if (GameManager.Instance.CheckFinishedStage(boardData)) 
                GameManager.Instance.GenerateNewStage();
        }

        // Ủy quyền cho GameManager kiểm tra win/lose lượt
        GameManager.Instance.FinalizeTurnCheck(boardData);
    }

    private void UpdateCellsIndex(int startIndex)
    {
        for (int i = startIndex; i < cellViews.Count; ++i)
        {
            cellViews[i].UpdateIndex(i);
        }
    }

    private void CreateNewLine()
    {
        for (int i = 0; i < Constants.COLUMNS; ++i)
        {
            boardData.Add(0);
            AddCell(0);
        }
    }

    // Hàm này giữ lại để bắt sự kiện từ nút UI (On Click), nhưng đẩy logic cho GameManager xử lý
    public void HandleAddMoreNumbers()
    {
        HintManager.Instance.ClearHint(cellViews);

        if (IsAnimating) return;

        GameManager.Instance.HandleAddMoreNumbers(boardData);
    }

    public bool CanInteract() => !IsAnimating;

    public void SetAnimating(bool active)
    {
        IsAnimating = active;
    }
}