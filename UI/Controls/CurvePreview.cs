using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using FlowWheel.Core;

namespace FlowWheel.UI.Controls
{
    public class CurvePreview : CurveControlBase
    {
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

        public CurvePreview() { }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _canvas = GetTemplateChild("PART_Canvas") as Canvas;
            _curvePath = GetTemplateChild("PART_CurvePath") as Path;
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

        protected override void Redraw()
        {
            if (_canvas == null || _curvePath == null || !_isLoaded) return;

            var (pw, ph) = GetPlotSize();
            if (pw <= 0 || ph <= 0) return;

            ClearCanvasElements();
            DrawGrid();
            DrawCurve();
            DrawAxisLabels(pw, ph);
        }

        private void DrawCurve()
        {
            if (_curvePath == null || _canvas == null) return;

            var config = CreateTempConfig();
            var accent = GetAccentColor();

            // Area fill under curve (inserted at bottom so curve line stays on top)
            var areaPath = new Path
            {
                Data = BuildAreaGeometry(t => ComputeCurve(t, config), 80),
                Fill = GetCurveAreaBrush(accent),
                Stroke = null,
                IsHitTestVisible = false,
                Opacity = 0.75
            };
            _canvas.Children.Insert(0, areaPath);
            _canvasElements.Add(areaPath);

            // Subtle glow path behind main curve
            var glowPath = new Path
            {
                Data = BuildCurveGeometry(t => ComputeCurve(t, config), 80),
                Stroke = GetCurveGlowBrush(accent),
                StrokeThickness = 4,
                StrokeLineJoin = PenLineJoin.Round,
                Fill = null,
                IsHitTestVisible = false,
                Opacity = 0.25,
                Effect = new System.Windows.Media.Effects.BlurEffect { Radius = 3 }
            };
            _canvas.Children.Insert(1, glowPath);
            _canvasElements.Add(glowPath);

            _curvePath.Data = BuildCurveGeometry(t => ComputeCurve(t, config), 80);
            _curvePath.Stroke = GetCurveBrush();
            _curvePath.StrokeThickness = 3;
            _curvePath.Fill = null;
            _curvePath.StrokeLineJoin = PenLineJoin.Round;
        }

        private double ComputeCurve(double t, AppConfig config)
        {
            return CurveType switch
            {
                AccelerationCurveType.Linear => global::FlowWheel.Core.AccelerationCurve.ApplyCurve(t, AccelerationCurveType.Linear, config),
                AccelerationCurveType.Exponential => global::FlowWheel.Core.AccelerationCurve.ApplyCurve(t, AccelerationCurveType.Exponential, config),
                AccelerationCurveType.Logarithmic => global::FlowWheel.Core.AccelerationCurve.ApplyCurve(t, AccelerationCurveType.Logarithmic, config),
                AccelerationCurveType.Sigmoid => global::FlowWheel.Core.AccelerationCurve.ApplyCurve(t, AccelerationCurveType.Sigmoid, config),
                AccelerationCurveType.Custom => CustomPoints != null && CustomPoints.Count >= 2
                    ? global::FlowWheel.Core.AccelerationCurve.ApplyCurveWithPoints(t, CustomPoints)
                    : global::FlowWheel.Core.AccelerationCurve.ApplyCurve(t, AccelerationCurveType.Linear, config),
                _ => t
            };
        }

        private void DrawAxisLabels(double pw, double ph)
        {
            if (_canvas == null) return;
            var labelBrush = GetLabelBrush();

            var xLabel = new TextBlock
            {
                Text = "Input",
                FontSize = 9,
                Foreground = labelBrush,
                Opacity = 0.7
            };
            Canvas.SetLeft(xLabel, AxisMarginLeft + pw / 2 - 12);
            Canvas.SetTop(xLabel, AxisMarginTop + ph + 4);
            AddCanvasElement(xLabel);

            var yLabel = new TextBlock
            {
                Text = "Output",
                FontSize = 9,
                Foreground = labelBrush,
                Opacity = 0.7
            };
            Canvas.SetLeft(yLabel, 3);
            Canvas.SetTop(yLabel, AxisMarginTop - 2);
            AddCanvasElement(yLabel);
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

        private System.Windows.Media.Brush GetCurveBrush()
        {
            if (TryFindResource("Brush.Curve.Line") is System.Windows.Media.Brush brush)
                return brush;
            return new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 120, 212));
        }
    }
}
