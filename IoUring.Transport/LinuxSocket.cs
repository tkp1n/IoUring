using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Tmds.Linux;
using static Tmds.Linux.LibC;

namespace IoUring.Transport
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
            var rv = accept4(_fd, (sockaddr*)&addr, &len, SOCK_CLOEXEC | SOCK_NONBLOCK);
            if (rv < 0)
            {
                var err = errno;
                if (err == EAGAIN || err == EWOULDBLOCK)
                {
                    endPoint = default;
                    return -1;
                }

                throw new ErrnoException(err);
            }

            endPoint = IPEndPointFormatter.AddrToIpEndPoint(&addr);
            return rv;
        }

        public static implicit operator LinuxSocket(int v) => new LinuxSocket(v);
        public static implicit operator int(LinuxSocket s) => s._fd;
    }
}