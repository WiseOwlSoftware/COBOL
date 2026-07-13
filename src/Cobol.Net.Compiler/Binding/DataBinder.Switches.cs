// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Common;
using CobolNet.Editions;
using CobolNet.Frontend.Generated;

using CobolNet.Binding.Model;

namespace CobolNet.Binding;

using Core = CobolParserCore;

/// <summary>
/// The SPECIAL-NAMES external-switch registry (ISO §12.3.7, version-invariant 85→2023): the switch-name clause
/// associates an implementor-defined external switch with an optional mnemonic-name (Option 1) and/or ON/OFF
/// status condition-names (either Option). The mnemonic is referenced only in SET (SR5); the condition-names are
/// interrogated as switch-status conditions (GR2 / §8.8.4.6); SET alters the status (GR3 / §14.9.39 F3).
/// </summary>
public sealed partial class DataBinder
{
    /// <summary>SPECIAL-NAMES switch mnemonic-names (case-insensitive) → the implementor switch-name they set
    /// (ISO §12.3.7 Option 1; SR5 — a mnemonic-name may be specified only in a SET statement). An Option 2 entry
    /// (no mnemonic) registers the switch-name itself, accepting <c>SET switch-name</c> — legacy-parity leniency
    /// (the conforming program cannot SET an Option 2 switch at all, so no conforming program changes meaning).</summary>
    public Dictionary<string, string> SwitchMnemonics { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Switch-status condition-names (case-insensitive) → (implementor switch-name, posited-ON) (ISO
    /// §12.3.7 GR2; §8.4.4.2 Format 1 SR1 — the condition-name shall be associated with a switch-name in
    /// SPECIAL-NAMES). Consulted by the condition binder AFTER level-88 resolution (NC211A defines a name as BOTH;
    /// the level-88 wins).</summary>
    public Dictionary<string, (string ImplementorName, bool IsOn)> SwitchConditions { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>User-defined CLASS names (case-insensitive) → the EXPANDED member-character set (ISO §12.3.7
    /// class-name clause: each literal lists its characters; a THRU pair contributes every character between the
    /// two ordinals in the NATIVE collating sequence, in either order). Consulted by the class-condition binder
    /// (§8.8.4.1.4 — true when the operand consists entirely of members).</summary>
    public Dictionary<string, string> UserClasses { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>ALPHABET names (case-insensitive) → the built collating table (ISO §12.3.7 GR7), or null for a
    /// NATIVE/STANDARD-1/STANDARD-2 alphabet (ISO/IEC 646 order IS the Latin-1 native order — identity, no table).</summary>
    public Dictionary<string, CollatingTable?> Alphabets { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The resolved PROGRAM COLLATING SEQUENCE (ISO §12.3.6 GR9–GR11), or null when none is specified /
    /// the named alphabet is the native order. Drives relation, condition-name, and (later) SORT/MERGE-key
    /// comparisons (GR11/GR13) and the runtime HIGH-/LOW-VALUE characters (§8.3.3.6 GR6/GR7).</summary>
    public CollatingTable? Collating { get; private set; }

    /// <summary>DECIMAL-POINT IS COMMA (ISO §12.3.7 GR14 — the decimal and grouping separator characters EXCHANGE
    /// functionality in numeric literals [GR14a] and PICTURE character-strings / edited insertion [GR14b]).
    /// Version-invariant 85→2023 (no VERSION_CHANGE_REFERENCE row). Set BEFORE any literal or PICTURE binds —
    /// SPECIAL-NAMES is walked at the top of <see cref="Bind"/>.</summary>
    public bool DecimalPointIsComma { get; private set; }

    /// <summary>The currency PICTURE SYMBOL (ISO §12.3.7 GR13; <c>$</c> per the SR25 implied clause). A bare
    /// <c>CURRENCY SIGN IS literal-7</c> makes literal-7 both string and symbol (SR22); the 2002+ <c>WITH PICTURE
    /// SYMBOL literal-8</c> form names the symbol separately (SR23/SR26).</summary>
    public char CurrencyPicSymbol { get; private set; } = '$';

    /// <summary>The currency STRING inserted where the symbol lands (ISO §12.3.7 GR13). Today always one
    /// character (== <see cref="CurrencyPicSymbol"/> for the bare form): a multi-character literal-7 under
    /// PICTURE SYMBOL is rejected with COBOLNET0896 — the M2-deferred multi-char-currency surface (it changes
    /// the edited item's SIZE per §13.18.40.4, which PicInfo/CobolEdit don't model yet).</summary>
    public string CurrencyString { get; private set; } = "$";

    /// <summary>Normalize a NUMERIC literal's source text to the canonical dot-decimal form the whole emit-side
    /// decode pipeline consumes (ISO §12.3.7 GR14a: under DECIMAL-POINT IS COMMA "the character written in
    /// numeric literals to represent the decimal separator shall be the comma" — and §8.3.3.3.2 admits ONLY the
    /// decimal point in a fixed-point literal, so the OTHER separator is diagnosed in each mode; the legacy's
    /// unconditional acceptance of both is a version-invariant non-conformance, not ported). The ONE literal
    /// chokepoint: expression paths route through <c>StatementBinder.CheckLiteral</c>, VALUE / level-88 capture
    /// call this directly.</summary>
    public string NormalizeNumericLiteral(string text)
    {
        if (DecimalPointIsComma)
        {
            if (text.Contains('.'))
                Edition.Error("COBOLNET0895", $"numeric literal '{text}': under DECIMAL-POINT IS COMMA the "
                    + "decimal separator is the comma (ISO §12.3.7 GR14a); '.' is not valid in a numeric literal");
            return text.Replace(',', '.');
        }
        if (text.Contains(','))
            Edition.Error("COBOLNET0895", $"numeric literal '{text}': a comma decimal separator requires "
                + "DECIMAL-POINT IS COMMA (ISO §12.3.7 GR14a; §8.3.3.3.2 admits only '.' as the decimal point)");
        return text.Replace(',', '.');   // diagnosed; normalized so downstream decode stays well-formed
    }

    /// <summary>Normalize <paramref name="text"/> when it is a numeric literal (digits with optional sign and
    /// separators); any other operand text (quoted literal, figurative word, data-name) passes through. Used by
    /// the VALUE-clause and level-88 capture paths, whose operand texts are not yet classified.</summary>
    internal string NormalizeIfNumericLiteral(string text)
    {
        bool anyDigit = false;
        foreach (char c in text)
        {
            if (char.IsAsciiDigit(c)) { anyDigit = true; continue; }
            if (c is not ('+' or '-' or '.' or ',')) return text;
        }
        return anyDigit ? NormalizeNumericLiteral(text) : text;
    }

    /// <summary>Bind <c>DECIMAL-POINT IS COMMA</c> (ISO §12.3.7 — the only word the format admits after IS is
    /// COMMA; the grammar carries it as a generic IDENTIFIER).</summary>
    private void SwitchBindDecimalPoint(Core.DecimalPointClauseContext dp)
    {
        if (dp.IDENTIFIER()?.GetText() is { } word && word.Equals("COMMA", StringComparison.OrdinalIgnoreCase))
            DecimalPointIsComma = true;
        else
            Edition.Error("COBOLNET0894", $"DECIMAL-POINT IS {dp.IDENTIFIER()?.GetText()}: the only form is "
                + "DECIMAL-POINT IS COMMA (ISO §12.3.7)");
    }

    /// <summary>Bind <c>CURRENCY [SIGN] [IS] literal-7 [WITH PICTURE SYMBOL literal-8]</c> (ISO §12.3.7).
    /// Bare form: literal-7 is both currency string and symbol — a single character outside the SR22 forbidden
    /// set. PICTURE SYMBOL form (2002+ — the 85 standard had only the single-character form): literal-7 is the
    /// currency string (SR23), literal-8 the symbol (SR26/SR27). SR21: two clauses may not give one symbol two
    /// different strings.</summary>
    private void SwitchBindCurrency(Core.CurrencySignClauseContext cur)
    {
        var lits = cur.literal();
        if (lits.Length == 0) return;
        string literal7 = LiteralChars(lits[0]);

        char symbol;
        string currencyString;
        if (cur.PIC() is not null)
        {
            // currency-picture-symbol-2002 (§12.3.7): the pass owns the edition gate (Exec Step E).
            // PICMODE exploit: the word between PICTURE and literal-8 arrives as a PIC_STRING token — it must be
            // the keyword SYMBOL (the grammar cannot distinguish; semantic check per the grammar's own note).
            if (cur.PIC_STRING()?.GetText() is { } sym && !sym.Equals("SYMBOL", StringComparison.OrdinalIgnoreCase))
                Edition.Error("COBOLNET0892", $"CURRENCY SIGN: expected 'WITH PICTURE SYMBOL', found 'PICTURE {sym}' (ISO §12.3.7)");
            string literal8 = lits.Length > 1 ? LiteralChars(lits[1]) : "";
            if (literal8.Length != 1)
            {
                Edition.Error("COBOLNET0892", "CURRENCY SIGN: the PICTURE SYMBOL literal shall be a single "
                    + "character (ISO §12.3.7 SR26)");
                return;
            }
            if (literal7.Trim().Length == 0)
                Edition.Error("COBOLNET0890", "CURRENCY SIGN: the currency string shall contain at least one "
                    + "non-space character (ISO §12.3.7 SR23a)");
            if (literal7.Length != 1)
            {
                // The multi-character currency STRING changes the edited item's size (§13.18.40.4) — the
                // M2-deferred surface; reject loudly rather than mis-size (docs/MULTIVERSION_ROADMAP M2 catalog).
                Edition.Error("COBOLNET0896", $"CURRENCY SIGN \"{literal7}\": a multi-character currency string "
                    + "is not yet supported (single-character strings only; ISO §12.3.7 SR23)");
                return;
            }
            symbol = literal8[0];
            currencyString = literal7;
            ValidateCurrencyChar(symbol, "PICTURE SYMBOL literal");   // SR27 — same forbidden set as SR22
        }
        else
        {
            // Bare form (the COBOL-85 surface): SR22 — one character, both string and symbol.
            if (literal7.Length != 1)
            {
                Edition.Error("COBOLNET0890", $"CURRENCY SIGN \"{literal7}\": without PICTURE SYMBOL the literal "
                    + "shall consist of a single character (ISO §12.3.7 SR22)");
                return;
            }
            symbol = literal7[0];
            currencyString = literal7;
            ValidateCurrencyChar(symbol, "CURRENCY SIGN literal");
        }

        // SR21: no two clauses may bind equivalent symbols to different strings (single-clause programs trivially pass).
        if (CurrencyPicSymbol != '$' && char.ToUpperInvariant(CurrencyPicSymbol) == char.ToUpperInvariant(symbol)
            && CurrencyString != currencyString)
            Edition.Error("COBOLNET0891", $"CURRENCY SIGN: the symbol '{symbol}' is already bound to "
                + $"\"{CurrencyString}\" (ISO §12.3.7 SR21)");
        CurrencyPicSymbol = symbol;
        CurrencyString = currencyString;
    }

    /// <summary>The ISO §12.3.7 SR22/SR27 forbidden currency-symbol set: digits; A B C D E N P R S V X Z (either
    /// case) or space; and <c>+ - , . * / ; ( ) " =</c>.</summary>
    private void ValidateCurrencyChar(char c, string what)
    {
        bool forbidden = char.IsAsciiDigit(c)
            || char.ToUpperInvariant(c) is 'A' or 'B' or 'C' or 'D' or 'E' or 'N' or 'P' or 'R' or 'S' or 'V' or 'X' or 'Z'
            || c is ' ' or '+' or '-' or ',' or '.' or '*' or '/' or ';' or '(' or ')' or '"' or '=';
        if (forbidden)
            Edition.Error("COBOLNET0891", $"{what} '{c}': not a valid currency symbol — digits, the picture "
                + "letters A B C D E N P R S V X Z, space, and + - , . * / ; ( ) \" = are excluded (ISO §12.3.7 SR22/SR27)");
    }

    /// <summary>Populate the switch registry from the SPECIAL-NAMES paragraph's switch-name clauses (ISO §12.3.7
    /// general format: <c>switch-name-1 [IS mnemonic-name-1] [ON [STATUS] [IS] condition-name-1]
    /// [OFF [STATUS] [IS] condition-name-2]</c>; the NIST-85 surface also writes <c>ON IS cond</c> with no STATUS —
    /// both shapes are one grammar rule). Any switch-name is accepted (SR8 — the available names are
    /// implementor-specified; see <c>ExternalSwitches</c> for the documented item-191 contract).</summary>
    private void SwitchBindSpecialNames(Core.ProgramUnitContext program)
    {
        var cfg = program.environmentDivision()?.configurationSection();
        if (cfg is null) return;
        string? pcsName = null;
        foreach (var para in cfg.configurationParagraph())
        {
            // OBJECT-COMPUTER … PROGRAM COLLATING SEQUENCE IS alphabet-name (ISO §12.3.6 — the 85 single-name form).
            if (para.objectComputerParagraph()?.programCollatingSequenceClause() is { } pcs)
                pcsName = pcs.cobolWord().GetText();
            if (para.specialNamesParagraph() is not { } sn) continue;
            foreach (var entry in sn.specialNameEntry())
            {
                if (entry.alphabetClause() is { } alpha) { AlphabetBind(alpha); continue; }
                if (entry.classDefinitionClause() is { } cd) { SwitchBindClass(cd); continue; }
                if (entry.symbolicCharactersClause() is { } sc)
                {
                    // SYMBOLIC CHARACTERS … FOR ALPHANUMERIC/NATIONAL — the FOR phrase edition gate is now
                    // VersionConformancePass ParseArm.VisitSymbolicCharactersClause (14g.4). The base SYMBOLIC
                    // CHARACTERS clause stays accepted-inert (unbound).
                    continue;
                }
                if (entry.decimalPointClause() is { } dp) { SwitchBindDecimalPoint(dp); continue; }
                if (entry.currencySignClause() is { } cur) { SwitchBindCurrency(cur); continue; }
                if (entry.implementorSwitchEntry() is not { } sw) continue;
                var ids = sw.cobolWord();   // [0] = switch-name; [1] = mnemonic-name when Option 1
                if (ids.Length == 0) continue;
                string? onName = sw.switchOnClause()?.cobolWord()?.GetText();
                string? offName = sw.switchOffClause()?.cobolWord()?.GetText();
                // Only a genuine switch clause registers: a mnemonic (Option 1) or ≥1 status condition (Option 2)
                // — the §12.3.7 format requires at least one of the three phrases.
                if (ids.Length < 2 && onName is null && offName is null) continue;

                string implName = ids[0].GetText();
                SwitchMnemonics.TryAdd(ids.Length >= 2 ? ids[1].GetText() : implName, implName);
                if (onName is not null) SwitchConditions.TryAdd(onName, (implName, true));
                if (offName is not null) SwitchConditions.TryAdd(offName, (implName, false));
            }
        }
        // The PCS resolves AFTER the walk (OBJECT-COMPUTER precedes SPECIAL-NAMES in source, §12.3.6 GR9); only
        // the NAMED alphabet becomes the program sequence — other defined alphabets have no effect (NC219A's
        // unreferenced COLLATING-SEQ-2). A native-order alphabet leaves Collating null (the fast path).
        if (pcsName is not null && Alphabets.TryGetValue(pcsName, out var pcsTable))
            Collating = pcsTable;
    }

    /// <summary>Build one <c>ALPHABET name IS …</c> clause (ISO §12.3.7 GR7): NATIVE / STANDARD-1 / STANDARD-2 are
    /// the native (ISO/IEC 646) order — no table; a literal phrase assigns successive ascending positions per
    /// k)1–k)6 — a numeric literal is the 1-based NATIVE ordinal (k1a), a multi-character literal positions each
    /// character leftmost-first (k1b), THRU expands the native run in EITHER direction (k5), ALSO members share
    /// ONE position (k6), and every unspecified character takes a DISTINCT ascending position above the highest
    /// specified, in native relative order (k3). HIGH-/LOW-VALUE written INSIDE the clause are the NATIVE extremes
    /// (GR10 — the PCS re-derivation applies only outside SPECIAL-NAMES).</summary>
    private void AlphabetBind(Core.AlphabetClauseContext alpha)
    {
        // ALPHABET … FOR ALPHANUMERIC/NATIONAL — the FOR phrase edition gate is now VersionConformancePass
        // ParseArm.VisitAlphabetClause (14g.4, recognition).
        string name = alpha.cobolWord().GetText();
        var def = alpha.alphabetDefinition();
        if (def.NATIVE() is not null || def.STANDARD_1() is not null || def.STANDARD_2() is not null)
        {
            Alphabets.TryAdd(name, null);
            return;
        }

        var pos = new ushort[256];
        Array.Fill(pos, ushort.MaxValue);                  // sentinel: not yet specified
        var specOrder = new List<char>();                  // every specified char in source order (tie rules)
        ushort next = 0;
        void Assign(char c, bool advance)
        {
            int code = c & 0xFF;
            if (pos[code] != ushort.MaxValue) return;      // SR14a duplicate — first wins (diagnostic later)
            pos[code] = next;
            specOrder.Add((char)code);
            if (advance) next++;
        }

        foreach (var entry in def.alphabetEntry())
        {
            var operands = AlphabetOperands(entry);
            if (operands.Count == 0) continue;
            if (entry.THRU() is not null || entry.THROUGH() is not null)
            {
                // k)5: the native run from operand-1 to operand-2, either direction, ascending positions.
                if (operands.Count >= 2 && operands[0].Length == 1 && operands[1].Length == 1)
                {
                    int a = operands[0][0] & 0xFF, b = operands[1][0] & 0xFF, step = a <= b ? 1 : -1;
                    for (int c = a; ; c += step) { Assign((char)c, advance: true); if (c == b) break; }
                }
                continue;
            }
            if (entry.ALSO().Length > 0)
            {
                // k)6: operand-1 and every ALSO operand share ONE ordinal position; operand-1 is the position's
                // first character (the CHAR() pick and the LOW-VALUE tie winner).
                foreach (var op in operands)
                    if (op.Length == 1) Assign(op[0], advance: false);
                next++;
                continue;
            }
            // k)1.b: a (possibly multi-character) literal — each character, leftmost first, ascending positions.
            foreach (char c in operands[0]) Assign(c, advance: true);
        }

        // k)3: unspecified characters follow, DISTINCT ascending positions in native relative order.
        for (int code = 0; code < 256; code++)
            if (pos[code] == ushort.MaxValue) pos[code] = next++;

        // GR8/GR9 extremes: highest/lowest POSITION; ties (an ALSO group) take the last/first char SPECIFIED.
        ushort maxPos = 0, minPos = ushort.MaxValue;
        for (int code = 0; code < 256; code++) { if (pos[code] > maxPos) maxPos = pos[code]; if (pos[code] < minPos) minPos = pos[code]; }
        char high = '\u00ff', low = '\u0000';
        for (int code = 255; code >= 0; code--) if (pos[code] == maxPos) { high = (char)code; break; }
        foreach (char c in specOrder) if (pos[c & 0xFF] == maxPos) high = c;                  // tie → LAST specified
        for (int code = 0; code < 256; code++) if (pos[code] == minPos) { low = (char)code; break; }
        foreach (char c in specOrder) if (pos[c & 0xFF] == minPos) { low = c; break; }        // tie → FIRST specified

        Alphabets.TryAdd(name, new CollatingTable(pos, high, low));
    }

    /// <summary>An alphabet entry's operand texts in source order: quoted literals decoded, an unsigned integer
    /// literal as the character at that 1-based NATIVE ordinal (GR7 k1a), and the figurative words written inside
    /// SPECIAL-NAMES as the NATIVE extremes/values (GR10 — HIGH-VALUE=U+00FF, LOW-VALUE=U+0000, SPACE, QUOTE,
    /// ZERO).</summary>
    private static List<string> AlphabetOperands(Core.AlphabetEntryContext entry)
    {
        var result = new List<string>();
        for (int i = 0; i < entry.ChildCount; i++)
        {
            switch (entry.GetChild(i))
            {
                case Core.LiteralContext lit:
                    result.Add(LiteralChars(lit));
                    break;
                case Core.CobolWordContext w:
                    string t = w.GetText().ToUpperInvariant();
                    result.Add(t switch
                    {
                        "HIGH-VALUE" or "HIGH-VALUES" => "\u00ff",
                        "LOW-VALUE" or "LOW-VALUES" => "\u0000",
                        "SPACE" or "SPACES" => " ",
                        "QUOTE" or "QUOTES" => "\"",
                        "ZERO" or "ZEROS" or "ZEROES" => "0",
                        _ => t,
                    });
                    break;
            }
        }
        return result;
    }

    /// <summary>One <c>CLASS class-name IS {literal [THRU literal]}…</c> clause (ISO §12.3.7): expand each value
    /// item to its member characters — a multi-character literal lists each character; a THRU pair contributes the
    /// contiguous native-collating range between the two single-character ordinals, ASCENDING OR DESCENDING (the
    /// clause's GR allows either order — NC174A's <c>"D" THROUGH "A"</c> equals <c>"A" THRU "D"</c>).</summary>
    private void SwitchBindClass(Core.ClassDefinitionClauseContext cd)
    {
        // CLASS … FOR ALPHANUMERIC/NATIONAL — the FOR phrase edition gate is now VersionConformancePass
        // ParseArm.VisitClassDefinitionClause (14g.4, recognition).
        string name = cd.cobolWord(0).GetText();
        var members = new System.Text.StringBuilder();
        foreach (var item in cd.classValueSet().classValueItem())
        {
            var lits = item.literal();
            string lo = LiteralChars(lits[0]);
            if (lits.Length >= 2)
            {
                string hi = LiteralChars(lits[1]);
                if (lo.Length == 1 && hi.Length == 1)
                {
                    char a = lo[0], b = hi[0];
                    if (a > b) (a, b) = (b, a);
                    for (char c = a; c <= b; c++) members.Append(c);
                    continue;
                }
            }
            members.Append(lo);
        }
        UserClasses.TryAdd(name, members.ToString());
    }

    /// <summary>The character content of a class-definition literal: a quoted literal's characters, or — for an
    /// unsigned integer literal — the character at that ORDINAL position of the native collating sequence
    /// (1-based, ISO §12.3.7; ordinal n ⇒ char code n−1 over the 8-bit native sequence).</summary>
    private static string LiteralChars(Core.LiteralContext lit)
    {
        string text = lit.GetText();
        if (CobolLiteral.IsStringLiteral(text))   // both ISO §8.3.1.2 delimiters (an apostrophe CLASS literal was miscompiled)
            return CobolLiteral.Decode(text);
        return int.TryParse(text, out int ordinal) && ordinal >= 1 && ordinal <= 256
            ? ((char)(ordinal - 1)).ToString()
            : text;
    }
}
