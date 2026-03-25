using System;
using System.Collections.Generic;
using System.Linq;

namespace FlowWheel.Core
{
    public static class AccelerationCurve
    {
        public static double ApplyCurve(double normalizedInput, AccelerationCurveType curveType, AppConfig config)
        {
            normalizedInput = Math.Clamp(normalizedInput, 0, 1);
            
            double result = curveType switch
            {
                AccelerationCurveType.Linear => ApplyLinear(normalizedInput),
                AccelerationCurveType.Exponential => ApplyExponential(normalizedInput, config.AccelerationExponent),
                AccelerationCurveType.Logarithmic => ApplyLogarithmic(normalizedInput, config.AccelerationLogBase),
                AccelerationCurveType.Sigmoid => ApplySigmoid(normalizedInput, config.SigmoidMidpoint, config.SigmoidSteepness),
                AccelerationCurveType.Custom => ApplyCustom(normalizedInput, config.CustomCurvePoints),
                _ => normalizedInput
            };
            
            return Math.Clamp(result, 0, 1);
        }
        
        public static double ApplyCurveWithPoints(double normalizedInput, List<CustomCurvePoint> points)
        {
            normalizedInput = Math.Clamp(normalizedInput, 0, 1);
            double result = ApplyCustom(normalizedInput, points);
            return Math.Clamp(result, 0, 1);
        }
        
        private static double ApplyLinear(double x)
        {
            return x;
        }
        
        private static double ApplyExponential(double x, double exponent)
        {
            if (exponent <= 0) return x;
            return Math.Pow(x, exponent);
        }
        
        private static double ApplyLogarithmic(double x, double logBase)
        {
            if (logBase <= 1) return x;
            if (x <= 0) return 0;
            return Math.Log(1 + (logBase - 1) * x, logBase);
        }
        
        private static double ApplySigmoid(double x, double midpoint, double steepness)
        {
            if (steepness <= 0) return x;
            double adjustedX = x - midpoint;
            double sigmoid = 1.0 / (1.0 + Math.Exp(-steepness * adjustedX * 10));
            double minSigmoid = 1.0 / (1.0 + Math.Exp(steepness * midpoint * 10));
            double maxSigmoid = 1.0 / (1.0 + Math.Exp(-steepness * (1 - midpoint) * 10));
            return (sigmoid - minSigmoid) / (maxSigmoid - minSigmoid);
        }
        
        private static double ApplyCustom(double x, List<CustomCurvePoint> points)
        {
            if (points == null || points.Count < 2)
                return x;
            
            var sortedPoints = points.OrderBy(p => p.X).ToList();
            
            if (x <= sortedPoints[0].X)
                return sortedPoints[0].Y;
            if (x >= sortedPoints[^1].X)
                return sortedPoints[^1].Y;
            
            for (int i = 0; i < sortedPoints.Count - 1; i++)
            {
                if (x >= sortedPoints[i].X && x <= sortedPoints[i + 1].X)
                {
                    double t = (x - sortedPoints[i].X) / (sortedPoints[i + 1].X - sortedPoints[i].X);
                    t = SmoothStep(t);
                    return sortedPoints[i].Y + t * (sortedPoints[i + 1].Y - sortedPoints[i].Y);
                }
            }
            
            return x;
        }
        
        private static double SmoothStep(double t)
        {
            t = Math.Clamp(t, 0, 1);
            return t * t * (3 - 2 * t);
        }
        
        public static List<CustomCurvePoint> GenerateDefaultCurve(AccelerationCurveType curveType, AppConfig config)
        {
            var points = new List<CustomCurvePoint>();
            int numPoints = 11;
            
            for (int i = 0; i < numPoints; i++)
            {
                double x = (double)i / (numPoints - 1);
                double y = ApplyCurve(x, curveType, config);
                points.Add(new CustomCurvePoint(x, y));
            }
            
            return points;
        }
    }
}
