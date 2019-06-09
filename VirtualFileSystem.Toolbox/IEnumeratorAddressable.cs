using System;
using System.Collections.Generic;

namespace VirtualFileSystem.Toolbox
{
    internal interface IEnumeratorAddressable<T> : IEnumerator<T>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="newPosition"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        void SetPosition(int newPosition);

        int Position { get; }

        bool IsAtLastElement { get; }

        int Count { get; }
    }
}