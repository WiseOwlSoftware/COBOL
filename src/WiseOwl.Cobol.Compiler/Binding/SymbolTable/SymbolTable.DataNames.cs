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

    /// <summary>Record the screen-names the refused SCREEN SECTION declared, as written in source order, and answer each distinct name
    /// once, as first written, for the declaration funnel (§2023:8.3.2.2).</summary>
    internal List<string> DeclareScreenNames(IEnumerable<string> names)
    {
        List<string> declared = [];
        foreach (var n in names)
            if (_screenNames.Add(Key(n))) declared.Add(n);
        return declared;
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
    /// <see cref="DataBinder.UniqueOrReportAmbiguous{T}"/>). Every written qualifier is applied over the whole visible set, then the
    /// nearest declaring source element's survivors are kept (§2023:8.4.6.2.1 3)), counted with the REPORT SECTION entries of that
    /// element (kb/Work PB5775); an unqualified name is the same walk with no qualifier to apply (<see cref="DataNameCandidates"/>,
    /// one rule for both arms since remainder slice 3). A qualifiable name's identification IS its candidate set, typed and unboxed
    /// (the index-name's <see cref="IdentifyIndexName"/>, the condition-name's <see cref="IdentifyConditionName"/>).
    /// <paramref name="report"/> is <see cref="Identify(Antlr4.Runtime.ParserRuleContext, NameClass, bool, NameReach, ProcedureSite?, ReportSite)"/>'s:
    /// listing step 7b records the occurrence here when it is true and the set has one member. Pure: it reports nothing.
    /// </summary>
    internal NameCandidates<DataItem> IdentifyDataName(WrittenQualifiedName written, bool report) =>
        IdentifyDataName(written, _data.ActiveScope, report);

    /// <summary>
    /// ⭐ THE LOOKUP OF A DATA DIVISION CLAUSE'S WRITTEN DATA-NAME (§2023:8.4.2.2.2 Format 1; docs/rearchitecture/DESIGN-listing.md
    /// section 6.7.1, "Step 6b, remainder slice 2"): the §2023:8.4.2.2 candidate set of <paramref name="operand"/>, the clause's
    /// data-name-n as the model keeps it (<see cref="WrittenDataName"/>), in <paramref name="scope"/>. Every written qualifier is
    /// applied over the whole visible set, then the nearest declaring source element's survivors are kept (§2023:8.4.6.2.1 3)),
    /// counted with the REPORT SECTION entries of that element (kb/Work PB5775), so the count is the caller's uniqueness verdict
    /// (<see cref="DataBinder.UniqueOrReportAmbiguous{T}"/>, <c>DataBinder.ResolveClauseOperand</c>). A clause operand is resolved
    /// after the data division is declared, from the model, which holds no parse tree: the written words are the operand's.
    /// Pure: it reports nothing.
    /// </summary>
    internal NameCandidates<DataItem> IdentifyDataName(WrittenDataName operand, Scope scope) =>
        DataNameCandidates(operand.Name, operand.Qualifiers, scope);

    /// <summary>Whether a data item of <paramref name="operand"/>'s spelling is visible in <paramref name="scope"/> at all, its
    /// qualifiers not applied: the question a clause's unidentified-operand diagnostic asks to say "declared, but not under the
    /// given qualifiers" rather than "no declaration" (a declaration question: no symbol answers).</summary>
    internal bool DeclaresDataNameSpelled(WrittenDataName operand, Scope scope) => DeclaresDataName(operand.Name, scope);

    /// <summary>The presence fact of the formal parameter <paramref name="written"/> spells (§2023:8.8.4.8.3 SR1: data-name-1 of an
    /// omitted-argument condition "shall be a formal parameter defined in the source element in which this condition is
    /// specified"), or null when it spells none: inside a METHOD the method's own formals (<paramref name="methodFormals"/>,
    /// §2023:14.9.23.4 GR9), elsewhere the program's or function's (<c>LinkageBinder.Formals</c>). The formals are the source
    /// element's USING operands, so the condition's word is matched against their data-names here, where every comparison of
    /// names is the table's (section 6.7.1, rule (5)); kb/Work PB133, PB757.</summary>
    internal OmittedProbe? FormalSpelled(Antlr4.Runtime.ParserRuleContext written, IReadOnlyList<OoFormal>? methodFormals)
    {
        var key = Key(written.GetText().Trim());
        if (methodFormals is not null)
        {
            foreach (var f in methodFormals)
                if (f.Item.CobolName is { } name && Key(name) == key) return f.Probe;
            return null;
        }
        foreach (var f in _data.Linkage.Formals)
            if (f.Item.CobolName is { } name && Key(name) == key) return f.Probe;
        return null;
    }
}
