using static PInvoke.Kernel32;
using static PInvoke.Kernel32.ACCESS_MASK.GenericRight;
using static PInvoke.Kernel32.CreateFileFlags;
using static PInvoke.Kernel32.CreationDisposition;
using static PInvoke.Kernel32.FileShare;

namespace Scout
{
    internal class NTFSVolume : Volume, IRecorder
    {
        private SafeObjectHandle _volumeHandle;

        /// <summary>
        /// IO 控制码，决定了 DeviceIoControl 的行为
        /// </summary>
        private enum IoControlCode : int
        {
            FSCTL_ENUM_USN_DATA = 0x900b3,
            FSCTL_CREATE_USN_JOURNAL = 0x900e7,
            FSCTL_QUERY_USN_JOURNAL = 0x900f4,
        }

        //[StructLayout(LayoutKind.Sequential)]
        // 用于传入 DeviceIoControl 的参数类型，可以决定日志的属性
        private struct CREATE_USN_JOURNAL_DATA
        {
            private ulong MaximumSize;  // 为日志分配的最大大小
            private ulong AllocationDelta;  // 如果超过了，就删掉长度为该值的旧日志
        }

        public NTFSVolume(DriveInfo driveInfo) : base(driveInfo)
        {
            _volumeHandle = SafeObjectHandle.Null;
            InitVolumeHandle();
        }

        public bool GenerateVolumeIndex()
        {
            throw new NotImplementedException();
        }

        private bool CreateUSNJournal()
        {
            CREATE_USN_JOURNAL_DATA CUJD = new();  // 初始化为 0
            int RetBytes;
            unsafe
            {
                void* nullptr = default;
                OVERLAPPED* nullOverLapped = default;
                if (DeviceIoControl(_volumeHandle, (int)IoControlCode.FSCTL_CREATE_USN_JOURNAL,
                    &CUJD, 16, nullptr, 0, out RetBytes, nullOverLapped))
                    return true;
            }
            return false;
        }

        private bool InitVolumeHandle()
        {
            //申请句柄，记得关掉，记得申请管理员权限，不然要丁真
            //设备命名空间
            SafeObjectHandle volumeHandle = CreateFile(string.Concat(@"\\.\", _driveInfo.RootDirectory.ToString().AsSpan(0, 2)),
                GENERIC_READ, FILE_SHARE_WRITE | FILE_SHARE_READ, IntPtr.Zero, OPEN_EXISTING, FILE_ATTRIBUTE_READONLY, SafeObjectHandle.Null);
            if (volumeHandle.IsInvalid)
            {
                return false;
            }
            _volumeHandle = volumeHandle;
            return true;
        }
    }
}