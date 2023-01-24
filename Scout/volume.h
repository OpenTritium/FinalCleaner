#pragma once
/// @brief 卷基类
class Volume {
protected:
	HANDLE volumeHandle = INVALID_HANDLE_VALUE;
	wchar_t driveLetter;
	std::wstring rootPath;
	std::wstring fileSystem;
	size_t driveType;
	void GainVolumeHandle(void);

public:
	const HANDLE GetVolumeHandle(void) const;
	const wchar_t& GetDriveLetter(void) const noexcept;
	std::wstring_view GetRootPath(void) const noexcept;
	std::wstring_view GetFileSystem(void) const noexcept;
	virtual bool GenerateDiskIndex(void) = 0; // 其他文件系统交给它自己实现
	virtual void RefreshDiskIndex(void) = 0; // 记得设计卷实例容器更新器
	virtual ~Volume(void) noexcept {};
};

class NTFSVolume : public Volume {
public:
	bool GenerateDiskIndex(void) override;
	void RefreshDiskIndex(void) override;
	NTFSVolume(wchar_t& driveLetter);
};

bool NTFSVolume::GenerateDiskIndex(void) {
	return false;
}

void NTFSVolume::RefreshDiskIndex(void) {
}

NTFSVolume::NTFSVolume(wchar_t& driveLetter) {
	this->driveLetter = driveLetter;
}
