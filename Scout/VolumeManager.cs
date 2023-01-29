namespace Scout
{
    public static class VolumeManager
    {
        public static int VolumeCount { get; private set; }
        internal static List<Volume> VolumeList { get; private set; }
        private static FileSystem[] _formatSeq;

        public enum FileSystem : sbyte
        {
            Others = -1, exFAT = 0, NTFS,
        }

        static VolumeManager()
        {
            VolumeList = new();
            VolumeCount = default;
            _formatSeq = new FileSystem[GetVolumeCount()];
        }

        private static int GetVolumeCount() => VolumeCount = DriveInfo.GetDrives().Length;

        /// <summary>
        /// Initialize the volume management static class.
        /// </summary>
        public static void Init()
        {
            DriveInfo[] driveInfos = DriveInfo.GetDrives();
            _formatSeq = new FileSystem[GetVolumeCount()];
            for (byte i = 0; i <= VolumeCount; ++i)
            {
                var driveInfo = driveInfos[i];
                string driveFormat = driveInfo.DriveFormat;
                // 按优先级分支
                if (driveFormat.CompareTo("NTFS") == 0)
                    _formatSeq[i] = FileSystem.NTFS;
                else if (driveFormat.CompareTo("exFAT") == 0)
                    _formatSeq[i] = FileSystem.exFAT;
                else
                    _formatSeq[i] = FileSystem.Others;
                // 实例化卷对象
                Volume volume = new(driveInfo);
                VolumeList.Add(volume);
            }
        }

        /// <summary>
        /// Refresh volume management static class.
        /// </summary>
        public static void Refresh()
        {
            VolumeList.Clear();
            VolumeCount = default;
            Init();
        }

        /// <summary>
        /// Return available volume objects by file system.
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        internal static List<Volume> GetVolumes(FileSystem driveFormat)
        {
            List<Volume> volumeList = new();
            for (byte i = 0; i < VolumeCount; ++i)
            {
                if (_formatSeq[i] == driveFormat)
                volumeList.Add(VolumeList[i]);
            }
            return volumeList;
        }

        /// <summary>
        /// Returns the available volume objects by drive type.
        /// </summary>
        /// <param name="driveType"></param>
        /// <returns></returns>
        private static List<Volume> GetVolumes(DriveType driveType)
        {
            List<Volume> volumeList = new();
            foreach (Volume volume in VolumeList)
                if (volume.DriveType == driveType) volumeList.Add(volume);
            return volumeList;
        }
    }
}