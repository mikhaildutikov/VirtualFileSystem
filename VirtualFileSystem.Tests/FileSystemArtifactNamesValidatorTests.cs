using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VirtualFileSystem.Tests.Helpers;

namespace VirtualFileSystem.Tests
{
    [TestClass]
    public class FileSystemArtifactNamesValidatorTests
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
        public void MakeSureYouCannotInitializeAValidatorPassingNullAsListOfIllegalCharacters()
        {
            ExceptionAssert.MakeSureExceptionIsRaisedBy<ArgumentNullException>(
                delegate
                    {
                        new FileSystemArtifactNamesValidator(null, 255);
                    });
        }

        [TestMethod]
        public void MakeSureValidatorDoesNotLetNullAndEmptyStringThrough()
        {
            var validator = new FileSystemArtifactNamesValidator(new char[0], 24);

            ExceptionAssert.MakeSureExceptionIsRaisedBy<InvalidNameException>(
                delegate
                {
                    validator.Validate(null);
                });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<InvalidNameException>(
                delegate
                {
                    validator.Validate(String.Empty);
                });
        }

        [TestMethod]
        public void CheckValidatorOnDataSuite1()
        {
            var validator = new FileSystemArtifactNamesValidator(new char[]{',', '.', '!'}, 255);

            ExceptionAssert.MakeSureExceptionIsRaisedBy<InvalidNameException>(
                delegate
                {
                    validator.Validate("dfngsfdjgliulirhliuhrelt!eriih4u3jjoj43o");
                });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<InvalidNameException>(
                delegate
                {
                    validator.Validate(".......................");
                });

            ExceptionAssert.MakeSureExceptionIsRaisedBy<InvalidNameException>(
                delegate
                {
                    validator.Validate(",';'';';';';';''");
                });

            validator.Validate("12435");
            validator.Validate("kfmlksmklgmelglemlgmel");
            validator.Validate("@$#@%$@#^#%$^&$%&%^*%&^");
        }

        [TestMethod]
        public void CheckValidatorOnDataSuite2()
        {
            var validator = new FileSystemArtifactNamesValidator(new char[] { }, Constants.FileAndFolderMaximumNameLength);

            validator.Validate("12435");
            validator.Validate("kfmlksmklgmelglemlgmel");
            validator.Validate("@$#@%$@#^#%$^&$%&%^*%&^");
        }
    }
}