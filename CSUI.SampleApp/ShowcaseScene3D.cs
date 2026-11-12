using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace CSUI.SampleApp
{
    internal sealed class ShowcaseScene3DController
    {
        private readonly ShowcaseViewModel _viewModel;
        private readonly AxisAngleRotation3D _crystalYawRotation = new AxisAngleRotation3D(new Vector3D(0, 1, 0), 28.0);
        private readonly AxisAngleRotation3D _crystalPitchRotation = new AxisAngleRotation3D(new Vector3D(1, 0, 0), 18.0);
        private readonly AxisAngleRotation3D _panelYawRotation = new AxisAngleRotation3D(new Vector3D(0, 1, 0), -16.0);
        private readonly DirectionalLight _keyLight;
        private readonly PointLight _accentPointLight;
        private readonly EmissiveMaterial _crystalEmissive;
        private readonly ShowcaseButton _surface2DButton;
        private readonly PerspectiveCamera _perspectiveCamera;
        private readonly OrthographicCamera _orthoCamera;
        private bool _usingOrthoCamera;
        private double _cameraDistance = 6.2;
        private Point _lastDragPoint;
        private bool _isDraggingOrbit;

        public Viewport3D Viewport { get; }

        public ShowcaseScene3DController(ShowcaseViewModel viewModel, double width, double height)
        {
            _viewModel = viewModel;

            _perspectiveCamera = new PerspectiveCamera(
                new Point3D(0, 0.35, _cameraDistance),
                new Vector3D(0, -0.06, -1),
                new Vector3D(0, 1, 0),
                45.0);

            _orthoCamera = new OrthographicCamera(
                new Point3D(0, 0.35, _cameraDistance),
                new Vector3D(0, -0.06, -1),
                new Vector3D(0, 1, 0),
                5.2);

            Viewport = new Viewport3D
            {
                Width = width,
                Height = height,
                Camera = _perspectiveCamera
            };

            // 1. Lighting Rig (Ambient + Directional + Point + Spot)
            Model3DGroup lightGroup = new Model3DGroup();
            lightGroup.Children.Add(new AmbientLight(Color.FromRgb(42, 48, 68)));
            _keyLight = new DirectionalLight(Color.FromRgb(235, 240, 255), new Vector3D(-0.55, -0.75, -0.9));
            _accentPointLight = new PointLight(Color.FromRgb(56, 189, 248), new Point3D(2.8, 2.2, 3.2))
            {
                Range = 16.0,
                ConstantAttenuation = 1.0,
                LinearAttenuation = 0.08
            };
            SpotLight rimSpot = new SpotLight(
                Color.FromRgb(244, 114, 182),
                new Point3D(-3.0, 2.5, 3.0),
                new Vector3D(0.75, -0.6, -0.8),
                35.0,
                18.0);

            lightGroup.Children.Add(_keyLight);
            lightGroup.Children.Add(_accentPointLight);
            lightGroup.Children.Add(rimSpot);
            Viewport.Children.Add(new ModelVisual3D { Content = lightGroup });

            // 2. Interactive 3D Crystal Cube (ModelUIElement3D with 3D RoutedEvent + Blinn-Phong MaterialGroup)
            Transform3DGroup crystalTransform = new Transform3DGroup();
            crystalTransform.Children.Add(new RotateTransform3D(_crystalPitchRotation));
            crystalTransform.Children.Add(new RotateTransform3D(_crystalYawRotation));
            crystalTransform.Children.Add(new TranslateTransform3D(-0.85, 0.15, 0));

            LinearGradientBrush crystalGradient = new LinearGradientBrush(
                Color.FromRgb(56, 189, 248),
                Color.FromRgb(129, 140, 248),
                45.0);
            MaterialGroup frontMat = new MaterialGroup();
            frontMat.Children.Add(new DiffuseMaterial(crystalGradient));
            frontMat.Children.Add(new SpecularMaterial(Brushes.White, 42.0));
            _crystalEmissive = new EmissiveMaterial(new SolidColorBrush(Color.FromRgb(12, 28, 56)));
            frontMat.Children.Add(_crystalEmissive);

            GeometryModel3D crystalModel = new GeometryModel3D(CreateBeveledCubeMesh(1.18), frontMat)
            {
                BackMaterial = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(244, 63, 94)))
            };

            ModelUIElement3D crystalElement3D = new ModelUIElement3D
            {
                Model = crystalModel,
                Transform = crystalTransform
            };
            crystalElement3D.MouseDown += OnCrystalMouseDown;

            ContainerUIElement3D interactiveContainer = new ContainerUIElement3D();
            interactiveContainer.Children.Add(crystalElement3D);
            Viewport.Children.Add(interactiveContainer);

            // 3. Interactive 2D-on-3D Surface (Viewport2DVisual3D)
            DiffuseMaterial hostMaterial = new DiffuseMaterial(Brushes.White);
            Viewport2DVisual3D.SetIsVisualHostMaterial(hostMaterial, true);

            _surface2DButton = new ShowcaseButton("3D 曲面 2D 按钮 (点我)", 190, 56);
            _surface2DButton.Click += OnSurface2DButtonClick;

            Transform3DGroup panelTransform = new Transform3DGroup();
            panelTransform.Children.Add(new RotateTransform3D(_panelYawRotation));
            panelTransform.Children.Add(new TranslateTransform3D(1.35, 0.05, 0.25));

            Viewport2DVisual3D viewport2DOn3D = new Viewport2DVisual3D
            {
                Geometry = CreateQuadMesh(1.85, 0.72),
                Material = hostMaterial,
                Visual = _surface2DButton,
                Transform = panelTransform
            };
            Viewport.Children.Add(viewport2DOn3D);

            UpdateLightingFromViewModel();
        }

        public void UpdateLightingFromViewModel()
        {
            double factor = Math.Clamp(_viewModel.LightIntensity / 100.0, 0.1, 1.0);
            byte kR = (byte)Math.Clamp((int)(245 * factor), 25, 255);
            byte kG = (byte)Math.Clamp((int)(248 * factor), 25, 255);
            byte kB = (byte)Math.Clamp((int)(255 * factor), 30, 255);
            _keyLight.Color = Color.FromRgb(kR, kG, kB);

            byte pR = (byte)Math.Clamp((int)(56 * factor), 10, 255);
            byte pG = (byte)Math.Clamp((int)(189 * factor), 20, 255);
            byte pB = (byte)Math.Clamp((int)(248 * factor), 30, 255);
            _accentPointLight.Color = Color.FromRgb(pR, pG, pB);
        }

        public void StepAnimation(double dtSeconds)
        {
            if (_viewModel.IsAutoRotating && !_isDraggingOrbit)
            {
                _crystalYawRotation.Angle = (_crystalYawRotation.Angle + _viewModel.RotationSpeed * dtSeconds) % 360.0;
                _panelYawRotation.Angle = -16.0 + Math.Sin(_crystalYawRotation.Angle * Math.PI / 180.0) * 12.0;
            }
        }

        public void ToggleCameraProjection()
        {
            _usingOrthoCamera = !_usingOrthoCamera;
            if (_usingOrthoCamera)
            {
                Viewport.Camera = _orthoCamera;
                _viewModel.CameraModeName = "OrthographicCamera (Width 5.2)";
            }
            else
            {
                Viewport.Camera = _perspectiveCamera;
                _viewModel.CameraModeName = "PerspectiveCamera (FOV 45°)";
            }
        }

        public void ResetView()
        {
            _crystalYawRotation.Angle = 28.0;
            _crystalPitchRotation.Angle = 18.0;
            _cameraDistance = 6.2;
            UpdateCameraDistance();
            _viewModel.LastHitSummary = "视角已重置 (Yaw=28°, Pitch=18°, Dist=6.2)";
        }

        public void HandleMouseDown(Point viewportPos)
        {
            // Perform 3D ray hit test to inspect barycentric coordinates
            RayMeshGeometry3DHitTestResult rayHit = null;
            VisualTreeHelper.HitTest(
                Viewport,
                null,
                result =>
                {
                    if (result is RayMeshGeometry3DHitTestResult meshHit)
                    {
                        rayHit = meshHit;
                        return HitTestResultBehavior.Stop;
                    }
                    return HitTestResultBehavior.Continue;
                },
                new PointHitTestParameters(viewportPos));

            if (rayHit != null)
            {
                _viewModel.MeshHitCount++;
                Point3D p = rayHit.PointHit;
                _viewModel.LastHitSummary =
                    $"命中 3D 网格 #{_viewModel.MeshHitCount}: Point=({p.X:F2}, {p.Y:F2}, {p.Z:F2}), 重心=({rayHit.VertexWeight1:F2}, {rayHit.VertexWeight2:F2}, {rayHit.VertexWeight3:F2})";
            }
            else
            {
                _isDraggingOrbit = true;
                _lastDragPoint = viewportPos;
            }
        }

        public void HandleMouseMove(Point viewportPos, bool leftPressed)
        {
            if (!_isDraggingOrbit || !leftPressed)
            {
                _isDraggingOrbit = false;
                return;
            }

            Vector delta = viewportPos - _lastDragPoint;
            _lastDragPoint = viewportPos;
            _crystalYawRotation.Angle = (_crystalYawRotation.Angle + delta.X * 0.55) % 360.0;
            _crystalPitchRotation.Angle = Math.Clamp(_crystalPitchRotation.Angle + delta.Y * 0.45, -75.0, 75.0);
        }

        public void HandleMouseUp()
        {
            _isDraggingOrbit = false;
        }

        public void HandleMouseWheel(int wheelDelta)
        {
            double step = wheelDelta > 0 ? -0.35 : 0.35;
            _cameraDistance = Math.Clamp(_cameraDistance + step, 3.4, 10.5);
            UpdateCameraDistance();
        }

        private void UpdateCameraDistance()
        {
            _perspectiveCamera.Position = new Point3D(0, 0.35, _cameraDistance);
            _orthoCamera.Position = new Point3D(0, 0.35, _cameraDistance);
            _orthoCamera.Width = _cameraDistance * 0.84;
        }

        private void OnCrystalMouseDown(object sender, MouseButtonEventArgs e)
        {
            bool pulse = (_viewModel.MeshHitCount % 2) == 0;
            _crystalEmissive.Brush = new SolidColorBrush(pulse
                ? Color.FromRgb(45, 95, 165)
                : Color.FromRgb(12, 28, 56));
        }

        private void OnSurface2DButtonClick(object sender, RoutedEventArgs e)
        {
            _viewModel.SurfaceClickCount++;
            int count = _viewModel.SurfaceClickCount;
            _surface2DButton.LabelText = $"3D 曲面已点击 ({count})";
            _surface2DButton.Background = (count % 2 == 1)
                ? new SolidColorBrush(Color.FromRgb(16, 185, 129))
                : new SolidColorBrush(Color.FromRgb(38, 94, 212));
            _viewModel.LastHitSummary = $"通过 Viewport2DVisual3D 重心 UV 映射触发 2D 按钮 Click 事件 (第 {count} 次)";
        }

        private static MeshGeometry3D CreateQuadMesh(double halfWidth, double halfHeight)
        {
            MeshGeometry3D mesh = new MeshGeometry3D();
            mesh.Positions.Add(new Point3D(-halfWidth,  halfHeight, 0));
            mesh.Positions.Add(new Point3D(-halfWidth, -halfHeight, 0));
            mesh.Positions.Add(new Point3D( halfWidth, -halfHeight, 0));
            mesh.Positions.Add(new Point3D( halfWidth,  halfHeight, 0));

            mesh.TextureCoordinates.Add(new Point(0, 0));
            mesh.TextureCoordinates.Add(new Point(0, 1));
            mesh.TextureCoordinates.Add(new Point(1, 1));
            mesh.TextureCoordinates.Add(new Point(1, 0));

            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(2);
            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(2);
            mesh.TriangleIndices.Add(3);
            return mesh;
        }

        private static MeshGeometry3D CreateBeveledCubeMesh(double s)
        {
            MeshGeometry3D mesh = new MeshGeometry3D();
            AddQuadFace(mesh,
                new Point3D(-s,  s,  s), new Point3D(-s, -s,  s), new Point3D( s, -s,  s), new Point3D( s,  s,  s),
                new Vector3D(0, 0, 1));
            AddQuadFace(mesh,
                new Point3D( s,  s, -s), new Point3D( s, -s, -s), new Point3D(-s, -s, -s), new Point3D(-s,  s, -s),
                new Vector3D(0, 0, -1));
            AddQuadFace(mesh,
                new Point3D(-s,  s, -s), new Point3D(-s, -s, -s), new Point3D(-s, -s,  s), new Point3D(-s,  s,  s),
                new Vector3D(-1, 0, 0));
            AddQuadFace(mesh,
                new Point3D( s,  s,  s), new Point3D( s, -s,  s), new Point3D( s, -s, -s), new Point3D( s,  s, -s),
                new Vector3D(1, 0, 0));
            AddQuadFace(mesh,
                new Point3D(-s,  s, -s), new Point3D(-s,  s,  s), new Point3D( s,  s,  s), new Point3D( s,  s, -s),
                new Vector3D(0, 1, 0));
            AddQuadFace(mesh,
                new Point3D(-s, -s,  s), new Point3D(-s, -s, -s), new Point3D( s, -s, -s), new Point3D( s, -s,  s),
                new Vector3D(0, -1, 0));
            return mesh;
        }

        private static void AddQuadFace(MeshGeometry3D mesh, Point3D p0, Point3D p1, Point3D p2, Point3D p3, Vector3D normal)
        {
            int baseIdx = mesh.Positions.Count;
            mesh.Positions.Add(p0);
            mesh.Positions.Add(p1);
            mesh.Positions.Add(p2);
            mesh.Positions.Add(p3);

            for (int i = 0; i < 4; i++)
            {
                mesh.Normals.Add(normal);
            }

            mesh.TextureCoordinates.Add(new Point(0, 0));
            mesh.TextureCoordinates.Add(new Point(0, 1));
            mesh.TextureCoordinates.Add(new Point(1, 1));
            mesh.TextureCoordinates.Add(new Point(1, 0));

            mesh.TriangleIndices.Add(baseIdx + 0);
            mesh.TriangleIndices.Add(baseIdx + 1);
            mesh.TriangleIndices.Add(baseIdx + 2);
            mesh.TriangleIndices.Add(baseIdx + 0);
            mesh.TriangleIndices.Add(baseIdx + 2);
            mesh.TriangleIndices.Add(baseIdx + 3);
        }
    }
}
