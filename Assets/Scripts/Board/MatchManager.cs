using System.Collections.Generic;
using UnityEngine;

public class MatchManager : MonoBehaviour
{
    public static MatchManager Instance { get; private set; }
    private List<int>[] neighborsCache;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        
        int totalCells = Constants.START_ROWS * Constants.COLUMNS;
        if (neighborsCache == null || neighborsCache.Length != totalCells) {
            InitializeCache(totalCells);
        }

        // Nếu muốn tồn tại xuyên suốt các Scene:
        // DontDestroyOnLoad(this.gameObject); 
    }

    public void InitializeCache(int totalCells)
    {
        neighborsCache = new List<int>[totalCells];
        for (int i = 0; i < totalCells; i++)
        {
            neighborsCache[i] = GetNeighborIndices(i, totalCells);
        }
    }
    public List<int> GetNeighborIndices(int index, int total)
    {
        List<int> neighbors = new List<int>();
        int r = index / Constants.COLUMNS;
        int c = index % Constants.COLUMNS;

        for (int dr = -1; dr <= 1; ++dr)
        {
            for (int dc = -1; dc <= 1; ++ dc)
            {
                if (dr == 0 && dc == 0) continue;
                int nr = r + dr, nc = c + dc;
                if (nr < 0 || nc < 0 || nc >= Constants.COLUMNS) continue;
                int ni = nr * Constants.COLUMNS + nc;
                if (ni >= total) continue;
                neighbors.Add(ni);
            }
        }

        if (index != total - 1 && (index % Constants.COLUMNS) == (Constants.COLUMNS - 1)) neighbors.Add(index + 1);
        if (index != 0         && (index % Constants.COLUMNS) == 0                      ) neighbors.Add(index - 1);

        return neighbors;
    }
    public List<int> GetNeighbors(int index)
    {
        return neighborsCache[index];
    }
    public int CountMatchPairs(List<int> boardData)
    {
        int result = 0;
        bool[] visited = new bool[boardData.Count];
        for (int i = 0; i < boardData.Count; ++i) if (!visited[i] && boardData[i] > 0)
        {
            for (int j = i + 1; j < boardData.Count; ++j) if (!visited[j] && boardData[j] > 0)
            {
                if (PreMatch(i, j, boardData) && CanMatch(i, j, boardData))
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
    public bool PreMatch(int a, int b, List<int> boardData)
    {
        bool isSame     = boardData[a] == boardData[b];
        bool isSumOfTen = (boardData[a] + boardData[b]) == 10;
        return isSame || isSumOfTen;
    }
    public bool CanMatch(int a, int b, List<int> boardData)
    {
        // Chắc chắn rằng a nhỏ hơn b
        if (a > b) return CanMatch(b, a, boardData);

        int[] x = { a / Constants.COLUMNS, b / Constants.COLUMNS };
        int[] y = { a % Constants.COLUMNS, b % Constants.COLUMNS };

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
            for (int i = a + Constants.COLUMNS; i < b; i += Constants.COLUMNS) if (boardData[i] > 0) 
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
                if (boardData[i * Constants.COLUMNS + j] > 0) {
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
                if (boardData[i * Constants.COLUMNS + j] > 0)
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
    public bool isSpecialMatch(int a, int b)
    {
        int r1 = a / Constants.COLUMNS, r2 = b / Constants.COLUMNS;
        int c1 = a % Constants.COLUMNS, c2 = b % Constants.COLUMNS;

        if (r1 == r2 || c1 == c2 || r1 + c1 == r2 + c2 || r1 - c1 == r2 - c2) return false;
        
        return true;
    }
    public (int, int) FindMatchablePair(List<int> boardData)
    {
        for (int i = 0; i < boardData.Count; ++i) if (boardData[i] > 0)
        {
            for (int j = i + 1; j < boardData.Count; ++j) if (boardData[j] > 0) 
            {
                if (PreMatch(i, j, boardData) && CanMatch(i, j, boardData))
                {
                    return (i, j);
                }
            }
        }

        return (-1, -1);
    }
    public List<int> GetBlockingIndices(int a, int b, List<int> boardData)
    {
        List<int> blockers = new List<int>();
        if (a > b) { (a, b) = (b, a); }

        int r1 = a / Constants.COLUMNS, c1 = a % Constants.COLUMNS;
        int r2 = b / Constants.COLUMNS, c2 = b % Constants.COLUMNS;

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
            for (int i = a + Constants.COLUMNS; i < b; i += Constants.COLUMNS)
                if (boardData[i] > 0) blockers.Add(i);
            return blockers;
        }

        // 3. Kiểm tra Đường chéo chính
        if (r1 - c1 == r2 - c2)
        {
            int i = r1 + 1, j = c1 + 1;
            while (i < r2 && j < c2)
            {
                int idx = i * Constants.COLUMNS + j;
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
                int idx = i * Constants.COLUMNS + j;
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
    public void EvaluateMatch(ref int firstSelected, ref int secondSelected, List<int> boardData, List<CellView> cellViews)
    {
        bool matched = CanMatch(firstSelected, secondSelected, boardData);

        if (matched)
        {
            AudioManager.Instance.PlaySound(AudioManager.AudioType.PairClear);

            if (firstSelected < secondSelected)
            {
                ProcessMatch(secondSelected, firstSelected, boardData, cellViews);
            }
            else {
                ProcessMatch(firstSelected, secondSelected, boardData, cellViews);
            }
        }
        else
        {
            AudioManager.Instance.PlaySound(AudioManager.AudioType.Wrong);

            VFXManager.Instance.ShakeWrongNumber(boardData, cellViews, firstSelected, secondSelected);

            BoardManager.Instance.Deselect(firstSelected);
            BoardManager.Instance.Deselect(secondSelected);
        }   

        firstSelected  = -1;
        secondSelected = -1;

        SuggestManager.Instance.ResetMatchableWithSelected(cellViews);
    }
    public void ProcessMatch(int a, int b, List<int> boardData, List<CellView> cellViews)
    {
        if (a < b) (a, b) = (b, a);

        Debug.Log($"Matched: [{a}] = {boardData[a]} & [{b}] = {boardData[b]}");

        int gemTypeA = cellViews[a].SetCleared();
        int gemTypeB = cellViews[b].SetCleared();

        // Nếu isSpecialMatch(a, b) thì DrawMatchLine vẽ từ b (vì b có index bé hơn a) đến cuối hàng của b 
        // rồi vẽ từ đầu hàng của a đến a.
        VFXManager.Instance.DrawMatchLine(a, b, cellViews);

        GemManager.Instance.UpdateStatus(gemTypeA);
        GemManager.Instance.UpdateStatus(gemTypeB);

        if (gemTypeA != 0 || gemTypeB != 0)
        {
            AudioManager.Instance.PlaySound(AudioManager.AudioType.GemCollect);
        }

        boardData[a] = -1;
        boardData[b] = -1;

        // ── Kiểm tra TRƯỚC (sync, không đụng data) ──
        bool clearLineA = CheckClearLine(a, boardData);
        bool clearLineB = CheckClearLine(b, boardData);

        if (clearLineA || clearLineB)
        {
            AudioManager.Instance.PlaySound(AudioManager.AudioType.RowClear);

            int startA = (a / Constants.COLUMNS) * Constants.COLUMNS;
            int startB = (b / Constants.COLUMNS) * Constants.COLUMNS;

            // Kick off animation, truyền callback để chạy phần còn lại sau
            // StartCoroutine(ProcessClearLinesAndContinue(a, b, clearLineA, clearLineB));
            VFXManager.Instance.PlayClearLineAnimation(
                cellViews, 
                Constants.COLUMNS, 
                startA, 
                startB, 
                clearLineA, 
                clearLineB, 
                () => BoardManager.Instance.HandleDataAfterClearAnimation(a, b, startA, startB, clearLineA, clearLineB)
            );
        }
        else
        {
            // Không có clear line → chạy luôn như cũ
            BoardManager.Instance.PostClearProcess(a, b, false, false);
        }

        UIManager.Instance.UpdatePairsText(CountMatchPairs(boardData));
    }
    private bool CheckClearLine(int a, List<int> boardData)
    {
        int x = a / Constants.COLUMNS;
        int startIndex = x * Constants.COLUMNS;

        if (startIndex < 0 || startIndex >= boardData.Count) return false;

        int endIndex = System.Math.Min(startIndex + Constants.COLUMNS, boardData.Count);
        for (int i = startIndex; i < endIndex; ++i)
            if (boardData[i] > 0) return false;

        return true;
    }
}