// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.Binding;

/// <summary>
/// The registers the standard (or the 1985 debug module) describes IMPLICITLY, so no DATA DIVISION entry declares them
/// (docs/rearchitecture/DESIGN-architecture-review.md §8.3 (1); kb/Work PB2296 M6): the X3.23-1985 <c>DEBUG-ITEM</c> family
/// and the predefined object reference <c>EXCEPTION-OBJECT</c>. A value slice of <see cref="DataBindState"/>, kept OFF
/// <see cref="ForestSlice.ByName"/> and <see cref="ForestSlice.Roots"/> like the CAPACITY register: it is not storage this
/// program declares or emits; the resolver builds a <see cref="DebugRegisterPlace"/> or an <see cref="ExceptionObjectPlace"/>
/// VIEW over the runtime's. Its writers are the procedure table builder (the debug family, activated when a procedure-subject
/// debugging declarative is collected) and the first reference to <c>EXCEPTION-OBJECT</c> (synthesized on demand).
/// </summary>
public sealed class ImplicitRegisters
{
    private readonly Dictionary<string, (DataItem, DebugRegisterMember)> _debugRegisters = new(CobolNames.Comparer);

    /// <summary>The X3.23-1985 <c>DEBUG-ITEM</c> special register and its members (DEBUG-LINE / DEBUG-NAME / DEBUG-SUB-1/2/3 /
    /// DEBUG-CONTENTS), keyed by COBOL name (case-insensitive) → the synthesized alphanumeric <see cref="DataItem"/> VIEW + its
    /// read-only runtime read expression. The register is IMPLICITLY described (no DATA DIVISION entry, §ISO absent — 1985 debug
    /// module, VCR Table 7 row 7.17). Populated by <see cref="ActivateDebugRegisters"/> when a procedure-subject debugging
    /// declarative is collected under WITH DEBUGGING MODE (empty otherwise — a non-debug program's resolution is unchanged).</summary>
    public IReadOnlyDictionary<string, (DataItem Item, DebugRegisterMember Member)> DebugRegisters => _debugRegisters;

    /// <summary>The implicit description of the predefined object reference <c>EXCEPTION-OBJECT</c> (ISO §2023:8.4.3.6.3 SR2 —
    /// "EXCEPTION-OBJECT is implicitly described as class object and category object reference, as an external data item, and as
    /// a universal object reference"; kb/Work PB922), synthesized on first reference and shared thereafter: §2023:8.4.3.6.4 GR2 gives
    /// the run unit ONE instance of it. It is a DESCRIPTION carrier only, so every class/category screen (§2023:14.9.25.3 SR1's
    /// "shall not be … object", a relation condition's operand class, an argument's class) reads it as the object-reference item
    /// SR2 says it is.</summary>
    public DataItem ExceptionObjectRegister => field ??= new DataItem
    {
        Level = 1,
        CobolName = "EXCEPTION-OBJECT",
        ClrName = "__exceptionObject",
        Pic = PicInfo.ObjectReferenceItem(ObjectRefDescriptor.Universal),
        // ⚠ SR2's "as an external data item" is NOT carried as HasExternalClause: that flag drives the
        // §2023:13.18.22 EXTERNAL-store re-basing of a DECLARED record, and this item is never declared, laid out or
        // emitted. What the phrase asserts — one instance shared by the whole run unit (§2023:8.4.3.6.4 GR2) — is
        // already realized by the runtime's single ExceptionState, which is what the place reads.
    };

    /// <summary>Register the X3.23-1985 DEBUG-ITEM special-register family — called by the procedure table builder when it
    /// collects a <c>USE FOR DEBUGGING</c> declarative under WITH DEBUGGING MODE, BEFORE statement binding resolves the
    /// DEBUG-* references inside the debugging section, and again by <c>FinalizeDebug</c> once the targets are known, to
    /// WIDEN DEBUG-CONTENTS to the largest of 30 and every data-name or cd-name target's image width (general rule 15:
    /// "large enough to contain the data required"; docs/CONTROL_FLOW_DESIGN.md D20).
    /// <para>⛔ DEBUG-ITEM IS A GROUP, AND IS MODELED AS ONE (kb/Work PB4903). General rule 10 describes
    /// <c>01 DEBUG-ITEM</c> with eleven subordinate entries (the six members and the single-space FILLER between each pair), so
    /// the item is a <see cref="DataItem"/> group whose <see cref="DataItem.Children"/> are exactly those entries: every consumer
    /// that walks a group by its children (INITIALIZE's receiver walk, MOVE CORRESPONDING's pair matcher, the group image width)
    /// sees the description the standard gives, instead of one alphanumeric item that INITIALIZE would fill with spaces. Each
    /// member is a synthesized elementary VIEW over the program-instance <c>__dbgItem</c>
    /// (<see cref="WiseOwl.Cobol.Runtime.DebugItem"/>) — carried as a STRUCTURAL <see cref="DebugRegisterMember"/> selector
    /// (the C# read text lives in the renderer); the whole-group DEBUG-ITEM reads the concatenated image.</para>
    /// ⛔ DEBUG-SUB-n IS A NUMERIC
    /// ITEM: general rule 10 describes it as <c>PICTURE S9999 SIGN IS LEADING SEPARATE</c>, so its profile is that PICTURE's and it is
    /// IMAGE-CARRIED (<see cref="StorageForm.CharImage"/>, the report sum counter's precedent): the register stores its five-character
    /// image (<c>+0003</c>, SPACES for a reference that is not subscripted) and every numeric consumer decodes it — a comparison,
    /// COMPUTE or DISPLAY sees the value, and a MOVE to an alphanumeric item drops the separate sign (§2023:14.9.25.4 GR6; CCVS DB201A
    /// moves it to X(5) and compares <c>"0005"</c>). <paramref name="takeUid"/> is the binder's unique-id counter
    /// (<c>DataBinder.TakeUid</c>).</summary>
    internal void ActivateDebugRegisters(Func<int> takeUid, int contentsWidth = Runtime.DebugItem.MinContentsWidth)
    {
        contentsWidth = Math.Max(contentsWidth, Runtime.DebugItem.MinContentsWidth);
        if (_debugRegisters.Count > 0 && contentsWidth <= DebugContentsWidth) return;
        DebugContentsWidth = contentsWidth;
        DataItem NewItem(string? name, PicInfo pic) => new()
        {
            Level = 2,
            ClrName = name is null ? "Filler" : NamingConvention.DebugRegisterName(name),
            CobolName = name,
            Pic = pic,
            Uid = takeUid(),
        };
        // A re-activation keeps a member that did not change (its unique id is its identity) and replaces the widened ones.
        DataItem Member(string name, PicInfo pic) =>
            _debugRegisters.TryGetValue(name, out var held) && held.Item1.Pic == pic ? held.Item1 : NewItem(name, pic);
        static PicInfo Alnum(int width) => new(PicCategory.Alphanumeric, Usage.Display, Length: width, Digits: 0, Scale: 0, Signed: false);
        // General rule 10: DEBUG-SUB-n is PICTURE S9999 SIGN IS LEADING SEPARATE — a separate leading sign and four digits, five characters.
        var sub = new PicInfo(PicCategory.Numeric, Usage.Display, Length: 4, Digits: 4, Scale: 0, Signed: true)
        { SignKind = PicInfo.SignKindFor(Usage.Display, signed: true, new SignSpec(Leading: true, Separate: true)) };
        DataItem SubMember(string name)
        {
            var item = Member(name, sub);
            item.Storage = new StorageForm.CharImage(item.ImageWidth, PicCategory.Numeric);
            return item;
        }

        var group = new DataItem
        {
            Level = 1,
            ClrName = NamingConvention.DebugRegisterName("DEBUG-ITEM"),
            CobolName = "DEBUG-ITEM",
            Uid = takeUid(),
        };
        // The implicit description, in order (general rule 10): each member, then the single-space FILLER that follows it.
        (DataItem Item, DebugRegisterMember Member)[] members =
        [
            (Member("DEBUG-LINE", Alnum(Runtime.DebugItem.LineWidth)), DebugRegisterMember.Line),
            (Member("DEBUG-NAME", Alnum(Runtime.DebugItem.NameWidth)), DebugRegisterMember.Name),
            (SubMember("DEBUG-SUB-1"), DebugRegisterMember.Sub1),
            (SubMember("DEBUG-SUB-2"), DebugRegisterMember.Sub2),
            (SubMember("DEBUG-SUB-3"), DebugRegisterMember.Sub3),
            (Member("DEBUG-CONTENTS", Alnum(contentsWidth)), DebugRegisterMember.Contents),
        ];
        for (int i = 0; i < members.Length; i++)
        {
            group.AddMember(members[i].Item);
            members[i].Item.Parent = group;
            _debugRegisters[members[i].Item.CobolName!] = members[i];
            if (i < members.Length - 1) AddFiller(group, NewItem(null, Alnum(1)));
        }
        _debugRegisters["DEBUG-ITEM"] = (group, DebugRegisterMember.Item);
        _debugSubRegisters = [members[2].Item, members[3].Item, members[4].Item];

        static void AddFiller(DataItem g, DataItem filler) { g.AddMember(filler); filler.Parent = g; }
    }

    /// <summary>The members of the DEBUG-ITEM group in the order of its implicit description (general rule 10) — the named
    /// entries only; the single-space FILLER between each pair is not a receiver (§2023:14.9.20.4 GR5a2) and has no register.
    /// Empty until <see cref="ActivateDebugRegisters"/> runs.</summary>
    public IEnumerable<(DataItem Item, DebugRegisterMember Member)> DebugItemMembers =>
        _debugRegisters.Values.Where(r => r.Item2 is not DebugRegisterMember.Item).OrderBy(r => r.Item2);

    /// <summary>The <see cref="DebugRegisterPlace"/> of a member (or of the whole group) of the DEBUG-ITEM register, by its
    /// item — the one place a CORRESPONDING pair and an INITIALIZE receiver reach a member by the entry the group lists —
    /// or <see langword="null"/> for an item that is no register.</summary>
    public DebugRegisterPlace? DebugPlaceOf(DataItem item) =>
        _debugRegisters.Values.Where(r => ReferenceEquals(r.Item1, item)).Select(r => new DebugRegisterPlace(r.Item1, r.Item2)).FirstOrDefault();

    /// <summary>Whether <paramref name="item"/> is the DEBUG-ITEM group or one of its named members (the item a reference to the register
    /// carries; a FILLER entry has no register and no reference reaches it).</summary>
    public bool IsDebugRegister(DataItem item) => DebugPlaceOf(item) is not null;

    private DataItem[] _debugSubRegisters = [];

    /// <summary>The three DEBUG-SUB-n members (general rule 10: <c>PICTURE S9999 SIGN IS LEADING SEPARATE</c>), empty until
    /// <see cref="ActivateDebugRegisters"/> runs; the emitter declares each one's numeric profile.</summary>
    public IReadOnlyList<DataItem> DebugSubRegisters => _debugSubRegisters;

    /// <summary>The declared DEBUG-CONTENTS width of the DEBUG-ITEM register — the width the emitted <c>__dbgItem</c> is
    /// constructed with; 30 until <see cref="ActivateDebugRegisters"/> widens it.</summary>
    public int DebugContentsWidth { get; private set; } = Runtime.DebugItem.MinContentsWidth;
}
