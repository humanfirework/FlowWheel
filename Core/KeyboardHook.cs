using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FlowWheel.Core
{
    public class KeyboardHook : IDisposable
    {
        private IntPtr _hookId = IntPtr.Zero;
        private NativeMethods.LowLevelKeyboardProc _proc;

        public event EventHandler<KeyboardEventArgs>? KeyboardEvent;

        public KeyboardHook()
        {
            _proc = HookCallback;
            _hookId = SetHook(_proc);
        }

        public void Dispose()
        {
            UnhookWindowsHookEx(_hookId);
        }

        private IntPtr SetHook(NativeMethods.LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule? curModule = curProcess.MainModule)
            {
                if (curModule == null) return IntPtr.Zero;
                return NativeMethods.SetWindowsHookEx(NativeMethods.WH_KEYBOARD_LL, proc,
                    NativeMethods.GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = (int)wParam;
                NativeMethods.KBDLLHOOKSTRUCT? hookStruct = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);

                if (hookStruct != null)
                {
                    var args = new KeyboardEventArgs(msg, (int)hookStruct.Value.vkCode);
                    KeyboardEvent?.Invoke(this, args);

                    if (args.Handled)
                    {
                        return (IntPtr)1;
                    }
                }
            }
            return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);
    }

    public class KeyboardEventArgs : EventArgs
    {
        public int Message { get; }
        public int VkCode { get; }
        public bool Handled { get; set; }

        public KeyboardEventArgs(int msg, int vkCode)
        {
            Message = msg;
            VkCode = vkCode;
            Handled = false;
        }
    }
}
