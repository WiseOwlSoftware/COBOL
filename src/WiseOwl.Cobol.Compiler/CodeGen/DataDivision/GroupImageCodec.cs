// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Common;
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.CodeGen.Emit;
using OdoTail = WiseOwl.Cobol.Runtime.OdoTail;

namespace WiseOwl.Cobol.CodeGen;

/// <summary>The whole-group character-image codec (P7 Step 9l; DESIGN §14.4): the generated
/// <c>AsImage</c>/<c>FromImage</c> pair every image-capable group carries — string leaves verbatim, NATIVE
/// fixed-point leaves through their zoned image profile, a USAGE BIT run through <see cref="BitImageCodec"/> — and the
/// variable-length group's current-extent image, activation-boundary carrier (<c>AsVarImage</c>/<c>FromVarImage</c>) and
/// record layout. The compile-time INITIAL image a byte-addressed area starts at is <see cref="ImageInitializer"/>'s
/// (DESIGN-architecture-review §8.3, kb/Work PB2337). Wired by <see cref="DataEmitter"/>.</summary>
internal sealed class GroupImageCodec(PhysicalModel phys)
{
    /// <summary>Emit a variable-length group's <c>CurrentImage()</c> — the §2023:14.9.11.4 GR7 implementor-defined
    /// DISPLAY format, documented as CONFORMANCE.md A.1 item 57 (kb/Work PB164): the members' images in
    /// declaration order, each FIXED member contributing exactly its <see cref="AsImageOf"/> recipe (the ONE
    /// member-image law — no second copy), each dynamic-length leaf its CURRENT content, each dynamic-capacity
    /// table every occurrence at its CURRENT capacity, each nested SCALAR variable-length group its own
    /// <c>CurrentImage()</c>, each FIXED-OCCURS table of variable-length group elements the concatenation of its
    /// occurrences' (kb/Work PB244), and the group's OCCURS DEPENDING table the prefix of its maximum image that its
    /// CURRENT count names (the <c>__odo</c> parameter, §2023:13.18.38.4 GR8). The geometry follows the §2023:15.50.4 RV7 LENGTH sum in CHARACTER POSITIONS —
    /// <c>FUNCTION LENGTH(G)</c> equals <c>CurrentImage().Length</c> (a NATIONAL member contributes the two bytes
    /// it occupies, A.1 item 57), and a table of variable-length ELEMENTS under OCCURS DEPENDING or dynamic
    /// capacity contributes each occurrence's own current image (<see cref="VarPartKind.OdoTable"/> and the
    /// dynamic-table element lane — kb/Work PB244). DISPLAY and LENGTH read it; the static record codec
    /// (<c>AsImage</c>/<c>FromImage</c>) still excludes these groups (D9 — no fixed record window).</summary>
    public void EmitCurrentImageMethod(DataItem group, CodeWriter w)
    {
        // ONE classification (VarParts) feeds this composer, the boundary carrier and the record layout, so the
        // DISPLAY format, the crossing and the file image cannot disagree about what a member is.
        var parts = VarParts(group).Select(CurrentPartImage).ToList();
        w.Line($"public readonly string CurrentImage({OdoParameter(group)}) => "
            + $"{(parts.Count > 0 ? string.Join(" + ", parts) : "\"\"")};");
    }

    /// <summary>The parameter list of a variable-length group's current-extent methods (<c>CurrentImage</c>,
    /// <c>AsVarImage</c>, <c>CurrentExtents</c>): the occurrence count of the group's OCCURS DEPENDING table when
    /// it holds one, else nothing. ⛔ The count is a PARAMETER because the composing method is a struct instance
    /// method while data-name-1 may live outside the group entirely (the PB164 fleet's reason for refusing the
    /// shape); the operand's access path supplies it (<c>PlaceRenderer</c>: the clamped current count for a
    /// statement's operand, the maximum at an activation boundary — §2023:14.8.2.2 "the maximum length is used").</summary>
    internal static string OdoParameter(DataItem group) => DataItem.HasOdoBeneath(group) ? "int __odo" : "";

    /// <summary>The argument that forwards <see cref="OdoParameter"/> to a nested group's method.</summary>
    private static string OdoArgument(DataItem group) => DataItem.HasOdoBeneath(group) ? "__odo" : "";

    /// <summary>Where the OCCURS DEPENDING table lies in a member that holds it as its TRAILING storage
    /// (§2023:13.18.38.3 SR22), read off <see cref="OdoModel.GeometryOf"/>, THE ONE geometry <c>OdoGroupPlace</c> also reads
    /// for a group operand (§2023:13.18.38.4 GR8), so the member's CURRENT image is the same character prefix of its maximum
    /// image the operand takes. ⛔ It used to take one occurrence's <see cref="DataItem.ImageWidth"/>, a national
    /// element's POSITIONS, where the image holds its BYTES (kb/Work PB546's two widths).
    /// <para>The table is counted in the member's own unit: CHARACTERS (bytes on the physical basis), or BITS when a
    /// USAGE BIT leaf lies in the subtree (§2023:8.5.1.6.3 places a bit item "at the next bit position in storage", so a bit
    /// occurrence shares bytes and the current extent is <c>ceiling(bits / 8)</c> characters, §2023:15.50.4 RV9; kb/Work
    /// PB3030). Every consumer asks this record, never the unit.</para></summary>
    /// <param name="Tail">The table as the runtime states it (<see cref="OdoModel.FixedImageTail"/>).</param>
    /// <param name="MemberWidth">The member's emitted field width in characters (its image at the maximum).</param>
    private readonly record struct OdoMemberTail(OdoTail Tail, int MemberWidth)
    {
        public static OdoMemberTail Of(VarPart p) =>
            new(OdoModel.FixedImageTail(p.Item!, DataItem.OdoTableOnOrBeneath(p.Item!)!), p.Field.Width);

        /// <summary>The member's extent in CHARACTERS at the count <paramref name="odo"/> (a C# expression): its
        /// maximum image less the characters beyond the count (<c>OdoTail.CutAt</c>, the bit arm's ceiling included).</summary>
        public string CurrentChars(string odo) => $"{MemberWidth} - {RuntimeApi.OdoTailCutAt(Tail, odo)}";

        /// <summary>The character offset in the member where the table starts: the RECORD layout takes the table out of the
        /// fixed run there and makes it its last component (determination D-FRA (ix); <c>CobolContiguousLayout.Of</c> reads
        /// its unit and most units off the tail, <c>OdoTail.RecordUnit</c> / <c>RecordMaxUnits</c>).</summary>
        public int TableStart => MemberWidth - Tail.Width;

        /// <summary>The C# statement that keeps the member's occurrences past the RECEIVING count <c>__odo</c>
        /// (§2023:13.18.38.4 GR8a): the received fixed run <c>__s</c>, whose member starts at character
        /// <paramref name="off"/>, takes the member's own content (<paramref name="ownImage"/>) from the first unused
        /// occurrence on — <c>OdoTail.KeepUnused</c>, the one splice a cell-backed group's store asks too (a bit window
        /// for a table counted in bits, whose first unused occurrence may start inside a character).</summary>
        public string KeepUnusedOccurrences(int off, string ownImage) =>
            $"if (__odo < {Tail.Max}) __s = __s[..{off}] + "
            + RuntimeApi.OdoTailKeepUnused(Tail, $"__s.Substring({off}, {MemberWidth})", ownImage, "__odo")
            + $" + __s[{off + MemberWidth}..];";
    }

    /// <summary>The image of a member holding the OCCURS DEPENDING table, truncated to the table's current count.</summary>
    private static string OdoPrefixImage(VarPart p) =>
        $"{AsImageOf(p.Field)}.Substring(0, {OdoMemberTail.Of(p).CurrentChars("__odo")})";

    private string CurrentPartImage(VarPart p) => p.Kind switch
    {
        VarPartKind.Fixed => AsImageOf(p.Field),
        VarPartKind.OdoFixed => OdoPrefixImage(p),
        VarPartKind.DynLeaf or VarPartKind.DynTable or VarPartKind.DynGroupTable => CurrentMemberImage(p.Item!, agree: "true"),
        VarPartKind.Nested => $"{p.Field.Name}.CurrentImage({OdoArgument(p.Item!)})",
        VarPartKind.NestedTable =>
            $"string.Concat(global::System.Array.ConvertAll({p.Field.Name}, __e => __e.CurrentImage()))",
        // The first __odo occurrences, each at its own current extent (§2023:13.18.38.4 GR8; kb/Work PB244).
        VarPartKind.OdoTable => RuntimeApi.TableConcatImages(p.Field.Name, "__odo", "static __e => __e.CurrentImage()"),
        _ => throw new InvalidOperationException($"unknown variable-length part {p.Kind}"),
    };

    /// <summary>The carrier switches below handle exactly the kinds a carrier-capable group holds
    /// (<c>DataItem.CurrentExtentImageCapable</c>); an image-only kind reaching one is a compiler defect — the
    /// gate that emits the carrier methods is the only thing keeping it out — never a silent omission.</summary>
    private static InvalidOperationException ImageOnlyPartInCarrier(VarPart p) =>
        new($"variable-length part '{p.Field.Name}' ({p.Kind}) has an image but no place in the component carrier");

    /// <summary>The parameters of <c>AsVarImage</c>: the OCCURS DEPENDING count when the group holds the table
    /// (<see cref="OdoParameter"/>) and <c>__agree</c> — whether this is a STATEMENT's read of the group, which
    /// asks a LINKAGE dynamic-length member to agree with its DYNAMIC LENGTH clause (ISO §2023:14.6.13.2 GR5; kb/Work
    /// PB1937 / PB1118), as opposed to the activation boundary's own copy-out, which references nothing.</summary>
    private static string AsVarImageParameters(DataItem group) =>
        DataItem.HasOdoBeneath(group) ? "int __odo, bool __agree = false, bool __byReference = false"
                                      : "bool __agree = false, bool __byReference = false";

    /// <summary>The parameters of <c>FromVarImage</c>: the carrier, <c>__storage</c> (a LINKAGE formal's copy-in, kb/Work
    /// PB1937) and, for a group that holds the OCCURS DEPENDING table, the RECEIVING count <c>__odo</c> - how many
    /// occurrences the operation uses (kb/Work PB244; ISO §2023:13.18.38.4 GR8a): "If the data item referenced by data-name-1
    /// is outside the group, only that part of the table area that is specified by the value of the data item ... at the
    /// start of the operation will be used", so the occurrences past it keep their content). The default is the
    /// MAXIMUM - GR8 b) for a receiving group whose DEPENDING item is inside it, and the activation boundary's own
    /// copy-in, where "the maximum length is used" (§2023:14.8.2.2).</summary>
    private static string FromVarImageParameters(DataItem group) =>
        DataItem.HasOdoBeneath(group) ? "CobolVarGroup __v, bool __storage = false, int __odo = int.MaxValue"
                                      : "CobolVarGroup __v, bool __storage = false";

    private static string AsVarImageArguments(DataItem group) =>
        DataItem.HasOdoBeneath(group) ? "__odo, __agree, __byReference" : "__agree, __byReference";

    /// <param name="agree">null: the plain field; <c>"true"</c>: a sending read that agrees with the maximum when
    /// the member lies in the LINKAGE SECTION — storage the activating element wrote through ITS OWN description
    /// (§2023:14.2.3 GR8) and which <c>PlaceRenderer.Read</c> already agrees on an elementary reference
    /// (<c>ReadsForeignDynamicLength</c>); <c>"__agree"</c>: the same, decided at run time by the caller.</param>
    private string CurrentMemberImage(DataItem d, string? agree = null) =>
        d.IsDynamicTable ? $"{d.ClrName}.CurrentImage({DynElementImage(d)})"
        // §2023:15.50.4 RV7b — a dynamic-length leaf's current content at its current length, as the image it occupies: a
        // NATIONAL leaf's characters are its UTF-16BE byte pairs (D-N1), exactly as a fixed national member's are
        // (AsImageOf; kb/Work PB3441), so the group's image is its storage and DISPLAY, LENGTH and CONVERT ANY agree.
        : d.ImageUnit > 1 ? RuntimeApi.NatBytes(DynLeafContent(d, agree))
        : DynLeafContent(d, agree);

    /// <summary>A dynamic-length leaf's current content in ITS OWN characters (<see cref="CurrentMemberImage"/> images it).</summary>
    private static string DynLeafContent(DataItem d, string? agree) =>
        agree is not null && d.Section is EntrySection.Linkage
            ? agree == "true" ? RuntimeApi.DynAgree(d.ClrName, d.DynMaxSize)
                              : $"({agree} ? {RuntimeApi.DynAgree(d.ClrName, d.DynMaxSize)} : {d.ClrName})"
        : d.ClrName;

    /// <summary>The lambda that renders ONE element of the dynamic-capacity table <paramref name="d"/> as its image — what
    /// the table's current-extent image concatenates (<see cref="CurrentMemberImage"/>) and what its created element is
    /// carried as (<c>CobolDynTable.CreatedAs</c>, <see cref="EmitVarImageMethods"/>; kb/Work PB2690).</summary>
    private string DynElementImage(DataItem d) =>
        // A GROUP element of fixed image renders through its AsImage; one that is itself a variable-length group
        // (kb/Work PB244) through its own CurrentImage - each occurrence at its own current extent.
        d.IsGroup ? d.ElementTransferImageCapable ? "static __e => __e.AsImage()" : "static __e => __e.CurrentImage()"
        // A class pointer/object ELEMENT (kb/Work PB3358: a strong variable-length group's dynamic-capacity table of
        // them reaches the one-way CurrentImage only): each occurrence is its 8 positions exactly as a fixed table's
        // are (SlotImageOf) - the pointer's storage image of DOC-A.1-216, or the object's reserved placeholder. The
        // carrier never asks this arm: CurrentExtentImageCapable refuses a slot-carried element.
        : SlotWindow.CarriedBySlot(d)
            ? SlotWindow.CarriesTokenImage(d) ? $"static __e => {RuntimeApi.SlotImageOf(d, "__e")}"
                                                : $"static __e => {SlotPlaceholder(d.ByteWidth)}"
        // The element lane mirrors PhysicalModel's numLeaf rule exactly: a NATIVE numeric element goes
        // through its byte-form lane (float via the distinctly-named IEEE lane), a string-carried element
        // (alphanumeric / edited / StoreAsImage) passes through. A USAGE BIT element is the third lane (kb/Work
        // PB2818): its carrier is the '0'/'1' string of its boolean positions, and its slice of the image is
        // those positions PACKED into ceil(n/8) characters (§2023:8.5.1.6.3, §2023:13.18.60.4 GR5) - the width ByteWidth gives
        // the table's element, which FromCurrentImage divides the carried content by.
        : BitLayout.IsBitLeaf(d) ? $"__e => {RuntimeApi.BitsPack("__e", $"{d.Pic!.Length}")}"
        // A NATIONAL element is its characters' UTF-16BE byte pairs (kb/Work PB3441), the ElementWidth bytes the table's
        // elements are divided by (FromCurrentImage); the national-form NUMERIC composes the two as AsImageOf does (D-N7).
        : !d.StoreAsImage && d.Pic is { HasImageByteForm: true }
            ? $"__e => {NationalImage(d, d.Pic.IsFloat ? RuntimeApi.NumFormatImageFloat("__e", d.ProfileName, d.Pic.IsSingle)
                                                       : RuntimeApi.NumFormatImage("__e", d.ProfileName))}"
        : d.ImageUnit > 1 ? $"static __e => {RuntimeApi.NatBytes("__e")}"
        : "static __e => __e";

    /// <summary>The image of a leaf whose character image is <paramref name="characters"/>: those characters as UTF-16BE byte
    /// pairs when the leaf is USAGE NATIONAL (<see cref="DataItem.ImageUnit"/>), else themselves.</summary>
    private static string NationalImage(DataItem d, string characters) =>
        d.ImageUnit > 1 ? RuntimeApi.NatBytes(characters) : characters;

    // ── The VARIABLE-LENGTH GROUP's ACTIVATION-BOUNDARY codec (ISO §2023:14.8.2.2 / §2023:14.8.3.2 via §2023:14.9.4.3 SR25;
    //    kb/Work PB204) ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>What one member of a variable-length group contributes to its boundary carrier.</summary>
    private enum VarPartKind
    {
        /// <summary>A member with a fixed image — it lands in <c>CobolVarGroup.Fixed</c> at its own offset.</summary>
        Fixed,
        /// <summary>A dynamic-length elementary item — ONE carried component (§2023:8.5.1.10).</summary>
        DynLeaf,
        /// <summary>A dynamic-capacity table — ONE carried component, its occurrences concatenated (§2023:8.5.1.9).</summary>
        DynTable,
        /// <summary>A dynamic-capacity table whose ELEMENTS are variable-length groups (kb/Work PB2496) — ONE carried
        /// component, NESTED: its occurrences ride as their own carriers (<c>CobolVarGroup.Elements</c>), because their
        /// count is a run-time quantity and each holds components of its own, so flattening them would move the ordinal
        /// of every later component. §2023:14.6.9.2 moves such a table element by element, a group MOVE per occurrence.</summary>
        DynGroupTable,
        /// <summary>A nested SCALAR variable-length group — FLATTENED: its own fixed run joins ours and its own
        /// components join ours in place, because §2023:8.5.1.12 is stated over relative byte positions and is blind
        /// to the declaration tree.</summary>
        Nested,
        /// <summary>A FIXED-OCCURS table of variable-length group elements — <c>Occurs</c> times <see cref="Nested"/>,
        /// each occurrence flattened in place in order. The multiplicity is a COMPILE-TIME fact, so the carrier's
        /// ordinal component list still describes it (§2023:8.5.1.12.2 puts each occurrence's dynamic-length items at
        /// their own relative byte positions).</summary>
        NestedTable,
        /// <summary>A member of FIXED image that holds the group's OCCURS DEPENDING table as its trailing storage
        /// (§2023:13.18.38.3 SR22). Its image is the same recipe as <see cref="Fixed"/>'s, cut to the table's CURRENT
        /// count (<c>__odo</c>); its <see cref="VarPart.FixedWidth"/> is the MAXIMUM width, so every offset is the
        /// one the maximum-length formal sees (§2023:14.8.2.2: "the maximum length is used"). In the record layout the
        /// table is the last variable-length component (<see cref="ContiguousLayout"/>).</summary>
        OdoFixed,
        /// <summary>The group's OCCURS DEPENDING table itself, whose ELEMENTS are variable-length groups (kb/Work PB244):
        /// <see cref="NestedTable"/> at a count that is a parameter. Its IMAGE is each of the first <c>__odo</c>
        /// occurrences' own <c>CurrentImage()</c>, in order (§2023:13.18.38.4 GR8); its CARRIER is those occurrences'
        /// carriers concatenated (<c>CobolTable.ConcatVarImages</c>), and the receiving side takes every occurrence it
        /// has from whatever was carried (<c>FromVarImage</c> loops to the maximum; a slice the carrier does not hold is
        /// the §2023:14.9.25.4 GR9b excess part). Its <see cref="VarPart.FixedWidth"/> / <see cref="VarPart.DynCount"/> are
        /// the MAXIMUM's, so every offset is the one the maximum-length formal sees (§2023:14.8.2.2) and the table, the
        /// group's trailing storage (§2023:13.18.38.3 SR22), keeps every earlier component where it is. In the RECORD layout
        /// its components are listed at the maximum and a record holding N occurrences is laid out by the first N
        /// (<see cref="RecordOdo"/>; kb/Work PB2497).</summary>
        OdoTable,
    }

    /// <param name="Occurs">For <see cref="VarPartKind.NestedTable"/>: the occurrence count; else 1.</param>
    private readonly record struct VarPart(
        VarPartKind Kind, PhysicalModel.Physical Field, DataItem? Item, int FixedWidth, int DynCount, int Occurs = 1);

    /// <summary>A variable-length group's members classified, in PHYSICAL order — the ONE classification the
    /// DISPLAY / WRITE composer (<see cref="EmitCurrentImageMethod"/>), the activation-boundary carrier
    /// (<see cref="EmitVarImageMethods"/>) and the record layout (<see cref="ContiguousLayout"/>) all read, so the
    /// format on screen, the crossing and the file image can never disagree about what a member is. The gate that
    /// admits a group (<c>DataItem.CurrentExtentImageCapable</c>) has the same five member kinds.</summary>
    private List<VarPart> VarParts(DataItem group)
    {
        var composite = group.Children
            .Where(c => c.RedefinesTargetName is null && (c.IsGroup || c.IsElementary)
                && (c.IsDynamicTable || c.IsDynamicLength || (c.IsGroup && !c.TransferImageCapable)
                    || DataItem.HasOdoOnOrBeneath(c)))
            .ToDictionary(c => c.ClrName, StringComparer.Ordinal);
        var parts = new List<VarPart>();
        var runContinuations = new HashSet<string>(StringComparer.Ordinal);
        foreach (var f in phys.PhysicalChildrenOf(group))
        {
            // ⛔ A §2023:8.5.1.6.3 RUN IS ONE SLICE, AT ITS LEADER'S FIELD (PhysicalModel.Physical.BitRun), so the member a
            // run holds is classified THERE: an OCCURS DEPENDING table that continues a run (05 B PIC 1 USAGE BIT.
            // 05 TB PIC 1 USAGE BIT OCCURS 1 TO 16 DEPENDING ON N.) lives in the leader's characters, which are then
            // the OdoFixed part, cut to the table's count; the continuation's own field holds nothing (kb/Work PB2921
            // review R1-1: classifying the empty continuation cut "" at a negative length while the leader packed
            // the whole run at the maximum). SR22 makes the table the run's last member.
            DataItem? d;
            if (f.BitRun is { } run)
            {
                foreach (var m in run.Skip(1)) runContinuations.Add(m.ClrName);
                d = run.Select(m => composite.GetValueOrDefault(m.ClrName)).LastOrDefault(x => x is not null);
            }
            else d = runContinuations.Contains(f.Name) ? null : composite.GetValueOrDefault(f.Name);
            if (d is null)
            {
                parts.Add(new VarPart(VarPartKind.Fixed, f, null, f.Width, 0));
                continue;
            }
            parts.Add(d.IsDynamicLength ? new VarPart(VarPartKind.DynLeaf, f, d, 0, 1)
                : d.IsDynamicTable ? new VarPart(d.ElementTransferImageCapable ? VarPartKind.DynTable : VarPartKind.DynGroupTable, f, d, 0, 1)
                : d.TransferImageCapable ? new VarPart(VarPartKind.OdoFixed, f, d, f.Width, 0)
                : d.OccursSpec?.DependingOperand is not null
                    ? new VarPart(VarPartKind.OdoTable, f, d, RecordLayout.ItemSize(RecordLayout.Times(VarFixedWidth(d), d)),
                        RecordLayout.ItemSize(RecordLayout.Times(VarComponentCount(d), d)), RecordLayout.Occurrences(d))
                : d.Occurs is { } n ? new VarPart(VarPartKind.NestedTable, f, d, RecordLayout.ItemSize(RecordLayout.Times(VarFixedWidth(d), d)),
                    RecordLayout.ItemSize(RecordLayout.Times(VarComponentCount(d), d)), n)
                : new VarPart(VarPartKind.Nested, f, d, VarFixedWidth(d), VarComponentCount(d)));
        }
        return parts;
    }

    /// <summary>The OCCURS DEPENDING table the group holds as its trailing storage (§2023:13.18.38.3 SR22), as its record
    /// layout states it (<c>CobolContiguousLayout</c>'s <c>Odo</c>; kb/Work PB244, PB2497): for elements of a fixed
    /// image <see cref="OdoModel.FixedImageTail"/> over the member that holds it (<see cref="OdoModel.ImageHolder"/>, the
    /// <see cref="VarPartKind.OdoFixed"/> part), which the layout makes its LAST component, taken out of the fixed run
    /// (<see cref="OdoMemberTail.TableStart"/>); for variable-length elements one occurrence's fixed run, the maximum
    /// count and the element's own component count, whose components the layout lists at the maximum
    /// (<see cref="VarPartKind.OdoTable"/>, flattened in place). <c>default</c> for a group without one.</summary>
    private OdoTail RecordOdo(DataItem group) =>
        OdoModel.TableUnder(group) is not { } t ? default
        : !DataItem.HasVariableLengthSubordinate(t)
            ? OdoModel.FixedImageTail(OdoModel.ImageHolder(group, t), t)
        : new OdoTail(VarFixedWidth(t), RecordLayout.Occurrences(t), VarComponentCount(t));

    /// <summary>The character width of a variable-length group's FIXED run — its image with every
    /// variable-length component collapsed to nothing. This is the §2023:8.5.1.12.3 accounting the compatibility
    /// relation is stated in ("all dynamic-length elementary items are considered to be of zero length"),
    /// which is exactly why two COMPATIBLE groups lay this string out the same way.</summary>
    public int VarFixedWidth(DataItem group) => VarParts(group).Sum(p => p.FixedWidth);

    /// <summary>How many variable-length components a group carries, nested groups flattened in.</summary>
    public int VarComponentCount(DataItem group) => VarParts(group).Sum(p => p.DynCount);

    /// <summary>Does the group's carrier hold a NESTED component (<see cref="VarPartKind.DynGroupTable"/>) at any depth
    /// of its flattening — whether its <c>AsVarImage</c> states the carrier's <c>Elements</c> (kb/Work PB2496).</summary>
    private bool CarriesNested(DataItem group) =>
        VarParts(group).Any(p => p.Kind is VarPartKind.DynGroupTable
            || p.Kind is VarPartKind.Nested or VarPartKind.NestedTable or VarPartKind.OdoTable && CarriesNested(p.Item!));

    /// <summary>Does the group's carrier hold a dynamic-capacity table (<see cref="VarPartKind.DynTable"/>,
    /// <see cref="VarPartKind.DynGroupTable"/>) at any depth of its flattening — whether its <c>AsVarImage</c> states the
    /// carrier's <c>Created</c>, each table's creation of an element (kb/Work PB2690).</summary>
    private bool CarriesDynTable(DataItem group) =>
        VarParts(group).Any(p => p.Kind is VarPartKind.DynTable or VarPartKind.DynGroupTable
            || p.Kind is VarPartKind.Nested or VarPartKind.NestedTable or VarPartKind.OdoTable && CarriesDynTable(p.Item!));

    /// <summary>Emit a variable-length group's <c>AsVarImage()</c> / <c>FromVarImage()</c> — the boundary codec
    /// §2023:14.8.2.2 and §2023:14.8.3.2 need, the exact analogue of <c>AsImage</c>/<c>FromImage</c> for a group that has
    /// no fixed record window. Gated on <see cref="DataItem.CurrentExtentImageCapable"/>, THE ONE capability
    /// (kb/Work PB204): a group whose extent DISPLAY can compose is a group whose extent a crossing can carry,
    /// and giving the crossing its own gate would be the second copy this repo's two-arm rule forbids.
    /// <para>The fixed members ride exactly the <see cref="AsImageOf"/> / <see cref="EmitMemberFromImage"/>
    /// member-image law the record codec uses — the ONE recipe, no second spelling — at offsets computed with
    /// the variable-length components contributing ZERO, which is §2023:8.5.1.12.3's own convention.</para></summary>
    public void EmitVarImageMethods(DataItem group, CodeWriter w)
    {
        var parts = VarParts(group);
        int totalFixed = parts.Sum(p => p.FixedWidth);
        bool nested = CarriesNested(group);
        bool creates = CarriesDynTable(group);
        using (w.Block($"public readonly CobolVarGroup AsVarImage({AsVarImageParameters(group)})"))
        {
            var fixedParts = new List<string>();
            var dynParts = new List<string>();
            // Parallel to dynParts when the carrier holds a nested component (kb/Work PB2496): each component's
            // occurrence carriers, null for a plain one; a spliced nested group's own list, every entry, in place.
            var elemParts = new List<string>();
            // Parallel to dynParts when the carrier holds a dynamic-capacity table (kb/Work PB2690): the element each
            // INITIALIZED table creates (ISO §2023:8.5.1.9.5, CobolDynTable.CreatedAs, null for a table without the phrase),
            // which a BY REFERENCE formal's fixed table sees past the capacity and creates from; null for a dynamic-length
            // item; a spliced nested group's own list in place.
            // Stated only for STORAGE (__byReference: a BY REFERENCE crossing's carrier) - a MOVE's sender and a BY CONTENT
            // copy are values, and allocate nothing for it.
            var createdParts = new List<string>();
            int n = 0;
            foreach (var p in parts)
            {
                switch (p.Kind)
                {
                    case VarPartKind.Fixed:
                        if (p.Field.Width > 0 || p.Field.BitRun is not null) fixedParts.Add(AsImageOf(p.Field));
                        break;
                    case VarPartKind.OdoFixed:
                        // Rides the fixed run at the table's CURRENT count (the maximum at a boundary — the
                        // caller passes it): the carrier's fixed run is then as long as the sender's group is.
                        fixedParts.Add(OdoPrefixImage(p));
                        break;
                    case VarPartKind.DynLeaf:
                        dynParts.Add(CurrentMemberImage(p.Item!, agree: "__agree"));
                        elemParts.Add("null");
                        createdParts.Add("null");
                        break;
                    case VarPartKind.DynTable:
                        dynParts.Add(CurrentMemberImage(p.Item!, agree: "__agree"));
                        elemParts.Add("null");
                        createdParts.Add($"{p.Field.Name}.CreatedAs({DynElementImage(p.Item!)})");
                        break;
                    case VarPartKind.DynGroupTable:
                        // ONE nested component: no characters of its own, its occurrences' carriers beside it.
                        dynParts.Add("\"\"");
                        elemParts.Add($"{p.Field.Name}.CurrentOccurrencesAs(__e => __e.AsVarImage(__agree, __byReference))");
                        createdParts.Add($"{p.Field.Name}.CreatedAs(__e => __e.AsVarImage(__agree, true))");
                        break;
                    case VarPartKind.Nested:
                    case VarPartKind.NestedTable:
                    case VarPartKind.OdoTable:
                        // The nested group's own carrier (every occurrence's, in order, for a table), spliced in
                        // place: its fixed run extends ours and its components take the next slots (the flattening
                        // §2023:8.5.1.12 requires). An OCCURS DEPENDING table contributes the first __odo occurrences
                        // (the current count at a statement, the maximum at a boundary); the receiver loops to its
                        // own maximum below, and a slice the carrier did not carry is the GR9b excess part.
                        w.Line($"var __n{n} = " + p.Kind switch
                        {
                            VarPartKind.Nested => $"{p.Field.Name}.AsVarImage({AsVarImageArguments(p.Item!)});",
                            VarPartKind.NestedTable =>
                                $"{RuntimeApi.VarGroupConcat($"global::System.Array.ConvertAll({p.Field.Name}, __e => __e.AsVarImage(__agree, __byReference))")};",
                            _ => $"{RuntimeApi.TableConcatVarImages(p.Field.Name, "__odo", "__e => __e.AsVarImage(__agree, __byReference)")};",
                        });
                        fixedParts.Add($"__n{n}.Fixed");
                        dynParts.Add($".. __n{n}.Dynamic");
                        elemParts.Add($".. {RuntimeApi.VarGroupElementList($"__n{n}")}");
                        createdParts.Add($".. {RuntimeApi.VarGroupCreatedList($"__n{n}")}");
                        n++;
                        break;
                    default:
                        throw ImageOnlyPartInCarrier(p);
                }
            }
            w.Line($"return new CobolVarGroup({(fixedParts.Count > 0 ? string.Join(" + ", fixedParts) : "\"\"")}, "
                + $"[{string.Join(", ", dynParts)}]"
                + $"{(nested || creates ? $", {(nested ? $"[{string.Join(", ", elemParts)}]" : "null")}" : "")}"
                + $"{(creates ? $", __byReference ? [{string.Join(", ", createdParts)}] : null" : "")});");
        }
        // THE FILE-RECORD HALF (determination D-FRA; kb/Work PB981): a record read back as ONE contiguous image
        // (§2023:8.5.1.11.2 — what CurrentImage() wrote) is decomposed into this same carrier by the ONE split rule,
        // CobolContiguousLayout.Decompose, over the component layout below — the flattened VarParts walk, so a
        // nested variable-length group's components are located exactly where FromVarImage's Slice expects them.
        var layout = new List<LayoutComponent>();
        var tails = new List<int>();
        ContiguousLayout(group, 0, layout, tails);
        var odo = RecordOdo(group);
        // The OCCURS DEPENDING table of fixed-image elements is the layout's last component, and CobolContiguousLayout.Of
        // places it from the tail alone (the fixed run's last odo.Width characters, §2023:13.18.38.3 SR22): the walk's own
        // record of where the member holding it starts its table must be that place, or the two would disagree silently.
        if (tails.Count != (odo.Present && odo.Comps == 0 ? 1 : 0) || tails.Count == 1 && tails[0] != totalFixed - odo.Width)
            throw new InvalidOperationException(
                $"internal error: the OCCURS DEPENDING table of '{group.CobolName}' is not its layout's trailing component "
                + $"(walk {string.Join(", ", tails)}; tail at {totalFixed - odo.Width})");
        // ⛔ ONE LAYOUT OBJECT PER RECORD TYPE (kb/Work PB1025): the same layout locates a key a variable-length
        // member precedes (CobolContiguousLayout.Position, read through __Contiguous by the SORT/MERGE key and the
        // indexed key registrations), so the decomposition and the key window cannot disagree. It also carries each
        // component's DYNAMIC LENGTH STRUCTURE (kb/Work PB1094), so the same object frames the record on the way out
        // (MediumImage) and unframes it on the way in (Decompose).
        w.Line($"private static readonly CobolContiguousLayout {RuntimeApi.ContiguousLayoutField} = "
            + RuntimeApi.ContiguousLayoutOfComponents(totalFixed, layout.Select(l => l.FixedAt), layout.Select(l => l.MaxSize),
                layout.Select(l => l.LayoutCode), layout.Select(l => l.TableWidth), odo) + ";");
        w.Line($"public readonly CobolContiguousLayout {RuntimeApi.ContiguousLayoutProperty} => "
            + $"{RuntimeApi.ContiguousLayoutField};");
        // ⛔ THE EXTENT TABLE (D-FRA (v); kb/Work PB1053): the WRITE / REWRITE / RELEASE side sends where each
        // component of CurrentImage() ends, and the READ / RETURN side decomposes by it when it describes the record
        // received — so every layout round-trips, however many variable-length members flank a fixed one.
        // A table of variable-length ELEMENTS under OCCURS DEPENDING (kb/Work PB2497) has a run-time multiplicity of
        // components: the table states it by how many it describes (the layout above lists them at the maximum), and
        // a record that carries no table is decomposed at the count the READ / RETURN passes in __odo. The result says
        // whether the record stated its own count; when it did not, the landing decomposes it again at data-name-1's
        // value (PlaceRenderer.WriteVarGroupContiguous; determination D-FRA (viii)).
        w.Line($"public readonly RecordExtents CurrentExtents({OdoParameter(group)}) => "
            + $"{RuntimeApi.ContiguousLayoutField}.ExtentsOf(AsVarImage({OdoArgument(group)}));");
        using (w.Block("public bool FromContiguousImage(string __r, RecordExtents? __e, bool __fixedForm = false, int __odo = int.MaxValue)"))
        {
            w.Line($"FromVarImage({RuntimeApi.ContiguousLayoutField}.Decompose(__r, __e, __fixedForm, __odo));");
            w.Line($"return {RuntimeApi.ContiguousLayoutField}.StatesCount(__r, __e, __fixedForm);");
        }
        // ⛔ __storage (kb/Work PB1937) — the boundary copy-in of a LINKAGE formal: §2023:14.2.3 GR8 makes the formal
        // "occupy the same storage area as the argument", so a dynamic-length member keeps the argument's WHOLE
        // content even when this description's LIMIT is smaller (the shortened copy was then written back over the
        // caller's). A read through this description agrees with its maximum (CurrentMemberImage: §2023:14.6.13.2 GR5,
        // kb/Work PB1118); every other store — a MOVE into the group — still truncates (§2023:8.5.1.10.4).
        using (w.Block($"public void FromVarImage({FromVarImageParameters(group)})"))
        {
            w.Line($"string __s = {RuntimeApi.StrStore("__v.Fixed", $"{totalFixed}")};");
            int off = 0, dynAt = 0;
            foreach (var p in parts)
            {
                switch (p.Kind)
                {
                    case VarPartKind.Fixed:
                    case VarPartKind.OdoFixed:   // the whole maximum width: a carrier shorter than it pads (StrStore above)
                        if (p.Kind is VarPartKind.OdoFixed)
                        {
                            // §2023:13.18.38.4 GR8a: only the first __odo occurrences are used, so the table's remaining
                            // occurrences keep the content they have (the carrier's fixed run is spliced over the member's
                            // CURRENT image from the first unused occurrence on).
                            w.Line(OdoMemberTail.Of(p).KeepUnusedOccurrences(off, AsImageOf(p.Field)));
                        }
                        // A message tag decodes its token positions as every token leaf does (kb/Work PB4838), so a variable-length message area decomposes like any
                        // other; a RECEIVE then resolves a token of the sending run unit against the message's slots (AsyncMessaging.Resolved, kb/Work PB4957).
                        EmitMemberFromImage(p.Field, off, w);
                        off = RecordLayout.Advance(off, p.Field.Width);
                        break;
                    case VarPartKind.NestedTable:
                    case VarPartKind.OdoTable:   // every occurrence it USES (§2023:13.18.38.4 GR8 a: __odo of them), to its maximum
                        using (w.Block($"for (int __i = 0; __i < {(p.Kind is VarPartKind.OdoTable ? $"global::System.Math.Min({p.Occurs}, __odo)" : p.Occurs.ToString())}; __i++)"))
                            w.Line($"{p.Field.Name}[__i].FromVarImage(__v.Slice({off} + __i * {p.FixedWidth / p.Occurs}, "
                                + $"{p.FixedWidth / p.Occurs}, {dynAt} + __i * {p.DynCount / p.Occurs}, {p.DynCount / p.Occurs}), __storage);");
                        off = RecordLayout.Advance(off, p.FixedWidth);
                        dynAt += p.DynCount;
                        break;
                    // ⛔ EACH COMPONENT IS STORED BY THE CARRIER'S ONE RECEIVING STORE OF ITS KIND (kb/Work PB2746), the
                    // operation a cell-backed group's store (StorageCell.StoreVarGroupAt) calls too; the emitted text
                    // names only where the component lives and how one element decodes.
                    case VarPartKind.DynLeaf:
                        // §2023:8.5.1.10.4 — replace, truncate on the right to the LIMIT, never pad; a NATIONAL leaf's
                        // component is its UTF-16BE image, decoded first so the LIMIT counts POSITIONS (kb/Work PB3441).
                        w.Line($"{p.Field.Name} = {RuntimeApi.VarGroupReceivedDyn("__v", dynAt, p.Item!.DynMaxSize, "__storage", p.Item.ImageUnit > 1)};");
                        dynAt++;
                        break;
                    case VarPartKind.DynTable:
                        // §2023:14.6.9.2's recreation when carried, §2023:14.6.9.4's space fill when not (kb/Work PB393).
                        w.Line(RuntimeApi.VarGroupStoreTableComponent("__v", dynAt, p.Field.Name, p.Field.Width, p.Item!.ImageUnit,
                            TableElementFromImage(p.Item!)) + ";");
                        dynAt++;
                        break;
                    case VarPartKind.DynGroupTable:
                        // The same pair over the occurrences' own carriers, each stored by the element's own FromVarImage
                        // (kb/Work PB2496).
                        w.Line(RuntimeApi.VarGroupStoreGroupTableComponent("__v", dynAt, p.Field.Name,
                            "(__e, __x) => { __e.FromVarImage(__x, __storage); return __e; }") + ";");
                        dynAt++;
                        break;
                    case VarPartKind.Nested:
                        w.Line($"{p.Field.Name}.FromVarImage(__v.Slice({off}, {p.FixedWidth}, {dynAt}, {p.DynCount}), __storage"
                            + $"{(DataItem.HasOdoBeneath(p.Item!) ? ", __odo" : "")});");
                        off = RecordLayout.Advance(off, p.FixedWidth);
                        dynAt += p.DynCount;
                        break;
                    default:
                        throw ImageOnlyPartInCarrier(p);
                }
            }
        }
    }

    /// <summary>One variable-length component of a declared group's record layout, as <c>CobolContiguousLayout.Of</c> takes
    /// it (the one builder a cell-backed group's layout calls too, kb/Work PB2746): its offset in the FIXED run, its maximum
    /// (a table's maximum capacity; a dynamic-length item's maximum size in image characters, <see cref="DataItem.SlotMaxSize"/>),
    /// its layout code and its table element width (0 for a dynamic-length item).</summary>
    private readonly record struct LayoutComponent(int FixedAt, int MaxSize, int LayoutCode, int TableWidth);

    /// <summary>The flattened component list <c>CobolContiguousLayout.Of</c> builds a record's layout from: each
    /// variable-length component's offset in the FIXED run, its maximum, its layout code and its table element width
    /// (<see cref="LayoutComponent"/>). A dynamic-length item's layout code is <see cref="FileModel.LayoutCodeOf"/>, the one reader
    /// of what a structured item occupies in a record (its DYNAMIC LENGTH STRUCTURE, ISO §2023:12.3.7.4 GR18/GR19, or the
    /// unit of its image alone - two for a national item, kb/Work PB1094, PB3441); a dynamic-capacity table's is the same call (the bare unit of its elements,
    /// two for a national one, kb/Work PB6138) and its layout unit is its element width. Nested scalar variable-length groups flatten in place, the same flattening
    /// <see cref="VarParts"/> gives the carrier. The OCCURS DEPENDING table of fixed-image elements is NOT listed: the
    /// layout builder makes it the trailing component from the group's tail (<see cref="RecordOdo"/>), and
    /// <paramref name="tails"/> records where the walk found it, which the caller checks against that place.</summary>
    private void ContiguousLayout(DataItem group, int baseAt, List<LayoutComponent> layout, List<int> tails)
    {
        int off = baseAt;
        foreach (var p in VarParts(group))
            switch (p.Kind)
            {
                case VarPartKind.Fixed: off = RecordLayout.Advance(off, p.Field.Width); break;
                case VarPartKind.DynLeaf: layout.Add(new(off, p.Item!.SlotMaxSize, FileModel.LayoutCodeOf(p.Item), 0)); break;
                case VarPartKind.DynTable:
                case VarPartKind.DynGroupTable: layout.Add(new(off, p.Item!.VariableComponentMax, FileModel.LayoutCodeOf(p.Item), p.Field.Width)); break;
                case VarPartKind.Nested: ContiguousLayout(p.Item!, off, layout, tails); off = RecordLayout.Advance(off, p.FixedWidth); break;
                case VarPartKind.NestedTable:
                case VarPartKind.OdoTable:   // the components at the table's MAXIMUM: where compare / the boundary look for them
                    for (int i = 0; i < p.Occurs; i++)
                        ContiguousLayout(p.Item!, RecordLayout.OccurrenceAt(off, i, p.FixedWidth / p.Occurs), layout, tails);
                    off = RecordLayout.Advance(off, p.FixedWidth);
                    break;
                case VarPartKind.OdoFixed:
                    // The table itself leaves the FIXED run at the character its member starts it.
                    int startChar = OdoMemberTail.Of(p).TableStart;
                    tails.Add(off + startChar);
                    off = RecordLayout.Advance(off, startChar);
                    break;
                default:
                    throw ImageOnlyPartInCarrier(p);
            }
    }

    /// <summary>The inverse of <see cref="CurrentMemberImage"/>'s dynamic-table element lane, arm for arm: a
    /// group element distributes through its own <c>FromImage</c>, a USAGE BIT element unpacks its n positions from
    /// the ceil(n / 8) characters (kb/Work PB2818), a NATIVE numeric element decodes through its
    /// byte-form lane (a float through the distinctly-named IEEE lane), a string-carried element passes
    /// through. The seed occurrence is handed in so a group element keeps the arrays its initializer
    /// allocated. <c>sending: false</c> for the same reason the two <c>FromImage</c> sites below use it
    /// (kb/Work PB230): this is the RECEIVING side of a group decode — the program-visible sending
    /// reference was the write that produced the image, not this redistribution of it into members.
    /// <para>A group element that holds message tags distributes through its <c>FromImage</c>, whose tags decode their token positions
    /// (kb/Work PB4838); a RECEIVE resolves a token of the sending run unit against the message's slots afterwards (kb/Work PB4957).</para></summary>
    private static string TableElementFromImage(DataItem d)
    {
        // A NATIONAL element's image is its UTF-16BE byte pairs (DynElementImage): decode them back to its characters first,
        // then to the carrier (the inverse composition of AsImageOf's, design D-N7; kb/Work PB3441).
        string x = d.ImageUnit > 1 ? RuntimeApi.NatFromStorage("__x") : "__x";
        return d.IsGroup ? "(__e, __x) => { __e.FromImage(__x); return __e; }"
            : BitLayout.IsBitLeaf(d) ? $"(__e, __x) => {RuntimeApi.BitsUnpack("__x", $"{d.Pic!.Length}")}"
            : !d.StoreAsImage && d.Pic is { HasImageByteForm: true }
                ? (d.Pic.IsFloat
                    ? $"(__e, __x) => ({d.Pic.ClrType}){RuntimeApi.NumParseImageFloat(x, d.ProfileName, binary32Carrier: d.Pic.IsSingle)}"
                    : $"(__e, __x) => ({d.Pic.ClrType}){RuntimeApi.NumParseImage(x, d.ProfileName, sending: false)}")
            : $"(__e, __x) => {x}";
    }

    /// <summary>THE ONE RECIPE for what a class pointer/object/message-tag leaf occupies in a byte image (kb/Work PB231 ->
    /// PB244, PB1071, PB4838). A class-object leaf has no image: <paramref name="width"/> RESERVED placeholder positions,
    /// spaces. A POINTER or MESSAGE-TAG leaf's positions hold the token image <c>PointerImage</c> / <c>MessageTagImage</c> define
    /// (DOC-A.1-216, DOC-A.1-212 - the address or identifier in the implementation's binary order, NULL as zero):
    /// <c>ImageInitializer.SlotSeedOf</c> is its seed, the NULL image, and <see cref="SlotImageOf"/> its live value in a record struct. A
    /// shared cell keeps the same positions equal to its slot through <c>CobolPtr.SlotWrite</c> and ALLOCATE's seed, so a
    /// strongly-typed group DISPLAYs the same characters whether it lives in a record struct or in a BASED/ADDRESS-OF storage
    /// cell, and a file record carries the token and reads it back (<see cref="EmitMemberFromImage"/>).</summary>
    internal static string SlotPlaceholder(int width) => $"new string(' ', {width})";

    /// <summary>A slot-carried leaf field's slice of a record struct's one-way transfer image (<see cref="AsImageOf"/>),
    /// every occurrence of a fixed table at once: the field's current pointer image, or the placeholder.</summary>
    private static string SlotImageOf(PhysicalModel.Physical f, DataItem slot) =>
        !SlotWindow.CarriesTokenImage(slot) ? SlotPlaceholder(f.Width)
        : f.Occurs == 0 ? RuntimeApi.SlotImageOf(slot, f.Name)
        : $"string.Concat(global::System.Array.ConvertAll({f.Name}, __e => {RuntimeApi.SlotImageOf(slot, "__e")}))";

    /// <summary>The whole-group image facility of a record struct. <c>AsImage()</c> is emitted for every
    /// <see cref="DataItem.ElementTransferImageCapable"/> group; <c>FromImage</c> (and the bit / national faces)
    /// only for the two-way <see cref="DataItem.ElementRecordImageCapable"/> ones - a group with a class-object leaf has a
    /// one-way image and no inverse (kb/Work PB244; see <see cref="DataItem.TransferImageCapable"/>), while a pointer or
    /// message-tag leaf decodes its token (kb/Work PB4838).</summary>
    public void EmitImageMethods(DataItem group, CodeWriter w)
    {
        var members = phys.PhysicalChildrenOf(group);
        // ⛔ A BIT GROUP'S BYTE FACE IS ITS BIT FACE PACKED (§2023:13.18.29.4 GR1b, kb/Work PB3564): the bit codec emits both
        // faces from the one §2023:8.5.1.6.3 area; the member-by-member run packing below is for every other group.
        if (group.GroupUsage is GroupUsage.Bit && group.ElementImageCapable)
        {
            BitImageCodec.EmitBitGroupFaces(group, w);
            return;
        }
        w.Line($"public readonly string AsImage() => {(members.Count > 0 ? string.Join(" + ", members.Select(AsImageOf)) : "\"\"")};");
        // ⛔ A RECEIVED MESSAGE IS STORED BY THIS SAME FromImage (kb/Work PB4957; §2023:14.9.31.4 GR1b: "The content of identifier-1 is the content of the
        // message"): a tag's positions decode as a file record's do, and a token of the SENDING run unit, which this one cannot decode, is resolved against the
        // slots the message carries beside its image afterwards (AsyncMessaging.Resolved), so there is no second receiving half to skip it.
        if (!group.ElementRecordImageCapable) return;   // one-way (PB244): no FromImage over an object leaf, which has no token
        using (w.Block("public void FromImage(string __s)"))
        {
            w.Line($"__s = {RuntimeApi.StrStore("__s", $"{members.Sum(f => f.Width)}")};");   // pad/truncate to the image width
            int off = 0;
            foreach (var f in members)
            {
                EmitMemberFromImage(f, off, w);
                off = RecordLayout.Advance(off, f.Width);
            }
        }
        if (group.GroupUsage is GroupUsage.Bit) BitImageCodec.EmitBitMethods(group, w);
        if (group.GroupUsage is GroupUsage.National) EmitNatMethods(group, w);
    }

    /// <summary>A NATIONAL GROUP's elementary face (ISO §2023:13.18.29.4 GR2b — "a national group is treated as though
    /// it were an elementary data item of usage national and class and category national described with PICTURE
    /// N(m), where m is the length of the group"; D20/PB79, kb/Work PB327): <c>AsNat()</c> is the group's m
    /// national CHARACTER positions and <c>FromNat</c> stores m of them back. The BYTE form stays
    /// <c>AsImage()</c>/<c>FromImage()</c> (the record / REDEFINES / file image), exactly as it does for a bit
    /// group — two faces of ONE composition, so they cannot disagree.
    /// <para>⛔ DERIVED FROM THE IMAGE, never a second walk over the children: §2023:13.18.29.3 SR3 requires that "all
    /// elementary items subordinate to the subject of the entry shall be explicitly or implicitly described as
    /// usage national", so every byte pair of a national group's image IS a national character position and the
    /// two faces are exactly <c>NationalImage.Bytes</c> and its inverse. That is why this needs no <c>BitImageCodec.BitAreaOf</c>
    /// analogue: the bit face exists because §2023:8.5.1.6.3 lets same-level bit items SHARE a byte, and national
    /// positions never share one.</para>
    /// <para>The receiving side fits in POSITIONS before serializing (<c>CobolString.Store</c> then
    /// <c>NatBytes</c>): padding the BYTES with 0x20 would manufacture U+2020 characters instead of the national
    /// spaces §2023:14.9.25.4's national move requires — the identical trap <c>NationalImage.WriteWindow</c> documents.</para></summary>
    private static void EmitNatMethods(DataItem group, CodeWriter w)
    {
        // m — the as-if PICTURE N(m) length, THE ONE reader of it (DataItem.AsIfPic reads the same ImageWidth,
        // which stays the CARRIER width: national leaves contribute character positions to it, never bytes).
        int m = group.ImageWidth;
        w.Line($"public readonly string AsNat() => {RuntimeApi.NatReadWindow("AsImage()", "0", $"{m}")};");
        w.Line($"public void FromNat(string __n) => FromImage({RuntimeApi.NatBytes(RuntimeApi.StrStore("__n", $"{m}"))});");
    }

    /// <summary>One member's AsImage sub-expression: a scalar string field directly, a nested group's
    /// <c>AsImage()</c>, a NATIVE fixed-point leaf the BYTES it occupies (<c>CobolNum.FormatImage</c> with the
    /// leaf's own profile — its zoned digits for USAGE DISPLAY, its radix-2 / BCD bytes for BINARY / PACKED,
    /// §2023:13.18.60.4 GR4/GR11), or — for a fixed-OCCURS table — the concatenation of every occurrence's image
    /// (ISO §2023:14.9: a group move treats the whole group, INCLUDING every OCCURS position, as one alphanumeric
    /// item).</summary>
    private static string AsImageOf(PhysicalModel.Physical f) =>
        // D19/PB43 — a USAGE BIT run images as its PACKED bits (§2023:13.18.60.4 GR5), high-order first, and a run may
        // span several FIELDS because §2023:8.5.1.6.3 puts same-level bit items at successive bit positions. The run's
        // leader packs every member's carrier concatenated; a continuation contributed Width 0 and images as "".
        // ⛔ THE RUN'S TRAILING FILLER IS PART OF THE PACK (§2023:8.5.1.6.3, kb/Work PB3564): the positions after the last member,
        // up to the byte the next item starts on, are "a filler elementary bit data item" of the group and have a field
        // of their own (BitLayout.FillersOf), so the image carries what the last group move wrote there. Without it the
        // pack zero-filled them and MOVE "AB" TO G read back as NUL,B.
        f.BitRun is { } run ? BitImageCodec.PackedRunOf(run)
        : f.Width == 0 ? "\"\""
        // A class pointer/object leaf (kb/Work PB244) contributes its slice of the one-way transfer image, every
        // occurrence at once: the pointer's storage image (kb/Work PB1071) or the object's reserved placeholder
        // positions. Reached through the AsImage of a TransferImageCapable group; FromImage inverts it for a token leaf
        // (EmitMemberFromImage) and is never emitted over an object leaf.
        : f.SlotLeaf is { } slot ? SlotImageOf(f, slot)
        // ⛔ A NATIONAL LEAF IMAGES AS ITS BYTES (kb/Work PB327): two per character position, high-order first
        // (ISO §2023:13.18.60.4 GR8 leaves the size to the implementor — D-N1 pins two, UTF-16BE), through the ONE
        // serializer NationalImage.Bytes that the Tier-B window, the EXTERNAL/BASED cell seed and CONVERT's
        // raw-storage channel already ride. Concatenating the occurrences BEFORE serializing is the same string
        // as serializing each and concatenating, so the OCCURS arm needs no separate spelling.
        // ⛔ IT IS A TRANSFORM OVER THE CARRIER'S OWN CHARACTER IMAGE, NOT AN ALTERNATIVE TO IT (design D-N7,
        // kb/Work PB646). A national leaf's category decides what its characters ARE — for the category-national
        // and boolean forms the carrier IS those characters, and for the national-form NUMERIC of §2023:13.18.60.3
        // SR12 they are the zoned digit run CobolNum.FormatImage writes from the native carrier — while the
        // USAGE decides only how a character is SERIALIZED. Composing the two is what let SR12's numeric shapes
        // land without a second numeric byte form; the two used to be mutually exclusive arms, because the
        // national-form numeric was staged loud at PictureAnalyzer and could not reach emit.
        : f.NatLeaf is not null ? RuntimeApi.NatBytes(CarrierImageOf(f)) : CarrierImageOf(f);

    /// <summary>The field's contribution to its group's image in its own CHARACTER alphabet — a nested group's
    /// <c>AsImage()</c>, a native fixed-point leaf's zoned/radix-2/BCD/IEEE image through its own profile, else the
    /// carrier string verbatim. <see cref="AsImageOf"/> serializes the result (a national leaf's characters become
    /// UTF-16BE byte pairs; every other leaf's are its bytes already).</summary>
    private static string CarrierImageOf(PhysicalModel.Physical f) =>
        f.Occurs == 0
            ? (f.IsGroupStruct ? $"{f.Name}.AsImage()"
               // A FLOAT leaf encodes through the IEEE lane (kb/Work PB164 wave 2 — distinctly named so
               // integer call sites stay unambiguous).
               : f.NumLeaf is { } leaf ? (leaf.Pic!.IsFloat
                    ? RuntimeApi.NumFormatImageFloat(f.Name, leaf.ProfileName, leaf.Pic.IsSingle)
                    : RuntimeApi.NumFormatImage(f.Name, leaf.ProfileName))
               : f.Name)
        : f.IsGroupStruct ? $"string.Concat(global::System.Array.ConvertAll({f.Name}, __e => __e.AsImage()))"
        : f.NumLeaf is { } l ? $"string.Concat(global::System.Array.ConvertAll({f.Name}, __e => {(l.Pic!.IsFloat
                ? RuntimeApi.NumFormatImageFloat("__e", l.ProfileName, l.Pic.IsSingle)
                : RuntimeApi.NumFormatImage("__e", l.ProfileName))}))"
        : $"string.Concat({f.Name})";

    /// <summary>Distribute the slice of the image at <paramref name="off"/> into one member: a scalar string field
    /// gets its substring; a nested group gets <c>FromImage</c>; a NATIVE fixed-point leaf decodes its byte slice
    /// (<c>CobolNum.ParseImage</c> with the leaf's own profile, cast to its CLR storage type — an incompatible
    /// position, e.g. the spaces a short record's pad legitimately deposits, decodes deterministically per ISO
    /// §2023:14.6.13.2, see CobolNum); a fixed-OCCURS table loops its occurrences, each taking its per-occurrence width
    /// in source order (the array elements are value-type structs/strings, mutated in place).
    /// <para>ONE profile, never an image-specific override: since the image IS the item's bytes, a BINARY/PACKED
    /// leaf's sign lives in those bytes (two's complement / the sign nibble) and its <c>SignKind</c> — a DISPLAY
    /// concern — is not consulted at all. The former <c>ImageProfileOf</c> sign rewrite existed only because the
    /// image was a zoned digit run that had to carry a fixed-width sign.</para></summary>
    private static void EmitMemberFromImage(PhysicalModel.Physical f, int off, CodeWriter w)
    {
        // D19/PB43 — a USAGE BIT run's slice is unpacked once at its leader (BitImageCodec.EmitRunFromImage); the
        // continuations have Width 0 and consume no slice.
        if (f.BitRun is { } run)
        {
            BitImageCodec.EmitRunFromImage(run, off, f.Width, w);
            return;
        }
        if (f.Width == 0) return;   // a run continuation — its value came from the leader's unpack
        // ⛔ A POINTER OR MESSAGE-TAG LEAF DECODES ITS TOKEN (kb/Work PB4838): the inverse of SlotImageOf, through the decoder of the
        // leaf's own category (RuntimeApi.SlotFromImage), every occurrence of a fixed table in turn. Reached only through a group
        // whose every slot leaf CarriesTokenImage (DataItem.ElementRecordImageCapable) - an object leaf never gets a FromImage.
        if (f.SlotLeaf is { } slot)
        {
            if (f.Occurs == 0)
            {
                w.Line($"{f.Name} = {RuntimeApi.SlotFromImage(slot, $"__s.Substring({off}, {f.Width})")};");
                return;
            }
            int unit = f.Width / f.Occurs;
            using (w.Block($"for (int __i = 0; __i < {f.Occurs}; __i++)"))
                w.Line($"{f.Name}[__i] = {RuntimeApi.SlotFromImage(slot, $"__s.Substring({off} + __i * {unit}, {unit})")};");
            return;
        }
        // ⛔ THE INVERSE OF AsImageOf's national arm (kb/Work PB327): decode the UTF-16BE byte pairs back to the
        // leaf's character positions through NationalImage.ReadWindow — the inverse NationalImage.Bytes names, never
        // a second decoder. A pair the image is too short to hold decodes as the NATIONAL space, which is exactly
        // §2023:14.9.30.4 GR15's fill for a national record area ("a trailing space is defined to be the national space
        // character") — the connector's byte-level pad and this decode agree on it by construction.
        if (f.NatLeaf is { } nat)
        {
            // ⛔ THE INVERSE COMPOSITION OF AsImageOf's national arm (design D-N7, kb/Work PB646): the byte
            // pairs decode to the leaf's CHARACTER positions, and the carrier decoder below turns those
            // characters into the carrier — a string for the category-national/national-edited/boolean forms,
            // the native fixed-point value for §2023:13.18.60.3 SR12's national-form NUMERIC. Reading the window and
            // then NOT decoding the digits is what a single mutually-exclusive arm did, and it stored a string
            // into a `long` field (uncompilable generated C#, unreachable only while the category was staged).
            int pos = NationalWindow.PositionsOf(nat)!.Value;
            if (f.Occurs == 0)
            {
                w.Line($"{f.Name} = {CarrierFromChars(f, RuntimeApi.NatReadWindow("__s", $"{off}", $"{pos}"))};");
                return;
            }
            using (w.Block($"for (int __i = 0; __i < {f.Occurs}; __i++)"))
                w.Line($"{f.Name}[__i] = {CarrierFromChars(f, RuntimeApi.NatReadWindow("__s",
                    $"{off} + __i * {RecordLayout.ItemSize(RecordLayout.Product(RuntimeApi.BytesPerNational, pos))}", $"{pos}"))};");
            return;
        }
        if (f.Occurs == 0)
        {
            w.Line(f.IsGroupStruct
                ? $"{f.Name}.FromImage(__s.Substring({off}, {f.Width}));"
                : $"{f.Name} = {CarrierFromChars(f, $"__s.Substring({off}, {f.Width})")};");
            return;
        }
        int elem = f.Width / f.Occurs;   // per-occurrence width (Width = elem × Occurs, exact by construction)
        using (w.Block($"for (int __i = 0; __i < {f.Occurs}; __i++)"))
            w.Line(f.IsGroupStruct
                ? $"{f.Name}[__i].FromImage(__s.Substring({off} + __i * {elem}, {elem}));"
                : $"{f.Name}[__i] = {CarrierFromChars(f, $"__s.Substring({off} + __i * {elem}, {elem})")};");
    }

    /// <summary>⛔ THE ONE carrier decoder — the inverse of <see cref="CarrierImageOf"/>: the member's CHARACTER
    /// image (<paramref name="chars"/>) as its stored carrier. A native fixed-point leaf decodes through its own
    /// profile and is cast to its CLR storage type; a FLOAT leaf takes the IEEE bit-reinterpretation lane, because
    /// the Int128 lane's cast would numerically CONVERT the parsed integer (kb/Work PB164 wave 2); every
    /// string-shaped field is its characters already. An incompatible position — e.g. the spaces a short record's
    /// pad legitimately deposits — decodes deterministically per ISO §2023:14.6.13.2 (see CobolNum).
    /// <para>It is ONE function because it was three copies, and the national arm was a fourth that decoded
    /// nothing (kb/Work PB646).</para></summary>
    private static string CarrierFromChars(PhysicalModel.Physical f, string chars) =>
        f.NumLeaf is { } leaf
            ? $"({leaf.Pic!.ClrType}){(leaf.Pic!.IsFloat
                    ? RuntimeApi.NumParseImageFloat(chars, leaf.ProfileName, binary32Carrier: leaf.Pic.IsSingle)
                    : RuntimeApi.NumParseImage(chars, leaf.ProfileName, sending: false))}"
            : chars;
}
