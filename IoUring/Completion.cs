namespace IoUring
{
    public readonly struct Completion
    {
        /// <summary>
        /// Return value of the completed operation or -errno
        /// </summary>
        public readonly int res;

        /// <summary>
        /// User data supplied, when submitting the operation
        /// </summary>
        public readonly ulong userData;

        public Completion(int res, ulong userData)
        {
            this.res = res;
            this.userData = userData;
        }

        public void Deconstruct(out int res, out ulong userData)
        {
            res = this.res;
            userData = this.userData;
        }
    }
}