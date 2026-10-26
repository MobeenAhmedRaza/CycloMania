using UnityEngine;
using UnityEngine.UI;

namespace Cyclomania
{
    // Resolution-independent geometry; no texture assets needed for these UI symbols.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TouchControlGraphic : MaskableGraphic
    {
        public enum Symbol { Left, Right, Pedal, Brake, Sprint, Jump, Wheelie, Reset }
        public Symbol symbol;
        private MobileButtonHandler input;
        private Vector2 center;
        private float radius;
        protected override void Awake() { base.Awake(); input = GetComponent<MobileButtonHandler>(); }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = rectTransform.rect;
            center = rect.center;
            radius = Mathf.Min(rect.width, rect.height) * 0.5f - 3f;
            bool pressed = input != null && input.IsPressed;
            Disc(vh, pressed ? new Color(.05f,.64f,.78f,.65f) : new Color(.07f,.10f,.13f,.34f));
            Ring(vh, Vector2.zero, 1f, .028f, pressed ? new Color(.35f,.94f,1f,.95f) : new Color(1,1,1,.5f));
            Color ink = new Color(1,1,1,pressed ? 1f : .85f);
            switch (symbol)
            {
                case Symbol.Left: Chevron(vh, -90, ink); break;
                case Symbol.Right: Chevron(vh, 90, ink); break;
                case Symbol.Pedal: Chevron(vh, 0, ink); break;
                case Symbol.Brake: Chevron(vh, 180, ink); break;
                case Symbol.Sprint:
                    Line(vh,new Vector2(.12f,.56f),new Vector2(-.23f,.1f),.12f,ink);
                    Line(vh,new Vector2(-.23f,.1f),new Vector2(.19f,.1f),.12f,ink);
                    Line(vh,new Vector2(.19f,.1f),new Vector2(-.12f,-.3f),.12f,ink); break;
                case Symbol.Jump:
                    Line(vh,new Vector2(0,-.24f),new Vector2(0,.51f),.09f,ink);
                    Line(vh,new Vector2(-.25f,.25f),new Vector2(0,.51f),.09f,ink);
                    Line(vh,new Vector2(.25f,.25f),new Vector2(0,.51f),.09f,ink);
                    Line(vh,new Vector2(-.37f,-.25f),new Vector2(.37f,-.25f),.055f,ink); break;
                case Symbol.Wheelie:
                    Ring(vh,new Vector2(-.3f,-.2f),.22f,.045f,ink);
                    Ring(vh,new Vector2(.35f,.23f),.22f,.045f,ink);
                    Line(vh,new Vector2(-.3f,-.2f),new Vector2(-.12f,.25f),.06f,ink);
                    Line(vh,new Vector2(-.12f,.25f),new Vector2(.1f,-.05f),.06f,ink);
                    Line(vh,new Vector2(.1f,-.05f),new Vector2(-.3f,-.2f),.06f,ink);
                    Line(vh,new Vector2(.1f,-.05f),new Vector2(.21f,.52f),.06f,ink);
                    Line(vh,new Vector2(.21f,.52f),new Vector2(.35f,.23f),.06f,ink); break;
                case Symbol.Reset:
                    for (int i=0;i<24;i++)
                    {
                        float a=(i*11+25)*Mathf.Deg2Rad,b=((i+1)*11+25)*Mathf.Deg2Rad;
                        Line(vh,new Vector2(Mathf.Cos(a),Mathf.Sin(a))*.4f,new Vector2(Mathf.Cos(b),Mathf.Sin(b))*.4f,.07f,ink);
                    }
                    Line(vh,new Vector2(.36f,.18f),new Vector2(.13f,.24f),.08f,ink);
                    Line(vh,new Vector2(.36f,.18f),new Vector2(.4f,.44f),.08f,ink); break;
            }
        }

        private void Chevron(VertexHelper vh, float angle, Color ink)
        {
            Quaternion rot = Quaternion.Euler(0,0,-angle);
            Line(vh,rot*new Vector2(-.32f,-.15f),rot*new Vector2(0,.24f),.18f,ink);
            Line(vh,rot*new Vector2(0,.24f),rot*new Vector2(.32f,-.15f),.18f,ink);
        }
        private void Disc(VertexHelper vh, Color tint)
        {
            for (int i=0;i<64;i++)
            {
                float a=i*Mathf.PI/32,b=(i+1)*Mathf.PI/32;
                int n=vh.currentVertCount;
                vh.AddVert(center,tint,Vector2.zero);
                vh.AddVert(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,tint,Vector2.zero);
                vh.AddVert(center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,tint,Vector2.zero);
                vh.AddTriangle(n,n+1,n+2);
            }
        }
        private void Ring(VertexHelper vh, Vector2 origin, float r, float width, Color tint)
        {
            for (int i=0;i<64;i++)
            {
                float a=i*Mathf.PI/32,b=(i+1)*Mathf.PI/32;
                Line(vh,origin+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r,origin+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*r,width,tint);
            }
        }
        private void Line(VertexHelper vh, Vector2 from, Vector2 to, float width, Color tint)
        {
            Vector2 normal=new Vector2(-(to-from).y,(to-from).x).normalized*width*.5f;
            int n=vh.currentVertCount;
            vh.AddVert(center+(from-normal)*radius,tint,Vector2.zero);
            vh.AddVert(center+(from+normal)*radius,tint,Vector2.zero);
            vh.AddVert(center+(to+normal)*radius,tint,Vector2.zero);
            vh.AddVert(center+(to-normal)*radius,tint,Vector2.zero);
            vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
        }
    }
}
