namespace Scout
{
    public static class VolumeManager
    {

        public static int VolumeCount { get; private set; }
        internal static List<Volume>? VolumeList { get; private set; }

        public static void Init()
        {
            VolumeCount = DriveInfo.GetDrives().Length;
            DriveInfo[] driveInfos = DriveInfo.GetDrives();
            VolumeList = new();
            foreach (DriveInfo driveInfo in driveInfos)
            {
                VolumeList.Add(new Volume(driveInfo));
            }
        }

    }
}