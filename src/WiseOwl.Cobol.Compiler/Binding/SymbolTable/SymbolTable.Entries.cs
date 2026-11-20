// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Symbols;

namespace WiseOwl.Cobol.Binding;

/// <summary>
/// An ENTRY of the symbol table (docs/rearchitecture/DESIGN-listing.md section 6.7.1): an opaque number, never an entry or a symbol,
/// made when the binder declares the name (<see cref="SymbolTable.EntryStore.Enter"/>) and handed back with every identification of it
/// (<see cref="IdentifiedName.Id"/>), so an occurrence joins its entry by id for every class (listing step 7b). 1-based within the
/// compilation group; <c>default</c> is no entry.
/// </summary>
internal readonly record struct SymbolId(int Value)
{
    /// <summary>Whether this is an entry (the default is none).</summary>
    public bool IsSet => Value > 0;
}

/// <summary>A candidate set an identification may answer instead of one symbol (a report-section name that reaches several entries,
/// a CAPACITY register several tables name, a procedure-name several procedures carry): the entry of each candidate, in the order of
/// its candidates. Listing step 7b records an occurrence when the set has exactly one member.</summary>
internal interface ICandidateEntries
{
    /// <summary>Each candidate's entry, in the order of the candidates.</summary>
    IReadOnlyList<SymbolId> Ids { get; }
}

public sealed partial class SymbolTable
{
    /// <summary>
    /// ⛔ THE ENTRIES OF ONE COMPILATION GROUP (docs/rearchitecture/DESIGN-listing.md section 6.7.1, "Step 6b, the entry ids"): every
    /// name the group's source elements declare, entered once where the binder declares it, in the order declared, with its class,
    /// its name as written, where it stands and the symbol the binder made for it. One per group: the elements' tables and the
    /// compilation group's tier (<see cref="ClassNames"/>) enter into the same one (<see cref="Compiler.Oo.OoClassTable.Entries"/>), so
    /// an entry's id is unique in the group and a contained element that identifies its container's GLOBAL name answers the
    /// container's entry.
    /// <para>The id stands BESIDE THE SYMBOL in one place for every class: an identity index from the symbol the declaration made (a
    /// <c>FileModel</c>, a <c>ReportModel</c>, a <c>DataItem</c>, an <c>IndexDeclaration</c>, an <c>OoClassSymbol</c>, ...) to its
    /// entry. A name an element inherits holds its container's symbol, so it answers the container's entry with nothing copied; and a
    /// class whose answer is no object of its own declaration (a paragraph's pc, a configuration-section value, an implementor-name
    /// row a mnemonic-name shares) enters a declaration object of its own (<see cref="NameDeclaration{T}"/>) that its table keeps.</para>
    /// <para>Every compilation makes them (the table is the binder's one store of names); the finished snapshot reads them
    /// (<c>SymbolTableFinish.Of</c>) only when a listing asks for the symbol table.</para>
    /// </summary>
    internal sealed class EntryStore
    {
        private readonly List<Entry> _entries = [];
        private readonly Dictionary<object, SymbolId> _bySymbol = new(ReferenceEqualityComparer.Instance);

        /// <summary>Enter the declaration of <paramref name="name"/> of class <paramref name="class"/> at <paramref name="at"/>, whose
        /// symbol is <paramref name="symbol"/>, made by <paramref name="element"/>'s table (null for the compilation group's tier).
        /// A symbol is entered once: a second declaration of one symbol is an internal error.</summary>
        internal SymbolId Enter(SymbolTable? element, NameClass @class, string name, SymbolPosition at, object symbol)
        {
            var id = new SymbolId(_entries.Count + 1);
            if (!_bySymbol.TryAdd(symbol, id))
                throw new InvalidOperationException($"the symbol table entered '{name}' ({@class}) twice");
            _entries.Add(new Entry(element, @class, name, at, symbol));
            return id;
        }

        /// <summary>The entry the declaration of <paramref name="symbol"/> made, or none.</summary>
        internal SymbolId IdOf(object symbol) => _bySymbol.GetValueOrDefault(symbol);

        /// <summary>Make <paramref name="answer"/>, an object the binder made LATER for a name already entered (a constant entry's
        /// bound value, a program prototype a specifier resolved to), answer the entry <paramref name="id"/>.</summary>
        internal void Answer(object answer, SymbolId id)
        {
            if (!id.IsSet) throw new InvalidOperationException("an answer joins an entry that exists");
            _bySymbol.TryAdd(answer, id);
        }

        /// <summary>The entry <paramref name="id"/>.</summary>
        internal Entry this[SymbolId id] => _entries[id.Value - 1];

        /// <summary>Every entry, in the order entered.</summary>
        internal int Count => _entries.Count;

        /// <summary>The entries <paramref name="element"/>'s table made (null: the compilation group tier's), in the order entered.</summary>
        internal IEnumerable<(SymbolId Id, Entry Entry)> MadeBy(SymbolTable? element)
        {
            for (int i = 0; i < _entries.Count; i++)
                if (ReferenceEquals(_entries[i].Element, element))
                    yield return (new SymbolId(i + 1), _entries[i]);
        }
    }

    /// <summary>One entry: the table that made it (null for the group tier), its class, its name as written, where it stands
    /// (the position the listing places it at; line 0 for a name the standard declares implicitly) and the declaration's symbol,
    /// whose attributes the finish reads once the bind is finished (section 6.6).</summary>
    internal readonly record struct Entry(SymbolTable? Element, NameClass Class, string Name, SymbolPosition At, object Symbol);

    /// <summary>The declaration object of a name whose answer is no object of its own (a configuration-section value, a mnemonic's
    /// implementor-name row, a paragraph's pc): the class table keeps it, and its identity is the entry's.</summary>
    private sealed class NameDeclaration<T>(T value)
    {
        public T Value { get; } = value;
    }

    /// <summary>The group's entries this element's table enters into.</summary>
    private readonly EntryStore _entries;

    /// <summary>The compilation group's entries (the finish reads them).</summary>
    internal EntryStore GroupEntries => _entries;

    /// <summary>Enter a name this element declares.</summary>
    private SymbolId Enter(NameClass @class, string name, SymbolPosition at, object symbol) =>
        _entries.Enter(this, @class, name, at, symbol);

    /// <summary>The entry the declaration of <paramref name="symbol"/> made, in this element or any other of the group.</summary>
    internal SymbolId EntryOf(object symbol) => _entries.IdOf(symbol);

    /// <summary>A position of the source: the ANTLR line (1-based) and column (0-based).</summary>
    private static SymbolPosition PositionOf(WiseOwl.Cobol.Editions.DiagnosticCursor at) => new(at.Line, at.Column);

    /// <summary>The position of a token.</summary>
    private static SymbolPosition PositionOf(Antlr4.Runtime.IToken token) => new(token.Line, token.Column);
}
