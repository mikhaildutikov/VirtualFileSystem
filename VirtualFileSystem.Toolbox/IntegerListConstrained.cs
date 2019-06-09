using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using VirtualFileSystem.Toolbox.Extensions;

namespace VirtualFileSystem.Toolbox
{
    internal class IntegerListConstrained : IEnumerable<int>
    {
        private const int IntegerSizeInBytes = sizeof(Int32);
        private readonly int _maximumCountOfIntegers;
        private readonly ListTrackingChanges<int> _integers = new ListTrackingChanges<int>(new List<int>());

        /// <summary>
        /// 
        /// </summary>
        /// <param name="integersAsByteArrayLittleEndian"></param>
        /// <param name="currentCountOfIntegers"></param>
        /// <param name="maximumCountOfIntegers"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public IntegerListConstrained(byte[] integersAsByteArrayLittleEndian, int currentCountOfIntegers, int maximumCountOfIntegers)
        {
            MethodArgumentValidator.ThrowIfNegative(currentCountOfIntegers, "currentCountOfIntegers");
            MethodArgumentValidator.ThrowIfNegative(maximumCountOfIntegers, "maximumCountOfIntegers");
            MethodArgumentValidator.ThrowIfNull(integersAsByteArrayLittleEndian, "integersAsByteArrayLittleEndian");

            if (currentCountOfIntegers * IntegerSizeInBytes > integersAsByteArrayLittleEndian.Length)
            {
                throw new ArgumentException("Из массива байт не удастся прочитать следующее число целых: {0}. Проверьте аргументы.".FormatWith(currentCountOfIntegers), "currentCountOfIntegers");
            }

            if (maximumCountOfIntegers < currentCountOfIntegers)
            {
                throw new ArgumentException("Максимальное число целых в списке не может быть меньше текущего числа элементов.".FormatWith(currentCountOfIntegers), "currentCountOfIntegers, maximumCountOfIntegers");
            }

            this.FillInInternalListOfIntegers(integersAsByteArrayLittleEndian, currentCountOfIntegers);

            _maximumCountOfIntegers = maximumCountOfIntegers;
        }

        private void FillInInternalListOfIntegers(byte[] integersAsByteArrayLittleEndian, int currentCountOfIntegers)
        {
            var stream = new MemoryStream(integersAsByteArrayLittleEndian);
            var binaryReader = new BinaryReader(stream);

            for (int i = 0; i < currentCountOfIntegers; i++)
            {
                int newInteger = binaryReader.ReadInt32();

                if (newInteger < 0)
                {
                    throw new ArgumentException("В массиве не должно быть отрицательных целых чисел.", "integersAsByteArrayLittleEndian");
                }

                _integers.Add(newInteger);
            }
        }

        public int MaximumCount
        {
            get
            {
                return _maximumCountOfIntegers;
            }
        }

        public bool IsFull
        {
            get { return (_integers.Count == _maximumCountOfIntegers); }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newInteger"></param>
        /// <exception cref="InvalidOperationException">Если размер набора целых чисел достиг максимума.</exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void AddInteger(int newInteger)
        {
            MethodArgumentValidator.ThrowIfNegative(newInteger, "newInteger");

            if (_integers.Count == _maximumCountOfIntegers)
            {
                throw new InvalidOperationException("В списке чисел не может быть больше следующего числа элементов: {0}.".FormatWith(_maximumCountOfIntegers));
            }

            // Note: можно было бы тут же и сохранять, обернув класс декоратором, как я сделал в нескольких других местах.

            _integers.Add(newInteger);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newSize"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public ReadOnlyCollection<int> Shrink(int newSize)
        {
            MethodArgumentValidator.ThrowIfNegative(newSize, "newSize");

            if (newSize > _integers.Count)
            {
                throw new ArgumentOutOfRangeException("newSize", "Новый размер массива должен быть меньше текущего.");
            }

            var removedItems = new List<int>();

            while (_integers.Count != newSize)
            {
                int lastIntegerIndex = _integers.Count - 1;

                removedItems.Add(_integers[lastIntegerIndex]);
                _integers.RemoveAt(lastIntegerIndex);
            }

            return removedItems.AsReadOnly();
        }

        public int Count
        {
            get
            {
                return _integers.Count;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public int this[int index]
        {
            get
            {
                return _integers[index];
            }
            set
            {
                MethodArgumentValidator.ThrowIfNegative(value, "value");

                if (index >= _integers.Count)
                {
                    throw new ArgumentOutOfRangeException("index");
                }

                _integers[index] = value;
            }
        }

        /// <summary>
        /// Note: Little Endian.
        /// </summary>
        /// <returns></returns>
        public byte[] ToByteArray()
        {
            var array = new byte[_integers.Count * IntegerSizeInBytes];

            var stream = new MemoryStream(array, true);
            var writer = new BinaryWriter(stream);

            foreach (int integer in _integers)
            {
                writer.Write(integer);
            }

            return stream.ToArray();
        }

        public IEnumerator<int> GetEnumerator()
        {
            return _integers.GetEnumerator();
        }

        public EnumeratorAddressable<int> GetAddressableEnumerator()
        {
            return new EnumeratorAddressable<int>(_integers);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}