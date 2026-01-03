using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TabletLib.Utilities;

static class Blocker
{
    delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);
    
    private static IntPtr _hook = IntPtr.Zero;
    private static HookProc _proc = HookCallback;
    
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;

    private enum Keys : int
    {
        LWin = 0x5B,
        RWin = 0x5C
    }
    
    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
        {
            int vkCode = Marshal.ReadInt32(lParam);
            if (vkCode == (int)Keys.LWin || vkCode == (int)Keys.RWin)
            {
                return (IntPtr)1;
            }
        }
        return CallNextHookEx(_hook, nCode, wParam, lParam);
    }
    public static void DisableWinKey()
    {
        _hook = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, IntPtr.Zero, 0);

    }

    public static async Task DisableWinKey(int seconds)
    {
        DisableWinKey();
        await Task.Delay(TimeSpan.FromSeconds(seconds));
        EnableWinKey();
    }
    public static void EnableWinKey()
    {
        if (_hook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hook);
            _hook = IntPtr.Zero;
        }
    }
    
    #region Dll Imports
    [DllImport("user32.dll")] 
    public static extern int ShowCursor(bool bShow);
    
    [DllImport("user32.dll")] 
    static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll")] 
    static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")] 
    static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
    #endregion
    }