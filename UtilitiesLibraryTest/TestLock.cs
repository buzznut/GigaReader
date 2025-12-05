//  <@$&< copyright begin >&$@> 24fe144c2255e2f7ccb65514965434a807ae8998c9c4d01902a628f980431c98:20241017.A:2025:12:5:9:40
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
// Copyright © 2024-2025 Stewart A. Nutter - All Rights Reserved.
// No warranty is implied or given.
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
// <@$&< copyright end >&$@>

using Microsoft.Testing.Platform.Logging;
using System.Diagnostics;

namespace UtilitiesLibraryTest;

[TestClass]
public sealed class TestNoLock
{
    [TestMethod]
    public void TestMethodNoLock()
    {
        int testCount = 100000;

        byte[] aCountLock = new byte[sizeof(long)];

        Stopwatch stopwatchLock = Stopwatch.StartNew();
        object lockObj = new object();
        Parallel.Invoke(
            () =>
            {
                for (int i = 0; i < testCount; i++)
                {
                    TestAction(ref aCountLock);
                }
            },
                () =>
                {
                    for (int i = 0; i < testCount; i++)
                    {
                        TestAction(ref aCountLock);
                    }
                }
            );

        stopwatchLock.Stop();

        long testValue = BitConverter.ToInt64(aCountLock);
        Assert.AreNotEqual(testCount * 2, testValue);

        if (Debugger.IsAttached) Debugger.Log((int)LogLevel.Information, "Elapsed", $"Lock: {stopwatchLock.ElapsedMilliseconds} ms");
    }

    [TestMethod]
    public void TestMethodLock()
    {
        int testCount = 1000000;

        byte[] aCountLock = new byte[sizeof(long)];

        Stopwatch stopwatchLock = Stopwatch.StartNew();
        object lockObj = new object();
        Parallel.Invoke(
            () =>
                {
                    for (int i = 0; i < testCount; i++)
                    {
                        lock (lockObj)
                        {
                            TestAction(ref aCountLock);
                        }
                    }
                },
                () =>
                {
                    for (int i = 0; i < testCount; i++)
                    {
                        lock (lockObj)
                        {
                            TestAction(ref aCountLock);
                        }
                    }
                }
            );

        stopwatchLock.Stop();

        long testValue = BitConverter.ToInt64(aCountLock);
        Assert.AreEqual(testCount * 2, testValue);

        if (Debugger.IsAttached) Debugger.Log((int)LogLevel.Information, "Elapsed", $"Lock: {stopwatchLock.ElapsedMilliseconds} ms");
    }

    [TestMethod]
    public void TestMethodSpinLock()
    {
        int testCount = 1000000;

        byte[] aCountLock = new byte[sizeof(long)];
        SpinLock spinLock = new SpinLock();

        Stopwatch stopwatchLock = Stopwatch.StartNew();
        object lockObj = new object();
        Parallel.Invoke(
            () =>
            {
                for (int i = 0; i < testCount; i++)
                {
                    bool haveLock = false;
                    try
                    {
                        spinLock.Enter(ref haveLock);
                        TestAction(ref aCountLock);
                    }
                    finally
                    {
                        if (haveLock) spinLock.Exit();
                    }
                }
            },
                () =>
                {
                    for (int i = 0; i < testCount; i++)
                    {
                        bool haveLock = false;
                        try
                        {
                            spinLock.Enter(ref haveLock);
                            TestAction(ref aCountLock);
                        }
                        finally
                        {
                            if (haveLock) spinLock.Exit();
                        }
                    }
                }
            );

        stopwatchLock.Stop();

        long testValue = BitConverter.ToInt64(aCountLock);
        Assert.AreEqual(testCount * 2, testValue);

        if (Debugger.IsAttached) Debugger.Log((int)LogLevel.Information, "Elapsed", $"SpinLock: {stopwatchLock.ElapsedMilliseconds} ms");
    }

    private void TestAction(ref byte[] aCountLock)
    {
        long a = BitConverter.ToInt64(aCountLock);
        a++;
        byte[] bytes = BitConverter.GetBytes(a);
        Array.Copy(bytes, aCountLock, sizeof(long));
    }
}
