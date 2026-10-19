using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AmazeRebuild
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class BoardView : MaskableGraphic, IPointerDownHandler, IPointerUpHandler, IDragHandler, IScrollHandler
    {
        public BoardModel Model { get; private set; }
        public Action<int> Tapped;
        public Color Ink = new Color32(30, 45, 62, 255);
        public Color Highlight = new Color32(233, 150, 26, 255);
        public bool ShowGrid;
        public int HintIndex = -1, FailedIndex = -1, AnimatedIndex = -1;
        public float Travel, Zoom = 1;
        public Vector2 Pan;
        Vector2 down;
        bool dragged, pinch;
        float pinchDistance;
        static readonly Color[] Palette = {
            new Color32(227, 130, 174, 255), new Color32(231, 169, 111, 255),
            new Color32(255, 210, 48, 255), new Color32(133, 184, 48, 255),
            new Color32(118, 205, 160, 255), new Color32(124, 180, 226, 255),
            new Color32(168, 150, 230, 255)
        };
        Color[] arrowColors;
        readonly List<CandyArrowGraphic> arrowGraphics = new List<CandyArrowGraphic>();
        public void Bind(BoardModel model)
        {
            Model = model; Zoom = 1; Pan = Vector2.zero;
            HintIndex = FailedIndex = AnimatedIndex = -1; Travel = 0;
            var random = new System.Random();
            arrowColors = new Color[model.Paths.Length];
            for (int i = 0; i < arrowColors.Length; i++)
                arrowColors[i] = Palette[random.Next(Palette.Length)];
            foreach (var graphic in arrowGraphics) if (graphic) Destroy(graphic.gameObject);
            arrowGraphics.Clear();
            for (int i = 0; i < model.Paths.Length; i++)
            {
                var go = new GameObject("Flat arrow " + i, typeof(RectTransform), typeof(CanvasRenderer));
                var rect = go.GetComponent<RectTransform>();
                rect.SetParent(transform, false); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                var graphic = go.AddComponent<CandyArrowGraphic>();
                graphic.Owner = this; graphic.Index = i; graphic.raycastTarget = false;
                arrowGraphics.Add(graphic);
            }
            Refresh();
        }
        Color ArrowColor(int index)
        {
            if (index == HintIndex)
                return Color.Lerp(new Color32(255, 210, 48, 255), new Color32(255, 242, 171, 255), .5f + .5f * Mathf.Sin(Time.unscaledTime * 6));
            if (index == FailedIndex) return new Color32(245, 112, 119, 255);
            return arrowColors[index];
        }
        public float Cell => Model == null ? 20 : Mathf.Min((rectTransform.rect.width - 62) / Mathf.Max(1, Model.Level.XSize - 1), (rectTransform.rect.height - 90) / Mathf.Max(1, Model.Level.YSize - 1)) * Zoom;
        Vector2 Origin => Pan - new Vector2(Model.Level.XSize - 1, Model.Level.YSize - 1) * Cell * .5f;
        Vector2 At(Vector2 p) => Origin + p * Cell;
        public void Refresh()
        {
            SetVerticesDirty();
            foreach (var graphic in arrowGraphics) if (graphic) graphic.SetVerticesDirty();
        }
        public void SetZoom(float value) { Zoom = Mathf.Clamp(value, .65f, 3.2f); Refresh(); }
        void Update()
        {
            if (HintIndex >= 0 && HintIndex < arrowGraphics.Count) arrowGraphics[HintIndex].SetVerticesDirty();
            if (Input.touchCount == 2)
            {
                float distance = Vector2.Distance(Input.GetTouch(0).position, Input.GetTouch(1).position);
                if (pinch && pinchDistance > 0) SetZoom(Zoom * distance / pinchDistance);
                pinchDistance = distance; pinch = true; dragged = true;
            }
            else if (Input.touchCount == 0) pinch = false;
        }
        public void OnPointerDown(PointerEventData e) { down = e.position; dragged = false; }
        public void OnDrag(PointerEventData e)
        {
            if (Input.touchCount > 1) return;
            if ((e.position - down).sqrMagnitude > 100) dragged = true;
            if (!dragged) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, e.position, e.pressEventCamera, out var a);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, e.position - e.delta, e.pressEventCamera, out var b);
            Pan += a - b;
            Pan = Vector2.Max(-rectTransform.rect.size, Vector2.Min(rectTransform.rect.size, Pan));
            Refresh();
        }
        public void OnPointerUp(PointerEventData e)
        {
            if (dragged || pinch || Model == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, e.position, e.pressEventCamera, out var local);
            int hit = -1; float best = Mathf.Max(Cell * .35f, 10);
            for (int i = 0; i < Model.Paths.Length; i++)
            {
                if (Model.Removed[i]) continue;
                var path = Model.Paths[i];
                for (int j = 0; j < path.Count - 1; j++)
                {
                    var a = At(path[j]); var b = At(path[j + 1]);
                    var ab = b - a;
                    float t = Mathf.Clamp01(Vector2.Dot(local - a, ab) / ab.sqrMagnitude);
                    float d = Vector2.Distance(local, a + t * ab);
                    if (d < best) { best = d; hit = i; }
                }
            }
            if (hit >= 0) Tapped?.Invoke(hit);
        }
        public void OnScroll(PointerEventData e) => SetZoom(Zoom * (e.scrollDelta.y > 0 ? 1.12f : .89f));
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); if (Model == null) return;
            if (ShowGrid)
                for (int x = 0; x < Model.Level.XSize; x++) for (int y = 0; y < Model.Level.YSize; y++)
                {
                    var p = At(new Vector2(x, y));
                    // Cull dots outside the viewport and bound vertices for very large boards.
                    if (!rectTransform.rect.Contains(p) || vh.currentVertCount > 64000) continue;
                    float r = Mathf.Clamp(Cell * .035f, .7f, 1.5f);
                    Disk(vh, p, r, new Color32(115, 120, 149, 110), 8);
                }
        }
        // One Graphic per arrow avoids the UGUI 65k vertex ceiling on dense levels.
        internal void DrawArrow(VertexHelper vh, int i)
        {
            vh.Clear();
            if (Model == null || (Model.Removed[i] && i != AnimatedIndex)) return;
            float cell = Cell, width = cell * .23f;
            var points = GetMovingPath(i, i == AnimatedIndex ? Travel : 0);
            Color color = ArrowColor(i);
            // A single flat color across the entire path, including its solid triangle head.
            for (int j = 0; j < points.Count - 1; j++)
                Segment(vh, At(points[j]), At(points[j + 1]), width, color, color);
            foreach (var p in points) Disk(vh, At(p), width * .5f, color, 12);
            var direction = (Vector2)Model.Direction(i);
            var side = new Vector2(-direction.y, direction.x);
            Vector2 tip = At(points[0]) + direction * cell * .30f;
            Vector2 back = tip - direction * cell * .55f;
            Triangle(vh, tip, back + side * cell * .27f, back - side * cell * .27f, color);
        }
        List<Vector2> GetMovingPath(int i, float travel)
        {
            var original = Model.Paths[i];
            var result = new List<Vector2>();
            float start = -travel, end = original.Count - 1 - travel;
            result.Add(Sample(original, start, Model.Direction(i)));
            for (int j = Mathf.Max(0, Mathf.FloorToInt(start) + 1); j < original.Count - 1 && j < end; j++)
            {
                if (j == 0 || original[j] - original[j - 1] != original[j + 1] - original[j]) result.Add(original[j]);
            }
            result.Add(Sample(original, end, Model.Direction(i)));
            return result;
        }
        static Vector2 Sample(List<Vector2Int> path, float s, Vector2 dir)
        {
            if (s <= 0) return (Vector2)path[0] - dir * s;
            int i = Mathf.Clamp(Mathf.FloorToInt(s), 0, path.Count - 2);
            return Vector2.Lerp(path[i], path[i + 1], s - i);
        }
        static void Triangle(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color color)
        { int n = vh.currentVertCount; vh.AddVert(a, color, Vector2.zero); vh.AddVert(b, color, Vector2.zero); vh.AddVert(c, color, Vector2.zero); vh.AddTriangle(n, n + 1, n + 2); }
        static void Segment(VertexHelper vh, Vector2 a, Vector2 b, float width, Color from, Color to)
        {
            Vector2 dir = (b - a).normalized; Vector2 n = new Vector2(-dir.y, dir.x) * width * .5f;
            int k = vh.currentVertCount;
            vh.AddVert(a + n, from, Vector2.zero); vh.AddVert(b + n, to, Vector2.zero);
            vh.AddVert(b - n, to, Vector2.zero); vh.AddVert(a - n, from, Vector2.zero);
            vh.AddTriangle(k, k + 1, k + 2); vh.AddTriangle(k, k + 2, k + 3);
        }
        static void Disk(VertexHelper vh, Vector2 center, float radius, Color color, int sides)
        {
            int start = vh.currentVertCount; vh.AddVert(center, color, Vector2.zero);
            for (int j = 0; j < sides; j++)
            {
                float angle = j * 2 * Mathf.PI / sides;
                vh.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, color, Vector2.zero);
            }
            for (int j = 0; j < sides; j++) vh.AddTriangle(start, start + 1 + j, start + 1 + (j + 1) % sides);
        }
    }
}
