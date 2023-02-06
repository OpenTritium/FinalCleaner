using System.Runtime.InteropServices;
using static PInvoke.Kernel32;
using static PInvoke.Kernel32.ACCESS_MASK.GenericRight;
using static PInvoke.Kernel32.CreateFileFlags;
using static PInvoke.Kernel32.CreationDisposition;
using static PInvoke.Kernel32.FileShare;

// 记得处理句柄，还有未托管内存！！！

namespace Scout
{
    internal sealed class NtfsVolume : Volume, IRecorder
    {
        private readonly SafeObjectHandle _volumeHandle;
        private UsnJournalData _USNJournalData;

        /// <summary>
        /// IO 控制码，决定了 DeviceIoControl 的行为。
        /// </summary>
        private enum IoControlCode
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
        private struct CreateUsnJournalData
        {
            public ulong MaximumSize;  // 为日志缓冲区分配的最大空间。
            public ulong AllocationDelta;  // 如果超过了，就删掉前面指定字节数量的记录，这样后面添加的记录会被写在追加的新空间。
        }

        /// <summary>
        /// 由于 WIN8 以上系统使用 UsnJournalData_V1
        /// 此处使用 V1 版本（本来就没考虑过兼容 WIN10 以下的版本）
        /// USN 是更新序列号，是自增的但不连续，越小代表代表的事件发生的越早
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct UsnJournalData
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
        /// 由于 WIN8 以上系统使用 MftEnumData_V1
        /// 此处使用 V1 版本（本来就没考虑过兼容 WIN10 以下的版本）
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct MftEnumData
        {
            public ulong StartFileReferenceNumber;  //第一次设置的时候为 0，到下一页缓存的时候重新获取
            public ulong LowUsn; // USN 值的上限
            public ulong HighUsn;  // USN 值的下限
            public ushort MinMajorVersion;  // 最小日志版本
            public ushort MaxMajorVersion;  // 最大日志版本，这个可能造成返回版本不同的日志的情况
        }

        /// <summary>
        /// UsnRecord_V2
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct UsnRecord
        {
            public uint RecordLength;
            public ushort MajorVersion;
            public ushort MinorVersion;
            public ulong FileReferenceNumber;
            public ulong ParentFileReferenceNumber;
            public ulong Usn;
            public ulong TimeStamp;
            public uint Reason;
            public uint SourceInfo;
            public uint SecurityId;
            public uint FileAttributes;
            public ushort FileNameLength;
            public ushort FileNameOffset;
            public char[] FileName;  // 这里的 char 是 Unicode，但是系统提供的应该是 ANSI 的
        }

        /// <summary>
        /// 我们需要一种数据结构，描述当前文件节点信息
        /// </summary>
        private struct FileInfoNode
        {
            public ulong parentRef;  // 上级目录文件索引号
            public string? fileName;
            public ulong timeStamp;
        }

        internal NtfsVolume(DriveInfo driveInfo) : base(driveInfo)
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
            CreateUsnJournalData CUJD = new();  // 初始化为 0
            bool isCreated;
            unsafe
            {
                isCreated = DeviceIoControl(
                    _volumeHandle,
                    (int)IoControlCode.FSCTL_CREATE_USN_JOURNAL,
                    &CUJD,
                    sizeof(CreateUsnJournalData),
                    null,
                    0,
                    out _,  // lpOverlapped 为空指针时 pBytesreturned 不能为空
                    (OVERLAPPED*)null);
            }
            return isCreated;
        }

        private bool QueryUSNJournal()
        {
            var UJD = default(UsnJournalData);
            bool isCreated;
            unsafe
            {
                isCreated = DeviceIoControl(
                        _volumeHandle,
                        (int)IoControlCode.FSCTL_QUERY_USN_JOURNAL,
                        null,
                        0,
                        &UJD,
                        sizeof(UsnJournalData),
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
            MftEnumData mft = new()
            {
                StartFileReferenceNumber = 0,
                LowUsn = _USNJournalData.FirstUsn,
                HighUsn = _USNJournalData.NextUsn,
                MinMajorVersion = 2,
                MaxMajorVersion = 2,
                // 把日志版本卡的死死的，后面版本的结构体有变化，先停在最小支持版本
            };

            IntPtr pMft = Marshal.AllocHGlobal(Marshal.SizeOf(mft));  // 为输入缓存准备非托管内存
            Marshal.StructureToPtr<MftEnumData>(mft, pMft, true);  // 指向结构体的指针
            const int bufferSize = 131027;  // 128KB
            IntPtr pBuffer = Marshal.AllocHGlobal(bufferSize);  // 为输出缓存分配非托管内存
            Dictionary<ulong, FileInfoNode> FileInfoMap = new();
            FileInfoNode Node = new();

            while (DeviceIoControl(
                    _volumeHandle,
                    (int)IoControlCode.FSCTL_ENUM_USN_DATA,
                    pMft,
                    Marshal.SizeOf<MftEnumData>(mft),
                    pBuffer,
                    bufferSize,
                    out int USNDataSize,
                    IntPtr.Zero))
            {
                /* 为访问下条 USN 记录做准备 */
                var RetBytes = Convert.ToUInt32(USNDataSize);  
                // 奶奶滴微软的文档什么时候能统一一下，有的写的是 uint 有的写的是 int
                unsafe
                {
                    RetBytes -= sizeof(ulong);  // USN 数据空间减去一条记录的长度
                    IntPtr pUsnRecord = new IntPtr(pBuffer.ToInt64());
                    while (RetBytes > 0)
                    {
                        var usnRecord = Marshal.PtrToStructure<UsnRecord>(pUsnRecord);
                        Node.fileName = usnRecord.FileName.ToString();
                        Node.parentRef = usnRecord.ParentFileReferenceNumber;
                        Node.timeStamp = usnRecord.TimeStamp;
                        FileInfoMap[usnRecord.FileReferenceNumber] = Node;
                        var recordLen = usnRecord.RecordLength;
                        RetBytes -= recordLen;
                        pUsnRecord = IntPtr.Subtract(pUsnRecord, (int)recordLen);  // 读下条 USN 日志
                    }
                    mft.StartFileReferenceNumber = (ulong)Marshal.ReadInt64(pBuffer, 0);
                    // 每次调用 FSCTL_ENUM_USN_DATA 检索后续调用的起点作为输出缓冲区中的第一个条目
                }
            }
            return true;
        }

        public bool GenerateVolumeIndex()
        {
            throw new NotImplementedException();
        }
    }
}