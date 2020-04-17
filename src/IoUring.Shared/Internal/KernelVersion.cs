using System;
using static IoUring.Internal.ThrowHelper;

namespace IoUring.Internal
{
    internal static class KernelVersion
    {
        private readonly struct VersionTuple : IComparable, IComparable<VersionTuple>
        {
            private readonly int _storage;

            public int Major => _storage >> 16;
            public int Minor => _storage & 0xFFFF;

            public VersionTuple(int major, int minor)
            {
                if (major > short.MaxValue) ThrowArgumentOutOfRangeException(ExceptionArgument.major);
                if (minor > short.MaxValue) ThrowArgumentOutOfRangeException(ExceptionArgument.minor);

                _storage = (major << 16) | minor;
            }

            public int CompareTo(object? obj)
            {
                if (!(obj is ValueTuple)) ThrowArgumentException(ExceptionArgument.obj);
                return _storage.CompareTo(obj);
            }

            public int CompareTo(VersionTuple other) => _storage.CompareTo(other._storage);

            public override bool Equals(object? obj)
                => obj is VersionTuple other && this._storage == other._storage;

            public override int GetHashCode() => _storage;

            public static bool operator >(VersionTuple a, VersionTuple b) => a._storage > b._storage;
            public static bool operator <(VersionTuple a, VersionTuple b) => a._storage < b._storage;
            public static bool operator >=(VersionTuple a, VersionTuple b) => a._storage >= b._storage;
            public static bool operator <=(VersionTuple a, VersionTuple b) => a._storage <= b._storage;
            public static bool operator ==(VersionTuple a, VersionTuple b) => a._storage == b._storage;
            public static bool operator !=(VersionTuple a, VersionTuple b) => a._storage != b._storage;
        }

        private static readonly VersionTuple Version = new VersionTuple(Environment.OSVersion.Version.Major, Environment.OSVersion.Version.Minor);

        public static class Supports
        {
            private static bool AtLeast(int major, int minor) => Version >= new VersionTuple(major, minor);

            public static bool IoUring => AtLeast(5, 1);

            public static bool IOSQE_FIXED_FILE => AtLeast(5, 1);
            public static bool IOSQE_IO_DRAIN => AtLeast(5, 2);
            public static bool IOSQE_IO_LINK => AtLeast(5, 3);
            public static bool IOSQE_IO_HARDLINK => AtLeast(5, 5);
            public static bool IOSQE_ASYNC => AtLeast(5, 6);

            public static bool IORING_SETUP_IOPOLL => AtLeast(5, 1);
            public static bool IORING_SETUP_SQPOLL => AtLeast(5, 1);
            public static bool IORING_SETUP_SQ_AFF => AtLeast(5, 1);
            public static bool IORING_SETUP_CQSIZE => AtLeast(5, 5);
            public static bool IORING_SETUP_CLAMP => AtLeast(5, 6);
            public static bool IORING_SETUP_ATTACH_WQ => AtLeast(5, 6);

            public static bool IORING_OP_NOP => AtLeast(5, 1);
            public static bool IORING_OP_READV => AtLeast(5, 1);
            public static bool IORING_OP_WRITEV => AtLeast(5, 1);
            public static bool IORING_OP_FSYNC => AtLeast(5, 1);
            public static bool IORING_OP_READ_FIXED => AtLeast(5, 1);
            public static bool IORING_OP_WRITE_FIXED => AtLeast(5, 1);
            public static bool IORING_OP_POLL_ADD => AtLeast(5, 1);
            public static bool IORING_OP_POLL_REMOVE => AtLeast(5, 1);

            public static bool IORING_OP_SYNC_FILE_RANGE => AtLeast(5, 2);

            public static bool IORING_OP_SENDMSG => AtLeast(5, 3);
            public static bool IORING_OP_RECVMSG => AtLeast(5, 3);

            public static bool IORING_OP_TIMEOUT => AtLeast(5, 4);

            public static bool IORING_OP_TIMEOUT_REMOVE => AtLeast(5, 5);
            public static bool IORING_OP_ACCEPT => AtLeast(5, 5);
            public static bool IORING_OP_ASYNC_CANCEL => AtLeast(5, 5);
            public static bool IORING_OP_LINK_TIMEOUT => AtLeast(5, 5);
            public static bool IORING_OP_CONNECT => AtLeast(5, 5);

            public static bool IORING_OP_FALLOCATE => AtLeast(5, 6);
            public static bool IORING_OP_OPENAT => AtLeast(5, 6);
            public static bool IORING_OP_CLOSE => AtLeast(5, 6);
            public static bool IORING_OP_FILES_UPDATE => AtLeast(5, 6);
            public static bool IORING_OP_STATX => AtLeast(5, 6);
            public static bool IORING_OP_READ => AtLeast(5, 6);
            public static bool IORING_OP_WRITE => AtLeast(5, 6);
            public static bool IORING_OP_FADVISE => AtLeast(5, 6);
            public static bool IORING_OP_MADVISE => AtLeast(5, 6);
            public static bool IORING_OP_SEND => AtLeast(5, 6);
            public static bool IORING_OP_RECV => AtLeast(5, 6);
            public static bool IORING_OP_OPENAT2 => AtLeast(5, 6);
            public static bool IORING_OP_EPOLL_CTL => AtLeast(5, 6);

            public static bool IORING_REGISTER_BUFFERS => AtLeast(5, 1);
            public static bool IORING_REGISTER_FILES => AtLeast(5, 1);
            public static bool IORING_REGISTER_EVENTFD => AtLeast(5, 2);
            public static bool IORING_REGISTER_FILES_UPDATE => AtLeast(5, 5);
            public static bool IORING_REGISTER_EVENTFD_ASYNC => AtLeast(5, 6);
            public static bool IORING_REGISTER_PROBE => AtLeast(5, 6);
            public static bool IORING_REGISTER_PERSONALITY => AtLeast(5, 6);
        }
    }
}