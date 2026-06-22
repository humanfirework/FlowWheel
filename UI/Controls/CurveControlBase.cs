using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using FlowWheel.Core;

namespace FlowWheel.UI.Controls
{
    using WpfPoint = System.Windows.Point;
    using WpfColor = System.Windows.Media.Color;
    using WpfBrush = System.Windows.Media.Brush;

    /// <summary>
    /// Shared base class for CurveEditor and CurvePreview, extracting common
    /// coordinate conversion, grid drawing, and theme adaptation logic.
    /// </summary>
    public abstract class CurveControlBase : System.Windows.Controls.Control
    {
        protected Canvas? _canvas;
        protected Path? _curvePath;
        protected bool _isLoaded = false;
        protected bool _themeChangeHandlerAdded = false;
        protected readonly List<UIElement> _canvasElements = new List<UIElement>();
        private DispatcherTimer? _resizeDebounceTimer;

        protected const double AxisMarginLeft = 28;
        protected const double AxisMarginBottom = 22;
        protected const double AxisMarginTop = 6;
        protected const double AxisMarginRight = 6;

        protected CurveControlBase()
        {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            SizeChanged += OnSizeChanged;
        }

        protected virtual void OnLoaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;

            if (!_themeChangeHandlerAdded)
            {
                SystemParameters.StaticPropertyChanged += OnSystemPropertyChanged;
                _themeChangeHandlerAdded = true;
            }

            Dispatcher.BeginInvoke(new Action(() =>
            {
                Redraw();
            }), System.Windows.Threading.DispatcherPriority.Render);
        }

        protected virtual void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_themeChangeHandlerAdded)
            {
                SystemParameters.StaticPropertyChanged -= OnSystemPropertyChanged;
                _themeChangeHandlerAdded = false;
            }
        }

        private void OnSystemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SystemParameters.HighContrast))
            {
                Redraw();
            }
        }

        protected virtual void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!_isLoaded) return;

            // Debounce resize events to prevent multiple redraws during window resize
            _resizeDebounceTimer?.Stop();
            _resizeDebounceTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(50), DispatcherPriority.Render, (s, args) =>
            {
                Redraw();
                _resizeDebounceTimer?.Stop();
            }, Dispatcher);
            _resizeDebounceTimer.Start();
        }

        #region Coordinate helpers

        protected (double plotW, double plotH) GetPlotSize()
        {
            double cw = _canvas?.ActualWidth ?? 300;
            double ch = _canvas?.ActualHeight ?? 200;
            double pw = Math.Max(1, cw - AxisMarginLeft - AxisMarginRight);
            double ph = Math.Max(1, ch - AxisMarginTop - AxisMarginBottom);
            return (pw, ph);
        }

        protected (double cx, double cy) ToCanvas(double nx, double ny)
        {
            var (pw, ph) = GetPlotSize();
            return (AxisMarginLeft + nx * pw,
                    AxisMarginTop + (1.0 - ny) * ph);
        }

        protected (double nx, double ny) ToNormalised(double cx, double cy)
        {
            var (pw, ph) = GetPlotSize();
            double nx = (cx - AxisMarginLeft) / pw;
            double ny = 1.0 - (cy - AxisMarginTop) / ph;
            return (Math.Clamp(nx, 0, 1), Math.Clamp(ny, 0, 1));
        }

        #endregion

        #region Canvas element management

        protected void ClearCanvasElements()
        {
            if (_canvas == null) return;
            foreach (var el in _canvasElements)
                _canvas.Children.Remove(el);
            _canvasElements.Clear();
        }

        protected void AddCanvasElement(UIElement element)
        {
            _canvas?.Children.Add(element);
            _canvasElements.Add(element);
        }

        #endregion

        #region Grid rendering (shared)

        protected void DrawGrid()
        {
            if (_canvas == null) return;

            var (pw, ph) = GetPlotSize();
            var gridBrush = GetGridBrush();
            var axisBrush = GetAxisBrush();
            var labelBrush = GetLabelBrush();
            var diagBrush = GetDiagonalBrush();

            // Draw 5 major divisions (0.0, 0.2, 0.4, 0.6, 0.8, 1.0)
            for (int i = 0; i <= 5; i++)
            {
                double x = AxisMarginLeft + i * pw / 5;
                var vline = new Line
                {
                    X1 = x, Y1 = AxisMarginTop,
                    X2 = x, Y2 = AxisMarginTop + ph,
                    Stroke = gridBrush,
                    StrokeThickness = 0.6,
                    Opacity = i == 0 || i == 5 ? 0.7 : 0.4
                };
                AddCanvasElement(vline);

                var tb = new TextBlock
                {
                    Text = (i / 5.0).ToString("0.0"),
                    FontSize = 9,
                    Foreground = labelBrush,
                    Opacity = 0.7
                };
                Canvas.SetLeft(tb, x - 6);
                Canvas.SetTop(tb, AxisMarginTop + ph + 4);
                AddCanvasElement(tb);
            }

            for (int i = 0; i <= 5; i++)
            {
                double y = AxisMarginTop + i * ph / 5;
                var hline = new Line
                {
                    X1 = AxisMarginLeft, Y1 = y,
                    X2 = AxisMarginLeft + pw, Y2 = y,
                    Stroke = gridBrush,
                    StrokeThickness = 0.6,
                    Opacity = i == 0 || i == 5 ? 0.7 : 0.4
                };
                AddCanvasElement(hline);

                var tb = new TextBlock
                {
                    Text = (1.0 - i / 5.0).ToString("0.0"),
                    FontSize = 9,
                    Foreground = labelBrush,
                    Opacity = 0.7
                };
                Canvas.SetLeft(tb, 3);
                Canvas.SetTop(tb, y - 6);
                AddCanvasElement(tb);
            }

            AddLine(AxisMarginLeft, AxisMarginTop + ph, AxisMarginLeft + pw, AxisMarginTop + ph, axisBrush, 1.5);
            AddLine(AxisMarginLeft, AxisMarginTop, AxisMarginLeft, AxisMarginTop + ph, axisBrush, 1.5);
            AddLine(AxisMarginLeft, AxisMarginTop + ph, AxisMarginLeft + pw, AxisMarginTop, diagBrush, 1,
                new DoubleCollection { 5, 3 });
        }

        protected void AddLine(double x1, double y1, double x2, double y2,
            WpfBrush stroke, double thickness, DoubleCollection? dash = null)
        {
            var line = new Line
            {
                X1 = x1, Y1 = y1, X2 = x2, Y2 = y2,
                Stroke = stroke, StrokeThickness = thickness
            };
            if (dash != null) line.StrokeDashArray = dash;
            AddCanvasElement(line);
        }

        #endregion

        #region Curve path generation (shared)

        protected PathGeometry BuildCurveGeometry(Func<double, double> evaluateY, int segments = 120)
        {
            var geometry = new PathGeometry();
            var (sx, sy) = ToCanvas(0, evaluateY(0));
            var figure = new PathFigure { StartPoint = new WpfPoint(sx, sy) };

            for (int i = 1; i <= segments; i++)
            {
                double t = (double)i / segments;
                double y = evaluateY(t);
                var (cx, cy) = ToCanvas(t, y);
                figure.Segments.Add(new LineSegment(new WpfPoint(cx, cy), true));
            }

            geometry.Figures.Add(figure);
            return geometry;
        }

        protected PathGeometry BuildAreaGeometry(Func<double, double> evaluateY, int segments = 120)
        {
            var geometry = new PathGeometry();
            var (pw, ph) = GetPlotSize();
            double bottomY = AxisMarginTop + ph;
            var (startX, startY) = ToCanvas(0, evaluateY(0));

            var figure = new PathFigure { StartPoint = new WpfPoint(startX, bottomY) };
            figure.Segments.Add(new LineSegment(new WpfPoint(startX, startY), true));

            for (int i = 1; i <= segments; i++)
            {
                double t = (double)i / segments;
                double y = evaluateY(t);
                var (cx, cy) = ToCanvas(t, y);
                figure.Segments.Add(new LineSegment(new WpfPoint(cx, cy), true));
            }

            double endX = AxisMarginLeft + pw;
            figure.Segments.Add(new LineSegment(new WpfPoint(endX, bottomY), true));
            figure.IsClosed = true;

            geometry.Figures.Add(figure);
            return geometry;
        }

        #endregion

        #region Theme brushes (shared)

        protected WpfColor GetAccentColor()
        {
            if (TryFindResource("Brush.Curve.Line") is SolidColorBrush brush)
                return brush.Color;
            return WpfColor.FromRgb(0, 120, 212);
        }

        protected WpfBrush GetGridBrush()
        {
            if (TryFindResource("CurveEditorGridBrush") is WpfBrush brush)
                return brush;
            if (TryFindResource("Brush.Curve.Grid") is WpfBrush brush2)
                return brush2;
            return new SolidColorBrush(WpfColor.FromArgb(60, 180, 180, 180));
        }

        protected WpfBrush GetAxisBrush()
        {
            if (TryFindResource("CurveEditorAxisBrush") is WpfBrush brush)
                return brush;
            if (TryFindResource("Brush.Curve.Axis") is WpfBrush brush2)
                return brush2;
            return new SolidColorBrush(WpfColor.FromRgb(100, 100, 100));
        }

        protected WpfBrush GetLabelBrush()
        {
            if (TryFindResource("CurveEditorLabelBrush") is WpfBrush brush)
                return brush;
            if (TryFindResource("Brush.Curve.Label") is WpfBrush brush2)
                return brush2;
            return new SolidColorBrush(WpfColor.FromRgb(80, 80, 80));
        }

        protected WpfBrush GetDiagonalBrush()
        {
            if (TryFindResource("Brush.Curve.Diagonal") is WpfBrush brush)
                return brush;
            return new SolidColorBrush(WpfColor.FromArgb(150, 150, 150, 150));
        }

        protected WpfColor GetPointFillColor()
        {
            if (TryFindResource("Brush.Curve.PointFill") is SolidColorBrush brush)
                return brush.Color;
            return Colors.White;
        }

        protected System.Windows.Media.LinearGradientBrush GetCurveAreaBrush(WpfColor accent)
        {
            return new System.Windows.Media.LinearGradientBrush
            {
                StartPoint = new System.Windows.Point(0.5, 0),
                EndPoint = new System.Windows.Point(0.5, 1),
                GradientStops = new System.Windows.Media.GradientStopCollection
                {
                    new System.Windows.Media.GradientStop(System.Windows.Media.Color.FromArgb(60, accent.R, accent.G, accent.B), 0),
                    new System.Windows.Media.GradientStop(System.Windows.Media.Color.FromArgb(10, accent.R, accent.G, accent.B), 0.65),
                    new System.Windows.Media.GradientStop(System.Windows.Media.Color.FromArgb(0, accent.R, accent.G, accent.B), 1)
                }
            };
        }

        protected System.Windows.Media.Brush GetCurveGlowBrush(WpfColor accent)
        {
            return new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromArgb(80, accent.R, accent.G, accent.B));
        }

        #endregion

        protected abstract void Redraw();
    }
}
