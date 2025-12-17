#Requires AutoHotkey v1.1.0+
;==============================================================
; getWindowThreadProcessId — Get the process ID for a given window handle
;
; GitHub: https://github.com/SevenKeyboard/get-window-thread-process-id
; Author: SevenKeyboard Ltd. (2025)
; License: The Unlicense
;==============================================================
getWindowThreadProcessId(hWnd)    {
    varSetCapacity(lpdwProcessId,4,0)
    return dllCall("User32.dll\GetWindowThreadProcessId", "Ptr",hwnd, "Ptr",&lpdwProcessId, "Int")
        ?numGet(lpdwProcessId,"UInt")
        :0
}