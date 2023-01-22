#include "pch.h"
#include "Volume.h"

class NTFSVolume: public Volume{
public:
	bool GenerateDiskIndex(void) override;
	void RefreshDiskIndex(void) override;
};

bool NTFSVolume::GenerateDiskIndex(void)
{
	return false;
}

void NTFSVolume::RefreshDiskIndex(void)
{
}
