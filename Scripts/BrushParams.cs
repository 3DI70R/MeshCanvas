using System;
using UnityEngine;

namespace ThreeDISevenZeroR.MCanvas
{
    /// <summary>
    /// Brush settings
    /// </summary>
    [Serializable]
    public struct BrushParams
    {
        /// <summary>
        /// Brush texture that will be painted
        /// </summary>
        public Texture texture;

        /// <summary>
        /// Brush color, texture color will be multiplied to this color
        /// </summary>
        public Color color;
    
        /// <summary>
        /// Smoothing start for each axis, position inside this boundaries always painted with full intensity
        /// </summary>
        public Vector3 smoothingStart;
    
        /// <summary>
        /// Smoothing end for each axis, position outside this boundaries are not painted
        /// </summary>
        public Vector3 smoothingEnd;

        /// <summary>
        /// Base rotation for decal
        /// </summary>
        public Quaternion rotation;
    
        /// <summary>
        /// Base size for decal
        /// </summary>
        public Vector3 size;

        /// <summary>
        /// Get canvas brush with default settings
        /// - White color
        /// - No XY Smoothing
        /// - Z smoothing from 0.5 to 1
        /// - No rotation
        /// - No scalong
        /// </summary>
        public static BrushParams GetDefault()
        {
            return new BrushParams
            {
                color = Color.white,
                smoothingStart = new Vector3(1, 1, 0.5f),
                smoothingEnd = Vector3.one,
                rotation = Quaternion.identity,
                size = Vector3.one
            };
        }
    }
}
