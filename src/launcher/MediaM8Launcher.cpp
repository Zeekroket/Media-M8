typedef unsigned long DWORD;
typedef int BOOL;
typedef unsigned short WORD;
typedef unsigned char BYTE;
typedef void *HANDLE;
typedef void *LPVOID;
typedef wchar_t WCHAR;
typedef WCHAR *LPWSTR;
typedef const WCHAR *LPCWSTR;

typedef struct _STARTUPINFOW {
    DWORD cb;
    LPWSTR lpReserved;
    LPWSTR lpDesktop;
    LPWSTR lpTitle;
    DWORD dwX;
    DWORD dwY;
    DWORD dwXSize;
    DWORD dwYSize;
    DWORD dwXCountChars;
    DWORD dwYCountChars;
    DWORD dwFillAttribute;
    DWORD dwFlags;
    WORD wShowWindow;
    WORD cbReserved2;
    BYTE *lpReserved2;
    HANDLE hStdInput;
    HANDLE hStdOutput;
    HANDLE hStdError;
} STARTUPINFOW;

typedef struct _PROCESS_INFORMATION {
    HANDLE hProcess;
    HANDLE hThread;
    DWORD dwProcessId;
    DWORD dwThreadId;
} PROCESS_INFORMATION;

extern "C" __declspec(dllimport) DWORD __stdcall GetModuleFileNameW(HANDLE, LPWSTR, DWORD);
extern "C" __declspec(dllimport) BOOL __stdcall CreateProcessW(
    LPCWSTR, LPWSTR, LPVOID, LPVOID, BOOL, DWORD, LPVOID, LPCWSTR, STARTUPINFOW *, PROCESS_INFORMATION *
);
extern "C" __declspec(dllimport) BOOL __stdcall CloseHandle(HANDLE);
extern "C" __declspec(dllimport) __declspec(noreturn) void __stdcall ExitProcess(DWORD);

#define CREATE_NO_WINDOW 0x08000000UL
#define PATH_CAPACITY 4096
#define COMMAND_CAPACITY 8192

static WCHAR scriptPath[PATH_CAPACITY];
static WCHAR commandLineBuffer[COMMAND_CAPACITY];
static STARTUPINFOW startupInfo;
static PROCESS_INFORMATION processInfo;
static const WCHAR commandPrefix[] =
    L"powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"";

static DWORD append_text(WCHAR *destination, DWORD capacity, DWORD position, const WCHAR *source)
{
    DWORD i = 0;
    while (source[i] != 0) {
        if (position + 1 >= capacity) { return 0xFFFFFFFFUL; }
        destination[position++] = source[i++];
    }
    destination[position] = 0;
    return position;
}

extern "C" __declspec(noreturn) void entry(void)
{
    DWORD length = GetModuleFileNameW((HANDLE)0, scriptPath, PATH_CAPACITY);
    if (length == 0 || length >= PATH_CAPACITY) { ExitProcess(10); }

    DWORD lastSeparator = 0xFFFFFFFFUL;
    DWORD lastDot = 0xFFFFFFFFUL;

    for (DWORD i = 0; i < length; ++i) {
        WCHAR ch = scriptPath[i];
        if (ch == L'\\' || ch == L'/') { lastSeparator = i; }
        else if (ch == L'.') { lastDot = i; }
    }

    if (lastDot == 0xFFFFFFFFUL || (lastSeparator != 0xFFFFFFFFUL && lastDot < lastSeparator)) {
        ExitProcess(11);
    }

    if (lastDot + 4 >= PATH_CAPACITY) { ExitProcess(12); }

    scriptPath[lastDot + 0] = L'.';
    scriptPath[lastDot + 1] = L'p';
    scriptPath[lastDot + 2] = L's';
    scriptPath[lastDot + 3] = L'1';
    scriptPath[lastDot + 4] = 0;

    DWORD position = append_text(commandLineBuffer, COMMAND_CAPACITY, 0, commandPrefix);
    if (position == 0xFFFFFFFFUL) { ExitProcess(13); }

    position = append_text(commandLineBuffer, COMMAND_CAPACITY, position, scriptPath);
    if (position == 0xFFFFFFFFUL || position + 2 >= COMMAND_CAPACITY) { ExitProcess(14); }

    commandLineBuffer[position++] = L'\"';
    commandLineBuffer[position] = 0;
    startupInfo.cb = (DWORD)sizeof(STARTUPINFOW);

    if (!CreateProcessW(
        (LPCWSTR)0, commandLineBuffer, (LPVOID)0, (LPVOID)0, 0, CREATE_NO_WINDOW,
        (LPVOID)0, (LPCWSTR)0, &startupInfo, &processInfo
    )) {
        ExitProcess(16);
    }

    CloseHandle(processInfo.hThread);
    CloseHandle(processInfo.hProcess);
    ExitProcess(0);
}
