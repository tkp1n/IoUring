namespace IoUring
{
    public enum RingOperation : byte
    {
        Nop,
        ReadV,
        WriteV,
        Fsync,
        ReadFixed,
        WriteFixed,
        PollAdd,
        PollRemove,
        SyncFileRange,
        SendMsg,
        RecvMsg,
        Timeout,
        TimeoutRemove,
        Accept,
        Cancel,
        LinkTimeout,
        Connect,
        Fallocate,
        OpenAt,
        Close,
        FilesUpdate,
        Statx,
        Read,
        Write,
        Fadvise,
        Madvise,
        Send,
        Recv,
        OpenAt2,
        EpollCtl,
        Splice,
        ProvideBuffers,
        RemoveBuffers,
    }
}
