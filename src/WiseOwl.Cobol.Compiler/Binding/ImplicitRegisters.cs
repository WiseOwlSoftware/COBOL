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
    /// "large enough to contain the data required"; docs/CONTROL_FLOW_DESIGN.md D20). Each member is a synthesized
    /// elementary VIEW of its 1985 implicit description over the program-instance <c>__dbgItem</c>
    /// (<see cref="WiseOwl.Cobol.Runtime.DebugItem"/>) — carried as a STRUCTURAL <see cref="DebugRegisterMember"/> selector
    /// (the C# read text lives in the renderer); the whole-group DEBUG-ITEM reads the concatenated image. ⛔ DEBUG-SUB-n IS A NUMERIC
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
        DataItem NewItem(string name, PicInfo pic) => new()
        {
            Level = 49,
            ClrName = NamingConvention.DebugRegisterName(name),
            CobolName = name,
            Pic = pic,
            Uid = takeUid(),
        };
        void Reg(string name, PicInfo pic, DebugRegisterMember member)
        {
            // A re-activation keeps a member that did not change (its unique id is its identity) and replaces the widened ones.
            var item = _debugRegisters.TryGetValue(name, out var held) && held.Item1.Pic == pic ? held.Item1 : NewItem(name, pic);
            _debugRegisters[name] = (item, member);
        }
        static PicInfo Alnum(int width) => new(PicCategory.Alphanumeric, Usage.Display, Length: width, Digits: 0, Scale: 0, Signed: false);
        // General rule 10: DEBUG-SUB-n is PICTURE S9999 SIGN IS LEADING SEPARATE — a separate leading sign and four digits, five characters.
        var sub = new PicInfo(PicCategory.Numeric, Usage.Display, Length: 4, Digits: 4, Scale: 0, Signed: true)
        { SignKind = PicInfo.SignKindFor(Usage.Display, signed: true, new SignSpec(Leading: true, Separate: true)) };
        Reg("DEBUG-ITEM", Alnum(Runtime.DebugItem.GroupWidthFor(contentsWidth)), DebugRegisterMember.Item);
        Reg("DEBUG-LINE", Alnum(Runtime.DebugItem.LineWidth), DebugRegisterMember.Line);
        Reg("DEBUG-NAME", Alnum(Runtime.DebugItem.NameWidth), DebugRegisterMember.Name);
        // The three members share ONE description (one profile): three items of the same PICTURE that only the member a place reads tells apart.
        DebugSubRegister ??= NewItem("DEBUG-SUB-1", sub);
        DebugSubRegister.Storage = new StorageForm.CharImage(DebugSubRegister.ImageWidth, PicCategory.Numeric);
        _debugRegisters["DEBUG-SUB-1"] = (DebugSubRegister, DebugRegisterMember.Sub1);
        _debugRegisters["DEBUG-SUB-2"] = (DebugSubRegister, DebugRegisterMember.Sub2);
        _debugRegisters["DEBUG-SUB-3"] = (DebugSubRegister, DebugRegisterMember.Sub3);
        Reg("DEBUG-CONTENTS", Alnum(contentsWidth), DebugRegisterMember.Contents);
    }

    /// <summary>Whether <paramref name="item"/> is the description of DEBUG-ITEM or one of its members (the item a reference to the register carries).</summary>
    public bool IsDebugRegister(DataItem item) => _debugRegisters.Values.Any(register => ReferenceEquals(register.Item1, item));

    /// <summary>The description every DEBUG-SUB-n member shares (general rule 10: <c>PICTURE S9999 SIGN IS LEADING SEPARATE</c>), null until
    /// <see cref="ActivateDebugRegisters"/> runs; the emitter declares its numeric profile.</summary>
    public DataItem? DebugSubRegister { get; private set; }

    /// <summary>The declared DEBUG-CONTENTS width of the DEBUG-ITEM register — the width the emitted <c>__dbgItem</c> is
    /// constructed with; 30 until <see cref="ActivateDebugRegisters"/> widens it.</summary>
    public int DebugContentsWidth { get; private set; } = Runtime.DebugItem.MinContentsWidth;
}
