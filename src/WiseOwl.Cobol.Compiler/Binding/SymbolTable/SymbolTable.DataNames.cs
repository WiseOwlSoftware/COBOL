// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding;

public sealed partial class SymbolTable
{
    /// <summary>
    /// Every named data item of this source element and every GLOBAL one a container offered, keyed by data-name → the items
    /// of that name (docs/rearchitecture/DESIGN-listing.md section 6.7.1; kb/Work PB5932, the closing slice of step 6b). COBOL
    /// permits duplicate data-names told apart only by qualification (§2023:8.4.2.2.1), so this is a MULTIMAP: a single-valued
    /// table would silently drop all but the last (DESIGN §3.5). The element's own entries first, in declaration order, then the
    /// inherited items as <see cref="InheritGlobalItem"/> joins them. A METHOD's own data-names are its <see cref="MethodNames"/>'.
    /// </summary>
    private readonly Dictionary<CobolName, List<DataItem>> _dataNames = [];

    /// <summary>The level-88 condition-names of this source element and the GLOBAL ones a container offered → the conditions of
    /// that name (a list: one name may be declared under several conditional variables and told apart by qualification).</summary>
    private readonly Dictionary<CobolName, List<Condition88>> _conditionNames = [];

    /// <summary>The names DECLARED in the SCREEN SECTION, which is refused as the declined Annex A.4.2 module (COBOL1560;
    /// <see cref="ScreenFacility"/>). Two questions still need them after the refusal: a reference to such a name is exempt from
    /// the §2023:8.4.2.1 UNDEFINED diagnostic (kb/Work R32: "declared in a refused section" is the true verdict, not "not
    /// defined"), and <c>ACCEPT screen-name-1</c> / <c>DISPLAY screen-name-1</c> are told from their token-identical DEVICE formats
    /// only by the operand's declaration (<see cref="DeclaresScreenName"/>). kb/Work PB260 made the section a refusal.</summary>
    private readonly HashSet<CobolName> _screenNames = [];

    /// <summary>The data-name table of the source element <paramref name="method"/> names: the method's own, else this element's.</summary>
    private Dictionary<CobolName, List<DataItem>> DataNamesOf(OoMethodDataScope? method) =>
        method is null ? _dataNames : MethodNamesOf(method).DataNames;

    /// <summary>Index a named data item in the data-name table of the source element that owns its record:
    /// <paramref name="method"/>'s own when its record is a method's (§2023:11.7.4 GR5; kb/Work PB3192), else this element's.
    /// The one insert of a bound entry and of a TYPE copy alike.</summary>
    internal void DeclareDataName(DataItem item, string name, OoMethodDataScope? method)
    {
        var table = DataNamesOf(method);
        var key = Key(name);
        if (!table.TryGetValue(key, out var list)) table[key] = list = [];
        list.Add(item);
    }

    /// <summary>Index a level-88 condition-name in the table of the source element that owns its conditional variable's record
    /// (<paramref name="method"/>'s when it is a method's; kb/Work PB3192).</summary>
    internal void DeclareConditionName(Condition88 cond, OoMethodDataScope? method)
    {
        var table = method is null ? _conditionNames : MethodNamesOf(method).ConditionNames;
        var key = Key(cond.Name);
        if (!table.TryGetValue(key, out var list)) table[key] = list = [];
        list.Add(cond);
    }

    /// <summary>Record the screen-names the refused SCREEN SECTION declared.</summary>
    internal void DeclareScreenNames(IEnumerable<string> names)
    {
        foreach (var n in names) _screenNames.Add(Key(n));
    }

    /// <summary>Whether the refused SCREEN SECTION declares <paramref name="word"/> (a declaration question: no symbol answers).</summary>
    internal bool DeclaresScreenName(string word) => _screenNames.Count != 0 && _screenNames.Contains(Key(word));

    /// <summary>Whether the SCREEN SECTION declared any name (so a caller asks nothing of an element that has none).</summary>
    internal bool HasScreenNames => _screenNames.Count != 0;

    /// <summary>⛔ THE ONE QUESTION "IS THIS WORD A DATA-NAME THIS SOURCE ELEMENT ITSELF DEFINES?" — the test behind every rule
    /// that reads "shall not be defined elsewhere in the source element" (§2023:13.18.38.3 SR30 for a CAPACITY register,
    /// §2023:13.18.64.3 SR2 for a report VARYING counter) and behind the report section's "also declared in ordinary storage"
    /// screens (§2023:8.4.2.1). A container's GLOBAL data-name is inherited into the table before the resolve passes run
    /// (<see cref="InheritGlobalItem"/>), but it is defined in ANOTHER source element: this element's own declaration of the
    /// spelling hides it (§2023:8.4.6.2.1 3), so it can neither duplicate nor be duplicated by it. Asking whether the table holds
    /// the spelling at all refuses a legal program (kb/Work PB3766, review finding R2-1).</summary>
    internal bool DeclaresOwnDataName(string name) =>
        _dataNames.TryGetValue(Key(name), out var items) && items.Any(i => _data.State.Forest.DeclaringDepth(i) == 0);

    /// <summary>Whether a data item of <paramref name="word"/> has been bound SO FAR in this element (own or inherited): a
    /// diagnostic's positive hint only, never a verdict (a later sibling is not registered yet).</summary>
    internal bool HasBoundDataName(string word) => _dataNames.ContainsKey(Key(word));

    /// <summary>Every data-name of this element (own and inherited, the TYPE copies included) with the items it names, in the
    /// order each name was first declared: an enumeration of declarations, no lookup by name, for the screens that judge every
    /// declaration of a spelling together (§2023:8.4.2.1).</summary>
    internal IEnumerable<(string Name, IReadOnlyList<DataItem> Items)> DataNameDeclarations()
    {
        foreach (var (name, items) in _dataNames) yield return (name.Word, items);
    }

    /// <summary>The level-88 entries a SELECT WHEN clause of <paramref name="record"/> may name (§2023:13.18.51.1: the clause
    /// "specifies a condition-name condition under which a record description entry is associated" with a record): every condition-name of the spelling the clause carries, resolved from the record that carries it as slice 6 resolved a
    /// restricted pointer's prototype from its item (section 6.7.1, "A name a SYMBOL carries is resolved by the table from the
    /// symbol"). Empty when the record has no SELECT WHEN condition-name.</summary>
    internal IReadOnlyList<Condition88> SelectWhenConditionsOf(DataItem record) =>
        record.SelectWhen is { ConditionName: { } name } && _conditionNames.TryGetValue(Key(name), out var list) ? list : [];

    /// <summary>
    /// ⭐ THE LOOKUP OF A WRITTEN DATA-NAME (§2023:8.4.2.2.2 Format 1; docs/rearchitecture/DESIGN-listing.md section 6.7.1, "Step 6b,
    /// remainder slice 1"): its §2023:8.4.2.2 candidate set in the active scope, whose count is the caller's uniqueness verdict
    /// (<c>NameResolution.DataItemNamed</c>, the procedure division's; the data division's clause operands meet
    /// <see cref="DataBinder.UniqueOrReportAmbiguous{T}"/>). A qualified name is matched over the whole visible set and then narrowed
    /// to the nearest declaring source element (§2023:8.4.6.2.1 3), <c>DataBinder.QualifiedCandidates</c>); an unqualified one is the
    /// nearest declaring element's items (<see cref="TryResolveUnqualified"/>), every survivor of its spelling counted with the
    /// REPORT SECTION entries of that element (kb/Work PB5775). A qualifiable name's identification IS its candidate set, typed and
    /// unboxed (the index-name's <see cref="IdentifyIndexName"/>, the condition-name's <see cref="IdentifyConditionName"/>).
    /// <paramref name="report"/> is <see cref="Identify(Antlr4.Runtime.ParserRuleContext, NameClass, bool, NameReach, ProcedureSite?, ReportSite)"/>'s:
    /// listing step 7b records the occurrence here when it is true and the set has one member. Pure: it reports nothing.
    /// <para>The two arms keep the two answers the procedure division gave before the lookup moved here (an unqualified name is not
    /// screened for a lapsed type-declaration member alone, kb/Work PB5932's dated file <c>2026-11-19-step-6b-r1</c>): the move is
    /// behaviour-neutral, and the one rule they should share is that file's lead.</para>
    /// </summary>
    internal NameCandidates<DataItem> IdentifyDataName(WrittenQualifiedName written, bool report)
    {
        _ = report;   // the occurrence it gates is listing step 7b's
        if (written.QualifierWords.Count > 0) return _data.QualifiedCandidates(written.Name, written.QualifierWords, _data.ActiveScope);
        if (!TryResolveUnqualified(written.Name, _data.ActiveScope, out var items)) return new([]);
        return new(items, _data.ReportSection.ReportEntriesInTierOf(new NameCandidates<DataItem>(items), written.Name, []));
    }
}
