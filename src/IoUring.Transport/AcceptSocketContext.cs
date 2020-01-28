using System.Net;
using System.Threading.Channels;
using Microsoft.AspNetCore.Connections;

namespace IoUring.Transport
{
    internal class AcceptSocketContext
    {
        public AcceptSocketContext(LinuxSocket socket, IPEndPoint endPoint, ChannelWriter<ConnectionContext> acceptQueue)
        {
            EndPoint = endPoint;
            AcceptQueue = acceptQueue;
            Socket = socket;
        }

        public LinuxSocket Socket { get; }
        public IPEndPoint EndPoint { get; }
        public ChannelWriter<ConnectionContext> AcceptQueue { get; }
    }
}