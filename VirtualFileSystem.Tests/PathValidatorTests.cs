using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using VirtualFileSystem.Tests.Helpers;

namespace VirtualFileSystem.Tests
{
    [TestClass]
    public class PathValidatorTests
    {
        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        [TestMethod]
        public void MakeSureYouCannotConstructAValidatorWithInvalidCharactersAsPartOfTheRoot()
        {
            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentException>(
                delegate
                    {
                        new PathValidator("12", new char[] { '1', '2' }, MockRepository.GenerateStub<IFileSystemArtifactNamesValidator>(), '/');
                    });
        }

        [TestMethod]
        public void MakeSureYouCannotConstructAValidatorWithInvalidCharacterAsDirectorySeparator()
        {
            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentException>(
                delegate
                {
                    new PathValidator("root", new char[] { '1', '2' }, MockRepository.GenerateStub<IFileSystemArtifactNamesValidator>(), '2');
                });
        }

        [TestMethod]
        public void TestValidatorOnDataSuite1()
        {
            var validator = new PathValidator("root", new char[] { '(', ')', '-' }, MockRepository.GenerateStub<IFileSystemArtifactNamesValidator>(), '/');

            ExceptionAssert.MakeSureExceptionIsRaisedBy<InvalidPathException>(
                delegate
                {
                    validator.Validate("/-dfngsfdjgliulirhliuhrelteriih4u3jjoj43o");
                });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<InvalidPathException>(
                delegate
                {
                    validator.Validate("(");
                });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<InvalidPathException>(
                delegate
                {
                    validator.Validate("';'';';';';';'')");
                });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<InvalidPathException>(
                delegate
                {
                    validator.Validate("root+mumboJumbo");
                });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<InvalidPathException>(
                delegate
                {
                    validator.Validate("rootroot@$#@%$@#^#%$^&$%&%^*%&^");
                });

            validator.Validate("root");
            validator.Validate("root/kfmlksmkl/rootlemlgmel");
            validator.Validate("root/root/root");
            validator.Validate("root/root/root");
        }

        [TestMethod]
        public void TestValidatorOnDataSuite2()
        {
            var namesValidator = new FileSystemArtifactNamesValidator(Constants.IllegalCharactersForNames, Constants.FileAndFolderMaximumNameLength);
            var validator = new PathValidator(VirtualFileSystem.Root, Constants.IllegalCharactersForPaths, namesValidator, VirtualFileSystem.DirectorySeparatorChar);

            validator.Validate(VirtualFileSystem.Root);

            ExceptionAssert.MakeSureExceptionIsRaisedBy<InvalidPathException>(
                delegate
                {
                    validator.Validate("dfngsfdjgliulirhliuhrelteriih4u3jjoj43o");
                });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<InvalidPathException>(
                delegate
                {
                    validator.Validate("\\dfngsfdjgliulirhliuhrelteriih4u3jjoj43oeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeefffffffffffffffffffffffffffffffffffffffffffffffffsssssssssssssssssssssssssssssssssseeeeeeeeeeeeeeeeeeeeeeeeeeeevvvvvvvvvvvvvvvvvvvvvvoidjgoidjgiojrtoigjirotghiorhtiohrtiohoirthoiroigneoignoiengoineoirgnoirengorengoenrognerognoerngoernogneoreoi");
                });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<InvalidPathException>(
                delegate
                {
                    validator.Validate(@"\\\\");
                });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<InvalidPathException>(
                delegate
                {
                    validator.Validate("\\*");
                });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<InvalidPathException>(
                delegate
                {
                    validator.Validate("");
                });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<InvalidPathException>(
                delegate
                {
                    validator.Validate(null);
                });

            validator.Validate("\\kfmlksmkl\\rootlemlgmel");
        }
    }
}