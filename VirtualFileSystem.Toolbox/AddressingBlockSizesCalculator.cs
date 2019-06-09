using System;

namespace VirtualFileSystem.Toolbox
{
    internal class AddressingBlockSizesCalculator
    {
        private readonly int _doubleIndirectBlockCapacity;
        private readonly int _singleIndirectBlockCapacity;

        public AddressingBlockSizesCalculator(int doubleIndirectBlockCapacity, int singleIndirectBlockCapacity)
        {
            MethodArgumentValidator.ThrowIfNegative(doubleIndirectBlockCapacity, "doubleIndirectBlockCapacity");
            MethodArgumentValidator.ThrowIfNegative(singleIndirectBlockCapacity, "singleIndirectBlockCapacity");

            _doubleIndirectBlockCapacity = doubleIndirectBlockCapacity;
            _singleIndirectBlockCapacity = singleIndirectBlockCapacity;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="numberOfBlocksToStore"></param>
        /// <returns></returns>
        /// <exception cref="OverflowException"></exception>
        public AddressingSystemBlockSizes GetSizesOfAddressingBlocksSufficientToStoreItems(int numberOfBlocksToStore)
        {
            MethodArgumentValidator.ThrowIfNegative(numberOfBlocksToStore, "numberOfBlocksToStore");

            if (numberOfBlocksToStore == 0)
            {
                return new AddressingSystemBlockSizes(0, 0);
            }

            int lastSingleIndirectBlockSize;
            int firstBlockSize = Math.DivRem(numberOfBlocksToStore, _singleIndirectBlockCapacity, out lastSingleIndirectBlockSize);

            if (lastSingleIndirectBlockSize > 0)
            {
                firstBlockSize++;
            }

            if (lastSingleIndirectBlockSize == 0)
            {
                lastSingleIndirectBlockSize = _singleIndirectBlockCapacity;
            }

            if (firstBlockSize > _doubleIndirectBlockCapacity)
            {
                throw new OverflowException("Невозможно распределить указанное число блоков.");
            }

            return new AddressingSystemBlockSizes(firstBlockSize, lastSingleIndirectBlockSize);
        }
    }
}