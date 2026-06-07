#include "pch.h"
#include "TabletSignGetter.Bridge.h"

using namespace System;
using namespace TabletSignGetterLib;


public ref class SignGetterBridge
{
public:
	static bool CheckCanBeExecutedWrapper() {
		return GetterManager::CheckCanBeExecuted();
	}

	static int GetCropPaddingWrapper() {
		return GetterManager::GetCropPadding();
	}

	static void SetCropPaddingWrapper(int padding) {
		GetterManager::SetCropPadding(padding);
	}

	static int GetSignWrapper(IntPtr% returnArrayPointer, int% returnArraySize,
		int% returnImageWidth, int% returnImageHeight, int% returnImageStride) {
		return GetterManager::GetSign(returnArrayPointer, returnArraySize, returnImageWidth, returnImageHeight, returnImageStride);
	}

	static void ReleaseOneMemoryWrapper() {
		GetterManager::ReleaseOneMemory();
	}

	static void ReleaseMemoryWrapper() {
		GetterManager::ReleaseMemory();
	}

	static void ShutGetterWrapper() {
		GetterManager::ShutGetter();
	}
};

extern "C" {
	BRIDGE_API int SignGetter_GetSign(
		void** returnArrayPointer,
		int* returnArraySize,
		int* returnImageWidth,
		int* returnImageHeight,
		int* returnImageStride
	) {
		try {
			IntPtr managedPtr;
			int size, width, height, stride;

			int result = SignGetterBridge::GetSignWrapper(
				managedPtr, size, width, height, stride
			);

			*returnArrayPointer = managedPtr.ToPointer();
			*returnArraySize = size;
			*returnImageWidth = width;
			*returnImageHeight = height;
			*returnImageStride = stride;
			return result;
		}
		catch (Exception^) {
			*returnArrayPointer = nullptr;
			*returnArraySize = 0;
			*returnImageWidth = 0;
			*returnImageHeight = 0;
			*returnImageStride = 0;
			return 0x2;
		}
	}

	BRIDGE_API bool SignGetter_CanBeExecuted() {
		return SignGetterBridge::CheckCanBeExecutedWrapper();
	}

	BRIDGE_API void SignGetter_ReleaseOneMemory() {
		SignGetterBridge::ReleaseOneMemoryWrapper();
	}

	BRIDGE_API void SignGetter_ReleaseMemory() {
		SignGetterBridge::ReleaseMemoryWrapper();
	}

	BRIDGE_API int SignGetter_GetCropPadding() {
		return SignGetterBridge::GetCropPaddingWrapper();
	}

	BRIDGE_API void SignGetter_SetCropPadding(int padding) {
		SignGetterBridge::SetCropPaddingWrapper(padding);
	}

	BRIDGE_API void SignGetter_ShutGetter() {
		SignGetterBridge::ShutGetterWrapper();
	}
}

