// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding;

public sealed partial class SymbolTable
{
    /// <summary>⛔ THE CONSTANT-NAMES OF ONE SOURCE ELEMENT (kb/Work PB3164): the binder's own element (a program, a function, or
    /// a class's object, factory or interface definition: <see cref="_ownConstants"/>), or one METHOD
    /// (<see cref="_methodConstants"/>). A method definition is a source unit of its own, and so a source element (§2023:3.164,
    /// "source unit excluding any contained source units"), which "may use identical user-defined words to name items
    /// independent of the use of these user-defined words by other source elements" (§2023:8.4.6.1). A method's constant-name is
    /// a local name (§2023:8.4.6.2.2; §2023:13.18.27.3 SR4 bars GLOBAL in a method definition), invisible to its sibling methods
    /// and to its object, while the object's own constant-names are global names its methods see. §2023:11.7.4 GR5 makes the
    /// method's constant-name, and every other word the method's data division defines, hide a same-spelled word of the
    /// containing object or factory definition inside that method.</summary>
    private sealed class ConstantNames
    {
        /// <summary>The bound constants by name (§2023:13.10.4 GR1 substitution source): this element's OWN entries only. The
        /// GLOBAL constants of the containing programs are <see cref="_inheritedConstants"/>, read after these (kb/Work
        /// PB1009).</summary>
        internal Dictionary<CobolName, ConstantDef> Bound { get; } = [];

        /// <summary>The element's constant entries by name, in source order (a duplicated name, §2023:13.10.3 SR9, has
        /// several), declared before any of them binds (kb/Work PB1231).</summary>
        internal Dictionary<CobolName, List<DeclaredConstant>> Declared { get; } = [];

        /// <summary>The words a METHOD's data division defines as anything but a constant-name: its data-names,
        /// condition-names, index-names and type-names, but not the names inside a type declaration, which "may be
        /// referenced only as subordinate items of groups defined using the type-name" (§2023:13.18.58.4 GR1) and so hide
        /// nothing (kb/Work PB3192). §2023:11.7.4 GR5 makes each hide a word of the same spelling in the containing object or
        /// factory definition, from before the entry that defines it binds (<see cref="MethodDeclaresWord"/>). Empty for the
        /// binder's own element, whose data-names cannot repeat one of its own constant-names (§2023:8.3.2.2) and whose
        /// containers' constants it shadows through <see cref="DropShadowedConstants"/>.</summary>
        internal HashSet<CobolName> OtherWords { get; } = [];

        /// <summary>Whether the element has any constant, bound or still to bind.</summary>
        internal bool Any => Bound.Count > 0 || Declared.Count > 0;
    }

    /// <summary>One inherited GLOBAL constant: its folded definition and the containment distance of the container that declared
    /// it (1 = the direct container), the tier <see cref="DropShadowedConstants"/> weighs against a same-spelled name's (kb/Work
    /// PB1047).</summary>
    private readonly record struct InheritedConstant(ConstantDef Definition, int Depth);

    /// <summary>This binder's own source element's constants (<see cref="ConstantNames"/>).</summary>
    private readonly ConstantNames _ownConstants = new();

    /// <summary>Each METHOD's own constants (<see cref="ConstantNames"/>), keyed by the method's name scope. The methods of a
    /// class half are bound by that half's binder, their data and their statements alike, so one map holds every table a
    /// reference made in one of them reads.</summary>
    private readonly Dictionary<OoMethodDataScope, ConstantNames> _methodConstants = new(ReferenceEqualityComparer.Instance);

    /// <summary>The GLOBAL constant-names of every containing program, nearest container first (§2023:13.18.27.4 GR1: "A
    /// constant-name, data-name, file-name, report-name, or screen-name described using a GLOBAL clause is a global name"; GR2: a
    /// contained program "may reference that name without describing it again"; kb/Work PB1009). A constant occupies no
    /// storage, so it needs no bridge: the container's folded definition is read here, after the unit's own constants (a
    /// local constant entry of the spelling hides the inherited one, §2023:8.4.6), and it leaves this table when a name
    /// declared NEARER hides it (<see cref="DropShadowedConstants"/>; §2023:8.4.6.2.1 3) a)). Written before the unit's first
    /// data phase (<see cref="InheritConstants"/>).</summary>
    private readonly Dictionary<CobolName, InheritedConstant> _inheritedConstants = [];

    /// <summary>The constant table of the method whose name scope is <paramref name="method"/>, made the first time it is asked
    /// for (every method has one, empty until its data division declares an entry).</summary>
    private ConstantNames MethodConstantsOf(OoMethodDataScope method)
    {
        if (!_methodConstants.TryGetValue(method, out var names)) _methodConstants[method] = names = new ConstantNames();
        return names;
    }

    /// <summary>The table a constant entry of <paramref name="method"/> (null: the binder's own element) is declared in.</summary>
    private ConstantNames ConstantNamesOf(OoMethodDataScope? method) => method is null ? _ownConstants : MethodConstantsOf(method);

    /// <summary>The METHOD whose constants a reference made now reads first: the method whose data division is being bound,
    /// else the method whose statements are; null at program, object, factory and interface level.</summary>
    private ConstantNames? ReferencingMethodConstants =>
        _data.Oo.CurrentMethodScope is { } method ? MethodConstantsOf(method) : null;

    /// <summary>The table's half of "does the method whose name scope is <paramref name="method"/> declare
    /// <paramref name="word"/>" (§2023:11.7.4 GR5; kb/Work PB3164, PB3192): one of its type declarations or constant entries
    /// names it, or a data description entry of its data division does, declared before any entry binds. The model's half is
    /// <see cref="OoMethodDataScope.IndexesDeclare"/>; <see cref="DataBinder.MethodDeclares"/> asks both.</summary>
    internal bool MethodDeclaresWord(OoMethodDataScope method, string word) =>
        Key(word) is var key
        && ((_methodNames.TryGetValue(method, out var tables) && tables.Declares(key))
            || (_methodConstants.TryGetValue(method, out var names) && (names.Declared.ContainsKey(key) || names.OtherWords.Contains(key))));

    /// <summary>⛔ THE ONE ANSWER TO "WHICH ELEMENT'S CONSTANT DOES <paramref name="name"/> NAME HERE?" (kb/Work PB3164;
    /// §2023:11.7.4 GR5: "If a given user-defined word is defined in the data division of this method definition and in the
    /// data division of the containing object definition, the use of that word in this method refers to the declaration in
    /// this method"). Inside a method: the method's own table when it declares a constant entry of that name; null when its
    /// data division defines the word as something else (that item is the one the word names), or when its statements reach
    /// another name of the spelling (<c>OoDataBinder.ActiveMethodDeclares</c>: among them a method table's named CAPACITY
    /// register, which no data description entry names, §2023:13.18.38.3 SR30; kb/Work PB3154 review R1-1, R2-1); else the
    /// containing definition's. Elsewhere, the binder's own table.</summary>
    private ConstantNames? ConstantNamesFor(CobolName name)
    {
        if (ReferencingMethodConstants is { } method)
        {
            if (method.Declared.ContainsKey(name)) return method;
            if (_data.Oo.ActiveMethodDeclares(name.Word)) return null;
        }
        return _ownConstants;
    }

    /// <summary>The GLOBAL constant of a container that <paramref name="name"/> names in <paramref name="names"/>, or null: only
    /// the unit's own element reads the inherited table (a method's constants hide, never inherit, §2023:11.7.4 GR5), and an
    /// entry of this unit's own that is DECLARED under the name (bound, still to bind or refused) hides it (§2023:8.4.6).</summary>
    private ConstantDef? InheritedConstantOf(ConstantNames names, CobolName name) =>
        ReferenceEquals(names, _ownConstants) && !names.Declared.ContainsKey(name)
        && _inheritedConstants.TryGetValue(name, out var inherited) ? inherited.Definition : null;

    /// <summary>The constant bound under <paramref name="name"/> in <paramref name="names"/>: the element's own, else the GLOBAL
    /// one a container offered.</summary>
    private ConstantDef? BoundConstantOf(ConstantNames names, CobolName name) =>
        names.Bound.TryGetValue(name, out var def) ? def : InheritedConstantOf(names, name);

    /// <summary>⛔ THE ONE CONSTANT RESOLUTION (§2023:13.10.4 GR1/GR3 substitution; kb/Work PB1231), <see cref="Identify"/>'s
    /// constant arm: the defined constant named <paramref name="name"/>, or null. A constant entry of this unit that is DECLARED
    /// but not yet bound (it stands later in the source than the reference, or in a section the binder walks later) is bound
    /// HERE, on demand, through <see cref="ConstantEntryBinder.BindDeclaredConstant"/>; so a reference may precede the entry it
    /// names (§2023:13.10.3 SR4 and SR5 forbid only a CIRCULAR dependence, which that bind reports).</summary>
    private ConstantDef? ConstantNamed(CobolName name)
    {
        if (ConstantNamesFor(name) is not { } names) return null;
        if (names.Declared.TryGetValue(name, out var declared))
            foreach (var d in declared)
            {
                if (names.Bound.ContainsKey(name)) break;
                _data.Constants.BindDeclaredConstant(d, demanded: true);
            }
        return BoundConstantOf(names, name);
    }

    /// <summary>Does <paramref name="name"/> name a constant entry of the element it is read in: bound, still to bind, or
    /// refused?</summary>
    internal bool IsDeclaredConstant(string name) => IsDeclaredConstant(Key(name));

    private bool IsDeclaredConstant(CobolName key) => ConstantNamesFor(key)?.Declared.ContainsKey(key) == true;

    /// <summary>Is <paramref name="name"/> a constant-name here at all: a constant entry of the element it is read in
    /// (<see cref="IsDeclaredConstant"/>) or a GLOBAL one inherited from a container (§2023:13.18.27.4 GR1; bound at the
    /// inheritance)? A PURE question: unlike the constant arm of <see cref="Identify"/> it binds nothing. The reporter of an
    /// unidentified reference asks it so a constant-name is never told it "is not defined".</summary>
    internal bool IsConstantName(string name) =>
        Key(name) is var key && ConstantNamesFor(key) is { } names
        && (names.Declared.ContainsKey(key) || BoundConstantOf(names, key) is not null);

    /// <summary>Is <paramref name="name"/> a constant-name whose every entry FINISHED with no value (refused, and reported at the
    /// entry: a cycle, a bad operand)? A PURE question, like <see cref="IsConstantName"/>: a reference to it is the consequence of
    /// that one fault, so its reader reports nothing more (kb/Work PB1906).</summary>
    internal bool IsRefusedConstantName(string name) =>
        Key(name) is var key && ConstantNamesFor(key) is { } names && !names.Bound.ContainsKey(key)
        && names.Declared.TryGetValue(key, out var declared)
        && declared.TrueForAll(static d => d.State == ConstantBindState.Finished);

    /// <summary>Does the word <paramref name="written"/> name a constant entry, here, that yielded no value? Its own entry was reported
    /// (a cycle, a bad operand), so a reader recovers without a second "not a constant-name" error. It binds the entry on demand, as
    /// a reference would, so it takes the written word like <see cref="Identify(Antlr4.Runtime.ParserRuleContext, NameClass, bool, NameReach, ProcedureSite?, ReportSite)"/>
    /// (kb/Work PB5932's dated file <c>2026-11-19-review-leads-slices-2-and-3</c>): only a single word is a name.</summary>
    internal bool IsFailedConstant(Antlr4.Runtime.ParserRuleContext written) => WordOf(written) is { } key && IsFailedConstant(key);

    /// <summary><see cref="IsFailedConstant(Antlr4.Runtime.ParserRuleContext)"/> for a word inside a token (a PICTURE repetition's
    /// constant-name, §2023:13.10.3 SR2).</summary>
    internal bool IsFailedConstant(WrittenWord written) => IsFailedConstant(Key(written.Text));

    private bool IsFailedConstant(CobolName key) => IsDeclaredConstant(key) && ConstantNamed(key) is null;

    /// <summary>Does the word <paramref name="written"/> name a constant here: one of this unit's constant entries (bound, bindable or
    /// refused) or an inherited GLOBAL one? The question a position whose word alternative has a meaning of its own (an ALPHABET
    /// code-name, an external-locale-name, a literal phrase's word) asks before taking that meaning. It binds the entry on demand,
    /// so it takes the written word as a reference does.</summary>
    internal bool NamesConstant(Antlr4.Runtime.ParserRuleContext written) =>
        WordOf(written) is { } key && (IsDeclaredConstant(key) || ConstantNamed(key) is not null);

    /// <summary>The containment distance of the container whose GLOBAL constant <paramref name="name"/> names here (1 = the
    /// direct container), 0 when the name is not an inherited constant: this element's own declaration hides it (ISO
    /// §2023:8.4.6.2.1 3); kb/Work PB4432: the depth <see cref="Declarations.NonConditionDeclarationsOf"/> ranks it at).</summary>
    internal int InheritedConstantDepthOf(string name) =>
        Key(name) is var key && !_ownConstants.Declared.ContainsKey(key) && _inheritedConstants.TryGetValue(key, out var inherited)
            ? inherited.Depth : 0;

    /// <summary>Whether a constant, bound or still to bind, is visible here (this unit's own, an inherited one, or the method's
    /// being bound): the guard the PICTURE expansion takes so a constant-free program's PICTURE pipeline is untouched.</summary>
    internal bool UnitHasConstants =>
        _ownConstants.Any || _inheritedConstants.Count > 0 || ReferencingMethodConstants?.Any == true;

    /// <summary>Is a constant entry of the spelling of <paramref name="written"/> being evaluated now, so that a reference to it
    /// closes a circular dependence? The entry's binder reports the cycle once (<see cref="ConstantEntryBinder.ReportConstantCycle"/>),
    /// and the caller recovers silently. Only one word is a name (<see cref="WordOf"/>).</summary>
    internal bool ClosesConstantCycle(Antlr4.Runtime.ParserRuleContext written)
    {
        if (WordOf(written) is not { } key) return false;
        if (ConstantNamesFor(key)?.Declared.TryGetValue(key, out var declared) != true
            || declared!.FirstOrDefault(static d => d.State == ConstantBindState.Evaluating) is not { } reentered)
            return false;
        _data.Constants.ReportConstantCycle(reentered);
        return true;
    }

    /// <summary>Declares a NAMED constant entry: it joins the table of its own source element
    /// (<see cref="DeclaredConstant.Method"/>) under its constant-name. A local declaration SHADOWS a container's GLOBAL constant of the same name from here on
    /// (§2023:8.4.6; kb/Work PB1009), whether or not its own bind succeeds.</summary>
    internal void Declare(DeclaredConstant constant)
    {
        var names = ConstantNamesOf(constant.Method);
        var key = Key(constant.Name);
        if (!names.Declared.TryGetValue(key, out var list)) names.Declared[key] = list = [];
        list.Add(constant);
    }

    /// <summary>Declares a word the data division of <paramref name="method"/> defines as something other than a constant-name
    /// (§2023:11.7.4 GR5; kb/Work PB3164): it hides the containing definition's word of the spelling inside the method.</summary>
    internal void DeclareMethodWord(OoMethodDataScope method, string word) => MethodConstantsOf(method).OtherWords.Add(Key(word));

    /// <summary>A bound constant entry's value joins its element's table under its constant-name, unless the element has
    /// already bound an entry of that name: §2023:13.10.3 SR9's duplicate, which this answers (the earlier definition) so the
    /// caller can compare the two specifications. Read within the entry's own element (kb/Work PB1009, PB3164).</summary>
    internal ConstantDef? BindConstant(DeclaredConstant constant, ConstantDef def)
    {
        var names = ConstantNamesOf(constant.Method);
        var key = Key(constant.Name);
        if (names.Bound.TryGetValue(key, out var prior)) return prior;
        names.Bound[key] = def;
        return null;
    }

    /// <summary>ISO §2023:13.18.27.4 GR1–GR2 for constant-names (kb/Work PB1009): the constants a program contained in
    /// <paramref name="container"/> inherits from it, <paramref name="depth"/> containment levels out, the nearer container's
    /// kept. They are the container's own bound GLOBAL constants and the GLOBAL constants it inherited itself that no
    /// declaration of its own hides; <see cref="InheritStep"/> offers each container, nearest first, so the first definition
    /// of a name wins ("directly or indirectly", with the nearest global declaration taking precedence). A local declaration of
    /// the same name shadows (§2023:8.4.6): a local constant entry hides the inherited one structurally
    /// (<see cref="InheritedConstantOf"/>), and a name declared NEARER removes it (<see cref="DropShadowedConstants"/>).</summary>
    internal void InheritConstants(SymbolTable container, int depth)
    {
        foreach (var (name, def) in container._ownConstants.Bound)
            if (def.IsGlobal)
                _inheritedConstants.TryAdd(name, new(def, depth));
        foreach (var (name, inherited) in container._inheritedConstants)
            if (inherited.Definition.IsGlobal && !container._ownConstants.Declared.ContainsKey(name))
                _inheritedConstants.TryAdd(name, new(inherited.Definition, depth));
    }

    /// <summary>Drops each inherited constant that a declaration in a source element NEARER than its container hides
    /// (<paramref name="declaredNearerThan"/>: the name and the container's distance; §2023:8.4.6.2.1 3) a); kb/Work PB1009,
    /// PB1047), so it leaves the table before any procedure reference can substitute it. <see cref="InheritStep"/> only.</summary>
    internal void DropShadowedConstants(Func<string, int, bool> declaredNearerThan)
    {
        foreach (var (name, constant) in _inheritedConstants.ToList())
            if (declaredNearerThan(name.Word, constant.Depth))
                _inheritedConstants.Remove(name);
    }
}
