// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using WiseOwl.Cobol.Binding.Model;
using WiseOwl.Cobol.Compiler.Oo;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Symbols;

namespace WiseOwl.Cobol.Binding;

using Core = CobolParserCore;

public sealed partial class SymbolTable
{
    /// <summary>
    /// The OBJECT-CLASS-NAMES and INTERFACE-NAMES, THE COMPILATION GROUP'S TIER of the table (docs/rearchitecture/DESIGN-listing.md
    /// section 6.7.1, "Step 6b, the OO names"; kb/Work PB5932). A class or interface definition is a source unit of the group, built
    /// before any source element binds so that a reference resolves a class defined later in the file (OO deep-dive D1), and one
    /// element's REPOSITORY paragraph names classes of other units (§2023:8.4.6.4), so the names are the group's, not one
    /// element's: one <see cref="ClassNames"/> per <see cref="OoClassTable"/>, which builds the symbols and owns their order, the
    /// skeletons and the §2023:8.3.2.2 censuses. Every table here is keyed by <see cref="CobolName"/> and private:
    /// <list type="bullet">
    /// <item>a WRITTEN object-class-name or interface-name resolves through <see cref="Resolve"/> or its non-diagnosing half
    /// <see cref="Lookup"/>, in the referring source element's §2023:8.4.6.4 scope (<see cref="RepositoryScope"/>);</item>
    /// <item>a name a SYMBOL carries as text (an object-reference description's class, a RAISING element) is resolved here from the
    /// carrier (<see cref="ClassOf(ObjectRefDescriptor)"/> and its siblings), as slice 6 resolves a restricted program-pointer's
    /// prototype;</item>
    /// <item>an identity the external repository publishes (§2023:8.13; DESIGN-external-repository §8.4) is looked up or imported
    /// by that identity (<see cref="FindByExternalizedName"/>, <see cref="ImportClass"/>): the operating environment's name, no
    /// word of the program;</item>
    /// <item>the questions that answer no symbol (an interface's method surface by method-name, the RETURNING reference graph of
    /// §2023:9.3.8.2.3, a property's accessors) are the tier's, so no caller compares two of these names.</item>
    /// </list>
    /// </summary>
    internal sealed class ClassNames
    {
        private readonly OoClassTable _table;

        /// <summary>The classes of the group (a CLASS-ID's object-class-name), and each IMPORTED class under its table key
        /// (<see cref="OoClassTable.ImportedKey"/>). Classes and interfaces share ONE name namespace (§2023:8.3.2.2).</summary>
        private readonly Dictionary<CobolName, OoClassSymbol> _classes = [];

        /// <summary>The interfaces of the group, and each imported interface under its table key.</summary>
        private readonly Dictionary<CobolName, OoInterfaceSymbol> _interfaces = [];

        /// <summary>The imported classes by their externalized identity (DESIGN-external-repository §10.1).</summary>
        private readonly Dictionary<CobolName, OoClassSymbol> _importedClasses = [];

        /// <summary>The imported interfaces by their externalized identity (DESIGN-external-repository §10.6).</summary>
        private readonly Dictionary<CobolName, OoInterfaceSymbol> _importedInterfaces = [];

        /// <summary>The identities already asked of the importer (a refusal is remembered).</summary>
        private readonly HashSet<CobolName> _classImportsAsked = [];
        private readonly HashSet<CobolName> _interfaceImportsAsked = [];

        /// <summary>Per interface, the prototype it PRESENTS for a method-name it inherits from several interfaces
        /// (§2023:11.6.3 SR5, <see cref="ChooseInheritedPrototype"/>), keyed by method-name as <see cref="AllPrototypes"/> is.</summary>
        private readonly Dictionary<OoInterfaceSymbol, Dictionary<CobolName, OoMethodSymbol>> _conformingChoice = [];

        internal ClassNames(OoClassTable table) => _table = table;

        /// <summary>The standard class BASE, entered the first time the group names it: the standard declares it, not the source, so
        /// its entry stands at no position, and a group that never names it has none (as an imported class is entered when it is
        /// imported).</summary>
        private OoClassSymbol StandardBase
        {
            get
            {
                if (!_table.Entries.IdOf(_table.StandardBase).IsSet)
                    _table.Entries.Enter(null, NameClass.ObjectClass, _table.StandardBase.Name, default, _table.StandardBase);
                return _table.StandardBase;
            }
        }

        // ---- declarations (OoClassTable.Build, the importer) ------------------------------------------------------------

        /// <summary>Whether a class of the group carries the name <paramref name="word"/> (a duplicate CLASS-ID or an interface
        /// spelled like one).</summary>
        internal bool NamesClass(string word) => _classes.ContainsKey(Key(word));

        /// <summary>Whether an interface of the group carries the name <paramref name="word"/>.</summary>
        internal bool NamesInterface(string word) => _interfaces.ContainsKey(Key(word));

        /// <summary>Declare a class of the group under its object-class-name, written at <paramref name="word"/> (its CLASS-ID's), where
        /// its entry stands; false (and no entry) when one is already declared.</summary>
        internal bool TryDeclareClass(OoClassSymbol symbol, IToken word)
        {
            if (!_classes.TryAdd(Key(symbol.Name), symbol)) return false;
            _table.Entries.Enter(null, NameClass.ObjectClass, symbol.Name, new(word.Line, word.Column), symbol);
            return true;
        }

        /// <summary>Declare an interface of the group under its interface-name, written at <paramref name="word"/> (its INTERFACE-ID's),
        /// where its entry stands (the caller has refused a duplicate).</summary>
        internal void DeclareInterface(OoInterfaceSymbol symbol, IToken word)
        {
            _interfaces.Add(Key(symbol.Name), symbol);
            _table.Entries.Enter(null, NameClass.Interface, symbol.Name, new(word.Line, word.Column), symbol);
        }

        /// <summary>Declare the parameter-names of a parameterized class or interface definition bound as itself (§2023:11.3.2 /
        /// §2023:11.6.2 <c>USING parameter-name-1</c>): each is an entry at its word, which a reference to the formal answers
        /// (<see cref="Lookup"/>'s third outcome).</summary>
        internal void DeclareParameterNames(IEnumerable<Core.OoParameterNameContext> formals)
        {
            foreach (var formal in formals)
                if (!_table.Entries.IdOf(formal).IsSet)
                    _table.Entries.Enter(null, NameClass.ParameterName, formal.GetText(), new(formal.Start.Line, formal.Start.Column), formal);
        }

        // ---- the external repository's identities ------------------------------------------------------------------------

        /// <summary>The class whose EXTERNALIZED name (its CLASS-ID's <c>AS literal-1</c>, or its name) is
        /// <paramref name="externalized"/>: what a REPOSITORY class-specifier's <c>AS literal-1</c> names (ISO §2023:12.3.8.4 GR2;
        /// kb/Work PB974). The standard class BASE is externalized as its own name, and answers only when no definition of the group
        /// claims it; then a class already imported under that identity.</summary>
        internal OoClassSymbol? FindByExternalizedName(string externalized)
        {
            var key = Key(externalized);
            return _table.Classes.FirstOrDefault(c => Key(c.ExternalizedName) == key)
                ?? (Key(_table.StandardBase.ExternalizedName) == key ? StandardBase : _importedClasses.GetValueOrDefault(key));
        }

        /// <summary>The interface twin of <see cref="FindByExternalizedName"/> (literal-2).</summary>
        internal OoInterfaceSymbol? FindInterfaceByExternalizedName(string externalized)
        {
            var key = Key(externalized);
            return _table.Interfaces.FirstOrDefault(i => Key(i.ExternalizedName) == key) ?? _importedInterfaces.GetValueOrDefault(key);
        }

        /// <summary>⛔ THE IMPORT (DESIGN-external-repository §8.4 step 3; DESIGN-external-repository §10.1): the class whose EXTERNALIZED name is
        /// <paramref name="identity"/> — the group's own definition, else the standard class BASE, else one already imported —
        /// and otherwise the one the external repository publishes, registered here by that identity so the whole group shares
        /// one symbol per identity. Null when nothing answers; the importer has reported a finding of its own when it refused
        /// one. Each identity is asked of the importer once (a refusal is remembered), and a table with no importer answers
        /// only the first three.</summary>
        internal OoClassSymbol? ImportClass(string identity)
        {
            if (FindByExternalizedName(identity) is { } known) return known;
            if (_table.Importer is not { } importer || !_classImportsAsked.Add(Key(identity))) return null;
            if (importer.Class(identity, _table) is not { } found) return null;
            // Registered BEFORE it is completed: a method of the class that takes the class itself, or a class that takes it,
            // names this identity, which must answer with the symbol under construction rather than ask again.
            var symbol = found.Symbol;
            // Imported from the external repository: declared by no source text of this group, so its entry stands at no position.
            _table.Entries.Enter(null, NameClass.ObjectClass, symbol.Name, default, symbol);
            _importedClasses[Key(symbol.ExternalizedName)] = symbol;
            _table.AddImported(symbol);
            _classes[Key(symbol.Name)] = symbol;
            found.Complete();
            return symbol;
        }

        /// <summary>The interface twin of <see cref="ImportClass"/> (ISO §2023:12.3.8.3 SR9; DESIGN-external-repository §10.6),
        /// registered here by identity and BEFORE it is completed (its INHERITS list and its prototypes name other interfaces, and
        /// a prototype may name its own). §16 defines no standard interface, so there is no BASE step.</summary>
        internal OoInterfaceSymbol? ImportInterface(string identity)
        {
            if (FindInterfaceByExternalizedName(identity) is { } known) return known;
            if (_table.Importer is not { } importer || !_interfaceImportsAsked.Add(Key(identity))) return null;
            if (importer.Interface(identity, _table) is not { } found) return null;
            var symbol = found.Symbol;
            _table.Entries.Enter(null, NameClass.Interface, symbol.Name, default, symbol);
            _importedInterfaces[Key(symbol.ExternalizedName)] = symbol;
            _table.AddImported(symbol);
            _interfaces[Key(symbol.Name)] = symbol;
            found.Complete();
            return symbol;
        }

        // ---- a written name ------------------------------------------------------------------------------------------------

        /// <summary>
        /// ⛔ THE ONE FUNNEL FOR RESOLVING A WRITTEN object-class-name OR interface-name (kb/Work PB365): <paramref name="written"/>,
        /// one word, in the scope of the source element <paramref name="site"/> lies in, reporting through
        /// <paramref name="code"/> when it is not. The rule is ISO §2023:8.4.6.4: "The object-class-name of an object class
        /// referenced within a source element shall be either the name of the containing object class definition or declared in
        /// the REPOSITORY paragraph of that or a containing source element", and the same sentence for interface-names. Every
        /// per-statement rule that says "shall be the name of a class specified in the REPOSITORY paragraph" (§2023:11.3.3 SR2,
        /// §2023:11.4.3 SR1, §2023:11.6.3 SR2, §2023:11.8.3 SR1, §2023:14.2.2 SR8 and SR9, §2023:14.9.49.3 SR16 and SR17) is an
        /// instance of it. The message distinguishes the two failures, because their fixes differ: a name DEFINED in the group but
        /// not declared in scope needs a REPOSITORY entry; a name defined nowhere needs a definition. The census of the element
        /// is the site's (<see cref="OoClassTable.RecordReferenceUse"/>), so <paramref name="site"/> may enclose the word (an
        /// INHERITS or IMPLEMENTS operand is asked at its class definition).
        /// </summary>
        /// <param name="where">The reference site, e.g. <c>"declarative section 'EO-SEC': USE AFTER EXCEPTION OBJECT"</c>.</param>
        /// <param name="code">The REFERENCING construct's own diagnostic code: each site keeps the code its goldens pin.</param>
        /// <param name="ruleCitation">The referencing construct's own syntax rule, e.g. <c>"ISO §2023:14.9.49.3 SR16/SR17"</c>.</param>
        internal OoNameResult Resolve(EditionContext edition, RuleContext? site, ParserRuleContext written, OoNameWant want,
            string where, string code, string ruleCitation)
        {
            var found = Lookup(site, written, want);
            if (found.Ok || found.IsFormal) return found;
            string name = written.GetText();

            // A PARAMETERIZED definition is a skeleton, not a class or interface, so it is never in the table and a reference to
            // it can never resolve (kb/Work PB759). Say which rule, rather than "not defined": the definition IS in the source.
            // ISO §2023:12.3.8.4 GR1 (class) and GR7 (interface) confine the name to the REPOSITORY paragraph, where its only use is
            // as an EXPANDS operand.
            if (_table.IsParameterized(name))
            {
                edition.Error(code, $"{where} '{name}': '{name}' is a parameterized class or interface (its "
                    + "CLASS-ID / INTERFACE-ID paragraph has a USING clause), which may be specified only in the "
                    + "REPOSITORY paragraph — reference an expansion of it, declared with the EXPANDS phrase "
                    + $"({ruleCitation}; ISO §2023:12.3.8.4 GR1/GR7)");
                return default;
            }

            var cls = want is OoNameWant.Class or OoNameWant.Either ? ClassKeyed(name) : null;
            bool definedInGroup = cls is not null
                || (want is OoNameWant.Interface or OoNameWant.Either && InterfaceKeyed(name) is not null);
            string kind = want switch
            {
                OoNameWant.Class => "a class",
                OoNameWant.Interface => "an interface",
                _ => "a class or interface",
            };
            string notDeclared = "it is not the name of the containing class or interface definition and is not "
                + "declared in the REPOSITORY paragraph of this source element or a containing one "
                + $"({ruleCitation}; ISO §2023:8.4.6.4)";
            edition.Error(code,
                cls is { Origin: OoSymbolOrigin.Standard }
                    // The standard class is reached exactly like a written one — through a REPOSITORY class-specifier
                    // (§2023:12.3.8.3 SR6 b): name the missing entry, which is the whole fix.
                    ? $"{where} '{name}': '{name}' is the standard class BASE (ISO §2023:16.1), but {notDeclared} — "
                      + $"add `CLASS {name}` to the REPOSITORY paragraph"
                : definedInGroup
                    ? $"{where} '{name}': {kind} of that name is defined in this compilation group, but {notDeclared}"
                : $"{where} '{name}': no {kind[(kind.IndexOf(' ') + 1)..]} of that name is defined in this compilation "
                  + $"group ({ruleCitation})");
            return default;
        }

        /// <summary>The NON-DIAGNOSING half of <see cref="Resolve"/>, for the sites that PARTITION on "is this word a class-name
        /// here?" — the PROCEDURE DIVISION / METHOD-ID RAISING word that may instead be an exception-name (§2023:14.2.2 SR7 vs SR8 and
        /// SR9), and INVOKE's receiver, which may instead be an identifier. The partition uses the SAME scoped set: a class the
        /// source element cannot reference is not a class-name in that position. A name of several words names nothing.
        /// <para>A name that resolves is a USE of the word as that type in the referring source element, which that element's
        /// §2023:8.3.2.2 census records (<see cref="OoClassTable.RecordReferenceUse"/>, kb/Work PB3132): the one thing this half
        /// reports, and only for a word the element has made two types. A parameter-name is recorded as the type its own
        /// definition's REPOSITORY specifier declares it (§2023:11.3.3 SR8 / §2023:11.6.3 SR4 require that specifier), so the
        /// definition's own references to its formal agree with its declaration of it. The census records the word as written,
        /// as the declaration funnel does, so a conflict is worded in the programmer's spelling.</para>
        /// <para>This is the compilation group tier's RECORDING POINT, the twin of <see cref="SymbolTable.Identify(ParserRuleContext,
        /// NameClass, bool, NameReach, ProcedureSite?, ReportSite)"/> (docs/rearchitecture/DESIGN-listing.md section 6.7.1): only the
        /// table makes an <see cref="OoNameResult"/>, and listing step 7b records the occurrence here, at
        /// <paramref name="written"/>, under the condition the census use is recorded.</para></summary>
        internal OoNameResult Lookup(RuleContext? site, ParserRuleContext written, OoNameWant want)
        {
            if (WordOf(written) is not { } key) return default;
            var found = Find(site, key, want);
            if (found.IsFormal || found.Ok)
            {
                _table.RecordReferenceUse(site, written.GetText(),
                    found.Class is not null || found.IsFormal && _table.RepositoryScopeFor(site).DeclaresClass(key.Word)
                        ? UserWordKind.ObjectClassName : UserWordKind.InterfaceName);
                // The answer carries the entry its declaration made (section 6.7.1, "Step 6b, the entry ids").
                found = found with
                {
                    Entry = _table.Entries.IdOf(found.Class ?? (object?)found.Interface
                        ?? _table.RepositoryScopeFor(site).FormalDeclaration(key.Word)
                        ?? throw new InvalidOperationException($"the formal '{key.Word}' has no declaration")),
                };
                if (!found.Entry.IsSet)
                    throw new InvalidOperationException($"SymbolTable.ClassNames.Lookup answered '{key.Word}', whose declaration entered no "
                        + "entry: every declaring path enters its name (DESIGN-listing.md section 6.7.1, \"Step 6b, the entry ids\")");
            }
            return found;
        }

        /// <summary>The resolution <see cref="Lookup"/> answers, before it records the use.</summary>
        private OoNameResult Find(RuleContext? site, CobolName key, OoNameWant want)
        {
            var scope = _table.RepositoryScopeFor(site);
            string name = key.Word;
            // A parameter-name of the enclosing parameterized definition, in a position of the kind its own REPOSITORY specifier
            // declares (§2023:11.3.3 SR8 / §2023:11.6.3 SR4 make that declaration mandatory). Asked FIRST: inside the definition
            // the word names the formal even when some class of the group is spelled the same, since every expansion replaces it
            // (§2023:12.3.8.4 GR5 / GR8; kb/Work PB2051).
            if (scope.IsFormal(name)
                && (want is OoNameWant.Class or OoNameWant.Either && scope.DeclaresClass(name)
                    || want is OoNameWant.Interface or OoNameWant.Either && scope.DeclaresInterface(name)))
                return new OoNameResult(null, null, IsFormal: true);
            // A specifier with `AS literal-n` names the definition whose EXTERNALIZED name is the literal (§2023:12.3.8.4 GR2,
            // kb/Work PB974); without it the declared word names the definition of that name. The group's definition wins a tie
            // with the repository (DESIGN-external-repository §8.4).
            if (want is OoNameWant.Class or OoNameWant.Either && scope.ClassTarget(name, out var clsExt)
                && (clsExt is null ? ClassKeyed(name) ?? ImportClass(name) : FindByExternalizedName(clsExt) ?? ImportClass(clsExt))
                    is { } cls)
                return new OoNameResult(cls, null);
            if (want is OoNameWant.Interface or OoNameWant.Either && scope.InterfaceTarget(name, out var ifcExt)
                && (ifcExt is null ? InterfaceKeyed(name) ?? ImportInterface(name)
                    : FindInterfaceByExternalizedName(ifcExt) ?? ImportInterface(ifcExt)) is { } ifc)
                return new OoNameResult(null, ifc);
            return default;
        }

        /// <summary>The class or interface a class-specifier or interface-specifier <paramref name="entry"/> stands for in its own
        /// scope — what its identity (<c>literal-1 ?? word</c>, ISO §2023:12.3.8.4 GR2) names in the group, the standard class or
        /// the external repository — without recording a use of the word (the NON-diagnosing, census-free half of
        /// <see cref="Lookup"/>). Neither when nothing answers or the word is a formal parameter-name of the definition it lies
        /// in.</summary>
        internal (OoClassSymbol? Class, OoInterfaceSymbol? Interface) ResolveSpecifier(Core.RepositoryEntryContext entry)
        {
            var scope = _table.RepositoryScopeFor(entry);
            if (entry.className() is { } cn)
            {
                string word = cn.GetText();
                if (scope.IsFormal(word) || !scope.ClassTarget(word, out var ext)) return default;
                return (ext is null ? ClassKeyed(word) ?? ImportClass(word) : FindByExternalizedName(ext) ?? ImportClass(ext), null);
            }
            if (entry.interfaceName() is { } inm)
            {
                string word = inm.GetText();
                if (scope.IsFormal(word) || !scope.InterfaceTarget(word, out var ext)) return default;
                return (null, ext is null ? InterfaceKeyed(word) ?? ImportInterface(word) : FindInterfaceByExternalizedName(ext) ?? ImportInterface(ext));
            }
            return default;
        }

        /// <summary>The class of the GROUP a specifier's written word <paramref name="word"/> names, whatever scope it is written
        /// in (the standard class BASE included): the question a REPOSITORY specifier asks of the external repository's
        /// information (§2023:12.3.8.3 SR6, SR16), never "may this source element reference it" (<see cref="Lookup"/>'s).</summary>
        internal OoClassSymbol? GroupClassNamed(ParserRuleContext word) => WordOf(word) is { } key ? ClassKeyed(key.Word) : null;

        /// <summary>The interface twin of <see cref="GroupClassNamed"/>.</summary>
        internal OoInterfaceSymbol? GroupInterfaceNamed(ParserRuleContext word) => WordOf(word) is { } key ? InterfaceKeyed(key.Word) : null;

        // ---- a name a symbol carries -------------------------------------------------------------------------------------

        /// <summary>The class an object-reference description names (its <see cref="ObjectRefDescriptor.Name"/>, the table key the
        /// description was resolved to where it was written), or null.</summary>
        internal OoClassSymbol? ClassOf(ObjectRefDescriptor described) => described.Name is { } n ? ClassKeyed(n) : null;

        /// <summary>The interface an object-reference description names, or null.</summary>
        internal OoInterfaceSymbol? InterfaceOf(ObjectRefDescriptor described) => described.Name is { } n ? InterfaceKeyed(n) : null;

        /// <summary>The class a RAISING element names (its table key, resolved where the phrase is written), or null.</summary>
        internal OoClassSymbol? ClassOf(RaisingTarget raised) => ClassKeyed(raised.Name);

        /// <summary>The interface a RAISING element names, or null.</summary>
        internal OoInterfaceSymbol? InterfaceOf(RaisingTarget raised) => InterfaceKeyed(raised.Name);

        /// <summary>The unexpanded parameterized class an ACTIVE-CLASS description written inside it names (its containing class,
        /// <see cref="ObjectRefDescriptor.ActiveClass"/>; kb/Work PB2937): it is in no name lookup (it creates no class), but its
        /// bodies bind as themselves and SELF there is described by it.</summary>
        internal OoClassSymbol? SkeletonOf(ObjectRefDescriptor described) =>
            described.IsActiveClass && described.Name is { } n
                ? _table.ParameterizedClasses.FirstOrDefault(c => Key(c.Name) == Key(n))
                : null;

        /// <summary>A class of the group by its table key, or the standard class BASE (ISO §2023:16.1), whose information the
        /// implementation's external repository always holds (§2023:12.3.8.3 SR6 b)). A class DEFINED in the group wins.</summary>
        private OoClassSymbol? ClassKeyed(string name)
        {
            var key = Key(name);
            return _classes.GetValueOrDefault(key) ?? (key == Key(OoStandardClasses.BaseName) ? StandardBase : null);
        }

        /// <summary>An interface of the group by its table key.</summary>
        private OoInterfaceSymbol? InterfaceKeyed(string name) => _interfaces.GetValueOrDefault(Key(name));

        // ---- the questions that answer no symbol ---------------------------------------------------------------------------

        /// <summary>Record <paramref name="proto"/> as the prototype <paramref name="iface"/> PRESENTS for a method-name it inherits
        /// from several interfaces: §2023:11.6.3 SR5 requires "this interface conforms to all inherited interfaces", so one
        /// inherited prototype shall conform to every other; <c>OoConformance.ValidateInterfaceInheritance</c> finds it.
        /// <see cref="OoInterfaceSymbol.PresentedInherited"/>, which the emitter reads, is the interface's chosen set.</summary>
        internal void ChooseInheritedPrototype(OoInterfaceSymbol iface, OoMethodSymbol proto)
        {
            if (!_conformingChoice.TryGetValue(iface, out var chosen)) _conformingChoice[iface] = chosen = [];
            chosen[Key(proto.Name)] = proto;
            iface.Present([.. chosen.Values]);
        }

        /// <summary>The interface's FULL method surface: own prototypes + the INHERITS closure (§2023:9.3.8.2.2), ONE prototype per
        /// method-name. An inherited name cannot also be an own prototype (§2023:11.7.3 SR4 b), refused at Build), and a name
        /// inherited from several interfaces presents the prototype every other one conforms to (<see cref="ChooseInheritedPrototype"/>;
        /// §2023:11.6.3 SR5) — before that pass, and for a name with one prototype, discovery order.</summary>
        internal IReadOnlyList<OoMethodSymbol> AllPrototypes(OoInterfaceSymbol iface)
        {
            var surface = new List<OoMethodSymbol>();
            var choice = _conformingChoice.GetValueOrDefault(iface);
            var seen = new HashSet<CobolName>();
            var stack = new Stack<OoInterfaceSymbol>();
            var visited = new HashSet<OoInterfaceSymbol>();
            stack.Push(iface);
            while (stack.Count > 0)
            {
                var i = stack.Pop();
                if (!visited.Add(i)) continue;
                foreach (var m in i.Prototypes)
                    if (Key(m.Name) is var name && seen.Add(name))
                        surface.Add(choice?.GetValueOrDefault(name) ?? m);
                foreach (var b in i.Inherits) stack.Push(b);
            }
            return surface;
        }

        /// <summary>The accessor kind (<c>'G'</c> get, <c>'S'</c> set) of the first of <paramref name="methods"/> that is a property
        /// accessor of the property <paramref name="property"/> (the name the declared classes know it by: a PROPERTY specifier's
        /// literal-4, or its property-name-1, §2023:12.3.8.3 SR16); <c>'\0'</c> when none is. The search over a roster is the table's,
        /// so no caller applies a name equality to each method (step 6b-r5 review round 1, lead R1-2).</summary>
        internal static char AccessorAmong(IEnumerable<OoMethodSymbol> methods, string property)
        {
            var key = Key(property);
            foreach (var m in methods)
                if (m.Accessor != '\0' && m.PropertyName is { } own && Key(own) == key) return m.Accessor;
            return '\0';
        }

        /// <summary>Does the RETURNING reference graph lead from the class or interface named <paramref name="start"/> to the one
        /// named <paramref name="target"/> (the start itself counting — a DIRECT reference)? ISO §2023:9.3.8.2.3's closing sentence
        /// (kb/Work PB1498, R63): a class or an interface is a node by its NAME, the key this tier resolves both by; the edges are
        /// every RETURNING item of a class's methods (instance and factory) or an interface's prototypes (inherited ones included),
        /// a class's INHERITS and both IMPLEMENTS lists, an interface's INHERITS. Breadth-first with a visited set.</summary>
        internal bool ReturningReaches(string start, string target)
        {
            var goal = Key(target);
            var seen = new HashSet<CobolName>();
            var queue = new Queue<string>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                string node = queue.Dequeue();
                if (!seen.Add(Key(node))) continue;
                if (Key(node) == goal) return true;
                if (ClassKeyed(node) is { } cls)
                {
                    foreach (var m in cls.Methods.Concat(cls.FactoryMethods))
                        if (ReturningReference(m) is { } r) queue.Enqueue(r);
                    if (cls.Base is { } b) queue.Enqueue(b.Name);
                    foreach (var i in cls.Implements.Concat(cls.FactoryImplements)) queue.Enqueue(i.Name);
                }
                else if (InterfaceKeyed(node) is { } iface)
                {
                    foreach (var p in AllPrototypes(iface))
                        if (ReturningReference(p) is { } r) queue.Enqueue(r);
                    foreach (var i in iface.Inherits) queue.Enqueue(i.Name);
                }
            }
            return false;
        }

        /// <summary>The class or interface a method's RETURNING item references — the name an object reference described with an
        /// object-class-name, an interface-name or ACTIVE-CLASS (whose descriptor names the containing class) carries; null for a
        /// universal reference or a returning item that is not an object reference.</summary>
        internal static string? ReturningReference(OoMethodSymbol m) =>
            m.Binding?.Returning?.Pic is { Category: PicCategory.ObjectReference, ObjectRef: { Kind: not ObjectRefKind.Universal, Name: { } n } }
                ? n : null;

        /// <summary>For each written word of <paramref name="words"/> (an INHERITS clause's operands), whether an EARLIER word of the
        /// list is the same name (§2023:8.3.2.2: the Annex C fold): §2023:11.3.3 SR7 and §2023:11.6.3 SR6 forbid a repeat.</summary>
        internal static bool[] RepeatsAnEarlierWord(IReadOnlyList<ParserRuleContext> words)
        {
            var seen = new HashSet<CobolName>();
            var repeats = new bool[words.Count];
            for (int i = 0; i < words.Count; i++)
                repeats[i] = !seen.Add(Key(words[i].GetText()));
            return repeats;
        }

        /// <summary>Is the written <paramref name="word"/> the same name as one of <paramref name="declared"/> (a parameterized class
        /// definition's parameter-names, §2023:11.3.2 `USING parameter-name-1`; or its own name)?</summary>
        internal static bool NamesOneOf(ParserRuleContext word, IEnumerable<ParserRuleContext> declared)
        {
            var key = Key(word.GetText());
            return declared.Any(d => Key(d.GetText()) == key);
        }

        /// <summary>Is <paramref name="written"/> the parameter-name a parameterized class definition bound as itself names in its
        /// INHERITS clause (<see cref="OoClassSymbol.FormalBaseName"/>; §2023:8.4.3.8.3 SR4 over a formal superclass, kb/Work
        /// PB2937)?</summary>
        internal static bool NamesFormalBase(ParserRuleContext written, OoClassSymbol cls) =>
            cls.FormalBaseName is { } formal && Key(written.GetText()) == Key(formal);
    }

    /// <summary>
    /// The set of object-class-names and interface-names ONE source element may reference (ISO §2023:8.4.6.4): its own REPOSITORY
    /// declarations plus every containing source element's (§2023:12.3.4 GR1), plus the name of the containing class or interface
    /// definition itself — §2023:12.3.8.3 SR5/SR8 say the self-naming specifier "is ignored", so the definition's own name is in
    /// scope whether or not it is declared. Built from the parse tree by an ancestor walk, because §2023:12.3.3 SR1 to SR3 confine
    /// a REPOSITORY paragraph to the outermost program, a function definition, a class definition and an interface definition:
    /// the walk IS the rule, with no state to thread. Keyed by <see cref="CobolName"/>; its questions answer a fact (whether a
    /// word is declared, and the externalized name a specifier's AS phrase gave it), never a symbol: <see cref="ClassNames"/>
    /// resolves.
    /// </summary>
    internal sealed class RepositoryScope
    {
        /// <summary>The scope of a reference outside every source element that can declare one. It admits nothing: §2023:8.4.6.4
        /// offers no third source for a visible name.</summary>
        internal static readonly RepositoryScope Empty = new([], [], []);

        // Declared name → the EXTERNALIZED name the specifier's `AS literal-n` gives, or null when it wrote none (the containing
        // definition's own name, too): null resolves by the declared word (kb/Work PB974).
        private readonly Dictionary<CobolName, string?> _classes;
        private readonly Dictionary<CobolName, string?> _interfaces;
        // The parameter-names of a containing PARAMETERIZED class or interface definition (its USING clause).
        private readonly Dictionary<CobolName, Core.OoParameterNameContext> _formals;

        private RepositoryScope(Dictionary<CobolName, string?> classes, Dictionary<CobolName, string?> interfaces,
            Dictionary<CobolName, Core.OoParameterNameContext> formals)
        {
            _classes = classes;
            _interfaces = interfaces;
            _formals = formals;
        }

        internal bool DeclaresClass(string name) => _classes.ContainsKey(Key(name));
        internal bool DeclaresInterface(string name) => _interfaces.ContainsKey(Key(name));

        /// <summary>Whether <paramref name="name"/> is a parameter-name of the parameterized class or interface definition this
        /// scope lies in (§2023:11.3.2 / §2023:11.6.2 `USING parameter-name-1`). Only the written skeleton has one: an expansion's
        /// re-parsed text has its USING clause dropped and every formal replaced (kb/Work PB759).</summary>
        internal bool IsFormal(string name) => _formals.ContainsKey(Key(name));

        /// <summary>The parameter-name of the definition this scope lies in that <paramref name="name"/> spells: its declaration, the
        /// symbol of its entry (<see cref="ClassNames.DeclareParameterNames"/>).</summary>
        internal Core.OoParameterNameContext? FormalDeclaration(string name) => _formals.GetValueOrDefault(Key(name));

        /// <summary>Whether <paramref name="name"/> is a class-name in this scope, and the externalized name its specifier's AS
        /// phrase gave it (null: resolve by the declared word).</summary>
        internal bool ClassTarget(string name, out string? externalized) => _classes.TryGetValue(Key(name), out externalized);

        /// <summary>The interface twin of <see cref="ClassTarget"/>.</summary>
        internal bool InterfaceTarget(string name, out string? externalized) => _interfaces.TryGetValue(Key(name), out externalized);

        /// <summary>Build the scope visible at <paramref name="site"/> by walking its parse-tree ancestors: every REPOSITORY
        /// paragraph on the way out contributes (the reference's own source element first, then each containing one —
        /// §2023:12.3.4 GR1), and a class / interface definition contributes its own name. <paramref name="variables"/> is the
        /// group's &gt;&gt;DEFINE timeline, for a FROM constant-name written as a specifier's AS operand. The AS literals are read
        /// in the spelling of <paramref name="dialectLevel"/>, the edition's (kb/Work PB2840).</summary>
        internal static RepositoryScope Build(RuleContext? site, DirectiveTimeline<CompilationVariableEvent> variables,
            int dialectLevel)
        {
            var classes = new Dictionary<CobolName, string?>();
            var interfaces = new Dictionary<CobolName, string?>();
            var formals = new Dictionary<CobolName, Core.OoParameterNameContext>();
            for (RuleContext? c = site; c is not null; c = c.Parent)
            {
                switch (c)
                {
                    case Core.ClassDefinitionContext cd:
                        // className()[0] is the class's OWN name; the rest are the INHERITS operands (§2023:11.3.2's
                        // `[ INHERITS FROM { object-class-name-2 } … ]`), which are REFERENCES. §2023:12.3.8.3 SR5/SR8: a specifier
                        // naming the containing definition "is ignored" — its own name resolves to it, whatever an inner entry wrote.
                        if (cd.classIdParagraph()?.className().FirstOrDefault() is { } cn) classes[Key(cn.GetText())] = null;
                        foreach (var p in cd.classIdParagraph()?.ooParameterName() ?? []) formals.TryAdd(Key(p.GetText()), p);
                        break;
                    case Core.InterfaceDefinitionContext idf:
                        if (idf.interfaceName().FirstOrDefault() is { } inm) interfaces[Key(inm.GetText())] = null;
                        foreach (var p in idf.ooParameterName()) formals.TryAdd(Key(p.GetText()), p);
                        break;
                }
                // The environment division of THIS context, when it directly carries one. Only a source-element context does; a
                // statement or clause context has no such child, so the test is a cheap miss.
                for (int i = 0; i < c.ChildCount; i++)
                    if (c.GetChild(i) is Core.EnvironmentDivisionContext env)
                        Collect(env, classes, interfaces, variables, dialectLevel);
            }
            return classes.Count == 0 && interfaces.Count == 0 ? Empty : new RepositoryScope(classes, interfaces, formals);
        }

        private static void Collect(Core.EnvironmentDivisionContext env, Dictionary<CobolName, string?> classes,
            Dictionary<CobolName, string?> interfaces, DirectiveTimeline<CompilationVariableEvent> variables, int dialectLevel)
        {
            // The source element's OWN specifiers are collected before a containing element's (the ancestor walk runs outward), so
            // TryAdd keeps the innermost declaration — §2023:12.3.4 GR1 spreads a container's entries to contained units, and a
            // contained unit's own specifier for the same name is the one in force there. The literal is read by the PURE
            // ExternalizedName.Peek: DataBinder.BindSpecifierExternalizedName reports on it, once, and a rejected literal resolves
            // by the declared word — its own fallback. A constant-name operand (§2023:13.10.3 SR2; kb/Work PB2588) is read off the
            // parse tree, because no DATA DIVISION is bound yet.
            foreach (var re in SpecifierEntries(env))
            {
                string? ext = re.externalizedNamePhrase() is { } asPhrase
                    ? ExternalizedName.Peek(asPhrase, k => IdentifyConstantBeforeBind(k, variables), dialectLevel)
                    : null;
                if (re.className() is { } cn) classes.TryAdd(Key(cn.GetText()), ext);
                else if (re.interfaceName() is { } inm) interfaces.TryAdd(Key(inm.GetText()), ext);
            }
        }

        /// <summary>The class-specifiers and interface-specifiers (§2023:12.3.8.2) of the REPOSITORY paragraph directly in
        /// <paramref name="env"/> — with or without an EXPANDS phrase, whose object-class-name-1 / interface-name-2 is DECLARED
        /// exactly as a plain specifier's is (kb/Work PB759). The one walk this scope and <see cref="OoExpansion"/> (a parameterized
        /// definition's own formals, §2023:11.3.3 SR8 / §2023:11.6.3 SR4) share.</summary>
        internal static IEnumerable<Core.RepositoryEntryContext> SpecifierEntries(Core.EnvironmentDivisionContext? env)
            => RepositoryEntries(env).Where(r => r.CLASS() is not null || r.INTERFACE() is not null);

        /// <summary>Every specifier of the REPOSITORY paragraph directly in <paramref name="env"/> — the scope a parameterized
        /// definition's parameter-names are declared in (<c>DataBinder.DeclareParameterNames</c>, kb/Work PB1744), whose FUNCTION …
        /// INTRINSIC specifiers are not class- or interface-specifiers.</summary>
        internal static IEnumerable<Core.RepositoryEntryContext> RepositoryEntries(Core.EnvironmentDivisionContext? env)
            => env?.configurationSection()?.configurationParagraph()
                   .Select(p => p.repositoryParagraph()).Where(r => r is not null)
                   .SelectMany(r => r!.repositoryEntry()) ?? [];

        /// <summary>The class-specifier or interface-specifier of <paramref name="paragraph"/> that declares the written word
        /// <paramref name="word"/> (an expansion's actual, ISO §2023:12.3.8.4 GR5 / GR8), or null.</summary>
        internal static Core.RepositoryEntryContext? SpecifierDeclaring(IEnumerable<Core.RepositoryEntryContext> paragraph, string word)
        {
            var key = Key(word);
            return paragraph.FirstOrDefault(r => (r.className()?.GetText() ?? r.interfaceName()?.GetText()) is { } d && Key(d) == key);
        }
    }
}
