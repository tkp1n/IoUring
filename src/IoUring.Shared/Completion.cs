namespace IoUring
{
    /// <summary>
    /// Data associated with a Completion Queue Event
    /// </summary>
    public readonly struct Completion
    {
        /// <summary>
        /// Return value of the completed operation or -errno
        /// </summary>
        public readonly int result;

        /// <summary>
        /// User data supplied, when submitting the operation
        /// </summary>
        public readonly ulong userData;

        public Completion(int result, ulong userData)
        {
            this.result = result;
            this.userData = userData;
        }

        public void Deconstruct(out int result, out ulong userData)
        {
            result = this.result;
            userData = this.userData;
        }
    }
}