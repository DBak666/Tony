using UnityEngine.UI;

namespace AmazeRebuild
{
    // Procedural beveled UI mesh: shares the board's hit testing, masks and movement.
    public sealed class CandyArrowGraphic : MaskableGraphic
    {
        public BoardView Owner;
        public int Index;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (Owner) Owner.DrawArrow(vh, Index);
        }
    }
}
