using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FlowWheel.Core
{
    public class MouseHook : IDisposable
    {
        private IntPtr _hookId = IntPtr.Zero;
        private NativeMethods.LowLevelMouseProc _proc;

        public event EventHandler<MouseEventArgs>? MouseEvent;

        public MouseHook()
        {
            _proc = HookCallback;
            _hookId = SetHook(_proc);
            if (_hookId == IntPtr.Zero)
            {
                System.Diagnostics.Debug.WriteLine("Failed to install mouse hook");
            }
        }

        public void Dispose()
        {
            if (_hookId != IntPtr.Zero)
                UnhookWindowsHookEx(_hookId);
        }

        private IntPtr SetHook(NativeMethods.LowLevelMouseProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule? curModule = curProcess.MainModule)
            {
                IntPtr moduleHandle = IntPtr.Zero;
                if (curModule != null)
                {
                    moduleHandle = NativeMethods.GetModuleHandle(curModule.ModuleName);
                }
                
                if (moduleHandle == IntPtr.Zero)
                {
                    moduleHandle = NativeMethods.GetModuleHandle(null);
                }

                return NativeMethods.SetWindowsHookEx(NativeMethods.WH_MOUSE_LL, proc, moduleHandle, 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            // Fast path: nCode < 0 means we must pass to next hook without processing
            if (nCode < 0)
            {
                return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
            }

            int msg = (int)wParam;
            
            // Fast path: only process specific messages we care about
            // WM_MOUSEMOVE = 0x0200, WM_LBUTTONDOWN = 0x0201, WM_LBUTTONUP = 0x0202
            // WM_RBUTTONDOWN = 0x0204, WM_RBUTTONUP = 0x0205
            // WM_MBUTTONDOWN = 0x0207, WM_MBUTTONUP = 0x0208
            // WM_XBUTTONDOWN = 0x020B, WM_XBUTTONUP = 0x020C
            // WM_MOUSEWHEEL = 0x020A
            if (msg != NativeMethods.WM_MOUSEMOVE && 
                msg != NativeMethods.WM_LBUTTONDOWN &&
                msg != NativeMethods.WM_RBUTTONDOWN &&
                msg != NativeMethods.WM_MBUTTONDOWN &&
                msg != NativeMethods.WM_MBUTTONUP &&
                msg != NativeMethods.WM_XBUTTONDOWN &&
                msg != NativeMethods.WM_XBUTTONUP &&
                msg != NativeMethods.WM_MOUSEWHEEL)
            {
                return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
            }

            NativeMethods.MSLLHOOKSTRUCT? hookStruct = Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam);

            // Ignore injected events or null struct
            if (hookStruct == null || hookStruct.Value.dwExtraInfo == MouseHook.INJECTED_SIGNATURE)
            {
                return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
            }

            int mouseData = 0;
            if (msg == NativeMethods.WM_MOUSEWHEEL)
            {
                mouseData = (short)((hookStruct.Value.mouseData >> 16) & 0xFFFF);
            }

            var args = new MouseEventArgs(msg, hookStruct.Value.pt, mouseData);
            MouseEvent?.Invoke(this, args);

            if (args.Handled)
            {
                return (IntPtr)1; // Block the event
            }
            
            return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        public static readonly IntPtr INJECTED_SIGNATURE = (IntPtr)0xFF55AA;
    }

    public class MouseEventArgs : EventArgs
    {
        public int Message { get; }
        public NativeMethods.POINT Point { get; }
        public int MouseData { get; }
        public bool Handled { get; set; }

        public MouseEventArgs(int msg, NativeMethods.POINT point, int data)
        {
            Message = msg;
            Point = point;
            MouseData = data;
            Handled = false;
        }
    }
}
