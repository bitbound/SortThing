using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SortThing.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SortThing.Tests
{
    [TestClass]
    public class MetadataReaderTests
    {
        private readonly string _assemblyDir = Path.GetDirectoryName(typeof(MetadataReaderTests).Assembly.Location);
        private Mock<IFileLogger> _logger;
        private MetadataReader _metadataReader;


        [TestInitialize]
        public void Init()
        {
            _logger = new Mock<IFileLogger>();
            _metadataReader = new MetadataReader(_logger.Object);
        }

        [TestMethod]
        public void ParseExifDateTime_GivenInvalidData_Fails()
        {
            var result = _metadataReader.ParseExifDateTime(null);
            Assert.IsFalse(result.IsSuccess);

            result = _metadataReader.ParseExifDateTime(string.Empty);
            Assert.IsFalse(result.IsSuccess);

            result = _metadataReader.ParseExifDateTime(" ");
            Assert.IsFalse(result.IsSuccess);

            result = _metadataReader.ParseExifDateTime("2021:09");
            Assert.IsFalse(result.IsSuccess);

            result = _metadataReader.ParseExifDateTime("2021-09-24");
            Assert.IsFalse(result.IsSuccess);
        }

        [TestMethod]
        public void ParseExifDateTime_GivenValidData_Succeeds()
        {
            var result = _metadataReader.ParseExifDateTime("2021:09:24 13:27:34");
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(new DateTime(2021, 9, 24, 13, 27, 34), result.Value);

            result = _metadataReader.ParseExifDateTime("2021:09:24");
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(new DateTime(2021, 9, 24), result.Value);
        }

        [TestMethod]
        public async Task TryGetExifData_GivenInvalidPath_Fails()
        {
            var result = await _metadataReader.TryGetExifData(string.Empty);
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("File could not be found.", result.Error);

            result = await _metadataReader.TryGetExifData(_assemblyDir);
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("File could not be found.", result.Error);

            result = await _metadataReader.TryGetExifData(Path.Combine(_assemblyDir, "Resources", "PicWithoutExif.jpg"));
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("DateTime is missing from metadata.", result.Error);
        }

        [TestMethod]
        public async Task TryGetExifData_GivenValidPath_Succeeds()
        {
            var result = await _metadataReader.TryGetExifData(Path.Combine(_assemblyDir, "Resources", "PicWithExif.jpg"));
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(result.Value.DateTaken, new DateTime(2015, 11, 14, 14, 41, 14));
            Assert.AreEqual(result.Value.CameraModel, "Lumia 640 LTE");
        }
    }
}
