using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace PixelPackersMenuKit
{
    [DisallowMultipleComponent]
    public sealed class PortableMenuController : MonoBehaviour
    {
        [Header("Set these to scenes in YOUR project's build scene list")]
        public string gameScene="",tutorialScene="",timedScene="",lunarScene="",multiplayerScene="";
        [Header("Optional integration callbacks")]
        public UnityEvent<string> onControlSelected=new UnityEvent<string>();
        public UnityEvent<string> onModeSelected=new UnityEvent<string>();
        public UnityEvent<float> onSensitivityChanged=new UnityEvent<float>();
        public static string SelectedControl {get;private set;}="PC";
        public static string SelectedMode {get;private set;}="Practice";
        RectTransform page;Font font;string mode,destination;
        static readonly Color Ink=new Color(.025f,.025f,.045f),Card=new Color(.16f,.045f,.29f),Mint=new Color(.28f,.83f,.94f),Muted=new Color(.70f,.77f,.85f);
        void Start()
        {
            Time.timeScale=1;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if(Camera.main==null){var camera=new GameObject("Menu Camera",typeof(Camera),typeof(AudioListener));camera.tag="MainCamera";camera.GetComponent<Camera>().clearFlags=CameraClearFlags.SolidColor;camera.GetComponent<Camera>().backgroundColor=Ink;}
            if(EventSystem.current==null)new GameObject("Menu EventSystem",typeof(EventSystem));
            var events=EventSystem.current;
#if ENABLE_INPUT_SYSTEM
            foreach(var module in events.GetComponents<BaseInputModule>())if(!(module is InputSystemUIInputModule))module.enabled=false;
            if(events.GetComponent<InputSystemUIInputModule>()==null)events.gameObject.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
#else
            if(events.GetComponent<StandaloneInputModule>()==null)events.gameObject.AddComponent<StandaloneInputModule>();
#endif
            var canvas=new GameObject("Pixel Packers Portable Menu",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));canvas.transform.SetParent(transform,false);
            canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            page=canvas.GetComponent<RectTransform>();AudioListener.volume=PlayerPrefs.GetFloat("PP.MenuKit.Volume",.8f);Home();
        }
        RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size)
        {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=Vector2.up;r.anchoredPosition=position;r.sizeDelta=size;return r;}
        RectTransform Box(string name,Transform parent,Vector2 position,Vector2 size,Color color)
        {var r=Rect(name,parent,position,size);var image=r.gameObject.AddComponent<Image>();image.sprite=SoftPanels.Rounded;image.type=Image.Type.Sliced;image.color=color;image.raycastTarget=false;return r;}
        Text Label(Transform parent,string name,string text,Vector2 pos,Vector2 size,int pixels,Color color)
        {var r=Rect(name,parent,pos,size);var t=r.gameObject.AddComponent<Text>();t.font=font;t.fontSize=pixels;t.fontStyle=pixels>=28?FontStyle.Bold:FontStyle.Normal;t.text=text;t.color=color;t.raycastTarget=false;t.supportRichText=false;return t;}
        Button Button(string name,string title,string detail,Vector2 pos,Vector2 size,Action action,bool active=true)
        {
            var r=Box(name,page,pos,size,Card);r.GetComponent<Image>().raycastTarget=true;var b=r.gameObject.AddComponent<Button>();b.targetGraphic=r.GetComponent<Image>();b.interactable=active;r.gameObject.AddComponent<MenuMotion>();
            Box("Accent",r,Vector2.zero,new Vector2(5,size.y),active?new Color(.63f,.87f,.29f):Muted);
            Label(r,"Title",title,new Vector2(28,-20),new Vector2(size.x-56,40),28,active?Color.white:Muted);
            Label(r,"Detail",detail,new Vector2(28,-67),new Vector2(size.x-56,size.y-72),19,Muted);
            if(action!=null)b.onClick.AddListener(()=>{var animation=page.GetComponent<MenuTransition>();if(animation==null)animation=page.gameObject.AddComponent<MenuTransition>();animation.Run(action);});return b;
        }
        void Clear()
        {
            foreach(Transform child in page){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            var image=page.GetComponent<Image>();if(image==null)image=page.gameObject.AddComponent<Image>();image.color=Ink;
        }
        public void Home()
        {
            Clear();var texture=Resources.Load<Texture2D>("PixelPackersMenuKit/MenuBackdrop");
            var bg=Rect("Lunar backdrop",page,Vector2.zero,new Vector2(1920,1080)).gameObject.AddComponent<RawImage>();bg.texture=texture;bg.color=new Color(.5f,.5f,.6f,.7f);bg.raycastTarget=false;
            Box("Operation field",page,new Vector2(930,0),new Vector2(990,1080),new Color(.045f,.025f,.075f,.92f));
            Label(page,"NASA heading","NASA HUNCH  /  LLASO",new Vector2(110,-100),new Vector2(700,45),23,Mint);
            var logo=Resources.Load<Texture2D>("PixelPackersMenuKit/TeamLogo");var logoRect=Rect("Transparent team logo",page,new Vector2(115,-170),new Vector2(690,logo!=null?690f*logo.height/logo.width:460));var art=logoRect.gameObject.AddComponent<RawImage>();art.texture=logo;art.raycastTarget=false;
            Label(page,"Tagline","PACKING MADE PRACTICAL",new Vector2(115,-700),new Vector2(730,55),30,Mint);
            Label(page,"Description","Every supply has a destination.\nScan, carry, and organize cargo for the mission ahead.",new Vector2(115,-760),new Vector2(730,110),25,Muted);
            Label(page,"Operation heading","CHOOSE YOUR OPERATION",new Vector2(1020,-153),new Vector2(800,45),26,Mint);
            var first=Button("Tutorial","GUIDED TUTORIAL","Pick up, scan, place, then log.",new Vector2(1020,-220),new Vector2(780,125),()=>Controls("Tutorial",tutorialScene));
            Button("Play","SINGLE PLAYER / PRACTICE","Explore the warehouse without a time limit.",new Vector2(1020,-360),new Vector2(780,125),()=>Controls("Practice",gameScene));
            Button("Timed","TIME & EFFICIENCY","Store cargo and compare your results.",new Vector2(1020,-500),new Vector2(780,125),()=>Controls("Timed",string.IsNullOrWhiteSpace(timedScene)?gameScene:timedScene));
            Button("Day missions","DAY MISSIONS / COMING SOON","Daily cargo objectives are being prepared.",new Vector2(1020,-640),new Vector2(780,125),null,false);
            Button("Local multiplayer","LOCAL MULTIPLAYER","Team and VS",new Vector2(1020,-780),new Vector2(780,110),()=>Load("LocalMultiplayer",multiplayerScene));
            Button("Settings","SETTINGS","Sound and look sensitivity",new Vector2(1020,-910),new Vector2(370,125),Settings);
            Button("Quit","QUIT","Close the application",new Vector2(1430,-910),new Vector2(370,125),()=>Application.Quit());
            Button("Lunar preview","LUNAR MAP UNFINISHED","Preview the lunar work area",new Vector2(110,-875),new Vector2(700,110),()=>Load("Lunar",lunarScene));
            Label(page,"Footer","PIXEL PACKERS  /  EARTH OPERATIONS PROTOTYPE",new Vector2(110,-1015),new Vector2(850,35),17,Muted);
            if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(first.gameObject);
        }
        void Controls(string playMode,string scene)
        {
            mode=playMode;destination=scene;Clear();Label(page,"Heading","HOW WILL YOU PLAY?",new Vector2(150,-125),new Vector2(1600,100),64,Color.white);
            Label(page,"Note","Choose your controls for "+playMode+".",new Vector2(155,-240),new Vector2(1600,70),28,Muted);
            var pc=Button("PC","PC / KEYBOARD + MOUSE","Keyboard and mouse controls",new Vector2(160,-350),new Vector2(770,185),()=>Choose("PC"));ControlIcon.Attach(pc,ControlIconKind.PC);
            ControlIcon.Attach(Button("Controller","NINTENDO SWITCH CONTROLLER","Connect a compatible controller first.",new Vector2(990,-350),new Vector2(770,185),()=>Choose("Gamepad")),ControlIconKind.Switch);
            ControlIcon.Attach(Button("Phone","PHONE / SCAN TO CONNECT","Phone controller connection",new Vector2(160,-570),new Vector2(770,185),()=>Choose("Phone")),ControlIconKind.Phone);
            ControlIcon.Attach(Button("VR","VR / QUEST","Quest Link controls",new Vector2(990,-570),new Vector2(770,185),()=>Choose("VR")),ControlIconKind.VR);
            Button("Back","BACK","Choose an operation",new Vector2(160,-855),new Vector2(600,130),Home);if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(pc.gameObject);
        }
        void Choose(string controls){SelectedControl=controls;onControlSelected.Invoke(controls);Load(mode,destination);}
        void Load(string playMode,string scene)
        {
            SelectedMode=playMode;onModeSelected.Invoke(playMode);
            if(!string.IsNullOrWhiteSpace(scene)&&Application.CanStreamedLevelBeLoaded(scene)){SceneManager.LoadScene(scene);return;}
            Clear();Label(page,"Not configured","THIS OPERATION IS NOT READY",new Vector2(155,-200),new Vector2(1600,100),52,Color.white);
            Label(page,"Unavailable note","The game scene for this operation hasn't been connected yet.",new Vector2(160,-360),new Vector2(1500,130),30,Muted);
            Button("Back","BACK","Return to the menu",new Vector2(160,-750),new Vector2(700,140),Home);
        }
        void Settings()
        {
            Clear();Label(page,"Settings title","SETTINGS",new Vector2(150,-160),new Vector2(1500,110),72,Color.white);
            Slider("MASTER VOLUME",380,PlayerPrefs.GetFloat("PP.MenuKit.Volume",.8f),0,1,value=>{AudioListener.volume=value;PlayerPrefs.SetFloat("PP.MenuKit.Volume",value);});
            Slider("LOOK SENSITIVITY",550,PlayerPrefs.GetFloat("PP.MenuKit.Sensitivity",.12f),.03f,.3f,value=>{PlayerPrefs.SetFloat("PP.MenuKit.Sensitivity",value);onSensitivityChanged.Invoke(value);});
            Button("Back","BACK","Return to your operation",new Vector2(160,-790),new Vector2(760,145),()=>{PlayerPrefs.Save();Home();});
        }
        void Slider(string title,float y,float value,float min,float max,Action<float> changed)
        {
            Label(page,title,title,new Vector2(160,-y),new Vector2(630,60),29,Muted);var root=Box(title+" slider",page,new Vector2(840,-y),new Vector2(750,42),Card);root.GetComponent<Image>().raycastTarget=true;
            var fillArea=Stretch("Fill area",root,new Vector2(15,12),new Vector2(-15,-12));var fill=Stretch("Fill",fillArea,Vector2.zero,Vector2.zero);fill.gameObject.AddComponent<Image>().color=Mint;
            var handles=Stretch("Handle area",root,new Vector2(15,0),new Vector2(-15,0));var handle=Stretch("Handle",handles,new Vector2(-13,0),new Vector2(13,0));handle.anchorMax=new Vector2(0,1);var image=handle.gameObject.AddComponent<Image>();image.sprite=SoftPanels.Rounded;image.type=Image.Type.Sliced;
            var slider=root.gameObject.AddComponent<Slider>();slider.fillRect=fill;slider.handleRect=handle;slider.targetGraphic=image;slider.minValue=min;slider.maxValue=max;slider.value=value;slider.onValueChanged.AddListener(v=>changed(v));
        }
        RectTransform Stretch(string name,Transform parent,Vector2 min,Vector2 max)
        {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=min;r.offsetMax=max;return r;}
    }
    public static class MenuSounds
    {
        // Hook retained for the UI animation scripts; no game audio manager dependency.
        public static void Play(string cue,float volume){}
    }
}
