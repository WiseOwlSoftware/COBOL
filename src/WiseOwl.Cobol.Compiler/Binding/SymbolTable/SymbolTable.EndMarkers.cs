// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;

namespace WiseOwl.Cobol.Binding;

public sealed partial class SymbolTable
{
    /// <summary>Does an end marker's name repeat the name its source unit's header declares (§2023:10.7.3 SR1 to SR6: END PROGRAM,
    /// END FUNCTION, END CLASS, END INTERFACE and END METHOD)? A question of the parse tree, asked before any element's table exists
    /// (as <see cref="IdentifyConstantBeforeBind"/> is), and answered with the table's one name equality: two written words, both
    /// declarations, compared here so that no pass beside the table compares COBOL names (docs/rearchitecture/DESIGN-listing.md
    /// section 6.7.1, "Equality of COBOL names is private too").</summary>
    internal static bool EndMarkerAgrees(ParserRuleContext endName, ParserRuleContext declared) =>
        Key(endName.GetText()) == Key(declared.GetText());

    /// <summary>Does an END PROGRAM or END FUNCTION marker name one of <paramref name="programs"/> (the program-names of the
    /// source units of one nesting chain: the containers still open, or the programs the unit contains)? The marker then stands
    /// where another unit's belongs (§1985:X.2.5.3 SR3, the markers out of order).</summary>
    internal static bool EndMarkerNamesAny(ParserRuleContext endName, IEnumerable<ParserRuleContext> programs)
    {
        var key = Key(endName.GetText());
        return programs.Any(p => Key(p.GetText()) == key);
    }
}
