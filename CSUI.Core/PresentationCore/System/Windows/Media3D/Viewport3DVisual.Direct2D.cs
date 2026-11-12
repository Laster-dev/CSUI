// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using CSUI.Core.Interop.DirectX;

namespace System.Windows.Media.Media3D
{
    public abstract partial class Visual3D
    {
        internal Model3D? InternalVisual3DModel => _visual3DModel;
    }

    internal static class Viewport3DSceneCollector
    {
        public static void CollectScene(
            Viewport3DVisual viewportVisual,
            in Point3D cameraWorldPos,
            List<DxLightData> lightsOut,
            List<DxDrawCommand> drawsOut)
        {
            Visual3DCollection? children = viewportVisual.Children;
            if (children == null || children.InternalCount == 0)
                return;

            Matrix3D rootTransform = Matrix3D.Identity;
            for (int i = 0, count = children.InternalCount; i < count; i++)
            {
                Visual3D? child = children.InternalGetItem(i);
                if (child != null)
                {
                    TraverseVisual3D(child, in rootTransform, in cameraWorldPos, lightsOut, drawsOut);
                }
            }
        }

        private static void TraverseVisual3D(
            Visual3D visual3D,
            in Matrix3D parentWorld,
            in Point3D cameraWorldPos,
            List<DxLightData> lightsOut,
            List<DxDrawCommand> drawsOut)
        {
            if (!visual3D.InternalIsVisible)
                return;

            if (visual3D is UIElement3D uie3D && uie3D.Visibility != Visibility.Visible)
                return;

            Matrix3D localWorld = parentWorld;
            Transform3D? t = visual3D.Transform;
            if (t != null)
            {
                Matrix3D tm = t.Value;
                if (!tm.IsIdentity)
                {
                    localWorld = tm * parentWorld;
                }
            }

            Model3D? model = visual3D.InternalVisual3DModel;
            if (model != null)
            {
                TraverseModel3D(model, in localWorld, in cameraWorldPos, lightsOut, drawsOut);
            }

            IVisual3DContainer container = visual3D;
            int childCount = container.GetChildrenCount();
            for (int i = 0; i < childCount; i++)
            {
                Visual3D? child = container.GetChild(i);
                if (child != null)
                {
                    TraverseVisual3D(child, in localWorld, in cameraWorldPos, lightsOut, drawsOut);
                }
            }
        }

        private static void TraverseModel3D(
            Model3D model,
            in Matrix3D parentWorld,
            in Point3D cameraWorldPos,
            List<DxLightData> lightsOut,
            List<DxDrawCommand> drawsOut)
        {
            Matrix3D world = parentWorld;
            Transform3D? t = model.Transform;
            if (t != null)
            {
                Matrix3D tm = t.Value;
                if (!tm.IsIdentity)
                {
                    world = tm * parentWorld;
                }
            }

            if (model is Model3DGroup group)
            {
                Model3DCollection? children = group.Children;
                if (children == null)
                    return;

                for (int i = 0, count = children.Count; i < count; i++)
                {
                    Model3D? child = children[i];
                    if (child != null)
                    {
                        TraverseModel3D(child, in world, in cameraWorldPos, lightsOut, drawsOut);
                    }
                }
                return;
            }

            if (model is Light light)
            {
                CollectLight(light, in world, lightsOut);
                return;
            }

            if (model is GeometryModel3D geomModel && geomModel.Geometry is MeshGeometry3D mesh)
            {
                bool flipWinding = DxViewport3DRenderer.ComputeDeterminant3x3(in world) < 0.0;
                double sortDepthSq = ComputeBoundsCenterDistSq(mesh, in world, in cameraWorldPos);

                if (geomModel.Material != null)
                {
                    EmitMaterialDraws(
                        mesh,
                        in world,
                        geomModel.Material,
                        flipWinding,
                        cullFrontForBack: false,
                        normalNegate: false,
                        sortDepthSq,
                        drawsOut);
                }

                if (geomModel.BackMaterial != null)
                {
                    EmitMaterialDraws(
                        mesh,
                        in world,
                        geomModel.BackMaterial,
                        flipWinding,
                        cullFrontForBack: true,
                        normalNegate: true,
                        sortDepthSq,
                        drawsOut);
                }
            }
        }

        private static void EmitMaterialDraws(
            MeshGeometry3D mesh,
            in Matrix3D world,
            Material material,
            bool flipWinding,
            bool cullFrontForBack,
            bool normalNegate,
            double sortDepthSq,
            List<DxDrawCommand> drawsOut)
        {
            Matrix3D worldCopy = world;
            // Fast common path: coalesce MaterialGroup containing at most 1 Diffuse, 1 Specular, 1 Emissive into a single draw.
            DxMaterialPassDesc current = default;
            bool hasPending = false;

            void FlushPending()
            {
                if (!hasPending)
                    return;

                drawsOut.Add(new DxDrawCommand
                {
                    Mesh = mesh,
                    WorldMatrix = worldCopy,
                    Material = current,
                    FlipWinding = flipWinding,
                    CullFrontForBackMaterial = cullFrontForBack,
                    NormalNegate = normalNegate,
                    SortDepthSq = sortDepthSq,
                    Translucent = current.IsPotentiallyTranslucent,
                });
                current = default;
                hasPending = false;
            }

            void VisitMaterial(Material mat)
            {
                if (mat is MaterialGroup group)
                {
                    MaterialCollection? children = group.Children;
                    if (children == null)
                        return;

                    for (int i = 0, count = children.Count; i < count; i++)
                    {
                        Material? child = children[i];
                        if (child != null)
                        {
                            VisitMaterial(child);
                        }
                    }
                    return;
                }

                if (mat is DiffuseMaterial diff)
                {
                    if (current.HasDiffuse)
                        FlushPending();

                    current.HasDiffuse = true;
                    current.DiffuseColor = diff.Color;
                    current.AmbientColor = diff.AmbientColor;
                    current.DiffuseBrush = diff.Brush;
                    hasPending = true;
                }
                else if (mat is SpecularMaterial spec)
                {
                    if (current.HasSpecular)
                        FlushPending();

                    current.HasSpecular = true;
                    current.SpecularColor = spec.Color;
                    current.SpecularPower = spec.SpecularPower;
                    current.SpecularBrush = spec.Brush;
                    hasPending = true;
                }
                else if (mat is EmissiveMaterial emis)
                {
                    if (current.HasEmissive)
                        FlushPending();

                    current.HasEmissive = true;
                    current.EmissiveColor = emis.Color;
                    current.EmissiveBrush = emis.Brush;
                    hasPending = true;
                }
            }

            VisitMaterial(material);
            FlushPending();
        }

        private static void CollectLight(Light light, in Matrix3D world, List<DxLightData> lightsOut)
        {
            if (lightsOut.Count >= 32)
                return;

            Color c = light.Color;
            float r = c.R / 255f;
            float g = c.G / 255f;
            float b = c.B / 255f;
            if (c.A == 0 || (r == 0f && g == 0f && b == 0f))
                return;

            if (light is AmbientLight)
            {
                lightsOut.Add(new DxLightData
                {
                    ColorR = r,
                    ColorG = g,
                    ColorB = b,
                    Kind = 1f,
                });
                return;
            }

            if (light is DirectionalLight dirLight)
            {
                Vector3D dir = world.Transform(dirLight.Direction);
                if (dir.LengthSquared <= 1e-24)
                    return;
                dir.Normalize();

                lightsOut.Add(new DxLightData
                {
                    ColorR = r,
                    ColorG = g,
                    ColorB = b,
                    Kind = 2f,
                    DirX = (float)dir.X,
                    DirY = (float)dir.Y,
                    DirZ = (float)dir.Z,
                });
                return;
            }

            if (light is PointLightBase plb)
            {
                Point3D pos = world.Transform(plb.Position);
                double rawRange = plb.Range;
                float range = double.IsPositiveInfinity(rawRange) || rawRange > 1e30 ? 1e30f : (float)Math.Max(0.0, rawRange);

                if (light is SpotLight spot)
                {
                    Vector3D dir = world.Transform(spot.Direction);
                    if (dir.LengthSquared <= 1e-24)
                        return;
                    dir.Normalize();

                    double outerHalfRad = Math.Clamp(spot.OuterConeAngle, 0.0, 360.0) * (0.5 * Math.PI / 180.0);
                    double innerHalfRad = Math.Clamp(spot.InnerConeAngle, 0.0, 360.0) * (0.5 * Math.PI / 180.0);
                    if (innerHalfRad > outerHalfRad)
                        innerHalfRad = outerHalfRad;

                    lightsOut.Add(new DxLightData
                    {
                        ColorR = r,
                        ColorG = g,
                        ColorB = b,
                        Kind = 4f,
                        PosX = (float)pos.X,
                        PosY = (float)pos.Y,
                        PosZ = (float)pos.Z,
                        Range = range,
                        DirX = (float)dir.X,
                        DirY = (float)dir.Y,
                        DirZ = (float)dir.Z,
                        CosHalfInner = (float)Math.Cos(innerHalfRad),
                        ConstAtten = (float)Math.Max(0.0, plb.ConstantAttenuation),
                        LinearAtten = (float)Math.Max(0.0, plb.LinearAttenuation),
                        QuadAtten = (float)Math.Max(0.0, plb.QuadraticAttenuation),
                        CosHalfOuter = (float)Math.Cos(outerHalfRad),
                    });
                }
                else
                {
                    lightsOut.Add(new DxLightData
                    {
                        ColorR = r,
                        ColorG = g,
                        ColorB = b,
                        Kind = 3f,
                        PosX = (float)pos.X,
                        PosY = (float)pos.Y,
                        PosZ = (float)pos.Z,
                        Range = range,
                        ConstAtten = (float)Math.Max(0.0, plb.ConstantAttenuation),
                        LinearAtten = (float)Math.Max(0.0, plb.LinearAttenuation),
                        QuadAtten = (float)Math.Max(0.0, plb.QuadraticAttenuation),
                    });
                }
            }
        }

        private static double ComputeBoundsCenterDistSq(MeshGeometry3D mesh, in Matrix3D world, in Point3D cameraPos)
        {
            Rect3D b = mesh.Bounds;
            if (b.IsEmpty)
                return 0.0;

            Point3D center = world.Transform(new Point3D(b.X + b.SizeX * 0.5, b.Y + b.SizeY * 0.5, b.Z + b.SizeZ * 0.5));
            Vector3D d = center - cameraPos;
            return d.LengthSquared;
        }
    }
}
