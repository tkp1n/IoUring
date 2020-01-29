using System.Net;
using Microsoft.AspNetCore.Routing.Matching;
using Tmds.Linux;
using static Tmds.Linux.LibC;

namespace IoUring.Transport.Internals
{
    internal readonly struct LinuxSocket
    {
        private readonly int _fd;

        public LinuxSocket(int fd)
        {
            _fd = fd;
        }

        public unsafe void SetOption(int level, int option, int value)
        {
            var rv = setsockopt(_fd, level, option, (byte*) &value, 4);
            if (rv != 0) throw new ErrnoException(errno);
        }

        public unsafe bool TryConnectNonBlocking(IPEndPoint endPoint)
        {
            sockaddr_storage addr;
            endPoint.ToSockAddr(&addr, out var length);
            var rv = connect(_fd, (sockaddr*) &addr, length);
            if (rv == 0) return true;

            var result = errno;
            if (result == EINPROGRESS) return false;
            
            throw new ErrnoException(result);
        }

        public unsafe void Bind(IPEndPoint endPoint)
        {
            sockaddr_storage addr;
            endPoint.ToSockAddr(&addr, out var length);
            var rv = bind(_fd, (sockaddr*) &addr, length);
            if (rv < 0) throw new ErrnoException(errno);
        }

        public void Listen(int backlog)
        {
            var rv = listen(_fd, backlog);
            if (rv < 0) throw new ErrnoException(errno);
        }

        public unsafe LinuxSocket Accept(out IPEndPoint endPoint)
        {
            sockaddr_storage addr;
            socklen_t len = SizeOf.sockaddr_storage;
            var rv = accept4(_fd, (sockaddr*)&addr, &len, SOCK_NONBLOCK | SOCK_CLOEXEC);
            if (rv < 0)
            {
                var err = errno;
                if (err == EAGAIN || err == EWOULDBLOCK || err == EINTR)
                {
                    endPoint = default;
                    return -1;
                }

                throw new ErrnoException(err);
            }

            endPoint = IPEndPointFormatter.AddrToIpEndPoint(&addr);
            return rv;
        }

        public unsafe EndPoint GetLocalAddress()
        {
            sockaddr_storage addr;
            socklen_t length = SizeOf.sockaddr_storage;
            if (getsockname(_fd, (sockaddr*) &addr, &length) != 0) throw new ErrnoException(errno);
            return IPEndPointFormatter.AddrToIpEndPoint(&addr);
        }

        public unsafe EndPoint GetPeerAddress()
        {
            sockaddr_storage addr;
            socklen_t length = SizeOf.sockaddr_storage;
            if (getpeername(_fd, (sockaddr*) &addr, &length) != 0) throw new ErrnoException(errno);
            return IPEndPointFormatter.AddrToIpEndPoint(&addr);
        }

        public unsafe int GetReadableBytes() // TODO avoid if possible
        {
            int readableBytes;
            int rv = ioctl(_fd, FIONREAD, &readableBytes);
            if (rv == -1)
            {
                throw new ErrnoException(errno);
            }

            return readableBytes;
        }

        public void Close() => close(_fd);
        
        public static implicit operator LinuxSocket(int v) => new LinuxSocket(v);
        public static implicit operator int(LinuxSocket s) => s._fd;
    }
}