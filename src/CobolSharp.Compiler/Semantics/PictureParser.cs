namespace CobolSharp.Compiler.Semantics;

/// <summary>
/// Parses PICTURE strings into PictureInfo. Phase 1 handles basic 9, X, A, V, S patterns.
/// </summary>
public static class PictureParser
{
    public static PictureInfo Parse(string pictureString)
    {
        string upper = pictureString.ToUpperInvariant();
        int pos = 0;

        bool signed = false;
        int integerDigits = 0;
        int decimalDigits = 0;
        int alphaCount = 0;
        int alphanumCount = 0;
        bool pastDecimalPoint = false;

        while (pos < upper.Length)
        {
            char c = upper[pos];
            pos++;

            int count = 1;
            // Check for repeat count: X(5)
            if (pos < upper.Length && upper[pos] == '(')
            {
                int closeIdx = upper.IndexOf(')', pos);
                if (closeIdx > pos + 1 && int.TryParse(upper.AsSpan(pos + 1, closeIdx - pos - 1), out int rep))
                {
                    count = rep;
                    pos = closeIdx + 1;
                }
            }

            switch (c)
            {
                case 'S':
                    signed = true;
                    break;
                case '9':
                    if (pastDecimalPoint)
                        decimalDigits += count;
                    else
                        integerDigits += count;
                    break;
                case 'V':
                    pastDecimalPoint = true;
                    break;
                case 'X':
                    alphanumCount += count;
                    break;
                case 'A':
                    alphaCount += count;
                    break;
            }
        }

        PictureCategory category;
        int size;

        if (alphanumCount > 0)
        {
            category = PictureCategory.Alphanumeric;
            size = alphanumCount;
        }
        else if (alphaCount > 0)
        {
            category = PictureCategory.Alphabetic;
            size = alphaCount;
        }
        else
        {
            category = PictureCategory.Numeric;
            size = integerDigits + decimalDigits;
        }

        return new PictureInfo(pictureString, category, size,
            integerDigits, decimalDigits, signed);
    }
}
