namespace Scout
{
    internal class Volume : IVolume
    {
        protected readonly DriveInfo _driveInfo;
        public string VolumeLabel { get => _driveInfo.VolumeLabel; }
        public string FileSystem { get => _driveInfo.DriveFormat; }
        public long FreeBytes { get => _driveInfo.TotalFreeSpace; }
        public long TotalBytes { get => _driveInfo.TotalSize; }
        public bool IsReady { get => _driveInfo.IsReady; }
        public DirectoryInfo RootPath { get => _driveInfo.RootDirectory; }
        public DriveType DriveType { get => _driveInfo.DriveType; }

        public Volume(DriveInfo driveInfo) => _driveInfo = driveInfo;
    }
}