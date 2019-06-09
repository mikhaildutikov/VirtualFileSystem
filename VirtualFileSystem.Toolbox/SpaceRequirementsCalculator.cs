using System;

namespace VirtualFileSystem.Toolbox
{
    internal static class SpaceRequirementsCalculator
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="numberOfItemsToStore"></param>
        /// <param name="chunkSize"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static int GetNumberOfChunksNeededToStoreData(int numberOfItemsToStore, int chunkSize)
        {
            MethodArgumentValidator.ThrowIfNegative(numberOfItemsToStore, "numberOfItemsToStore");
            
            if (chunkSize <= 0)
            {
                throw new ArgumentOutOfRangeException("chunkSize", "Требуется положительное число");
            }

            int remainder;

            int numberOfBlocksNeeded = Math.DivRem(numberOfItemsToStore, chunkSize, out remainder);

            if (remainder > 0)
            {
                numberOfBlocksNeeded++;
            }

            return numberOfBlocksNeeded;
        }
    }
}