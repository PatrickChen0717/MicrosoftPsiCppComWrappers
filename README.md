# MicrosoftPsiCppComWrappers

## How to create new function and use it in C++
#### 1. Create a c# namespace and class
```
namespace TestCSspace
{
    [Guid("FEDCBA98-7654-3210-FEDC-BA9876543210")]
    public class ClassYouWantToUse : IInterface
    {
        private bool connected;

        public void Connect()
        {
            Console.WriteLine("c# connect");
        }

        public void Disconnect()
        {
            Console.WriteLine("c# disconnect");
        }
    }
}
```
You can make up any GUID. It must have the same length as the one in the example above, and can only contian numbers (0-9) and letters (A-F).

#### 2. Build the project in correct architecture
#### 3. Locate the built .tlb file
Possible location: ..\\bin\\x64\\Debug\\ClassLibrary2.tlb

#### 4. Include the .tlb in C++ project
```
#import "ClassLibrary2.tlb" raw_interfaces_only

class ComInterfaceHandler {
public:
    QtVidconf* vc;
    ClassLibrary2::PSiCSspace_IInterfacePtr obj;
    std::future<void> backgroundTask;

    int test_cs(QtVidconf* vidconf);
    void CsGetImage();
};
```
Your complier may not recognize the raw_interface & COM_Interface and report errors, but it won't effect Cmake. So please ignore.

#### 5. Cmake will generate a .tlh based on the .tlb
It may look similar to the one below.
```
struct __declspec(uuid("12345678-90ab-cdef-1234-567890abcdee"))
PSiCSspace_IInterface : IDispatch
{
    //
    // Raw methods provided by interface
    //

      virtual HRESULT __stdcall Connect ( ) = 0;
      virtual HRESULT __stdcall Disconnect ( ) = 0;
      virtual HRESULT __stdcall initDepthCamera ( ) = 0;
      virtual HRESULT __stdcall getImagedata (
        /*[out]*/ __int64 * imageData,
        /*[out]*/ long * size,
        /*[out,retval]*/ long * pRetVal ) = 0;
};
```

#### 5. You can then invoke the C# functions in C++ based on the headers in .tlh
```
    obj.CreateInstance(__uuidof(ClassLibrary2::ClassYouWantToUse));
    obj.Connect();
```

## Links

A more detailed steps: https://stackoverflow.com/questions/13293888/how-to-call-a-c-sharp-library-from-native-c-using-c-cli-and-ijw

