// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace WiseOwl.Cobol.Analyzers;

/// <summary>⛔ ONE DECIDER OF A MESSAGE'S WORDS BY EDITION (kb/Work PB4950; docs/VERSION_TEST_MATRIX_DESIGN.md P2.3a), run over the REAL
/// compilation of every <c>src/</c> project at build time. <c>EditionInfo.Cite(y1985, later, y2002, y2014)</c> is the one choice of a
/// citation, or of the words a message quotes, by edition; a site that spells its own choice is a second decider the next edition's text
/// has to be threaded through by hand.
/// <para><b>EDITION-DERIVED VALUES, AS A FIXED POINT OF THE SEMANTIC MODEL.</b> The seeds are the members of <c>EditionInfo</c> that answer
/// which edition or a number of it (each public instance <c>int</c> property, each public instance <c>bool</c> method of one <c>int</c>:
/// <c>Has</c>), read from its symbol, plus any property named <c>DialectLevel</c> or <c>Std</c>. A <c>bool</c> or <c>int</c> local or
/// field whose initializer, or property, method or local function whose return expression, reads a derived value (or compares a name
/// that says edition, level or year with an edition) is derived too: a method only when every parameter carries an edition
/// (<c>Is85(EditionInfo e)</c>, <c>Printed(int edition)</c>), since one that also takes the program's data answers a question about
/// the program. The derivation repeats until nothing changes, across the files of the compilation. <c>EditionInfo</c>'s own file is
/// the decider: it is neither scanned nor derived from (its <c>bool</c> properties are named rule facts).</para>
/// <para><b>ACROSS ASSEMBLIES, BY ATTRIBUTE.</b> An analyzer sees one compilation, and a referenced assembly is metadata with no bodies to
/// derive from. So a derived member another assembly can read (public or protected all the way out, or internal in an assembly that
/// grants <c>InternalsVisibleTo</c> to an assembly that is not a test, read from the compilation's own attributes) must carry <c>[EditionDerived]</c> (<see cref="UnmarkedId"/>, an error where it is
/// declared; a seed, which every compilation knows by name, needs none), and a referenced member that carries it is derived in the compilation that reads it. The rule that requires the mark
/// is the one that computes the need for it, so the set cannot drift. The attribute is matched by its name,
/// <c>EditionDerivedAttribute</c>, in any namespace (<c>WiseOwl.Cobol.Runtime.EditionDerivedAttribute</c> is the products' one).</para>
/// <para><b>THE CHOICES REFUSED</b> (<see cref="ChoiceId"/>), outside generated code and the code generator (whose strings are emitted C#):
/// a conditional expression, a switch expression or statement, an if/else, or an if that returns with the next statement returning,
/// that chooses between two strings (written, or of type string and not a call: Cite would run both arms) on a condition that reads an
/// edition-derived value; and an interpolation hole whose own value a derived member computes, a string or a number spliced into a
/// citation (<c>§{Printed(edition)}:8.12</c>). A choice that can answer null is a verdict (whether a rule fires), and a hole that
/// prints the compile edition (<c>COBOL-{DialectLevel}</c>) names it. A program question beside the edition stays a ternary:
/// <c>x &amp;&amp; is1985 ? a : b</c> is written <c>x ? Cite(a, b) : b</c>.</para></summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EditionCiteAnalyzer : DiagnosticAnalyzer
{
    /// <summary>A string chosen by edition outside <c>EditionInfo.Cite</c>.</summary>
    public const string ChoiceId = "WOCA0001";

    /// <summary>An edition-derived member another assembly can read, not marked <c>[EditionDerived]</c>.</summary>
    public const string UnmarkedId = "WOCA0002";

    /// <summary>The decider's type, by metadata name.</summary>
    public const string EditionInfoName = "WiseOwl.Cobol.Editions.EditionInfo";

    /// <summary>The cross-assembly mark, by type name.</summary>
    public const string AttributeName = "EditionDerivedAttribute";

    /// <summary>The editions as the source compares them; <c>EditionCiteAnalyzerTests</c> pins this to <c>EditionInfo.All</c> (an
    /// analyzer cannot evaluate a referenced assembly's static).</summary>
    public static IReadOnlyList<int> Editions { get; } = [85, 2002, 2014, 2023];

    private static readonly HashSet<int> EditionValues = [.. Editions];

    private static readonly DiagnosticDescriptor Choice = new(ChoiceId,
        "A string chosen by edition outside EditionInfo.Cite",
        "This {0} chooses a string by edition, directly or through an alias of the edition; write it as "
        + "EditionInfo.Cite(y1985, later[, y2002][, y2014]) (kb/Work PB4950, docs/VERSION_TEST_MATRIX_DESIGN.md P2.3a)",
        "WiseOwl.Editions", DiagnosticSeverity.Error, isEnabledByDefault: true, customTags: [WellKnownDiagnosticTags.CompilationEnd, WellKnownDiagnosticTags.NotConfigurable]);

    private static readonly DiagnosticDescriptor Unmarked = new(UnmarkedId,
        "An edition-derived member another assembly can read is not marked [EditionDerived]",
        "'{0}' answers an edition question and another assembly can read it; mark it [EditionDerived] so the compilations that read it "
        + "derive from it (kb/Work PB4950, docs/VERSION_TEST_MATRIX_DESIGN.md P2.3a)",
        "WiseOwl.Editions", DiagnosticSeverity.Error, isEnabledByDefault: true, customTags: [WellKnownDiagnosticTags.CompilationEnd, WellKnownDiagnosticTags.NotConfigurable]);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [Choice, Unmarked];

    public override void Initialize(AnalysisContext context)
    {
        // Generated trees (an auto-generated header: the ANTLR lexer and parser, the *.Table.cs files) are analyzed and reported
        // like any other (kb/Work PB4950 round 8, R8-1): a member derived there is derived wherever it is read, and which trees hold
        // no message is this analyzer's own decision (IsOutsideTheMessages), not the header's.
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var state = new State(start.Compilation.GetTypeByMetadataName(EditionInfoName),
                InternalsReachAnotherProduct(start.Compilation.Assembly), OursIn(start.Compilation));
            start.RegisterSemanticModelAction(sm => state.Collect(sm.SemanticModel, sm.CancellationToken));
            start.RegisterCompilationEndAction(state.Report);
        });
    }

    /// <summary>⛔ ONE SYMBOL FOR ONE MEMBER (kb/Work PB4950 round 11, R10-2). A reduced extension method (<c>e.IsOld()</c>) and a member
    /// of a constructed generic type (<c>Rules&lt;int&gt;.Old</c>) are other symbols than the one declared: every symbol the analyzer
    /// compares is taken through <c>ReducedFrom</c> and then <c>OriginalDefinition</c>, here and nowhere else.</summary>
    private static ISymbol Normalize(ISymbol symbol) =>
        (symbol is IMethodSymbol { ReducedFrom: { } reduced } ? reduced : symbol).OriginalDefinition;

    /// <summary>The members a member stands for in a read through a base: the members it overrides and the interface members it
    /// implements (kb/Work PB4950 round 11, R10-4), each declared in one of OUR assemblies (<paramref name="ours"/>). A read of
    /// <c>IRule.Old</c> or of an abstract <c>Old</c> runs whatever implements it, so it is derived when an implementation in the
    /// compilation is. ⛔ Never a member of the framework or of a third party (round 12, R11-1): one edition-derived override of
    /// <c>object.GetHashCode</c>, or one implementation of <c>IReadOnlyCollection&lt;T&gt;.Count</c>, would make every unrelated read
    /// of that member in the compilation an edition question.</summary>
    private static IEnumerable<ISymbol> Bases(ISymbol member, Func<IAssemblySymbol?, bool> ours) =>
        AllBases(member).Where(b => ours(b.ContainingAssembly));

    private static IEnumerable<ISymbol> AllBases(ISymbol member)
    {
        for (ISymbol? s = member switch { IMethodSymbol m => m.OverriddenMethod, IPropertySymbol p => p.OverriddenProperty, _ => null };
             s is not null; s = s switch { IMethodSymbol m => m.OverriddenMethod, IPropertySymbol p => p.OverriddenProperty, _ => null })
            yield return Normalize(s);
        foreach (var explicitly in member switch
                 {
                     IMethodSymbol m => m.ExplicitInterfaceImplementations.Cast<ISymbol>(),
                     IPropertySymbol p => p.ExplicitInterfaceImplementations.Cast<ISymbol>(),
                     _ => [],
                 })
            yield return Normalize(explicitly);
        if (member.ContainingType is not { } type) yield break;
        foreach (var contract in type.AllInterfaces.SelectMany(i => i.GetMembers(member.Name)))
            if (type.FindImplementationForInterfaceMember(contract) is { } implementation
                && SymbolEqualityComparer.Default.Equals(Normalize(implementation), member))
                yield return Normalize(contract);
    }

    /// <summary>OUR assemblies, the ones a derivation may climb into (kb/Work PB4950 round 12, R11-1): this compilation's, and every
    /// referenced assembly that can carry the cross-assembly mark, because it declares <c>EditionDerivedAttribute</c> or references an
    /// assembly that does. Derivation crosses an assembly only by that mark, so an assembly that cannot name it (the framework, a
    /// third-party package) holds nothing to derive. It is read from the compilation's references, never from a list of names.</summary>
    private static Func<IAssemblySymbol?, bool> OursIn(Compilation compilation)
    {
        var marking = new HashSet<IAssemblySymbol>(SymbolEqualityComparer.Default);
        foreach (var assembly in compilation.SourceModule.ReferencedAssemblySymbols.Append(compilation.Assembly))
            if (assembly.TypeNames.Contains(AttributeName))
                marking.Add(assembly);
        return assembly => assembly is not null
            && (SymbolEqualityComparer.Default.Equals(assembly, compilation.Assembly) || marking.Contains(assembly)
                || assembly.Modules.Any(m => m.ReferencedAssemblySymbols.Any(marking.Contains)));
    }

    /// <summary>A declaration that can carry an edition-derived value: what it declares, what its bodies read, and whether they compare
    /// an edition-named value with an edition.</summary>
    private sealed class Declaration(ISymbol declared, ImmutableArray<ISymbol> reads, bool comparesEdition)
    {
        public ISymbol Declared { get; } = declared;
        public ImmutableArray<ISymbol> Reads { get; } = reads;
        public bool ComparesEdition { get; } = comparesEdition;
    }

    /// <summary>A candidate choice: it is reported when it fires unconditionally (an edition-named value compared with an edition) or
    /// when a symbol its condition reads is a seed or derived (only derived, for an interpolation hole's head).</summary>
    private sealed class Candidate(Location location, string kind, ImmutableArray<ISymbol> reads, bool fires, bool derivedOnly)
    {
        public Location Location { get; } = location;
        public string Kind { get; } = kind;
        public ImmutableArray<ISymbol> Reads { get; } = reads;
        public bool Fires { get; } = fires;
        public bool DerivedOnly { get; } = derivedOnly;
    }

    private sealed class State(INamedTypeSymbol? editionInfo, bool internalsReachAnotherProduct, Func<IAssemblySymbol?, bool> ours)
    {
        private readonly HashSet<string> _seedNames = SeedNames(editionInfo);
        private readonly ConcurrentBag<Declaration> _declarations = [];
        private readonly ConcurrentBag<Candidate> _candidates = [];

        private static HashSet<string> SeedNames(INamedTypeSymbol? type)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            if (type is null) return names;
            foreach (var member in type.GetMembers())
            {
                if (member.IsStatic || member.DeclaredAccessibility != Accessibility.Public) continue;
                if (member is IPropertySymbol { Type.SpecialType: SpecialType.System_Int32 }
                    || (member is IMethodSymbol { MethodKind: MethodKind.Ordinary, ReturnType.SpecialType: SpecialType.System_Boolean } m
                        && m.Parameters.Length == 1 && m.Parameters[0].Type.SpecialType == SpecialType.System_Int32))
                    names.Add(member.Name);
            }
            return names;
        }

        private bool IsSeed(ISymbol symbol) => Normalize(symbol) is var s &&
            ((s.ContainingType is { } ct && SymbolEqualityComparer.Default.Equals(ct.OriginalDefinition, editionInfo) && _seedNames.Contains(s.Name))
            || (s is IPropertySymbol && s.Name is "DialectLevel" or "Std"));

        private static bool IsMarked(ISymbol s) =>
            Normalize(s).GetAttributes().Any(a => a.AttributeClass?.Name == AttributeName);

        private bool NamesEdition(ExpressionSyntax e)
        {
            string name = e switch
            {
                IdentifierNameSyntax id => id.Identifier.ValueText,
                MemberAccessExpressionSyntax m => m.Name.Identifier.ValueText,
                _ => "",
            };
            return Says(name, "edition") || Says(name, "level") || Says(name, "year") || _seedNames.Contains(name)
                || name is "DialectLevel" or "Std";
        }

        private static bool Says(string name, string word) =>
        name.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0;   // not a COBOL word: a C# identifier and an English word

        /// <summary>A name that says it holds an edition compared, or matched, with an edition constant.</summary>
        private bool ComparesEditionName(SyntaxNode node) => node.DescendantNodesAndSelf().Any(n => n switch
        {
            BinaryExpressionSyntax b => (IsEditionValue(b.Right) && NamesEdition(b.Left)) || (IsEditionValue(b.Left) && NamesEdition(b.Right)),
            IsPatternExpressionSyntax p => NamesEdition(p.Expression)
                && p.Pattern.DescendantNodesAndSelf().OfType<LiteralExpressionSyntax>().Any(IsEditionValue),
            _ => false,
        });

        /// <summary>A parameter that carries an edition: of type EditionInfo or EditionContext, or a number named for one.</summary>
        private bool IsEditionParameter(ParameterSyntax p) =>
            p.Type?.ToString() is "EditionInfo" or "EditionContext" or "WiseOwl.Cobol.Binding.EditionContext"
            || (p.Type?.ToString() == "int" && NamesEdition(SyntaxFactory.IdentifierName(p.Identifier.ValueText)));

        /// <summary>A hole that is the compile edition's number itself (<c>Year</c>, <c>DialectLevel</c>, an edition-named int): it names
        /// the edition, as the edition gate's "(targeting COBOL-85)" does, and chooses no text.</summary>
        private bool PrintsTheEdition(ExpressionSyntax e) => e switch
        {
            IdentifierNameSyntax id => NamesEdition(id),
            MemberAccessExpressionSyntax m => m.Name.Identifier.ValueText is "Year" or "DialectLevel",
            _ => false,
        };

        public void Collect(SemanticModel model, CancellationToken cancel)
        {
            var tree = model.SyntaxTree;
            if (editionInfo is not null && editionInfo.DeclaringSyntaxReferences.Any(r => r.SyntaxTree == tree)) return;   // the decider
            var root = tree.GetRoot(cancel);

            // ── The declarations that can carry an edition-derived value ──
            foreach (var node in root.DescendantNodes())
            {
                (ISymbol? declared, IEnumerable<SyntaxNode> bodies) = node switch
                {
                    VariableDeclaratorSyntax { Initializer: { } init } v => (model.GetDeclaredSymbol(v, cancel), new SyntaxNode[] { init.Value }),
                    PropertyDeclarationSyntax p => (model.GetDeclaredSymbol(p, cancel), ReturnedBy(p.ExpressionBody,
                        p.AccessorList?.Accessors.FirstOrDefault(a => a.IsKind(SyntaxKind.GetAccessorDeclaration)) is { } g
                            ? (SyntaxNode?)g.ExpressionBody ?? g.Body : null)),
                    // A predicate whose parameters all carry an edition is followed too: Is85(edition) answers the edition question
                    // whatever edition it is handed. One that also takes the program's data (InEdition(row), RemovedAt2023(word, level))
                    // answers a question about the program: a verdict, like a condition that reads it beside the edition.
                    MethodDeclarationSyntax m when m.ParameterList.Parameters.All(IsEditionParameter) =>
                        (model.GetDeclaredSymbol(m, cancel), ReturnedBy(m.ExpressionBody, m.Body)),
                    LocalFunctionStatementSyntax f when f.ParameterList.Parameters.All(IsEditionParameter) =>
                        (model.GetDeclaredSymbol(f, cancel), ReturnedBy(f.ExpressionBody, f.Body)),
                    _ => ((ISymbol?)null, Enumerable.Empty<SyntaxNode>()),
                };
                // An edition question answers a bool or an int, as the seeds do; a string or a record computed from an edition-derived
                // value (a picture folded by edition, a rule record) carries data, not the question, and is not followed.
                if (declared is null || TypeOf(declared) is not { SpecialType: SpecialType.System_Boolean or SpecialType.System_Int32 }) continue;
                var list = bodies.ToList();
                if (list.Count == 0) continue;
                _declarations.Add(new Declaration(Normalize(declared), [.. list.SelectMany(b => SymbolsIn(b, model, cancel))], list.Any(ComparesEditionName)));
            }

            // ── The candidate choices ──
            if (IsOutsideTheMessages(tree.FilePath)) return;
            // An arm is a string when it is written as one or its type is string (a member, a call); a null arm is no choice of words.
            // A call is a computation, not a text (two renderings of an operand, two screens of a word): Cite would run both.
            // An arm that can answer null (a message or null) is a verdict: whether the rule fires, not its words.
            bool Str(ExpressionSyntax e) => !YieldsNull(e) && (IsString(e)
                || (e is not InvocationExpressionSyntax && model.GetTypeInfo(e, cancel).Type?.SpecialType == SpecialType.System_String));
            void Add(SyntaxNode at, string kind, IEnumerable<SyntaxNode> conditions, bool fires = false) =>
                _candidates.Add(new Candidate(at.GetLocation(), kind, [.. conditions.SelectMany(c => SymbolsIn(c, model, cancel))],
                    fires || conditions.Any(ComparesEditionName), derivedOnly: false));
            foreach (var node in root.DescendantNodes())
            {
                switch (node)
                {
                    case ConditionalExpressionSyntax c when Str(c.WhenTrue) && Str(c.WhenFalse):
                        Add(c, "conditional expression", [c.Condition]);
                        break;
                    case SwitchExpressionSyntax s when s.Arms.Count(a => Str(a.Expression)) >= 2 && !s.Arms.Any(a => YieldsNull(a.Expression)):
                        Add(s, "switch expression",
                            [s.GoverningExpression, .. s.Arms.Select(a => a.WhenClause?.Condition).OfType<ExpressionSyntax>()],
                            fires: NamesEdition(s.GoverningExpression)
                                && s.Arms.Any(a => a.Pattern.DescendantNodesAndSelf().OfType<LiteralExpressionSyntax>().Any(IsEditionValue)));
                        break;
                    case IfStatementSyntax { Else: { } e } i when YieldsString(i.Statement, Str) && YieldsString(e.Statement, Str):
                        Add(i, "if/else", [i.Condition]);
                        break;
                    // An if with no else whose branch returns a string, followed by a return of another: the else is the rest of the block.
                    case IfStatementSyntax { Else: null, Parent: BlockSyntax b } i when Returns(i.Statement, Str)
                        && b.Statements.IndexOf(i) is var at && at + 1 < b.Statements.Count && Returns(b.Statements[at + 1], Str):
                        Add(i, "if/return", [i.Condition]);
                        break;
                    case SwitchStatementSyntax w when w.Sections.Count(sec => sec.Statements.Any(st => YieldsString(st, Str))) >= 2:
                        Add(w, "switch statement", [w.Expression],
                            fires: NamesEdition(w.Expression) && w.Sections.SelectMany(sec => sec.Labels)
                                .Any(l => l.DescendantNodes().OfType<LiteralExpressionSyntax>().Any(IsEditionValue)));
                        break;
                    // An interpolation hole whose OWN value an edition-derived member computes (§{Printed(edition)}) chooses the text as
                    // surely as a ternary does: a string, or a number spliced into a citation (right after "§" or right before its ":").
                    // A hole that prints the compile edition itself (COBOL-{DialectLevel}) names it, and one that hands the edition to a
                    // helper as an argument (Format(DialectLevel, "SR1")) leaves the choice to the helper, which is read where it is written.
                    case InterpolationSyntax h when !PrintsTheEdition(h.Expression) && HeadOf(h.Expression, model, cancel) is { } head
                        && (model.GetTypeInfo(h.Expression, cancel).Type?.SpecialType == SpecialType.System_String || SplicedIntoCitation(h)):
                        _candidates.Add(new Candidate(h.GetLocation(), "interpolation", [head], fires: false, derivedOnly: true));
                        break;
                }
            }
        }

        public void Report(CompilationAnalysisContext end)
        {
            var declarations = _declarations.ToList();
            var derived = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
            bool Derived(ISymbol s) => derived.Contains(Normalize(s)) || IsMarked(s);
            bool Add(ISymbol s)
            {
                bool added = derived.Add(s);
                foreach (var b in Bases(s, ours))   // a read through the base member is a read of this one
                    added |= derived.Add(b);
                return added;
            }
            for (bool grew = true; grew;)
            {
                grew = false;
                foreach (var d in declarations)
                    if (!derived.Contains(d.Declared) && (d.ComparesEdition || d.Reads.Any(s => IsSeed(s) || Derived(s))))
                        grew |= Add(d.Declared);
            }
            foreach (var c in _candidates)
                if (c.DerivedOnly ? c.Reads.Any(Derived) : c.Fires || c.Reads.Any(s => IsSeed(s) || Derived(s)))
                    end.ReportDiagnostic(Diagnostic.Create(Choice, c.Location, c.Kind));
            // Every derived member this compilation declares, a base member included (an interface or abstract member has no body of
            // its own to be a declaration), that another assembly can read carries the mark.
            foreach (var s in derived.Where(s => SymbolEqualityComparer.Default.Equals(s.ContainingAssembly, end.Compilation.Assembly)
                         && s.Locations.Any(l => l.IsInSource)).OrderBy(s => s.Locations[0].SourceSpan.Start))
                if (!IsSeed(s) && VisibleToAnotherAssembly(s, internalsReachAnotherProduct) && !IsMarked(s))
                    end.ReportDiagnostic(Diagnostic.Create(Unmarked, s.Locations.First(l => l.IsInSource), s.ToDisplayString()));
        }
    }

    /// <summary>Code that writes no message: generated code (a <c>Generated</c> directory, a <c>.g.cs</c> file), the build's own compile
    /// items (<c>obj/</c>), and the code generator (<c>CodeGen</c>), whose strings are the C# it emits, which no reader of a diagnostic sees.</summary>
    private static bool IsOutsideTheMessages(string path)
    {
        string[] segments = path.Replace('\\', '/').Split('/');
        return segments.Contains("Generated") || segments.Contains("obj") || segments.Contains("CodeGen")
            || path.EndsWith(".g.cs", StringComparison.Ordinal);
    }

    /// <summary>Whether another assembly can read the member: public or protected all the way out; or internal, when the compilation
    /// grants <c>InternalsVisibleTo</c> to another product assembly (<paramref name="internalsReachAnotherProduct"/>).</summary>
    private static bool VisibleToAnotherAssembly(ISymbol symbol, bool internalsReachAnotherProduct)
    {
        for (ISymbol? s = symbol; s is not null and not INamespaceSymbol; s = s.ContainingSymbol)
        {
            bool outward = s.DeclaredAccessibility is Accessibility.Public or Accessibility.Protected or Accessibility.ProtectedOrInternal;
            bool internalToAProduct = internalsReachAnotherProduct
                && s.DeclaredAccessibility is Accessibility.Internal or Accessibility.ProtectedAndInternal;
            if (!outward && !internalToAProduct) return false;
        }
        return true;
    }

    /// <summary>Whether the assembly grants <c>InternalsVisibleTo</c> to an assembly that is not a test assembly, read from the
    /// compilation's own attributes (the SDK writes a csproj's <c>InternalsVisibleTo</c> items into them). A test assembly is one
    /// whose name has a dotted segment that is <c>Tests</c> or ends in it (<c>WiseOwl.Cobol.Tests.Unit</c>): a test reads the product
    /// but is no compilation this analyzer judges. Any other grantee is taken for a product, the safe direction (a mark more, never a
    /// derivation missed).</summary>
    private static bool InternalsReachAnotherProduct(IAssemblySymbol assembly) =>
        assembly.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "System.Runtime.CompilerServices.InternalsVisibleToAttribute"
            && a.ConstructorArguments.Length == 1 && a.ConstructorArguments[0].Value is string grantee
            && !grantee.Split(',')[0].Trim().Split('.').Any(segment => segment.EndsWith("Tests", StringComparison.Ordinal)));

    /// <summary>Whether <paramref name="e"/> can answer null: the null literal, or an arm of a conditional or switch expression that can.</summary>
    private static bool YieldsNull(ExpressionSyntax e) => e switch
    {
        LiteralExpressionSyntax l => l.IsKind(SyntaxKind.NullLiteralExpression),
        ParenthesizedExpressionSyntax p => YieldsNull(p.Expression),
        ConditionalExpressionSyntax c => YieldsNull(c.WhenTrue) || YieldsNull(c.WhenFalse),
        SwitchExpressionSyntax s => s.Arms.Any(a => YieldsNull(a.Expression)),
        _ => false,
    };

    private static ITypeSymbol? TypeOf(ISymbol s) => s switch
    {
        ILocalSymbol l => l.Type,
        IFieldSymbol f => f.Type,
        IPropertySymbol p => p.Type,
        IMethodSymbol m => m.ReturnType,
        _ => null,
    };

    /// <summary>The expressions a property or method returns: its expression body, or each return statement of its block.</summary>
    private static IEnumerable<SyntaxNode> ReturnedBy(ArrowExpressionClauseSyntax? arrow, SyntaxNode? body) =>
        arrow is not null ? [arrow.Expression]
        : body switch
        {
            ArrowExpressionClauseSyntax a => [a.Expression],
            BlockSyntax b => b.DescendantNodes().OfType<ReturnStatementSyntax>().Where(r => r.Expression is not null)
                .Select(r => (SyntaxNode)r.Expression!),
            _ => [],
        };

    private static IEnumerable<ISymbol> SymbolsIn(SyntaxNode node, SemanticModel model, CancellationToken cancel) =>
        node.DescendantNodesAndSelf().OfType<SimpleNameSyntax>()
            .Select(id => model.GetSymbolInfo(id, cancel) is var info && info.Symbol is { } s ? s : info.CandidateSymbols.FirstOrDefault())
            .OfType<ISymbol>().Select(Normalize);

    private static bool IsEditionValue(ExpressionSyntax e) =>
        e is LiteralExpressionSyntax { Token.Value: int v } && EditionValues.Contains(v);

    private static bool IsString(ExpressionSyntax e) => e switch
    {
        LiteralExpressionSyntax l => l.IsKind(SyntaxKind.StringLiteralExpression),
        InterpolatedStringExpressionSyntax => true,
        ParenthesizedExpressionSyntax p => IsString(p.Expression),
        BinaryExpressionSyntax b when b.IsKind(SyntaxKind.AddExpression) => IsString(b.Left) || IsString(b.Right),
        ConditionalExpressionSyntax c => IsString(c.WhenTrue) || IsString(c.WhenFalse),
        _ => false,
    };

    /// <summary>The symbol whose value a hole is: the method a call invokes, or the member or local it reads.</summary>
    private static ISymbol? HeadOf(ExpressionSyntax e, SemanticModel model, CancellationToken cancel) => e switch
    {
        ParenthesizedExpressionSyntax p => HeadOf(p.Expression, model, cancel),
        InvocationExpressionSyntax or MemberAccessExpressionSyntax or IdentifierNameSyntax =>
            model.GetSymbolInfo(e, cancel) is var info && (info.Symbol ?? info.CandidateSymbols.FirstOrDefault()) is { } s ? Normalize(s) : null,
        _ => null,
    };

    /// <summary>A hole spliced into a citation: the text before it ends in "§", or the text after it begins with ":".</summary>
    private static bool SplicedIntoCitation(InterpolationSyntax h)
    {
        var contents = ((InterpolatedStringExpressionSyntax)h.Parent!).Contents;
        int at = contents.IndexOf(h);
        return (at > 0 && contents[at - 1] is InterpolatedStringTextSyntax before && before.TextToken.ValueText.EndsWith("§", StringComparison.Ordinal))
            || (at + 1 < contents.Count && contents[at + 1] is InterpolatedStringTextSyntax after
                && after.TextToken.ValueText.StartsWith(":", StringComparison.Ordinal));
    }

    private static bool Returns(StatementSyntax s, Func<ExpressionSyntax, bool> str) => s switch
    {
        BlockSyntax { Statements.Count: 1 } b => Returns(b.Statements[0], str),
        ReturnStatementSyntax { Expression: { } r } => str(r),
        _ => false,
    };

    private static bool YieldsString(StatementSyntax s, Func<ExpressionSyntax, bool> str) => s switch
    {
        BlockSyntax { Statements.Count: 1 } b => YieldsString(b.Statements[0], str),
        ReturnStatementSyntax { Expression: { } r } => str(r),
        ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax a } => str(a.Right),
        _ => false,
    };
}
