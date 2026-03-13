namespace CobolSharp.Compiler.Semantics;

/// <summary>
/// Parses COBOL PICTURE strings into PictureInfo.
/// Handles all standard PICTURE symbols per ISO/IEC 1989:2023 §13.
/// </summary>
public static class PictureParser
{
    public static PictureInfo Parse(string pictureString)
    {
        string upper = pictureString.ToUpperInvariant();
        var symbols = ExpandSymbols(upper);

        bool signed = false;
        int integerDigits = 0;
        int decimalDigits = 0;
        int alphaCount = 0;
        int alphanumCount = 0;
        int displaySize = 0;
        bool pastDecimalPoint = false;
        bool hasEditingSymbols = false;
        bool hasNumericSymbols = false;
        bool hasAlphaSymbols = false;
        bool hasAlphanumSymbols = false;

        foreach (var sym in symbols)
        {
            switch (sym.Kind)
            {
                case PictureSymbolKind.Sign:
                    signed = true;
                    // S does not occupy a display position in USAGE DISPLAY
                    break;

                case PictureSymbolKind.Nine:
                    hasNumericSymbols = true;
                    if (pastDecimalPoint)
                        decimalDigits += sym.Count;
                    else
                        integerDigits += sym.Count;
                    displaySize += sym.Count;
                    break;

                case PictureSymbolKind.Decimal:
                    pastDecimalPoint = true;
                    // V does not occupy a display position
                    break;

                case PictureSymbolKind.P:
                    // P represents scaling — counts as a digit but no display position
                    if (pastDecimalPoint)
                        decimalDigits += sym.Count;
                    else
                        integerDigits += sym.Count;
                    break;

                case PictureSymbolKind.X:
                    hasAlphanumSymbols = true;
                    alphanumCount += sym.Count;
                    displaySize += sym.Count;
                    break;

                case PictureSymbolKind.A:
                    hasAlphaSymbols = true;
                    alphaCount += sym.Count;
                    displaySize += sym.Count;
                    break;

                // Numeric editing symbols — each occupies one display position
                case PictureSymbolKind.Z:
                    hasEditingSymbols = true;
                    hasNumericSymbols = true;
                    if (pastDecimalPoint)
                        decimalDigits += sym.Count;
                    else
                        integerDigits += sym.Count;
                    displaySize += sym.Count;
                    break;

                case PictureSymbolKind.Star:
                    hasEditingSymbols = true;
                    hasNumericSymbols = true;
                    if (pastDecimalPoint)
                        decimalDigits += sym.Count;
                    else
                        integerDigits += sym.Count;
                    displaySize += sym.Count;
                    break;

                case PictureSymbolKind.Plus:
                case PictureSymbolKind.Minus:
                    hasEditingSymbols = true;
                    hasNumericSymbols = true;
                    signed = true;
                    // Floating +/- : first one is a sign position, rest are digit positions
                    if (sym.Count > 1)
                    {
                        if (pastDecimalPoint)
                            decimalDigits += sym.Count - 1;
                        else
                            integerDigits += sym.Count - 1;
                    }
                    displaySize += sym.Count;
                    break;

                case PictureSymbolKind.Currency:
                    hasEditingSymbols = true;
                    hasNumericSymbols = true;
                    // Floating $ : first one is currency, rest are digit positions
                    if (sym.Count > 1)
                    {
                        if (pastDecimalPoint)
                            decimalDigits += sym.Count - 1;
                        else
                            integerDigits += sym.Count - 1;
                    }
                    displaySize += sym.Count;
                    break;

                case PictureSymbolKind.CR:
                case PictureSymbolKind.DB:
                    hasEditingSymbols = true;
                    signed = true;
                    displaySize += 2; // CR/DB occupy 2 positions
                    break;

                case PictureSymbolKind.Period:
                    hasEditingSymbols = true;
                    pastDecimalPoint = true;
                    displaySize += 1;
                    break;

                case PictureSymbolKind.Comma:
                case PictureSymbolKind.Slash:
                case PictureSymbolKind.Zero:
                    hasEditingSymbols = true;
                    displaySize += sym.Count;
                    break;

                case PictureSymbolKind.B:
                    hasEditingSymbols = true;
                    displaySize += sym.Count;
                    break;
            }
        }

        // Determine category
        PictureCategory category;
        if (hasAlphanumSymbols && hasEditingSymbols)
        {
            category = PictureCategory.AlphanumericEdited;
        }
        else if (hasAlphanumSymbols)
        {
            category = PictureCategory.Alphanumeric;
        }
        else if (hasAlphaSymbols && !hasNumericSymbols)
        {
            category = PictureCategory.Alphabetic;
        }
        else if (hasEditingSymbols)
        {
            category = PictureCategory.NumericEdited;
        }
        else
        {
            category = PictureCategory.Numeric;
        }

        return new PictureInfo(pictureString, category, displaySize,
            integerDigits, decimalDigits, signed, hasEditingSymbols, symbols);
    }

    /// <summary>
    /// Expands a PICTURE string into a list of symbols with counts.
    /// Handles repeat notation like 9(5), multi-character symbols like CR/DB.
    /// </summary>
    private static List<PictureSymbol> ExpandSymbols(string upper)
    {
        var symbols = new List<PictureSymbol>();
        int pos = 0;

        while (pos < upper.Length)
        {
            char c = upper[pos];
            pos++;

            // Check for multi-character symbols: CR, DB
            if (c == 'C' && pos < upper.Length && upper[pos] == 'R')
            {
                pos++;
                symbols.Add(new PictureSymbol(PictureSymbolKind.CR));
                continue;
            }
            if (c == 'D' && pos < upper.Length && upper[pos] == 'B')
            {
                pos++;
                symbols.Add(new PictureSymbol(PictureSymbolKind.DB));
                continue;
            }

            var kind = CharToSymbolKind(c);
            if (kind == null) continue; // skip unrecognized

            // Check for repeat count: X(5)
            int count = 1;
            if (pos < upper.Length && upper[pos] == '(')
            {
                int closeIdx = upper.IndexOf(')', pos);
                if (closeIdx > pos + 1 &&
                    int.TryParse(upper.AsSpan(pos + 1, closeIdx - pos - 1), out int rep))
                {
                    count = rep;
                    pos = closeIdx + 1;
                }
            }

            symbols.Add(new PictureSymbol(kind.Value, count));
        }

        return symbols;
    }

    private static PictureSymbolKind? CharToSymbolKind(char c) => c switch
    {
        '9' => PictureSymbolKind.Nine,
        'S' => PictureSymbolKind.Sign,
        'V' => PictureSymbolKind.Decimal,
        'P' => PictureSymbolKind.P,
        'X' => PictureSymbolKind.X,
        'A' => PictureSymbolKind.A,
        'Z' => PictureSymbolKind.Z,
        '*' => PictureSymbolKind.Star,
        '+' => PictureSymbolKind.Plus,
        '-' => PictureSymbolKind.Minus,
        '$' => PictureSymbolKind.Currency,
        ',' => PictureSymbolKind.Comma,
        '.' => PictureSymbolKind.Period,
        '0' => PictureSymbolKind.Zero,
        '/' => PictureSymbolKind.Slash,
        'B' => PictureSymbolKind.B,
        _ => null
    };
}
