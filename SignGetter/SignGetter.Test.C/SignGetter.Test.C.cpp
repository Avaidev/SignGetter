#include <iostream>
#include <fstream>
#include <string>
#include <cstdint>
#include <cstring>
#include "TabletSignGetter.Bridge.h"

using namespace std;

// ============================================================
//  Updated BMP writer for Bgr24 (3 bytes per pixel)
// ============================================================
static bool saveBmp24(const string& path,
    const uint8_t* bgrPixels,
    int width, int height, int stride)
{
    // Bgr24 = 3 bytes per pixel
    // Note: 'stride' from C# already includes the necessary 4-byte alignment padding
    int fileSize = 54 + (stride * height);

    uint8_t header[54] = {};

    // File header (14 bytes)
    header[0] = 'B'; header[1] = 'M';
    header[2] = fileSize & 0xFF;
    header[3] = (fileSize >> 8) & 0xFF;
    header[4] = (fileSize >> 16) & 0xFF;
    header[5] = (fileSize >> 24) & 0xFF;
    header[10] = 54; // data offset

    // DIB header (40 bytes)
    header[14] = 40;
    header[18] = width & 0xFF;
    header[19] = (width >> 8) & 0xFF;
    header[20] = (width >> 16) & 0xFF;
    header[21] = (width >> 24) & 0xFF;

    // Negative height for top-down (matches WPF CopyPixels)
    int negHeight = -height;
    header[22] = negHeight & 0xFF;
    header[23] = (negHeight >> 8) & 0xFF;
    header[24] = (negHeight >> 16) & 0xFF;
    header[25] = (negHeight >> 24) & 0xFF;

    header[26] = 1;   // planes
    header[28] = 24;  // <--- SET TO 24 for Bgr24

    ofstream f(path, ios::binary);
    if (!f) return false;

    f.write(reinterpret_cast<const char*>(header), 54);

    // Write pixel data
    // We write 'stride' bytes per row because your C# code 
    // calculated 'stride' to include the BMP-required padding.
    for (int y = 0; y < height; ++y) {
        const uint8_t* row = bgrPixels + y * stride;
        f.write(reinterpret_cast<const char*>(row), stride);
    }

    return f.good();
}

// ============================================================
//  Test harness
// ============================================================
void GetSign(int testIndex) {
    void* buffer = nullptr;
    int size = 0, width = 0, height = 0, stride = 0;

    int result = SignGetter_GetSign(&buffer, &size, &width, &height, &stride);
    cout << "\nResult Code: " << result << endl;

    if (buffer && size > 10) {
        uint8_t* bytes = static_cast<uint8_t*>(buffer);
        cout << "First 10 bytes of pixel data (Expect 255 if background is white): ";
        for (int i = 0; i < 10; i++) {
            cout << (int)bytes[i] << " ";
        }
        cout << endl;
    }

    if (result == 0 || result == 16) {
        // In 24-bit, stride / width will be 3 (plus a tiny bit for padding)
        int bpp = (width > 0) ? (stride * 8 / width) : 0;

        cout << "Report:"
            << "\n  Size:     " << size
            << "\n  Width:    " << width
            << "\n  Height:   " << height
            << "\n  Stride:   " << stride
            << "\n  Bits/Pix: " << bpp << " (Expected: ~24)"
            << "\n End of Report" << endl;

        // Verify we have at least 3 bytes per pixel
        if (buffer && size > 0 && stride >= (width * 3)) {
            string filename = "sign_test" + to_string(testIndex) + ".bmp";
            bool ok = saveBmp24(filename,
                static_cast<const uint8_t*>(buffer),
                width, height, stride);
            cout << (ok ? "Saved (24-bit BMP): " : "ERROR saving: ") << filename << endl;
        }
        else {
            cout << "Buffer check failed: size=" << size << " stride=" << stride << endl;
        }
    }
    else {
        cout << "Wrong result code: " << result << endl;
    }

    SignGetter_ReleaseMemory();
    cout << "\nPress Enter to continue..." << endl;
    cin.ignore(1000, '\n');
    cin.get();
}

int main()
{
    cout << "TabletSignGetterLib Testing (Bgr24 Mode)" << endl;
    cout << "Getter Can Be Executed: " << (SignGetter_CanBeExecuted() ? "YES" : "NO") << endl;

    cout << "\nTest 1:" << endl;
    GetSign(1);

    cout << "\nTest 2:" << endl;
    GetSign(2);

    SignGetter_ShutGetter();
    cout << "End of TabletSignGetterLib Testing" << endl;
    return 0;
}