// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime;

/// <summary>⛔ <b>ISO §14.6.10 1) — AN OVERLAPPING MOVE'S RECEIVER IS WRITTEN WHILE ITS SENDER IS STILL BEING READ —
/// WHAT WISEOWL COBOL DOES, WRITTEN ONCE</b> (kb/Work PB1910, PB1907; docs/CONFORMANCE.md §3 D-OVL2).
/// <para>"When the data items are not described by the same data description entry, the result of the statement is
/// undefined" (§14.6.10 1); Annex A.2 item 36), so the result is the implementor's, and CLAUDE.md rule 1 settles it:
/// follow GnuCOBOL. GnuCOBOL's <c>libcob</c> converts a numeric DISPLAY or alphanumeric sender into a numeric DISPLAY or
/// PACKED-DECIMAL receiver IN PLACE: it clears the receiver, then writes it one digit (DISPLAY) or one digit pair
/// (PACKED-DECIMAL), left to right, taking each from the sender's bytes AS THE STORAGE THEN HOLDS THEM — so a position
/// the receiver has already written is read back as the receiver's content, not the sender's. The result is not
/// "the receiver is zero, then the sender is converted" (that is right only where the receiver lies at or to the
/// left of the sender's digits): <c>MOVE N6 TO N4</c>, <c>N6 PIC 9(6)</c> holding 123456 and <c>N4 PIC 9(4)</c>
/// over its positions 4 to 7, gives <c>3333</c>, because each digit written into N4 is the next one read.</para>
/// <para>Each method is that algorithm and nothing else, operating on a character arena that holds the two
/// operands' storage images laid over each other at their static offset (<c>receiverAt</c>, the receiver's first
/// byte minus the sender's). It is reached ONLY where the compiler proved the overlap (<c>MoveOverlap.Classify</c>),
/// so the ordinary MOVE, which cannot overlap, never calls it. The sender's operational sign is read before the
/// receiver is cleared and written back after the receiver is stored, exactly as <c>libcob</c> does, so a receiver
/// that covers the sender's sign byte ends with the sign punched onto whatever that byte then holds.</para>
/// <para>NOT adopted, with the reasons, in docs/CONFORMANCE.md D-OVL2: numeric-edited receivers (the editing routine
/// interleaves its picture scan with reads of the sender and back-patches earlier positions) and PACKED-DECIMAL
/// senders (the library's byte copy there is <c>memcpy</c> over overlapping storage, whose result depends on the C
/// library's implementation, not on a rule).</para></summary>
public static class OverlappingMove
{
    /// <summary>A numeric DISPLAY sender into a PACKED-DECIMAL receiver (<c>cob_move_display_to_packed</c>).</summary>
    /// <param name="sender">The sender's storage image (its zoned digits and, for SIGN SEPARATE, its sign character).</param>
    /// <param name="receiver">The receiver's storage image (its packed bytes).</param>
    /// <param name="receiverAt">The receiver's first byte minus the sender's first byte.</param>
    /// <returns>The receiver's storage image after the store.</returns>
    public static string DisplayToPacked(string sender, string receiver, int receiverAt, in NumProfile from, in NumProfile to)
    {
        var (m, s0, r0) = Lay(sender, receiver, receiverAt);
        var s = new Zoned(s0, from);
        int sign = SignOf(m, s, adjust: true);
        int digits1 = from.Digits;   // the stored digit positions: a P position stores nothing, it is in the scale alone
        int digits2 = to.Digits;
        bool noSignNibble = to.ByteForm == NumericByteForm.PackedNoSign;

        // The receiver digit position the first transferred digit lands on, and the first sender digit that fits.
        uint i = noSignNibble ? (uint)(digits2 % 2) : (uint)(1 - digits2 % 2);
        int p = s.DataAt + (digits1 - from.FractionDigits) - (digits2 - to.FractionDigits);
        while (p < s.DataAt) { p++; i++; }

        Array.Clear(m, r0, receiver.Length);   // the receiver is cleared BEFORE the sender is read
        int q = r0 + (int)(i / 2);
        uint iEnd = (uint)receiver.Length;
        int pEnd = s.DataAt + digits1;
        if (i % 2 == 1) { m[q++] = (char)Digit(m[p++]); i++; }
        i /= 2;
        // "i" now counts receiver BYTES. The first loop is bounded by the receiver, the second by the sender; the
        // comparison is unsigned, as the library's is, so a sender digit run that ends before p reads as "huge".
        if (iEnd - i < (uint)(pEnd - p + 1) / 2)
        {
            while (i < iEnd) { m[q] = (char)(((m[p] << 4) & 0xFF) + Digit(m[p + 1])); q++; i++; p += 2; }
        }
        else
        {
            while (p < pEnd) { m[q] = (char)(((m[p] << 4) & 0xFF) + Digit(m[p + 1])); q++; p += 2; }
        }
        // A pair that took one digit past the sender's end leaves that digit's nibble in the last byte: clear it.
        if (p > pEnd) m[q - 1] = (char)(m[q - 1] & 0xF0);

        PutSignAdjusted(m, s, sign);
        if (!noSignNibble)
        {
            int last = r0 + receiver.Length - 1;
            m[last] = !to.Signed ? (char)(m[last] | 0x0F)
                : (char)((m[last] & 0xF0) | (sign < 0 ? 0x0D : 0x0C));
        }
        return new string(m, r0, receiver.Length);
    }

    /// <summary>A numeric DISPLAY sender into a numeric DISPLAY receiver of a DIFFERENT description
    /// (<c>cob_move_display_to_display</c>; identical descriptions are copied byte for byte and never reach here).</summary>
    public static string DisplayToDisplay(string sender, string receiver, int receiverAt, in NumProfile from, in NumProfile to)
    {
        var (m, s0, r0) = Lay(sender, receiver, receiverAt);
        var s = new Zoned(s0, from);
        var r = new Zoned(r0, to);
        int sign = SignOf(m, s, adjust: false);
        StoreCommonRegion(m, r, m, s.DataAt, from.Digits, from.FractionDigits, verified: false);
        PutSign(m, s, sign);
        PutSign(m, r, sign);
        return new string(m, r0, receiver.Length);
    }

    /// <summary>A PACKED-DECIMAL sender into a numeric DISPLAY receiver (<c>cob_move_packed_to_display</c>). The
    /// sender is unpacked to a private buffer before anything is written, so its DIGITS are a snapshot; its SIGN is
    /// read from its last byte AFTER the receiver is stored, so a receiver that covers that byte supplies a sign
    /// nibble of its own (and a sender that was negative reads positive).</summary>
    public static string PackedToDisplay(string sender, string receiver, int receiverAt, in NumProfile from, in NumProfile to)
    {
        var (m, s0, r0) = Lay(sender, receiver, receiverAt);
        var r = new Zoned(r0, to);
        bool noSignNibble = from.ByteForm == NumericByteForm.PackedNoSign;
        int scale = from.FractionDigits;
        int digits = from.Digits;   // stored digit positions; the P positions are in the scale alone
        int d = s0, dEnd = s0 + sender.Length - 1;
        var buff = new List<char>();

        // The leading all-zero bytes are skipped (their digits are dropped from the buffer and from "digits"), then
        // every remaining nibble is emitted as the character '0' + nibble; the final nibble is the sign.
        int lastData = noSignNibble ? dEnd : dEnd - 1;   // the last byte whose BOTH nibbles are digits
        if (noSignNibble ? digits % 2 == 1 : digits % 2 == 0)
        {
            // The first byte holds ONE digit (its low nibble): an odd digit count with no sign nibble, or an even one
            // with a sign nibble (PACKED-DECIMAL pads an even count with a leading zero nibble).
            int start = m[d++] & 0x0F;
            if (start != 0) buff.Add((char)('0' + start));
            else
            {
                digits -= 1;
                while (d <= lastData && m[d] == 0) { digits -= 2; d++; }
            }
        }
        else
        {
            while (d <= lastData && m[d] == 0) { digits -= 2; d++; }
        }
        for (; d <= lastData; d++)
        {
            buff.Add((char)('0' + (m[d] >> 4)));
            buff.Add((char)('0' + (m[d] & 0x0F)));
        }
        if (!noSignNibble) buff.Add((char)('0' + (m[dEnd] >> 4)));   // the last byte's digit nibble

        StoreCommonRegion(m, r, buff.ToArray(), 0, digits, scale, verified: true);
        PutSign(m, r, noSignNibble ? 0 : (m[dEnd] & 0x0F) == 0x0D ? -1 : 1);   // the LIVE last byte of the sender
        return new string(m, r0, receiver.Length);
    }

    /// <summary>The library's common-region copy (<c>store_common_region</c>): the receiver is cleared to '0', then
    /// the source digits that fall inside both operands' scales are copied left to right. With
    /// <paramref name="verified"/> false (a DISPLAY sender) a blank or NUL source position leaves the '0' that was
    /// set and every other byte is copied as it is — read from the arena AS IT NOW STANDS, which is the point.</summary>
    private static void StoreCommonRegion(char[] m, in Zoned r, char[] source, int sourceAt, int sourceSize, int sourceScale,
        bool verified)
    {
        int lf1 = -sourceScale, lf2 = -r.P.FractionDigits;
        int lcf = Math.Max(lf1, lf2);
        int hf1 = sourceSize + lf1, hf2 = r.P.Digits + lf2;
        int gcf = Math.Min(hf1, hf2);
        Array.Fill(m, '0', r.DataAt, r.P.Digits);
        if (gcf <= lcf) return;
        int src = sourceAt + hf1 - gcf;
        int dst = r.DataAt + hf2 - gcf;
        for (int end = dst + gcf - lcf; dst < end; dst++, src++)
        {
            char c = source[src];
            if (verified || (c != ' ' && c != '\0')) m[dst] = c;
        }
    }

    /// <summary>An alphanumeric sender (an alphanumeric or alphabetic item, or a reference-modified slice) into a
    /// numeric DISPLAY receiver (<c>cob_move_alphanum_to_display</c>): the receiver is cleared, then the sender is
    /// scanned for an optional sign, digits and a decimal point and its digits are stored right-aligned on the
    /// receiver's decimal point.</summary>
    public static string AlphanumericToDisplay(string sender, string receiver, int receiverAt, in NumProfile to,
        bool decimalPointIsComma)
    {
        var (m, s0, r0) = Lay(sender, receiver, receiverAt);
        var r = new Zoned(r0, to);
        char decPt = decimalPointIsComma ? ',' : '.';
        char numSep = decimalPointIsComma ? '.' : ',';
        int s1 = s0, e1 = s0 + sender.Length;
        int s2 = r.DataAt, e2 = r.DataAt + to.Digits;

        Array.Fill(m, '0', r0, receiver.Length);   // the whole receiver, a separate sign position included

        while (s1 < e1 && IsSpace(m[s1])) s1++;
        int sign = 0;
        if (s1 != e1 && (m[s1] == '+' || m[s1] == '-')) sign = m[s1++] == '+' ? 1 : -1;

        int count = 0;
        for (int p = s1; p < e1 && m[p] != decPt; p++)
            if (m[p] >= '0' && m[p] <= '9') count++;

        int size = to.Digits - to.FractionDigits;
        if (count < size) s2 += size - count;
        else
            while (count-- > size)
            {
                while (m[s1] < '0' || m[s1] > '9') s1++;
                s1++;
            }

        int points = 0;
        for (; s1 < e1 && s2 < e2; s1++)
        {
            char c = m[s1];
            if (c >= '0' && c <= '9') m[s2++] = c;
            else if (c == decPt) { if (points++ > 0) goto Invalid; }
            else if (!(IsSpace(c) || c == numSep)) goto Invalid;
        }
        PutSign(m, r, sign);
        return new string(m, r0, receiver.Length);

    Invalid:   // anything but digits, white space, one decimal point and the group separator: zero, unsigned
        Array.Fill(m, '0', r0, receiver.Length);
        PutSign(m, r, 0);
        return new string(m, r0, receiver.Length);
    }

    // ---- the arena ------------------------------------------------------------------------------------------------

    /// <summary>Both storage images laid over each other (they agree where they overlap), with two bytes of slack past
    /// the end — the library reads the byte after the last sender digit when it pairs digits.</summary>
    private static (char[] M, int SenderAt, int ReceiverAt) Lay(string sender, string receiver, int receiverAt)
    {
        int lo = Math.Min(0, receiverAt);
        int hi = Math.Max(sender.Length, receiverAt + receiver.Length);
        var m = new char[hi - lo + 2];
        sender.CopyTo(0, m, -lo, sender.Length);
        receiver.CopyTo(0, m, receiverAt - lo, receiver.Length);
        return (m, -lo, receiverAt - lo);
    }

    /// <summary>The low nibble of a stored byte: <c>COB_D2I</c>, how the library reads a digit position.</summary>
    private static int Digit(char c) => c & 0x0F;

    private static bool IsSpace(char c) => c is ' ' or '\t' or '\n' or '\v' or '\f' or '\r';

    private static bool IsDigit(char c) => c >= '0' && c <= '9';

    // ---- a zoned (USAGE DISPLAY) operand's geometry and sign ------------------------------------------------------

    /// <summary>A numeric DISPLAY operand laid in the arena: where its first stored byte is and what its profile says.</summary>
    private readonly record struct Zoned(int At, NumProfile P)
    {
        public bool Separate => P.SignKind is NumericSign.LeadingSeparate or NumericSign.TrailingSeparate;
        public bool Leading => P.SignKind is NumericSign.LeadingSeparate or NumericSign.LeadingOverpunch;
        public int Size => P.Digits + (Separate ? 1 : 0);
        /// <summary>The first DIGIT byte: past a leading separate sign character.</summary>
        public int DataAt => At + (P.SignKind == NumericSign.LeadingSeparate ? 1 : 0);
        public int SignAt => Leading ? At : At + Size - 1;
        public SignEncoding Encoding => P.SignEncoding;
    }

    /// <summary>The operand's sign as the library reads it: 1 non-negative, -1 negative, 0 unsigned item. With
    /// <paramref name="adjust"/> (the PACKED receiver's reader) an over-punched sign under the IBM convention is
    /// UNPUNCHED IN PLACE and reported as 2 / -2, to be punched back once the store is done.</summary>
    private static int SignOf(char[] m, in Zoned f, bool adjust)
    {
        if (!f.P.Signed) return 0;
        int p = f.SignAt;
        if (f.Separate) return m[p] == '-' ? -1 : 1;
        if (IsDigit(m[p]) || m[p] == ' ') return 1;
        if (f.Encoding == SignEncoding.Ibm)
        {
            int s = UnpunchIbm(m, p);
            return adjust ? (s < 0 ? -2 : 2) : s;
        }
        if (adjust) return (m[p] & 0xF0) == 0x70 ? -1 : 1;
        return UnpunchAscii(m, p);
    }

    private static void PutSignAdjusted(char[] m, in Zoned f, int sign)
    {
        if (sign is 2 or -2) PutSign(m, f, sign == 2 ? 1 : -1);
    }

    private static void PutSign(char[] m, in Zoned f, int sign)
    {
        if (!f.P.Signed) return;
        int p = f.SignAt;
        if (f.Separate)
        {
            char c = sign == -1 ? '-' : '+';
            if (m[p] != c) m[p] = c;
        }
        else if (f.Encoding == SignEncoding.Ibm) PunchIbm(m, p, sign);
        else if (sign == -1) m[p] = (char)(m[p] | 0x40);
    }

    /// <summary>The ASCII convention's reader: a negative digit is <c>digit + 0x40</c> (<c>p</c> to <c>y</c>); it is
    /// replaced by the plain digit and -1 returned; a byte that is no digit at all reads as a positive zero.</summary>
    private static int UnpunchAscii(char[] m, int p)
    {
        if (m[p] >= 'p' && m[p] <= 'y') { m[p] = (char)(m[p] & ~0x40); return -1; }
        if (IsDigit(m[p])) return 1;
        m[p] = '0';
        return 1;
    }

    private const string IbmPositive = "{ABCDEFGHI";
    private const string IbmNegative = "}JKLMNOPQR";

    /// <summary>The IBM convention's reader: <c>{ A–I</c> positive, <c>} J–R</c> negative, the plain digit left and
    /// reported 0 (unsigned), anything else a positive digit by its low nibble (a non-digit one is zero).</summary>
    private static int UnpunchIbm(char[] m, int p)
    {
        int k = IbmPositive.IndexOf(m[p]);
        if (k >= 0) { m[p] = (char)('0' + k); return 1; }
        k = IbmNegative.IndexOf(m[p]);
        if (k >= 0) { m[p] = (char)('0' + k); return -1; }
        if (IsDigit(m[p])) return 0;
        m[p] = (m[p] & 0x0F) > 9 ? '0' : (char)('0' + (m[p] & 0x0F));
        return 1;
    }

    /// <summary>The IBM convention's writer: a digit becomes its punched character (positive or negative); a byte
    /// that already is a punched character of the sign asked for stays; anything else becomes a punched zero.</summary>
    private static void PunchIbm(char[] m, int p, int sign)
    {
        if (sign == -1)
        {
            if (IsDigit(m[p])) m[p] = IbmNegative[m[p] - '0'];
            else if (IbmNegative.IndexOf(m[p]) < 0) m[p] = '}';
            return;
        }
        if (IsDigit(m[p])) m[p] = IbmPositive[m[p] - '0'];
        else if (IbmPositive.IndexOf(m[p]) < 0) m[p] = '{';
    }
}
