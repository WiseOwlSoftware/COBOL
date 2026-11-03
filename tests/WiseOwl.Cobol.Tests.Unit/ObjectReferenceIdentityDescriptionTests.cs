// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.CodeGen;
using WiseOwl.Cobol.Compiler.Oo;
using WiseOwl.Cobol.Frontend.Diagnostics;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Runtime;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;
using CnFrontend = WiseOwl.Cobol.Frontend.Frontend;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A RUN-TIME DESCRIPTION NAMES A CLASS OR AN INTERFACE BY ITS IDENTITY, NEVER BY ITS WORD (DESIGN-external-repository
/// §6.4; kb/Work PB2104). A word is per source element; two separately compiled groups name one class by its EXTERNALIZED
/// name (ISO §11.3.4 GR1: "literal-1, if specified, is the name of the class that is externalized to the operating
/// environment"; §11.6.4 GR1 for an interface), and the run-time relations compare the descriptions of the two (§14.9.4.4
/// GR3 d), §14.8.2.3.2 rule 3: "the same object-class-name"). So <c>ActivationDescriptions.Of</c> spells the class or
/// interface a description names, and every class and interface of its facts, by the externalized name, while the
/// compile-time SET table (<c>OoClassTable.SetOperand</c>), which relates two descriptions of one scope, keeps the words.
/// The fixture's class and interface are each declared with one word and externalized under another.
/// </summary>
public sealed class ObjectReferenceIdentityDescriptionTests
{
    private const string Source = """
               IDENTIFICATION DIVISION.
               INTERFACE-ID. I1 AS "PB2104-SPEAKER".
               END INTERFACE I1.

               IDENTIFICATION DIVISION.
               CLASS-ID. K1 AS "PB2104-ACCOUNT" INHERITS FROM BASE.
               ENVIRONMENT DIVISION.
               CONFIGURATION SECTION.
               REPOSITORY.
                   CLASS BASE
                   INTERFACE I1 AS "PB2104-SPEAKER".
               IDENTIFICATION DIVISION.
               OBJECT. IMPLEMENTS I1.
               END OBJECT.
               END CLASS K1.

               IDENTIFICATION DIVISION.
               PROGRAM-ID. P2104D.
               ENVIRONMENT DIVISION.
               CONFIGURATION SECTION.
               REPOSITORY.
                   CLASS K1 AS "PB2104-ACCOUNT"
                   INTERFACE I1 AS "PB2104-SPEAKER".
               DATA DIVISION.
               LINKAGE SECTION.
               01 L-K USAGE OBJECT REFERENCE K1.
               01 L-I USAGE OBJECT REFERENCE I1.
               PROCEDURE DIVISION USING L-K L-I.
                   GOBACK.
               END PROGRAM P2104D.
        """;

    private static (DataItem Class, DataItem Interface, OoClassTable Classes) Bind()
    {
        string path = Path.Combine(Path.GetTempPath(), $"objrefidentity_{Guid.NewGuid():N}.cob");
        File.WriteAllText(path, Source);
        try
        {
            var diags = new DiagnosticBag();
            var frontend = new CnFrontend { InitialFormat = InitialReferenceFormat.Auto, DialectLevel = 2023 };
            var tree = frontend.Parse(path, diags);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
            var edition = new EditionContext(2023);
            var comp = new CSharpEmitter().Bind(tree!, edition, frontend.Directives);
            Assert.True(edition.Diagnostics.Count == 0, string.Join("\n", edition.Diagnostics));
            var formals = comp.Units.Single(u => u.Name == "P2104D").Data.LinkageFormals;
            return (formals[0].Item, formals[1].Item, comp.OoClasses);
        }
        finally { FileHarness.TryDelete(path); }
    }

    [Fact]
    public void AClassDescription_NamesTheClassAndItsLineageByIdentity()
    {
        var (item, _, classes) = Bind();
        var d = ActivationDescriptions.Of(item, classes)!;
        Assert.Equal(CobolNames.Fold("PB2104-ACCOUNT"), d.ObjectName);
        Assert.Equal([CobolNames.Fold("PB2104-ACCOUNT"), CobolNames.Fold("BASE")], d.ObjectClasses!);
        Assert.Equal([CobolNames.Fold("PB2104-SPEAKER")], d.ObjectInterfaces!);
    }

    [Fact]
    public void AnInterfaceDescription_NamesTheInterfaceByIdentity()
    {
        var (_, item, classes) = Bind();
        var d = ActivationDescriptions.Of(item, classes)!;
        Assert.Equal(CobolNames.Fold("PB2104-SPEAKER"), d.ObjectName);
        Assert.Equal([CobolNames.Fold("PB2104-SPEAKER")], d.ObjectInterfaces!);
    }

    [Fact]   // The readers that hold no class table (a prototype's signature identity) spell the same name: the description carries it.
    public void ADescriptionWithNoClassTable_SpellsTheIdentityTheDescriptorCarries()
    {
        var (klass, iface, _) = Bind();
        Assert.Equal("PB2104-ACCOUNT", klass.Pic!.ObjectRef!.Value.Identity);
        Assert.Equal("PB2104-SPEAKER", iface.Pic!.ObjectRef!.Value.Identity);
        Assert.Equal(CobolNames.Fold("PB2104-ACCOUNT"), ActivationDescriptions.Of(klass, classes: null)!.ObjectName);
        Assert.Equal(CobolNames.Fold("PB2104-SPEAKER"), ActivationDescriptions.Of(iface, classes: null)!.ObjectName);
    }

    [Fact]
    public void TheCompileTimeSetOperand_KeepsTheWords()
    {
        var (item, _, classes) = Bind();
        var operand = classes.SetOperand(item.Pic!.ObjectRef!.Value);
        Assert.Equal("K1", operand.Name);
        Assert.Equal(["K1", "BASE"], operand.Classes);
        Assert.Equal(["I1"], operand.Interfaces);
    }
}
