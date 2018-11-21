using System;
using UnityEngine;

namespace ThreeDISevenZeroR.MCanvas
{
    /// <summary>
    /// Class which handles world texel texture creation
    /// </summary>
    public interface IMeshWorldTexelExtractor : IDisposable
    {
        /// <summary>
        /// Extract world positions for mesh texels and store it into specified render texture
        /// </summary>
        /// <param name="texture">Render texture which should receive world positions of this provider</param>
        void WriteWorldTexelTexture(RenderTexture texture);
    }
}