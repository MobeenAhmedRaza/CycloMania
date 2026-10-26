using UnityEngine;
namespace Cyclomania
{
    public sealed class RideEnvironment:MonoBehaviour
    {
        public int Weather {get;private set;} public float Hour {get;private set;}=12;
        public bool Cycle;public Transform Target;
        Light sun;GameObject ownedSun;ParticleSystem precipitation;Material rainMaterial;
        UnityEngine.Rendering.AmbientMode oldAmbientMode;bool oldFog;FogMode oldMode;float oldDensity,oldIntensity;Color oldFogColor,oldAmbient,oldSunColor;Quaternion oldRotation;Material oldSkybox;
        public void Initialize()
        {
            oldAmbientMode=RenderSettings.ambientMode;oldFog=RenderSettings.fog;oldMode=RenderSettings.fogMode;oldDensity=RenderSettings.fogDensity;oldFogColor=RenderSettings.fogColor;oldAmbient=RenderSettings.ambientLight;oldSkybox=RenderSettings.skybox;
            sun=RenderSettings.sun;if(sun==null)foreach(var light in FindObjectsByType<Light>(FindObjectsSortMode.None))if(light.type==LightType.Directional){sun=light;break;}
            if(sun==null){ownedSun=new GameObject("Cyclomania sun");sun=ownedSun.AddComponent<Light>();sun.type=LightType.Directional;}
            oldIntensity=sun.intensity;oldSunColor=sun.color;oldRotation=sun.transform.rotation;
            var go=new GameObject("Local precipitation");go.transform.SetParent(transform);precipitation=go.AddComponent<ParticleSystem>();precipitation.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=precipitation.main;main.maxParticles=500;main.simulationSpace=ParticleSystemSimulationSpace.World;main.startLifetime=1.5f;main.startSpeed=18;main.startSize=.06f;
            var shape=precipitation.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(20,20,1);
            var shader=Shader.Find("Particles/Standard Unlit")??Shader.Find("Sprites/Default");if(shader!=null){rainMaterial=new Material(shader);precipitation.GetComponent<ParticleSystemRenderer>().sharedMaterial=rainMaterial;}
        }
        public void Configure(int weather,int preset,bool cycle){Weather=Mathf.Clamp(weather,0,3);Hour=new[]{12f,18f,0f,6f}[Mathf.Clamp(preset,0,3)];Cycle=cycle;Apply();}
        void Update()
        {
            if(Cycle && Time.deltaTime>0){Hour=Mathf.Repeat(Hour+Time.deltaTime/50,24);ApplyLight();}
            if(Target!=null && precipitation!=null)precipitation.transform.SetPositionAndRotation(Target.position+Vector3.up*12,Quaternion.Euler(90,0,0));
        }
        void Apply()
        {
            ApplyLight();var main=precipitation.main;bool snow=Weather==3;main.startSpeed=snow?3:18;main.startLifetime=snow?5:1.5f;main.startSize=snow?.12f:.045f;main.startColor=snow?Color.white:new Color(.6f,.75f,.9f,.65f);
            var emission=precipitation.emission;emission.rateOverTime=Weather==1?200:Weather==3?60:0;
            if(Weather==1 || Weather==3)precipitation.Play();else precipitation.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        void ApplyLight()
        {
            float daylight=Mathf.Clamp01(Mathf.Sin((Hour-6)/24*Mathf.PI*2)*1.5f);
            sun.transform.rotation=Quaternion.Euler(Hour/24*360-90,25,0);sun.intensity=Mathf.Lerp(.12f,1.1f,daylight)*(Weather==0?1:.65f);sun.color=Color.Lerp(new Color(.45f,.55f,.9f),new Color(1,.93f,.8f),daylight);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.skybox=null;RenderSettings.ambientLight=Color.Lerp(new Color(.12f,.16f,.25f),new Color(.65f,.7f,.75f),daylight);
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=Weather==2?.018f:Weather==1?.006f:Weather==3?.009f:.0015f;
            RenderSettings.fogColor=Color.Lerp(new Color(.035f,.05f,.09f),new Color(.58f,.67f,.75f),daylight);
        }
        void OnDestroy()
        {
            RenderSettings.ambientMode=oldAmbientMode;RenderSettings.fog=oldFog;RenderSettings.fogMode=oldMode;RenderSettings.fogDensity=oldDensity;RenderSettings.fogColor=oldFogColor;RenderSettings.ambientLight=oldAmbient;RenderSettings.skybox=oldSkybox;
            if(sun!=null){sun.intensity=oldIntensity;sun.color=oldSunColor;sun.transform.rotation=oldRotation;}if(ownedSun!=null)Destroy(ownedSun);if(rainMaterial!=null)Destroy(rainMaterial);
        }
    }
}
