using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Resources;
using System.Text;
using System.Windows.Forms;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
//using Un4seen.Bass;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace YL_Player
{
    public class MW
    {
        public MainWindow X { set; get; }
    }

    public static class GlobalHook
    {
        public enum KeyState
        {
            KeyDown,
            KeyUp
        }

        public static KeyState GetState(Keys Key)
        {
            if (_KeysDown.Contains(Key))
                return KeyState.KeyDown;
            else
                return KeyState.KeyUp;
        }

        public delegate void KeyboardEvent(Keys Key);
        public delegate void KeyboardStateEvent(Keys Key, GlobalHook.KeyState State);
        public static event KeyboardEvent KeyDown;
        public static event KeyboardEvent KeyUp;
        public static event KeyboardStateEvent KeyEvent;
        private static List<Keys> _KeysDown = new List<Keys>();
        public static List<Keys> KeysDown
        {
            get
            {
                return _KeysDown;
            }
        }
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        public static void SetHook()
        {
            _hookID = Hook(_proc);
        }

        public static void UnHook()
        {
            UnhookWindowsHookEx(_hookID);
        }

        private static IntPtr Hook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(
            int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(
            int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                if (wParam == (IntPtr)WM_KEYDOWN)
                {
                    if (!_KeysDown.Contains((Keys)vkCode))
                    {
                        if (KeyDown != null)
                            KeyDown((Keys)vkCode);

                        if (KeyEvent != null)
                            KeyEvent((Keys)vkCode, KeyState.KeyDown);

                        _KeysDown.Add((Keys)vkCode);
                    }
                }
                if (wParam == (IntPtr)WM_KEYUP)
                {
                    if (KeyUp != null)
                        KeyUp((Keys)vkCode);

                    if (KeyEvent != null)
                        KeyEvent((Keys)vkCode, KeyState.KeyUp);

                    _KeysDown.Remove((Keys)vkCode);
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }

    public partial class App : System.Windows.Application
    {
        public static MW mw;
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            GlobalHook.SetHook();
            GlobalHook.KeyUp += GlobalHook_KeyUp;

            // Create the startup window
            mw = new MW();
            mw.X = new MainWindow();
            mw.X.Show();// Show the window
        }

        static void GlobalHook_KeyUp(Keys Key)
        {
            if (Key == Keys.MediaPlayPause)
                mw.X.PlayPause();
            if (Key == Keys.MediaNextTrack)
            {
                mw.X.Next();
            }
            if (Key == Keys.MediaPreviousTrack)
            {
                mw.X.Prev();
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            GlobalHook.UnHook();
        }
    }
}
