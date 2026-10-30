#include "../Native/interception.h"
#include "FakeSdk.h"
#include <windows.h>
#include <future>
#include <iostream>
#include <random>
#include <stdexcept>
unsigned checks=0;
void Check(bool ok,const char* message){++checks;if(!ok)throw std::runtime_error(message);}
int All(int){return 1;}
int main()try{
    FakeReset();auto c=interception_create_context();Check(c!=nullptr,"Create");
    Check(!interception_create_context()&&interceptor_error()==ERROR_BUSY,"Never steal capture");
    HANDLE readiness=static_cast<HANDLE>(interceptor_duplicate_ready_event(c));
    Check(readiness!=nullptr&&WaitForSingleObject(readiness,0)==WAIT_TIMEOUT,"Idle readiness event");
    Check(interception_is_keyboard(10)&&!interception_is_keyboard(11)&&interception_is_mouse(20)&&!interception_is_mouse(21),"Boundary IDs");
    wchar_t text[512]{};Check(interception_get_hardware_id(c,1,text,sizeof(text))>2,"Hardware ID");
    Check(interception_get_hardware_id(c,1,text,2)>2&&interceptor_error()==ERROR_INSUFFICIENT_BUFFER,"Bounded ID buffer");
    interception_set_precedence(c,1,1);Check(interceptor_error()==ERROR_NOT_SUPPORTED,"Explicit unsupported priority");
    FakePushKey(101,30);FakePushMouse(201,12,-7);Check(FakeAwaitSent(1,1),"Unselected inputs pass through");
    Check(interception_wait_with_timeout(c,0)==0&&interceptor_error()==0,"Empty poll");
    interceptor_wake(c);Check(interception_wait(c)==0,"Wake before wait is not lost");
    interception_set_filter(c,interception_is_keyboard,65535);interception_set_filter(c,interception_is_mouse,65535);
    FakePushKey(102,23);Check(interception_wait_with_timeout(c,5000)==2,"Second keyboard identity");
    Check(WaitForSingleObject(readiness,5000)==WAIT_OBJECT_0,"Selected report signals native readiness");
    alignas(InterceptionMouseStroke) InterceptionStroke data{};
    Check(interception_receive(c,1,&data,1)==0,"No cross-device consumption");
    Check(interception_receive(c,2,&data,1)==1&&reinterpret_cast<InterceptionKeyStroke*>(data)->code==23,"Read second keyboard");
    Check(WaitForSingleObject(readiness,0)==WAIT_TIMEOUT,"Consumption clears readiness");
    FakePushMouse(202,900,-485,0x800,-120,2);
    Check(interception_wait_with_timeout(c,5000)==12,"Second mouse identity");
    Check(interception_receive(c,12,&data,1)==1,"Mouse receive");auto* m=reinterpret_cast<InterceptionMouseStroke*>(data);
    Check(m->x==900&&m->y==-485&&m->rolling==-120&&m->state==0x800&&m->flags==2,"Exact signed mouse fields");
    Check(interception_send(c,12,&data,1)==1,"Mouse send");
    interception_set_filter(c,interception_is_keyboard,2);FakePushKey(101,31,0);FakePushKey(101,31,1);
    Check(interception_wait_with_timeout(c,5000)==1,"Up-only filter");Check(interception_receive(c,1,&data,1)==1&&reinterpret_cast<InterceptionKeyStroke*>(data)->state==1,"Up state");
    Check(FakeAwaitSent(2,2),"Nonmatching key passthrough");
    interception_set_filter(c,All,65535);
    for(unsigned i=0;i<20000;++i){FakePushKey(101,static_cast<unsigned short>(i));Check(interception_wait_with_timeout(c,5000)==1,"Stress wait");Check(interception_receive(c,1,&data,1)==1,"Stress receive");Check(reinterpret_cast<InterceptionKeyStroke*>(data)->code==static_cast<unsigned short>(i),"Stress order");Check(interception_send(c,1,&data,1)==1,"Stress forward");}
    FakeRemove(101);Check(interception_get_hardware_id(c,1,text,sizeof(text))==0,"Disconnected source");Check(interception_send(c,1,&data,1)==0&&interceptor_error()==ERROR_DEVICE_NOT_CONNECTED,"Never reuse stale destination");
    auto waiting=std::async(std::launch::async,[&]{return interception_wait(c);});interception_destroy_context(c);
    Check(waiting.wait_for(std::chrono::seconds(5))==std::future_status::ready&&waiting.get()==0,"Destroy cancels waiter without use-after-free");
    Check(WaitForSingleObject(readiness,0)==WAIT_OBJECT_0,"Duplicated readiness remains valid across close");CloseHandle(readiness);
    Check(interception_receive(c,1,&data,1)==0&&interceptor_error()==ERROR_INVALID_HANDLE,"Closed context rejection");
    std::mt19937 random(9147);for(unsigned i=0;i<100000;++i){auto fake=reinterpret_cast<void*>(static_cast<uintptr_t>(random())+1000);Check(interception_receive(fake,1,&data,1)==0,"Unknown handles cannot be dereferenced");}
    for(unsigned i=0;i<100;++i){FakeReset();auto h=interception_create_context();Check(h!=nullptr,"Repeated open");interception_destroy_context(h);Check(interceptor_error()==0,"Repeated close");}
    FakeReset();c=interception_create_context();Check(c!=nullptr,"Overflow create");interception_set_filter(c,All,65535);for(unsigned i=0;i<10000;++i)FakePushKey(101,30);
    bool overflow=false;for(unsigned i=0;i<1000&&!overflow;++i){interception_wait_with_timeout(c,10);overflow=interceptor_error()==ERROR_BUFFER_OVERFLOW;Sleep(1);}Check(overflow,"Queue overflow closes capture explicitly");interception_destroy_context(c);
    std::cout<<checks<<" bridge checks passed\n";return 0;
}catch(const std::exception& e){std::cerr<<"FAIL: "<<e.what()<<"; "<<interceptor_error_message()<<"\n";return 1;}
