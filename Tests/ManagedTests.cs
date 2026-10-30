using System;
using System.Threading;
using System.Runtime.InteropServices;
using AutoHotInterception;
using AutoHotInterception.Helpers;
class ManagedTests {
    [DllImport("InterceptorBridge.dll",CallingConvention=CallingConvention.Cdecl)]static extern void bridge_test_push_key(uint device,uint code,uint flags);
    [DllImport("InterceptorBridge.dll",CallingConvention=CallingConvention.Cdecl)]static extern void bridge_test_push_mouse(uint device,int x,int y);
    [DllImport("InterceptorBridge.dll",CallingConvention=CallingConvention.Cdecl)]static extern uint bridge_test_sent_keys();
    [DllImport("InterceptorBridge.dll",CallingConvention=CallingConvention.Cdecl)]static extern uint bridge_test_sent_mice();
    static int checks;
    static void Check(bool value,string message){++checks;if(!value)throw new Exception(message);}
    static bool Until(Func<bool> predicate){var end=DateTime.UtcNow.AddSeconds(5);while(!predicate()&&DateTime.UtcNow<end)Thread.Sleep(1);return predicate();}
    static int Main(){try{
        using(var manager=new Manager()){
            Check(manager.GetDeviceList().Length==4,"Device enumeration");
            Check(manager.GetKeyboardId(0x1234,0x1002)==2&&manager.GetMouseId(0x1234,0x1001)==11,"VID/PID lookup");
            int first=0,second=0,mouse=0,context=0;
            manager.SubscribeKeyboard(1,true,new Action<ushort,int>((code,state)=>Interlocked.Add(ref first,code)));
            manager.SubscribeKeyboard(2,true,new Action<ushort,int>((code,state)=>Interlocked.Add(ref second,code)));
            manager.SubscribeMouseMoveRelative(11,true,new Action<int,int>((x,y)=>Interlocked.Add(ref mouse,x+y)));
            bridge_test_push_key(101,30,0);bridge_test_push_mouse(201,7,9);bridge_test_push_key(102,48,0);
            Check(Until(()=>first==30&&second==48&&mouse==16),"Mixed devices remain distinct");
            manager.UnsubscribeKeyboard(1);manager.UnsubscribeKeyboard(2);manager.UnsubscribeMouseMoveRelative(11);
            var sent=bridge_test_sent_keys();bridge_test_push_key(101,30,0);Check(Until(()=>bridge_test_sent_keys()>sent),"Unsubscribe restores passthrough");
            manager.SetContextCallback(1,new Action<int>(state=>Interlocked.Increment(ref context)));
            bridge_test_push_key(101,30,1);Check(Until(()=>context==2),"Context wraps forward");manager.RemoveContextCallback(1);
            manager.SetContextCallback(11,new Action<int>(state=>Interlocked.Increment(ref context)));
            bridge_test_push_mouse(201,1,2);Check(Until(()=>context==4),"Mouse context wraps forward");manager.RemoveContextCallback(11);
            int callbacks=0;
            for(int iteration=0;iteration<100;iteration++){
                manager.SubscribeKey(1,30,true,new Action<int>(state=>Interlocked.Increment(ref callbacks)));
                bridge_test_push_key(101,30,0);int expected=iteration+1;
                Check(Until(()=>callbacks==expected),"Repeated subscription callback");manager.UnsubscribeKey(1,30);
            }
            manager.SubscribeKey(1,30,true,new Action<int>(state=>{throw new Exception("Expected callback failure");}),true);
            bridge_test_push_key(101,30,0);Check(Until(()=>manager.GetLastError().Contains("Expected callback failure")),"Concurrent callback errors are reported");
            manager.SetState(false);
            foreach(ushort code in new ushort[]{30,0x11d,0x137,69})foreach(int state in new[]{0,1}){
                var strokes=ScanCodeHelper.TranslateAhkCode(code,state);var translated=ScanCodeHelper.TranslateScanCodes(strokes);
                Check(translated.Code==code&&translated.State==state,"Scan-code round trip "+code+" state "+state);
            }
        }
        Console.WriteLine(checks+" managed checks passed");return 0;
    }catch(Exception error){Console.Error.WriteLine(error);return 1;}}
}
