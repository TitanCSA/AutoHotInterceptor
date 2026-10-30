#Requires AutoHotkey v1.1.33+
#SingleInstance Force
#Persistent
#Include %A_ScriptDir%\Lib\AutoHotInterception.ahk
AHI := new AutoHotInterception()
keyboard := 0
for id, device in AHI.GetDeviceList() {
    if (!device.IsMouse) {
        keyboard := id
        break
    }
}
if (!keyboard) {
    MsgBox No keyboard found. Install the driver and restart if requested.
    ExitApp
}
AHI.SubscribeKey(keyboard, GetKeySC("a"), true, Func("Remap"))
TrayTip, AutoHotInterceptor, % "A becomes B on keyboard " keyboard ". Close this script from its tray icon."
return
Remap(state) {
    global AHI, keyboard
    AHI.SendKeyEvent(keyboard, GetKeySC("b"), state)
}
