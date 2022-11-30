/*
Win API DLL
Author: Brian Tu
Date: 2022/11/29
Description: This is an API library for Rester project to control activation of color filter and reg reading.
*/

#include "pch.h"
#include <shlwapi.h>
#include <Shellapi.h>
#include <Winreg.h>

using namespace std;

extern "C" {
    // activate and setup the color filter
    __declspec(dllexport) int APIENTRY __stdcall SetGrayMoniter() {
        int nActive = 1;
        int nType = 0;
        SHSetValue(HKEY_CURRENT_USER, L"Software\\Microsoft\\ColorFiltering", L"Active", REG_DWORD, &nActive, sizeof(nActive));
        SHSetValue(HKEY_CURRENT_USER, L"Software\\Microsoft\\ColorFiltering", L"FilterType", REG_DWORD, &nType, sizeof(nType));
        ShellExecute(NULL, NULL, L"C:\\WINDOWS\\system32\\atbroker.exe", L"/colorfiltershortcut /resettransferkeys", NULL, SW_SHOWNORMAL);
        return 1;
    }

    // dectivate the color filter
    __declspec(dllexport) int APIENTRY __stdcall ResetMoniter() {
        int nActive = 0;
        SHSetValue(HKEY_CURRENT_USER, L"Software\\Microsoft\\ColorFiltering", L"Active", REG_DWORD, &nActive, sizeof(nActive));
        ShellExecute(NULL, NULL, L"C:\\WINDOWS\\system32\\atbroker.exe", L"/colorfiltershortcut /resettransferkeys", NULL, SW_SHOWNORMAL);
        return 1;
    }

    // read the activation status of color filter
    __declspec(dllexport) int APIENTRY __stdcall GetMoniterStatus() {
        DWORD value = 0;
        DWORD buffer_size = sizeof(value);
        DWORD type = REG_DWORD;
        RegGetValue(HKEY_CURRENT_USER, L"Software\\Microsoft\\ColorFiltering", L"Active", RRF_RT_REG_DWORD, &type, (PVOID) & value, &buffer_size);
        return value;
    }
}