#pragma once
/// @brief 卷基类
class Volume
{
private:
	HANDLE VolumeHandle;
	wchar_t diveLetter;
	std::wstring rootPath;
	std::wstring FileSystem;
	size_t DriveType;
	HANDLE GainVolumeHandle();
public:
	HANDLE GetVolumeHandle();
	wchar_t GetDriveLetter();
	std::wstring GetRootPath();
	std::wstring GetFileSystem();
	virtual bool GenerateDiskIndex() = 0;  // 其他文件系统交给它自己实现
	virtual void RefreshDiskIndex() = 0;  // 记得设计卷实例容器更新器
	virtual ~Volume() {};
};
