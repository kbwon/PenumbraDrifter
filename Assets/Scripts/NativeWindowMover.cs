using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

public static class NativeWindowMover
{
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    [DllImport("user32.dll", SetLastError = true)]
    static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int X,
        int Y,
        int cx,
        int cy,
        uint uFlags
    );

    [DllImport("user32.dll")]
    static extern int GetSystemMetrics(int nIndex);

    const int SM_CXSCREEN = 0;
    const int SM_CYSCREEN = 1;

    const uint SWP_NOSIZE = 0x0001;
    const uint SWP_NOZORDER = 0x0004;
    const uint SWP_NOACTIVATE = 0x0010;
    const uint SWP_NOOWNERZORDER = 0x0200;

    static IntPtr cachedHandle;

    public static bool TryMove(int x, int y)
    {
        IntPtr handle = GetWindowHandle();

        if (handle == IntPtr.Zero)
            return false;

        return SetWindowPos(
            handle,
            IntPtr.Zero,
            x,
            y,
            0,
            0,
            SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_NOOWNERZORDER
        );
    }

    public static bool TryGetPrimaryScreenSize(out int width, out int height)
    {
        width = GetSystemMetrics(SM_CXSCREEN);
        height = GetSystemMetrics(SM_CYSCREEN);

        return width > 0 && height > 0;
    }

    static IntPtr GetWindowHandle()
    {
        if (cachedHandle != IntPtr.Zero)
            return cachedHandle;

        Process process = Process.GetCurrentProcess();
        process.Refresh();

        cachedHandle = process.MainWindowHandle;
        return cachedHandle;
    }
#else
    public static bool TryMove(int x, int y)
    {
        return false;
    }

    public static bool TryGetPrimaryScreenSize(out int width, out int height)
    {
        width = 0;
        height = 0;
        return false;
    }
#endif
}