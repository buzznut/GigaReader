//  <@$&< copyright begin >&$@> 24fe144c2255e2f7ccb65514965434a807ae8998c9c4d01902a628f980431c98:20241017.A:2025:12:5:9:40
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
// Copyright © 2024-2025 Stewart A. Nutter - All Rights Reserved.
// No warranty is implied or given.
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
// <@$&< copyright end >&$@>

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
