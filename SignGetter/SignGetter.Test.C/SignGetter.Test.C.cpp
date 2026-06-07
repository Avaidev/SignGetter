#include <iostream>
#include <fstream>
#include <string>
#include <cstdint>
#include <cstring>
#include "TabletSignGetter.Bridge.h"

using namespace std;

// ============================================================
//  Updated BMP writer for Bgr32 (4 bytes per pixel)
//  This matches the Bgr32 format used in the C# code.
// ============================================================
static bool saveBmp32(const string& path,
    const uint8_t* bgrPixels,
    int width, int height, int stride)
{
    // Bgr32 = 4 bytes per pixel. 
    // Data size is exactly stride * height.
    int fileSize = 54 + (stride * height);

    uint8_t header[54] = {};

    // --- File header (14 bytes) ---
    header[0] = 'B'; header[1] = 'M';
    header[2] = fileSize & 0xFF;
    header[3] = (fileSize >> 8) & 0xFF;
    header[4] = (fileSize >> 16) & 0xFF;
    header[5] = (fileSize >> 24) & 0xFF;
    header[10] = 54; // Offset to start of pixel data

    // --- DIB header (40 bytes) ---
    header[14] = 40; // Header size
    header[18] = width & 0xFF;
    header[19] = (width >> 8) & 0xFF;
    header[20] = (width >> 16) & 0xFF;
    header[21] = (width >> 24) & 0xFF;

    // Use negative height for top-down orientation (matches WPF/Windows memory)
    int negHeight = -height;
    header[22] = negHeight & 0xFF;
    header[23] = (negHeight >> 8) & 0xFF;
    header[24] = (negHeight >> 16) & 0xFF;
    header[25] = (negHeight >> 24) & 0xFF;

    header[26] = 1;   // Planes
    header[28] = 32;  // <--- SET TO 32 BITS (Bgr32)
    header[30] = 0;   // BI_RGB (no compression)
    header[34] = (stride * height) & 0xFF; // Image size

    ofstream f(path, ios::binary);
    if (!f) return false;

    // Write header and pixel data
    f.write(reinterpret_cast<const char*>(header), 54);
    f.write(reinterpret_cast<const char*>(bgrPixels), stride * height);

    return f.good();
}

// ============================================================
//  Test harness
// ============================================================
void GetSign(int testIndex) {
    void* buffer = nullptr;
    int size = 0, width = 0, height = 0, stride = 0;

    // Call the Bridge function
    int result = SignGetter_GetSign(&buffer, &size, &width, &height, &stride);
    cout << "\nResult Code: " << result << endl;

    if (result == 0 || result == 16) {
        // Calculate Bits Per Pixel to verify C# output
        // For Bgr32, this should be exactly 32.
        int bpp = (width > 0) ? (stride * 8 / width) : 0;

        cout << "Report:"
            << "\n  Size:     " << size
            << "\n  Width:    " << width
            << "\n  Height:   " << height
            << "\n  Stride:   " << stride
            << "\n  Bits/Pix: " << bpp << " (Expected: 32)"
            << "\n End of Report" << endl;

        if (buffer && size > 0 && stride >= (width * 4)) {
            string filename = "sign_test" + to_string(testIndex) + ".bmp";

            // Call the 32-bit BMP saver
            bool ok = saveBmp32(filename,
                static_cast<const uint8_t*>(buffer),
                width, height, stride);

            cout << (ok ? "Saved (32-bit BMP): " : "ERROR saving: ") << filename << endl;
        }
        else {
            cout << "Buffer check failed: size=" << size << " stride=" << stride << endl;
        }
    }
    else {
        cout << "Wrong result code: " << result << endl;
    }

    // Crucial: release the memory allocated by Marshal.AllocHGlobal in C#
    SignGetter_ReleaseMemory();

    cout << "\nPress Enter to continue..." << endl;
    cin.ignore(1000, '\n');
    cin.get();
}

int main()
{
    cout << "TabletSignGetter Testing (Bgr32 Mode)" << endl;

    if (!SignGetter_CanBeExecuted()) {
        cout << "Error: Getter cannot be executed." << endl;
        return 1;
    }

    GetSign(1); // Test first call (e.g. your first C# function)
    GetSign(2); // Test second call (e.g. your second C# function)

    SignGetter_ShutGetter();
    return 0;
}