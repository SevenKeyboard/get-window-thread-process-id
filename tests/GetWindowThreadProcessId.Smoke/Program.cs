using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using SevenKeyboard;

[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.System32)]

internal static partial class Program
{
    private static int checks;

    private static int Main()
    {
        try
        {
            Run();
            Console.WriteLine($"PASS: {checks} checks, {RuntimeInformation.ProcessArchitecture}.");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }

    private static void Run()
    {
        using ManualResetEventSlim ready = new(false);
        using ManualResetEventSlim release = new(false);
        nint window = 0;
        uint ownerThreadId = 0;
        uint ownerProcessId = 0;
        bool ownerSucceeded = false;
        bool hidden = false;
        bool inactive = false;
        Exception? workerError = null;

        Thread worker = new(() =>
        {
            try
            {
                ownerThreadId = GetCurrentThreadId();
                // WS_EX_NOACTIVATE, without WS_VISIBLE: only this owned window is created.
                window = CreateWindowExW(0x08000000, "STATIC", "", 0,
                    0, 0, 0, 0, 0, 0, 0, 0);
                if (window == 0)
                    throw new Win32Exception(Marshal.GetLastPInvokeError());

                hidden = IsWindowVisible(window) == 0;
                inactive = GetActiveWindow() != window && GetFocus() != window;
                ownerSucceeded = GetWindowThreadProcessId.TryGet(window, out ownerProcessId);
                ready.Set();
                release.Wait();
            }
            catch (Exception error)
            {
                workerError = error;
                ready.Set();
            }
            finally
            {
                // DestroyWindow must run on the thread that created the window.
                if (window != 0 && DestroyWindow(window) == 0)
                    workerError = new Win32Exception(Marshal.GetLastPInvokeError());
            }
        }) { IsBackground = true };
        worker.Start();

        try
        {
            Check(ready.Wait(TimeSpan.FromSeconds(10)), "Window owner became ready.");
            if (workerError != null)
                throw new InvalidOperationException("Window setup failed.", workerError);

            uint expectedProcessId = (uint)Environment.ProcessId;
            Check(hidden, "Window remains hidden.");
            Check(inactive, "Window is inactive and unfocused.");
            Check(ownerThreadId != GetCurrentThreadId(), "Window belongs to another thread.");
            Check(ownerThreadId != expectedProcessId, "Thread ID differs from process ID.");
            Check(ownerSucceeded && ownerProcessId == expectedProcessId, "Owner thread receives its PID.");

            Check(GetWindowThreadProcessId.TryGet(window, out uint processId),
                "Query a valid window from another thread.");
            Check(processId == expectedProcessId, "Output is the owning process ID.");
            Check(processId != ownerThreadId, "Output is not the native return value (TID).");
            CheckFailure(0);
            CheckFailure(-1);
            Check(GetWindowThreadProcessId.TryGet(window, out processId) && processId == expectedProcessId,
                "Valid query still succeeds after failures.");
            Console.WriteLine($"PID={processId}, owner TID={ownerThreadId}, caller TID={GetCurrentThreadId()}.");
        }
        finally
        {
            release.Set();
            Check(worker.Join(TimeSpan.FromSeconds(10)), "Owner destroys its window and exits.");
        }

        if (workerError != null)
            throw new InvalidOperationException("Window cleanup failed.", workerError);
        CheckFailure(window);
    }

    private static void CheckFailure(nint window)
    {
        uint processId = uint.MaxValue;
        Check(!GetWindowThreadProcessId.TryGet(window, out processId), $"Failure for HWND={window}.");
        Check(processId == 0, "Failure clears the output.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
        checks++;
    }

    [LibraryImport("kernel32.dll", EntryPoint = "GetCurrentThreadId")]
    private static partial uint GetCurrentThreadId();

    [LibraryImport("user32.dll", EntryPoint = "CreateWindowExW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    private static partial nint CreateWindowExW(uint extendedStyle, string className, string title, uint style,
        int x, int y, int width, int height, nint parent, nint menu, nint instance, nint parameter);

    [LibraryImport("user32.dll", EntryPoint = "DestroyWindow", SetLastError = true)]
    private static partial int DestroyWindow(nint window);

    [LibraryImport("user32.dll", EntryPoint = "IsWindowVisible")]
    private static partial int IsWindowVisible(nint window);

    [LibraryImport("user32.dll", EntryPoint = "GetActiveWindow")]
    private static partial nint GetActiveWindow();

    [LibraryImport("user32.dll", EntryPoint = "GetFocus")]
    private static partial nint GetFocus();
}
