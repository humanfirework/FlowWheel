using System;
using System.Runtime.InteropServices;

namespace FlowWheel.Core
{
    public class MouseHook : BaseHook
    {
        private readonly NativeMethods.LowLevelMouseProc _proc;

        public event EventHandler<MouseEventArgs>? MouseEvent;

        protected override IntPtr SetHook()
        {
            IntPtr moduleHandle = GetModuleHandle();
            return NativeMethods.SetWindowsHookEx(NativeMethods.WH_MOUSE_LL, _proc, moduleHandle, 0);
        }

        public MouseHook()
        {
            _proc = HookCallback;
            InitializeHook(); // Must be called AFTER _proc is initialized
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode < 0)
            {
                return CallNextHook(nCode, wParam, lParam);
            }

            int msg = (int)wParam;
            
            if (msg != NativeMethods.WM_MOUSEMOVE && 
                msg != NativeMethods.WM_LBUTTONDOWN &&
                msg != NativeMethods.WM_LBUTTONUP &&
                msg != NativeMethods.WM_RBUTTONDOWN &&
                msg != NativeMethods.WM_RBUTTONUP &&
                msg != NativeMethods.WM_MBUTTONDOWN &&
                msg != NativeMethods.WM_MBUTTONUP &&
                msg != NativeMethods.WM_XBUTTONDOWN &&
                msg != NativeMethods.WM_XBUTTONUP &&
                msg != NativeMethods.WM_MOUSEWHEEL)
            {
                return CallNextHook(nCode, wParam, lParam);
            }

            NativeMethods.MSLLHOOKSTRUCT? hookStruct = Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam);

            if (hookStruct == null || hookStruct.Value.dwExtraInfo == MouseHook.INJECTED_SIGNATURE)
            {
                return CallNextHook(nCode, wParam, lParam);
            }

            int mouseData = 0;
            if (msg == NativeMethods.WM_MOUSEWHEEL)
            {
                mouseData = (short)((hookStruct.Value.mouseData >> 16) & 0xFFFF);
            }
            else if (msg == NativeMethods.WM_XBUTTONDOWN || msg == NativeMethods.WM_XBUTTONUP)
            {
                mouseData = (int)(hookStruct.Value.mouseData >> 16);
            }

            var args = new MouseEventArgs(msg, hookStruct.Value.pt, mouseData);
            MouseEvent?.Invoke(this, args);

            if (args.Handled)
            {
                return (IntPtr)1;
            }
            
            return CallNextHook(nCode, wParam, lParam);
        }

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
