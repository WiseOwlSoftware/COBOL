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
        // DT (F5's type); PRTA, which F4 and PRTA's own P name, is one prototype row naming itself — and PP, the type of
        // PRTA's formal P, which PRTA declares and RTONE does not (§8.13 "type declarations required for the description of
        // parameters"): a referenced prototype's formals are described with ITS declarations (DataItem.TypeDeclaration).
        Assert.Equal(["DT", "PP", "PT", "T1"], record.TypeDeclarations.Select(t => t.Name!).Order(StringComparer.Ordinal));
        int t1 = record.TypeDeclarations.Select((t, i) => (t, i)).Single(x => x.t.Name == "T1").i;
        int pp = record.TypeDeclarations.Select((t, i) => (t, i)).Single(x => x.t.Name == "PP").i;
        Assert.Equal(t1, record.Formals[2].Description.TypeDeclaration);
        Assert.Single(record.ReferencedPrototypes);
        Assert.Equal(0, record.Formals[3].Description.Pic!.RestrictedPrototype);
        Assert.Equal(0, record.ReferencedPrototypes[0].Formals[1].Description.Pic!.RestrictedPrototype);
        Assert.Equal(pp, record.ReferencedPrototypes[0].Formals[1].Description.TypeDeclaration);
        Assert.Equal(t1, record.Formals[4].Description.Pic!.RestrictedTypeDeclaration);
        Assert.True(RepositoryCodec.Of(comp.Units.Single(u => u.Name == "RTTWO")).Formals[1].Optional);
    }

    // A class and an interface whose records cover what ISO §8.13 lists for them: "the object properties of the source unit"
    // (an object property from a PROPERTY clause, a factory property, an interface's GET PROPERTY prototype) and "the methods
    // contained in the source unit, if any, and details about the method's externalized name, parameters, returning item, and
    // entry convention" (a BY VALUE formal and a RETURNING item, an OPTIONAL formal, a factory method returning an object
    // reference); the superclass and the implemented interface as identities, an AS name, DECIMAL-POINT IS COMMA; and a
    // method formal typed by the object's TYPEDEF and one typed by the METHOD's own (ISO §11.7.4 GR5: the method's
    // declaration is the one its word refers to).
    internal const string OoSource = """
               IDENTIFICATION DIVISION.
               INTERFACE-ID. ROI.
               PROCEDURE DIVISION.
               METHOD-ID. SCALE.
               DATA DIVISION.
               LINKAGE SECTION.
               01 LK-N PIC S9(5)V99 COMP-3.
               01 LK-R PIC S9(7)V99 COMP-3.
               PROCEDURE DIVISION USING BY VALUE LK-N RETURNING LK-R.
               END METHOD SCALE.
               METHOD-ID. GET PROPERTY TAG.
               DATA DIVISION.
               LINKAGE SECTION.
               01 LK-T PIC X(4).
               PROCEDURE DIVISION RETURNING LK-T.
               END METHOD.
               END INTERFACE ROI.

               IDENTIFICATION DIVISION.
               CLASS-ID. ROC AS "RO-CLASS" INHERITS FROM BASE.
               ENVIRONMENT DIVISION.
               CONFIGURATION SECTION.
               SPECIAL-NAMES.
                   DECIMAL-POINT IS COMMA.
               REPOSITORY.
                   CLASS BASE
                   INTERFACE ROI.
               IDENTIFICATION DIVISION.
               FACTORY.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
               01 F-COUNT PIC 9(4) VALUE 0 PROPERTY.
               PROCEDURE DIVISION.
               METHOD-ID. MAKE.
               DATA DIVISION.
               LINKAGE SECTION.
               01 LK-O USAGE OBJECT REFERENCE ROC.
               PROCEDURE DIVISION RETURNING LK-O.
                   INVOKE SELF "NEW" RETURNING LK-O.
                   ADD 1 TO F-COUNT.
                   GOBACK.
               END METHOD MAKE.
               END FACTORY.
               IDENTIFICATION DIVISION.
               OBJECT. IMPLEMENTS ROI.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
               01 OT TYPEDEF.
                  05 OT-A PIC X(2).
                  05 OT-B PIC 9(3).
               01 TAG PIC X(4) VALUE "ROC1" PROPERTY.
               PROCEDURE DIVISION.
               METHOD-ID. SCALE.
               DATA DIVISION.
               LINKAGE SECTION.
               01 LK-N PIC S9(5)V99 COMP-3.
               01 LK-R PIC S9(7)V99 COMP-3.
               PROCEDURE DIVISION USING BY VALUE LK-N RETURNING LK-R.
                   COMPUTE LK-R = LK-N * 2.
                   GOBACK.
               END METHOD SCALE.
               METHOD-ID. FILL-IN.
               DATA DIVISION.
               LOCAL-STORAGE SECTION.
               01 MT TYPEDEF PIC X(6).
               LINKAGE SECTION.
               01 LK-G TYPE OT.
               01 LK-M TYPE MT.
               PROCEDURE DIVISION USING LK-G OPTIONAL LK-M.
                   MOVE "AB" TO OT-A OF LK-G.
                   MOVE 7 TO OT-B OF LK-G.
                   GOBACK.
               END METHOD FILL-IN.
               END OBJECT.
               END CLASS ROC.

               IDENTIFICATION DIVISION.
               PROGRAM-ID. ROMAIN.
               ENVIRONMENT DIVISION.
               CONFIGURATION SECTION.
               REPOSITORY.
                   CLASS ROC AS "RO-CLASS"
                   INTERFACE ROI
                   PROPERTY TAG.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
               01 O USAGE OBJECT REFERENCE ROC.
               01 I USAGE OBJECT REFERENCE ROI.
               01 N PIC S9(5)V99 COMP-3 VALUE 1.25.
               01 R PIC S9(7)V99 COMP-3.
               01 R-D PIC -9(7).99.
               PROCEDURE DIVISION.
                   INVOKE ROC "MAKE" RETURNING O.
                   SET I TO O.
                   INVOKE I "SCALE" USING BY VALUE N RETURNING R.
                   MOVE R TO R-D.
                   DISPLAY "R=" R-D " TAG=" TAG OF I.
                   STOP RUN.
               END PROGRAM ROMAIN.
        """;

    [Fact]
    public void AClassAndAnInterfaceRecord_StateTheFactsSection813Lists()
    {
        var comp = Bind(OoSource, 2023);
        var records = OoRecordsOf(comp).Select(r => r.Record).ToList();
        var iface = records.Single(r => r.Kind == WiseOwl.Cobol.Runtime.Repository.RepositoryUnitKind.Interface);
        var cls = records.Single(r => r.Kind == WiseOwl.Cobol.Runtime.Repository.RepositoryUnitKind.Class);

        Assert.Equal(("ROI", "ROI"), (iface.Name, iface.ExternalizedName));
        Assert.Equal(["SCALE", "TAG"], iface.Class!.Methods.Select(m => m.ExternalizedName));
        Assert.Equal(WiseOwl.Cobol.Runtime.Repository.PassingMode.Value, iface.Class.Methods[0].Formals.Single().Mode);
        Assert.NotNull(iface.Class.Methods[0].Returning);
        Assert.Equal(new PropertyRecord("TAG", "TAG", IsFactory: false, Get: true, Set: false), iface.Class.Properties.Single());
        Assert.Null(iface.Class.FactoryType);

        Assert.Equal(("ROC", "RO-CLASS"), (cls.Name, cls.ExternalizedName));
        Assert.True(cls.DecimalPointIsComma);
        var c = cls.Class!;
        Assert.Equal("BASE", c.BaseIdentity);
        Assert.Equal(["ROI"], c.Implements);
        Assert.Empty(c.FactoryImplements);
        Assert.Equal(comp.ClassUnits.Single().Symbol.FactoryCsName, c.FactoryType);
        Assert.Equal([new PropertyRecord("TAG", "TAG", false, true, true), new PropertyRecord("F-COUNT", "F-COUNT", true, true, true)], c.Properties);
        // The object roster, then the factory roster; an accessor carries its property's externalized name (ISO §12.3.8.4 GR2).
        Assert.Equal([("SCALE", false), ("FILL-IN", false), ("TAG", false), ("TAG", false), ("MAKE", true), ("F-COUNT", true), ("F-COUNT", true)],
            c.Methods.Select(m => (m.ExternalizedName, m.IsFactory)));
        Assert.All(c.Methods, m => Assert.Equal(RepositoryCodec.CobolEntryConvention, m.EntryConvention));
        var fill = c.Methods.Single(m => m.Name == "FILL-IN");
        Assert.True(fill.Formals[1].Optional);
        Assert.Equal(["OT", "MT"], cls.TypeDeclarations.Select(t => t.Name!));
        Assert.Equal(0, fill.Formals[0].Description.TypeDeclaration);   // OT, the object's
        Assert.Equal(1, fill.Formals[1].Description.TypeDeclaration);   // MT, the method's own
        Assert.Equal(WiseOwl.Cobol.Runtime.ObjectReferenceKind.ObjectClass, c.Methods.Single(m => m.Name == "MAKE").Returning!.Pic!.ObjectRefKind);
        Assert.Equal("RO-CLASS", c.Methods.Single(m => m.Name == "MAKE").Returning!.Pic!.ObjectRefIdentity);

        Assert.Equal(["ROMAIN", "ROI", "ROC"], RoundTripAll(comp));
    }

    // A formal whose OCCURS DEPENDING ON object is QUALIFIED because its name is not unique in the record (ISO §8.4.2.2.3
    // SR1: "uniqueness shall be established through a sequence of qualifiers that precludes any ambiguity of reference"),
    // and one whose REDEFINES chain repeats a name: `X REDEFINES X` names the first X, and so does `C REDEFINES X`
    // (§13.18.44.3 SR7: "Multiple redefinitions of the same storage area shall each specify as data-name-2 the data-name of
    // the entry that originally defined the area"; NOTE 1: "If data-name-2 is not unique, no ambiguity of reference exists
    // because of the required placement of the REDEFINES clause"). A record that keeps either link as a bare word decodes it
    // to whichever entry of that name a lookup meets first (kb/Work PB978's shape), so the decoded twin is not the bound item.
    internal const string AmbiguousNamesSource = """
               IDENTIFICATION DIVISION.
               PROGRAM-ID. RTQUAL.
               DATA DIVISION.
               LINKAGE SECTION.
               01 REC.
                  05 A.
                     10 N PIC 99.
                  05 B.
                     10 N PIC 99.
                  05 T PIC X OCCURS 1 TO 10 DEPENDING ON N OF B.
               01 RDF.
                  05 X PIC X(4).
                  05 X REDEFINES X PIC 9(4).
                  05 C REDEFINES X PIC X(4).
               PROCEDURE DIVISION USING REC RDF.
                   GOBACK.
               END PROGRAM RTQUAL.
               """;

    /// <summary>Both links of <see cref="AmbiguousNamesSource"/> decode to the entry the binder resolved — the second N (the
    /// one in B) and the first X: the record names a DEPENDING ON object by its position in the description's tree, and the
    /// decoder finds a REDEFINES object by the binder's own placement rule (<see cref="DataItem.RedefinedEntry"/>).</summary>
    [Fact]
    public void AQualifiedDependingOnObject_AndARepeatedRedefinesName_DecodeToTheBoundEntries()
    {
        var comp = Bind(AmbiguousNamesSource, 2023);
        var unit = comp.Units.Single(u => u.Name == "RTQUAL");
        var record = RepositoryCodec.Of(unit);
        Assert.Equal(4, record.Formals[0].Description.Children[2].Table!.DependingEntry);   // REC 0, A 1, N 2, B 3, N 4
        var decoded = RepositoryCodec.Decode(record, comp.SignatureClasses);
        var t = decoded.Formals[0].Item.Children[2];
        Assert.Same(decoded.Formals[0].Item.Children[1].Children[0], t.OccursSpec!.Depending);
        var rdf = decoded.Formals[1].Item;
        Assert.Same(rdf.Children[0], rdf.Children[1].RedefinesTarget);
        Assert.Same(rdf.Children[0], rdf.Children[2].RedefinesTarget);
        Assert.Equal(["RTQUAL"], RoundTripAll(comp));
    }

    /// <summary>A REDEFINES chain ISO §13.18.44.3 SR7 forbids ("Multiple redefinitions of the same storage area shall each
    /// specify as data-name-2 the data-name of the entry that originally defined the area") is COBOL1656, an error strict
    /// and a warning under --permissive, where the binder links the nearest same-named candidate because every candidate is
    /// itself a redefinition. The renderer publishes that link, so the decoder asks the same rule, both halves
    /// (<see cref="DataItem.RedefinedEntry"/>), and decodes `C` over `B`, never refusing a record of an accepted program.</summary>
    [Fact]
    public void APermissiveRedefinesChain_DecodesToTheBoundEntry()
    {
        var comp = Bind("""
               IDENTIFICATION DIVISION.
               PROGRAM-ID. RCHAIN.
               DATA DIVISION.
               LINKAGE SECTION.
               01 REC.
                  05 A PIC X(4).
                  05 B REDEFINES A PIC 9(4).
                  05 C REDEFINES B PIC X(4).
               PROCEDURE DIVISION USING REC.
                   GOBACK.
               END PROGRAM RCHAIN.
               """, 2023, permissive: true);
        var bound = comp.Units.Single(u => u.Name == "RCHAIN").Data.LinkageFormals[0].Item;
        Assert.Same(bound.Children[1], bound.Children[2].RedefinesTarget);
        var decoded = RepositoryCodec.Decode(RepositoryCodec.Of(comp.Units.Single(u => u.Name == "RCHAIN")), comp.SignatureClasses);
        var rec = decoded.Formals[0].Item;
        Assert.Same(rec.Children[0], rec.Children[1].RedefinesTarget);
        Assert.Same(rec.Children[1], rec.Children[2].RedefinesTarget);
        Assert.Equal(["RCHAIN"], RoundTripAll(comp));

        // The one record the rule leaves undecodable is one no compilation writes: a word naming no preceding sibling.
        var record = RepositoryCodec.Of(comp.Units.Single(u => u.Name == "RCHAIN"));
        var d = record.Formals[0].Description;
        var unnamed = record with
        {
            Formals = new([record.Formals[0] with { Description = d with { Children = new([d.Children[0], d.Children[1], d.Children[2] with { RedefinesTarget = "Z" }]) } }]),
        };
        var e = Assert.Throws<RepositoryRecordException>(() => RepositoryCodec.Decode(unnamed, comp.SignatureClasses));
        Assert.Contains("'C' REDEFINES 'Z', which names no preceding sibling", e.Message);
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
                ? f with { Description = f.Description with { Children = new([f.Description.Children[0], table with { Table = table.Table! with { Depending = "G1", DependingEntry = 0 } }]) } }
                : f)]),
        };
        e = Assert.Throws<RepositoryRecordException>(() => RepositoryCodec.Decode(cyclic, comp.SignatureClasses));
        Assert.Contains("DEPENDING ON 'G1'", e.Message);
        Assert.Contains("SR17", e.Message);

        // The DEPENDING ON object's position and its word are one fact written twice: a record whose two disagree, that
        // names a word of the record with no position, or a position past the tree is one the renderer cannot have written.
        foreach (var (occurs, detail) in new (OccursRecord, string)[]
        {
            (table.Table! with { DependingEntry = 0 }, "which entry 0 ('G1') is not"),
            (table.Table! with { DependingEntry = null }, "with entry none"),
            (table.Table! with { Depending = RepositoryCodec.OutsideTheRecord }, "DEPENDING ON '<outside>' with entry 1"),
            (table.Table! with { DependingEntry = 9 }, "index 9"),
        })
        {
            var malformed = record with
            {
                Formals = new([.. record.Formals.Select(f => f == g1
                    ? f with { Description = f.Description with { Children = new([f.Description.Children[0], table with { Table = occurs }]) } }
                    : f)]),
            };
            e = Assert.Throws<RepositoryRecordException>(() => RepositoryCodec.Decode(malformed, comp.SignatureClasses));
            Assert.Contains(detail, e.Message);
        }
    }

    // ── the shared round-trip check (also the corpus enumeration's) ─────────────────────────────────────────────────

    /// <summary>Round-trip every outermost program, function and prototype unit of <paramref name="comp"/>, and every method
    /// of every class and interface (<see cref="OoRecordsOf"/>); returns the names checked.</summary>
    internal static IReadOnlyList<string> RoundTripAll(BoundCompilation comp)
    {
        var names = new List<string>();
        foreach (var unit in comp.Units.Where(u => u.Parent is null))
        {
            RoundTrip(unit, comp.SignatureClasses);
            names.Add(unit.Name);
        }
        foreach (var (record, methods) in OoRecordsOf(comp))
        {
            var decoded = RepositoryCodec.Decode(record, comp.SignatureClasses);
            Assert.Equal(methods.Count, decoded.Methods.Count);
            for (int i = 0; i < methods.Count; i++)
                RoundTrip($"{record.Name} {methods[i].SourceName}", methods[i].Binding!.Formals.Select(f => (f.Item, f.Position, f.ByValue, f.Optional)).ToList(),
                    methods[i].Binding!.Returning, decoded.Methods[i].Formals, decoded.Methods[i].Returning);
            names.Add(record.Name);
        }
        return names;
    }

    /// <summary>Every interface's and every class's record (<see cref="RepositoryCodec.Of(OoInterfaceSymbol, DataBinder)"/>,
    /// <see cref="RepositoryCodec.Of(OoClassUnit)"/>), each with the methods its record lists in their order — the object
    /// roster, then the factory roster — after checking that two encodings of it are equal.</summary>
    internal static IEnumerable<(RepositoryRecord Record, IReadOnlyList<OoMethodSymbol> Methods)> OoRecordsOf(BoundCompilation comp)
    {
        foreach (var iface in comp.OoClasses.Interfaces)
        {
            var record = RepositoryCodec.Of(iface, comp.InterfaceData[iface]);
            Assert.Equal(record, RepositoryCodec.Of(iface, comp.InterfaceData[iface]));
            yield return (record, iface.Prototypes);
        }
        foreach (var cls in comp.ClassUnits)
        {
            var record = RepositoryCodec.Of(cls);
            Assert.Equal(record, RepositoryCodec.Of(cls));
            yield return (record, [.. cls.Symbol.Methods, .. cls.Symbol.FactoryMethods]);
        }
    }

    /// <summary>§6.3's invariant for one unit: <c>Decode(Of(unit))</c> is indistinguishable from the unit's LINKAGE to every
    /// comparator and derived quantity.</summary>
    internal static void RoundTrip(BoundUnit unit, PrototypeSignatureClasses classes)
    {
        var record = RepositoryCodec.Of(unit);
        Assert.Equal(record, RepositoryCodec.Of(unit));
        var decoded = RepositoryCodec.Decode(record, classes);
        var formals = unit.Data.LinkageFormals;
        RoundTrip(unit.Name, formals.Select(f => (f.Item, f.Position, f.ByValue, f.Optional)).ToList(), unit.Data.LinkageReturning,
            decoded.Formals, decoded.Returning);
        Assert.True(PrototypeSignatures.Same(new CalleeSignature(formals, unit.Data.LinkageReturning), decoded.Signature),
            $"{unit.Name}: the decoded signature is not the same as the unit's (PrototypeSignatures.Same)");
    }

    /// <summary>§6.3's invariant for one signature (a unit's or a method's): each decoded formal and the decoded returning
    /// item are indistinguishable from the bound ones.</summary>
    private static void RoundTrip(string at, IReadOnlyList<(DataItem Item, int Position, bool ByValue, bool Optional)> formals, DataItem? returning,
        IReadOnlyList<LinkageFormal> decodedFormals, DataItem? decodedReturning)
    {
        Assert.Equal(formals.Count, decodedFormals.Count);
        for (int i = 0; i < formals.Count; i++)
        {
            Assert.Equal(formals[i].ByValue, decodedFormals[i].ByValue);
            Assert.Equal(formals[i].Optional, decodedFormals[i].Optional);
            Assert.Equal(formals[i].Position, decodedFormals[i].Position);
            Same(at, formals[i].Item, decodedFormals[i].Item);
        }
        Assert.Equal(returning is null, decodedReturning is null);
        if (returning is not null) Same(at, returning, decodedReturning!);
    }

    private static void Same(string unit, DataItem x, DataItem y)
    {
        string at = $"{unit} {x.CobolName}";
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
        for (int i = 0; i < xs.Count; i++)
        {
            SameEntry($"{at} / {xs[i].CobolName}", xs[i], ys[i]);
            // The links between entries are positions in the tree (an object outside it is -1 on both sides).
            Assert.True(Link(xs, xs[i].OccursSpec?.Depending) == Link(ys, ys[i].OccursSpec?.Depending),
                $"{at} / {xs[i].CobolName}: DEPENDING ON entry {Link(xs, xs[i].OccursSpec?.Depending)} vs {Link(ys, ys[i].OccursSpec?.Depending)}");
            Assert.True(Link(xs, xs[i].RedefinesTarget) == Link(ys, ys[i].RedefinesTarget),
                $"{at} / {xs[i].CobolName}: REDEFINES entry {Link(xs, xs[i].RedefinesTarget)} vs {Link(ys, ys[i].RedefinesTarget)}");
        }
    }

    private static int Link(List<DataItem> entries, DataItem? target) =>
        target is null ? -1 : entries.FindIndex(e => ReferenceEquals(e, target));

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

    internal static BoundCompilation Bind(string source, int edition, bool permissive = false)
    {
        string path = Path.Combine(Path.GetTempPath(), $"repocodec_{Guid.NewGuid():N}.cob");
        File.WriteAllText(path, source);
        try { return BindFile(path, edition, InitialReferenceFormat.Auto, permissive); }
        finally { FileHarness.TryDelete(path); }
    }

    internal static BoundCompilation BindFile(string path, int edition, InitialReferenceFormat format, bool permissive = false)
    {
        var diags = new DiagnosticBag();
        var frontend = new CnFrontend { InitialFormat = format, DialectLevel = edition };
        frontend.AddCopySearchPath(Path.GetDirectoryName(path)!);   // the suite's library: the case's own directory
        var tree = frontend.Parse(path, diags);
        Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
        var context = new EditionContext(edition, permissive);
        var comp = new CSharpEmitter().Bind(tree!, context, frontend.Directives);
        Assert.False(context.HasErrors, string.Join("\n", context.Diagnostics));
        return comp;
    }
}
