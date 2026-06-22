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
        private const int ProbeDistance = 100;

        public void UpdateTargets(NativeMethods.POINT mousePos)
        {
            _targets.Clear();

            IntPtr currentMonitor = NativeMethods.MonitorFromPoint(mousePos, NativeMethods.MONITOR_DEFAULTTONEAREST);
            IntPtr currentWindow = NativeMethods.WindowFromPoint(mousePos);
            IntPtr currentRoot = GetRootWindow(currentWindow);

            // 1. Multi-Monitor Logic: enumerate other monitors
            NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
                (IntPtr hMonitor, IntPtr hdcMonitor, ref NativeMethods.RECT lprcMonitor, IntPtr dwData) =>
                {
                    if (hMonitor != currentMonitor)
                    {
                        int centerX = lprcMonitor.Left + (lprcMonitor.Right - lprcMonitor.Left) / 2;
                        int centerY = lprcMonitor.Top + (lprcMonitor.Bottom - lprcMonitor.Top) / 2;

                        NativeMethods.POINT centerPt = new NativeMethods.POINT { x = centerX, y = centerY };
                        IntPtr hWnd = NativeMethods.WindowFromPoint(centerPt);
                        TryAddTarget(hWnd, centerPt, currentRoot);
                    }
                    return true;
                }, IntPtr.Zero);

            // 2. Same-Monitor Side-by-Side Logic
            if (currentRoot != IntPtr.Zero && NativeMethods.GetWindowRect(currentRoot, out NativeMethods.RECT winRect))
            {
                int winWidth = winRect.Right - winRect.Left;
                int winHeight = winRect.Bottom - winRect.Top;
                int centerX = winRect.Left + winWidth / 2;
                int centerY = winRect.Top + winHeight / 2;

                // Scan Left
                TryAddSideTarget(winRect.Left - ProbeDistance, centerY, currentRoot);
                // Scan Right
                TryAddSideTarget(winRect.Right + ProbeDistance, centerY, currentRoot);
                // Scan Top
                TryAddSideTarget(centerX, winRect.Top - ProbeDistance, currentRoot);
                // Scan Bottom
                TryAddSideTarget(centerX, winRect.Bottom + ProbeDistance, currentRoot);
            }
        }

        private void TryAddSideTarget(int probeX, int probeY, IntPtr currentRoot)
        {
            NativeMethods.POINT probe = new NativeMethods.POINT { x = probeX, y = probeY };
            IntPtr hWnd = NativeMethods.WindowFromPoint(probe);
            TryAddTarget(hWnd, probe, currentRoot);
        }

        private void TryAddTarget(IntPtr hWnd, NativeMethods.POINT center, IntPtr currentRoot)
        {
            IntPtr root = GetRootWindow(hWnd);
            if (root == IntPtr.Zero) return;
            if (root == currentRoot) return;
            if (!NativeMethods.IsWindowVisible(root)) return;
            if (IsShellWindow(root)) return;

            foreach (var existing in _targets)
            {
                if (existing.Handle == root) return;
            }

            _targets.Add(new TargetWindow { Handle = root, Center = center });
        }

        private static IntPtr GetRootWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return IntPtr.Zero;
            IntPtr root = NativeMethods.GetAncestor(hWnd, NativeMethods.GA_ROOT);
            return root != IntPtr.Zero ? root : hWnd;
        }

        private static bool IsShellWindow(IntPtr hWnd)
        {
            // Filter common shell/utility windows that should never receive synthetic scroll
            IntPtr owner = NativeMethods.GetWindow(hWnd, NativeMethods.GW_OWNER);
            if (owner != IntPtr.Zero) return true;

            IntPtr exStyle = (IntPtr)NativeMethods.GetWindowLong(hWnd, NativeMethods.GWL_EXSTYLE);
            uint ex = (uint)exStyle.ToInt64();
            if ((ex & NativeMethods.WS_EX_TOOLWINDOW) == NativeMethods.WS_EX_TOOLWINDOW) return true;
            if ((ex & NativeMethods.WS_EX_NOACTIVATE) == NativeMethods.WS_EX_NOACTIVATE) return true;

            return false;
        }

        public void Scroll(int delta, bool isHorizontal)
        {
            if (_targets.Count == 0) return;

            uint msg = isHorizontal ? WM_MOUSEHWHEEL : NativeMethods.WM_MOUSEWHEEL;
            // High-order word is the signed wheel delta
            IntPtr wParam = (IntPtr)(((short)delta) << 16);

            foreach (var target in _targets)
            {
                // lParam coordinates are 16-bit signed screen coordinates
                int x = (short)target.Center.x;
                int y = (short)target.Center.y;
                IntPtr lParam = (IntPtr)(((short)y << 16) | (x & 0xFFFF));

                NativeMethods.PostMessage(target.Handle, msg, wParam, lParam);
            }
        }
    }
}
