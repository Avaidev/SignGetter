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

    _declspec(dllimport) bool SignGetter_SelectTablet();

    _declspec(dllimport) int SignGetter_GetStatusCode();

    __declspec(dllimport) bool SignGetter_CanBeExecuted();

    __declspec(dllimport) void SignGetter_ReleaseOneMemory();

    __declspec(dllimport) void SignGetter_ReleaseMemory();

    _declspec(dllimport) void SignGetter_RestartGetter();

    __declspec(dllimport) void SignGetter_ShutGetter();

#ifdef __cplusplus
}
#endif


