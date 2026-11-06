using UnityEngine;
using UnityEngine.UI;
namespace PixelPackersMenuKit
{
    public enum ControlIconKind { PC, Switch, Phone, VR }
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ControlIcon : Graphic
    {
        public ControlIconKind kind;
        public static void Attach(Button button,ControlIconKind kind)
        {
            var disc=new GameObject("Control icon badge",typeof(RectTransform),typeof(Image)).GetComponent<RectTransform>();disc.SetParent(button.transform,false);
            disc.anchorMin=disc.anchorMax=disc.pivot=Vector2.up;disc.anchoredPosition=new Vector2(26,-35);disc.sizeDelta=new Vector2(112,112);
            var background=disc.GetComponent<Image>();background.color=new Color(.07f,.035f,.13f);background.sprite=SoftPanels.Rounded;background.type=Image.Type.Sliced;background.raycastTarget=false;
            var graphic=new GameObject("Control icon",typeof(RectTransform),typeof(CanvasRenderer),typeof(ControlIcon)).GetComponent<ControlIcon>();graphic.transform.SetParent(disc,false);
            graphic.rectTransform.anchorMin=Vector2.zero;graphic.rectTransform.anchorMax=Vector2.one;graphic.rectTransform.offsetMin=new Vector2(7,7);graphic.rectTransform.offsetMax=new Vector2(-7,-7);graphic.kind=kind;graphic.raycastTarget=false;
            graphic.color=new Color(.8f,.97f,1);
            graphic.SetAllDirty();
            foreach(string label in new[]{"Title","Detail"})
            {var text=button.transform.Find(label) as RectTransform;if(text==null)continue;text.anchoredPosition=new Vector2(160,text.anchoredPosition.y);text.sizeDelta=new Vector2(((RectTransform)button.transform).sizeDelta.x-190,text.sizeDelta.y);}
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if(kind==ControlIconKind.Phone)
            {Outline(vh,29,9,43,82);Line(vh,42,19,58,19,4);Line(vh,45,80,55,80,4);}
            if(kind==ControlIconKind.PC)
            {Outline(vh,8,10,64,47);Line(vh,40,58,40,68,4);Line(vh,26,68,54,68,4);Outline(vh,9,77,61,15);for(int i=0;i<5;i++)Line(vh,17+i*10,81,17+i*10,87,2);Outline(vh,80,61,15,29);Line(vh,87.5f,65,87.5f,74,3);}
            if(kind==ControlIconKind.Switch)
            {Outline(vh,9,23,82,55);Outline(vh,31,30,38,41);Line(vh,26,24,26,77,3);Line(vh,74,24,74,77,3);Circle(vh,17,41,4);Line(vh,13,60,22,60,3);Line(vh,17.5f,55.5f,17.5f,64.5f,3);Circle(vh,83,61,4);Circle(vh,80,39,2);Circle(vh,87,45,2);Circle(vh,80,50,2);}
            if(kind==ControlIconKind.VR)
            {Outline(vh,10,31,80,39);Line(vh,24,30,32,16,4);Line(vh,32,16,68,16,4);Line(vh,68,16,76,30,4);Line(vh,36,69,43,60,4);Line(vh,43,60,57,60,4);Line(vh,57,60,64,69,4);Circle(vh,30,49,7);Circle(vh,70,49,7);}
        }
        Vector2 Point(float x,float y){var r=rectTransform.rect;return new Vector2(r.x+x*r.width/100,r.y+(100-y)*r.height/100);}
        void Outline(VertexHelper vh,float x,float y,float w,float h){Line(vh,x,y,x+w,y,4);Line(vh,x+w,y,x+w,y+h,4);Line(vh,x+w,y+h,x,y+h,4);Line(vh,x,y+h,x,y,4);}
        void Line(VertexHelper vh,float x,float y,float x2,float y2,float thickness)
        {
            var a=Point(x,y);var b=Point(x2,y2);var d=b-a;var n=new Vector2(-d.y,d.x).normalized*thickness*rectTransform.rect.width/200;
            int index=vh.currentVertCount;var v=UIVertex.simpleVert;v.color=color;
            foreach(var p in new[]{a+n,b+n,b-n,a-n}){v.position=p;vh.AddVert(v);}vh.AddTriangle(index,index+1,index+2);vh.AddTriangle(index,index+2,index+3);
        }
        void Circle(VertexHelper vh,float x,float y,float radius)
        {for(int i=0;i<20;i++){float a=i*Mathf.PI/10,b=(i+1)*Mathf.PI/10;Line(vh,x+Mathf.Cos(a)*radius,y+Mathf.Sin(a)*radius,x+Mathf.Cos(b)*radius,y+Mathf.Sin(b)*radius,3);}}
    }
}
