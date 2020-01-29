using System.Net;
using Microsoft.AspNetCore.Connections.Features;

namespace IoUring.Transport.Internals.Outbound
{
    internal sealed class OutboundConnectionContext : IoUringConnectionContext, IConnectionInherentKeepAliveFeature
    {
        public OutboundConnectionContext(LinuxSocket socket, EndPoint remote, TransportThreadContext threadContext)
            : base(socket, null, remote, threadContext)
        {
            // Add IConnectionInherentKeepAliveFeature to the tcp connection impl since Kestrel doesn't implement
            // the IConnectionHeartbeatFeature
            Features.Set<IConnectionInherentKeepAliveFeature>(this);
        }

        // We claim to have inherent keep-alive so the client doesn't kill the connection when it hasn't seen ping frames.
        public bool HasInherentKeepAlive => true;
    }
}