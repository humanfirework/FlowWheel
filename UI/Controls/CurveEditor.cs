using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using FlowWheel.Core;

using WpfColor = System.Windows.Media.Color;

namespace FlowWheel.UI.Controls
{
    public class CurveEditor : CurveControlBase
    {
        private readonly List<Ellipse> _controlEllipses = new List<Ellipse>();
        private readonly List<int> _ellipseToCurveIndex = new List<int>();
        private Ellipse? _draggingEllipse = null;
        private int _draggingCurveIndex = -1;
        private Path? _areaPath;
        private Path? _glowPath;
        private const double PointHitRadius = 18;
        private long _lastUpdateTick = 0;

        public static readonly DependencyProperty CurvePointsProperty =
            DependencyProperty.Register(nameof(CurvePoints), typeof(List<CustomCurvePoint>), typeof(CurveEditor),
                new PropertyMetadata(null, OnCurvePointsChanged));

        public static readonly DependencyProperty CurveTypeProperty =
            DependencyProperty.Register(nameof(CurveType), typeof(AccelerationCurveType), typeof(CurveEditor),
                new PropertyMetadata(AccelerationCurveType.Linear, OnCurveTypeChanged));

        public static readonly DependencyProperty ConfigProperty =
            DependencyProperty.Register(nameof(Config), typeof(AppConfig), typeof(CurveEditor),
                new PropertyMetadata(null, OnConfigChanged));

        public List<CustomCurvePoint>? CurvePoints
        {
            get => (List<CustomCurvePoint>?)GetValue(CurvePointsProperty);
            set => SetValue(CurvePointsProperty, value);
        }

        public AccelerationCurveType CurveType
        {
            get => (AccelerationCurveType)GetValue(CurveTypeProperty);
            set => SetValue(CurveTypeProperty, value);
        }

        public AppConfig? Config
        {
            get => (AppConfig?)GetValue(ConfigProperty);
            set => SetValue(ConfigProperty, value);
        }

        public event EventHandler? CurveChanged;

        static CurveEditor()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CurveEditor),
                new FrameworkPropertyMetadata(typeof(CurveEditor)));
        }

        public CurveEditor() { }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _canvas = GetTemplateChild("PART_Canvas") as Canvas;
            if (_canvas != null)
            {
                _canvas.MouseLeftButtonDown += OnCanvasLeftButtonDown;
                _canvas.MouseMove += OnCanvasMouseMove;
                _canvas.MouseLeftButtonUp += OnCanvasLeftButtonUp;
                _canvas.MouseRightButtonDown += OnCanvasRightButtonDown;
            }
            _curvePath = GetTemplateChild("PART_CurvePath") as Path;
        }

        protected override void OnLoaded(object sender, RoutedEventArgs e)
        {
            base.OnLoaded(sender, e);

            if (CurvePoints == null || CurvePoints.Count < 2)
            {
                CurvePoints = new List<CustomCurvePoint>
                {
                    new CustomCurvePoint(0.0, 0.0),
                    new CustomCurvePoint(1.0, 1.0)
                };
            }
        }

        private static void OnCurvePointsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CurveEditor editor && editor._isLoaded)
                editor.Redraw();
        }

        private static void OnCurveTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CurveEditor editor && editor._isLoaded)
                editor.Redraw();
        }

        private static void OnConfigChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CurveEditor editor && editor._isLoaded)
                editor.Redraw();
        }

        #region Rendering

        protected override void Redraw()
        {
            if (_canvas == null || _curvePath == null || !_isLoaded) return;

            var (pw, ph) = GetPlotSize();
            if (pw <= 0 || ph <= 0) return;

            ClearCanvas();
            DrawGrid();
            DrawCurve();
        }

        private void ClearCanvas()
        {
            ClearCanvasElements();

            if (_areaPath != null)
            {
                _canvas?.Children.Remove(_areaPath);
                _areaPath = null;
            }
            if (_glowPath != null)
            {
                _canvas?.Children.Remove(_glowPath);
                _glowPath = null;
            }

            foreach (var ep in _controlEllipses)
            {
                ep.MouseLeftButtonDown -= OnEllipseMouseLeftButtonDown;
                ep.MouseMove -= OnEllipseMouseMove;
                ep.MouseLeftButtonUp -= OnEllipseMouseLeftButtonUp;
                _canvas!.Children.Remove(ep);
            }
            _controlEllipses.Clear();
            _ellipseToCurveIndex.Clear();
        }

        private void DrawCurve()
        {
            if (_curvePath == null || _canvas == null || CurvePoints == null) return;

            if (CurveType == AccelerationCurveType.Custom)
                DrawCustomCurve();
            else
                DrawPresetCurve();
        }

        private void DrawPresetCurve()
        {
            if (_curvePath == null || _canvas == null) return;
            var config = Config ?? new AppConfig();
            var accent = GetAccentColor();

            BuildDecorations(
                t => global::FlowWheel.Core.AccelerationCurve.ApplyCurve(t, CurveType, config),
                accent);

            _curvePath.Data = BuildCurveGeometry(t => global::FlowWheel.Core.AccelerationCurve.ApplyCurve(t, CurveType, config));
            _curvePath.Stroke = new SolidColorBrush(accent);
            _curvePath.StrokeThickness = 3;
            _curvePath.Fill = null;
            _curvePath.StrokeLineJoin = PenLineJoin.Round;
        }

        private void DrawCustomCurve()
        {
            if (_curvePath == null || _canvas == null) return;

            var points = CurvePoints;
            if (points == null || points.Count < 2)
            {
                points = new List<CustomCurvePoint>
                {
                    new CustomCurvePoint(0.0, 0.0),
                    new CustomCurvePoint(1.0, 1.0)
                };
                CurvePoints = points;
            }

            var sorted = global::FlowWheel.Core.AccelerationCurve.GetOrCacheSortedPoints(points);
            var accent = GetAccentColor();

            BuildDecorations(t => global::FlowWheel.Core.AccelerationCurve.ApplyCurveWithPoints(t, sorted), accent);

            _curvePath.Data = BuildCurveGeometry(t => global::FlowWheel.Core.AccelerationCurve.ApplyCurveWithPoints(t, sorted));
            _curvePath.Stroke = new SolidColorBrush(accent);
            _curvePath.StrokeThickness = 3;
            _curvePath.Fill = null;
            _curvePath.StrokeLineJoin = PenLineJoin.Round;

            // Control points
            for (int si = 0; si < sorted.Count; si++)
            {
                int ci = points.IndexOf(sorted[si]);
                var (ex, ey) = ToCanvas(sorted[si].X, sorted[si].Y);

                bool isEndpoint = (si == 0 || si == sorted.Count - 1);
                double r = isEndpoint ? 7 : 5;
                var accentColor = GetAccentColor();
                var fillColor = isEndpoint ? accentColor : GetPointFillColor();

                var ellipse = new Ellipse
                {
                    Width = r * 2,
                    Height = r * 2,
                    Fill = new SolidColorBrush(fillColor),
                    Stroke = new SolidColorBrush(accentColor),
                    StrokeThickness = isEndpoint ? 2 : 1.5,
                    Cursor = System.Windows.Input.Cursors.Hand,
                    ToolTip = $"({sorted[si].X:F2}, {sorted[si].Y:F2})"
                };

                ellipse.MouseLeftButtonDown += OnEllipseMouseLeftButtonDown;
                ellipse.MouseMove += OnEllipseMouseMove;
                ellipse.MouseLeftButtonUp += OnEllipseMouseLeftButtonUp;

                Canvas.SetLeft(ellipse, ex - r);
                Canvas.SetTop(ellipse, ey - r);

                _canvas.Children.Add(ellipse);
                _controlEllipses.Add(ellipse);
                _ellipseToCurveIndex.Add(ci);
            }
        }

        private void BuildDecorations(Func<double, double> evaluateY, WpfColor accent)
        {
            if (_canvas == null) return;

            if (_areaPath == null)
            {
                _areaPath = new Path
                {
                    Fill = GetCurveAreaBrush(accent),
                    Stroke = null,
                    IsHitTestVisible = false,
                    Opacity = 0.75
                };
                _canvas.Children.Insert(0, _areaPath);
            }
            else
            {
                _areaPath.Fill = GetCurveAreaBrush(accent);
            }
            _areaPath.Data = BuildAreaGeometry(evaluateY, 80);

            if (_glowPath == null)
            {
                _glowPath = new Path
                {
                    Stroke = GetCurveGlowBrush(accent),
                    StrokeThickness = 4,
                    StrokeLineJoin = PenLineJoin.Round,
                    Fill = null,
                    IsHitTestVisible = false,
                    Opacity = 0.25,
                    Effect = new System.Windows.Media.Effects.BlurEffect { Radius = 3 }
                };
                _canvas.Children.Insert(1, _glowPath);
            }
            _glowPath.Data = BuildCurveGeometry(evaluateY, 80);
        }

        private void OnEllipseMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (CurveType != AccelerationCurveType.Custom) return;

            if (sender is Ellipse ellipse)
            {
                int idx = _controlEllipses.IndexOf(ellipse);
                if (idx >= 0)
                {
                    _draggingEllipse = ellipse;
                    _draggingCurveIndex = _ellipseToCurveIndex[idx];
                    ellipse.CaptureMouse();
                    e.Handled = true;
                }
            }
        }

        private void OnEllipseMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_draggingEllipse == null || _draggingCurveIndex < 0 || _canvas == null || CurvePoints == null)
                return;

            var pos = e.GetPosition(_canvas);
            var (nx, ny) = ToNormalised(pos.X, pos.Y);

            int sortedIdx = GetSortedIndex(_draggingCurveIndex);
            if (sortedIdx == 0) nx = 0;
            else if (sortedIdx == CurvePoints.Count - 1) nx = 1;

            if (CurvePoints.Count > 2 && sortedIdx > 0 && sortedIdx < CurvePoints.Count - 1)
            {
                var sorted = global::FlowWheel.Core.AccelerationCurve.GetOrCacheSortedPoints(CurvePoints);
                double minX = sorted[sortedIdx - 1].X + 0.01;
                double maxX = sorted[sortedIdx + 1].X - 0.01;
                nx = Math.Clamp(nx, minX, maxX);
            }

            CurvePoints[_draggingCurveIndex] = new CustomCurvePoint(nx, ny);

            var (ecx, ecy) = ToCanvas(nx, ny);
            double r = _draggingEllipse.Width / 2;
            Canvas.SetLeft(_draggingEllipse, ecx - r);
            Canvas.SetTop(_draggingEllipse, ecy - r);
            _draggingEllipse.ToolTip = $"({nx:F2}, {ny:F2})";

            UpdateCurvePathOnly();
        }

        private void OnEllipseMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_draggingEllipse != null)
            {
                _draggingEllipse.ReleaseMouseCapture();
                _draggingEllipse = null;
                _draggingCurveIndex = -1;
                Redraw(); // refresh area fill and glow after drag
                CurveChanged?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
            }
        }

        #endregion

        #region Mouse interaction — canvas click/drag/right-click

        private void OnCanvasLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (CurveType != AccelerationCurveType.Custom || _canvas == null) return;

            var pos = e.GetPosition(_canvas);

            int bestIdx = -1;
            double bestDist = PointHitRadius;

            for (int i = 0; i < _controlEllipses.Count; i++)
            {
                var ep = _controlEllipses[i];
                double ex = Canvas.GetLeft(ep) + ep.Width / 2;
                double ey = Canvas.GetTop(ep) + ep.Height / 2;
                double d = Math.Sqrt((pos.X - ex) * (pos.X - ex) + (pos.Y - ey) * (pos.Y - ey));
                if (d < bestDist)
                {
                    bestDist = d;
                    bestIdx = i;
                }
            }

            if (bestIdx >= 0)
            {
                _draggingEllipse = _controlEllipses[bestIdx];
                _draggingCurveIndex = _ellipseToCurveIndex[bestIdx];
                _draggingEllipse.CaptureMouse();
                e.Handled = true;
            }
        }

        private void OnCanvasMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_draggingEllipse == null || _draggingCurveIndex < 0 || _canvas == null || CurvePoints == null)
                return;

            var pos = e.GetPosition(_canvas);
            var (nx, ny) = ToNormalised(pos.X, pos.Y);

            int sortedIdx = GetSortedIndex(_draggingCurveIndex);
            if (sortedIdx == 0) nx = 0;
            else if (sortedIdx == CurvePoints.Count - 1) nx = 1;

            if (CurvePoints.Count > 2 && sortedIdx > 0 && sortedIdx < CurvePoints.Count - 1)
            {
                var sorted = global::FlowWheel.Core.AccelerationCurve.GetOrCacheSortedPoints(CurvePoints);
                double minX = sorted[sortedIdx - 1].X + 0.01;
                double maxX = sorted[sortedIdx + 1].X - 0.01;
                nx = Math.Clamp(nx, minX, maxX);
            }

            CurvePoints[_draggingCurveIndex] = new CustomCurvePoint(nx, ny);

            var (ecx, ecy) = ToCanvas(nx, ny);
            double r = _draggingEllipse.Width / 2;
            Canvas.SetLeft(_draggingEllipse, ecx - r);
            Canvas.SetTop(_draggingEllipse, ecy - r);
            _draggingEllipse.ToolTip = $"({nx:F2}, {ny:F2})";

            UpdateCurvePathOnly();
        }

        private void OnCanvasLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_draggingEllipse != null)
            {
                _draggingEllipse.ReleaseMouseCapture();
                _draggingEllipse = null;
                _draggingCurveIndex = -1;
                Redraw(); // refresh area fill and glow after drag
                CurveChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnCanvasRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (CurveType != AccelerationCurveType.Custom || _canvas == null || CurvePoints == null) return;

            var pos = e.GetPosition(_canvas);
            var (nx, ny) = ToNormalised(pos.X, pos.Y);

            if (nx <= 0.005 || nx >= 0.995) return;

            // Check if right-click is near an existing non-endpoint → delete it
            var sorted = global::FlowWheel.Core.AccelerationCurve.GetOrCacheSortedPoints(CurvePoints);
            for (int si = 0; si < sorted.Count; si++)
            {
                if (si == 0 || si == sorted.Count - 1) continue;

                var (px, py) = ToCanvas(sorted[si].X, sorted[si].Y);
                double d = Math.Sqrt((pos.X - px) * (pos.X - px) + (pos.Y - py) * (pos.Y - py));
                if (d < PointHitRadius)
                {
                    CurvePoints.Remove(sorted[si]);
                    global::FlowWheel.Core.AccelerationCurve.InvalidateCache();
                    Redraw();
                    CurveChanged?.Invoke(this, EventArgs.Empty);
                    e.Handled = true;
                    return;
                }
            }

            // Add new point
            CurvePoints.Add(new CustomCurvePoint(nx, ny));
            global::FlowWheel.Core.AccelerationCurve.InvalidateCache();

            var reSorted = global::FlowWheel.Core.AccelerationCurve.GetOrCacheSortedPoints(CurvePoints);
            CurvePoints.Clear();
            CurvePoints.AddRange(reSorted);

            Redraw();
            CurveChanged?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }

        private int GetSortedIndex(int curveIndex)
        {
            if (CurvePoints == null || curveIndex < 0 || curveIndex >= CurvePoints.Count) return -1;
            var sorted = global::FlowWheel.Core.AccelerationCurve.GetOrCacheSortedPoints(CurvePoints);
            return sorted.IndexOf(CurvePoints[curveIndex]);
        }

        #endregion

        #region Path-only fast update (during drag)

        private void UpdateCurvePathOnly()
        {
            if (_curvePath == null || CurvePoints == null || CurvePoints.Count < 2) return;

            // Throttle to ~60fps (16ms per frame)
            long currentTick = Environment.TickCount64;
            if (currentTick - _lastUpdateTick < 16) return;
            _lastUpdateTick = currentTick;

            var sorted = global::FlowWheel.Core.AccelerationCurve.GetOrCacheSortedPoints(CurvePoints);
            var geometry = BuildCurveGeometry(t => global::FlowWheel.Core.AccelerationCurve.ApplyCurveWithPoints(t, sorted), 80);
            _curvePath.Data = geometry;
            if (_glowPath != null) _glowPath.Data = geometry;
            // Area fill is intentionally not updated during drag to reduce CPU load;
            // it will be redrawn when the drag completes (OnEllipseMouseLeftButtonUp / OnCanvasLeftButtonUp).
        }

        #endregion

        #region Public API

        public void ResetToDefault()
        {
            if (CurvePoints == null)
                CurvePoints = new List<CustomCurvePoint>();
            else
                CurvePoints.Clear();

            CurvePoints.Add(new CustomCurvePoint(0.0, 0.0));
            CurvePoints.Add(new CustomCurvePoint(1.0, 1.0));

            Redraw();
            CurveChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion
    }
}
