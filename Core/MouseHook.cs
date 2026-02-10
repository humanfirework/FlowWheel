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
        }

        public void Dispose()
        {
            UnhookWindowsHookEx(_hookId);
        }

        private IntPtr SetHook(NativeMethods.LowLevelMouseProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule? curModule = curProcess.MainModule)
            {
                if (curModule == null) return IntPtr.Zero; // Should not happen for main module
                return NativeMethods.SetWindowsHookEx(NativeMethods.WH_MOUSE_LL, proc,
                    NativeMethods.GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = (int)wParam;
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
