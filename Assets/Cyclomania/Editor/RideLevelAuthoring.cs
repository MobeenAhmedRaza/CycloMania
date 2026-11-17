using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;
namespace Cyclomania.Editor
{
    // Authoring only: the resulting geometry, meshes, materials and anchors are saved assets.
    public sealed class RideLevelAuthoring
    {
        private const string AssetFolder="Assets/Cyclomania/LevelAssets";
        private Transform root;
        private Material asphalt,accent,concrete,sand;
        private RideLevelLayout layout;
        [MenuItem("Cyclomania/Author levels in current map")]
        public static void Author()
        {
            if(Application.isPlaying)throw new InvalidOperationException("Leave Play mode before authoring levels.");
            if(Object.FindFirstObjectByType<RideLevelLayout>()!=null)throw new InvalidOperationException("This map already has authored levels. Edit its Level Layout hierarchy directly.");
            new RideLevelAuthoring().Build();
        }
        public static void BakeGameplay()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/gameplay.unity");Author();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("LEVEL_BAKE_SUCCESS");EditorApplication.Exit(0);
        }
        private void Build()
        {
            if(!AssetDatabase.IsValidFolder(AssetFolder))AssetDatabase.CreateFolder("Assets/Cyclomania","LevelAssets");
            var environment=GameObject.Find("NEW_Env");
            if(environment==null)throw new InvalidOperationException("Open the gameplay map with NEW_Env active.");
            foreach(var filter in environment.GetComponentsInChildren<MeshFilter>())
                if(filter.sharedMesh!=null && filter.GetComponent<Collider>()==null &&
                    (filter.name.StartsWith("Road_") || filter.name.StartsWith("road_")))
                    filter.gameObject.AddComponent<MeshCollider>().sharedMesh=filter.sharedMesh;
            layout=new GameObject("Level Layout").AddComponent<RideLevelLayout>();
            asphalt=Material("Cycle park asphalt",new Color(.11f,.15f,.19f));
            accent=Material("Course markings",new Color(.03f,.75f,.85f));
            concrete=Material("Ramp concrete",new Color(.55f,.62f,.64f));
            sand=Material("Trail surface",new Color(.65f,.44f,.22f));
            // Translations are selected against the active NEW_Env map's clear ground.
            Vector3[] offsets=Offsets();
            for(int i=0;i<5;i++) {
                root=new GameObject(RideWorld.Destinations[i].Name).transform;root.SetParent(layout.transform,false);
                layout.courseFrames[i]=root;
                if(i==1)BuildPark();
                if(i==2)BuildMountain();
                if(i==4)BuildDesert();
                var destination=RideWorld.Destinations[i];
                Beacon(destination);
                var arrival=new GameObject("Arrival").transform;arrival.SetParent(root,false);
                arrival.SetPositionAndRotation(destination.Position,Quaternion.Euler(0,destination.Heading,0));layout.arrivals[i]=arrival;
                root.position=offsets[i];
            }
            layout.canyonStart=new GameObject("Canyon start").transform;
            layout.canyonStart.SetParent(layout.courseFrames[4],false);layout.canyonStart.localPosition=new Vector3(1000,0,245);
            BuildSchool();
            Physics.SyncTransforms();
            // Move only foliage intersecting authored course surfaces to the adjacent perimeter.
            var surfaces=layout.GetComponentsInChildren<Collider>();
            foreach(var t in environment.GetComponentsInChildren<Transform>())
                if(t.name.StartsWith("tree_")) {
                    var renderer=t.GetComponent<Renderer>();
                    if(renderer==null)continue;
                    foreach(var surface in surfaces) {
                        var clearance=surface.bounds;clearance.Expand(new Vector3(1,4,1));
                        if(!renderer.bounds.Intersects(clearance))continue;
                        var destination=t.position;
                        if(destination.x<0)destination.x-=75;
                        else destination.z=250;
                        var hits=Physics.RaycastAll(destination+Vector3.up*200,Vector3.down,400);
                        float ground=float.NegativeInfinity;
                        foreach(var hit in hits)
                            if(!hit.collider.name.StartsWith("tree_") && hit.point.y>ground)ground=hit.point.y;
                        if(!float.IsNegativeInfinity(ground))destination.y=ground;
                        t.position=destination;break;
                    }
                }
            var rider=Object.FindFirstObjectByType<SBPScripts.BicycleController>();
            if(rider!=null)rider.transform.SetPositionAndRotation(layout.arrivals[0].position,layout.arrivals[0].rotation);
            layout.playableBounds=new Bounds(new Vector3(200,120,-390),new Vector3(1900,280,1600));
            layout.freestyleBounds=new[]{
                new Bounds(new Vector3(0,50,-200),new Vector3(1600,200,1600)),
                new Bounds(new Vector3(620,40,-110),new Vector3(100,100,180)),
                new Bounds(new Vector3(752.5f,40,245),new Vector3(125,100,350)),
                new Bounds(new Vector3(850,40,40),new Vector3(24,100,760)),
                new Bounds(new Vector3(1000,40,100),new Vector3(36,100,260))};
            EditorSceneManager.MarkSceneDirty(layout.gameObject.scene);AssetDatabase.SaveAssets();
        }
        private static Vector3[] Offsets()=>new[]{
            new Vector3(-76.64f,-.1f,172.52f), // central street junction
            new Vector3(170,.05f,150),       // east park beside the imported skate park
            new Vector3(-1295,.05f,-215),    // western hillside park
            new Vector3(-1345,-.1f,-610),    // existing western road, northbound
            new Vector3(-1550,.05f,-350)};   // narrow western dirt trail
        private void BuildSchool()
        {
            root=new GameObject("Cycling school course").transform;root.SetParent(layout.transform,false);layout.school=root;
            Box("Road",new Vector3(0,-.5f,50),new Vector3(12,1,115),asphalt);
            Box("Centre line",new Vector3(0,.015f,50),new Vector3(.12f,.02f,110),accent,false);
            Box("Stop box",new Vector3(2.5f,.02f,38),new Vector3(5,.02f,4),accent,false);
            Box("Finish",new Vector3(2.5f,.02f,95),new Vector3(5,.02f,1),accent,false);
            for(int z=0;z<=100;z+=20) {
                var sign=new GameObject("20 KM/H sign",typeof(TextMesh));sign.transform.SetParent(root,false);
                sign.transform.localPosition=new Vector3(5,2,z);sign.transform.localRotation=Quaternion.Euler(0,180,0);
                var text=sign.GetComponent<TextMesh>();text.text=z==40?"STOP / SIGNAL":"20 KM/H\nKEEP RIGHT";
                text.characterSize=.22f;text.fontSize=40;text.anchor=TextAnchor.MiddleCenter;
                text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");sign.GetComponent<MeshRenderer>().sharedMaterial=text.font.material;
            }
            root.position=new Vector3(-455,.05f,-670);
        }
        private Material Material(string name,Color color)
        { var material=new Material(Shader.Find("Standard")) {name=name,color=color}; AssetDatabase.CreateAsset(material,AssetDatabase.GenerateUniqueAssetPath(AssetFolder+"/"+name+".mat")); return material; }
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
        private void BuildMountain()
        {
            // Shared, mitered vertices make a continuous surface through the switchbacks.
            var points=new[]{new Vector3(730,0,80),new Vector3(730,10,160),new Vector3(780,14,180),
                new Vector3(730,24,250),new Vector3(790,24,250),new Vector3(790,0,410)};
            var vertices=new Vector3[points.Length*2];var triangles=new int[(points.Length-1)*6];
            for(int i=0;i<points.Length;i++) {
                var before=Vector3.ProjectOnPlane(points[i]-points[Mathf.Max(0,i-1)],Vector3.up).normalized;
                var after=Vector3.ProjectOnPlane(points[Mathf.Min(points.Length-1,i+1)]-points[i],Vector3.up).normalized;
                if(i==0)before=after;if(i==points.Length-1)after=before;
                var side=Vector3.Cross(Vector3.up,(before+after).normalized);
                side*=5/Mathf.Max(.25f,Vector3.Dot(side,Vector3.Cross(Vector3.up,after)));
                vertices[i*2]=points[i]-side;vertices[i*2+1]=points[i]+side;
                if(i==points.Length-1)continue;
                int v=i*2,t=i*6;triangles[t]=v;triangles[t+1]=v+2;triangles[t+2]=v+1;
                triangles[t+3]=v+1;triangles[t+4]=v+2;triangles[t+5]=v+3;
            }
            var mesh=new Mesh{name="Continuous climb road"};mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh,AssetDatabase.GenerateUniqueAssetPath(AssetFolder+"/Climb road.asset"));
            var go=new GameObject("Climb and summit bridge",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));
            go.transform.SetParent(root,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial=asphalt;go.GetComponent<MeshCollider>().sharedMesh=mesh;go.isStatic=true;
            for(int i=0;i<points.Length-1;i++)for(int side=0;side<2;side++) {
                var start=vertices[i*2+side];var end=vertices[(i+1)*2+side];var delta=end-start;
                var rail=Box("Climb safety rail",(start+end)*.5f+Vector3.up*.45f,new Vector3(.3f,.7f,delta.magnitude),accent);
                rail.transform.rotation=Quaternion.LookRotation(delta,Vector3.up);
            }
        }
        private void BuildDesert()
        {
            Box("Trail surface",new Vector3(1000,-.3f,100),new Vector3(28,.6f,260),sand);
            for(int i=0;i<3;i++) {
                float z=35+i*60;
                Ramp("Trail climb",new Vector3(1000,0,z),24,20,2+i,0,sand);
                Ramp("Trail descent",new Vector3(1000,0,z+40),24,20,2+i,180,sand);
            }
            Road("Jump approach",new Vector3(1000,0,220),new Vector3(1000,0,250),12);
            Ramp("Canyon takeoff ramp",new Vector3(1000,0,250),12,20,3,0,sand);
            Box("Canyon takeoff deck",new Vector3(1000,1.5f,275),new Vector3(12,3,10),sand);
            Box("Canyon landing deck",new Vector3(1000,1.5f,306),new Vector3(12,3,40),sand);
            Ramp("Canyon exit ramp",new Vector3(1000,0,346),12,20,3,180,sand);
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
            if(!solid) {go.GetComponent<Collider>().enabled=false;Object.DestroyImmediate(go.GetComponent<Collider>());}
            go.isStatic=true;return go;
        }
        private void Ramp(string name,Vector3 start,float width,float length,float height,float yaw,Material surface=null)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));go.transform.SetParent(root,false);
            go.transform.SetPositionAndRotation(start,Quaternion.Euler(0,yaw,0));
            float w=width/2;
            var mesh=new Mesh {name=name};
            mesh.vertices=new[]{new Vector3(-w,0,0),new Vector3(w,0,0),new Vector3(-w,0,length),new Vector3(w,0,length),new Vector3(-w,height,length),new Vector3(w,height,length)};
            AssetDatabase.CreateAsset(mesh,AssetDatabase.GenerateUniqueAssetPath(AssetFolder+"/"+name+".asset"));
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
    }
}
