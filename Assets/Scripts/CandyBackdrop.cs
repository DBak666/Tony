using UnityEngine;
using UnityEngine.UI;

namespace AmazeRebuild
{
    // Soft icy paper background, rendered in UI space without a texture dependency.
    public sealed class CandyBackdrop : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var rect = rectTransform.rect;
            const int divisions = 18;
            for (int y = 0; y <= divisions; y++) for (int x = 0; x <= divisions; x++)
            {
                float u = x / (float)divisions, v = y / (float)divisions;
                float glow = Mathf.Clamp01(1 - Vector2.Distance(new Vector2(u, v), new Vector2(.42f, .57f)) * 1.3f);
                Color c = Color.Lerp(new Color32(210, 232, 253, 255), new Color32(240, 249, 255, 255), glow);
                vh.AddVert(new Vector2(rect.xMin + u * rect.width, rect.yMin + v * rect.height), c, Vector2.zero);
            }
            for (int y = 0; y < divisions; y++) for (int x = 0; x < divisions; x++)
            {
                int i = y * (divisions + 1) + x;
                vh.AddTriangle(i, i + divisions + 1, i + 1);
                vh.AddTriangle(i + 1, i + divisions + 1, i + divisions + 2);
            }
        }
        public static void Attach(RectTransform parent)
        {
            if (parent.Find("Candy sky")) return;
            var go = new GameObject("Candy sky", typeof(RectTransform), typeof(CanvasRenderer), typeof(CandyBackdrop));
            var rect = go.GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.SetAsFirstSibling(); go.GetComponent<CandyBackdrop>().raycastTarget = false;
        }
    }
}
