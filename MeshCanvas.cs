using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public interface IWorldTexelTextureProvider : IDisposable
{
    void WriteWorldTexelTexture(RenderTexture texture);
}

public class MeshCanvas : IDisposable
{
    public static readonly RenderTextureFormat worldBufferFormat = RenderTextureFormat.ARGBHalf;

    public static readonly string positionBakeShader = "Hidden/MeshCanvas/PositionBake";
    public static readonly string positionExpandShader = "Hidden/MeshCanvas/PositionExpand";
    public static readonly string decalPaintShader = "Hidden/MeshCanvas/DecalPaint";
    
    private static readonly Lazy<Material> expandBorderFilter = new Lazy<Material>(() => 
        new Material(Shader.Find(positionExpandShader)), false);
    
    private List<IWorldTexelTextureProvider> texelProvider;
    private readonly int worldBufferWidth;
    private readonly int worldBufferHeight;
    private readonly int positionBakeLayer;
    private bool positionIsDirty;
    private RenderTexture worldTexelTexture;
    private Lazy<Material> decalPaintMaterial = new Lazy<Material>(() => 
        new Material(Shader.Find(decalPaintShader)));

    public Texture WorldTexture => worldTexelTexture;

    public MeshCanvas(int worldBufferWidth, int worldBufferHeight, int positionBakeLayer)
    {
        this.positionBakeLayer = positionBakeLayer;
        this.worldBufferWidth = worldBufferWidth;
        this.worldBufferHeight = worldBufferHeight;
        
        texelProvider = new List<IWorldTexelTextureProvider>();
        MarkPositionAsDirty();
    }
    
    ~MeshCanvas()
    {
        OnDispose();
    }

    public void MarkPositionAsDirty()
    {
        positionIsDirty = true;
    }

    public void UpdatePositionTexture()
    {
        MarkPositionAsDirty();
        UpdateWorldTexture();
    }

    public void AddRenderer(params SkinnedMeshRenderer[] renderer)
    {
        texelProvider.Add(new SkinnedMeshTexelTextureProvider(renderer, positionBakeLayer));
        MarkPositionAsDirty();
    }

    public void AddRenderer(MeshRenderer renderer)
    {
        texelProvider.Add(new StaticMeshTexelTextureProvider(renderer, renderer.GetComponent<MeshFilter>()));
        MarkPositionAsDirty();
    }

    public void DrawDecal(RenderTexture texture, Transform position, Texture sprite)
    {
        UpdateWorldTexture();
        decalPaintMaterial.Value.SetMatrix("_DecalMatrix", position.worldToLocalMatrix);
        decalPaintMaterial.Value.mainTexture = sprite;
        Graphics.Blit(null, texture, decalPaintMaterial.Value);
    }
    
    private void UpdateWorldTexture()
    {
        if (positionIsDirty)
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
                OnNewWorldTextureCreated(worldBuffer);
            }

            Graphics.Blit(worldBuffer, worldTexelTexture, expandBorderFilter.Value);
            RenderTexture.ReleaseTemporary(worldBuffer);
        }
    }

    private void ReleaseWorldTexture()
    {
        if (worldTexelTexture != null)
        {
            worldTexelTexture.Release();
            worldTexelTexture = null;
        }
    }

    private void OnNewWorldTextureCreated(RenderTexture texture)
    {
        decalPaintMaterial.Value.SetTexture("_PositionTex", texture);
    }

    private void OnDispose()
    {
        ReleaseWorldTexture();
    }

    public void Dispose()
    {
        OnDispose();
        GC.SuppressFinalize(this);
    }
}

public class StaticMeshTexelTextureProvider : IWorldTexelTextureProvider
{
    private static readonly Lazy<Material> positionBakeShader = new Lazy<Material>(() => 
        new Material(Shader.Find(MeshCanvas.positionBakeShader)), false);
    
    private readonly MeshRenderer renderer;
    private readonly MeshFilter filter;

    public StaticMeshTexelTextureProvider(MeshRenderer renderer, MeshFilter filter)
    {
        this.renderer = renderer;
        this.filter = filter;
    }
    
    public void WriteWorldTexelTexture(RenderTexture texture)
    {
        if (renderer && filter)
        {
            positionBakeShader.Value.SetPass(0);
            Graphics.DrawMeshNow(filter.sharedMesh, renderer.localToWorldMatrix);
        }
    }

    public void Dispose()
    {
        // noop
    }
}

public class SkinnedMeshTexelTextureProvider : IWorldTexelTextureProvider
{
    private static readonly Lazy<Shader> positionBakeShader = new Lazy<Shader>(() => 
        Shader.Find(MeshCanvas.positionBakeShader), false);
    
    private readonly SkinnedMeshRenderer[] renderers;
    private readonly int[] originalLayers;
    private readonly int positionBakeLayer;
    private readonly GameObject cameraObject;
    private readonly Camera renderCamera;

    public SkinnedMeshTexelTextureProvider(SkinnedMeshRenderer[] renderers, int positionBakeLayer)
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

        cameraObject.transform.position = bounds.center - Vector3.forward * 10f;
        renderCamera.targetTexture = texture;
        renderCamera.RenderWithShader(positionBakeShader.Value, "");
        
        for (var i = renderers.Length - 1; i >= 0; i--)
        {
            var r = renderers[i];

            if (r)
            {
                r.gameObject.layer = originalLayers[i];
            }
        }
    }
    
    public void Dispose()
    {
        Object.Destroy(cameraObject);
    }
}