namespace VirtualFileSystem.Toolbox
{
    internal struct BucketDistribution
    {
        private readonly int _numberOfItemsDistributed;
        private readonly int _indexOfFirstItemTheBucketGot;
        private readonly int _bucketIndex;

        public BucketDistribution(int bucketIndex, int indexOfFirstItemItemTheBucketGot, int numberOfItemsDistributed)
        {
            _indexOfFirstItemTheBucketGot = indexOfFirstItemItemTheBucketGot;
            _bucketIndex = bucketIndex;
            _numberOfItemsDistributed = numberOfItemsDistributed;
        }

        public int BucketIndex
        {
            get { return _bucketIndex; }
        }

        public int IndexOfFirstItemTheBucketGot
        {
            get { return _indexOfFirstItemTheBucketGot; }
        }
        
        public int NumberOfItemsDistributed
        {
            get { return _numberOfItemsDistributed; }
        }
    }
}