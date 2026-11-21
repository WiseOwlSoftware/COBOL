// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Symbols;

namespace WiseOwl.Cobol.Binding;

/// <summary>A SPECIAL-NAMES switch-status condition-name's symbol (ISO §2023:12.3.7.2 <c>ON STATUS IS condition-name-1</c> |
/// <c>OFF STATUS IS condition-name-2</c>): the implementor switch-name it tests and whether it names the ON status.</summary>
internal sealed record SwitchStatusName(string ImplementorName, bool IsOn);

/// <summary>A SPECIAL-NAMES symbolic-character's symbol (ISO §2023:12.3.7.4 GR11 a: "Symbolic-character-1 defines a figurative
/// constant"): its one-character value and whether it is of class national.</summary>
internal sealed record SymbolicCharacterName(string Value, bool National);

public sealed partial class SymbolTable
{
    /// <summary>
    /// ⛔ ONE TABLE OF NAMES A CONFIGURATION SECTION DECLARES, WHOSE EVERY REFERENCE IS A §2023:8.3.2.2 USE (kb/Work PB3153;
    /// docs/rearchitecture/DESIGN-listing.md section 6.7.1, "Step 6b, slice 4"). The types of user-defined word ISO §2023:8.4.6.1
    /// lists as "<i>declared within a configuration section</i>" (alphabet-name, class-name, a SPECIAL-NAMES condition-name,
    /// dynamic-length-structure-name, locale-name, mnemonic-name, ordering-name, symbolic-character) "<i>may be referenced only by
    /// statements and entries either in that source element that contains a configuration section or in any source element
    /// contained within that source element</i>", so a contained program, and a class's OBJECT or FACTORY half, resolves names
    /// no declaration of its own announced (§2023:12.3.4 GR1 hands them down; <see cref="InheritConfigurationNames"/>).
    /// §2023:8.3.2.2 says "<i>Within a source element, a given user-defined word may be used as only one type of user-defined
    /// word</i>", so a reference <see cref="Identify(Antlr4.Runtime.ParserRuleContext, NameClass, bool, NameReach, ProcedureSite?)"/>
    /// resolves with <c>report: true</c> is recorded in the census of the element it is written in
    /// (<see cref="DataBinder.RecordConfigurationNameUse"/>), and a probe (<c>report: false</c>) records nothing.
    /// </summary>
    private sealed class ConfigurationNames<T>(UserWordKind kind) where T : notnull
    {
        private readonly Dictionary<CobolName, NameDeclaration<T>> _entries = [];

        /// <summary>The containment distance of each name <see cref="InheritFrom"/> took from a containing element, still
        /// unredeclared here: 1 for the direct container's own declaration, one more per level it came down. A name absent here
        /// but in <see cref="_entries"/> is this source element's OWN declaration (§2023:8.4.6.1).</summary>
        private readonly Dictionary<CobolName, int> _inherited = [];

        /// <summary>The type of user-defined word a reference to an entry uses the word as (§2023:8.3.2.2).</summary>
        public UserWordKind Kind => kind;

        public int Count => _entries.Count;

        /// <summary>The declared names as written, in declaration order (a diagnostic's "declared:" list).</summary>
        public IEnumerable<string> Names => _entries.Keys.Select(static k => k.Word);

        /// <summary>The inherited names with their containment distances: the tier a same-spelled name declared in a NEARER
        /// source element hides them at (§2023:8.4.6.2.1 3)).</summary>
        public IEnumerable<KeyValuePair<string, int>> InheritedDepths =>
            _inherited.Select(static p => KeyValuePair.Create(p.Key.Word, p.Value));

        /// <summary>Declare a name; false (the first declaration kept) when it is already declared.</summary>
        public bool TryAdd(CobolName name, NameDeclaration<T> value) => _entries.TryAdd(name, value);

        /// <summary>Declare a name, replacing an INHERITED entry of the same name.</summary>
        public void Set(CobolName name, NameDeclaration<T> value)
        {
            _entries[name] = value;
            _inherited.Remove(name);
        }

        public bool Declares(CobolName name) => _entries.ContainsKey(name);

        public T? Of(CobolName name) => _entries.TryGetValue(name, out var declared) ? declared.Value : default;

        /// <summary>The declaration of <paramref name="name"/> (its entry's symbol), or null.</summary>
        public NameDeclaration<T>? DeclarationOf(CobolName name) => _entries.GetValueOrDefault(name);

        /// <summary>-1 when the name is not declared, 0 when THIS source element declares it, n when only the n-th containing
        /// element did (§2023:8.4.6.2.1 3)).</summary>
        public int ContainmentDepth(CobolName name) => !_entries.ContainsKey(name) ? -1 : _inherited.GetValueOrDefault(name);

        /// <summary>Take every entry of a CONTAINING element's table this one does not declare (§2023:12.3.4 GR1), each one level
        /// farther out than the container had it.</summary>
        public void InheritFrom(ConfigurationNames<T> container)
        {
            foreach (var (k, v) in container._entries)
                if (_entries.TryAdd(k, v))
                    _inherited[k] = container._inherited.GetValueOrDefault(k) + 1;
        }
    }

    /// <summary>Switch-status condition-names (§2023:12.3.7.2; §2023:8.4.4.2 Format 1 SR1).</summary>
    private readonly ConfigurationNames<SwitchStatusName> _switchConditions = new(UserWordKind.ConditionName);

    /// <summary>User-defined CLASS names and their expanded member sets (§2023:12.3.7 class-name clause).</summary>
    private readonly ConfigurationNames<UserClassDef> _userClasses = new(UserWordKind.ClassName);

    /// <summary>ALPHANUMERIC alphabet-names (§2023:12.3.7.4 GR7). An <c>ALPHABET … FOR NATIONAL</c> clause declares in
    /// <see cref="_nationalAlphabets"/> instead: the two classes are disjoint reference domains (§2023:12.3.6.3 SR1, SR2).</summary>
    private readonly ConfigurationNames<AlphabetDef> _alphabets = new(UserWordKind.AlphabetName);

    /// <summary>NATIONAL alphabet-names (§2023:12.3.7.4 GR7, Table 6).</summary>
    private readonly ConfigurationNames<NationalAlphabetDef> _nationalAlphabets = new(UserWordKind.AlphabetName);

    /// <summary>SYMBOLIC CHARACTERS figurative constants (§2023:12.3.7.4 GR11).</summary>
    private readonly ConfigurationNames<SymbolicCharacterName> _symbolicCharacters = new(UserWordKind.SymbolicCharacter);

    /// <summary>ORDER TABLE ordering-names and the decoded literal-9 each identifies (§2023:12.3.7.4 GR17).</summary>
    private readonly ConfigurationNames<string> _orderTables = new(UserWordKind.OrderingName);

    /// <summary>DYNAMIC LENGTH STRUCTURE declarations (§2023:12.3.7.2; kb/Work PB829).</summary>
    private readonly ConfigurationNames<DynamicLengthStructure> _dynamicLengthStructures = new(UserWordKind.DynamicLengthStructureName);

    /// <summary>LOCALE locale-names (§2023:12.3.7.2; DESIGN-locale-facility seam S1).</summary>
    private readonly ConfigurationNames<LocaleSymbol> _locales = new(UserWordKind.LocaleName);

    /// <summary>The element's own SPECIAL-NAMES mnemonic-names (§2023:12.3.7.2: <c>switch-name-1 IS mnemonic-name-1</c> |
    /// <c>feature-name-1 IS mnemonic-name-2</c> | <c>device-name-1 IS mnemonic-name-3</c>) and the implementor-name ROW each
    /// stands for, so every consumer asks the row's KIND, never the entry's spelling (kb/Work PB862). The first declaration of a
    /// spelling is kept; a container's entries are read after these (<see cref="_configurationContainer"/>).</summary>
    private readonly Dictionary<CobolName, NameDeclaration<ImplementorName>> _mnemonics = [];

    /// <summary>The containing element's table whose mnemonic-names this element reads after its own (§2023:12.3.7.4 GR1, the
    /// nearest declaration winning); null for an outermost element.</summary>
    private SymbolTable? _configurationContainer;

    /// <summary>Is <paramref name="class"/> one of the nine classes of name a configuration section declares (the switch-status
    /// condition-names, class-names, both alphabet classes, symbolic-characters, ordering-names, dynamic-length structures, locales
    /// and mnemonic-names), each held in a table of this file rather than in the data-name tables?</summary>
    private static bool IsConfigurationClass(NameClass @class) => @class is NameClass.SwitchCondition or NameClass.ClassName
        or NameClass.Alphabet or NameClass.NationalAlphabet or NameClass.SymbolicCharacter or NameClass.OrderingName
        or NameClass.DynamicLengthStructure or NameClass.Locale or NameClass.Mnemonic;

    /// <summary>The symbol of a configuration-section class one word names here and the entry its declaration made, or null; and the
    /// type of user-defined word a reference uses it as.</summary>
    private ((object? Symbol, SymbolId Id) Answer, UserWordKind Kind) ConfigurationNamed(CobolName key, NameClass wanted) => wanted switch
    {
        NameClass.SwitchCondition => (Answer(_switchConditions.DeclarationOf(key)), _switchConditions.Kind),
        NameClass.ClassName => (Answer(_userClasses.DeclarationOf(key)), _userClasses.Kind),
        NameClass.Alphabet => (Answer(_alphabets.DeclarationOf(key)), _alphabets.Kind),
        NameClass.NationalAlphabet => (Answer(_nationalAlphabets.DeclarationOf(key)), _nationalAlphabets.Kind),
        NameClass.SymbolicCharacter => (Answer(_symbolicCharacters.DeclarationOf(key)), _symbolicCharacters.Kind),
        NameClass.OrderingName => (Answer(_orderTables.DeclarationOf(key)), _orderTables.Kind),
        NameClass.DynamicLengthStructure => (Answer(_dynamicLengthStructures.DeclarationOf(key)), _dynamicLengthStructures.Kind),
        NameClass.Locale => (Answer(_locales.DeclarationOf(key)), _locales.Kind),
        NameClass.Mnemonic => (Answer(MnemonicNamed(key)), UserWordKind.MnemonicName),
        _ => throw new ArgumentOutOfRangeException(nameof(wanted), wanted, "not a configuration-section name class"),
    };

    /// <summary>A configuration-section declaration's value, with its entry.</summary>
    private (object? Symbol, SymbolId Id) Answer<T>(NameDeclaration<T>? declared) where T : notnull =>
        declared is null ? default : (declared.Value, _entries.IdOf(declared));

    /// <summary>The declaration of the row a mnemonic-name names: this element's own entry, else the nearest container's.</summary>
    private NameDeclaration<ImplementorName>? MnemonicNamed(CobolName key) =>
        _mnemonics.GetValueOrDefault(key) ?? _configurationContainer?.MnemonicNamed(key);

    // ---- declarations (the SPECIAL-NAMES binder) --------------------------------------------------------------------------
    // Each takes the DECLARING WORD's token (the written word travels, never its text, slice 4): the entry stands at it.

    /// <summary>Enter a configuration-section name declared by <paramref name="word"/>: the declaration object its table keeps, whose
    /// identity is the entry's (a value such as <see cref="AlphabetDef.Native"/> or an implementor-name row is shared, so it cannot
    /// be the entry's symbol). A refused second declaration is a declaration too, and is entered.</summary>
    private NameDeclaration<T> EnterConfiguration<T>(NameClass @class, Antlr4.Runtime.IToken word, T value) where T : notnull
    {
        var declared = new NameDeclaration<T>(value);
        Enter(@class, word.Text, PositionOf(word), declared);
        return declared;
    }

    /// <summary>Declares a switch-status condition-name; the first declaration of a spelling stands.</summary>
    internal void DeclareSwitchCondition(Antlr4.Runtime.IToken word, string implementorName, bool isOn) =>
        _switchConditions.TryAdd(Key(word.Text), EnterConfiguration(NameClass.SwitchCondition, word, new SwitchStatusName(implementorName, isOn)));

    /// <summary>Declares a class-name; the first declaration of a spelling stands.</summary>
    internal void Declare(Antlr4.Runtime.IToken classWord, UserClassDef members) =>
        _userClasses.TryAdd(Key(classWord.Text), EnterConfiguration(NameClass.ClassName, classWord, members));

    /// <summary>Declares an alphanumeric alphabet-name; the first declaration of a spelling stands.</summary>
    internal void Declare(Antlr4.Runtime.IToken alphabetWord, AlphabetDef alphabet) =>
        _alphabets.TryAdd(Key(alphabetWord.Text), EnterConfiguration(NameClass.Alphabet, alphabetWord, alphabet));

    /// <summary>Declares a national alphabet-name; the first declaration of a spelling stands.</summary>
    internal void Declare(Antlr4.Runtime.IToken alphabetWord, NationalAlphabetDef alphabet) =>
        _nationalAlphabets.TryAdd(Key(alphabetWord.Text), EnterConfiguration(NameClass.NationalAlphabet, alphabetWord, alphabet));

    /// <summary>Declares a symbolic-character; false (the first declaration kept) when the spelling is already declared.</summary>
    internal bool DeclareSymbolicCharacter(Antlr4.Runtime.IToken word, string value, bool national) =>
        _symbolicCharacters.TryAdd(Key(word.Text), EnterConfiguration(NameClass.SymbolicCharacter, word, new SymbolicCharacterName(value, national)));

    /// <summary>Declares an ordering-name, replacing an inherited one of the spelling.</summary>
    internal void DeclareOrderingName(Antlr4.Runtime.IToken word, string literal9) =>
        _orderTables.Set(Key(word.Text), EnterConfiguration(NameClass.OrderingName, word, literal9));

    /// <summary>Declares a dynamic-length-structure-name, replacing an INHERITED structure of the spelling (§2023:8.4.6.1).</summary>
    internal void Declare(Antlr4.Runtime.IToken word, DynamicLengthStructure structure) =>
        _dynamicLengthStructures.Set(Key(structure.Name), EnterConfiguration(NameClass.DynamicLengthStructure, word, structure));

    /// <summary>Declares a locale-name; the first declaration of a spelling stands.</summary>
    internal void Declare(Antlr4.Runtime.IToken word, LocaleSymbol locale) =>
        _locales.TryAdd(Key(locale.Name), EnterConfiguration(NameClass.Locale, word, locale));

    /// <summary>Declares a mnemonic-name and the implementor-name row it stands for; the first declaration of a spelling stands.</summary>
    internal void DeclareMnemonic(Antlr4.Runtime.IToken word, ImplementorName row) =>
        _mnemonics.TryAdd(Key(word.Text), EnterConfiguration(NameClass.Mnemonic, word, row));

    /// <summary>Takes the containing element's configuration-section names (§2023:12.3.4 GR1: "The entries explicitly or
    /// implicitly specified in the configuration section of a source unit that contains other source units apply to each directly
    /// or indirectly contained source unit"): each table copies the container's entries this element does not declare, one level
    /// farther out, and the mnemonic-names are read from the container after this element's own. <see cref="ConfigurationStep"/>
    /// only.</summary>
    internal void InheritConfigurationNames(SymbolTable container)
    {
        _switchConditions.InheritFrom(container._switchConditions);
        _userClasses.InheritFrom(container._userClasses);
        _alphabets.InheritFrom(container._alphabets);
        _nationalAlphabets.InheritFrom(container._nationalAlphabets);
        _symbolicCharacters.InheritFrom(container._symbolicCharacters);
        _orderTables.InheritFrom(container._orderTables);
        _dynamicLengthStructures.InheritFrom(container._dynamicLengthStructures);
        _locales.InheritFrom(container._locales);   // §2023:12.3.7.4 GR1 — locale-names reach contained units
        InheritProgramSpecifiers(container);         // §2023:12.3.8.4 GR10 — the REPOSITORY's program-specifiers too
        InheritPropertySpecifiers(container);        // §2023:12.3.4 GR1 — and its PROPERTY specifiers
        InheritFunctionSpecifiers(container);        // §2023:12.3.4 GR1 — and its user-defined-function specifiers
        _configurationContainer = container;
    }

    // ---- questions that answer no symbol -----------------------------------------------------------------------------------

    /// <summary>Is <paramref name="name"/> declared as a name of class <paramref name="class"/> (this element's own or inherited)?
    /// Records nothing: the question a disambiguation or a declaration asks, never a reference's resolution.</summary>
    internal bool DeclaresConfigurationName(NameClass @class, string name) => @class switch
    {
        NameClass.Mnemonic => MnemonicNamed(Key(name)) is not null,
        _ => ConfigurationNamed(Key(name), @class).Answer.Symbol is not null,
    };

    /// <summary>Is <paramref name="name"/> an alphabet-name of either class?</summary>
    internal bool IsAlphabetName(string name) =>
        _alphabets.Declares(Key(name)) || _nationalAlphabets.Declares(Key(name));

    /// <summary>Is <paramref name="name"/> an alphabet "associated with a locale" (§2023:8.8.4.4.3 SR2), of either class? Such an
    /// alphabet defines a collating sequence only (§2023:12.3.7.4 GR7, Table 6).</summary>
    internal bool IsLocaleAlphabet(string name) =>
        _alphabets.Of(Key(name)) is { Locale: not null } || _nationalAlphabets.Of(Key(name)) is { Locale: not null };

    /// <summary>The containment tier of the switch-status condition-name <paramref name="name"/> (§2023:8.4.6.2.1 3)): -1 when it
    /// is not declared, 0 when this element declares it, n when only the n-th containing element did.</summary>
    internal int SwitchConditionDepth(string name) => _switchConditions.ContainmentDepth(Key(name));

    /// <summary>How many ordering-names are declared here, inherited ones included, and their spellings in declaration order.</summary>
    internal (int Count, IEnumerable<string> Names) OrderingNames => (_orderTables.Count, _orderTables.Names);

    /// <summary>How many locale-names are declared here, inherited ones included, and their spellings in declaration order.</summary>
    internal (int Count, IEnumerable<string> Names) LocaleNames => (_locales.Count, _locales.Names);

    // ---- symbolic-characters: declared ahead, and hidden ---------------------------------------------------------------

    /// <summary>The symbolic-character names of the SPECIAL-NAMES paragraph being bound, read syntactically before any clause
    /// binds: the clauses are order-free, so an ALPHABET clause may precede the SYMBOLIC CHARACTERS clause that declares a name it
    /// writes, and §2023:12.3.7.3 SR11 must still recognize it (kb/Work PB226).</summary>
    private HashSet<CobolName> _symbolicCharactersAhead = [];

    /// <summary>The inherited symbolic-characters a nearer declaration hides (§2023:8.4.6.1; kb/Work PB3154 review R1-1). Such a one
    /// is HIDDEN, never removed from the table: a contained element inherits the whole table from this one, and this element's
    /// local data-name hides nothing from it.</summary>
    private readonly HashSet<CobolName> _hiddenSymbolics = [];

    /// <summary>Announces the symbolic-character names the SPECIAL-NAMES paragraph being bound declares (<see cref="_symbolicCharactersAhead"/>).</summary>
    internal void DeclareSymbolicCharactersAhead(IEnumerable<string> names) => _symbolicCharactersAhead = [.. names.Select(Key)];

    /// <summary>Is <paramref name="word"/> a symbolic-character of this SPECIAL-NAMES paragraph (declared ahead) or of the table?</summary>
    internal bool IsSymbolicCharacterName(string word) =>
        _symbolicCharactersAhead.Contains(Key(word)) || _symbolicCharacters.Declares(Key(word));

    /// <summary>The inherited symbolic-characters with their containment distances, the tier a nearer declaration hides them at.</summary>
    internal IEnumerable<KeyValuePair<string, int>> InheritedSymbolicDepths => _symbolicCharacters.InheritedDepths;

    /// <summary>Does a nearer declaration hide the inherited symbolic-character <paramref name="word"/>?</summary>
    internal bool HidesSymbolic(string word) => _hiddenSymbolics.Contains(Key(word));

    /// <summary>Hides each inherited symbolic-character that a declaration in a source element NEARER than its container hides
    /// (<paramref name="declaredNearerThan"/>: the name and the container's distance; §2023:8.4.6.2.1 3) a)). <see cref="InheritStep"/>
    /// only.</summary>
    internal void HideShadowedSymbolics(Func<string, int, bool> declaredNearerThan)
    {
        foreach (var (name, depth) in _symbolicCharacters.InheritedDepths)
            if (declaredNearerThan(name, depth))
                _hiddenSymbolics.Add(Key(name));
    }
}
