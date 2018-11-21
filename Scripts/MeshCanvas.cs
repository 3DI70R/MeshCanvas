using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ThreeDISevenZeroR.MCanvas
{
    public class MeshCanvas : IDisposable
    {
        internal static readonly RenderTextureFormat worldBufferFormat = RenderTextureFormat.ARGBHalf;
        internal static readonly string positionBakeShader = "Hidden/MeshCanvas/PositionBake";
        internal static readonly string positionExpandShader = "Hidden/MeshCanvas/PositionExpand";
        internal static readonly string decalPaintShader = "Hidden/MeshCanvas/DecalPaint";
        
        private static readonly Lazy<Material> expandBorderFilter = new Lazy<Material>(() => 
            new Material(Shader.Find(positionExpandShader)), false);
        
        private readonly List<IMeshWorldTexelExtractor> texelProvider;
        private readonly int worldBufferWidth;
        private readonly int worldBufferHeight;
        private readonly Lazy<int> positionBakeLayer;
        private readonly string positionBakeLayerName;
        private readonly Lazy<Material> decalPaintMaterial = new Lazy<Material>(() => 
            new Material(Shader.Find(decalPaintShader)));
        
        private bool positionIsDirty;
        private bool isDisposed;
        private RenderTexture worldTexelTexture;
    
        /// <summary>
        /// Texture which contain calculated positions for each texel
        /// </summary>
        public Texture WorldTexture => worldTexelTexture;

        /// <summary>
        /// Is Dispose method called
        /// </summary>
        public bool IsDisposed => isDisposed;

        /// <summary>
        /// Creates MeshCanvas with specified world texture width and height.
        /// World texture size affects how accurate will painting operations perform, for more accurate results,
        /// you should use higher resolution.
        /// </summary>
        /// <param name="worldBufferWidth">Width of world texture</param>
        /// <param name="worldBufferHeight">Height of world texture</param>
        /// <param name="positionBakeLayerName">Layer for skinned mesh position baking</param>
        public MeshCanvas(int worldBufferWidth, int worldBufferHeight, 
            string positionBakeLayerName = "MeshCanvas Bake")
        {
            positionBakeLayer = new Lazy<int>(() => LayerMask.NameToLayer(positionBakeLayerName));
            this.positionBakeLayerName = positionBakeLayerName;
            this.worldBufferWidth = worldBufferWidth;
            this.worldBufferHeight = worldBufferHeight;
            
            texelProvider = new List<IMeshWorldTexelExtractor>();
            MarkPositionAsDirty();
        }
        
        ~MeshCanvas()
        {
            OnDispose();
        }
        
        /// <summary>
        /// Disposes MeshCanvas and deallocates texture buffers.
        /// You should use this method when 
        /// </summary>
        public void Dispose()
        {
            if (!isDisposed)
            {
                OnDispose();
                GC.SuppressFinalize(this);
            }
        }
    
        /// <summary>
        /// Marks position texture as dirty
        /// World texture is marked as outdated, and will be updated before next paint operation
        /// </summary>
        public void MarkPositionAsDirty()
        {
            positionIsDirty = true;
        }
    
        /// <summary>
        /// Forcefully updates world texture
        /// Use this method, if you use world texture by yourself and want to have up to date world texture
        /// </summary>
        public void UpdatePosition()
        {
            MarkPositionAsDirty();
            UpdatePositionIfDirty();
        }

        /// <summary>
        /// Adds Mesh Renderer to world texture update queue
        /// When world texture will be updated, it will contain mesh from this MeshRenderer
        /// </summary>
        /// <param name="renderer">Mesh renderer that should be included in update queue.
        /// Mesh renderer should have MeshFilter attached.</param>
        public void AddRenderer(MeshRenderer renderer)
        {
            if (isDisposed)
            {
                Debug.LogError("Cannot add renderer to MeshCanvas, it is disposed");
                return;
            }
            
            var filter = renderer.GetComponent<MeshFilter>();

            if (filter)
            {
                texelProvider.Add(new StaticMeshTexelExtractor(renderer, filter));
                MarkPositionAsDirty();
            }
            else
            {
                Debug.LogWarningFormat("Cannot add mesh renderer to MeshCanvas, " +
                                       "\"{0}\" does not contain mesh filter", renderer.gameObject.name);
            }
        }
    
        /// <summary>
        /// Adds Skinned Mesh Renderer to world texture update queue
        /// When world texture will be updated, those renderers will be included in world texture
        /// </summary>
        /// <param name="renderers">Renderers that needs to be included. Since world texture baking uses different logic
        /// for skinned renderers, it is possible to include multiple mesh renderers for batch baking.</param>
        public void AddRenderer(params SkinnedMeshRenderer[] renderers)
        {
            if (isDisposed)
            {
                Debug.LogError("Cannot add skinned renderer to MeshCanvas, it is disposed.");
                return;
            }
            
            var id = positionBakeLayer.Value;

            if (id >= 0)
            {
                texelProvider.Add(new SkinnedMeshTexelExtractor(renderers, id));
                MarkPositionAsDirty();
            }
            else
            {
                Debug.LogWarningFormat("Cannot add skinned mesh renderer to MeshCanvas, " +
                                       "layer \"{0}\" not defined, it will be used for world texture baking", 
                    positionBakeLayerName);
            }
        }

        /// <summary>
        /// Paints specified brush texture onto target texture using position/rotation/size
        /// </summary>
        /// <param name="target">Target render texture, which will receive painting operation result</param>
        /// <param name="brush">Texture that will be applied to target texture</param>
        /// <param name="position">Decal position in world space</param>
        /// <param name="rotation">Decal rotation in world space</param>
        /// <param name="size">Decal size in world space</param>
        public void Paint(RenderTexture target, Texture brush, Color color,
            Vector3 position, Quaternion rotation, Vector3 size)
        {
            if (isDisposed)
            {
                Debug.LogError("Cannot draw decal, MeshCanvas is disposed");
                return;
            }
            
            UpdatePositionIfDirty();
            
            decalPaintMaterial.Value.SetMatrix("_DecalMatrix", Matrix4x4.TRS(position, rotation, size).inverse);
            //decalPaintMaterial.Value.SetVector("_Skew", fromPosition - toPosition);
            decalPaintMaterial.Value.mainTexture = brush;
            decalPaintMaterial.Value.color = color;

            Graphics.Blit(null, target, decalPaintMaterial.Value);
        }
        
        private void UpdatePositionIfDirty()
        {
            if (positionIsDirty && !isDisposed)
            {
                var worldBuffer = RenderTexture.GetTemporary(worldBufferWidth, worldBufferHeight, 0, worldBufferFormat);
           
                RenderTexture.active = worldBuffer;
                GL.Clear(false, true, Color.clear);
            
                for (var i = 0; i < texelProvider.Count; i++)
                {
                    texelProvider[i].WriteWorldTexelTexture(worldBuffer);
                }
            
                if (worldTexelTexture != null && 
                    (worldTexelTexture.width != worldBufferWidth ||
                     worldTexelTexture.height != worldBufferHeight))
                {
                    ReleaseWorldTexture();
                }
    
                if (worldTexelTexture == null)
                {
                    worldTexelTexture = new RenderTexture(worldBufferWidth, worldBufferHeight, 0, worldBufferFormat);
                    worldTexelTexture.autoGenerateMips = false;
                    worldTexelTexture.antiAliasing = 1;
                    worldTexelTexture.useMipMap = false;
                    worldTexelTexture.filterMode = FilterMode.Bilinear;
                    worldTexelTexture.anisoLevel = 1;
                    OnNewWorldTextureCreated(worldTexelTexture);
                }
    
                Graphics.Blit(worldBuffer, worldTexelTexture, expandBorderFilter.Value);
                RenderTexture.ReleaseTemporary(worldBuffer);

                positionIsDirty = false;
            }
        }
        
        private void OnNewWorldTextureCreated(RenderTexture texture)
        {
            decalPaintMaterial.Value.SetTexture("_PositionTex", texture);
        }
    
        private void ReleaseWorldTexture()
        {
            if (worldTexelTexture != null)
            {
                worldTexelTexture.Release();
                worldTexelTexture = null;
            }
        }

        private void DisposeTexelProviders()
        {
            for (var i = 0; i < texelProvider.Count; i++)
            {
                texelProvider[i].Dispose();
            }
            
            texelProvider.Clear();
        }

        private void OnDispose()
        {
            if (!isDisposed)
            {
                ReleaseWorldTexture();
                DisposeTexelProviders();
                isDisposed = true;
            }
        }
    }
}