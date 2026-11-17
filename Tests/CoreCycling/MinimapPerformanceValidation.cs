using System;
using System.Reflection;
using Cyclomania;
using UnityEngine;
using UnityEngine.UI;
public static class MinimapPerformanceValidation
{
    static FieldInfo Field(string name)=>typeof(RideMinimap).GetField(name,BindingFlags.NonPublic|BindingFlags.Instance);
    static void Tick(RideMinimap map){Field("nextRender").SetValue(map,0f);typeof(RideMinimap).GetMethod("LateUpdate",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(map,null);}
    public static void Run(Transform rider)
    {
        var root=new GameObject("Minimap performance test",typeof(RectTransform));var image=new GameObject("Image",typeof(RectTransform),typeof(RawImage));image.transform.SetParent(root.transform);
        var marker=new GameObject("Marker",typeof(RectTransform));marker.transform.SetParent(root.transform);
        var map=root.AddComponent<RideMinimap>();map.Initialize(rider,image.GetComponent<RawImage>(),(RectTransform)marker.transform);
        Tick(map);float earlier=Time.unscaledTime-.5f;Field("lastRender").SetValue(map,earlier);Tick(map);
        if((float)Field("lastRender").GetValue(map)!=earlier)throw new Exception("Stationary minimap redrew unnecessarily");
        Debug.Log("PASS: stationary minimap skips redundant city renders");
        Vector3 original=rider.position;rider.position+=Vector3.right*2;Tick(map);rider.position=original;
        if((float)Field("lastRender").GetValue(map)!=Time.unscaledTime)throw new Exception("Moving minimap failed to refresh");
        Debug.Log("PASS: moving minimap refreshes after sufficient displacement");
        map.SetTarget(rider);if((bool)Field("rendered").GetValue(map))throw new Exception("Target swap must force map refresh");
        Debug.Log("PASS: bicycle swap requests fresh minimap");UnityEngine.Object.DestroyImmediate(root);
        Debug.Log("MINIMAP_PERFORMANCE_VALIDATION_SUCCESS 3");
    }
}
