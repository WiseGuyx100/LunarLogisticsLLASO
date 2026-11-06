using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace PixelPackersMenuKit
{
    public sealed class MenuMotion : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,ISelectHandler,IDeselectHandler
    {
        bool hover;float scale=1;public bool Selected;
        Image image;Text[] labels;Color[] colors;Color normal;
        void Start(){image=GetComponent<Image>();normal=image.color;labels=GetComponentsInChildren<Text>();colors=new Color[labels.Length];for(int i=0;i<labels.Length;i++)colors[i]=labels[i].color;var b=GetComponent<Button>();if(b!=null)b.transition=Selectable.Transition.None;}
        public void OnPointerEnter(PointerEventData e){hover=true;var b=GetComponent<Button>();if(b!=null&&b.interactable)MenuSounds.Play("Hover",.22f);}
        public void OnPointerExit(PointerEventData e){hover=false;}
        public void OnSelect(BaseEventData e){hover=true;}
        public void OnDeselect(BaseEventData e){hover=false;}
        void Update(){var b=GetComponent<Button>();bool lit=(hover||Selected)&&b!=null&&b.interactable;float target=lit?1.018f:1;scale=Mathf.Lerp(scale,target,1-Mathf.Exp(-Time.unscaledDeltaTime*16));transform.localScale=Vector3.one*scale;if(image!=null)image.color=Color.Lerp(image.color,lit?Color.white:normal,1-Mathf.Exp(-Time.unscaledDeltaTime*20));if(labels!=null)for(int i=0;i<labels.Length;i++)labels[i].color=lit?new Color(.07f,.045f,.11f):colors[i];}
    }
    public sealed class MenuTransition : MonoBehaviour
    {
        bool busy;
        public void Run(Action action){if(!busy){MenuSounds.Play("Click",.4f);StartCoroutine(Animate(action));}}
        IEnumerator Animate(Action action)
        {
            busy=true;var group=GetComponent<CanvasGroup>();if(group==null)group=gameObject.AddComponent<CanvasGroup>();group.interactable=false;
            for(float t=0;t<.13f;t+=Time.unscaledDeltaTime){group.alpha=1-t/.13f;yield return null;}
            action();
            if(this==null)yield break;
            for(float t=0;t<.2f;t+=Time.unscaledDeltaTime){group.alpha=Mathf.Clamp01(t/.2f);yield return null;}
            group.alpha=1;group.interactable=true;busy=false;
        }
    }
    public static class SoftPanels
    {
        static Sprite rounded;
        public static Sprite Rounded
        {
            get
            {
                if(rounded!=null)return rounded;
                const int n=64;var t=new Texture2D(n,n,TextureFormat.RGBA32,false);
                for(int y=0;y<n;y++)for(int x=0;x<n;x++){float cx=Mathf.Clamp(x,14,49),cy=Mathf.Clamp(y,14,49);float alpha=Mathf.Clamp01(14.5f-Vector2.Distance(new Vector2(x,y),new Vector2(cx,cy)));t.SetPixel(x,y,new Color(1,1,1,alpha));}
                t.Apply();rounded=Sprite.Create(t,new Rect(0,0,n,n),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,new Vector4(16,16,16,16));return rounded;
            }
        }
    }
}

