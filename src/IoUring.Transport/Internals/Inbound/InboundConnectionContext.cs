using System.Net;

namespace IoUring.Transport.Internals.Inbound
{
    internal sealed class InboundConnectionContext : IoUringConnectionContext
    {
        public InboundConnectionContext(LinuxSocket socket, EndPoint local, EndPoint remote, TransportThreadContext threadContext) 
            : base(socket, local, remote, threadContext) { }
    }
}