using System.Runtime.InteropServices;
using static PInvoke.Kernel32;
using static PInvoke.Kernel32.ACCESS_MASK.GenericRight;
using static PInvoke.Kernel32.CreateFileFlags;
using static PInvoke.Kernel32.CreationDisposition;
using static PInvoke.Kernel32.FileShare;

namespace Scout
{
    internal sealed class NTFSVolume : Volume, IRecorder
    {
        private readonly SafeObjectHandle _volumeHandle;
        private USN_JOURNAL_DATA _USNJournalData;

        /// <summary>
        /// IO 控制码，决定了 DeviceIoControl 的行为。
        /// </summary>
        private enum IoControlCode : int
        {
            FSCTL_ENUM_USN_DATA = 0x900b3,
            FSCTL_CREATE_USN_JOURNAL = 0x900e7,
            FSCTL_QUERY_USN_JOURNAL = 0x900f4,
        }

        /// <summary>
        /// 创建 USN 日志流命令。
        /// 用于输入 DeviceIoControl 执行相应操作的参数，可以决定日志的大小。
        /// NTFS Change Journal 即 USN Journal 可以视作一个记录文件与目录更改的数据库，
        /// 每个卷都有一个这样的数据库。
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct CREATE_USN_JOURNAL_DATA
        {
            public ulong MaximumSize;  // 为日志缓冲区分配的最大空间。
            public ulong AllocationDelta;  // 如果超过了，就删掉前面指定字节数量的记录，这样后面添加的记录会被写在追加的新空间。
        }

        /// <summary>
        /// 由于 WIN8 以上系统使用 USN_JOURNAL_DATA_V1
        /// 此处使用 V1 版本（本来就没考虑过兼容 WIN10 以下的版本）
        /// USN 是更新序列号，是自增的但不连续，越小代表代表的事件发生的越早
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct USN_JOURNAL_DATA
        {
            public ulong UsnJournalID;  // 当前日志标识符，不等同 USN
            public ulong FirstUsn;  // 第一条 USN
            public ulong NextUsn;  // 下一条 USN
            public ulong LowestValidUsn; // 写入此实例日志的第一条记录
            public ulong MaxUsn;  // 最大 USN，当 NextUsn 临近此值，就该删除日志了
            public ulong MaximumSize;
            public ulong AllocationDelta;
            public ushort MinSupportedMajorVersion;  // 日志最小支持版本
            public ushort MaxSupportedMajorVersion;  // 日志最大支持版本
        }

        /// <summary>
        /// 用于输入 DeviceIoControl，决定了被枚举的 USN 数据的属性
        /// 由于 WIN8 以上系统使用 MFT_ENUM_DATA_V1
        /// 此处使用 V1 版本（本来就没考虑过兼容 WIN10 以下的版本）
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct MFT_ENUM_DATA
        {
            public ulong StartFileReferenceNumber;  //第一次设置的时候为 0，到下一页缓存的时候重新获取
            public ulong LowUsn; // USN 值的上限
            public ulong HighUsn;  // USN 值的下限
            public ushort MinMajorVersion;  // 最小日志版本
            public ushort MaxMajorVersion;  // 最大日志版本，这个可能造成返回版本不同的日志的情况
        }

        [StructLayout(LayoutKind.Explicit, Size = 8)]
        private struct LARGE_INTEGER
        {
            [FieldOffset(0)] public Int64 QuadPart;
            [FieldOffset(0)] public UInt32 LowPart;
            [FieldOffset(4)] public Int32 HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct USN_RECORD
        {
            public uint RecordLength;
            public ushort MajorVersion;
            public ushort MinorVersion;
            public ulong FileReferenceNumber;
            public ulong ParentFileReferenceNumber;
            public ulong Usn;
            private LARGE_INTEGER TimeStamp;
            private uint Reason;
            private uint SourceInfo;
            private uint SecurityId;
            private uint FileAttributes;
            private ushort FileNameLength;
            private ushort FileNameOffset;
            private char[] FileName;
        }

        internal NTFSVolume(DriveInfo driveInfo) : base(driveInfo)
        {
            _USNJournalData = new();
            _volumeHandle = CreateFile(
                string.Concat(@"\\.\", _driveInfo.RootDirectory.ToString().AsSpan(0, 2)),
                GENERIC_READ,
                FILE_SHARE_WRITE | FILE_SHARE_READ,
                IntPtr.Zero,
                OPEN_EXISTING,
                FILE_ATTRIBUTE_READONLY,
                SafeObjectHandle.Null);
            // 遇到无效句柄记得抛异常
        }

        internal bool CheckVolumeValidity() => _volumeHandle.IsInvalid;

        internal bool CreateUSNJournal()
        {
            CREATE_USN_JOURNAL_DATA CUJD = new();  // 初始化为 0
            bool isCreated;
            unsafe
            {
                isCreated = DeviceIoControl(
                    _volumeHandle,
                    (int)IoControlCode.FSCTL_CREATE_USN_JOURNAL,
                    &CUJD,
                    sizeof(CREATE_USN_JOURNAL_DATA),
                    null,
                    0,
                    out _,  // lpOverlapped 为空指针时 pBytesreturned 不能为空
                    (OVERLAPPED*)null);
            }
            return isCreated;
        }

        private bool QueryUSNJournal()
        {
            var UJD = default(USN_JOURNAL_DATA);
            bool isCreated;
            unsafe
            {
                isCreated = DeviceIoControl(
                        _volumeHandle,
                        (int)IoControlCode.FSCTL_QUERY_USN_JOURNAL,
                        null,
                        0,
                        &UJD,
                        sizeof(USN_JOURNAL_DATA),
                        out _,  // lpOverlapped 为空指针时 pBytesreturned 不能为空
                        (OVERLAPPED*)null);
            }
            if (isCreated)
            {
                _USNJournalData = UJD;
                return true;
            }
            return false;
        }

        private bool EnumUSNData()
        {
            MFT_ENUM_DATA MFTEnumData = new()
            {
                StartFileReferenceNumber = 0,
                LowUsn = _USNJournalData.FirstUsn,
                HighUsn = _USNJournalData.NextUsn,
                MinMajorVersion = _USNJournalData.MinSupportedMajorVersion,
                MaxMajorVersion = _USNJournalData.MaxSupportedMajorVersion,
                // 把日志版本卡的死死的，后面版本的结构体有变化，先停在最小支持版本
            };
            const int BUF_LEN = 0x10000;
            char[] buffer = new char[BUF_LEN];
            var pBuffer = IntPtr.Parse(buffer);
            USN_RECORD record = new();
            unsafe
            {
                while (DeviceIoControl(
                    _volumeHandle,
                    (int)IoControlCode.FSCTL_ENUM_USN_DATA,
                    &MFTEnumData,
                    sizeof(MFT_ENUM_DATA),
                    pBuffer.ToPointer(),
                    BUF_LEN,
                    out int USNDataSize,
                    (OVERLAPPED*)null))
                {
                    /* 为访问下条 USN 记录做准备 */
                    USNDataSize -= sizeof(ulong);  // USN 数据空间减去一条记录的长度
                    MFTEnumData.StartFileReferenceNumber = *(ulong*)pBuffer;  // 每次调用 FSCTL_ENUM_USN_DATA 检索后续调用的起点作为输出缓冲区中的第一个条目
                    record = *(USN_RECORD*)(pBuffer+sizeof(ulong));
                };
            }
        }

        private bool GainUSNJournal()
        {
            // StartFileReferenceNumber 被初始化为 0
            MFTEnumData.LowUsn = _USNJournalData.FirstUsn;
        }

        public bool GenerateVolumeIndex()
        {
            throw new NotImplementedException();
        }
    }
}