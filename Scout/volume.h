#pragma once
/// @brief ��ӿ�
class Volume
{
private:
	HANDLE VolumeHandle;
	wchar_t diveLetter;
	std::wstring rootPath;
	std::wstring FileSystem;
	size_t DriveType;
public:
	virtual HANDLE GetVolumeHandle() = 0;
	virtual wchar_t GetDriveLetter() = 0;
	virtual std::wstring GetRootPath() = 0;
	virtual std::wstring GetFileSystem() = 0;
	virtual void RefreshDiskIndex() = 0;  // �ǵ���ƾ�ʵ������������
	virtual ~Volume() {};
};
