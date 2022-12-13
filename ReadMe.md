# QA40x_BareMetal
This repo shows how to to use USB writes to control the QA40x hardware. There are four ways of controlling the QA40x hardware:

**QA40x Application**: This allows you to make measurements in a self-contained environment. 

**QA40x REST**: This allows you to control the QA40x hardware via the QA40x application. The control occurs via REST calls. This requires proficiency is writing software.

**Tractor**: Tractor is a standalone application that controls the QA40x application via REST. The application allows you to quickly build test scripts, specify pass/fail windows for measurement results and permit logging to databases. The scripts can also include operator instructions to allow manual testing by playing a WAV file (for example). This allows factory personnel to verify if pots are scratchy, for example. Tractor doesn't require any coding for standard tests.

**Bare Metal**: The Bare Metal interfaces is a USB register-based interface for directly controlling the hardware. There are 4 endpoints to the QA40x hardware. Two endpoints facilitate register read/write. And two endpoints facilitate data read/write. You put the QA40x hardware into a specific mode via register writes. For example, you might set the max input level to 6 dBV using a register write. And once you have set the hardware up as desired, you stream data to the DAC and stream data bacak from the ADC. The code in this repo shows how to do it. The code is liberally commented, but feel free to ask a question on the forum if not clear. Note that the language used is C#, and the LibUsbDotNet library is used for the USB reading and writing. Underneath the covers, the LibUsbDotNet uses LibUsb, which is a cross-platform library for Windwows, Linux and Mac. There are lots of wrappers for LibUsb, meaning C#, Python and other languages can readily use the library.

## Overlapped IO and Async Programming
The sample code in this repo performs overlapped IO. That is, you tell and operation to start (sending a USB packet) and then later to check to see if the operating is complete. In the interim, you can do other work. If you are not familiar with overlapped IO, this code will be frustrating because it's quite a bit more involved than, say, Serial Port code where you are blocking on operations. Addtionally, the code makes use of C# async programming model. This can be a bit confusing if you've not seen it before. Microsoft has a very good tutorial [HERE](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/async/) that might be helpful to building an understanding. But in a nutshell, if you see the keyword ```await``` that means that the code will pause until the function/method/task is complete. But it also means the thread isn't completely blocked--it can still perform work in other places that isn't dependent on the completion. This means your UI "stays alive" in most places even though it might be blocked at an await. 
