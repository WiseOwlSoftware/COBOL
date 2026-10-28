// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Binding.Repository;
using WiseOwl.Cobol.CodeGen;
using WiseOwl.Cobol.Compiler.Oo;
using WiseOwl.Cobol.Frontend.Diagnostics;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;
using CnFrontend = WiseOwl.Cobol.Frontend.Frontend;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A REPOSITORY RECORD DECODES TO A DESCRIPTION NO COMPARATOR CAN TELL FROM ITS ORIGINAL (ISO §8.13;
/// DESIGN-external-repository §6.3's invariant and §17.2's round trip; kb/Work PB2100). §8.13 stores "the description of the
/// parameters of the source unit, if any, and the manner of receiving parameters (by reference or by value) and whether they
/// are optional or not" as the unit's signature, so a record is right exactly when <c>Decode(Of(unit))</c>, bound in the
/// publishing element's own scope, answers every question the binder asks of a description as the bound unit does: the
/// activation description both ways (<see cref="OoConformance.SignatureDifference"/>), the §8.5.3.1 layout and type tests,
/// the widths, the host types, the normalized <c>PicInfo</c> equality and a restricted pointer's signature class — the last
/// after the decoded prototypes were ADMITTED as members of their own (§6.4), so equal identities mean "same signature",
/// never "same name". <see cref="RoundTrip"/> is that check, shared with the corpus enumeration
/// (<see cref="RepositoryRoundTripCorpusTests"/>).
/// </summary>
public sealed class RepositoryCodecTests
{
    // Programs whose LINKAGE covers the description shapes §6.3's table lists: an occurs-depending table, a REDEFINES, edited
    // and currency pictures, SIGN SEPARATE, SYNCHRONIZED LEFT and RIGHT, BLANK WHEN ZERO, JUSTIFIED, a strong TYPE, a pointer
    // restricted to that type, program-pointers restricted to a prototype that names itself (a finite graph of one row),
    // national, bit and float items, BY VALUE, OPTIONAL and RETURNING.
    internal const string Source = """
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PRTA IS PROTOTYPE.
               DATA DIVISION.
               LINKAGE SECTION.
               01 PP IS TYPEDEF USAGE PROGRAM-POINTER TO PRTA.
               01 A PIC 9(4).
               01 P TYPE PP.
               PROCEDURE DIVISION USING A P.
               END PROGRAM PRTA.
               IDENTIFICATION DIVISION.
               PROGRAM-ID. RTONE.
               ENVIRONMENT DIVISION.
               CONFIGURATION SECTION.
               SPECIAL-NAMES.
                   CURRENCY SIGN IS "EUR" WITH PICTURE SYMBOL "U".
               REPOSITORY.
                   PROGRAM PRTA.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
               01 T1 TYPEDEF STRONG.
                  05 T1-A PIC X(3).
                  05 T1-B PIC S9(5)V99 COMP-3.
               01 PT IS TYPEDEF USAGE PROGRAM-POINTER TO PRTA.
               01 DT IS TYPEDEF USAGE POINTER TO T1.
               LINKAGE SECTION.
               01 G1.
                  05 G1-N PIC 9(2).
                  05 G1-T OCCURS 1 TO 5 DEPENDING ON G1-N.
                     10 G1-X PIC X(2) JUSTIFIED RIGHT.
               01 G2.
                  05 G2-A PIC X(4).
                  05 G2-B REDEFINES G2-A PIC 9(4).
                  05 G2-C PIC ZZ9.99 BLANK WHEN ZERO.
                  05 G2-D PIC UUU9.99.
                  05 G2-S PIC S9(4) SIGN LEADING SEPARATE.
                  05 G2-Y PIC 9(4) COMP SYNC LEFT.
                  05 G2-Z PIC 9(4) COMP SYNC RIGHT.
               01 F3 TYPE T1.
               01 F4 TYPE PT.
               01 F5 TYPE DT.
               01 F6 PIC N(4).
               01 F7 PIC 1(8) USAGE BIT.
               01 F8 USAGE FLOAT-LONG.
               01 F9 PIC S9(4) BINARY.
               01 R1 PIC S9(9) BINARY.
               PROCEDURE DIVISION USING G1 G2 F3 F4 F5 F6 F7 F8
                   BY VALUE F9 RETURNING R1.
                   GOBACK.
               END PROGRAM RTONE.
               IDENTIFICATION DIVISION.
               PROGRAM-ID. RTTWO.
               DATA DIVISION.
               LINKAGE SECTION.
               01 H1 PIC X(8).
               01 H2 PIC 9(3) COMP-5.
               PROCEDURE DIVISION USING H1 OPTIONAL H2.
                   GOBACK.
               END PROGRAM RTTWO.
               """;

    [Fact]
    public void EveryOutermostUnit_RoundTripsThroughItsRecord()
    {
        var comp = Bind(Source, 2023);
        var checkedUnits = RoundTripAll(comp);
        Assert.Equal(["PRTA", "RTONE", "RTTWO"], checkedUnits);
    }

    [Fact]
    public void TheRecord_StatesTheUnitFactsSection813Lists()
    {
        var comp = Bind(Source, 2023);
        var record = RepositoryCodec.Of(comp.Units.Single(u => u.Name == "RTONE"));
        Assert.Equal(WiseOwl.Cobol.Runtime.Repository.RepositoryUnitKind.Program, record.Kind);
        Assert.Equal(RepositoryCodec.CobolEntryConvention, record.EntryConvention);
        Assert.Contains(record.Currencies, c => c.Symbol == 'U' && c.Text == "EUR");
        Assert.Equal(9, record.Formals.Count);
        Assert.Equal(WiseOwl.Cobol.Runtime.Repository.PassingMode.Value, record.Formals[8].Mode);
        Assert.NotNull(record.Returning);
        // The type declarations the descriptions name are rows: T1 (F3's type and F5's restriction), PT (F4's type) and
        // DT (F5's type); PRTA, which F4 and PRTA's own P name, is one prototype row naming itself.
        Assert.Equal(["DT", "PT", "T1"], record.TypeDeclarations.Select(t => t.Name!).Order(StringComparer.Ordinal));
        int t1 = record.TypeDeclarations.Select((t, i) => (t, i)).Single(x => x.t.Name == "T1").i;
        Assert.Equal(t1, record.Formals[2].Description.TypeDeclaration);
        Assert.Single(record.ReferencedPrototypes);
        Assert.Equal(0, record.Formals[3].Description.Pic!.RestrictedPrototype);
        Assert.Equal(0, record.ReferencedPrototypes[0].Formals[1].Description.Pic!.RestrictedPrototype);
        Assert.Equal(t1, record.Formals[4].Description.Pic!.RestrictedTypeDeclaration);
        Assert.True(RepositoryCodec.Of(comp.Units.Single(u => u.Name == "RTTWO")).Formals[1].Optional);
    }

    [Fact]
    public void TwoEncodingsOfOneUnit_AreEqual()
    {
        var comp = Bind(Source, 2023);
        var unit = comp.Units.Single(u => u.Name == "RTONE");
        Assert.Equal(RepositoryCodec.Of(unit), RepositoryCodec.Of(unit));
    }

    /// <summary>§6.4 part 3 and §17.2's perturbation: a record whose referenced prototype lost a formal no longer has its
    /// original's signature, so its decoded pointer founds a block of its own and its identity differs.</summary>
    [Fact]
    public void ARecordThatDroppedAReferencedPrototypesFormal_FailsTheRoundTrip()
    {
        var comp = Bind(Source, 2023);
        var unit = comp.Units.Single(u => u.Name == "RTONE");
        var record = RepositoryCodec.Of(unit);
        var row = record.ReferencedPrototypes[0];
        var perturbed = record with
        {
            ReferencedPrototypes = new([row with { Formals = new([row.Formals[0]]) }]),
        };
        var decoded = RepositoryCodec.Decode(perturbed, comp.SignatureClasses);
        var original = unit.Data.LinkageFormals[3].Item;
        Assert.False(PrototypeSignatures.Same(original.RestrictedPrototype!.Signature, decoded.Prototypes[0].Signature));
        Assert.NotEqual(original.PointerRestrictionIdentity, decoded.Formals[3].Item.PointerRestrictionIdentity);
        Assert.StartsWith("DECODED ", decoded.Formals[3].Item.PointerRestrictionIdentity);
    }

    /// <summary>§17.2's perturbation: a DATA-NAME is recorded and never compared (ISO §13.7.3 SR2), a TYPE-NAME is compared
    /// (§8.5.3.1: "Two type declarations are considered equivalent when they have the same type-name").</summary>
    [Fact]
    public void ADataNameChange_IsInert_AndATypeNameChange_IsSeen()
    {
        var comp = Bind(Source, 2023);
        var unit = comp.Units.Single(u => u.Name == "RTONE");
        var record = RepositoryCodec.Of(unit);
        var g2 = record.Formals[1];
        var renamed = record with
        {
            Formals = new([.. record.Formals.Select(f => f == g2
                ? f with { Name = "OTHER", Description = f.Description with { Name = "OTHER", Children = new([.. f.Description.Children.Select(c => c with { Name = c.Name + "-X", RedefinesTarget = c.RedefinesTarget is { } t ? t + "-X" : null })]) } }
                : f)]),
        };
        var decoded = RepositoryCodec.Decode(renamed, comp.SignatureClasses);
        Assert.True(PrototypeSignatures.Same(new CalleeSignature(unit.Data.LinkageFormals, unit.Data.LinkageReturning), decoded.Signature));

        var f3 = record.Formals[2];
        var retyped = record with
        {
            Formals = new([.. record.Formals.Select(f => f == f3 ? f with { Description = f.Description with { TypeName = "T2" } } : f)]),
        };
        var decodedRetyped = RepositoryCodec.Decode(retyped, comp.SignatureClasses);
        Assert.False(StrongTypeModel.SameType(unit.Data.LinkageFormals[2].Item, decodedRetyped.Formals[2].Item));
        Assert.False(PrototypeSignatures.Same(new CalleeSignature(unit.Data.LinkageFormals, unit.Data.LinkageReturning), decodedRetyped.Signature));
    }

    /// <summary>§15.3: a record the decoder cannot bind is a typed refusal naming the record, never an index fault or a
    /// cycle: an index past its table, and an OCCURS DEPENDING ON object ISO §13.18.38.3 SR17 forbids ("Data-name-1 shall
    /// describe an integer") — here the group that holds the table, the PB3510 shape whose link a consumer would follow
    /// without end.</summary>
    [Fact]
    public void AMalformedRecord_IsRefusedByName()
    {
        var comp = Bind(Source, 2023);
        var record = RepositoryCodec.Of(comp.Units.Single(u => u.Name == "RTONE"));
        var f4 = record.Formals[3];
        var pastTheTable = record with
        {
            Formals = new([.. record.Formals.Select(f => f == f4
                ? f with { Description = f.Description with { Pic = f.Description.Pic! with { RestrictedPrototype = 7 } } }
                : f)]),
        };
        var e = Assert.Throws<RepositoryRecordException>(() => RepositoryCodec.Decode(pastTheTable, comp.SignatureClasses));
        Assert.Contains("RTONE", e.Message);
        Assert.Contains("index 7", e.Message);

        var g1 = record.Formals[0];
        var table = g1.Description.Children[1];
        var cyclic = record with
        {
            Formals = new([.. record.Formals.Select(f => f == g1
                ? f with { Description = f.Description with { Children = new([f.Description.Children[0], table with { Table = table.Table! with { Depending = "G1" } }]) } }
                : f)]),
        };
        e = Assert.Throws<RepositoryRecordException>(() => RepositoryCodec.Decode(cyclic, comp.SignatureClasses));
        Assert.Contains("DEPENDING ON 'G1'", e.Message);
    }

    // ── the shared round-trip check (also the corpus enumeration's) ─────────────────────────────────────────────────

    /// <summary>Round-trip every outermost program, function and prototype unit of <paramref name="comp"/>; returns the
    /// names checked.</summary>
    internal static IReadOnlyList<string> RoundTripAll(BoundCompilation comp)
    {
        var names = new List<string>();
        foreach (var unit in comp.Units.Where(u => u.Parent is null))
        {
            RoundTrip(unit, comp.SignatureClasses);
            names.Add(unit.Name);
        }
        return names;
    }

    /// <summary>§6.3's invariant for one unit: <c>Decode(Of(unit))</c> is indistinguishable from the unit's LINKAGE to every
    /// comparator and derived quantity.</summary>
    internal static void RoundTrip(BoundUnit unit, PrototypeSignatureClasses classes)
    {
        var record = RepositoryCodec.Of(unit);
        Assert.Equal(record, RepositoryCodec.Of(unit));
        var decoded = RepositoryCodec.Decode(record, classes);
        var formals = unit.Data.LinkageFormals;
        Assert.Equal(formals.Count, decoded.Formals.Count);
        for (int i = 0; i < formals.Count; i++)
        {
            Assert.Equal(formals[i].ByValue, decoded.Formals[i].ByValue);
            Assert.Equal(formals[i].Optional, decoded.Formals[i].Optional);
            Assert.Equal(formals[i].Position, decoded.Formals[i].Position);
            Same(unit, formals[i].Item, decoded.Formals[i].Item);
        }
        Assert.Equal(unit.Data.LinkageReturning is null, decoded.Returning is null);
        if (unit.Data.LinkageReturning is { } returning) Same(unit, returning, decoded.Returning!);
        Assert.True(PrototypeSignatures.Same(new CalleeSignature(formals, unit.Data.LinkageReturning), decoded.Signature),
            $"{unit.Name}: the decoded signature is not the same as the unit's (PrototypeSignatures.Same)");
    }

    private static void Same(BoundUnit unit, DataItem x, DataItem y)
    {
        string at = $"{unit.Name} {x.CobolName}";
        Assert.True(OoConformance.SignatureDifference(x, y, "original", "decoded") is null,
            $"{at}: {OoConformance.SignatureDifference(x, y, "original", "decoded")}");
        Assert.True(OoConformance.SignatureDifference(y, x, "decoded", "original") is null,
            $"{at}: {OoConformance.SignatureDifference(y, x, "decoded", "original")}");
        Assert.True(StrongTypeModel.SameElementaryLayout(x, y), $"{at}: SameElementaryLayout");
        Assert.True(StrongTypeModel.SameType(x, x) == StrongTypeModel.SameType(x, y), $"{at}: SameType");
        Assert.True(x.ImageWidth == y.ImageWidth, $"{at}: ImageWidth {x.ImageWidth} vs {y.ImageWidth}");
        Assert.True(x.ByteWidth == y.ByteWidth, $"{at}: ByteWidth {x.ByteWidth} vs {y.ByteWidth}");
        Assert.True(BitLayout.ExtentBits(x) == BitLayout.ExtentBits(y), $"{at}: ExtentBits");
        Assert.True(HostSurface.TypeOf(x) == HostSurface.TypeOf(y), $"{at}: host type {HostSurface.TypeOf(x)} vs {HostSurface.TypeOf(y)}");
        var xs = DataItem.DescendantsOf(x).Prepend(x).ToList();
        var ys = DataItem.DescendantsOf(y).Prepend(y).ToList();
        Assert.True(xs.Count == ys.Count, $"{at}: {xs.Count} entries vs {ys.Count}");
        for (int i = 0; i < xs.Count; i++) SameEntry($"{at} / {xs[i].CobolName}", xs[i], ys[i]);
    }

    /// <summary>§6.3's normalization of <c>PicInfo</c>: record equality with the four restriction members and the editing
    /// rules set aside, the rules element-wise, the restriction names equal as written (one scope), and the restrictions
    /// equal by their relations.</summary>
    private static void SameEntry(string at, DataItem x, DataItem y)
    {
        Assert.True(x.Level == y.Level && x.CobolName == y.CobolName && x.Occurs == y.Occurs, $"{at}: level, name or OCCURS");
        Assert.True(x.IsGroup == y.IsGroup && x.IsAsIfElementary == y.IsAsIfElementary, $"{at}: group shape");
        if (x.Pic is not { } p || y.Pic is not { } q)
        {
            Assert.True(x.Pic is null && y.Pic is null, $"{at}: PICTURE presence");
            return;
        }
        Assert.True((p with { EditingRules = null, RestrictedTypeName = null, RestrictedTypeDecl = null, RestrictedPrototypeName = null })
            == (q with { EditingRules = null, RestrictedTypeName = null, RestrictedTypeDecl = null, RestrictedPrototypeName = null }),
            $"{at}: PicInfo {p} vs {q}");
        Assert.True(p.EditingRules is null ? q.EditingRules is null : q.EditingRules is { } qr && p.EditingRules.SequenceEqual(qr), $"{at}: EditingRules");
        Assert.True(p.RestrictedTypeName == q.RestrictedTypeName && p.RestrictedPrototypeName == q.RestrictedPrototypeName, $"{at}: restriction names");
        if (p.RestrictedTypeName is not null)
            Assert.True(StrongTypeModel.SameRestriction(StrongTypeModel.PointerRestriction(x), StrongTypeModel.PointerRestriction(y)), $"{at}: SameRestriction");
        if (p.RestrictedPrototypeName is not null)
        {
            Assert.True(PrototypeSignatures.SameType(x, y), $"{at}: PrototypeSignatures.SameType");
            Assert.True(x.PointerRestrictionIdentity == y.PointerRestrictionIdentity,
                $"{at}: restriction identity {x.PointerRestrictionIdentity} vs {y.PointerRestrictionIdentity}");
            Assert.True(PrototypeSignatures.Same(x.RestrictedPrototype?.Signature, y.RestrictedPrototype?.Signature), $"{at}: referenced prototype signature");
        }
    }

    internal static BoundCompilation Bind(string source, int edition)
    {
        string path = Path.Combine(Path.GetTempPath(), $"repocodec_{Guid.NewGuid():N}.cob");
        File.WriteAllText(path, source);
        try { return BindFile(path, edition, InitialReferenceFormat.Auto); }
        finally { FileHarness.TryDelete(path); }
    }

    internal static BoundCompilation BindFile(string path, int edition, InitialReferenceFormat format)
    {
        var diags = new DiagnosticBag();
        var frontend = new CnFrontend { InitialFormat = format, DialectLevel = edition };
        frontend.AddCopySearchPath(Path.GetDirectoryName(path)!);   // the suite's library: the case's own directory
        var tree = frontend.Parse(path, diags);
        Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
        var context = new EditionContext(edition);
        var comp = new CSharpEmitter().Bind(tree!, context, frontend.Directives);
        Assert.False(context.HasErrors, string.Join("\n", context.Diagnostics));
        return comp;
    }
}
