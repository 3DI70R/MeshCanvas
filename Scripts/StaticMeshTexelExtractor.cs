using System;
using UnityEngine;

namespace ThreeDISevenZeroR.MCanvas
{
    public class StaticMeshTexelExtractor : IMeshWorldTexelExtractor
    {
        private static readonly Lazy<Material> positionBakeMaterial = new Lazy<Material>(() => 
            new Material(Shader.Find(MeshCanvas.positionBakeShader)), false);
    
        private MeshRenderer renderer;
        private MeshFilter filter;

        public StaticMeshTexelExtractor(MeshRenderer renderer, MeshFilter filter)
        {
            this.renderer = renderer;
            this.filter = filter;
        }
    
        public void WriteWorldTexelTexture(RenderTexture texture)
        {
            if (renderer && filter)
            {
                positionBakeMaterial.Value.SetPass(0);
                Graphics.DrawMeshNow(filter.sharedMesh, renderer.localToWorldMatrix);
            }
        }

        public void Dispose()
        {
            renderer = null;
            filter = null;
        }
    }
}