namespace Scout
{
    internal interface IVolume
    {
        public string VolumeLabel { get; }
        public string FileSystem { get; }
        public long FreeBytes { get; }
        public long TotalBytes { get; }
        public bool IsReady { get; }
        public DirectoryInfo RootPath { get; }
        public DriveType DriveType { get; }
    }
}