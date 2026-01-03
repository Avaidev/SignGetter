#include "pch.h"
#include "TabletSignGetter.Bridge.h"
using namespace System;
using namespace TabletSignGetterLib::Manager;


public ref class SignGetterBridge
{
public:
	static bool CanBeExecutedWrapper() {
		return GetterManager::CanBeExecuted;
	}

	static int GetStatusCodeWrapper() {
		return GetterManager::GetStatusCode();
	}

	static int GetSignWrapper(IntPtr% returnArrayPointer, int% returnArraySize,
		int% returnImageWidth, int% returnImageHeight, int% returnImageStride) {
		return GetterManager::GetSign(returnArrayPointer, returnArraySize, returnImageWidth, returnImageHeight, returnImageStride);
	}

	static bool SelectTabletWrapper() {
		return GetterManager::SelectTablet();
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
	_declspec(dllexport) int SignGetter_GetSign(
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
			return -1;
		}
	}

	_declspec(dllexport) bool SignGetter_SelectTablet() {
		return SignGetterBridge::SelectTabletWrapper();
	}

	_declspec(dllexport) bool SignGetter_CanBeExecuted() {
		return SignGetterBridge::CanBeExecutedWrapper();
	}

	_declspec(dllexport) int SignGetter_GetStatusCode() {
		return SignGetterBridge::GetStatusCodeWrapper();
	}

	_declspec(dllexport) void SignGetter_ReleaseOneMemory() {
		SignGetterBridge::ReleaseOneMemoryWrapper();
	}

	_declspec(dllexport) void SignGetter_ReleaseMemory() {
		SignGetterBridge::ReleaseMemoryWrapper();
	}

	_declspec(dllexport) void SignGetter_ShutGetter() {
		SignGetterBridge::ShutGetterWrapper();
	}
}

