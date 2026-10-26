using UnityEngine;
using UnityEngine.UI;

namespace Cyclomania
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HudShape : MaskableGraphic
    {
        public bool triangle;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = rectTransform.rect;
            if (triangle)
            {
                vh.AddVert(new Vector2(r.center.x, r.yMax), color, Vector2.zero);
                vh.AddVert(new Vector2(r.xMin, r.yMin), color, Vector2.zero);
                vh.AddVert(new Vector2(r.center.x, r.yMin + r.height * .25f), color, Vector2.zero);
                vh.AddVert(new Vector2(r.xMax, r.yMin), color, Vector2.zero);
                vh.AddTriangle(0,1,2); vh.AddTriangle(0,2,3);
                return;
            }
            vh.AddVert(r.center, color, Vector2.zero);
            for (int i=0;i<=64;i++)
            {
                float a=i*Mathf.PI/32;
                vh.AddVert(r.center + new Vector2(Mathf.Cos(a)*r.width,Mathf.Sin(a)*r.height)*.5f, color, Vector2.zero);
                if(i>0) vh.AddTriangle(0,i,i+1);
            }
        }
    }
}
