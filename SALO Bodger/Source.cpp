#include <iostream>
//#include <string>
//#include <sstream>
//#include <cwchar>
#include <Windows.h>
//#include <Winternl.h> // for PROCESS_BASIC_INFORMATION and ProcessBasicInformation
//#include <stdio.h>
//#include <tchar.h>
//#include <shellapi.h>
//#include <thread>
//#include <future>
//#include <cstdlib>
typedef int(*f_compile)(char*);
std::string inpFile;

/*
typedef NTSTATUS(NTAPI *PFN_NT_QUERY_INFORMATION_PROCESS) (
	IN HANDLE ProcessHandle,
	IN PROCESSINFOCLASS ProcessInformationClass,
	OUT PVOID ProcessInformation,
	IN ULONG ProcessInformationLength,
	OUT PULONG ReturnLength OPTIONAL);



WCHAR* UnicodeStringToNulTerminated(UNICODE_STRING* str)
{
	WCHAR* result;
	if (str == NULL)
		return NULL;
	result = (WCHAR*)malloc(str->Length + 2);
	if (result == NULL)
		// raise?
		return NULL;
	memcpy(result, str->Buffer, str->Length);
	result[str->Length] = L'\0';
	return result;
}

wchar_t** GetCMD(int *fieldCount, wchar_t **original) {
	HANDLE hProcess = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ,
		FALSE, GetCurrentProcessId());
	PROCESS_BASIC_INFORMATION pbi;
	ULONG ReturnLength;
	PFN_NT_QUERY_INFORMATION_PROCESS pfnNtQueryInformationProcess =
		(PFN_NT_QUERY_INFORMATION_PROCESS)GetProcAddress(
			GetModuleHandle(TEXT("ntdll.dll")), "NtQueryInformationProcess");
	NTSTATUS status = pfnNtQueryInformationProcess(
		hProcess, ProcessBasicInformation,
		(PVOID)&pbi, sizeof(pbi), &ReturnLength);
	// remove full information about my command line
	pbi.PebBaseAddress->ProcessParameters->CommandLine.Length = 0;
	UNICODE_STRING commandLine = pbi.PebBaseAddress->ProcessParameters->CommandLine;
	WCHAR *commandLineContents = new WCHAR[commandLine.Length];
	if (!ReadProcessMemory(hProcess, commandLine.Buffer,
		commandLineContents, commandLine.Length, NULL))
	{
		printf("Could not read the command line string!\n");
		return NULL;
	}

	commandLineContents[commandLine.Length - 1] = L'\0';
	//std::cout << wcslen(commandLineContents) << std::endl;

	LPWSTR *szArglist;
	int nArgs;

	szArglist = CommandLineToArgvW(commandLineContents, &nArgs);
	
	//int i;
	//if (NULL == szArglist)
	//{
	//	wprintf(L"CommandLineToArgvW failed\n");
	//	return 0;
	//}
	//else 
	//{
	//	for (i = 0; i < nArgs; i++)
	//	{
	//		printf("%d: %ws", i, szArglist[i]);
	//		printf("\n");
	//	}
	//}

	// Free memory allocated for CommandLineToArgvW arguments.
	//LocalFree(szArglist);


	CloseHandle(hProcess);
	*fieldCount = nArgs;
	*original = commandLineContents;
	return szArglist;
}

void SetCMD(const wchar_t* value, int length) {
	HANDLE hProcess = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ,
		FALSE, GetCurrentProcessId());
	PROCESS_BASIC_INFORMATION pbi;
	ULONG ReturnLength;
	PFN_NT_QUERY_INFORMATION_PROCESS pfnNtQueryInformationProcess =
		(PFN_NT_QUERY_INFORMATION_PROCESS)GetProcAddress(
			GetModuleHandle(TEXT("ntdll.dll")), "NtQueryInformationProcess");
	NTSTATUS status = pfnNtQueryInformationProcess(
		hProcess, ProcessBasicInformation,
		(PVOID)&pbi, sizeof(pbi), &ReturnLength);
	// remove full information about my command line
	//pbi.PebBaseAddress->ProcessParameters->CommandLine.Length = 0;
	UNICODE_STRING commandLine = pbi.PebBaseAddress->ProcessParameters->CommandLine;
	WCHAR* newCMD = new WCHAR[length];
	wcsncpy_s(newCMD, length, value, length);
	pbi.PebBaseAddress->ProcessParameters->CommandLine.Buffer = newCMD;
	pbi.PebBaseAddress->ProcessParameters->CommandLine.Length = length * 2 - 2;
	pbi.PebBaseAddress->ProcessParameters->CommandLine.MaximumLength = length * 2;

	CloseHandle(hProcess);
}

void ExitApp() {
	BOOL deletedFile = DeleteFile(inpFile.c_str());
	if (deletedFile == 0) {
		DWORD err = GetLastError();
		std::cout << (err == ERROR_FILE_NOT_FOUND ? "File not found" : "Access denied") << std::endl;
	}
	else {
		std::cout << "Successfully terminated SALO Bodger" << std::endl;
	}
}
DWORD WriteCode(std::string &codeText, char* appName) {
	inpFile = appName;
	inpFile += ":code";
	//Number of bytes that were written using WriteFile
	DWORD dwRet;
	HANDLE hCode = CreateFile(
		inpFile.c_str(),
		GENERIC_READ | GENERIC_WRITE,
		FILE_SHARE_READ,
		NULL,
		CREATE_ALWAYS,
		0,
		0);
	if (hCode == INVALID_HANDLE_VALUE) {
		std::cout << "Could not open stream" << std::endl;
		return EXIT_FAILURE;
	}
	else
	{
		WriteFile(hCode,			// Handle
			codeText.c_str(),		// Data to be written
			codeText.size() + 1,		// Size of data
			&dwRet,					// Number of bytes written
			NULL);					// OVERLAPPED pointer
		CloseHandle(hCode);
		hCode = INVALID_HANDLE_VALUE;
	}
}
*/
int main(int argc, char** argv) {
	HINSTANCE hGetProcIDDLL = LoadLibrary("SALOFASM.DLL");
	if (!hGetProcIDDLL) {
		std::cout << "could not load the dynamic library" << std::endl;
		return EXIT_FAILURE;
	}

	f_compile compile = (f_compile)GetProcAddress(hGetProcIDDLL, "RunFASM");
	if (!compile) {
		std::cout << "could not locate the function" << std::endl;
		return EXIT_FAILURE;
	}
	if (argc < 2 || argc > 3) {
		std::cout << "Usage: <scr_name> [dst_name]" << std::endl;
		return EXIT_SUCCESS;
	}

	std::string dest;
	if (argc == 3) {
		dest = " ";
		dest += argv[2];
	}
	else {
		dest = "";
		/*std::string fullname = argv[1];
		size_t lastindex = fullname.find_last_of(".");
		//dot not found, so don't interfere with
		if (lastindex == std::string::npos) {
			dest = "";
		}
		std::string rawname = fullname.substr(0, lastindex);*/
	}

	char* params = NULL;
	std::string parameters;// , inpCode, outFile;
	/*std::cin >> inpCode >> outFile;*/
	/*inpCode = "include \'win32ax.inc\'\r\n\
.code\r\n\
start :\r\n\
invoke  MessageBox, HWND_DESKTOP, \"May I introduce myself?\", invoke GetCommandLine, MB_YESNO\r\n\
.if eax = IDYES\r\n\
invoke  MessageBox, HWND_DESKTOP, \"Hi! I\'m the example program!\", \"Hello!\", MB_OK\r\n\
.endif\r\n\
invoke  ExitProcess, 0\r\n\
.end start\r\n";*/
	//std::string empt = "";
	//WriteCode(empt, argv[0]);

	parameters = "fasm ";
	parameters += "\"";
	parameters += argv[1];//inpFile;
	parameters += "\"";
	parameters += dest;//outFile;
	//parameters = "fasm flatTest.ASM f.exe";
	params = new char[parameters.length() + 1];
	parameters.copy(params, parameters.length() + 1);
	params[parameters.length()] = '\0';

	/*const int result_1 = std::atexit(ExitApp);
	if (result_1 != 0) {
		std::cerr << "Registration failed\n";
		return EXIT_FAILURE;
	}*/

	int retValue = compile(params);
	delete[] params;
	return retValue;
}