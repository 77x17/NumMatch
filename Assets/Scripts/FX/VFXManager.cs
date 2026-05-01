using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    [Header("Dependences")]
    [SerializeField] private Canvas rootCanvas; // Kéo Canvas gốc (root) vào đây trong Editor
    [SerializeField] private GridLayoutGroup gridLayout;  // Kéo GridLayoutGroup vào đây
    private const float MATCH_LINE_DURATION = 0.5f;
    private Color MATCH_LINE_COLOR = Color.yellow;

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

    public void DrawMatchLine(int a, int b, List<CellView> cellViews)
    {
        int first  = Mathf.Min(a, b);
        int second = Mathf.Max(a, b);

        if (MatchManager.Instance.isSpecialMatch(first, second))
        {
            StartCoroutine(DrawSpecialMatchLineRoutine(first, second, cellViews));
        }
        else
        {
            StartCoroutine(DrawMatchLineRoutine(first, second, cellViews));
        }
    }
    private Vector2 GetCellScreenCenter(int index, List<CellView> cellViews)
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
    private IEnumerator DrawSpecialMatchLineRoutine(int b, int a, List<CellView> cellViews)
    {
        RectTransform canvasRT = rootCanvas.GetComponent<RectTransform>();
        Camera cam = (rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : rootCanvas.worldCamera;

        // --- TÍNH TOÁN CÁC ĐIỂM ---
        // 1. Điểm xuất phát từ b và kết thúc ở mép phải hàng b
        Vector2 screenB = GetCellScreenCenter(b, cellViews);
        int endRowBIndex = (b / 9) * 9 + 8; // Ô cuối cùng của hàng chứa b
        Vector2 screenEdgeB = GetCellScreenCenter(endRowBIndex, cellViews) + new Vector2(50f, 0); // Cộng thêm offset để ra ngoài biên

        // 2. Điểm xuất phát từ mép trái hàng a và kết thúc ở a
        Vector2 screenA = GetCellScreenCenter(a, cellViews);
        int startRowAIndex = (a / 9) * 9; // Ô đầu tiên của hàng chứa a
        Vector2 screenEdgeA = GetCellScreenCenter(startRowAIndex, cellViews) - new Vector2(50f, 0); // Trừ offset

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
        while (elapsed < MATCH_LINE_DURATION)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / MATCH_LINE_DURATION;

            float alpha = Mathf.Lerp(MATCH_LINE_COLOR.a, 0f, t);
            Color c = new Color(MATCH_LINE_COLOR.r, MATCH_LINE_COLOR.g, MATCH_LINE_COLOR.b, alpha);
            
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

        obj.GetComponent<Image>().color = MATCH_LINE_COLOR;
        return obj;
    }
    private IEnumerator DrawMatchLineRoutine(int a, int b, List<CellView> cellViews)
    {
        // Lấy screen position của 2 cell
        Vector2 screenA = GetCellScreenCenter(a, cellViews);
        Vector2 screenB = GetCellScreenCenter(b, cellViews);

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
        lineImg.color = MATCH_LINE_COLOR;

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
        while (elapsed < MATCH_LINE_DURATION)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / MATCH_LINE_DURATION;
            float t = Mathf.Pow(normalizedTime, 3);

            float alpha = Mathf.Lerp(MATCH_LINE_COLOR.a, 0f, t);
            Color newColor = new Color(MATCH_LINE_COLOR.r, MATCH_LINE_COLOR.g, MATCH_LINE_COLOR.b, alpha);

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
    private void CreateWaterDropEffect(Vector2 localPosition, float size, RectTransform parentCanvas)
    {
        // Tạo object giọt nước
        GameObject dropObj = new GameObject("WaterDropRipple", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dropObj.transform.SetParent(parentCanvas, false);
        
        RectTransform dropRT = dropObj.GetComponent<RectTransform>();
        dropRT.anchoredPosition = localPosition;
        dropRT.sizeDelta = new Vector2(size, size);
        
        Image dropImg = dropObj.GetComponent<Image>();
        dropImg.color = MATCH_LINE_COLOR; // Lấy cùng màu với Line
        
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
        capImg.color = MATCH_LINE_COLOR; // Gán màu ban đầu
        
        return capImg;
    }

    public IEnumerator DisplayCellsSequentially(List<int> valuesToDisplay, bool[] gemStatus, int startIndex, List<CellView> cellViews, System.Action onComplete = null)
    {
        BoardManager.Instance.SetAnimating(true);

        // Tính toán delayTime dựa trên tối đa 27 phần tử để tốc độ vẽ không bị quá chậm
        int countToAnimate = Mathf.Min(valuesToDisplay.Count, 27);
        float delayTime = Mathf.Max(0.03f, 0.5f / countToAnimate);

        AudioManager.Instance.PlaySound(AudioManager.AudioType.Write);

        for (int i = 0; i < valuesToDisplay.Count; i++)
        {
            int currentIndex = startIndex + i;
            
            // 1. Cập nhật dữ liệu và Gem (Logic chung)
            BoardManager.Instance.UpdateOrAddCell(currentIndex, valuesToDisplay[i], gemStatus[i]);

            Transform cellTransform = cellViews[currentIndex].transform;

            // 2. Kiểm tra điều kiện vẽ: Chỉ vẽ hiệu ứng cho 27 phần tử đầu tiên
            if (i < countToAnimate)
            {
                // Hiệu ứng Pop-up mượt mà
                cellTransform.localScale = Vector3.one * 0.8f;
                cellTransform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack, 1.0f).SetLink(cellTransform.gameObject);

                if (gemStatus[i])
                {
                    cellTransform.DOPunchRotation(new Vector3(0, 0, 10f), 0.5f, 2, 0.5f).SetLink(cellTransform.gameObject);
                }

                // Chờ trước khi qua ô tiếp theo
                yield return new WaitForSeconds(delayTime);
            }
            else
            {
                // Các phần tử từ 28 trở đi: Hiện ngay lập tức
                cellTransform.localScale = Vector3.one;
            }
        }

        BoardManager.Instance.SetAnimating(false);
        BoardManager.Instance.UpdateBoard();

        onComplete?.Invoke();
    }

    public void ShakeWrongNumber(List<int> boardData, List<CellView> cellViews, int firstSelected, int secondSelected)
    {
        // Lấy danh sách các ô đang chặn đường
        List<int> blockingIndices = MatchManager.Instance.GetBlockingIndices(firstSelected, secondSelected, boardData);

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
    }

    public void PlayClearLineAnimation(List<CellView> cellViews, int columns, int startA, int startB, bool clearLineA, bool clearLineB, System.Action onComplete)
    {
        StartCoroutine(ClearLineCoroutine(cellViews, columns, startA, startB, clearLineA, clearLineB, onComplete));
    }

    private IEnumerator ClearLineCoroutine(List<CellView> cellViews, int columns, int startA, int startB, bool clearLineA, bool clearLineB, System.Action onComplete)
    {
        BoardManager.Instance.SetAnimating(true);

        float individualDuration = 0.3f; // Thời gian biến mất của 1 ô
        float staggerDelay = 0.05f;      // Độ trễ giữa mỗi ô (tạo hiệu ứng gợn sóng)

        int endA = Mathf.Min(startA + columns, cellViews.Count);
        int endB = Mathf.Min(startB + columns, cellViews.Count);

        // Duyệt qua từng cột để kích hoạt animation đồng thời trên cả 2 dòng
        for (int col = 0; col < columns; col++)
        {
            float currentDelay = col * staggerDelay;

            // Xử lý dòng A
            if (clearLineA)
            {
                int idx = startA + col;
                if (idx < endA && cellViews[idx] != null)
                {
                    int capturedIdx = idx;
                    DOVirtual.DelayedCall(currentDelay, () => {
                        // Check thêm count để an toàn nếu cellViews bị thay đổi
                        if (capturedIdx < cellViews.Count && cellViews[capturedIdx] != null) 
                            cellViews[capturedIdx].PlayClearAnimation(individualDuration);
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
                        if (capturedIdx < cellViews.Count && cellViews[capturedIdx] != null) 
                            cellViews[capturedIdx].PlayClearAnimation(individualDuration);
                    });
                }
            }
        }

        // Chờ đợi toàn bộ chuỗi animation kết thúc
        float totalWaitTime = (columns * staggerDelay) + individualDuration;
        yield return new WaitForSeconds(totalWaitTime);

        BoardManager.Instance.SetAnimating(false);

        // Gọi callback báo cho BoardManager biết đã diễn hoạt xong để xóa data
        onComplete?.Invoke();
    }
}