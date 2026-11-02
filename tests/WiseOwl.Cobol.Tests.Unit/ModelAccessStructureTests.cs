// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using System.Text.RegularExpressions;
using WiseOwl.Cobol.Binding.Bound;
using WiseOwl.Cobol.Binding.Model;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE MODEL STATES AN ACCESS AS STRUCTURE, NEVER AS C# MEMBER-ACCESS TEXT BUILT FROM NAMES (kb/Work PB4320;
/// DESIGN-backend-abstraction.md §1.2 and §1.3 L7). Four binder sites used to compose C# text from CLR names: a
/// carrier-resident formal's <see cref="DataItem.ClrName"/> was overwritten with <c>__lnk{Uid}.Value</c>,
/// <see cref="AccessPath.Reroot"/> joined <c>__outer.</c> (or a container's class name) onto a root field, each GLOBAL
/// bridge held the joined text <c>anchor + member</c>, and a table-sort key held its member names joined with dots. A
/// backend that is not C# could only parse that text back. Now the carrier is <see cref="DataItem.ResidentCarrier"/>
/// with a <see cref="CarrierValueSegment"/> on the path, the containing program is a <see cref="ContainerAnchor"/> on
/// the root segment and on the bridge, and the sort key carries its member chain of items. These facts pin it: a
/// program group exercising every former leak binds to a model whose names are all bare identifiers and whose accesses
/// are the structural forms; and the two carriers of the former joined text expose no string but a name.
/// </summary>
public sealed class ModelAccessStructureTests
{
    private static readonly Regex Identifier = new("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.CultureInvariant);

    // PB4320A: a carrier-resident GLOBAL linkage formal (§13.18.27.3 SR1 b admits GLOBAL in the linkage section) that
    // the contained PB4320B references (§13.18.27.4 GR2), and a Format-2 table SORT whose key lies two members down
    // the element (§14.9.40.3 SR14 a). PB4320C: a RECURSIVE program, whose working-storage is static data (§13.5.4 GR1),
    // with a GLOBAL item the contained PB4320D references — reached through the container's class, not an instance.
    private const string Source = """
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PB4320A.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
               01 T.
                  05 E OCCURS 3.
                     10 G.
                        15 K PIC 9.
               LINKAGE SECTION.
               01 L-X PIC 9(4) GLOBAL.
               PROCEDURE DIVISION USING L-X.
                   SORT E ASCENDING K.
                   CALL "PB4320B".
                   GOBACK.
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PB4320B.
               PROCEDURE DIVISION.
                   ADD 1 TO L-X.
                   GOBACK.
               END PROGRAM PB4320B.
               END PROGRAM PB4320A.
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PB4320C RECURSIVE.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
               01 S PIC 9 GLOBAL.
               PROCEDURE DIVISION.
                   CALL "PB4320D".
                   GOBACK.
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PB4320D.
               PROCEDURE DIVISION.
                   ADD 1 TO S.
                   GOBACK.
               END PROGRAM PB4320D.
               END PROGRAM PB4320C.
        """;

    [Fact]
    public void EveryFormerLeak_BindsToStructure_AndEveryNameIsAnIdentifier()
    {
        var comp = RepositoryCodecTests.Bind(Source, 2023);
        BoundUnit Unit(string name) => comp.Units.Single(u => u.Name == name);

        // Every data item of every unit keeps a NAME — the carrier-resident formal included.
        var notNames = comp.Units
            .SelectMany(u => u.Data.Roots.Concat(u.Data.LinkageRoots))
            .SelectMany(DataItem.SelfAndDescendantsOf)
            .Where(d => !Identifier.IsMatch(d.ClrName))
            .Select(d => $"{d.CobolName}: '{d.ClrName}'")
            .Distinct()
            .ToList();
        Assert.True(notNames.Count == 0, "A DataItem.ClrName holds access text, not a name: " + string.Join(", ", notNames));

        // The carrier-resident formal: its storage is reached through its carrier, a structural step.
        var formal = Unit("PB4320A").Data.LinkageFormals.Single();
        Assert.True(formal.CarrierResident);
        Assert.Equal(new FormalCarrier.Member(formal.CarrierField), formal.Item.ResidentCarrier);
        var place = Assert.IsType<MemberPlace>(Unit("PB4320A").Refs.Places.ResolveItem(formal.Item));
        Assert.Equal([typeof(RootFieldSegment), typeof(CarrierValueSegment)], place.Path.Segments.Select(s => s.GetType()));
        Assert.Equal(formal.CarrierField, ((RootFieldSegment)place.Path.Segments[0]).MemberClrName);

        // The contained program's bridge to it: the carrier's name, anchored one containing instance out.
        var carrierBridge = Unit("PB4320B").Bridges.Single(b => b.Kind == CallBridgeKind.Carrier);
        Assert.Equal(formal.CarrierField, carrierBridge.MemberClrName);
        Assert.Equal(new ContainerAnchor.Instance(1), carrierBridge.Anchor);

        // A RECURSIVE container's static GLOBAL item: anchored at the container's CLASS.
        var staticBridge = Unit("PB4320D").Bridges.Single(b => b.Kind == CallBridgeKind.Field);
        Assert.Equal(new ContainerAnchor.Static(Unit("PB4320C").ClassName), staticBridge.Anchor);
        Assert.Matches(Identifier, staticBridge.MemberClrName);

        // The table-sort key: the member chain from the element down to K, as items.
        var sort = Unit("PB4320A").Bound.Paragraphs.SelectMany(p => p.Sentences).SelectMany(s => s)
            .OfType<BoundTableSort>().Single();
        var chain = Assert.Single(sort.Keys).MemberChain;
        Assert.NotNull(chain);
        Assert.Equal(["G", "K"], chain.Select(d => d.CobolName));
    }

    // PB4380K: a METHOD whose elementary USING formal is carrier-resident (§14.2.3 GR8), the method arm of PB4320A's L-X.
    private const string MethodSource = """
               IDENTIFICATION DIVISION.
               CLASS-ID. PB4380K.
               OBJECT.
               PROCEDURE DIVISION.
               METHOD-ID. BUMP.
               DATA DIVISION.
               LINKAGE SECTION.
               01 L-N PIC 9(4).
               PROCEDURE DIVISION USING L-N.
                   ADD 1 TO L-N.
               END METHOD BUMP.
               END OBJECT.
               END CLASS PB4380K.
        """;

    /// <summary>⛔ A METHOD'S CARRIER IS A LOCAL, NAMED BY ITS IDENTITY (kb/Work PB4380; DESIGN-backend-abstraction §1.2,
    /// §1.3 L8). The method arm's carrier-resident formal is reached through the method-local carrier, so its access path
    /// is rooted at a <see cref="LocalRootSegment"/> holding the formal's <see cref="StatementLocalRole.MethodFormalCarrier"/>
    /// identity, never at a <see cref="RootFieldSegment"/> holding the C# local's name, as it was before PB4380.</summary>
    [Fact]
    public void AMethodsResidentFormal_IsRootedAtItsCarrierLocalsIdentity()
    {
        var comp = RepositoryCodecTests.Bind(MethodSource, 2023);
        var cls = comp.ClassUnits.Single(c => c.Name == "PB4380K");
        var formal = cls.Data.OoBoundMethods.Single().Binding!.Formals.Single();
        Assert.True(formal.CarrierResident);
        var carrier = new StatementLocal(StatementLocalRole.MethodFormalCarrier, formal.Item.Uid);
        Assert.Equal(new FormalCarrier.Local(carrier), formal.Item.ResidentCarrier);
        var place = Assert.IsType<MemberPlace>(cls.Refs.Places.ResolveItem(formal.Item));
        Assert.Equal([typeof(LocalRootSegment), typeof(CarrierValueSegment)], place.Path.Segments.Select(s => s.GetType()));
        var root = (LocalRootSegment)place.Path.Segments[0];
        Assert.Equal(carrier, root.Local);
        Assert.Equal(OmittedFormalGuard.Of(formal.Item), root.Guard);
    }

    // PB4416K: a METHOD whose data roots the method holds as locals of its own — a LOCAL-STORAGE group (§8.6.4: "Data items
    // defined in the local-storage section are automatic items"), a LOCAL-STORAGE Tier-B REDEFINES class (a binary view
    // over characters), the RETURNING item and an unattached LINKAGE record — beside an AREA formal (a FLOAT-LONG formal,
    // laid over its argument's cell, whose backing is the class's member bridge over that cell) and OBJECT data, which are
    // members of the class.
    private const string MethodDataSource = """
               IDENTIFICATION DIVISION.
               CLASS-ID. PB4416K.
               OBJECT.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
               01 OBJ-N PIC 9(4).
               PROCEDURE DIVISION.
               METHOD-ID. M.
               DATA DIVISION.
               LOCAL-STORAGE SECTION.
               01 LS-G.
                  05 LS-A PIC 9(4) VALUE 7.
               01 LS-R PIC X(4) VALUE "1234".
               01 LS-V REDEFINES LS-R PIC S9(8) USAGE BINARY.
               LINKAGE SECTION.
               01 L-F USAGE FLOAT-LONG.
               01 L-U PIC X(3).
               01 L-R PIC 9(4).
               PROCEDURE DIVISION USING L-F RETURNING L-R.
                   ADD LS-A TO L-R.
                   MOVE LS-R TO OBJ-N.
                   MOVE LS-V TO L-R.
               END METHOD M.
               END OBJECT.
               END CLASS PB4416K.
        """;

    /// <summary>⛔ A METHOD'S DATA IS A LOCAL, NAMED BY ITS IDENTITY (kb/Work PB4416; DESIGN-backend-abstraction §1.2,
    /// §1.3 L8). Every access path to a method root's own storage starts at a <see cref="LocalRootSegment"/> over the
    /// root's <see cref="MethodDataLocal"/>, and a path to a method Tier-B class's backing at one over its
    /// <see cref="MethodRedefinesBackingLocal"/> — never at a <see cref="RootFieldSegment"/> holding the C# local's name, as
    /// each was before PB4416 — while object data stays a member root. The roots are asked by enumeration, so a root kind
    /// added to the fixture is held at once.</summary>
    [Fact]
    public void AMethodsDataRoots_AreRootedAtTheirLocalsIdentity()
    {
        var comp = RepositoryCodecTests.Bind(MethodDataSource, 2023);
        var cls = comp.ClassUnits.Single(c => c.Name == "PB4416K");
        var method = cls.Data.OoBoundMethods.Single().Binding!;
        var roots = method.LocalRoots.Concat(method.LinkageRoots).ToList();
        Assert.Equal(["LS-G", "LS-R", "LS-V", "L-F", "L-U", "L-R"], roots.Select(r => r.CobolName));
        Assert.All(roots, r => Assert.Equal(new MethodDataLocal(r), r.ActivationLocal));

        // The roots whose own storage the activation holds, and the member beneath one.
        foreach (var name in new[] { "LS-G", "L-U", "L-R" })
        {
            var root = roots.Single(r => r.CobolName == name);
            var place = Assert.IsType<MemberPlace>(cls.Refs.Places.ResolveItem(root));
            var seg = Assert.IsType<LocalRootSegment>(Assert.Single(place.Path.Segments));
            Assert.Equal(root.ActivationLocal, seg.Local);
            Assert.Equal(OmittedFormalGuard.Of(root), seg.Guard);
        }
        var member = Assert.IsType<MemberPlace>(cls.Refs.Places.ResolveItem(roots[0].Children.Single()));
        Assert.Equal([typeof(LocalRootSegment), typeof(MemberSegment)], member.Path.Segments.Select(s => s.GetType()));
        Assert.Equal(roots[0].ActivationLocal, ((LocalRootSegment)member.Path.Segments[0]).Local);

        // The Tier-B class, LS-R with its redefiner LS-V, is a window over the class backing local, never over a member.
        foreach (var name in new[] { "LS-R", "LS-V" })
        {
            var item = roots.Single(r => r.CobolName == name);
            var redef = item.Class!;
            Assert.Equal(RedefinesTier.StringCanonical, redef.Tier);
            Assert.False(redef.IsCellBacked);
            var view = Assert.IsType<RedefViewPlace>(cls.Refs.Places.ResolveItem(item));
            var seg = Assert.IsType<LocalRootSegment>(Assert.Single(view.Backing.Segments));
            Assert.Equal(new MethodRedefinesBackingLocal(redef), seg.Local);
            Assert.Equal(redef.BackingLocal, seg.Local);
        }

        // The AREA formal's backing is the class's member bridge over the argument's cell (kb/Work PB956), so its class
        // has no backing local and its window starts at the member.
        var area = roots.Single(r => r.CobolName == "L-F");
        Assert.True(area.Class!.IsCellBacked);
        Assert.Null(area.Class.BackingLocal);
        var areaView = Assert.IsType<RedefViewPlace>(cls.Refs.Places.ResolveItem(area));
        Assert.Equal(area.Class.BackingClrName,
            Assert.IsType<RootFieldSegment>(Assert.Single(areaView.Backing.Segments)).MemberClrName);

        // Object data is a member of the class, so it keeps its member root and carries no local.
        var objectRoot = cls.Data.Roots.Single(r => r.CobolName == "OBJ-N");
        Assert.Null(objectRoot.ActivationLocal);
        var objectPlace = Assert.IsType<MemberPlace>(cls.Refs.Places.ResolveItem(objectRoot));
        Assert.Equal(objectRoot.ClrName, Assert.IsType<RootFieldSegment>(Assert.Single(objectPlace.Path.Segments)).MemberClrName);
    }

    /// <summary>The records that held joined access text or a local's name now carry no string but a member's NAME: a new
    /// string property on any of them is a failure here until it is shown to be a name, not an access or a local.</summary>
    [Theory]
    [InlineData(typeof(CallBridge), "MemberClrName")]
    [InlineData(typeof(BoundTableSortKey))]
    [InlineData(typeof(RootFieldSegment), "MemberClrName")]
    [InlineData(typeof(LocalRootSegment))]
    [InlineData(typeof(FormalCarrier.Member), "MemberClrName")]
    [InlineData(typeof(FormalCarrier.Local))]
    [InlineData(typeof(MethodDataLocal))]
    [InlineData(typeof(MethodRedefinesBackingLocal))]
    public void TheFormerAccessTextCarriers_HoldNoStringButAName(Type carrier, params string[] names)
    {
        var strings = carrier.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(p => p.PropertyType == typeof(string))
            .Select(p => p.Name)
            .Order(StringComparer.Ordinal);
        Assert.Equal(names.Order(StringComparer.Ordinal), strings);
    }
}
