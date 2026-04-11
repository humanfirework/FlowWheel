using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace FlowWheel.Core
{
    /// <summary>
    /// Helper class for smooth color transition animations
    /// </summary>
    public static class AnimationHelper
    {
        /// <summary>
        /// Begins a color animation on a SolidColorBrush in the specified resource dictionary
        /// </summary>
        /// <param name="resourceDict">The resource dictionary containing the target brush</param>
        /// <param name="key">The key of the brush in the resource dictionary</param>
        /// <param name="toColor">The target color</param>
        /// <param name="durationMs">Animation duration in milliseconds</param>
        public static void ResBrushBeginAnimation(ResourceDictionary resourceDict, string key, System.Windows.Media.Color toColor, int durationMs = 600)
        {
            if (resourceDict[key] is SolidColorBrush brush)
            {
                bool isNewBrush = false;
                if (brush.IsFrozen)
                {
                    brush = new SolidColorBrush(brush.Color);
                    isNewBrush = true;
                }
                ColorAnimation colorAnimation = new ColorAnimation
                {
                    From = brush.Color,
                    To = toColor,
                    Duration = TimeSpan.FromMilliseconds(durationMs),
                    EasingFunction = new PowerEase() { EasingMode = EasingMode.EaseInOut },
                };
                brush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
                if (isNewBrush)
                    resourceDict[key] = brush;
            }
        }

        /// <summary>
        /// Begins a color animation on a SolidColorBrush in the application-level resource dictionary
        /// </summary>
        /// <param name="key">The key of the brush in the resource dictionary</param>
        /// <param name="toColor">The target color</param>
        /// <param name="durationMs">Animation duration in milliseconds</param>
        public static void ResBrushBeginAnimation(string key, System.Windows.Media.Color toColor, int durationMs = 600)
        {
            ResBrushBeginAnimation(System.Windows.Application.Current.Resources, key, toColor, durationMs);
        }
    }
}
