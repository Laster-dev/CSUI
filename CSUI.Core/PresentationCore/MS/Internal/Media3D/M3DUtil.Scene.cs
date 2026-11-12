// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace MS.Internal.Media3D
{
    internal static partial class M3DUtil
    {
        // Returns a new axis aligned bounding box that contains the old
        // bounding box post the given transformation.
        internal static Rect3D ComputeTransformedAxisAlignedBoundingBox(/* IN */ ref Rect3D originalBox, Transform3D transform)
        {
            if (transform == null || transform == Transform3D.Identity)
            {
                return originalBox;
            }

            Matrix3D matrix = transform.Value;

            return ComputeTransformedAxisAlignedBoundingBox(ref originalBox, ref matrix);
        }

        // Returns a new axis aligned bounding box that contains the old

        internal static Matrix3D GetWorldToViewportTransform3D(Camera camera, Rect viewport)
        {
            Debug.Assert(camera != null, "Caller is responsible for ensuring camera is not null.");
            
            return camera.GetViewMatrix() *
                camera.GetProjectionMatrix(M3DUtil.GetAspectRatio(viewport.Size)) *
                M3DUtil.GetHomogeneousToViewportTransform3D(viewport);
        }

        /// <summary>

        /// <param name="visual">The visual whose world space transform should be found</param>
        /// <returns>The world space transformation</returns>
        internal static Matrix3D GetWorldTransformationMatrix(Visual3D visual)
        {
            Viewport3DVisual ignored;

            return GetWorldTransformationMatrix(visual, out ignored);
        }

        /// <summary>
        /// Gets the object space to world space transformation for the given Visual3D
        /// </summary>
        /// <param name="visual3DStart">The visual whose world space transform should be found</param>
        /// <param name="viewport">The containing Viewport3D for the Visual3D</param>
        /// <returns>The world space transformation</returns>
        internal static Matrix3D GetWorldTransformationMatrix(Visual3D visual3DStart, out Viewport3DVisual viewport)
        {
            DependencyObject dependencyObject = visual3DStart;
            Matrix3D worldTransform = Matrix3D.Identity;
       
            while (dependencyObject != null)
            {
                Visual3D visual3D = dependencyObject as Visual3D;

                // we reached the top
                if (visual3D == null) 
                {
                    break;
                }
                
                Transform3D transform = (Transform3D)visual3D.GetValue(Visual3D.TransformProperty);

                transform?.Append(ref worldTransform);

                dependencyObject = VisualTreeHelper.GetParent(dependencyObject);      
            }

            if (dependencyObject != null)
            {
                viewport = (Viewport3DVisual)dependencyObject;
            }
            else
            {
                viewport = null;
            }

            return worldTransform;
        }

        /// <summary>
        /// Computes the transformation matrix to go from a 3D point in the given Visual3D's coordinate space out in to
        /// the Viewport3DVisual.
        /// </summary>
        internal static bool TryTransformToViewport3DVisual(Visual3D visual3D, out Viewport3DVisual viewport, out Matrix3D matrix)
        {
            matrix = GetWorldTransformationMatrix(visual3D, out viewport);

            if (viewport != null)
            {
                matrix *= GetWorldToViewportTransform3D(viewport.Camera, viewport.Viewport);
                return true;
            }
            else
            {
                return false;
            }
        }

    }
}
