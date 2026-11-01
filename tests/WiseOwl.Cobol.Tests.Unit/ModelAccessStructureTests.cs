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
        Assert.Equal(formal.CarrierField, formal.Item.ResidentCarrier);
        var place = Assert.IsType<MemberPlace>(Unit("PB4320A").Refs.ResolveItem(formal.Item));
        Assert.Equal([typeof(RootFieldSegment), typeof(CarrierValueSegment)], place.Path.Segments.Select(s => s.GetType()));
        Assert.Equal(formal.CarrierField, ((RootFieldSegment)place.Path.Segments[0]).CsField);

        // The contained program's bridge to it: the carrier's name, anchored one containing instance out.
        var carrierBridge = Unit("PB4320B").Bridges.Single(b => b.Kind == CallBridgeKind.Carrier);
        Assert.Equal(formal.CarrierField, carrierBridge.Field);
        Assert.Equal(new ContainerAnchor.Instance(1), carrierBridge.Anchor);

        // A RECURSIVE container's static GLOBAL item: anchored at the container's CLASS.
        var staticBridge = Unit("PB4320D").Bridges.Single(b => b.Kind == CallBridgeKind.Field);
        Assert.Equal(new ContainerAnchor.Static(Unit("PB4320C").ClassName), staticBridge.Anchor);
        Assert.Matches(Identifier, staticBridge.Field);

        // The table-sort key: the member chain from the element down to K, as items.
        var sort = Unit("PB4320A").Bound.Paragraphs.SelectMany(p => p.Sentences).SelectMany(s => s)
            .OfType<BoundTableSort>().Single();
        var chain = Assert.Single(sort.Keys).MemberChain;
        Assert.NotNull(chain);
        Assert.Equal(["G", "K"], chain.Select(d => d.CobolName));
    }

    /// <summary>The two records that held joined access text now carry no string but the bridged member's NAME: a new
    /// string property on either is a failure here until it is shown to be a name, not an access.</summary>
    [Theory]
    [InlineData(typeof(CallBridge), "Field")]
    [InlineData(typeof(BoundTableSortKey))]
    public void TheFormerAccessTextCarriers_HoldNoStringButAName(Type carrier, params string[] names)
    {
        var strings = carrier.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(p => p.PropertyType == typeof(string))
            .Select(p => p.Name)
            .Order(StringComparer.Ordinal);
        Assert.Equal(names.Order(StringComparer.Ordinal), strings);
    }
}
