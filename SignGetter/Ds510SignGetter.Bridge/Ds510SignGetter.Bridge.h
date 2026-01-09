#pragma once

#ifdef __cplusplus
extern "C" {
#endif
    __declspec(dllimport) int SignGetterDs510_GetSign(
        void** returnArrayPointer,
        int* returnArraySize,
        int* returnImageWidth,
        int* returnImageHeight,
        int* returnImageStride
    );

    _declspec(dllimport) int SignGetterDs510_GetStatusCode();

    __declspec(dllimport) bool SignGetterDs510_CanBeExecuted();

    __declspec(dllimport) void SignGetterDs510_ReleaseOneMemory();

    __declspec(dllimport) void SignGetterDs510_ReleaseMemory();

    _declspec(dllimport) void SignGetterDs510_RestartGetter();

    __declspec(dllimport) void SignGetterDs510_ShutGetter();

    __declspec(dllimport) int SignGetterDs510_TurnScreenOn();

    __declspec(dllimport) int SignGetterDs510_TurnScreenOff();

    __declspec(dllimport) int SignGetterDs510_RebootTablet();

#ifdef __cplusplus
}
#endif

