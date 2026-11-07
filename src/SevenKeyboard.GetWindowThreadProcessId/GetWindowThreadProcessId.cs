//==============================================================
// getWindowThreadProcessId — Get the process ID for a given window handle
//
// GitHub: https://github.com/SevenKeyboard/get-window-thread-process-id
// Author: SevenKeyboard Ltd. (2026)
// License: The Unlicense
//==============================================================

using System.Runtime.InteropServices;

namespace SevenKeyboard;

public static partial class GetWindowThreadProcessId
{
    public static bool TryGet(nint window, out uint processId)
    {
        processId = 0;
        // The native return value is the thread ID; the output parameter is the PID.
        if (GetWindowThreadProcessIdNative(window, out uint nativeProcessId) == 0)
            return false;

        processId = nativeProcessId;
        return true;
    }

    [LibraryImport("user32.dll", EntryPoint = "GetWindowThreadProcessId")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial uint GetWindowThreadProcessIdNative(nint window, out uint processId);
}
