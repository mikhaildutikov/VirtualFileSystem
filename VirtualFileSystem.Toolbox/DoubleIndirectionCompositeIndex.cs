using System;
using VirtualFileSystem.Toolbox.Extensions;

namespace VirtualFileSystem.Toolbox
{
    /// <summary>
    /// </summary>
    internal class DoubleIndirectionCompositeIndex
    {
        private CompositeIndex _compositeIndex;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="firstIndex"></param>
        /// <param name="firstIndexCapacity"></param>
        /// <param name="secondIndex"></param>
        /// <param name="secondIndexCapacity"></param>
        /// <param name="thirdIndex"></param>
        /// <param name="thirdIndexCapacity"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public DoubleIndirectionCompositeIndex(int firstIndex, int firstIndexCapacity, int secondIndex, int secondIndexCapacity, int thirdIndex, int thirdIndexCapacity)
        {
            MethodArgumentValidator.ThrowIfNegative(firstIndex, "firstIndex");
            MethodArgumentValidator.ThrowIfNegative(secondIndex, "secondIndex");
            MethodArgumentValidator.ThrowIfNegative(thirdIndex, "thirdIndex");
            MethodArgumentValidator.ThrowIfNegative(firstIndexCapacity, "firstIndexCapacity");
            MethodArgumentValidator.ThrowIfNegative(secondIndexCapacity, "secondIndexCapacity");
            MethodArgumentValidator.ThrowIfNegative(thirdIndexCapacity, "thirdIndexCapacity");

            if (firstIndex >= firstIndexCapacity)
            {
                throw new ArgumentException("Первый индекс должен быть в пределах от 0 до {0} (не включая {0})".FormatWith(firstIndexCapacity));
            }

            if (secondIndex >= secondIndexCapacity)
            {
                throw new ArgumentException("Второй индекс должен быть в пределах от 0 до {0} (не включая {0})".FormatWith(secondIndexCapacity));
            }

            if (thirdIndex >= thirdIndexCapacity)
            {
                throw new ArgumentException("Третий индекс должен быть в пределах от 0 до {0} (не включая {0})".FormatWith(thirdIndexCapacity));
            }

            checked
            {
                try
                {
                    int initialValue = firstIndex * secondIndexCapacity * thirdIndexCapacity + secondIndex * thirdIndexCapacity +
                        thirdIndex;

                    this.SetCapacitiesAndIndex(initialValue, firstIndexCapacity, secondIndexCapacity, thirdIndexCapacity);
                }
                catch (OverflowException)
                {
                    throw new ArgumentException("Заданная комбинация индексов образует число слишком большое для текущей версии.");
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="initialValue"></param>
        /// <param name="firstIndexCapacity"></param>
        /// <param name="secondIndexCapacity"></param>
        /// <param name="thirdIndexCapacity"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public DoubleIndirectionCompositeIndex(int initialValue, int firstIndexCapacity, int secondIndexCapacity, int thirdIndexCapacity)
        {
            MethodArgumentValidator.ThrowIfNegative(initialValue, "initialValue");

            this.SetCapacitiesAndIndex(initialValue, firstIndexCapacity, secondIndexCapacity, thirdIndexCapacity);
        }

        private void SetCapacitiesAndIndex(int initialValue, int firstIndexCapacity, int secondIndexCapacity, int thirdIndexCapacity)
        {
            MethodArgumentValidator.ThrowIfNegative(firstIndexCapacity, "firstIndexCapacity");
            MethodArgumentValidator.ThrowIfNegative(secondIndexCapacity, "secondIndexCapacity");
            MethodArgumentValidator.ThrowIfNegative(thirdIndexCapacity, "thirdIndexCapacity");

            FirstIndexCapacity = firstIndexCapacity;
            SecondIndexCapacity = secondIndexCapacity;
            ThirdIndexCapacity = thirdIndexCapacity;

            _compositeIndex = new CompositeIndex(initialValue, new[] { firstIndexCapacity, secondIndexCapacity, thirdIndexCapacity });
        }

        public int FirstIndexCapacity { get; private set; }
        public int SecondIndexCapacity { get; private set; }
        public int ThirdIndexCapacity { get; private set; }

        public int Capacity
        {
            get
            {
                return _compositeIndex.Capacity;
            }
        }

        public int MaxValue
        {
            get { return _compositeIndex.MaximumValue; }
        }

        public int First
        {
            get { return _compositeIndex.CompositeValue[0]; }
        }

        public int Second
        {
            get { return _compositeIndex.CompositeValue[1]; }
        }

        public int Third
        {
            get { return _compositeIndex.CompositeValue[2]; }
        }

        public int Value
        {
            get { return _compositeIndex.Value; }
        }
    }
}