using System;
using System.Collections.Generic;
using UnityEngine;
namespace Cyclomania
{
    [Serializable] public sealed class MissionRoadNode {public Vector3 point;public int[] links;}
    [Serializable] public sealed class MissionLocationData
    {
        public MissionRoadNode[] nodes;
        public int[] starts;
        public static int Slot(int mission,int level,int count)
        {
            if(count<=0)return -1;
            int stride=Math.Max(1,(int)(count*.6180339f));while(Gcd(stride,count)!=1)stride++;
            return (int)(((long)(mission*100+Mathf.Clamp(level,1,100)-1)*stride)%count);
        }
        static int Gcd(int a,int b){while(b!=0){int next=a%b;a=b;b=next;}return a;}
        public Vector3[] Route(int mission,int level)
        {
            if(nodes==null || starts==null || starts.Length<1300)return null;
            int start=starts[Slot(mission,level,starts.Length)],end=start;
            var parent=new int[nodes.Length];var distance=new float[nodes.Length];Array.Fill(parent,-2);parent[start]=-1;
            var queue=new Queue<int>();queue.Enqueue(start);
            while(queue.Count>0){int n=queue.Dequeue();if(distance[n]>distance[end])end=n;if(distance[n]>=700){end=n;break;}
                var links=nodes[n].links;for(int i=0;i<links.Length;i++){int next=links[(i+mission+level)%links.Length];if(parent[next]!=-2)continue;parent[next]=n;distance[next]=distance[n]+Vector3.Distance(nodes[n].point,nodes[next].point);queue.Enqueue(next);}}
            if(distance[end]<650)return null;
            var path=new List<Vector3>();for(int n=end;n>=0;n=parent[n])path.Add(nodes[n].point);path.Reverse();return path.ToArray();
        }
    }
    public static class MissionRoute
    {
        public static Vector3 Point(Vector3[] path,float distance)
        {
            if(path==null || path.Length==0)return Vector3.zero;
            for(int i=1;i<path.Length;i++){float length=Vector3.Distance(path[i-1],path[i]);if(distance<=length)return Vector3.LerpUnclamped(path[i-1],path[i],length>0?distance/length:0);distance-=length;}
            return path[path.Length-1];
        }
        public static float Nearest(Vector3[] path,Vector3 position,out float separation)
        {
            float total=0,result=0,best=float.PositiveInfinity;
            for(int i=1;i<path.Length;i++){Vector3 delta=path[i]-path[i-1];float length=delta.magnitude;float t=length>0?Mathf.Clamp01(Vector3.Dot(position-path[i-1],delta)/(length*length)):0;
                float sq=(position-(path[i-1]+delta*t)).sqrMagnitude;if(sq<best){best=sq;result=total+length*t;}total+=length;}
            separation=Mathf.Sqrt(best);return result;
        }
        public static Quaternion Heading(Vector3[] path,float distance)
        {Vector3 delta=Point(path,distance+1)-Point(path,Mathf.Max(0,distance-1));return delta.sqrMagnitude>.001f?Quaternion.LookRotation(Vector3.ProjectOnPlane(delta,Vector3.up)):Quaternion.identity;}
    }
    public sealed class MissionLocations:MonoBehaviour
    {
        public Vector3 Spawn {get;private set;}
        public float Heading {get;private set;}
        public string LocationName {get;private set;}="";
        private MissionLocationData data;
        public void Initialize(){var asset=Resources.Load<TextAsset>("MissionLocations");if(asset!=null)data=JsonUtility.FromJson<MissionLocationData>(asset.text);}
        public bool Place(MissionDefinition definition,int index,RideLevelLayout layout)
        {
            int level=definition.Level;
            if(index==MissionCatalog.MountainAscentIndex || index==MissionCatalog.CanyonJumpIndex){
                definition=layout.Place(definition);float t=(level-1)/99f;float lane=Mathf.Sin(level*2.399963f)*1.2f;
                if(index==MissionCatalog.MountainAscentIndex){
                    Spawn=layout.arrivals[2].position+new Vector3(lane,t*1.875f,t*15);Heading=0;
                    Vector3 previous=layout.arrivals[2].position;
                    foreach(var o in definition.Objectives){Vector3 original=o.point;o.point=Vector3.Lerp(previous,original,.85f+.15f*t)+Vector3.right*lane;previous=original;}
                    LocationName="Mountain · Climb route "+level;
                }else{
                    Spawn=layout.canyonStart.position+new Vector3(lane,0,-20+20*t);Heading=layout.canyonStart.eulerAngles.y;
                    var o=definition.Objectives[0];o.point+=new Vector3(lane,0,-1.5f+1.5f*t);o.landingPoint+=new Vector3(-lane,0,3*t);LocationName="Canyon · Jump line "+level;
                }
            }else{
                Vector3[] route=data?.Route(index,level);if(route==null)return false;
                Spawn=route[0];Heading=MissionRoute.Heading(route,0).eulerAngles.y;
                foreach(var o in definition.Objectives){
                    float along=o.point.z+240;
                    if(o.kind==ObjectiveKind.ReachPoint || o.kind==ObjectiveKind.PickupPackage || o.kind==ObjectiveKind.DeliverPackage)o.point=MissionRoute.Point(route,along);
                    else o.point=Spawn;
                    o.route=route;
                }
                var b=layout.playableBounds;float x=Mathf.InverseLerp(b.min.x,b.max.x,Spawn.x),z=Mathf.InverseLerp(b.min.z,b.max.z,Spawn.z);
                LocationName=(z<.33f?"South":z>.66f?"North":"Central")+(x<.33f?"west":x>.66f?"east":" district")+" · Route "+(MissionLocationData.Slot(index,level,data.starts.Length)+1);
            }
            definition.hasSpawn=true;definition.spawn=Spawn;return true;
        }
    }
}
