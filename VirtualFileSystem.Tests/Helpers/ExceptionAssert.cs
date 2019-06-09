using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VirtualFileSystem.Toolbox.Extensions;

namespace VirtualFileSystem.Tests.Helpers
{
    internal static class ExceptionAssert
    {
        /// <summary>
        /// Note: стандартный атрибут ExpectedExceptionAttribute, вешающийся на тестовый метод, плох двумя вещами
        ///  - исключение требуемого типа случайно может возникнуть внутри самого тестового метода, не в части Act (а в части Arrange, скажем), будет false positive
        ///  - поймав исключение, я могу захотеть еще проверить состояние объектов, а это немного удобнее делать без try-finally в тестовом методе;
        /// </summary>
        /// <typeparam name="TException"></typeparam>
        /// <param name="codeThatMustRaiseAnException"></param>
        public static void MakeSureExceptionIsRaisedBy<TException>(Action codeThatMustRaiseAnException)
            where TException : Exception
        {
            if (codeThatMustRaiseAnException == null)
            {
                throw new ArgumentNullException("codeThatMustRaiseAnException");
            }

            bool gotTheRightException = false;
            Exception exceptionCaught = null;

            try
            {
                codeThatMustRaiseAnException.Invoke();
            }
            catch (Exception exception)
            {
                gotTheRightException = exception.GetType().Equals(typeof(TException));
                exceptionCaught = exception;
            }

            if (!gotTheRightException && (exceptionCaught != null))
            {
                throw new AssertFailedException("Ожидалось исключение типа \"{0}\". В действительности получено следующее {1}{2}".FormatWith(typeof(TException).FullName, Environment.NewLine, exceptionCaught.ToString()));
            }
            else if (exceptionCaught == null)
            {
                throw new AssertFailedException("Ожидалось исключение типа \"{0}\".".FormatWith(typeof(TException).FullName));
            }
        }
    }
}