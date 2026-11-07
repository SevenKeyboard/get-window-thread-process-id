# get-window-thread-process-id

Gets the process ID that owns a window on Windows 10+ using C# (.NET 10).

```csharp
using SevenKeyboard;

if (GetWindowThreadProcessId.TryGet(window, out uint processId))
    Console.WriteLine(processId);
```

The output is the process ID, not the native function's thread ID.
On failure, `TryGet` returns false and `processId` is zero.

Version 2.0.0 separates success from the output value. Supports x64, x86 and NativeAOT.
