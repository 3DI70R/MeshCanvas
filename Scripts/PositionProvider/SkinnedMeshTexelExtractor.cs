using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ThreeDISevenZeroR.MCanvas
{
    public class SkinnedMeshTexelExtractor : IMeshWorldTexelExtractor
    {
        private static readonly Lazy<Shader> positionBakeShader = new Lazy<Shader>(() => 
            Shader.Find(MeshCanvas.positionBakeShader), false);
        
        private SkinnedMeshRenderer[] renderers;
        private GameObject cameraObject;
        private Camera renderCamera;
        private readonly int[] originalLayers;
        private readonly int positionBakeLayer;
    
        public SkinnedMeshTexelExtractor(SkinnedMeshRenderer[] renderers, int positionBakeLayer)
        {
            this.renderers = renderers;
            this.positionBakeLayer = positionBakeLayer;
            originalLayers = new int[renderers.Length];
    
            cameraObject = new GameObject("Skinned mesh camera");
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            cameraObject.SetActive(false);
            
            renderCamera = cameraObject.AddComponent<Camera>();
            renderCamera.enabled = false;
            renderCamera.depthTextureMode = DepthTextureMode.None;
            renderCamera.clearFlags = CameraClearFlags.Nothing;
            renderCamera.backgroundColor = Color.clear;
            renderCamera.useOcclusionCulling = false;
            renderCamera.layerCullSpherical = true;
            renderCamera.cullingMask = 1 << positionBakeLayer;
        }
        
        public void WriteWorldTexelTexture(RenderTexture texture)
        {
            if (renderers != null)
            {
                var bounds = renderers[0].bounds;
            
                for (var i = renderers.Length - 1; i >= 0; i--)
                {
                    var r = renderers[i];
                
                    bounds.Encapsulate(r.bounds);
    
                    if (r)
                    {
                        originalLayers[i] = r.gameObject.layer;
                        r.gameObject.layer = positionBakeLayer;
                    }
                }
    
                // TODO: better algorithm for camera placement, disable culling if possible
                cameraObject.transform.position = bounds.center - Vector3.forward * 100f;
                renderCamera.targetTexture = texture;
                renderCamera.RenderWithShader(positionBakeShader.Value, "");
                renderCamera.targetTexture = null;
            
                for (var i = renderers.Length - 1; i >= 0; i--)
                {
                    var r = renderers[i];
    
                    if (r)
                    {
                        r.gameObject.layer = originalLayers[i];
                    }
                }
            }
        }
        
        public void Dispose()
        {
            Object.Destroy(cameraObject);
            
            renderers = null;
            cameraObject = null;
            renderCamera = null;
        }
    }
}