#include "pch.h"
#include "volume.h"

Volume::Volume() {
	//空值记得抛异常
}

HANDLE Volume::GetVolumeHandle()
{
	std::wstring fileName = L"\\\\.\\" + rootPath.substr(0, 2);
	HANDLE volumeHandle = CreateFileW(fileName.data(), GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, nullptr,
		OPEN_EXISTING, FILE_ATTRIBUTE_READONLY, nullptr);
	if (INVALID_HANDLE_VALUE != volumeHandle) return volumeHandle;
	else throw "Invalid handle";
}

wchar_t Volume::GetDriveLetter()
{
	return L'\0';
}

std::wstring Volume::GetRootPath()
{
	return std::wstring();
}

std::wstring Volume::GetFileSystem()
{
	return std::wstring();
}