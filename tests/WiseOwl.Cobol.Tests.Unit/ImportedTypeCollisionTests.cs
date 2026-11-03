// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Microsoft.CodeAnalysis;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.CodeGen;
using WiseOwl.Cobol.Compiler.Oo;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE NAMES AN IMPORTED CLASS HAS (DESIGN-external-repository §10.1, §10.2; kb/Work PB2104): the backend reads a source
/// namespace or type meeting an imported one of the same full name as an ERROR (CS0435, CS0436, CS0437 are C# warnings that mean
/// the compile silently chose one of two declarations), an imported class is named by its full name under <c>global::</c> and its
/// factory beside it, and a description names a class by its identity without holding its symbol.
/// </summary>
public sealed class ImportedTypeCollisionTests
{
    /// <summary>The imported library: a type <c>P2104.Twin</c> and, in the namespace <c>P2104.Inner</c>, a type of its own.</summary>
    private const string Library = "namespace P2104 { public class Twin { } } namespace P2104.Inner { public class Deep { } }";

    [Theory]   // §10.2 — each of the three warnings a name meeting an imported one draws, escalated.
    [InlineData("CS0436", "namespace P2104 { public class Twin { } public class Use { Twin T; } }")]               // a source TYPE meeting an imported type
    [InlineData("CS0435", "namespace P2104.Twin { public class Deep { } } namespace P2104 { public class Use { Twin.Deep T; } }")]   // a source NAMESPACE meeting an imported type
    [InlineData("CS0437", "namespace P2104 { public class Inner { } public class Use { Inner T; } }")]             // a source TYPE meeting an imported namespace
    public void ASourceNameMeetingAnImportedOne_IsAnErrorNotACsharpWarning(string code, string source)
    {
        string dir = ScratchDirectory.Create("collide");
        try
        {
            string library = Path.Combine(dir, "Collide.dll");
            Assert.True(RoslynBackend.Compile(Library, library, "Collide", OutputKind.DynamicallyLinkedLibrary, []).Success);
            var r = RoslynBackend.Compile(source, Path.Combine(dir, "Caller.dll"), "Caller", OutputKind.DynamicallyLinkedLibrary, [library]);
            Assert.False(r.Success);
            Assert.Contains(r.Diagnostics, d => d.Id == code && d.Severity == DiagnosticSeverity.Error);
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    [Fact]   // Nothing in common: a clean compile against the same library.
    public void ASourceWithNoNameInCommon_CompilesAgainstTheLibrary()
    {
        string dir = ScratchDirectory.Create("no-collide");
        try
        {
            string library = Path.Combine(dir, "Collide.dll");
            Assert.True(RoslynBackend.Compile(Library, library, "Collide", OutputKind.DynamicallyLinkedLibrary, []).Success);
            Assert.True(RoslynBackend.Compile("namespace Mine { public class Use { global::P2104.Twin T; } }", Path.Combine(dir, "Clean.dll"),
                "Clean", OutputKind.DynamicallyLinkedLibrary, [library]).Success);
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    [Theory]   // The factory of an imported class is the sibling in the SAME namespace — the prefix goes on the last segment.
    [InlineData("LEDGER", "__FACTORY_LEDGER")]
    [InlineData("global::Cobol.LIB.LEDGER", "global::Cobol.LIB.__FACTORY_LEDGER")]
    [InlineData("global::Cobol.@class.LEDGER", "global::Cobol.@class.__FACTORY_LEDGER")]
    public void TheFactoryOfAClass_IsASiblingInItsNamespace(string clrName, string factory) =>
        Assert.Equal(factory, NamingConvention.FactoryClassName(clrName));

    [Fact]   // §10.1: an imported class is named by its full name under global::, the namespace's keyword segments escaped.
    public void AnImportedClass_IsNamedByItsFullNameUnderGlobal()
    {
        Assert.Equal("global::Cobol.LIB.LEDGER", NamingConvention.ImportedTypeName(CsNames.ModuleNamespaceOf("LIB"), "LEDGER"));
        Assert.Equal("global::Cobol.@class.LEDGER", NamingConvention.ImportedTypeName(CsNames.ModuleNamespaceOf("class"), "LEDGER"));
        // The key an imported class has in the class table holds a quote and a space, which no COBOL word does, so it is
        // neither a class of the group's word nor another identity's.
        string key = OoClassTable.ImportedKey("Ledger");
        Assert.Equal("class \"Ledger\"", key);
        Assert.Contains('"', key);
        Assert.Contains(' ', key);
    }

    [Fact]   // §6.4: the description is equal whoever resolved it; the identity and the CLR name are functions of the name.
    public void AnObjectReferenceDescription_IsEqualWhateverIdentityItCarries()
    {
        var plain = ObjectRefDescriptor.ObjectClass("LEDGER");
        var resolved = plain.Resolved("Ledger", clrName: null);
        Assert.Equal(plain, resolved);
        Assert.Equal(plain.GetHashCode(), resolved.GetHashCode());
        Assert.Equal("Ledger", resolved.Identity);
        Assert.NotEqual(ObjectRefDescriptor.ObjectClass("OTHER"), plain);
        // A class of the group is named by its word; an imported one by its full name, and its factory by the sibling.
        Assert.Equal("LEDGER", resolved.ClrTypeName);
        var imported = ObjectRefDescriptor.ObjectClass(OoClassTable.ImportedKey("Ledger"))
            .Resolved("Ledger", "global::Cobol.LIB.LEDGER");
        Assert.Equal("global::Cobol.LIB.LEDGER", imported.ClrTypeName);
        Assert.Equal("global::Cobol.LIB.__FACTORY_LEDGER", (imported with { Factory = true }).ClrTypeName);
    }
}
