# Signature Capture library (Windows)

A C# library for capturing handwritten signatures using design tablets that lack proper drivers. Windows often recognize such tablets as mouse devices - this library interprets those events to record pen strokes.

## ✨ Features

- Capture signatures from tablets recognized as HID mouse devices
- No vendor drivers or SDKs required
- Provides rasterized image output as byte array for later customizing
- Exposes a C API surface via a C++/CLI bridge
- Ready-to-use in C-based prjects
- Explicit resource management and robust error handling

## ⚙️ Requirements
- Windows 10 or later
- .NET Framework 4.7.2 (for C# library)

## 🚀 Getting Started

### 1. Download the Release

### 2. Include in Native Project
- Copy files into your project folder
- Include the provided header file:
  ```c
  #include "TabletSignGetter.Bridge.h"

## 📕 Available Functions

#### 1. GetSign
The functions for getting signature.
```c++
int SignGetter_GetSign(
        void** returnArrayPointer,
        int* returnArraySize,
        int* returnImageWidth,
        int* returnImageHeight,
        int* returnImageStride
    );
```
Returns the Status Code of the function execution result
- `returnArrayPointer` - the pointer for result byte array with image;
- `returnArraySize` - the size of the result array;
- `returnImageWidth` - the width of the result image;
- `returnImageHeight` - the height of the result image;
- `returnImageStride` - the stride of the result image (for later processing);

#### 2. SelectTablet
The function for selecting tablet from available list.
```c++
bool SignGetter_SelectTablet();
```
Returns `true` if the tablet was selected, otherwise `false`;

#### 3. GetStatusCode
The function for getting current Status Code.
```c++
int SignGetter_GetStatusCode();
```
Returns the current Status Code of SignGetter;

#### 4. CanBeExecuted
The function for checking whether the GetSign can be executed.
```c++
bool SignGetter_CanBeExecuted();
```
Returns `true` if GetSign can be executed, otherwise `false`;

#### 3. ReleaseOneMemory
The function to release the one first block of memory.
```c++
void SignGetter_ReleaseOneMemory();
```

#### 4. ReleaseMemory
The function to release all blocks of memory.
```c++
void SignGetter_ReleaseMemory();
```

#### 5. RestartGetter
The function to recreate window and reregister the tablet.
```c++
void SignGetter_RestartGetter();
```

#### 6. ShutGetter
The function to fully shut the SignGetter app.
```c++
void SignGetter_ShutGetter();
```
*!!! Use only at the end of program lifetime.*

## 👆 Interaction
Call the function -> Draw the signature -> \
Press `Enter` to accept and save \
Press `Escape` to exit without saving \
Press `Ctrl + Z` to reset canvas -> \
Do smth you want with result.

## ❗Status Codes
- `0` - Success;
- `-1` - Other Exception;

* `1 | 0x0001` - Tablets list is Empty;
* `2 | 0x0002` - Tablet not found;
* `4 | 0x0004` - Exception during selection;

- `8 | 0x0008` - Invalid input;
- `16 | 0x0010` - Automatically Selected;
- `32 | 0x0020` - Window Creation timed out;

* `64 | 0x0040` - Exception during saving;
* `128 | 0x0080` - Canvas is Null;
* `256 | 0x0100` - Canvas is Empty;
* `512 | 0x0200` - Cant allocate the memory (Out of Memory);

- `1024 | 0x0400` - SignGetter cant be executed at this time;
- `2048 | 0x0800` - SignGetter tablet registration failed;
- `4096 | 0x1000` - Exception during drawing;
- `8192 | 0x2000` - Exception in reading input data;
- `16384 | 0x4000` - The canvas window is null;

## License

[MIT](https://choosealicense.com/licenses/mit/)