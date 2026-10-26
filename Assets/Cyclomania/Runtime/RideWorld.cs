using System;
using System.Collections.Generic;
using SBPScripts;
using UnityEngine;

namespace Cyclomania
{
    public sealed class RideDestination
    {
        public readonly string Name, Description;
        public readonly Vector3 Position;
        public readonly float Heading;
        public RideDestination(string name,string description,Vector3 position,float heading)
        { Name=name; Description=description; Position=position; Heading=heading; }
    }

    // One owner per gameplay HUD; world geometry is built once, never on a bicycle swap.
    public sealed class RideWorld : MonoBehaviour
    {
        public static readonly RideDestination[] Destinations={
            new RideDestination("City Centre","Explore the streets and practise urban riding.",new Vector3(76.64f,0,-172.52f),-107.393f),
            new RideDestination("Industrial Yard","Ramps, jumps and a raised balance line.",new Vector3(620,0,-170),0),
            new RideDestination("Mountain Roads","Climb switchbacks and cross the high bridge.",new Vector3(730,0,80),0),
            new RideDestination("Highway","Long straights for bicycle sprint practice.",new Vector3(850,0,-240),0),
            new RideDestination("Desert Trails","Rolling dirt ramps and open trail riding.",new Vector3(1000,0,0),0)};
        public string Area { get; private set; }="City Centre";
        public int LastDestination { get; private set; }
        private GameplayHud hud;
        private Transform root;
        private Material asphalt, accent, concrete, sand;
        private readonly List<Mesh> meshes=new List<Mesh>();
        private float nextCheck;
        public void Initialize(GameplayHud owner)
        {
            hud=owner;
            root=new GameObject("Cyclomania riding areas").transform;
            asphalt=Material("Cycle park asphalt",new Color(.11f,.15f,.19f));
            accent=Material("Course markings",new Color(.03f,.75f,.85f));
            concrete=Material("Ramp concrete",new Color(.55f,.62f,.64f));
            sand=Material("Desert sand",new Color(.65f,.44f,.22f));
            BuildPark();
            BuildRoutes();
            foreach(var destination in Destinations) Beacon(destination);
        }
        private Material Material(string name,Color color)
        { return new Material(Shader.Find("Standard")) {name=name,color=color}; }
        private void BuildPark()
        {
            Box("Cycle park surface",new Vector3(620,-.3f,-110),new Vector3(100,.6f,180),asphalt);
            // Wide, gently graded wedges have continuous collision surfaces at the ground edge.
            Ramp("Starter ramp",new Vector3(600,0,-145),6,12,1.5f,0);
            Ramp("Jump takeoff",new Vector3(625,0,-140),8,12,2.5f,0);
            Ramp("Jump landing",new Vector3(625,0,-100),10,16,2.5f,180);
            Ramp("Balance entry",new Vector3(650,0,-153),2.5f,6,.7f,0);
            Box("Balance line",new Vector3(650,.35f,-138),new Vector3(2.5f,.7f,18),concrete);
            Ramp("Balance exit",new Vector3(650,0,-123),2.5f,6,.7f,180);
            // Circuit markings are flush visual strips, not collision obstacles.
            foreach(float x in new[]{585f,655f}) Box("Circuit edge",new Vector3(x,.015f,-65),new Vector3(.3f,.02f,65),accent,false);
            foreach(float z in new[]{-97f,-33f}) Box("Circuit end",new Vector3(620,.015f,z),new Vector3(70,.02f,.3f),accent,false);
            // Safety curbs leave the south entrance open.
            Box("West curb",new Vector3(570,.2f,-110),new Vector3(.5f,.4f,180),accent);
            Box("East curb",new Vector3(670,.2f,-110),new Vector3(.5f,.4f,180),accent);
            Box("North curb",new Vector3(620,.2f,-20),new Vector3(100,.4f,.5f),accent);
        }
        private void BuildRoutes()
        {
            // The eastern extension sits beyond the imported city buildings.
            Road("East connector",new Vector3(380,0,-300),new Vector3(850,0,-300),12);
            Road("Yard approach",new Vector3(620,0,-300),new Vector3(620,0,-195),12);
            Road("Highway",new Vector3(850,0,-340),new Vector3(850,0,420),24);
            for(int z=-330;z<420;z+=20) Box("Lane dash",new Vector3(850,.025f,z),new Vector3(.18f,.015f,9),accent,false);
            Road("Mountain approach",new Vector3(850,0,80),new Vector3(730,0,80),12);
            Road("Lower ascent",new Vector3(730,0,80),new Vector3(730,10,160),10);
            Road("First switchback",new Vector3(730,10,160),new Vector3(780,14,180),10);
            Road("Upper ascent",new Vector3(780,14,180),new Vector3(730,24,250),10);
            Road("Summit bridge",new Vector3(730,24,250),new Vector3(790,24,250),10);
            Road("Descent",new Vector3(790,24,250),new Vector3(790,0,410),10);
            Road("Mountain return",new Vector3(790,0,410),new Vector3(850,0,410),12);
            // Low-poly rock formations flank the rideable road; they never cover it.
            for(int i=0;i<6;i++) {
                var rock=GameObject.CreatePrimitive(PrimitiveType.Sphere);rock.name="Mountain rock";rock.transform.SetParent(root,false);
                rock.transform.position=new Vector3(700,0,130+i*38);rock.transform.localScale=new Vector3(35,30+i*5,35);rock.GetComponent<Renderer>().sharedMaterial=concrete;rock.isStatic=true;
            }
            Box("Desert ground",new Vector3(1000,-.3f,100),new Vector3(180,.6f,260),sand);
            Road("Desert approach",new Vector3(850,0,0),new Vector3(1000,0,0),12);
            for(int i=0;i<3;i++) {
                float z=35+i*60;
                Ramp("Dune climb",new Vector3(1000,0,z),24,20,2+i,0,sand);
                Ramp("Dune descent",new Vector3(1000,0,z+40),24,20,2+i,180,sand);
            }
            Road("Desert return",new Vector3(1000,0,220),new Vector3(850,0,220),12);
            Road("Canyon approach",new Vector3(1000,0,220),new Vector3(1000,0,250),12);
            Ramp("Canyon takeoff ramp",new Vector3(1000,0,250),12,20,3,0,sand);
            Box("Canyon takeoff deck",new Vector3(1000,1.5f,275),new Vector3(12,3,10),sand);
            Box("Canyon landing deck",new Vector3(1000,1.5f,306),new Vector3(12,3,40),sand);
            Ramp("Canyon exit ramp",new Vector3(1000,0,346),12,20,3,180,sand);
            Box("Canyon floor",new Vector3(1000,-6.25f,283),new Vector3(24,.5f,6),concrete);
        }
        private void Road(string name,Vector3 start,Vector3 end,float width)
        {
            Vector3 delta=end-start;
            var road=Box(name,(start+end)*.5f-Vector3.up*.2f,new Vector3(width,.4f,delta.magnitude+1),asphalt);
            road.transform.rotation=Quaternion.LookRotation(delta,Vector3.up);
            if(start.y>0 || end.y>0) {
                Vector3 side=Vector3.Cross(Vector3.up,delta.normalized)*width*.5f;
                foreach(float sign in new[]{-1f,1f}) {
                    var rail=Box(name+" edge",(start+end)*.5f+side*sign+Vector3.up*.45f,new Vector3(.3f,.7f,Mathf.Max(1,delta.magnitude-12)),accent);
                    rail.transform.rotation=road.transform.rotation;
                }
            }
        }
        private GameObject Box(string name,Vector3 position,Vector3 scale,Material material,bool solid=true)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(root,false);
            go.transform.position=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;
            if(!solid) {go.GetComponent<Collider>().enabled=false;Destroy(go.GetComponent<Collider>());}
            go.isStatic=true;return go;
        }
        private void Ramp(string name,Vector3 start,float width,float length,float height,float yaw,Material surface=null)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));go.transform.SetParent(root,false);
            go.transform.SetPositionAndRotation(start,Quaternion.Euler(0,yaw,0));
            float w=width/2;
            var mesh=new Mesh {name=name};
            mesh.vertices=new[]{new Vector3(-w,0,0),new Vector3(w,0,0),new Vector3(-w,0,length),new Vector3(w,0,length),new Vector3(-w,height,length),new Vector3(w,height,length)};
            meshes.Add(mesh);
            mesh.triangles=new[]{0,4,1,1,4,5,0,2,4,1,5,3,2,3,5,2,5,4,0,1,3,0,3,2};mesh.RecalculateNormals();mesh.RecalculateBounds();
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=surface!=null?surface:concrete;go.GetComponent<MeshCollider>().sharedMesh=mesh;
            go.isStatic=true;
        }
        private void Beacon(RideDestination destination)
        {
            // Mark the roadside, keeping the arrival footprint clear.
            Box(destination.Name+" marker",destination.Position+new Vector3(-5,1.5f,0),new Vector3(.35f,3,.35f),accent,false);
            Box(destination.Name+" map marker",destination.Position+new Vector3(-5,.025f,0),new Vector3(3,.03f,3),accent,false);
            var sign=new GameObject(destination.Name+" sign",typeof(TextMesh));sign.transform.SetParent(root,false);
            sign.transform.position=destination.Position+new Vector3(-5,3.4f,0);sign.transform.rotation=Quaternion.Euler(0,destination.Heading,0);
            var text=sign.GetComponent<TextMesh>();text.text=destination.Name.ToUpperInvariant();text.fontSize=48;text.characterSize=.12f;text.anchor=TextAnchor.MiddleCenter;text.color=Color.white;
            text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");sign.GetComponent<MeshRenderer>().sharedMaterial=text.font.material;
        }
        public bool Travel(int index)
        {
            if(hud==null || index<0 || index>=Destinations.Length) return false;
            var recovery=hud.bicycle!=null?hud.bicycle.GetComponent<CycleRecovery>():null;
            if(recovery==null || !recovery.IsReady) return false;
            hud.Missions?.Interrupt("World travel ended this attempt.");
            var target=Destinations[index];
            recovery.SetCheckpoint(target.Position,target.Heading);recovery.Respawn();
            LastDestination=index;Area=target.Name;return true;
        }
        public static bool OutsidePlayableArea(Vector3 position)
        { return !Finite(position.x) || !Finite(position.y) || !Finite(position.z) || position.x< -450 || position.x>1150 || position.z< -620 || position.z>1000 || position.y< -15; }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private void Update()
        {
            if(hud==null || hud.IsPaused || hud.bicycle==null || Time.unscaledTime<nextCheck) return;
            nextCheck=Time.unscaledTime+.25f;
            var bike=hud.bicycle;var recovery=bike.GetComponent<CycleRecovery>();
            if(recovery==null || !recovery.IsReady) return;
            if(OutsidePlayableArea(bike.transform.position)) {recovery.Respawn();return;}
            Vector3 position=bike.transform.position;
            Area=position.x>=988 && position.x<=1012 && position.z>230 && position.z<=350?"Canyon Course":
                 position.x>=910 && position.z>=-30 && position.z<=230?"Desert Trails":
                 position.x>=830 && position.x<=870 && position.z>=-350 && position.z<=430?"Highway":
                 position.x>=690 && position.x<=815 && position.z>=70 && position.z<=420?"Mountain Roads":
                 position.x>=565 && position.x<=675 && position.z>=-205 && position.z<=-15?"Industrial Yard":"City Streets";
            for(int i=0;i<Destinations.Length;i++)
            {
                float distance=Vector3.Distance(bike.transform.position,Destinations[i].Position);
                if(distance<65) Area=Destinations[i].Name;
                if(distance<8 && bike.IsGrounded && !recovery.IsCrashed && Vector3.Dot(bike.transform.up,Vector3.up)>.8f)
                {LastDestination=i;recovery.SetCheckpoint(Destinations[i].Position,Destinations[i].Heading);}
            }
        }
        private void OnDestroy()
        {
            if(root!=null) Destroy(root.gameObject);
            foreach(var mesh in meshes) if(mesh!=null) Destroy(mesh);
            if(asphalt!=null) Destroy(asphalt);if(accent!=null) Destroy(accent);if(concrete!=null) Destroy(concrete);if(sand!=null) Destroy(sand);
        }
    }
}
