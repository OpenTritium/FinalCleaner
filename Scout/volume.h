#pragma once
/// @brief �����
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
	virtual bool GenerateDiskIndex() = 0;  // �����ļ�ϵͳ�������Լ�ʵ��
	virtual void RefreshDiskIndex() = 0;  // �ǵ���ƾ�ʵ������������
	virtual ~Volume() {};
};
