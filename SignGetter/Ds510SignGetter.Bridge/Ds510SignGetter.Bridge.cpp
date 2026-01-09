#include "pch.h"
#include "Ds510SignGetter.Bridge.h"
using namespace System;
using namespace Ds510SignGetter::Manger;

public ref class SignGetterDs510Bridge {
public:
	static bool CanBeExecutedWrapper() {
		return GetterManager::CanBeExecuted;
	}

	static int GetStatusCodeWrapper() {
		return GetterManager::GetStatusCode;
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

	static void RestartGetterWrapper() {
		GetterManager::RestartGetter();
	}

	static void ShutGetterWrapper() {
		GetterManager::ShutGetter();
	}

	static int TurnScreenOnWrapper() {
		return GetterManager::TurnScreenOn();
	}

	static int TurnScreenOffWrapper() {
		return GetterManager::TurnScreenOff();
	}

	static int RebootTabletWrapper() {
		return GetterManager::RebootTablet();
	}
};

extern "C" {
	__declspec(dllexport) int SignGetterDs510_GetSign(
		void** returnArrayPointer,
		int* returnArraySize,
		int* returnImageWidth,
		int* returnImageHeight,
		int* returnImageStride
	) {
		try {
			IntPtr managedPtr;
			int size, width, height, stride;

			int result = SignGetterDs510Bridge::GetSignWrapper(
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
			return -3;
		}
	}

	_declspec(dllexport) int SignGetterDs510_GetStatusCode() {
		return SignGetterDs510Bridge::GetStatusCodeWrapper();
	}

	__declspec(dllexport) bool SignGetterDs510_CanBeExecuted() {
		return SignGetterDs510Bridge::CanBeExecutedWrapper();
	}

	__declspec(dllexport) void SignGetterDs510_ReleaseOneMemory() {
		SignGetterDs510Bridge::ReleaseOneMemoryWrapper();
	}

	__declspec(dllexport) void SignGetterDs510_ReleaseMemory() {
		SignGetterDs510Bridge::ReleaseMemoryWrapper();
	}

	_declspec(dllexport) void SignGetterDs510_RestartGetter() {
		SignGetterDs510Bridge::RestartGetterWrapper();
	}

	__declspec(dllexport) void SignGetterDs510_ShutGetter() {
		SignGetterDs510Bridge::ShutGetterWrapper();
	}

	__declspec(dllexport) int SignGetterDs510_TurnScreenOn() {
		return SignGetterDs510Bridge::TurnScreenOnWrapper();
	}

	__declspec(dllexport) int SignGetterDs510_TurnScreenOff() {
		return SignGetterDs510Bridge::TurnScreenOffWrapper();
	}

	__declspec(dllexport) int SignGetterDs510_RebootTablet() {
		return SignGetterDs510Bridge::RebootTabletWrapper();
	}

}


