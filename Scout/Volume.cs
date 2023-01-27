namespace Scout
{
    internal class Volume : IVolume
    {
        public string VolumeLabel { get; private set; }
        public string FileSystem { get; private set; }
        public long FreeBytes { get; private set; }
        public long TotalBytes { get; private set; }
        public bool IsReady { get; private set; }
        public DirectoryInfo RootPath { get; private set; }
        public DriveType DriveType { get; private set; }

        /// <summary>
        /// 构造器，初始化全部卷描述字段，字段是只读的，若驱动器数量变化则丢弃整个对象
        /// </summary>
        /// <param name="driveInfo">用于实例化该对象的驱动器信息</param>
        public Volume(DriveInfo driveInfo)
        {
            this.VolumeLabel = driveInfo.VolumeLabel;
            this.RootPath = driveInfo.RootDirectory;
            this.FileSystem = driveInfo.DriveFormat;
            this.FreeBytes = driveInfo.TotalFreeSpace;
            this.TotalBytes = driveInfo.TotalSize;
            this.IsReady = driveInfo.IsReady;
            this.DriveType = driveInfo.DriveType;
        }
    }
}