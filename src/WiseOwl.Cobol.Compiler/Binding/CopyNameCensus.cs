// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding;

/// <summary>A COPY statement's text-name or library-name as a user-defined word of the program unit it stands in: the word, its type
/// and the RESULTANT line the diagnostic about it is positioned at (kb/Work PB5482).</summary>
internal readonly record struct CopyWord(string Word, UserWordKind Kind, int Line);

/// <summary>⛔ THE ONE PLACE A COPY STATEMENT'S NAMES ARE ASSIGNED TO A SOURCE PROGRAM (kb/Work PB5482). §1985:IV.4.2.2.1.1: "Within a
/// given source program, but excluding any contained program, the user-defined words are grouped into the following disjoint
/// sets" — text-names and library-names among them — so a COPY statement's names are words of the program the statement stands
/// in, and a contained program's are its own. The preprocessor resolves the statement and gives each name with the physical line
/// of the statement (<see cref="CopyNameUse"/>); this places the statement in the resultant text, where the lines it leaves behind
/// keep its origin, and in the program unit whose text contains it: the innermost unit whose first and last tokens bracket the statement,
/// so a statement written after a contained program's END PROGRAM belongs to the container (a statement outside every unit belongs to
/// the first). Each unit's list is what its binder declares through
/// <c>DataBinder.DeclareUserWord</c> before any other word, so the refusal of a word in two sets lands at the other declaration.
/// Empty from COBOL-2002, where the names are system-names (<see cref="Editions.EditionInfo.CopyNamesAreUserDefinedWords"/>).</summary>
internal sealed class CopyNameCensus
{
    public static CopyNameCensus None { get; } = new([]);

    private readonly Dictionary<BoundUnit, List<CopyWord>> _byUnit;

    private CopyNameCensus(Dictionary<BoundUnit, List<CopyWord>> byUnit) => _byUnit = byUnit;

    /// <summary>The census of <paramref name="units"/> (every program unit of the group, in text order): <paramref name="linesOf"/>
    /// is the RESULTANT line of a unit's first token and of its last.</summary>
    public static CopyNameCensus Build(EditionContext edition, IReadOnlyList<BoundUnit> units, Func<BoundUnit, (int First, int Last)> linesOf)
    {
        if (!edition.Edition.CopyNamesAreUserDefinedWords || edition.CopyNames.Count == 0 || units.Count == 0) return None;
        var spans = units.Select(u => (Unit: u, Lines: linesOf(u))).OrderBy(s => s.Lines.First).ToArray();
        var byUnit = new Dictionary<BoundUnit, List<CopyWord>>();
        foreach (var use in edition.CopyNames)
        {
            int line = edition.LineMap?.ResultantLineOf(use.At) is > 0 and var found ? found : use.At.Line;
            var owner = spans.LastOrDefault(s => s.Lines.First <= line && line <= s.Lines.Last);
            if (owner.Unit is null) owner = spans[0];
            var kind = use.IsLibraryName ? UserWordKind.LibraryName : UserWordKind.TextName;
            if (!byUnit.TryGetValue(owner.Unit, out var words)) byUnit[owner.Unit] = words = [];
            words.Add(new CopyWord(use.Name, kind, line));
        }
        return new CopyNameCensus(byUnit);
    }

    /// <summary>The names the COPY statements of <paramref name="unit"/> wrote, in text order.</summary>
    public IReadOnlyList<CopyWord> Of(BoundUnit unit) => _byUnit.TryGetValue(unit, out var words) ? words : [];
}
