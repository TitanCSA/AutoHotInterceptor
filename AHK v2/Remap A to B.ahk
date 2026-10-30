#Requires AutoHotkey v2.0
#SingleInstance Force
#Include Lib\AutoHotInterception.ahk
Persistent
AHI := AutoHotInterception()
keyboard := 0
for id, device in AHI.GetDeviceList() {
    if !device.IsMouse {
        keyboard := id
        break
    }
}
if !keyboard {
    MsgBox("No keyboard found. Install the driver and restart if requested.")
    ExitApp
}
AHI.SubscribeKey(keyboard, GetKeySC("a"), true, Remap)
TrayTip("A becomes B on keyboard " keyboard ". Close this script from its tray icon.", "AutoHotInterceptor")
Remap(state) {
    global AHI, keyboard
    AHI.SendKeyEvent(keyboard, GetKeySC("b"), state)
}
