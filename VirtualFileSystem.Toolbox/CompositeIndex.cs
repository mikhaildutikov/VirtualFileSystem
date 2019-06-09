using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using VirtualFileSystem.Toolbox.Extensions;

namespace VirtualFileSystem.Toolbox
{
    internal class CompositeIndex
    {
        private readonly ReadOnlyCollection<int> _capacitiesOfCompositeIndexes;
        private readonly int _maximumValue;
        private readonly int _capacity;
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="initialValue"></param>
        /// <param name="capacitiesOfIndexes"></param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public CompositeIndex(int initialValue, IEnumerable<int> capacitiesOfIndexes)
        {
            MethodArgumentValidator.ThrowIfNegative(initialValue, "initialValue");
            MethodArgumentValidator.ThrowIfNull(capacitiesOfIndexes, "capacitiesOfIndexes");

            if (capacitiesOfIndexes.Any(number => (number <= 0)))
            {
                throw new ArgumentException("Массив не должен содержать отрицательных чисел и нулей.", "capacitiesOfIndexes");
            }

            var ordersList = new List<int>(capacitiesOfIndexes);

            if (ordersList.Count < 1)
            {
                throw new ArgumentException("Набор должен быть непустым.", "capacitiesOfIndexes");
            }

            _capacitiesOfCompositeIndexes = ordersList.AsReadOnly();

            _maximumValue = GetMaximumCapacityForGivenSystemValidatingInput(capacitiesOfIndexes) - 1;
            _capacity = GetMaximumCapacityForGivenSystemValidatingInput(capacitiesOfIndexes);

            this.AssignNewValue(initialValue);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newValue"></param>
        /// <exception cref="ArgumentException"></exception>
        private void AssignNewValue(int newValue)
        {
            if (newValue > MaximumValue)
            {
                throw new ArgumentException("Число {0} слишком велико, чтобы быть представленным в указанной системе индексации.".FormatWith(newValue));
            }

            var indexes = new int[_capacitiesOfCompositeIndexes.Count];

            int number = newValue;

            for (int i = _capacitiesOfCompositeIndexes.Count - 1; i >= 0; i--)
            {
                int index;

                number = Math.DivRem(number, _capacitiesOfCompositeIndexes[i], out index);

                indexes[i] = index;
            }

            this.CompositeValue = indexes.ToList().AsReadOnly();
            this.Value = newValue;
        }

        public ReadOnlyCollection<int> CompositeValue
        {
            get;
            private set;
        }

        public int Capacity
        {
            get
            {
                return _capacity;
            }
        }

        public int Value
        {
            get;
            private set;
        }

        public override bool Equals(object obj)
        {
            var comparandAsCompositeIndex = obj as CompositeIndex;

            if (comparandAsCompositeIndex == null)
            {
                return false;
            }

            return this.Value.Equals(comparandAsCompositeIndex.Value);
        }

        public override int GetHashCode()
        {
            return this.Value.GetHashCode();
        }

        public ReadOnlyCollection<int> CapacitiesOfCompositeIndexes
        {
            get
            {
                return _capacitiesOfCompositeIndexes;
            }
        }

        public int MaximumValue
        {
            get { return _maximumValue; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="orderNumbers"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private static int GetMaximumCapacityForGivenSystemValidatingInput(IEnumerable<int> orderNumbers)
        {
            int maximumAddress = 0;

            foreach (int integer in orderNumbers)
            {
                if (maximumAddress == 0)
                {
                    maximumAddress = integer;
                }
                else
                {
                    try
                    {
                        checked
                        {
                            maximumAddress *= integer;
                        }
                    }
                    catch (OverflowException)
                    {
                        throw new ArgumentException("Комбинация индексов столь больших размеров в текущей версии не поддерживается", "orderNumbers");
                    }
                }
            }

            return maximumAddress;
        }
    }
}