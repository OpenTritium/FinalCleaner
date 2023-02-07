namespace Scout
{
    /// <summary>
    /// 卷管理类需要常驻内存，负责向前台界面发送视图数据
    /// </summary>
    public static class VolumeManager
    {
        private static uint _volumeCount = (uint)DriveInfo.GetDrives().Length;
        private static readonly List<Volume> _volumeList = new();
        internal static uint VolumeCount { get => _volumeCount; }
        private static FileSystem[] _formatSeq = new FileSystem[_volumeCount];

        public enum FileSystem : byte
        {
            Others, exFAT, NTFS,
        }

        /// <summary>
        /// 实例化卷对象，并且添加到列表里
        /// </summary>
        public static void Init()
        {
            DriveInfo[] driveInfos = DriveInfo.GetDrives();
            _formatSeq = new FileSystem[_volumeCount];
            for (byte i = 0; i <= VolumeCount; ++i)
            {
                var driveInfo = driveInfos[i];
                string driveFormat = driveInfo.DriveFormat;

                #region 使用 NTFS 文件系统的情况会多得多

                if (driveFormat.CompareTo("NTFS") == 0)
                    _formatSeq[i] = FileSystem.NTFS;
                else if (driveFormat.CompareTo("exFAT") == 0)
                    _formatSeq[i] = FileSystem.exFAT;
                else
                    _formatSeq[i] = FileSystem.Others;

                #endregion NTFS 文件系统的情况会多得多

                Volume volume = new(driveInfo);
                _volumeList.Add(volume);
            }
        }

        /// <summary>
        /// 通过清除列表和计数器再初始化一遍卷对象来刷新卷管理类数据
        /// </summary>
        public static void Refresh()
        {
            _volumeList.Clear();
            _volumeCount = default;
            Init();
        }

        /// <summary>
        /// 根据文件系统来返回卷对象列表
        /// </summary>
        /// <param name="format">Others 0, exFAT 1, NTFS 2</param>
        /// <returns></returns>
        internal static List<Volume> GetVolumes(FileSystem driveFormat)
        {
            List<Volume> volumeList = new();
            for (byte i = 0; i < VolumeCount; ++i)
            {
                if (_formatSeq[i] == driveFormat)
                {
                    volumeList.Add(_volumeList[i]);
                }
            }
            return volumeList;
        }

        /// <summary>
        /// 根据驱动器类型返回卷对象列表
        /// </summary>
        /// <param name="driveType">Unknown 0,NoRootDirectory 1,Removable 2,Fixed 3,Network 4,CDRom 5,Ram 6</param>
        /// <returns></returns>
        private static List<Volume> GetVolumes(DriveType driveType)
        {
            List<Volume> volumeList = new();
            foreach (Volume volume in _volumeList)
                if (volume.DriveType == driveType) volumeList.Add(volume);
            return volumeList;
        }
    }
}