using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using NUnit.Framework.Api;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

internal static class CoreTestRunner
{
    private static int Main(string[] args)
    {
        NUnitTestAssemblyRunner runner = new NUnitTestAssemblyRunner(new DefaultTestAssemblyBuilder());
        runner.Load(Assembly.LoadFrom(args[0]), new Dictionary<string, object>());
        ITestResult result = runner.Run(TestListener.NULL, TestFilter.Empty);
        using (XmlWriter writer = XmlWriter.Create(args[1], new XmlWriterSettings { Indent = true }))
        {
            result.ToXml(true).WriteTo(writer);
        }
        Console.WriteLine($"Core NUnit: {result.PassCount} passed, {result.FailCount} failed, {result.SkipCount} skipped.");
        return result.FailCount == 0 && result.PassCount > 0 ? 0 : 1;
    }
}
