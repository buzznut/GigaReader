using UtilitiesLibrary;

namespace UtilitiesLibraryTest;

[TestClass]
public class TestLoad
{
    [TestMethod]
    public void TestMethodLoad()
    {
        Lines lines = new Lines(null);
        lines.Load(@"d:\test\test.eml");

        while (lines.Loading)
        {
            Task.Delay(10).Wait();
        }
    }
}
