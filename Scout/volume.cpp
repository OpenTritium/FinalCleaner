#include "pch.h"
#include "volume.h"
#include "exception.h"

const HANDLE Volume::GetVolumeHandle(void) const {
	if (this->volumeHandle != INVALID_HANDLE_VALUE) return this->volumeHandle;
	else throw new bad_volume_handle(this->driveLetter);
}

const wchar_t& Volume::GetDriveLetter(void) const {
	return this->driveLetter;
}

std::wstring_view Volume::GetRootPath(void) const {
	return this->rootPath;
}

std::wstring_view Volume::GetFileSystem(void) const {
	return this->fileSystem;
}

bool NTFSVolume::GenerateDiskIndex(void) {
	return false;
}

void NTFSVolume::RefreshDiskIndex(void) {
}

NTFSVolume::NTFSVolume(wchar_t& driveLetter) {
	this->driveLetter = driveLetter;
	this->rootPath = std::wstring(1, driveLetter) + L"://";
	std::wstring fileName = L"\\\\.\\" + rootPath.substr(0, 2);
	HANDLE volumeHandle = CreateFileW(fileName.data(), GENERIC_READ | GENERIC_WRITE,
		FILE_SHARE_READ | FILE_SHARE_WRITE, nullptr, OPEN_EXISTING, FILE_ATTRIBUTE_READONLY, nullptr);
	if (INVALID_HANDLE_VALUE != volumeHandle) this->volumeHandle = volumeHandle;
	else throw new bad_volume_handle(rootPath.at(1));
}