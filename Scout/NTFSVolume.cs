namespace Scout
{
    internal class NTFSVolume : Volume, IDatabase
    {
        public NTFSVolume(DriveInfo driveInfo) : base(driveInfo)
        {
        }

        public bool GenerateVolumeIndex()
        {
            throw new NotImplementedException();
        }
    }
}