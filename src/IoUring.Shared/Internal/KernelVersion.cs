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
            public static bool IoUring => Version >= new VersionTuple(5, 1);
            public static bool IORING_SETUP_CQSIZE => Version >= new VersionTuple(5, 5);
            public static bool IORING_SETUP_CLAMP => Version >= new VersionTuple(5, 6);
            public static bool IORING_SETUP_ATTACH_WQ => Version >= new VersionTuple(5, 6);
        }
    }
}