#Requires AutoHotkey v2.0
#SingleInstance Force
#Include ../AHK v2/Lib/AutoHotInterception.ahk
global received := false
try {
    ahi := AutoHotInterception()
    devices := ahi.GetDeviceList()
    if devices.Count != 4
        throw Error("Expected four synthetic devices")
    if ahi.GetKeyboardId(0x1234,0x1001) != 1 || ahi.GetMouseId(0x1234,0x1002) != 12
        throw Error("Device lookup failed")
    ahi.SubscribeKeyboard(1,false,OnKey)
    DllCall("InterceptorBridge\bridge_test_push_key","UInt",101,"UInt",30,"UInt",0,"Cdecl")
    deadline := A_TickCount + 5000
    while !received && A_TickCount < deadline
        Sleep 10
    if !received
        throw Error("No keyboard callback: " ahi.GetLastError())
    ahi.SetState(false)
    ahi.Instance.Dispose()
    FileAppend("PASS`n", A_Args[1])
    ExitApp 0
} catch as err {
    FileAppend("FAIL: " err.Message "`n", A_Args[1])
    ExitApp 1
}
OnKey(code,state) {
    global received
    received := code = 30 && state = 1
}
