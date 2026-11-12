// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Runtime.InteropServices;
using System.Windows.Media.Media3D;
using CSUI.Core.Interop.DirectX;
using MS.Internal;
using SR = MS.Internal.PresentationCore.SR;

namespace System.Windows.Media
{
    internal static class MILUtilities
    {
        /// <summary>
        /// Converts a System.Windows.Media.Matrix to a Direct2D D2D1_MATRIX_3X2_F.
        /// </summary>
        internal static D2D1_MATRIX_3X2_F ConvertToD2DMatrix(in Matrix matrix)
        {
            return new D2D1_MATRIX_3X2_F
            {
                M11 = (float)matrix.M11,
                M12 = (float)matrix.M12,
                M21 = (float)matrix.M21,
                M22 = (float)matrix.M22,
                M31 = (float)matrix.OffsetX,
                M32 = (float)matrix.OffsetY
            };
        }

        /// <summary>
        /// Converts a Direct2D D2D1_MATRIX_3X2_F to a System.Windows.Media.Matrix.
        /// </summary>
        internal static Matrix ConvertFromD2DMatrix(in D2D1_MATRIX_3X2_F d2dMatrix)
        {
            return new Matrix(
                d2dMatrix.M11,
                d2dMatrix.M12,
                d2dMatrix.M21,
                d2dMatrix.M22,
                d2dMatrix.M31,
                d2dMatrix.M32);
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MilRectF
        {
            public float Left;
            public float Top;
            public float Right;
            public float Bottom;
        }

        /// <summary>
        /// Managed replacement for native MilUtility_CopyPixelBuffer (zero DllImport).
        /// </summary>
        internal static unsafe void MILCopyPixelBuffer(
            byte* pOutputBuffer,
            uint outputBufferSize,
            uint outputBufferStride,
            uint outputBufferOffsetInBits,
            byte* pInputBuffer,
            uint inputBufferSize,
            uint inputBufferStride,
            uint inputBufferOffsetInBits,
            uint height,
            uint copyWidthInBits)
        {
            ArgumentNullException.ThrowIfNull(pOutputBuffer);
            ArgumentNullException.ThrowIfNull(pInputBuffer);

            if (height == 0 || copyWidthInBits == 0)
            {
                return;
            }

            if ((outputBufferOffsetInBits % 8) == 0 &&
                (inputBufferOffsetInBits % 8) == 0 &&
                (copyWidthInBits % 8) == 0)
            {
                ulong copyBytesPerRow = copyWidthInBits >> 3;
                ulong srcByteOffset = inputBufferOffsetInBits >> 3;
                ulong dstByteOffset = outputBufferOffsetInBits >> 3;

                if (copyBytesPerRow > inputBufferStride || copyBytesPerRow > outputBufferStride)
                {
                    throw new ArgumentException(SR.Image_InsufficientBufferSize);
                }

                ulong requiredSrc = (ulong)(height - 1) * inputBufferStride + srcByteOffset + copyBytesPerRow;
                ulong requiredDst = (ulong)(height - 1) * outputBufferStride + dstByteOffset + copyBytesPerRow;
                if (requiredSrc > inputBufferSize || requiredDst > outputBufferSize)
                {
                    throw new ArgumentException(SR.Image_InsufficientBufferSize);
                }

                for (uint y = 0; y < height; y++)
                {
                    byte* pSrc = pInputBuffer + (ulong)y * inputBufferStride + srcByteOffset;
                    byte* pDst = pOutputBuffer + (ulong)y * outputBufferStride + dstByteOffset;
                    Buffer.MemoryCopy(pSrc, pDst, outputBufferSize - ((ulong)y * outputBufferStride + dstByteOffset), copyBytesPerRow);
                }
                return;
            }

            for (uint y = 0; y < height; y++)
            {
                ulong srcRowBitBase = (ulong)y * inputBufferStride * 8 + inputBufferOffsetInBits;
                ulong dstRowBitBase = (ulong)y * outputBufferStride * 8 + outputBufferOffsetInBits;

                if ((srcRowBitBase + copyWidthInBits) > (ulong)inputBufferSize * 8 ||
                    (dstRowBitBase + copyWidthInBits) > (ulong)outputBufferSize * 8)
                {
                    throw new ArgumentException(SR.Image_InsufficientBufferSize);
                }

                for (uint bit = 0; bit < copyWidthInBits; bit++)
                {
                    ulong sBit = srcRowBitBase + bit;
                    ulong dBit = dstRowBitBase + bit;
                    int srcMask = 0x80 >> (int)(sBit & 7);
                    int dstMask = 0x80 >> (int)(dBit & 7);

                    bool isSet = (pInputBuffer[sBit >> 3] & srcMask) != 0;
                    if (isSet)
                    {
                        pOutputBuffer[dBit >> 3] |= (byte)dstMask;
                    }
                    else
                    {
                        pOutputBuffer[dBit >> 3] &= (byte)~dstMask;
                    }
                }
            }
        }

        /// <summary>
        /// Projects a 3D axis-aligned bounding box through a homogeneous projection/viewport matrix into a 2D bounding Rect.
        /// Pure managed replacement for native MILUtilities.ProjectBounds.
        /// </summary>
        internal static Rect ProjectBounds(ref Matrix3D viewProjMatrix, ref Rect3D bounds)
        {
            if (bounds.IsEmpty)
            {
                return Rect.Empty;
            }

            double x0 = bounds.X;
            double x1 = bounds.X + bounds.SizeX;
            double y0 = bounds.Y;
            double y1 = bounds.Y + bounds.SizeY;
            double z0 = bounds.Z;
            double z1 = bounds.Z + bounds.SizeZ;

            Span<Point4D> corners = stackalloc Point4D[8];
            corners[0] = new Point4D(x0, y0, z0, 1.0);
            corners[1] = new Point4D(x1, y0, z0, 1.0);
            corners[2] = new Point4D(x0, y1, z0, 1.0);
            corners[3] = new Point4D(x1, y1, z0, 1.0);
            corners[4] = new Point4D(x0, y0, z1, 1.0);
            corners[5] = new Point4D(x1, y0, z1, 1.0);
            corners[6] = new Point4D(x0, y1, z1, 1.0);
            corners[7] = new Point4D(x1, y1, z1, 1.0);

            for (int i = 0; i < 8; i++)
            {
                viewProjMatrix.MultiplyPoint(ref corners[i]);
            }

            const double wClip = 1e-6;
            double minX = double.PositiveInfinity;
            double minY = double.PositiveInfinity;
            double maxX = double.NegativeInfinity;
            double maxY = double.NegativeInfinity;
            bool anyVisible = false;

            for (int i = 0; i < 8; i++)
            {
                Point4D c = corners[i];
                if (c.W > wClip)
                {
                    double px = c.X / c.W;
                    double py = c.Y / c.W;
                    if (!double.IsNaN(px) && !double.IsNaN(py))
                    {
                        if (px < minX) minX = px;
                        if (px > maxX) maxX = px;
                        if (py < minY) minY = py;
                        if (py > maxY) maxY = py;
                        anyVisible = true;
                    }
                }
            }

            // Include edge intersections against W = wClip when corners span across the near plane.
            ReadOnlySpan<int> edgePairs =
            [
                0, 1, 2, 3, 4, 5, 6, 7,
                0, 2, 1, 3, 4, 6, 5, 7,
                0, 4, 1, 5, 2, 6, 3, 7
            ];
            for (int e = 0; e < edgePairs.Length; e += 2)
            {
                Point4D a = corners[edgePairs[e]];
                Point4D b = corners[edgePairs[e + 1]];
                bool aIn = a.W > wClip;
                bool bIn = b.W > wClip;
                if (aIn != bIn)
                {
                    double denom = b.W - a.W;
                    if (Math.Abs(denom) > 1e-12)
                    {
                        double t = (wClip - a.W) / denom;
                        double px = (a.X + (b.X - a.X) * t) / wClip;
                        double py = (a.Y + (b.Y - a.Y) * t) / wClip;
                        if (!double.IsNaN(px) && !double.IsNaN(py))
                        {
                            if (px < minX) minX = px;
                            if (px > maxX) maxX = px;
                            if (py < minY) minY = py;
                            if (py > maxY) maxY = py;
                            anyVisible = true;
                        }
                    }
                }
            }

            if (!anyVisible || double.IsInfinity(minX) || double.IsInfinity(minY) || double.IsInfinity(maxX) || double.IsInfinity(maxY))
            {
                return Rect.Empty;
            }

            return new Rect(minX, minY, Math.Max(0.0, maxX - minX), Math.Max(0.0, maxY - minY));
        }
    }
}
