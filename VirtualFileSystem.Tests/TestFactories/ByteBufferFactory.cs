using System;
using System.IO;

namespace VirtualFileSystem.Tests.TestFactories
{
    internal static class ByteBufferFactory
    {
        public static byte[] CreateByteBufferWithAllBytesSet(int bufferSize, byte byteToPutInEachArraySlot)
        {
            byte[] array = new byte[bufferSize];

            for (int i = 0; i < array.Length; i++)
            {
                array[i] = byteToPutInEachArraySlot;
            }

            return array;
        }

        public static byte[] BuildSomeGuidsIntoByteArray(int numberOfGuidsToGenerate)
        {
            var memoryStream = new MemoryStream();

            for (int i = 0; i < numberOfGuidsToGenerate; i++)
            {
                var guidByteArray = Guid.NewGuid().ToByteArray();

                memoryStream.Write(guidByteArray, 0, guidByteArray.Length);
            }

            return memoryStream.ToArray();
        }
    }
}