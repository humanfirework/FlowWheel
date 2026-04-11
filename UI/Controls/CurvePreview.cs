using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using FlowWheel.Core;

namespace FlowWheel.UI.Controls
{
    public class CurvePreview : System.Windows.Controls.Control
    {
        private Canvas? _canvas;
        private Path? _curvePath;
        private bool _isLoaded = false;
        private bool _themeChangeHandlerAdded = false;
        private readonly List<UIElement> _canvasElements = new List<UIElement>();
        private const double AxisMarginLeft = 30;
        private const double AxisMarginBottom = 20;
        private const double AxisMarginTop = 8;
        private const double AxisMarginRight = 8;

        public static readonly DependencyProperty CurveTypeProperty =
            DependencyProperty.Register(nameof(CurveType), typeof(AccelerationCurveType), typeof(CurvePreview),
                new PropertyMetadata(AccelerationCurveType.Linear, OnCurveParamsChanged));

        public static readonly DependencyProperty ExponentProperty =
            DependencyProperty.Register(nameof(Exponent), typeof(double), typeof(CurvePreview),
                new PropertyMetadata(1.5, OnCurveParamsChanged));

        public static readonly DependencyProperty LogBaseProperty =
            DependencyProperty.Register(nameof(LogBase), typeof(double), typeof(CurvePreview),
                new PropertyMetadata(2.0, OnCurveParamsChanged));

        public static readonly DependencyProperty SigmoidMidpointProperty =
            DependencyProperty.Register(nameof(SigmoidMidpoint), typeof(double), typeof(CurvePreview),
                new PropertyMetadata(0.5, OnCurveParamsChanged));

        public static readonly DependencyProperty SigmoidSteepnessProperty =
            DependencyProperty.Register(nameof(SigmoidSteepness), typeof(double), typeof(CurvePreview),
                new PropertyMetadata(8.0, OnCurveParamsChanged));

        public static readonly DependencyProperty CustomPointsProperty =
            DependencyProperty.Register(nameof(CustomPoints), typeof(List<CustomCurvePoint>), typeof(CurvePreview),
                new PropertyMetadata(null, OnCurveParamsChanged));

        public static readonly DependencyProperty AccentColorProperty =
            DependencyProperty.Register(nameof(AccentColor), typeof(System.Windows.Media.Color), typeof(CurvePreview),
                new PropertyMetadata(System.Windows.Media.Color.FromRgb(0, 120, 212), OnAccentColorChanged));

        public AccelerationCurveType CurveType
        {
            get => (AccelerationCurveType)GetValue(CurveTypeProperty);
            set => SetValue(CurveTypeProperty, value);
        }

        public double Exponent
        {
            get => (double)GetValue(ExponentProperty);
            set => SetValue(ExponentProperty, value);
        }

        public double LogBase
        {
            get => (double)GetValue(LogBaseProperty);
            set => SetValue(LogBaseProperty, value);
        }

        public double SigmoidMidpoint
        {
            get => (double)GetValue(SigmoidMidpointProperty);
            set => SetValue(SigmoidMidpointProperty, value);
        }

        public double SigmoidSteepness
        {
            get => (double)GetValue(SigmoidSteepnessProperty);
            set => SetValue(SigmoidSteepnessProperty, value);
        }

        public List<CustomCurvePoint>? CustomPoints
        {
            get => (List<CustomCurvePoint>?)GetValue(CustomPointsProperty);
            set => SetValue(CustomPointsProperty, value);
        }

        public System.Windows.Media.Color AccentColor
        {
            get => (System.Windows.Media.Color)GetValue(AccentColorProperty);
            set => SetValue(AccentColorProperty, value);
        }

        static CurvePreview()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CurvePreview),
                new FrameworkPropertyMetadata(typeof(CurvePreview)));
        }

        public CurvePreview()
        {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            SizeChanged += (s, e) => Redraw();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _canvas = GetTemplateChild("PART_Canvas") as Canvas;
            _curvePath = GetTemplateChild("PART_CurvePath") as Path;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;

            if (!_themeChangeHandlerAdded)
            {
                SystemParameters.StaticPropertyChanged += OnSystemPropertyChanged;
                _themeChangeHandlerAdded = true;
            }

            // 延迟重绘，确保控件有实际尺寸
            Dispatcher.BeginInvoke(new Action(() =>
            {
                Redraw();
            }), System.Windows.Threading.DispatcherPriority.Render);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
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

        private static void OnCurveParamsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CurvePreview preview)
            {
                if (preview._isLoaded)
                {
                    preview.Redraw();
                }
                else
                {
                    // 如果控件还没加载完成，延迟重绘
                    preview.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (preview._isLoaded)
                            preview.Redraw();
                    }), System.Windows.Threading.DispatcherPriority.Loaded);
                }
            }
        }

        private static void OnAccentColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CurvePreview preview && preview._isLoaded)
                preview.Redraw();
        }

        private (double pw, double ph) GetPlotSize()
        {
            if (_canvas == null) return (0, 0);
            double w = ActualWidth - AxisMarginLeft - AxisMarginRight;
            double h = ActualHeight - AxisMarginTop - AxisMarginBottom;
            return (System.Math.Max(10, w), System.Math.Max(10, h));
        }

        private (double cx, double cy) ToCanvas(double nx, double ny)
        {
            var (pw, ph) = GetPlotSize();
            double cx = AxisMarginLeft + nx * pw;
            double cy = AxisMarginTop + (1.0 - ny) * ph;
            return (cx, cy);
        }

        private void Redraw()
        {
            if (_canvas == null || _curvePath == null || !_isLoaded) return;

            var (pw, ph) = GetPlotSize();
            if (pw <= 0 || ph <= 0) return;

            // 只清除动态添加的元素，不清除 PART_CurvePath
            foreach (var el in _canvasElements)
                _canvas.Children.Remove(el);
            _canvasElements.Clear();

            DrawGrid(pw, ph);
            DrawAxes(pw, ph);
            DrawDiagonalRef(pw, ph);
            DrawCurve(pw, ph);
        }

        private void DrawGrid(double pw, double ph)
        {
            if (_canvas == null) return;

            var gridBrush = GetGridBrush();
            var labelBrush = GetLabelBrush();

            for (int i = 0; i <= 10; i++)
            {
                double x = AxisMarginLeft + i * pw / 10;
                var vline = new Line
                {
                    X1 = x, Y1 = AxisMarginTop,
                    X2 = x, Y2 = AxisMarginTop + ph,
                    Stroke = gridBrush, StrokeThickness = 1
                };
                _canvas.Children.Add(vline);
                _canvasElements.Add(vline);

                if (i % 2 == 0)
                {
                    var tb = new TextBlock
                    {
                        Text = (i / 10.0).ToString("0.0"),
                        FontSize = 9,
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
                var hline = new Line
                {
                    X1 = AxisMarginLeft, Y1 = y,
                    X2 = AxisMarginLeft + pw, Y2 = y,
                    Stroke = gridBrush, StrokeThickness = 1
                };
                _canvas.Children.Add(hline);
                _canvasElements.Add(hline);

                if (i % 2 == 0)
                {
                    var tb = new TextBlock
                    {
                        Text = (1.0 - i / 10.0).ToString("0.0"),
                        FontSize = 9,
                        Foreground = labelBrush
                    };
                    Canvas.SetLeft(tb, 3);
                    Canvas.SetTop(tb, y - 6);
                    _canvas.Children.Add(tb);
                    _canvasElements.Add(tb);
                }
            }

            var xLabel = new TextBlock
            {
                Text = "Input",
                FontSize = 9,
                Foreground = labelBrush
            };
            Canvas.SetLeft(xLabel, AxisMarginLeft + pw / 2 - 12);
            Canvas.SetTop(xLabel, AxisMarginTop + ph + 4);
            _canvas.Children.Add(xLabel);
            _canvasElements.Add(xLabel);

            var yLabel = new TextBlock
            {
                Text = "Output",
                FontSize = 9,
                Foreground = labelBrush
            };
            Canvas.SetLeft(yLabel, 3);
            Canvas.SetTop(yLabel, AxisMarginTop - 2);
            _canvas.Children.Add(yLabel);
            _canvasElements.Add(yLabel);
        }

        private void DrawAxes(double pw, double ph)
        {
            if (_canvas == null) return;

            var axisBrush = GetAxisBrush();

            var hAxis = new Line
            {
                X1 = AxisMarginLeft, Y1 = AxisMarginTop + ph,
                X2 = AxisMarginLeft + pw, Y2 = AxisMarginTop + ph,
                Stroke = axisBrush, StrokeThickness = 1.5
            };
            _canvas.Children.Add(hAxis);
            _canvasElements.Add(hAxis);

            var vAxis = new Line
            {
                X1 = AxisMarginLeft, Y1 = AxisMarginTop,
                X2 = AxisMarginLeft, Y2 = AxisMarginTop + ph,
                Stroke = axisBrush, StrokeThickness = 1.5
            };
            _canvas.Children.Add(vAxis);
            _canvasElements.Add(vAxis);
        }

        private void DrawDiagonalRef(double pw, double ph)
        {
            if (_canvas == null) return;

            var diagBrush = GetDiagonalBrush();

            var diagLine = new Line
            {
                X1 = AxisMarginLeft, Y1 = AxisMarginTop + ph,
                X2 = AxisMarginLeft + pw, Y2 = AxisMarginTop,
                Stroke = diagBrush,
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 4, 3 }
            };
            _canvas.Children.Add(diagLine);
            _canvasElements.Add(diagLine);
        }

        private void DrawCurve(double pw, double ph)
        {
            if (_curvePath == null) return;

            var config = CreateTempConfig();
            var geometry = new PathGeometry();
            var (sx, sy) = ToCanvas(0, ComputeCurve(0, config));
            var figure = new PathFigure { StartPoint = new System.Windows.Point(sx, sy) };

            int segs = 100;
            for (int i = 1; i <= segs; i++)
            {
                double t = (double)i / segs;
                double y = ComputeCurve(t, config);
                var (cx, cy) = ToCanvas(t, y);
                figure.Segments.Add(new LineSegment(new System.Windows.Point(cx, cy), true));
            }

            geometry.Figures.Add(figure);
            _curvePath.Data = geometry;
            _curvePath.Stroke = GetCurveBrush();
            _curvePath.StrokeThickness = 2.5;
            _curvePath.Fill = null;
        }

        private double ComputeCurve(double t, AppConfig config)
        {
            // 使用当前的 CurveType 属性，而不是 config 中的 AccelerationCurve
            return CurveType switch
            {
                AccelerationCurveType.Linear => AccelerationCurve.ApplyCurve(t, AccelerationCurveType.Linear, config),
                AccelerationCurveType.Exponential => AccelerationCurve.ApplyCurve(t, AccelerationCurveType.Exponential, config),
                AccelerationCurveType.Logarithmic => AccelerationCurve.ApplyCurve(t, AccelerationCurveType.Logarithmic, config),
                AccelerationCurveType.Sigmoid => AccelerationCurve.ApplyCurve(t, AccelerationCurveType.Sigmoid, config),
                AccelerationCurveType.Custom => CustomPoints != null && CustomPoints.Count >= 2
                    ? AccelerationCurve.ApplyCurveWithPoints(t, CustomPoints)
                    : AccelerationCurve.ApplyCurve(t, AccelerationCurveType.Linear, config),
                _ => t
            };
        }

        private AppConfig CreateTempConfig()
        {
            return new AppConfig
            {
                AccelerationExponent = Exponent,
                AccelerationLogBase = LogBase,
                SigmoidMidpoint = SigmoidMidpoint,
                SigmoidSteepness = SigmoidSteepness
            };
        }

        private System.Windows.Media.Brush GetGridBrush()
        {
            if (TryFindResource("Brush.Curve.Grid") is System.Windows.Media.Brush brush)
                return brush;
            return new SolidColorBrush(System.Windows.Media.Color.FromArgb(60, 180, 180, 180));
        }

        private System.Windows.Media.Brush GetAxisBrush()
        {
            if (TryFindResource("Brush.Curve.Axis") is System.Windows.Media.Brush brush)
                return brush;
            return new SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 100, 100));
        }

        private System.Windows.Media.Brush GetLabelBrush()
        {
            if (TryFindResource("Brush.Curve.Label") is System.Windows.Media.Brush brush)
                return brush;
            return new SolidColorBrush(System.Windows.Media.Color.FromRgb(80, 80, 80));
        }

        private System.Windows.Media.Brush GetDiagonalBrush()
        {
            if (TryFindResource("Brush.Curve.Diagonal") is System.Windows.Media.Brush brush)
                return brush;
            return new SolidColorBrush(System.Windows.Media.Color.FromArgb(150, 150, 150, 150));
        }

        private System.Windows.Media.Brush GetCurveBrush()
        {
            if (TryFindResource("Brush.Curve.Line") is System.Windows.Media.Brush brush)
                return brush;
            return new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 120, 212));
        }
    }
}
