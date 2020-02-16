namespace IoUring
{
    public enum TimeoutOptions : uint
    {
        /// <summary>
        /// Given timeout should be interpreted as relative value.
        /// </summary>
        Relative = 0,
        
        /// <summary>
        /// Given timeout should be interpreted as absolute value.
        /// </summary>
        Absolute = 1, // IORING_TIMEOUT_ABS
    }
}