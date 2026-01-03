#include <iostream>
#include "TabletSignGetter.Bridge.h"

using namespace std;

void GetSign();

int main()
{
    cout << "TabletSignGetterLib Testing" << endl;

    cout << "Getter Can Be Executed: " << SignGetter_CanBeExecuted() << endl;
    
    cout << "Test 1:\n" << endl;
    GetSign();

    cout << "Test 2:\n" << endl;
    GetSign();

    SignGetter_ShutGetter();
    cout << "End of TabletSignGetterLib Testing" << endl;
    return 0;
}

void GetSign() {
    void* buffer;
    int size, width, height, stride;
    int result = SignGetter_GetSign(&buffer, &size, &width, &height, &stride);

    cout << "\nResult Code: " << result << endl;
    if (result == 0 || result == 16) {
        cout << "Report:\nSize: " << size
            << "\nImage Width: " << width
            << "\nImage Height:" << height
            << "\nImage Stride: " << stride << "\n End of Report\n" << endl;
    }
    else cout << "Wrong Result" << endl;

    SignGetter_ReleaseMemory();

    cout << "press to continue;" << endl;
    cin.get();
}