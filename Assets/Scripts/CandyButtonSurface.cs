using UnityEngine;
using UnityEngine.UI;

namespace AmazeRebuild
{
    public sealed class CandyButtonSurface : MaskableGraphic
    {
        Image source;
        Color previous;
        public static void Attach(Image source)
        {
            if (source.transform.Find("Candy finish")) return;
            var go = new GameObject("Candy finish", typeof(RectTransform), typeof(CanvasRenderer));
            var rect = go.GetComponent<RectTransform>(); rect.SetParent(source.transform, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero; rect.SetAsFirstSibling();
            var finish = go.AddComponent<CandyButtonSurface>(); finish.source = source; finish.raycastTarget = false;
        }
        void LateUpdate()
        {
            if (source && previous != source.color) { previous = source.color; SetVerticesDirty(); }
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); if (!source) return;
            var rect = rectTransform.rect; Color c = source.color;
            Rounded(vh, new Rect(rect.x, rect.y - 3, rect.width, rect.height), Color.Lerp(c, Color.black, .65f), Color.Lerp(c, Color.black, .65f));
            Rounded(vh, rect, Color.Lerp(c, Color.black, .42f), Color.Lerp(c, Color.black, .2f));
            Rounded(vh, Inset(rect, 2), Color.Lerp(c, Color.black, .18f), Color.Lerp(c, Color.white, .85f));
            Rounded(vh, Inset(rect, 5), c, Color.Lerp(c, Color.white, .27f));
            var gloss = Inset(rect, 8); gloss.y += gloss.height * .56f; gloss.height *= .4f;
            Rounded(vh, gloss, Color.Lerp(c, Color.white, .12f), Color.Lerp(c, Color.white, .5f));
        }
        static Rect Inset(Rect rect, float amount) => new Rect(rect.x + amount, rect.y + amount, rect.width - amount * 2, rect.height - amount * 2);
        static void Rounded(VertexHelper vh, Rect rect, Color bottom, Color top)
        {
            if (rect.width <= 0 || rect.height <= 0) return;
            float r = Mathf.Min(rect.height * .5f, 24);
            int first = vh.currentVertCount; vh.AddVert(rect.center, Color.Lerp(bottom, top, .5f), Vector2.zero);
            for (int corner = 0; corner < 4; corner++) for (int step = 0; step <= 6; step++)
            {
                float angle = (corner * 90 + step * 15) * Mathf.Deg2Rad;
                Vector2 center = new Vector2(corner == 0 || corner == 3 ? rect.xMax - r : rect.xMin + r, corner < 2 ? rect.yMax - r : rect.yMin + r);
                Vector2 p = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
                vh.AddVert(p, Color.Lerp(bottom, top, Mathf.InverseLerp(rect.yMin, rect.yMax, p.y)), Vector2.zero);
            }
            for (int i = 0; i < 28; i++) vh.AddTriangle(first, first + i + 1, first + (i + 1) % 28 + 1);
        }
    }
}
