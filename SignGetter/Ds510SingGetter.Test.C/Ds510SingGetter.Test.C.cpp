#include <iostream>
#include "Ds510SignGetter.Bridge.h"

using namespace std;

void GetSign();

int main()
{
    cout << "TabletSignGetterLib Testing" << endl;

    cout << "Getter Can Be Executed: " << SignGetterDs510_CanBeExecuted() << endl;

    cout << "Test 1:\n" << endl;
    GetSign();

    SignGetterDs510_ShutGetter();
    cout << "End of TabletSignGetterLib Testing" << endl;
    return 0;
}

void GetSign() {
    void* buffer;
    int size, width, height, stride;
    int result = SignGetterDs510_GetSign(&buffer, &size, &width, &height, &stride);

    cout << "\nResult Code: " << result << endl;
    if (result == 0 || result == 16) {
        cout << "Report:\nSize: " << size
            << "\nImage Width: " << width
            << "\nImage Height:" << height
            << "\nImage Stride: " << stride << "\n End of Report\n" << endl;
    }
    else cout << "Wrong Result" << endl;

    SignGetterDs510_ReleaseMemory();

    cout << "press to continue;" << endl;
    cin.get();
}