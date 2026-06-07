#pragma once

#ifdef TABLETSIGNGETTER_BRIDGE_EXPORTS
    #define BRIDGE_API __declspec(dllexport)
#else
    #define BRIDGE_API __declspec(dllimport)
#endif

#ifdef __cplusplus
extern "C" {
#endif

    BRIDGE_API int SignGetter_GetSign(
        void** returnArrayPointer,
        int* returnArraySize,
        int* returnImageWidth,
        int* returnImageHeight,
        int* returnImageStride
    );

    BRIDGE_API bool SignGetter_CanBeExecuted();

    BRIDGE_API void SignGetter_ReleaseOneMemory();

    BRIDGE_API void SignGetter_ReleaseMemory();

    BRIDGE_API void SignGetter_ShutGetter();

    BRIDGE_API int SignGetter_GetCropPadding();

    __declspec(dllimport) void SignGetter_SetCropPadding(int padding);

#ifdef __cplusplus
}
#endif


