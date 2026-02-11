using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace FlowWheel.Core
{
    public class SyncScrollManager
    {
        private struct TargetWindow
        {
            public IntPtr Handle;
            public NativeMethods.POINT Center;
        }

        private readonly List<TargetWindow> _targets = new List<TargetWindow>();
        private const uint WM_MOUSEHWHEEL = 0x020E;

        public void UpdateTargets(NativeMethods.POINT mousePos)
        {
            _targets.Clear();

            // 1. Multi-Monitor Logic
            IntPtr currentMonitor = NativeMethods.MonitorFromPoint(mousePos, NativeMethods.MONITOR_DEFAULTTONEAREST);

            NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, 
                (IntPtr hMonitor, IntPtr hdcMonitor, ref NativeMethods.RECT lprcMonitor, IntPtr dwData) =>
                {
                    if (hMonitor != currentMonitor)
                    {
                        int centerX = lprcMonitor.Left + (lprcMonitor.Right - lprcMonitor.Left) / 2;
                        int centerY = lprcMonitor.Top + (lprcMonitor.Bottom - lprcMonitor.Top) / 2;
                        
                        NativeMethods.POINT centerPt = new NativeMethods.POINT { x = centerX, y = centerY };
                        IntPtr hWnd = NativeMethods.WindowFromPoint(centerPt);
                        
                        if (hWnd != IntPtr.Zero)
                        {
                            _targets.Add(new TargetWindow { Handle = hWnd, Center = centerPt });
                        }
                    }
                    return true;
                }, IntPtr.Zero);

            // 2. Same-Monitor Side-by-Side Logic
            // If we are scrolling a window on the current monitor, check if there's an adjacent window.
            IntPtr currentWindow = NativeMethods.WindowFromPoint(mousePos);
            if (currentWindow != IntPtr.Zero)
            {
                NativeMethods.RECT winRect;
                if (NativeMethods.GetWindowRect(currentWindow, out winRect))
                {
                    int winWidth = winRect.Right - winRect.Left;
                    int winHeight = winRect.Bottom - winRect.Top;
                    int centerY = winRect.Top + winHeight / 2;

                    // Scan Left
                    NativeMethods.POINT leftProbe = new NativeMethods.POINT { x = winRect.Left - 50, y = centerY };
                    IntPtr leftWindow = NativeMethods.WindowFromPoint(leftProbe);
                    if (leftWindow != IntPtr.Zero && leftWindow != currentWindow)
                    {
                        _targets.Add(new TargetWindow { Handle = leftWindow, Center = leftProbe });
                    }

                    // Scan Right
                    NativeMethods.POINT rightProbe = new NativeMethods.POINT { x = winRect.Right + 50, y = centerY };
                    IntPtr rightWindow = NativeMethods.WindowFromPoint(rightProbe);
                    if (rightWindow != IntPtr.Zero && rightWindow != currentWindow)
                    {
                        _targets.Add(new TargetWindow { Handle = rightWindow, Center = rightProbe });
                    }
                }
            }
        }

        public void Scroll(int delta, bool isHorizontal)
        {
            if (_targets.Count == 0) return;

            uint msg = isHorizontal ? WM_MOUSEHWHEEL : NativeMethods.WM_MOUSEWHEEL;
            // The high-order word is the delta. The low-order word is key state (0 for now).
            // Note: delta can be negative, so we cast to short then to int then shift.
            // Actually, (delta << 16) works if delta is treated as 32-bit int, 
            // but in C#, (int) << 16 shifts bits. 
            // WM_MOUSEWHEEL expects high word to be signed short.
            IntPtr wParam = (IntPtr)((delta << 16) & 0xFFFF0000);

            foreach (var target in _targets)
            {
                // lParam is coordinates relative to screen (low: x, high: y)
                // Note: For multi-monitor, coordinates can be negative, so we need careful casting.
                // LoWord/HiWord macros usually take short.
                int x = (short)target.Center.x;
                int y = (short)target.Center.y;
                IntPtr lParam = (IntPtr)((y << 16) | (x & 0xFFFF));
                
                NativeMethods.PostMessage(target.Handle, msg, wParam, lParam);
            }
        }
    }
}
