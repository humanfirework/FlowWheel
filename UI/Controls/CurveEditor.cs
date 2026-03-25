using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using FlowWheel.Core;

namespace FlowWheel.UI.Controls
{
    public class CurveEditor : System.Windows.Controls.Control
    {
        private Canvas? _canvas;
        private Path? _curvePath;
        private readonly List<Ellipse> _controlEllipses = new List<Ellipse>();
        private readonly List<int> _ellipseToCurveIndex = new List<int>();
        private readonly List<UIElement> _canvasElements = new List<UIElement>();
        private Ellipse? _draggingEllipse = null;
        private int _draggingCurveIndex = -1;
        private bool _isLoaded = false;
        private const double PointHitRadius = 14;
        private const double AxisMarginLeft = 28;
        private const double AxisMarginBottom = 22;
        private const double AxisMarginTop = 6;
        private const double AxisMarginRight = 6;

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

        public CurveEditor()
        {
            Loaded += OnLoaded;
            SizeChanged += OnSizeChanged;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _canvas = GetTemplateChild("PART_Canvas") as Canvas;
            if (_canvas != null)
            {
                _canvas.MouseLeftButtonDown += OnCanvasLeftButtonDown;
                _canvas.MouseMove += OnCanvasMouseMove;
                _canvas.MouseLeftButtonUp += OnCanvasLeftButtonUp;
                _canvas.MouseRightButtonDown += OnCanvasRightButtonDown;
            }

            _curvePath = GetTemplateChild("PART_CurvePath") as Path;

            _isLoaded = true;

            if (CurvePoints == null || CurvePoints.Count < 2)
            {
                CurvePoints = new List<CustomCurvePoint>
                {
                    new CustomCurvePoint(0.0, 0.0),
                    new CustomCurvePoint(1.0, 1.0)
                };
            }

            Dispatcher.BeginInvoke(new Action(UpdateCurve),
                System.Windows.Threading.DispatcherPriority.Loaded);
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
                var sorted = CurvePoints.OrderBy(p => p.X).ToList();
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
                CurveChanged?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
            }
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_isLoaded) UpdateCurve();
        }

        private static void OnCurvePointsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CurveEditor editor && editor._isLoaded)
                editor.UpdateCurve();
        }

        private static void OnCurveTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CurveEditor editor && editor._isLoaded)
                editor.UpdateCurve();
        }

        private static void OnConfigChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CurveEditor editor && editor._isLoaded)
                editor.UpdateCurve();
        }

        #region Coordinate helpers

        private (double plotW, double plotH) GetPlotSize()
        {
            double cw = _canvas?.ActualWidth ?? 300;
            double ch = _canvas?.ActualHeight ?? 200;
            double pw = Math.Max(1, cw - AxisMarginLeft - AxisMarginRight);
            double ph = Math.Max(1, ch - AxisMarginTop - AxisMarginBottom);
            return (pw, ph);
        }

        private (double cx, double cy) ToCanvas(double nx, double ny)
        {
            var (pw, ph) = GetPlotSize();
            return (AxisMarginLeft + nx * pw,
                    AxisMarginTop + (1.0 - ny) * ph);
        }

        private (double nx, double ny) ToNormalised(double cx, double cy)
        {
            var (pw, ph) = GetPlotSize();
            double nx = (cx - AxisMarginLeft) / pw;
            double ny = 1.0 - (cy - AxisMarginTop) / ph;
            return (Math.Clamp(nx, 0, 1), Math.Clamp(ny, 0, 1));
        }

        #endregion

        #region Rendering

        private void UpdateCurve()
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
            foreach (var el in _canvasElements)
                _canvas!.Children.Remove(el);
            _canvasElements.Clear();

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

        private void DrawGrid()
        {
            if (_canvas == null) return;

            var (pw, ph) = GetPlotSize();

            var gridBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(35, 128, 128, 128));
            var axisBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(90, 128, 128, 128));
            var labelBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(120, 128, 128, 128));
            var diagBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(40, 128, 128, 128));

            for (int i = 0; i <= 10; i++)
            {
                double x = AxisMarginLeft + i * pw / 10;
                var line = new Line
                {
                    X1 = x, Y1 = AxisMarginTop,
                    X2 = x, Y2 = AxisMarginTop + ph,
                    Stroke = gridBrush, StrokeThickness = 1
                };
                _canvas.Children.Add(line);
                _canvasElements.Add(line);

                if (i % 2 == 0)
                {
                    var tb = new TextBlock
                    {
                        Text = (i / 10.0).ToString("0.0"),
                        FontSize = 8,
                        Foreground = labelBrush
                    };
                    Canvas.SetLeft(tb, x - 8);
                    Canvas.SetTop(tb, AxisMarginTop + ph + 3);
                    _canvas.Children.Add(tb);
                    _canvasElements.Add(tb);
                }
            }

            for (int i = 0; i <= 10; i++)
            {
                double y = AxisMarginTop + i * ph / 10;
                var line = new Line
                {
                    X1 = AxisMarginLeft, Y1 = y,
                    X2 = AxisMarginLeft + pw, Y2 = y,
                    Stroke = gridBrush, StrokeThickness = 1
                };
                _canvas.Children.Add(line);
                _canvasElements.Add(line);

                if (i % 2 == 0)
                {
                    var tb = new TextBlock
                    {
                        Text = (1.0 - i / 10.0).ToString("0.0"),
                        FontSize = 8,
                        Foreground = labelBrush
                    };
                    Canvas.SetLeft(tb, 2);
                    Canvas.SetTop(tb, y - 6);
                    _canvas.Children.Add(tb);
                    _canvasElements.Add(tb);
                }
            }

            // X axis (bottom)
            AddLine(AxisMarginLeft, AxisMarginTop + ph, AxisMarginLeft + pw, AxisMarginTop + ph, axisBrush, 2);
            // Y axis (left)
            AddLine(AxisMarginLeft, AxisMarginTop, AxisMarginLeft, AxisMarginTop + ph, axisBrush, 2);
            // Diagonal y=x reference
            AddLine(AxisMarginLeft, AxisMarginTop + ph, AxisMarginLeft + pw, AxisMarginTop, diagBrush, 1,
                new DoubleCollection { 4, 2 });
        }

        private void AddLine(double x1, double y1, double x2, double y2,
            System.Windows.Media.Brush stroke, double thickness, DoubleCollection? dash = null)
        {
            var line = new Line
            {
                X1 = x1, Y1 = y1, X2 = x2, Y2 = y2,
                Stroke = stroke, StrokeThickness = thickness
            };
            if (dash != null) line.StrokeDashArray = dash;
            _canvas!.Children.Add(line);
            _canvasElements.Add(line);
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
            if (_curvePath == null) return;
            var config = Config ?? new AppConfig();

            var geometry = new PathGeometry();
            var (sx, sy) = ToCanvas(0, AccelerationCurve.ApplyCurve(0, CurveType, config));
            var figure = new PathFigure { StartPoint = new System.Windows.Point(sx, sy) };

            int segs = 120;
            for (int i = 1; i <= segs; i++)
            {
                double t = (double)i / segs;
                double y = AccelerationCurve.ApplyCurve(t, CurveType, config);
                var (cx, cy) = ToCanvas(t, y);
                figure.Segments.Add(new LineSegment(new System.Windows.Point(cx, cy), true));
            }

            geometry.Figures.Add(figure);
            _curvePath.Data = geometry;
            _curvePath.Stroke = new SolidColorBrush(GetAccentColor());
            _curvePath.StrokeThickness = 2;
            _curvePath.Fill = null;
        }

        private void DrawCustomCurve()
        {
            if (_curvePath == null || _canvas == null || CurvePoints == null) return;

            var points = CurvePoints;
            if (points.Count < 2)
            {
                points = new List<CustomCurvePoint>
                {
                    new CustomCurvePoint(0.0, 0.0),
                    new CustomCurvePoint(1.0, 1.0)
                };
                CurvePoints = points;
            }

            var sorted = points.OrderBy(p => p.X).ToList();

            // Curve path
            var (sx, sy) = ToCanvas(sorted[0].X, sorted[0].Y);
            var geometry = new PathGeometry();
            var figure = new PathFigure { StartPoint = new System.Windows.Point(sx, sy) };

            int segs = 120;
            for (int i = 1; i <= segs; i++)
            {
                double t = (double)i / segs;
                double y = AccelerationCurve.ApplyCurveWithPoints(t, sorted);
                var (cx, cy) = ToCanvas(t, y);
                figure.Segments.Add(new LineSegment(new System.Windows.Point(cx, cy), true));
            }

            geometry.Figures.Add(figure);
            _curvePath.Data = geometry;
            _curvePath.Stroke = new SolidColorBrush(GetAccentColor());
            _curvePath.StrokeThickness = 2;
            _curvePath.Fill = null;

            // Control points
            for (int si = 0; si < sorted.Count; si++)
            {
                int ci = points.IndexOf(sorted[si]);
                var (ex, ey) = ToCanvas(sorted[si].X, sorted[si].Y);

                bool isEndpoint = (si == 0 || si == sorted.Count - 1);
                double r = isEndpoint ? 6 : 5;
                var fill = isEndpoint ? GetAccentColor() : System.Windows.Media.Colors.White;
                var strokeC = GetAccentColor();

                var ellipse = new Ellipse
                {
                    Width = r * 2,
                    Height = r * 2,
                    Fill = new SolidColorBrush(fill),
                    Stroke = new SolidColorBrush(strokeC),
                    StrokeThickness = 2,
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

        #endregion

        #region Mouse interaction — left click drag

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
                var sorted = CurvePoints.OrderBy(p => p.X).ToList();
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
                CurveChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private int GetSortedIndex(int curveIndex)
        {
            if (CurvePoints == null || curveIndex < 0 || curveIndex >= CurvePoints.Count) return -1;
            var sorted = CurvePoints.OrderBy(p => p.X).ToList();
            return sorted.IndexOf(CurvePoints[curveIndex]);
        }

        #endregion

        #region Mouse interaction — right click (add / remove point)

        private void OnCanvasRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (CurveType != AccelerationCurveType.Custom || _canvas == null || CurvePoints == null) return;

            var pos = e.GetPosition(_canvas);
            var (nx, ny) = ToNormalised(pos.X, pos.Y);

            if (nx <= 0.005 || nx >= 0.995) return;

            // Check if right-click is near an existing non-endpoint → delete it
            var sorted = CurvePoints.OrderBy(p => p.X).ToList();
            for (int si = 0; si < sorted.Count; si++)
            {
                if (si == 0 || si == sorted.Count - 1) continue;

                var (px, py) = ToCanvas(sorted[si].X, sorted[si].Y);
                double d = Math.Sqrt((pos.X - px) * (pos.X - px) + (pos.Y - py) * (pos.Y - py));
                if (d < PointHitRadius)
                {
                    CurvePoints.Remove(sorted[si]);
                    UpdateCurve();
                    CurveChanged?.Invoke(this, EventArgs.Empty);
                    e.Handled = true;
                    return;
                }
            }

            // Add new point
            CurvePoints.Add(new CustomCurvePoint(nx, ny));

            var reSorted = CurvePoints.OrderBy(p => p.X).ToList();
            CurvePoints.Clear();
            CurvePoints.AddRange(reSorted);

            UpdateCurve();
            CurveChanged?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }

        #endregion

        #region Path-only fast update (during drag)

        private void UpdateCurvePathOnly()
        {
            if (_curvePath == null || CurvePoints == null || CurvePoints.Count < 2) return;

            var sorted = CurvePoints.OrderBy(p => p.X).ToList();

            var (sx, sy) = ToCanvas(sorted[0].X, sorted[0].Y);
            var geometry = new PathGeometry();
            var figure = new PathFigure { StartPoint = new System.Windows.Point(sx, sy) };

            int segs = 120;
            for (int i = 1; i <= segs; i++)
            {
                double t = (double)i / segs;
                double y = AccelerationCurve.ApplyCurveWithPoints(t, sorted);
                var (cx, cy) = ToCanvas(t, y);
                figure.Segments.Add(new LineSegment(new System.Windows.Point(cx, cy), true));
            }

            geometry.Figures.Add(figure);
            _curvePath.Data = geometry;
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

            UpdateCurve();
            CurveChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        private System.Windows.Media.Color GetAccentColor()
        {
            if (System.Windows.Application.Current.TryFindResource("Color.Accent")
                is System.Windows.Media.Color color)
                return color;
            return System.Windows.Media.Color.FromRgb(0, 120, 215);
        }
    }
}
