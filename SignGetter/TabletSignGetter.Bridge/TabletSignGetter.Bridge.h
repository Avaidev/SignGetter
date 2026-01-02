#pragma once

#ifdef __cplusplus
extern "C" {
#endif

    __declspec(dllimport) int SignGetter_GetSign(
        void** returnArrayPointer,
        int* returnArraySize,
        int* returnImageWidth,
        int* returnImageHeight,
        int* returnImageStride
    );

    __declspec(dllimport) int SignGetter_CanBeExecuted();

    __declspec(dllimport) void SignGetter_ReleaseOneMemory();

    __declspec(dllimport) void SignGetter_ReleaseMemory();

    __declspec(dllimport) void SignGetter_ShutGetter();

#ifdef __cplusplus
}
#endif


