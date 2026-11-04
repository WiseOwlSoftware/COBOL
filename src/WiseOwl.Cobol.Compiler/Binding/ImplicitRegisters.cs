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

    /// <summary>The implicit description of the predefined object reference <c>EXCEPTION-OBJECT</c> (ISO §8.4.3.6.3 SR2 —
    /// "EXCEPTION-OBJECT is implicitly described as class object and category object reference, as an external data item, and as
    /// a universal object reference"; kb/Work PB922), synthesized on first reference and shared thereafter: §8.4.3.6.4 GR2 gives
    /// the run unit ONE instance of it. It is a DESCRIPTION carrier only, so every class/category screen (§14.9.25.3 SR1's
    /// "shall not be … object", a relation condition's operand class, an argument's class) reads it as the object-reference item
    /// SR2 says it is.</summary>
    public DataItem ExceptionObjectRegister => field ??= new DataItem
    {
        Level = 1,
        CobolName = "EXCEPTION-OBJECT",
        ClrName = "__exceptionObject",
        Pic = PicInfo.ObjectReferenceItem(ObjectRefDescriptor.Universal),
        // ⚠ SR2's "as an external data item" is NOT carried as HasExternalClause: that flag drives the
        // §13.18.22 EXTERNAL-store re-basing of a DECLARED record, and this item is never declared, laid out or
        // emitted. What the phrase asserts — one instance shared by the whole run unit (§8.4.3.6.4 GR2) — is
        // already realized by the runtime's single ExceptionState, which is what the place reads.
    };

    /// <summary>Register the X3.23-1985 DEBUG-ITEM special-register family (idempotent) — called by the procedure table builder
    /// when it collects a <c>USE FOR DEBUGGING</c> procedure-subject declarative under WITH DEBUGGING MODE, BEFORE statement
    /// binding resolves the DEBUG-* references inside the debugging section. Each member is a synthesized elementary VIEW of
    /// its fixed 1985 width over the program-instance <c>__dbgItem</c> (<see cref="WiseOwl.Cobol.Runtime.DebugItem"/>) —
    /// carried as a STRUCTURAL <see cref="DebugRegisterMember"/> selector (the C# read text lives in the renderer); the
    /// whole-group DEBUG-ITEM reads the concatenated image. DEBUG-SUB-n carries the S9(4) SIGN LEADING SEPARATE image WIDTH (5)
    /// but as an alphanumeric character-image view: the procedure-trigger leg only ever renders it SPACES, and the subscripted
    /// numeric-value population + the §14.9.25.4 GR6a "sign not moved" MOVE semantics ride the staged data-name leg
    /// (COBOL1571). <paramref name="takeUid"/> is the binder's unique-id counter (<c>DataBinder.TakeUid</c>).</summary>
    internal void ActivateDebugRegisters(Func<int> takeUid)
    {
        if (_debugRegisters.Count > 0) return;
        void Reg(string name, int width, DebugRegisterMember member) =>
            _debugRegisters[name] = (new DataItem
            {
                Level = 49,
                ClrName = NamingConvention.DebugRegisterName(name),
                CobolName = name,
                Pic = new PicInfo(PicCategory.Alphanumeric, Usage.Display, Length: width, Digits: 0, Scale: 0, Signed: false),
                Uid = takeUid(),
            }, member);
        Reg("DEBUG-ITEM", Runtime.DebugItem.GroupWidth, DebugRegisterMember.Item);
        Reg("DEBUG-LINE", Runtime.DebugItem.LineWidth, DebugRegisterMember.Line);
        Reg("DEBUG-NAME", Runtime.DebugItem.NameWidth, DebugRegisterMember.Name);
        Reg("DEBUG-SUB-1", Runtime.DebugItem.SubWidth, DebugRegisterMember.Sub1);
        Reg("DEBUG-SUB-2", Runtime.DebugItem.SubWidth, DebugRegisterMember.Sub2);
        Reg("DEBUG-SUB-3", Runtime.DebugItem.SubWidth, DebugRegisterMember.Sub3);
        Reg("DEBUG-CONTENTS", Runtime.DebugItem.ContentsWidth, DebugRegisterMember.Contents);
    }
}
