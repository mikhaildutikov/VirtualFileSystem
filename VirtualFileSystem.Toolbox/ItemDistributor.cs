using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using VirtualFileSystem.Toolbox.Extensions;

namespace VirtualFileSystem.Toolbox
{
    internal static class ItemDistributor
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="numberOfItemsToDistribute"></param>
        /// <param name="firstBucketRemainingCapacity"></param>
        /// <param name="bucketCapacity"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static ReadOnlyCollection<BucketDistribution> Distribute(int numberOfItemsToDistribute, int firstBucketRemainingCapacity, int bucketCapacity)
        {
            MethodArgumentValidator.ThrowIfNegative(numberOfItemsToDistribute, "numberOfItemsToDistribute");
            MethodArgumentValidator.ThrowIfNegative(firstBucketRemainingCapacity, "firstBucketRemainingCapacity");
            MethodArgumentValidator.ThrowIfNegative(bucketCapacity, "bucketCapacity");

            if (firstBucketRemainingCapacity > bucketCapacity)
            {
                throw new ArgumentException("Первая из ячеек не может иметь места для еще {0} элемент(-а,-ов), так как каждая ячейка содержит, максимум, {1} элементов".FormatWith(firstBucketRemainingCapacity, bucketCapacity));
            }

            var distributionResult = new List<BucketDistribution>();
            int bucketIndex = 0;

            if (firstBucketRemainingCapacity < numberOfItemsToDistribute)
            {
                if (firstBucketRemainingCapacity != 0)
                {
                    distributionResult.Add(new BucketDistribution(bucketIndex, bucketCapacity - firstBucketRemainingCapacity, firstBucketRemainingCapacity));
                    numberOfItemsToDistribute -= firstBucketRemainingCapacity;
                }
            }
            else
            {
                distributionResult.Add(new BucketDistribution(bucketIndex, bucketCapacity - firstBucketRemainingCapacity, numberOfItemsToDistribute));
                return distributionResult.AsReadOnly();
            }

            int remainder;
            int fullyFilledBuckets = Math.DivRem(numberOfItemsToDistribute, bucketCapacity, out remainder);

            bucketIndex++;

            for (int i = 0; i < fullyFilledBuckets; i++)
            {
                distributionResult.Add(new BucketDistribution(bucketIndex, 0, bucketCapacity));
                bucketIndex++;
            }

            if (remainder > 0)
            {
                distributionResult.Add(new BucketDistribution(bucketIndex, 0, remainder));
            }

            return distributionResult.AsReadOnly();
        }
    }
}