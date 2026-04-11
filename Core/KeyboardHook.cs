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
            if (_hookId == IntPtr.Zero)
            {
                // Fallback or just log
                System.Diagnostics.Debug.WriteLine("Failed to install keyboard hook");
            }
        }

        public void Dispose()
        {
            if (_hookId != IntPtr.Zero)
                UnhookWindowsHookEx(_hookId);
        }

        private IntPtr SetHook(NativeMethods.LowLevelKeyboardProc proc)
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
                
                return NativeMethods.SetWindowsHookEx(NativeMethods.WH_KEYBOARD_LL, proc, moduleHandle, 0);
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
