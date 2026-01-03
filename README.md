# Signature Capture library (Windows)

A C# library for capturing handwritten signatures using design tablets that lack proper drivers. Windows often recognize such tablets as mouse devices - this library interprets those events to record pen strokes.

## ✨ Features

- Capture signatures from tablets recognized as HID mouse devices
- No vendor drivers or SDKs required
- Provides rasterized image output as byte array for later customizing
- Exposes a C API surface via a C++/CLI bridge
- Ready-to-use in C, C++, and Delphi projects
- Explicit resource management and robust error handling

## ⚙️ Requirements
- Windows 10 or later
- .NET Framework 4.7.2 (for managed library)
- Visual Studio (recommended for building bridge DLL)

## 🚀 Getting Started

### 1. Build the Managed Library
Open `/SignGetter/TabletSignGetterLib` in Visual Studio and build the project.  
This produces `TabletSignGetter.dll`.

### 2. Build the C++/CLI Bridge
Open `/SingGetter/TabletSignGetter.Bridge` and build the project.  
This produces `TabletSignGetter.Bridge.dll` which exposes a C API.

### 3. Include in Native Project
- Copy `TabletSignGetter.dll` and `TabletSignGetter.Bridge.dll` into your project folder
- Include additional files (like `HidSharp.dll`) to your project folder
- Include the provided header file:
  ```c
  #include "TabletSignGetter.Bridge.h"

## 📕 Available Functions

#### 1. GetSign
The functions for getting signature.\
Gets the `void*` array pointer, `int*` array size, `int*` image width, `int*` image height and `int*` image stride. \
Returns the `int` status code.
#### 2. CanBeExecuted
The function for checking whether the GetSign can be executed.
#### 3. ReleaseOneMemory
The function to release the one first block of memory.
#### 4. ReleaseMemory
The function to release all blocks of memory.
#### 5. ShutGetter
The function to fully shut the SignGetter app. \
*!!! Use only at the end of program lifetime.*

## 👆 Interaction
Call the function -> Draw the signature -> \
Press `Enter` to accept and save \
Press `Escape` to exit without saving \
Press `Ctrl + Z` to reset canvas -> \
Do smth you want.

## License

[MIT](https://choosealicense.com/licenses/mit/)
