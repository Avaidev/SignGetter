# Signature Capture library (Windows)

A special library for capturing handwritten signatures using design tablets.

## ✨ Features

- Supports HID and noHID devices
- No vendor drivers or SDKs required
- Provides rasterized image output as byte array for later customizing
- Exposes a C API surface via a C++/CLI bridge
- Ready-to-use in C-based projects
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

#### 2. SetCropPadding
The function that allows to set custom crop padding
```c++
void SignGetter_SetCropPadding(int padding);
```
- `padding` - the new padding value in pixels;

#### 3. GetCropPadding
The function for getting current crop padding.
```c++
int SignGetter_GetCropPadding();
```
Returns the current crop padding value;

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

#### 5. ShutGetter
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
* `0` - Success;
* `1` - Manual Interruption;
* `2` - Other Exception;

- `3` - Tablets list is Empty;
- `4` - Selected tablet not found;
- `5` - Tablet is not selected;
- `6` - Tablet registration failed;
- `7` - Window Creation timed out;
- `8` - Window is not found;
- `9` - Canvas is Empty;

* `10` - Invalid user input;
* `11` - Some process is currently executing;

## License

[MIT](https://choosealicense.com/licenses/mit/)