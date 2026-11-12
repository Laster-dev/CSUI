// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//
// Description: Implementation of GeometryGroup
//

using System.Windows.Markup;

using CSUI.Core.Interop;
using CSUI.Core.Interop.DirectX;

#nullable disable
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media
{
    #region GeometryGroup
    /// <summary>
    /// GeometryGroup
    /// </summary>
    [System.Windows.Markup.ContentProperty("Children")]
    public sealed partial class GeometryGroup : Geometry
    {
        #region Constructors
        /// <summary>
        /// Default constructor
        /// </summary>
        public GeometryGroup()
        {
        }
        #endregion

        #region Overrides

        internal override PathGeometry GetAsPathGeometry()
        {
            PathGeometry pg = new PathGeometry();
            pg.AddGeometry(this);

            pg.FillRule = FillRule;

            Debug.Assert(pg.CanFreeze);

            return pg;
        }
        
        #endregion

        #region GetPathFigureCollection
        internal override PathFigureCollection GetTransformedFigureCollection(Transform transform)
        {
            // Combine the transform argument with the internal transform
            Transform combined = new MatrixTransform(GetCombinedMatrix(transform));

            PathFigureCollection result = new PathFigureCollection();
            GeometryCollection children = Children;

            if (children != null)
            {
                for (int i = 0; i < children.Count; i++)
                {
                    PathFigureCollection pathFigures = children.Internal_GetItem(i).GetTransformedFigureCollection(combined);
                    if (pathFigures != null)
                    {
                        int count = pathFigures.Count;
                        for (int j = 0; j < count; ++j)
                        {
                            result.Add(pathFigures[j]);
                        }
                    }
                }
            }

            return result;
        }
        #endregion

        internal override unsafe void* CreateD2DGeometry(void* d2dFactory)
        {
            GeometryCollection children = Children;
            if (children == null || children.Count == 0)
            {
                return null;
            }

            int count = children.Count;
            void** childPtrs = stackalloc void*[count];
            int validCount = 0;
            try
            {
                for (int i = 0; i < count; i++)
                {
                    Geometry child = children.Internal_GetItem(i);
                    if (child != null && !child.IsObviouslyEmpty())
                    {
                        void* d2dChild = child.CreateD2DGeometry(d2dFactory);
                        if (d2dChild != null)
                        {
                            childPtrs[validCount++] = d2dChild;
                        }
                    }
                }

                if (validCount == 0)
                {
                    return null;
                }

                void* rawGroup = null;
                int hr = ID2D1FactoryExVtbl.CreateGeometryGroup(
                    d2dFactory,
                    GeometryDirect2DHelpers.ToD2DFillMode(FillRule),
                    childPtrs,
                    (uint)validCount,
                    &rawGroup);
                HResult.ThrowIfFailed(hr, "ID2D1Factory.CreateGeometryGroup");

                return GeometryDirect2DHelpers.WrapWithTransform(d2dFactory, rawGroup, Transform);
            }
            finally
            {
                for (int i = 0; i < validCount; i++)
                {
                    using var childCom = new ComPtr<byte>((byte*)childPtrs[i]);
                }
            }
        }

        #region IsEmpty

        /// <summary>
        /// Returns true if this geometry is empty
        /// </summary>
        public override bool IsEmpty()
        {
            GeometryCollection children = Children;
            if (children == null)
            {
                return true;
            }

            for (int i=0; i<children.Count; i++)
            {
                if (!((Geometry)children[i]).IsEmpty())
                {
                    return false;
                }
            }

            return true;
        }

        internal override bool IsObviouslyEmpty()
        {
            GeometryCollection children = Children;
            return (children == null) || (children.Count == 0);
        }

        #endregion IsEmpty

        /// <summary>
        /// Returns true if this geometry may have curved segments
        /// </summary>
        public override bool MayHaveCurves()
        {
            GeometryCollection children = Children;
            if (children == null)
            {
                return false;
            }

            for (int i = 0; i < children.Count; i++)
            {
                if (((Geometry)children[i]).MayHaveCurves())
                {
                    return true;
                }
            }

            return false;
        }
}
    #endregion
}


