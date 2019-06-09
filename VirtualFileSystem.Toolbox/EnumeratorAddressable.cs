using System;

namespace VirtualFileSystem.Toolbox
{
    internal class EnumeratorAddressable<T> : IEnumeratorAddressable<T>
    {
        public const int IndexOfPositionBeforeFirstElement = -1;
        private readonly ListTrackingChanges<T> _elementToEnumerateOn;
        private int _currentPosition;
        private T _currentObject;
        private Int64 _listVersionWhenFirstObserved;

        public EnumeratorAddressable(ListTrackingChanges<T> elementToEnumerateOn)
        {
            if (elementToEnumerateOn == null) throw new ArgumentNullException("elementToEnumerateOn");

            _elementToEnumerateOn = elementToEnumerateOn;

            this.Reset();
        }

        private void MoveBeforeFirstElement()
        {
            _currentObject = default(T); // не лучшая идея, но вроде бы привычно - как в Enumerator-ах, выполненных Microsoft.
            _currentPosition = IndexOfPositionBeforeFirstElement;
        }

        public T Current
        {
            get
            {
                return _currentObject;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newPosition"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void SetPosition(int newPosition)
        {
            if (newPosition >= _elementToEnumerateOn.Count)
            {
                throw new ArgumentOutOfRangeException("newPosition");
            }

            this.SetPositionExtractingCurrentElement(newPosition);
        }

        public int Position
        {
            get { return _currentPosition; }
        }

        public bool IsAtLastElement
        {
            get { return _currentPosition == (_elementToEnumerateOn.Count - 1); }
        }

        public int Count
        {
            get { return _elementToEnumerateOn.Count; }
        }

        private void SetPositionExtractingCurrentElement(int newPosition)
        {
            _currentPosition = newPosition;
            _currentObject = _elementToEnumerateOn[_currentPosition];
        }

        public void Dispose()
        {
        }

        object System.Collections.IEnumerator.Current
        {
            get
            {
                return _currentObject;
            }
        }

        public bool MoveNext()
        {
            MakeSureTheListHasNotChanged();

            if (!this.IsAtLastElement)
            {
                this.SetPositionExtractingCurrentElement(_currentPosition + 1);
                return true;
            }

            return false;
        }

        private void MakeSureTheListHasNotChanged()
        {
            if (_elementToEnumerateOn.ListVersion != _listVersionWhenFirstObserved)
            {
                throw new InvalidOperationException("Продолжение перебора элементов списка невозможно: список изменился. Пересоздайте итератор.");
            }
        }

        public void Reset()
        {
            this.MoveBeforeFirstElement();
            _listVersionWhenFirstObserved = _elementToEnumerateOn.ListVersion;
        }
    }
}