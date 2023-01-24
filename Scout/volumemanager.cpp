#include "pch.h"
#include "volume.h"
class VolumeManager {
private:
	ushort driveCount=0;
	std::vector<std::unique_ptr<NTFSVolume>> VolumeVector;
public:
	
	VolumeManager(void){
		size_t drivesMask = GetLogicalDrives();
		while (drivesMask) {
			if (drivesMask & 1) ++driveCount;
			drivesMask >>= 1;
		}
		size_t driveStringsLength = GetLogicalDriveStringsW(0, nullptr);
		std::wstring driveStrings(driveStringsLength, L'\0');
		GetLogicalDriveStringsW(driveStringsLength, driveStrings.data());
		for (int i = 0; i <= 1; ++i) {
			auto p = std::make_unique<NTFSVolume>(driveStrings.at(4 * i));
			VolumeVector.push_back(p);
		}
	} 
};

