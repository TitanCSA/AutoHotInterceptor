#Requires AutoHotkey v1.1.33+
#SingleInstance Force
#Include %A_ScriptDir%\..\AHK v1\Lib\AutoHotInterception.ahk
received := false
try {
    ahi := new AutoHotInterception()
    devices := ahi.GetDeviceList()
    if (devices.Count() != 4)
        throw Exception("Expected four synthetic devices")
    if (ahi.GetKeyboardId(0x1234,0x1001) != 1 || ahi.GetMouseId(0x1234,0x1002) != 12)
        throw Exception("Device lookup failed")
    ahi.SubscribeKeyboard(1,false,Func("OnKey"))
    DllCall("InterceptorBridge\bridge_test_push_key","UInt",101,"UInt",30,"UInt",0,"Cdecl")
    deadline := A_TickCount + 5000
    while (!received && A_TickCount < deadline)
        Sleep 10
    if (!received)
        throw Exception("No keyboard callback: " ahi.GetLastError())
    ahi.SetState(false)
    ahi.Instance.Dispose()
    FileAppend, PASS`n, % A_Args[1]
    ExitApp 0
} catch err {
    FileAppend, % "FAIL: " err.Message "`n", % A_Args[1]
    ExitApp 1
}
OnKey(code,state) {
    global received
    received := (code = 30 && state = 1)
}
