using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class MissionLocationBaker
{
    [Serializable] public class Node {public Vector3 point;public int[] links;}
    [Serializable] public class Data {public Node[] nodes;public int[] starts;}
    [MenuItem("Cyclomania/Bake mission street locations")]
    public static void Run()
    {
        try{
            if(Application.isPlaying)throw new InvalidOperationException("Leave Play mode before baking locations.");
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            EditorSceneManager.OpenScene("Assets/Scenes/gameplay.unity");Physics.SyncTransforms();var layout=UnityEngine.Object.FindFirstObjectByType<Cyclomania.RideLevelLayout>();var b=layout.playableBounds;
            var points=new Dictionary<Vector2Int,Vector3>();
            for(int x=(int)b.min.x+10;x<b.max.x-10;x+=5)for(int z=(int)b.min.z+10;z<b.max.z-10;z+=5){
                if(!Physics.Raycast(new Vector3(x,50,z),Vector3.down,out var h,70,~0,QueryTriggerInteraction.Ignore))continue;
                if(h.normal.y<.97f || h.point.y>3 || !h.collider.name.ToLowerInvariant().Contains("road") || h.collider.GetComponentInParent<Cyclomania.RideLevelLayout>()!=null)continue;
                Vector3 p=h.point;if(Physics.CheckCapsule(p+Vector3.up*.8f,p+Vector3.up*1.8f,.55f,~0,QueryTriggerInteraction.Ignore))continue;
                points[new Vector2Int(x,z)]=p;
            }
            var graph=new Dictionary<Vector2Int,List<Vector2Int>>();var dirs=new[]{new Vector2Int(5,0),new Vector2Int(-5,0),new Vector2Int(0,5),new Vector2Int(0,-5)};
            foreach(var pair in points){var links=new List<Vector2Int>();foreach(var d in dirs){var key=pair.Key+d;if(!points.TryGetValue(key,out var next) || Mathf.Abs(next.y-pair.Value.y)>.25f)continue;
                    var delta=next-pair.Value;if(!Physics.SphereCast(pair.Value+Vector3.up,.5f,delta.normalized,out _,delta.magnitude,~0,QueryTriggerInteraction.Ignore)
                        && Physics.Raycast((pair.Value+next)*.5f+Vector3.up*2,Vector3.down,out var middle,3,~0,QueryTriggerInteraction.Ignore)
                        && middle.normal.y>.97f && Mathf.Abs(middle.point.y-(pair.Value.y+next.y)*.5f)<.25f)links.Add(key);}
                graph[pair.Key]=links;}
            var largest=new List<Vector2Int>(graph.Keys);
            largest.Sort((a,c)=>a.x!=c.x?a.x.CompareTo(c.x):a.y.CompareTo(c.y));var index=new Dictionary<Vector2Int,int>();for(int i=0;i<largest.Count;i++)index[largest[i]]=i;
            var nodes=new Node[largest.Count];for(int i=0;i<nodes.Length;i++){var links=new List<int>();foreach(var n in graph[largest[i]])if(index.TryGetValue(n,out int j))links.Add(j);nodes[i]=new Node{point=points[largest[i]],links=links.ToArray()};}
            var starts=new List<int>();var visited=new HashSet<int>();
            int[] Distances(int from){var ds=new int[nodes.Length];Array.Fill(ds,-1);ds[from]=0;var q=new Queue<int>();q.Enqueue(from);while(q.Count>0){int n=q.Dequeue();foreach(int next in nodes[n].links)if(ds[next]<0){ds[next]=ds[n]+1;q.Enqueue(next);}}return ds;}
            for(int i=0;i<nodes.Length;i++){if(visited.Contains(i))continue;var d=Distances(i);int far=i;for(int n=0;n<d.Length;n++)if(d[n]>=0){visited.Add(n);if(d[n]>d[far])far=n;}
                var a=Distances(far);int other=far;for(int n=0;n<a.Length;n++)if(a[n]>a[other])other=n;var c=Distances(other);
                for(int n=0;n<nodes.Length;n++)if(Math.Max(a[n],c[n])>=140)starts.Add(n);
            }
            starts.Sort();
            System.IO.Directory.CreateDirectory("Assets/Cyclomania/Resources");
            System.IO.File.WriteAllText("Assets/Cyclomania/Resources/MissionLocations.json",JsonUtility.ToJson(new Data{nodes=nodes,starts=starts.ToArray()}));Debug.Log("SURVEY_GRAPH total="+points.Count+" starts="+starts.Count);AssetDatabase.Refresh();if(starts.Count<1300)throw new InvalidOperationException("Not enough connected road points for unique mission levels.");
        }catch(Exception e){Debug.LogException(e);throw;}
    }
}
