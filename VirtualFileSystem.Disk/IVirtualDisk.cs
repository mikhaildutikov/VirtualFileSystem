using System;

namespace VirtualFileSystem.Disk
{
    internal interface IVirtualDisk : IDisposable
    {
        /// <summary>
        /// Размер дискового блока в байтах
        /// </summary>
        int BlockSizeInBytes { get; }

        /// <summary>
        /// Размер виртуального диска в байтах
        /// </summary>
        long DiskSizeInBytes { get; }

        /// <summary>
        /// Количество дисковых блоков
        /// </summary>
        int NumberOfBlocks { get; }

        /// <summary>
        /// Читает содержимое блока (полностью)
        /// </summary>
        /// <param name="indexOfBlockToReadFrom">Индекс дискового блока, содержимое которого надо прочесть</param>
        /// <returns>Байтовый массив с содержимым блока</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        byte[] ReadAllBytesFromBlock(int indexOfBlockToReadFrom);

        /// <summary>
        /// Читает часть (впрочем, можно и все прочесть) содержимого блока в байтовый массив
        /// </summary>
        /// <param name="indexOfBlockToReadFrom">Индекс дискового блока, содержимое которого надо прочесть</param>
        /// <param name="startingPosition">Позиция в блоке, указывающее, с какого места начинать чтение данных</param>
        /// <param name="numberOfBytesToRead">Количество байт, начиная с <paramref name="startingPosition"/>, которое следует прочесть</param>
        /// <returns>Байтовый массив, содержащий прочитанные данные.</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        byte[] ReadBytesFromBlock(int indexOfBlockToReadFrom, int startingPosition, int numberOfBytesToRead);
        
        /// <summary>
        /// Пишет байты из указанного массива в дисковый блок (с самого начала блока и дальше)
        /// </summary>
        /// <param name="indexOfBlockToWriteTo">Индекс дискового блока, в который следует записать данные</param>
        /// <param name="bytesToWrite">Байтовый массив, содержащий данные, которые следует записать</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        void WriteBytesToBlock(int indexOfBlockToWriteTo, byte[] bytesToWrite);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="startingBlockIndex"></param>
        /// <param name="arrayOffset"></param>
        /// <param name="blockOffset"></param>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException"></exception>
        int WriteBytesContinuoslyStartingFromBlock(byte[] bytes, int startingBlockIndex, int arrayOffset, int blockOffset);

        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        void WriteBytesToBlock(int blockIndex, byte[] bytesToWrite, int arrayOffset, int blockOffset, int numberOfBytesToWrite);
    }
}