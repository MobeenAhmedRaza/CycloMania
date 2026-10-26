using SBPScripts;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Cyclomania
{
    [DisallowMultipleComponent, RequireComponent(typeof(Canvas))]
    public sealed partial class GameplayHud : MonoBehaviour
    {
        public BicycleController bicycle;
        public bool enableWorld;
        public GameSettings Settings {get;private set;}
        [System.NonSerialized] public string SettingsSaveKey="Cyclomania.Settings.v1";
        public AudioClip backgroundMusic;
        public GameObject bicycleSoundsPrefab;
        private GameObject bicycleSoundsInstance;
        public RideMissions Missions { get; private set; }
        [System.NonSerialized] public string MissionSaveKey="Cyclomania.Career.v1";
        public RideWorld World { get; private set; }
        private Text worldArea;
        public GameObject touchControls;
        [Tooltip("City, BMX, Mountain, Trials, Fatbike. Keep this order for saved setups.")]
        public GameObject[] garagePrefabs;
        public string equippedBicycleName = "City Bicycle";
        public RideSession Session { get; } = new RideSession();
        public bool IsPaused { get; private set; }
        public string CurrentPage { get; private set; } = "Ride";
        private CycleRecovery recovery;
        private BicycleStunts stunts;
        private Text stuntScore, stuntDetail;
        private RectTransform hud, modal, modalSafe, page;
        private Text speed, distance, objective, objectiveDetail, currency, pauseDistance;
        private Image progress;
        private Font font;
        private float savedTimeScale=1, nextRefresh;
        private bool savedAudioPause, savedControlsActive, initialized;
        private readonly Color navy=new Color(.025f,.08f,.14f,.94f);
        private readonly Color cyan=new Color(.05f,.67f,.8f,1);

        private void Start()
        {
            if(bicycle==null) bicycle=FindFirstObjectByType<BicycleController>();
            if(bicycle==null) { Debug.LogError("Gameplay HUD requires a bicycle.",this); enabled=false; return; }
            recovery=bicycle.GetComponent<CycleRecovery>();
            stunts=bicycle.GetComponent<BicycleStunts>();
            font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if(bicycleSoundsPrefab!=null)bicycleSoundsInstance=Instantiate(bicycleSoundsPrefab,transform);
            Settings=gameObject.AddComponent<GameSettings>();Settings.Initialize(SettingsSaveKey,backgroundMusic);
            BuildHud();
            BuildModal();
            initialized=true;
            if(enableWorld) { World=gameObject.AddComponent<RideWorld>(); World.Initialize(this); }
            InitializeGarage();
            if(enableWorld) {Missions=gameObject.AddComponent<RideMissions>();Missions.Initialize(this,MissionSaveKey);}
            if(enableWorld)InitializeAdvanced();
            Refresh();
        }
        private void FixedUpdate()
        {
            if(bicycle!=null && bicycle.rb!=null)
                Session.RecordTravel(bicycle.rb.linearVelocity,Time.fixedDeltaTime,!IsPaused && bicycle.enabled && (recovery==null || !recovery.IsCrashed));
        }
        private void Update()
        {
            if(!initialized) return;
            TickAdvanced();
            TickRecurring();
            if(CurrentPage=="Settings" && page!=null && Settings!=null) {
                var notice=page.Find("SettingsSave")?.GetComponent<Text>();if(notice!=null)notice.text=Settings.SaveNotice;
            }
            if(Missions!=null && Missions.ResultsPending && !changingBicycle) ShowMissionResult();
            if(!changingBicycle && Input.GetKeyDown(KeyCode.Escape))
            {
                if(!IsPaused) Pause();
                else if(CurrentPage!="Pause") ShowPausePage();
                else Resume();
            }
            if(Time.unscaledTime>=nextRefresh) { nextRefresh=Time.unscaledTime+.1f; Refresh(); }
        }
        public void Pause()
        {
            if(!initialized || IsPaused) return;
            savedTimeScale=Time.timeScale;
            savedAudioPause=AudioListener.pause;
            savedControlsActive=touchControls!=null && touchControls.activeSelf;
            if(bicycle!=null) bicycle.ClearInput();
            if(touchControls!=null) touchControls.SetActive(false);
            Time.timeScale=0;
            AudioListener.pause=true;
            IsPaused=true;
            modal.gameObject.SetActive(true);
            ShowPausePage();
        }
        public void Resume()
        {
            if(!IsPaused || changingBicycle) return;
            if(bicycle!=null) bicycle.ClearInput();
            Time.timeScale=savedTimeScale;
            AudioListener.pause=savedAudioPause;
            if(touchControls!=null) touchControls.SetActive(savedControlsActive);
            IsPaused=false;
            CurrentPage="Ride";
            if(modal!=null) modal.gameObject.SetActive(false);
        }
        public void RestartRide()
        {
            if(recovery==null) return;
            Missions?.Interrupt("Restart ended this attempt.");
            recovery.Respawn();
            Session.Restart();
            if(stunts!=null) stunts.RestartSession();
            Refresh();
            Resume();
        }
        public void ShowWorld()
        {
            if(!initialized || World==null || changingBicycle) return;
            if(!IsPaused) Pause();
            NewPage("World","CHOOSE A RIDING AREA");
            for(int i=0;i<RideWorld.Destinations.Length;i++) {
                int destination=i;var spot=RideWorld.Destinations[i];float y=125-i*73;
                Label(page,"Destination"+i,spot.Name+" — "+spot.Description,22,new Vector2(-130,y),new Vector2(710,65));
                ActionButton(page,"Travel"+i,"RIDE HERE",new Vector2(390,y),()=>{if(World.Travel(destination)) Resume();},new Vector2(220,60));
            }
            ActionButton(page,"Back","BACK",new Vector2(-370,-245),ShowPausePage,new Vector2(220,55));
            Label(page,"Travel hint","Travel preserves score and distance. Reset returns to your last safe destination.",18,new Vector2(135,-245),new Vector2(710,45));
        }
        public void ShowSettings()
        {
            if(!initialized)return;if(!IsPaused)Pause();
            NewPage("Settings","SETTINGS");
            SettingsSlider("SoundFX","SOUND FX",105,Settings.Preferences.effects,Settings.SetEffects);
            SettingsSlider("Music","MUSIC",0,Settings.Preferences.music,Settings.SetMusic);
            Label(page,"GraphicsLabel","GRAPHIC QUALITY",24,new Vector2(0,-90),new Vector2(900,35));
            string[] names={"LOW","MEDIUM","HIGH","2K"};
            for(int i=0;i<4;i++) {int selected=i;var button=ActionButton(page,"Quality"+i,names[i],new Vector2(-300+i*200,-140),()=>{Settings.SetGraphics(selected);ShowSettings();},new Vector2(180,50));
                button.interactable=Settings.Preferences.graphics!=i;
                if(!button.interactable){button.image.color=new Color(1,.75f,.05f);var colors=button.colors;colors.disabledColor=Color.white;button.colors=colors;button.GetComponentInChildren<Text>().color=navy;}
            }
            Label(page,"SettingsHint","Low: 30 FPS · Others: 60 FPS target · 2K: up to 2560 px on supported screens",17,new Vector2(0,-190),new Vector2(1000,30));
            Label(page,"SettingsSave",Settings.SaveNotice,17,new Vector2(0,-220),new Vector2(1000,25));
            ActionButton(page,"Back","BACK",new Vector2(-370,-260),()=>{Settings.Flush();ShowPausePage();},new Vector2(220,50));
        }
        private void SettingsSlider(string name,string title,float y,float value,UnityAction<float> changed)
        {
            var label=Label(page,name+"Label",title+$"  {value*100:0}%",24,new Vector2(0,y+35),new Vector2(900,35));
            var root=Rect(name,page,new Vector2(0,y-5),new Vector2(650,55));
            Panel("Track",root,Vector2.zero,new Vector2(650,12),new Color(.1f,.2f,.3f));
            var fillArea=Rect("Fill area",root,Vector2.zero,new Vector2(620,12));
            var fill=Panel("Fill",fillArea,Vector2.zero,Vector2.zero,cyan);Stretch(fill.rectTransform);
            var handleArea=Rect("Handle area",root,Vector2.zero,new Vector2(620,55));
            var handle=Panel("Handle",handleArea,Vector2.zero,new Vector2(32,48),Color.white);
            var slider=root.gameObject.AddComponent<Slider>();slider.fillRect=fill.rectTransform;slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;
            slider.minValue=0;slider.maxValue=1;slider.SetValueWithoutNotify(value);
            slider.onValueChanged.AddListener(v=>{changed(v);label.text=title+$"  {v*100:0}%";});
        }
        private void ShowPausePage()
        {
            NewPage("Pause","RIDE PAUSED");
            pauseDistance=Label(page,"RideDistance",DistanceText(),34,new Vector2(0,125),new Vector2(800,50));
            Label(page,"Score",$"SCORE {stunts?.Scoring.Score ?? 0}    BEST COMBO {stunts?.Scoring.BestCombo ?? 0}",26,new Vector2(0,75),new Vector2(800,36));
            Label(page,"Objective",Session.Objective,24,new Vector2(0,25),new Vector2(800,40));
            ActionButton(page,"Resume","RESUME",new Vector2(-310,-55),Resume);
            var restart=ActionButton(page,"Restart","RESTART RIDE",new Vector2(0,-55),RestartRide);
            restart.interactable=recovery!=null;
            ActionButton(page,"Garage","GARAGE",new Vector2(310,-55),ShowGarage);
            ActionButton(page,"Settings","SETTINGS",new Vector2(enableWorld?-310:0,-160),ShowSettings);
            if(enableWorld) ActionButton(page,"World","WORLD",new Vector2(0,-160),ShowWorld);
            if(Missions!=null) ActionButton(page,"Missions","MISSIONS",new Vector2(310,-160),ShowMissions);
            if(Missions!=null) {
                ActionButton(page,"Freestyle","FREESTYLE",new Vector2(0,-240),ShowFreestyle,new Vector2(280,55));
                ActionButton(page,"Shop","SHOP",new Vector2(-350,-240),ShowShop,new Vector2(280,55));
                ActionButton(page,"Advanced","ADVANCED",new Vector2(350,-240),ShowAdvanced,new Vector2(280,55));
            }
            else Label(page,"Stunt controls","JUMP: hold / release    |    WHEELIE + PEDAL: wheelie / backflip    |    WHEELIE + BRAKE: stoppie / frontflip",18,new Vector2(0,-240),new Vector2(1020,45));
        }
        private void BuildHud()
        {
            hud=Rect("Gameplay HUD",transform,Vector2.zero,Vector2.zero); Stretch(hud);
            hud.gameObject.AddComponent<SafeAreaPanel>();
            var map=Rect("Minimap",hud,new Vector2(160,-160),new Vector2(240,240),new Vector2(0,1));
            var border=map.gameObject.AddComponent<HudShape>(); border.color=new Color(1,1,1,.65f); border.raycastTarget=false;
            var mask=Rect("Map mask",map,Vector2.zero,new Vector2(230,230));
            mask.gameObject.AddComponent<HudShape>().raycastTarget=false;
            mask.gameObject.AddComponent<Mask>().showMaskGraphic=false;
            var mapImage=Rect("Map image",mask,Vector2.zero,Vector2.zero).gameObject.AddComponent<RawImage>(); Stretch(mapImage.rectTransform); mapImage.raycastTarget=false;
            var arrow=Rect("Rider",mask,Vector2.zero,new Vector2(22,30)); var graphic=arrow.gameObject.AddComponent<HudShape>(); graphic.triangle=true; graphic.color=new Color(.3f,1,.9f); graphic.raycastTarget=false;
            Label(map,"North","N",22,new Vector2(0,100),new Vector2(30,30));
            map.gameObject.AddComponent<RideMinimap>().Initialize(bicycle.transform,mapImage,arrow);
            if(enableWorld) worldArea=Label(hud,"Area","DOWNTOWN",21,new Vector2(160,-305),new Vector2(300,45));
            if(worldArea!=null) { worldArea.rectTransform.anchorMin=worldArea.rectTransform.anchorMax=new Vector2(0,1); }
            var speedPanel=Panel("Speed panel",hud,new Vector2(0,-65),new Vector2(320,110),navy,new Vector2(.5f,1));
            speed=Label(speedPanel.transform,"Speed","0 KM/H",44,new Vector2(0,16),new Vector2(300,58)); speed.color=new Color(1,.85f,.15f);
            distance=Label(speedPanel.transform,"Distance","0.00 KM",22,new Vector2(0,-31),new Vector2(300,32));
            var objectivePanel=Panel("Objective panel",hud,new Vector2(0,-190),new Vector2(560,115),navy,new Vector2(.5f,1));
            objective=Label(objectivePanel.transform,"Title","FREE RIDE",25,new Vector2(0,29),new Vector2(530,35));
            objectiveDetail=Label(objectivePanel.transform,"Detail","",20,new Vector2(0,-7),new Vector2(530,40));
            progress=Panel("Progress",objectivePanel.transform,new Vector2(-250,-45),new Vector2(500,5),cyan);
            progress.rectTransform.pivot=new Vector2(0,.5f);
            var stuntPanel=Panel("Stunt panel",hud,new Vector2(0,-315),new Vector2(560,110),navy,new Vector2(.5f,1));
            stuntScore=Label(stuntPanel.transform,"Score","SCORE 0",26,new Vector2(0,28),new Vector2(530,36));
            stuntDetail=Label(stuntPanel.transform,"Detail","LAND STUNTS TO SCORE",20,new Vector2(0,-16),new Vector2(530,50));
            var currencies=Panel("Balances",hud,new Vector2(-320,-65),new Vector2(260,75),navy,new Vector2(1,1));
            currency=Label(currencies.transform,"Currencies","COINS 0    GEMS 0",22,Vector2.zero,new Vector2(250,65));
            ActionButton(hud,"Pause","PAUSE",new Vector2(-100,-65),Pause,new Vector2(150,75),new Vector2(1,1));
        }
        private void BuildModal()
        {
            modal=Panel("Pause overlay",transform,Vector2.zero,Vector2.zero,new Color(0,0,0,.72f)).rectTransform; Stretch(modal);
            modalSafe=Rect("Safe content",modal,Vector2.zero,Vector2.zero); Stretch(modalSafe); modalSafe.gameObject.AddComponent<SafeAreaPanel>();
            modal.gameObject.SetActive(false);
        }
        private void NewPage(string name,string title)
        {
            if(page!=null) { page.gameObject.SetActive(false); Destroy(page.gameObject); }
            CurrentPage=name;
            page=Panel(name,modalSafe,Vector2.zero,new Vector2(1100,570),navy).rectTransform;
            Panel("Accent",page,new Vector2(0,280),new Vector2(1100,5),cyan).raycastTarget=false;
            Label(page,"Title",title,42,new Vector2(0,205),new Vector2(1000,65));
        }
        private string DistanceText() => $"DISTANCE  {Session.DistanceMeters/1000f:0.00} KM";
        private void Refresh()
        {
            if(speed==null) return;
            if(worldArea!=null && World!=null) worldArea.text=World.Area.ToUpperInvariant();
            speed.text=$"{bicycle.SpeedKmh:0} KM/H";
            distance.text=$"{Session.DistanceMeters/1000f:0.00} KM";
            bool crashed=recovery!=null && recovery.IsCrashed;
            objective.text=crashed ? "RECOVERING" : Session.Objective;
            objectiveDetail.text=crashed ? "Getting you back on your bicycle…" : Session.ObjectiveDetail;
            progress.gameObject.SetActive(!crashed && Session.ObjectiveProgress>=0);
            progress.rectTransform.sizeDelta=new Vector2(500*Mathf.Max(0,Session.ObjectiveProgress),5);
            currency.text=$"COINS {Session.Coins}    GEMS {Session.Gems}";
            if(stunts!=null)
            {
                var scoring=stunts.Scoring;
                stuntScore.text=$"SCORE {scoring.Score}   COMBO {scoring.PendingScore} (x{scoring.Multiplier})";
                string detail=scoring.Message;
                if(scoring.ActiveKind!=StuntKind.None) detail+=$"  {scoring.ActiveDuration:0.0}s / {scoring.ActiveDistance:0.0}m";
                else if(scoring.Airtime>0) detail+=$"  {scoring.Airtime:0.0}s";
                stuntDetail.text=detail;
            }
            if(pauseDistance!=null) pauseDistance.text=DistanceText();
        }
        private RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size,Vector2? anchor=null)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>(); rect.gameObject.layer=5;
            rect.SetParent(parent,false); rect.anchorMin=rect.anchorMax=anchor??new Vector2(.5f,.5f); rect.pivot=new Vector2(.5f,.5f); rect.anchoredPosition=position; rect.sizeDelta=size; return rect;
        }
        private static void Stretch(RectTransform rect) { rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero; }
        private Image Panel(string name,Transform parent,Vector2 pos,Vector2 size,Color tint,Vector2? anchor=null)
        {
            var image=Rect(name,parent,pos,size,anchor).gameObject.AddComponent<Image>(); image.color=tint; return image;
        }
        private Text Label(Transform parent,string name,string value,int size,Vector2 pos,Vector2 dimensions)
        {
            var text=Rect(name,parent,pos,dimensions).gameObject.AddComponent<Text>(); text.font=font; text.text=value; text.fontSize=size; text.color=Color.white;
            text.alignment=TextAnchor.MiddleCenter; text.raycastTarget=false; text.resizeTextForBestFit=true; text.resizeTextMinSize=16; text.resizeTextMaxSize=size; return text;
        }
        private Button ActionButton(Transform parent,string name,string title,Vector2 pos,UnityAction action,Vector2? size=null,Vector2? anchor=null)
        {
            var image=Panel(name,parent,pos,size??new Vector2(280,80),cyan,anchor); var button=image.gameObject.AddComponent<Button>(); button.targetGraphic=image;
            button.onClick.AddListener(action); var navigation=button.navigation; navigation.mode=Navigation.Mode.None; button.navigation=navigation;
            Label(image.transform,"Label",title,25,Vector2.zero,(size??new Vector2(280,80))-new Vector2(15,10)); return button;
        }
        private void OnApplicationPause(bool paused) { if(paused) {FlushRecurring();Pause();} }
        private void OnApplicationFocus(bool focused) { if(!focused) {FlushRecurring();Pause();} }
        private void OnEnable() { if(hud!=null) hud.gameObject.SetActive(true); }
        private void OnDisable() { Missions?.Interrupt("Mission interrupted."); changingBicycle=false; Resume(); if(hud!=null) hud.gameObject.SetActive(false); }
        private void OnDestroy()
        {
            changingBicycle=false;
            FlushRecurring();
            CloseGarage();
            CloseAdvanced();
            if(Settings!=null)Destroy(Settings);
            if(bicycleSoundsInstance!=null)Destroy(bicycleSoundsInstance);
            Resume();
            if(hud!=null) Destroy(hud.gameObject);
            if(modal!=null) Destroy(modal.gameObject);
        }
    }
}
