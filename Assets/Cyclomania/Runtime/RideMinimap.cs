using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Cyclomania
{
    public sealed class RideMinimap : MonoBehaviour
    {
        private Transform rider;
        private Camera mapCamera;
        private RenderTexture texture;
        private RectTransform marker;
        private float nextRender;

        public void Initialize(Transform target, RawImage image, RectTransform riderMarker)
        {
            rider = target;
            marker = riderMarker;
            texture = new RenderTexture(256,256,16,RenderTextureFormat.ARGB32) { name="Cyclomania minimap" };
            texture.Create();
            image.texture=texture;
            var go=new GameObject("Minimap camera");
            go.transform.SetParent(transform,false);
            mapCamera=go.AddComponent<Camera>();
            mapCamera.enabled=false;
            mapCamera.orthographic=true;
            mapCamera.orthographicSize=65;
            mapCamera.nearClipPlane=.5f;
            mapCamera.farClipPlane=250;
            mapCamera.clearFlags=CameraClearFlags.SolidColor;
            mapCamera.backgroundColor=new Color(.05f,.12f,.17f);
            mapCamera.cullingMask=~(1<<5); // Exclude UI.
            mapCamera.allowHDR=false;
            mapCamera.allowMSAA=false;
            mapCamera.useOcclusionCulling=false;
            mapCamera.targetTexture=texture;
        }
        public void SetTarget(Transform target) { rider=target; nextRender=0; }
        private void LateUpdate()
        {
            if (rider==null || mapCamera==null || Time.timeScale==0) return;
            marker.localRotation=Quaternion.Euler(0,0,-rider.eulerAngles.y);
            if(Time.unscaledTime<nextRender) return;
            nextRender=Time.unscaledTime+.1f; // 10 Hz to limit the extra camera cost on mobile.
            mapCamera.transform.SetPositionAndRotation(rider.position+Vector3.up*150,Quaternion.Euler(90,0,0));
            if(SystemInfo.graphicsDeviceType!=GraphicsDeviceType.Null) mapCamera.Render();
        }
        private void OnDestroy()
        {
            if(mapCamera!=null) { mapCamera.targetTexture=null; Destroy(mapCamera.gameObject); }
            if(texture!=null) { texture.Release(); Destroy(texture); }
        }
    }
}
