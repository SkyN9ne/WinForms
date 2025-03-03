﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.VisualBasic.ApplicationServices.Tests;

public class AssemblyInfoTests
{
    [Fact]
    public void Constructor_ArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new AssemblyInfo(null));
    }

    [Theory]
    [MemberData(nameof(AssemblyProperties_TestData))]
    public void AssemblyProperties(System.Reflection.Assembly assembly)
    {
        var assemblyInfo = new AssemblyInfo(assembly);
        var assemblyName = assembly.GetName();
        Assert.Equal(assemblyName.Name, assemblyInfo.AssemblyName);
        Assert.Equal(System.IO.Path.GetDirectoryName(assembly.Location), assemblyInfo.DirectoryPath);
        Assert.Equal(GetAttributeValue<AssemblyCompanyAttribute>(assembly, attr => attr.Company), assemblyInfo.CompanyName);
        Assert.Equal(GetAttributeValue<AssemblyCopyrightAttribute>(assembly, attr => attr.Copyright), assemblyInfo.Copyright);
        Assert.Equal(GetAttributeValue<AssemblyDescriptionAttribute>(assembly, attr => attr.Description), assemblyInfo.Description);
        Assert.Equal(GetAttributeValue<AssemblyProductAttribute>(assembly, attr => attr.Product), assemblyInfo.ProductName);
        Assert.Equal(GetAttributeValue<AssemblyTitleAttribute>(assembly, attr => attr.Title), assemblyInfo.Title);
        Assert.Equal(GetAttributeValue<AssemblyTrademarkAttribute>(assembly, attr => attr.Trademark), assemblyInfo.Trademark);
        Assert.Equal(assemblyName.Version, assemblyInfo.Version);
    }

    public static IEnumerable<object[]> AssemblyProperties_TestData()
    {
        yield return new object[] { typeof(object).Assembly };
        yield return new object[] { Assembly.GetExecutingAssembly() };
    }

    [Fact]
    public void LoadedAssemblies()
    {
        var executingAssembly = Assembly.GetExecutingAssembly();
        var assemblyInfo = new AssemblyInfo(executingAssembly);
        var loadedAssemblies = assemblyInfo.LoadedAssemblies;
        Assert.Contains(executingAssembly, loadedAssemblies);
    }

    [Fact]
    public void StackTrace()
    {
        // Property is independent of the actual assembly.
        var assemblyInfo = new AssemblyInfo(Assembly.GetExecutingAssembly());
        var stackTrace = assemblyInfo.StackTrace;
        Assert.Contains(nameof(AssemblyInfoTests), stackTrace);
    }

    [Fact]
    public void WorkingSet()
    {
        // Property is independent of the actual assembly.
        var assemblyInfo = new AssemblyInfo(Assembly.GetExecutingAssembly());
        var workingSet = assemblyInfo.WorkingSet;
        Assert.True(workingSet > 0);
    }

    private static string GetAttributeValue<TAttribute>(System.Reflection.Assembly assembly, Func<TAttribute, string> getAttributeValue)
        where TAttribute : Attribute
    {
        var attribute = (TAttribute)assembly.GetCustomAttribute(typeof(TAttribute));
        return (attribute is null) ? "" : getAttributeValue(attribute);
    }
}
