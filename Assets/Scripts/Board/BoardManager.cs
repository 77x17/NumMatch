using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class BoardManager : MonoBehaviour
{
    [Header("References")]
    // SerializeField cho phép chỉnh sửa trong Unity Editor thay vì public nên vẫn đảm bảo tính đóng gói.
    [SerializeField] private GameObject cellPrefab; // Cell.prefab
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform contentParent; // Content
    [SerializeField] private RectTransform viewportRect; // Kéo Viewport vào đây
    [SerializeField] private GridLayoutGroup gridLayout;  // Kéo GridLayoutGroup vào đây

    [SerializeField] private TextMeshProUGUI addButtonNumberText; // Add button
    [SerializeField] private TextMeshProUGUI stageText;
    [SerializeField] private GameObject losePanel;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject homePanel;
    [SerializeField] private GameObject settingPanel;

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

    private bool isAnimating = false; // thêm field này

    [Header("SFX")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip chooseNumberSound;
    [SerializeField] private AudioClip pairClearSound;
    [SerializeField] private AudioClip pop2Sound;
    [SerializeField] private AudioClip rowClearSound;
    [SerializeField] private AudioClip gemCollectSound;
    [SerializeField] private AudioClip writeSound;
    [SerializeField] private AudioClip wrongSound;

    private List<int>[] neighborsCache;

    [Header("Gems")]
    [SerializeField] private TextMeshProUGUI pinkGemsText;
    [SerializeField] private TextMeshProUGUI orangeGemsText;
    [SerializeField] private TextMeshProUGUI purpleGemsText;

    private const int TARGET_PINK_GEMS = 5;
    private const int TARGET_ORANGE_GEMS = 5;
    private const int TARGET_PURPLE_GEMS = 5;
    // private const int TARGET_PINK_GEMS = 1;
    // private const int TARGET_ORANGE_GEMS = 1;
    // private const int TARGET_PURPLE_GEMS = 1;
    private int currentPinkGems = 0;
    private int currentOrangeGems = 0;
    private int currentPurpleGems = 0;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(InitAfterLayout());
    }

    IEnumerator InitAfterLayout()
    {
        yield return null;
        yield return null;
        yield return new WaitForEndOfFrame();

        SetupGridCellSize();

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

        currentPinkGems   = 0;
        currentOrangeGems = 0;
        currentPurpleGems = 0;
        pinkGemsText.text   = Mathf.Max(TARGET_PINK_GEMS   - currentPinkGems  , 0).ToString();
        orangeGemsText.text = Mathf.Max(TARGET_ORANGE_GEMS - currentOrangeGems, 0).ToString();
        purpleGemsText.text = Mathf.Max(TARGET_PURPLE_GEMS - currentPurpleGems, 0).ToString();
        currentStage = 0;

        firstSelected = -1;
        secondSelected = -1;

        GenerateNewStage();
    }

    private void SetupGridCellSize()
    {
        Canvas.ForceUpdateCanvases();
        
        float viewportWidth = viewportRect.rect.width;
        float spacing = gridLayout.spacing.x;
        int   columns = 9;
        
        float cellSize = (viewportWidth - spacing * (columns - 1) - 20) / columns;
        gridLayout.cellSize = new Vector2(cellSize, cellSize);

        viewportRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, viewportWidth);
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

        while (boardData.Count < 9 * COLUMNS || (lastValue != -1 && boardData.Count - lastValue - 1 < 3 * COLUMNS))
        {
            CreateNewLine();
        }

        while (boardData.Count % 9 != 0)
        {
            boardData.Add(0);
            AddCell(0);
        }

        UpdateScrollState(lastValue, boardData.Count);
    }
    public void UpdateScrollState(int lastValue, int boardSize)
    {
        if (scrollRect == null) return;

        if (boardSize > 81 && lastValue > 81)
        {
            // Kích hoạt khả năng cuộn dọc
            scrollRect.vertical = true;
            
            // (Tùy chọn) Cho phép hiệu ứng đàn hồi khi kéo quá đà
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
        }
        else
        {
            // Đưa Content về vị trí đầu (top) trước khi khóa
            scrollRect.verticalNormalizedPosition = 1f;
            
            // Khóa cuộn dọc
            scrollRect.vertical = false;
            
            // Đổi sang Clamped để tránh việc Content bị lệch khi board nhỏ
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
        }
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

    private void InitializeCache(int totalCells)
    {
        neighborsCache = new List<int>[totalCells];
        for (int i = 0; i < totalCells; i++)
        {
            neighborsCache[i] = GetNeighborIndices(i, totalCells);
        }
    }

    private IEnumerator DisplayCellsSequentially(List<int> valuesToDisplay, bool[] gemStatus, int startIndex)
    {
        isAnimating = true;
        
        // Tính toán delayTime dựa trên tối đa 30 phần tử để tốc độ vẽ không bị quá chậm
        int countToAnimate = Mathf.Min(valuesToDisplay.Count, 30);
        float delayTime = Mathf.Max(0.03f, 0.5f / countToAnimate);

        audioSource.PlayOneShot(writeSound);

        for (int i = 0; i < valuesToDisplay.Count; i++)
        {
            int currentIndex = startIndex + i;
            
            // 1. Cập nhật dữ liệu và Gem (Logic chung)
            UpdateOrAddCell(currentIndex, valuesToDisplay[i], gemStatus[i]);

            Transform cellTransform = cellViews[currentIndex].transform;

            // 2. Kiểm tra điều kiện vẽ: Chỉ vẽ hiệu ứng cho 30 phần tử đầu tiên
            if (i < countToAnimate)
            {
                // Hiệu ứng Pop-up mượt mà
                cellTransform.localScale = Vector3.one * 0.8f;
                cellTransform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack, 1.0f);

                if (gemStatus[i])
                {
                    cellTransform.DOPunchRotation(new Vector3(0, 0, 10f), 0.5f, 2, 0.5f);
                }

                // Chờ trước khi qua ô tiếp theo
                yield return new WaitForSeconds(delayTime);
            }
            else
            {
                // Các phần tử từ 31 trở đi: Hiện ngay lập tức
                cellTransform.localScale = Vector3.one;
            }
        }

        isAnimating = false;
        UpdateBoard();
    }

    // Hàm hỗ trợ cập nhật dữ liệu để code gọn gàng hơn
    private void UpdateOrAddCell(int currentIndex, int value, bool isGem)
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
            // Logic SetGem của bạn...
            List<int> gemsType = new List<int>();
            if (TARGET_PINK_GEMS - currentPinkGems > 0) gemsType.Add(1);
            if (TARGET_ORANGE_GEMS - currentOrangeGems > 0) gemsType.Add(2);
            if (TARGET_PURPLE_GEMS - currentPurpleGems > 0) gemsType.Add(3);

            if (gemsType.Count > 0)
            {
                cellViews[currentIndex].SetGem(gemsType[Random.Range(0, gemsType.Count)]);
            }
        }
    }

    private void GenerateNewStage()
    {
        ++currentStage;
        stageText.text = $"Stage: {currentStage}";

        addButtonCounter         = MAX_ADD_TIME;
        addButtonNumberText.text = addButtonCounter.ToString();

        int totalCells = START_ROWS * COLUMNS;
        if (neighborsCache == null || neighborsCache.Length != totalCells) {
            InitializeCache(totalCells);
        }

        do {
            List<int> tempBoard = new List<int>();
            for (int i = 0; i < totalCells; ++i) tempBoard.Add(-1);

            int[] numberCounts = new int[COLUMNS + 1];
            for (int i = 1; i <= COLUMNS; ++i) 
                numberCounts[i] = START_ROWS;

            int targetPairs = (currentStage == 1) ? 3 : ((currentStage == 2) ? 2 : 1);
            for (int p = 0; p < targetPairs; ++p)
            {
                PlaceRandomMatch(tempBoard, numberCounts);
            }

            int finalStep = CalcFinalStep(tempBoard);   

            float endTime = Time.realtimeSinceStartup + 1.0f;

            if (FillRemainingCells(tempBoard, numberCounts, 0, finalStep, endTime))
            {
                bool[] gemStatus = GenerateGems(0, tempBoard);

                StartCoroutine(DisplayCellsSequentially(tempBoard, gemStatus, 0));
                
                Debug.Log($"[Generate New Stage] - Stage: {currentStage} with { CountMatchPairs(tempBoard) } pairs.");
                
                break;
            }
            else
            {
                Debug.Log($"[Generate New Stage] - Failed with { targetPairs } pairs.");
            }
        }
        while (true);

        UpdateBoard();
    }

    private int CountMatchPairs(List<int> board)
    {
        int result = 0;
        bool[] visited = new bool[board.Count];
        for (int i = 0; i < board.Count; ++i) if (!visited[i])
        {
            for (int j = i + 1; j < board.Count; ++j)
            {
                if (PreMatch(i, j, board) && CanMatch(i, j, board)) if (!visited[j])
                {
                    visited[i] = true;
                    visited[j] = true;
                    ++result;
                    break;
                }
            }
        }
        return result;
    }

    private void PlaceRandomMatch(List<int> board, int[] numberCounts)
    {
        List<int> emptyIndices = new List<int>();
        for (int i = 0; i < board.Count; i++) if (board[i] == -1) emptyIndices.Add(i);
        Shuffle(emptyIndices);
        
        foreach (int firstIndex in emptyIndices)
        {
            var neighbors = neighborsCache[firstIndex];
            foreach (int secondIndex in neighbors)
            {
                if (board[secondIndex] == -1)
                {
                    for (int v = 1; v <= COLUMNS; v++)
                    {
                        int value = Random.Range(1, 10);
                        int option = Random.Range(0, 2);
                        if (option == 0) {
                            if (numberCounts[value] >= 1 && numberCounts[10 - value] >= 1)
                            {
                                board[firstIndex ] = value;
                                board[secondIndex] = 10 - value;
                                --numberCounts[value];
                                --numberCounts[10 - value];
                                return;
                            }    
                            else if (numberCounts[value] >= 2)
                            {
                                board[firstIndex ] = value;
                                board[secondIndex] = value;
                                numberCounts[value] -= 2;
                                return;
                            }
                        }
                        else
                        {
                            if (numberCounts[value] >= 2)
                            {
                                board[firstIndex ] = value;
                                board[secondIndex] = value;
                                numberCounts[value] -= 2;
                                return;
                            }
                            else if (numberCounts[value] >= 1 && numberCounts[10 - value] >= 1)
                            {
                                board[firstIndex ] = value;
                                board[secondIndex] = 10 - value;
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

    private int GetMostOptimizedIndex(List<int> board)
    {
        int result = -1, neighborCountA = 0;
        for (int i = 0; i < board.Count; i++)
        {
            if (board[i] == -1) {
                if (result == -1) {
                    result = i;
                    neighborCountA = CountFilledNeighbors(i, board);
                }
                else
                {
                    int neighborCountB = CountFilledNeighbors(i, board);
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

    private bool FillRemainingCells(List<int> board, int[] numberCounts, int step, int finalStep, float endTime)
    {
        if (Time.realtimeSinceStartup > endTime) return false;

        if (step >= finalStep) return true;

        int currentIndex = GetMostOptimizedIndex(board);

        int availableMask = GetAvailableMask(numberCounts, board, currentIndex);
        if (availableMask == 0) return false;

        for (int number = 1; number <= COLUMNS; ++number) if ((availableMask & (1 << number)) != 0)
        {
            board[currentIndex] = number;
            --numberCounts[number];
            
            if (FillRemainingCells(board, numberCounts, step + 1, finalStep, endTime)) return true;

            ++numberCounts[number];
            board[currentIndex] = -1;
        } 

        return false;
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

    private List<int> GetNeighborIndices(int index, int total)
    {
        List<int> neighbors = new List<int>();
        int r = index / COLUMNS;
        int c = index % COLUMNS;

        for (int dr = -1; dr <= 1; ++dr)
        {
            for (int dc = -1; dc <= 1; ++ dc)
            {
                if (dr == 0 && dc == 0) continue;
                int nr = r + dr, nc = c + dc;
                if (nr < 0 || nc < 0 || nc >= COLUMNS) continue;
                int ni = nr * COLUMNS + nc;
                if (ni >= total) continue;
                neighbors.Add(ni);
            }
        }

        if (index != total - 1 && (index % COLUMNS) == (COLUMNS - 1)) neighbors.Add(index + 1);
        if (index != 0         && (index % COLUMNS) == 0            ) neighbors.Add(index - 1);

        return neighbors;
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

    private int CountFilledNeighbors(int index, List<int> board)
    {
        int count = 0;
        List<int> neighbors = neighborsCache[index];
        foreach (int n in neighbors)
        {
            if (board[n] != -1) ++count;
        }
        return count;
    }

    private int GetAvailableMask(int[] numberCounts, List<int> board, int index)
    {
        int forbiddenMask = 0;
        List<int> neighbors = neighborsCache[index];

        foreach (int n in neighbors)
        {
            int val = board[n];
            if (val != -1) 
            {
                forbiddenMask |= (1 << val);
            }
        }

        int availableMask = 0;
        for (int i = 1; i <= COLUMNS; i++)
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

    private void HandleCellClicked(int index)
    {
        if (isAnimating) return; 

        audioSource.PlayOneShot(chooseNumberSound);

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

    private bool PreMatch(int a, int b, List<int> list)
    {
        bool isSame     = list[a] == list[b];
        bool isSumOfTen = (list[a] + list[b]) == 10;
        return isSame || isSumOfTen;
    }

    private bool PreMatch(int a, int b)
    {
        return PreMatch(a, b, boardData);
    }

    private bool CanMatch(int a, int b, List<int> list)
    {
        // Chắc chắn rằng a nhỏ hơn b
        if (a > b) return CanMatch(b, a);

        int[] x = { a / COLUMNS, b / COLUMNS };
        int[] y = { a % COLUMNS, b % COLUMNS };

        // Kiểm tra theo chiều ngang
        if (x[0] == x[1]) 
        {
            bool matchFound = true;
            for (int i = a + 1; i < b; ++i) if (list[i] > 0) 
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
            for (int i = a + COLUMNS; i < b; i += COLUMNS) if (list[i] > 0) 
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
                if (list[i * COLUMNS + j] > 0) {
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
                if (list[i * COLUMNS + j] > 0)
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
            for (int i = a + 1; i < b; ++i) if (list[i] > 0)
            {
                matchFound = false;
                break;
            }
            if (matchFound) return true;
        }

        return false;
    }

    private bool CanMatch(int a, int b)
    {
        return CanMatch(a, b, boardData);
    }

    private List<int> GetBlockingIndices(int a, int b)
    {
        List<int> blockers = new List<int>();
        if (a > b) { int temp = a; a = b; b = temp; }

        int r1 = a / COLUMNS, c1 = a % COLUMNS;
        int r2 = b / COLUMNS, c2 = b % COLUMNS;

        // 1. Kiểm tra Ngang (cùng hàng)
        if (r1 == r2)
        {
            for (int i = a + 1; i < b; ++i)
                if (boardData[i] > 0) blockers.Add(i);
            return blockers; 
        }

        // 2. Kiểm tra Dọc (cùng cột)
        if (c1 == c2)
        {
            for (int i = a + COLUMNS; i < b; i += COLUMNS)
                if (boardData[i] > 0) blockers.Add(i);
            return blockers;
        }

        // 3. Kiểm tra Đường chéo chính
        if (r1 - c1 == r2 - c2)
        {
            int i = r1 + 1, j = c1 + 1;
            while (i < r2 && j < c2)
            {
                int idx = i * COLUMNS + j;
                if (boardData[idx] > 0) blockers.Add(idx);
                i++; j++;
            }
            return blockers;
        }

        // 4. Kiểm tra Đường chéo phụ
        if (r1 + c1 == r2 + c2)
        {
            int i = r1 + 1, j = c1 - 1;
            while (i < r2 && j > c2)
            {
                int idx = i * COLUMNS + j;
                if (boardData[idx] > 0) blockers.Add(idx);
                i++; j--;
            }
            return blockers;
        }

        // 5. Mặc định: Kiểm tra theo thứ tự index (Case tăng dần/nhảy hàng)
        // Nếu không thuộc 4 trường hợp hình học trên, ta lấy tất cả các ô ở giữa theo index
        for (int i = a + 1; i < b; ++i)
        {
            if (boardData[i] > 0) blockers.Add(i);
        }

        return blockers;
    }

    private void EvaluateMatch()
    {
        bool matched = CanMatch(firstSelected, secondSelected);

        if (matched)
        {
            audioSource.PlayOneShot(pairClearSound);

            if (firstSelected < secondSelected)
            {
                ProcessMatch(secondSelected, firstSelected);
            }
            else {
                ProcessMatch(firstSelected, secondSelected);
            }

            // Check Win trước chứ
            if (CheckWin())
            {
                ShowWinScreen();
            }
            else if (addButtonCounter == 0)
            {
                if (!CheckFinishedStage() && CheckLose())
                {
                    ShowLoseScreen();
                }
            }
        }
        else
        {
            audioSource.PlayOneShot(wrongSound);

            // Lấy danh sách các ô đang chặn đường
            List<int> blockingIndices = GetBlockingIndices(firstSelected, secondSelected);

            // Cho tất cả các ô đó "rung" lên
            foreach (int idx in blockingIndices)
            {
                if (idx >= 0 && idx < cellViews.Count)
                {
                    cellViews[idx].Shake();
                }
            }

            // Đồng thời làm rung cả 2 ô đang được chọn để báo hiệu "Không thể nối"
            cellViews[firstSelected].Shake();
            cellViews[secondSelected].Shake();

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
        // Time.timeScale = 0.0f;
    }

    private bool CheckWin()
    {
        if (currentPinkGems   >= TARGET_PINK_GEMS && 
            currentOrangeGems >= TARGET_ORANGE_GEMS && 
            currentPurpleGems >= TARGET_PURPLE_GEMS)
        {
            return true;
        }

        return false;
    }

    private void ShowWinScreen()
    {
        winPanel.SetActive(true);
        // Time.timeScale = 0.0f;
    }

    private void Deselect(int index)
    {
        if (index >= 0 && index < cellViews.Count)
        {
            cellViews[index].SetHighlight(false);
        }
    }

    // Chỉ kiểm tra - không động vào data, không animation
    private bool CheckClearLine(int a)
    {
        int x = a / COLUMNS;
        int startIndex = x * COLUMNS;

        if (startIndex < 0 || startIndex >= boardData.Count) return false;

        int endIndex = System.Math.Min(startIndex + COLUMNS, boardData.Count);
        for (int i = startIndex; i < endIndex; ++i)
            if (boardData[i] > 0) return false;

        return true;
    }

    [Header("Match Line")]
    [SerializeField] private Canvas rootCanvas; // Kéo Canvas gốc (root) vào đây trong Editor
    private float matchLineDuration = 0.5f;
    [SerializeField] private Color matchLineColor = Color.yellow;

    private void DrawMatchLine(int a, int b)
    {
        int first = Mathf.Min(a, b);
        int second = Mathf.Max(a, b);

        if (isSpecialMatch(first, second))
        {
            StartCoroutine(DrawSpecialMatchLineRoutine(first, second));
        }
        else
        {
            StartCoroutine(DrawMatchLineRoutine(first, second));
        }
    }

    private Vector2 GetCellScreenCenter(int index)
    {
        RectTransform rt = cellViews[index].GetComponent<RectTransform>();
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        // Trung điểm trong world space
        Vector3 worldCenter = (corners[0] + corners[2]) * 0.5f;

        // Với Screen Space - Overlay thì worldCenter đã là screen coords
        // Với Screen Space - Camera thì cần camera
        Camera cam = (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : rootCanvas.worldCamera;
        return RectTransformUtility.WorldToScreenPoint(cam, worldCenter);
    }

    private IEnumerator DrawSpecialMatchLineRoutine(int b, int a)
    {
        RectTransform canvasRT = rootCanvas.GetComponent<RectTransform>();
        Camera cam = (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : rootCanvas.worldCamera;

        // --- TÍNH TOÁN CÁC ĐIỂM ---
        // 1. Điểm xuất phát từ b và kết thúc ở mép phải hàng b
        Vector2 screenB = GetCellScreenCenter(b);
        int endRowBIndex = (b / 9) * 9 + 8; // Ô cuối cùng của hàng chứa b
        Vector2 screenEdgeB = GetCellScreenCenter(endRowBIndex) + new Vector2(50f, 0); // Cộng thêm offset để ra ngoài biên

        // 2. Điểm xuất phát từ mép trái hàng a và kết thúc ở a
        Vector2 screenA = GetCellScreenCenter(a);
        int startRowAIndex = (a / 9) * 9; // Ô đầu tiên của hàng chứa a
        Vector2 screenEdgeA = GetCellScreenCenter(startRowAIndex) - new Vector2(50f, 0); // Trừ offset

        // Convert tất cả sang Local Point
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, screenB, cam, out Vector2 locB);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, screenEdgeB, cam, out Vector2 locEdgeB);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, screenA, cam, out Vector2 locA);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, screenEdgeA, cam, out Vector2 locEdgeA);

        // --- TẠO OBJECTS ---
        float lineThickness = 20f;
        float cellWidth = gridLayout.cellSize.x;
        float cellHeight = gridLayout.cellSize.y;

        // Tạo 2 đoạn line
        GameObject line1 = CreateLineSegment("Line_Part1", locB, locEdgeB, lineThickness, canvasRT);
        GameObject line2 = CreateLineSegment("Line_Part2", locEdgeA, locA, lineThickness, canvasRT);

        // Thêm các đầu vuông (Chỉ thêm ở ô b và ô a)
        Image capB = CreateEndCap("CapB", line1.GetComponent<RectTransform>(), new Vector2(cellWidth, cellHeight));
        capB.rectTransform.anchoredPosition = new Vector2(-line1.GetComponent<RectTransform>().sizeDelta.x / 2, 0);

        Image capA = CreateEndCap("CapA", line2.GetComponent<RectTransform>(), new Vector2(cellWidth, cellHeight));
        capA.rectTransform.anchoredPosition = new Vector2(line2.GetComponent<RectTransform>().sizeDelta.x / 2, 0);

        // --- HIỆU ỨNG FADE ---
        Image img1 = line1.GetComponent<Image>();
        Image img2 = line2.GetComponent<Image>();
        float elapsed = 0f;
        while (elapsed < matchLineDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / matchLineDuration;

            float alpha = Mathf.Lerp(matchLineColor.a, 0f, t);
            Color c = new Color(matchLineColor.r, matchLineColor.g, matchLineColor.b, alpha);
            
            img1.color = img2.color = capA.color = capB.color = c;

            // Thu nhỏ kích thước (Scale từ 1 về 0)
            float scale = Mathf.Lerp(1f, 0f, t);
            capB.rectTransform.localScale = new Vector3(scale, scale, 1f);
            capA.rectTransform.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }

        Destroy(line1);
        Destroy(line2);
    }

    // Helper để tạo một đoạn thẳng nhanh
    private GameObject CreateLineSegment(string name, Vector2 start, Vector2 end, float thickness, RectTransform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.transform.SetParent(parent, false);
        obj.transform.SetAsLastSibling();

        RectTransform rt = obj.GetComponent<RectTransform>();
        Vector2 dir = end - start;
        float len = dir.magnitude;
        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = (start + end) * 0.5f;
        rt.sizeDelta = new Vector2(len, thickness);
        rt.localEulerAngles = new Vector3(0, 0, ang);

        obj.GetComponent<Image>().color = matchLineColor;
        return obj;
    }

    private IEnumerator DrawMatchLineRoutine(int a, int b)
    {
        // Lấy screen position của 2 cell
        Vector2 screenA = GetCellScreenCenter(a);
        Vector2 screenB = GetCellScreenCenter(b);

        // Convert sang local space của Canvas gốc
        RectTransform canvasRT = rootCanvas.GetComponent<RectTransform>();
        Camera cam = (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : rootCanvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, screenA, cam, out Vector2 localA);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, screenB, cam, out Vector2 localB);

        // Cấu hình thông số Line
        float lineThickness = 20f; // Độ rộng của line
        float cellWidth = gridLayout.cellSize.x;
        float cellHeight = gridLayout.cellSize.y;

        // Tạo line object — parent là Canvas gốc, KHÔNG phải contentParent
        GameObject lineObj = new GameObject("MatchLine", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        lineObj.transform.SetParent(canvasRT, false);
        lineObj.transform.SetAsLastSibling(); // Vẽ trên cùng

        RectTransform lineRT = lineObj.GetComponent<RectTransform>();
        lineRT.anchorMin = new Vector2(0.5f, 0.5f);
        lineRT.anchorMax = new Vector2(0.5f, 0.5f);
        lineRT.pivot     = new Vector2(0.5f, 0.5f);

        Vector2 dir    = localB - localA;
        float length   = dir.magnitude;
        float angle    = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        lineRT.anchoredPosition = (localA + localB) * 0.5f;
        lineRT.sizeDelta        = new Vector2(length, lineThickness);
        lineRT.localEulerAngles = new Vector3(0f, 0f, angle);

        Image lineImg = lineObj.GetComponent<Image>();
        lineImg.color = matchLineColor;

        // Tạo 2 hình vuông ở 2 đầu (là con của lineObj)
        Image startSquare = CreateEndCap("StartCap", lineRT, new Vector2(cellWidth, cellHeight));
        Image endSquare = CreateEndCap("EndCap", lineRT, new Vector2(cellWidth, cellHeight));

        // Đặt vị trí 2 đầu theo local space của Line
        // Vì pivot của Line là (0.5, 0.5) nên đầu trái là -length/2, đầu phải là length/2
        startSquare.rectTransform.anchoredPosition = new Vector2(-length / 2, 0);
        endSquare.rectTransform.anchoredPosition = new Vector2(length / 2, 0);

        startSquare.rectTransform.localEulerAngles = new Vector3(0, 0, -angle);
        endSquare.rectTransform.localEulerAngles = new Vector3(0, 0, -angle);

        // Fade out
        float elapsed = 0f;
        while (elapsed < matchLineDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / matchLineDuration;
            float t = Mathf.Pow(normalizedTime, 3);

            float alpha = Mathf.Lerp(matchLineColor.a, 0f, t);
            Color newColor = new Color(matchLineColor.r, matchLineColor.g, matchLineColor.b, alpha);

            lineImg.color = newColor;
            startSquare.color = newColor;
            endSquare.color = newColor;

            // Thu nhỏ kích thước (Scale từ 1 về 0)
            float scale = Mathf.Lerp(1f, 0f, t);
            startSquare.rectTransform.localScale = new Vector3(scale, scale, 1f);
            endSquare.rectTransform.localScale = new Vector3(scale, scale, 1f);

            yield return null;
        }

        Destroy(lineObj);

        // ==========================================
        // 3. KÍCH HOẠT HIỆU ỨNG GIỌT NƯỚC (RIPPLE)
        // ==========================================
        // Lấy kích thước trung bình của cell để làm base size cho giọt nước
        float dropSize = (cellWidth + cellHeight) / 2f; 
        
        CreateWaterDropEffect(localA, dropSize, canvasRT);
        CreateWaterDropEffect(localB, dropSize, canvasRT);
    }

    // Hàm mới: Tạo hiệu ứng giọt nước nổ ra rồi mờ đi
    private void CreateWaterDropEffect(Vector2 localPosition, float size, RectTransform parentCanvas)
    {
        // Tạo object giọt nước
        GameObject dropObj = new GameObject("WaterDropRipple", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dropObj.transform.SetParent(parentCanvas, false);
        
        RectTransform dropRT = dropObj.GetComponent<RectTransform>();
        dropRT.anchoredPosition = localPosition;
        dropRT.sizeDelta = new Vector2(size, size);
        
        Image dropImg = dropObj.GetComponent<Image>();
        dropImg.color = matchLineColor; // Lấy cùng màu với Line
        
        // MẸO UX: Bạn nên gán một Sprite hình tròn (Circle) vào đây
        // dropImg.sprite = yourCircleSprite; 

        // Bắt đầu scale từ 0.5 (nhỏ xíu)
        dropRT.localScale = Vector3.one * 0.5f;

        // Chạy Animation cho giọt nước
        Sequence dropSeq = DOTween.Sequence();
        
        // Phóng to ra gấp 1.5 hoặc 2 lần (thời gian 0.4s)
        dropSeq.Append(dropRT.DOScale(Vector3.one * 1.4f, 0.2f).SetEase(Ease.OutQuad));
        
        // ĐỒNG THỜI (Join): Làm mờ Alpha về 0
        dropSeq.Join(dropImg.DOFade(0f, 0.4f).SetEase(Ease.OutQuad));
        
        // Khi animation kết thúc, tự động dọn dẹp (Destroy) object này
        dropSeq.OnComplete(() => Destroy(dropObj));
    }

    private Image CreateEndCap(string name, RectTransform parent, Vector2 size)
    {
        GameObject capObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        capObj.transform.SetParent(parent, false);
        
        RectTransform capRT = capObj.GetComponent<RectTransform>();
        capRT.sizeDelta = size;
        
        Image capImg = capObj.GetComponent<Image>();
        capImg.color = matchLineColor; // Gán màu ban đầu
        
        return capImg;
    }


    private bool isSpecialMatch(int a, int b)
    {
        int r1 = a / 9, r2 = b / 9;
        int c1 = a % 9, c2 = b % 9;

        if (r1 == r2 || c1 == c2 || r1 + c1 == r2 + c2 || r1 - c1 == r2 - c2) return false;
        
        return true;
    }
    // a > b để xóa dòng dưới trước rồi xóa dòng trên khi a và b đều gây ra clear row
    private void ProcessMatch(int a, int b)
    {
        Debug.Log($"Matched: [{a}] = {boardData[a]} & [{b}] = {boardData[b]}");

        int gemTypeA = cellViews[a].SetCleared();
        int gemTypeB = cellViews[b].SetCleared();

        // Nếu isSpecialMatch(a, b) thì DrawMatchLine vẽ từ b (vì b có index bé hơn a) đến cuối hàng của b 
        // rồi vẽ từ đầu hàng của a đến a.
        DrawMatchLine(a, b);

        if (gemTypeA != 0)
        {
            if (gemTypeA == 1) ++currentPinkGems;
            else if (gemTypeA == 2) ++currentOrangeGems;
            else if (gemTypeA == 3) ++currentPurpleGems;
        }
        if (gemTypeB != 0)
        {
            if (gemTypeB == 1) ++currentPinkGems;
            else if (gemTypeB == 2) ++currentOrangeGems;
            else if (gemTypeB == 3) ++currentPurpleGems;
        }

        if (gemTypeA != 0 || gemTypeB != 0)
        {
            audioSource.PlayOneShot(gemCollectSound);
            pinkGemsText.text   = Mathf.Max(TARGET_PINK_GEMS   - currentPinkGems,   0).ToString();
            orangeGemsText.text = Mathf.Max(TARGET_ORANGE_GEMS - currentOrangeGems, 0).ToString();
            purpleGemsText.text = Mathf.Max(TARGET_PURPLE_GEMS - currentPurpleGems, 0).ToString();
        }

        boardData[a] = -1;
        boardData[b] = -1;

        // ── Kiểm tra TRƯỚC (sync, không đụng data) ──
        bool clearLineA = CheckClearLine(a);
        bool clearLineB = CheckClearLine(b);

        if (clearLineA || clearLineB)
        {
            audioSource.PlayOneShot(rowClearSound);

            // Kick off animation, truyền callback để chạy phần còn lại sau
            StartCoroutine(ProcessClearLinesAndContinue(a, b, clearLineA, clearLineB));
        }
        else
        {
            // Không có clear line → chạy luôn như cũ
            PostClearProcess(a, b, false, false);
        }
    }

    private IEnumerator ProcessClearLinesAndContinue(int a, int b, bool clearLineA, bool clearLineB)
    {
        isAnimating = true;

        int startA = (a / COLUMNS) * COLUMNS;
        int endA = Mathf.Min(startA + COLUMNS, cellViews.Count);

        int startB = (b / COLUMNS) * COLUMNS;
        int endB = Mathf.Min(startB + COLUMNS, cellViews.Count);

        float individualDuration = 0.3f; // Thời gian biến mất của 1 ô
        float staggerDelay = 0.05f;      // Độ trễ giữa mỗi ô (tạo hiệu ứng gợn sóng)

        // Duyệt qua từng cột để kích hoạt animation đồng thời trên cả 2 dòng
        for (int col = 0; col < COLUMNS; col++)
        {
            float currentDelay = col * staggerDelay;

            // Xử lý dòng A
            if (clearLineA)
            {
                int idx = startA + col;
                if (idx < endA && cellViews[idx] != null)
                {
                    // Dùng DOVirtual.DelayedCall để kích hoạt sau một khoảng delay
                    int capturedIdx = idx;
                    DOVirtual.DelayedCall(currentDelay, () => {
                        if(cellViews[capturedIdx] != null) cellViews[capturedIdx].PlayClearAnimation(individualDuration);
                    });
                }
            }

            // Xử lý dòng B (nếu khác dòng A)
            if (clearLineB && startB != startA)
            {
                int idx = startB + col;
                if (idx < endB && cellViews[idx] != null)
                {
                    int capturedIdx = idx;
                    DOVirtual.DelayedCall(currentDelay, () => {
                        if(cellViews[capturedIdx] != null) cellViews[capturedIdx].PlayClearAnimation(individualDuration);
                    });
                }
            }
        }

        // Chờ đợi toàn bộ chuỗi animation kết thúc
        // Tổng thời gian = (Số cột * delay) + thời gian animation ô cuối cùng
        float totalWaitTime = (COLUMNS * staggerDelay) + individualDuration;
        yield return new WaitForSeconds(totalWaitTime);

        // --- PHẦN LOGIC XÓA DATA (Giữ nguyên logic thứ tự của bạn) ---
        if (clearLineA && clearLineB && startA != startB)
        {
            int firstStart = Mathf.Min(startA, startB);
            int secondStart = Mathf.Max(startA, startB);
            DeleteLineData(secondStart, secondStart + COLUMNS, b > a ? b : a);
            DeleteLineData(firstStart, firstStart + COLUMNS, b > a ? a : b);
        }
        else if (clearLineA) DeleteLineData(startA, endA, a);
        else if (clearLineB) DeleteLineData(startB, endB, b);

        PostClearProcess(a, b, clearLineA, clearLineB);
        isAnimating = false;
    }

    // Tách riêng để tái sử dụng
    private void DeleteLineData(int startIndex, int endIndex, int a)
    {
        for (int i = startIndex; i < endIndex; ++i)
            if (cellViews[i] != null) Destroy(cellViews[i].gameObject);

        int count = endIndex - startIndex;
        if (count > 0)
        {
            boardData.RemoveRange(startIndex, count);
            cellViews.RemoveRange(startIndex, count);
        }

        Debug.Log($"[Clear line]: [{a / COLUMNS}]: boardData.Count {boardData.Count}; cellViews.Count {cellViews.Count}");

        UpdateCellsIndex(a);
    }

    private void FinalizeTurnCheck()
    {
        if (CheckWin())
        {
            ShowWinScreen();
        }
        else if (addButtonCounter == 0)
        {
            if (!CheckFinishedStage() && CheckLose())
            {
                ShowLoseScreen();
            }
        }
    }

    private void PostClearProcess(int a, int b, bool clearLineA, bool clearLineB)
    {
        if (clearLineA || clearLineB)
        {
            UpdateBoard();
        }

        if (clearLineA && clearLineB && ((a < COLUMNS) || (b < COLUMNS)))
        {
            if (CheckFinishedStage())
                GenerateNewStage();
        }

        FinalizeTurnCheck();
    }

    private bool CheckFinishedStage()
    {
        for (int index = 0; index < COLUMNS; ++index) if (boardData[index] > 0) return false;

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

    public void HandleAddMoreNumbers()
    {
        if (isAnimating) return;

        audioSource.PlayOneShot(pop2Sound);

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

        bool[] gemStatus = GenerateGems(startIndex, remaining);

        StartCoroutine(DisplayCellsSequentially(remaining, gemStatus, startIndex));

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

    public void HandleSettingButton()
    {
        if (isAnimating) return;

        audioSource.PlayOneShot(pop2Sound);

        settingPanel.SetActive(true);
        // Time.timeScale = 0.0f;
    }

    public void HandleHomeButton()
    {
        if (isAnimating) return;
        
        audioSource.PlayOneShot(pop2Sound);

        homePanel.SetActive(true);
        // Time.timeScale = 0.0f;
    }

    public void HandleReplayButton()
    {
        if (isAnimating) return;
        
        audioSource.PlayOneShot(pop2Sound);

        // Time.timeScale = 1.0f;
        losePanel.SetActive(false);
        winPanel.SetActive(false);
        homePanel.SetActive(false);
        settingPanel.SetActive(false);

        Start();
    }

    public void HandleBackButton()
    {
        if (isAnimating) return;

        audioSource.PlayOneShot(pop2Sound);

        homePanel.SetActive(false);
        settingPanel.SetActive(false);

        // Time.timeScale = 1.0f;
    }

    private bool[] GenerateGems(int startIndex, List<int> addNumbers)
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

    private int FindValidGemCandidate(
        List<int> addNumbers,
        int currentIndex,
        int startIndex,
        bool[] gemStatus)
    {
        startIndex %= 9;
        List<int> neighbors = GetNeighborIndices(startIndex + currentIndex, startIndex + addNumbers.Count);
        foreach (int n in neighbors) if (0 <= n - startIndex && n - startIndex < addNumbers.Count && gemStatus[n - startIndex] == true)
        {
            if (PreMatch(currentIndex, n - startIndex, addNumbers) && CanMatch(currentIndex, n - startIndex, addNumbers))
            {
                return -1;                
            }
        }
        return currentIndex;
    }
}
