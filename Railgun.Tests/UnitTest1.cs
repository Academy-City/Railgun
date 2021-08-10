using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace Railgun.Tests
{
    public class UnitTest1
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public UnitTest1(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void Test1()
        {
        }
        
        [Fact]
        public void ParsesCorrectly()
        {
            var fileData = File.ReadAllText("../../../data/add.rgx");
            _testOutputHelper.WriteLine(fileData);
        }
    }
}