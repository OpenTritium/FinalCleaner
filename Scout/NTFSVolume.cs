namespace Scout
{
    internal class NTFSVolume : Volume, IRecorder
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