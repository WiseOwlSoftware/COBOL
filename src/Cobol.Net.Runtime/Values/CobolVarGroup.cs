// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// ⛔ THE ACTIVATION-BOUNDARY CARRIER OF A VARIABLE-LENGTH GROUP (ISO §8.5.1.12; kb/Work PB204).
/// <para>A fixed-length group crosses a CALL / INVOKE boundary as ONE string — its record image — because
/// §14.2.3 GR8 makes the formal "occupy the same storage area as the argument" and that storage has a fixed
/// window. A VARIABLE-LENGTH group has no such window, so a flat string cannot be inverted: the receiver
/// cannot tell where a dynamic member's content ends and the next fixed member begins. This carrier is the
/// §8.5.1.12 model made into a wire form, and nothing more:</para>
/// <list type="bullet">
/// <item><see cref="Fixed"/> — the group's image with every variable-length component contributing NOTHING.
/// §8.5.1.12.3 says "all dynamic-length elementary items are considered to be of zero length" and makes a
/// matched dynamic-capacity table "the length of a single element", which is exactly the byte accounting under
/// which compatible groups have the SAME relative positions. So both sides lay this string out identically as
/// far as their fixed material agrees, and any tail difference is the §14.8.2.2 rule-1 size latitude, absorbed
/// by the ordinary width window.</item>
/// <item><see cref="Dynamic"/> — each variable-length component's CURRENT content, in declaration order:
/// a dynamic-length elementary item's characters, a dynamic-capacity table's occurrences concatenated at its
/// current capacity. §8.5.1.12.2's positional correspondence puts the two sides' components in the same order,
/// one for one, which is why an ordinal array is a faithful carrier and not an encoding trick. The receiving
/// table recovers its capacity by dividing by its own element width — legitimate because §8.5.1.12.3 makes
/// corresponding tables match only when "the byte length of their elements is equal".</item>
/// </list>
/// <para>NESTING is flattened by the emitted composer, because §8.5.1.12 is stated over relative byte positions
/// and is blind to the declaration tree: a nested variable-length group contributes its own fixed run and its
/// own dynamic components inline, and <see cref="Slice"/> hands it back exactly that window on the way in.</para>
/// <para>⛔ NOT a general-purpose serializer, and never persisted: it exists only between the argument
/// evaluation and the formal's copy-in (and back at the copy-out), the same lifetime the string image has.</para>
/// </summary>
/// <param name="Fixed">The group's image with the variable-length components collapsed to zero width.</param>
/// <param name="Dynamic">Each variable-length component's current content, in declaration order.</param>
public sealed record CobolVarGroup(string Fixed, string[] Dynamic)
{
    /// <summary>The empty carrier — an absent / OMITTED argument's value (ISO §14.9.4.4 GR11 hands out a
    /// carrier whose accessors raise; this is the shape those accessors return when checking is off).</summary>
    public static readonly CobolVarGroup Empty = new("", []);

    /// <summary>Component <paramref name="i"/>, or the zero-length string when the sending side carried fewer
    /// components than this side declares. A SHORTER sender is the §14.8.2.2 rule-1 direction the standard
    /// permits (the formal may be described with fewer bytes than the argument, and the reverse is diagnosed at
    /// bind), so a missing component is a zero-length value, never an index fault.</summary>
    public string Dyn(int i) => (uint)i < (uint)Dynamic.Length ? Dynamic[i] : "";

    /// <summary>Whether component <paramref name="i"/> was CARRIED at all — distinct from its being carried
    /// EMPTY, and the distinction is normative (kb/Work PB393). ISO §14.9.25.4 GR9b space-fills the receiving
    /// group's excess part, and its step 2 sends a dynamic-capacity table there to §14.6.9.4, where "the current
    /// capacity of the dynamic table is unaffected, and each element of the dynamic table is space-filled" —
    /// whereas a table whose SENDER carried a zero-capacity table is recreated at capacity zero by §14.6.9.2's
    /// "recreates or overwrites the receiving table with a copy of the sending table". Both arrive as a
    /// zero-length component string; only this tells them apart.</summary>
    public bool HasDyn(int i) => (uint)i < (uint)Dynamic.Length;

    /// <summary>The window a NESTED variable-length group occupies inside this carrier:
    /// <paramref name="fixedWidth"/> character positions of <see cref="Fixed"/> starting at
    /// <paramref name="fixedAt"/> (space-padded when the sender's fixed run was shorter — the same store rule
    /// every image distribution uses), and <paramref name="dynCount"/> components starting at
    /// <paramref name="dynAt"/>.</summary>
    public CobolVarGroup Slice(int fixedAt, int fixedWidth, int dynAt, int dynCount)
    {
        string f = CobolString.Store(
            fixedAt >= Fixed.Length ? "" : Fixed[fixedAt..Math.Min(Fixed.Length, fixedAt + fixedWidth)],
            fixedWidth);
        // ⛔ The window carries only what was ACTUALLY carried, never dynCount padded with empties: a nested
        // group must be able to answer <see cref="HasDyn"/> the same way its parent can, or GR9b step 2's
        // §14.6.9.4 space fill degrades into §14.6.9.2's recreate-at-zero one level down (kb/Work PB393).
        var d = new string[Math.Clamp(Dynamic.Length - dynAt, 0, dynCount)];
        for (int k = 0; k < d.Length; k++) d[k] = Dyn(dynAt + k);
        return new CobolVarGroup(f, d);
    }

    /// <summary>⛔ THE FIXED-LENGTH GROUP'S VIEW OF THIS CARRIER — the adapter that lets a FIXED group stand on
    /// the other side of an ISO §14.9.25.4 GR9 move (kb/Work PB393). §8.5.1.12.1 admits the pair explicitly
    /// ("either both operands may be variable-length groups or only one of the operands may be a variable-length
    /// group"), and §8.5.1.12.3 sentence 3 says how: a table corresponding to the other group's dynamic-capacity
    /// table "is treated as though it were a dynamic-capacity table whose capacity is either its fixed number of
    /// occurrences or the value of the DEPENDING operand, as applicable" — §14.6.9.1 states the same conversion
    /// for the operation itself. So a fixed group decomposes into EXACTLY this carrier: its record image with
    /// each corresponding table's character span lifted out as a component, in order.
    /// <para><paramref name="spans"/> is a FLAT (offset, width) pair list in character positions, computed at
    /// compile time from the group's own §8.5.1.12 atom layout. A width of −1 means "to the end of the image" —
    /// the occurs-depending table, whose current extent is a run-time length and which §13.18.38.3 SR22 makes
    /// the trailing storage of its record, so "the rest" IS its current occurrences.</para></summary>
    public static CobolVarGroup FromFixedImage(string image, int[] spans)
    {
        var dyn = new string[spans.Length / 2];
        var fixedRun = new System.Text.StringBuilder(image.Length);
        int at = 0;
        for (int k = 0; k < dyn.Length; k++)
        {
            int off = spans[2 * k];
            int width = spans[2 * k + 1];
            int start = Math.Min(off, image.Length);
            int end = width < 0 ? image.Length : Math.Min(start + width, image.Length);
            fixedRun.Append(image[Math.Min(at, image.Length)..start]);
            dyn[k] = image[start..end];
            at = end;
        }
        if (at < image.Length) fixedRun.Append(image[at..]);
        return new CobolVarGroup(fixedRun.ToString(), dyn);
    }

    /// <summary>The inverse of <see cref="FromFixedImage"/>: rebuild a FIXED group's record image of
    /// <paramref name="totalWidth"/> character positions by re-inserting each component at its span. A component
    /// is fitted to its span's width — ISO §14.6.9.2's own rule for a non-dynamic receiving table ("if the
    /// sending table has a higher current capacity than the receiving table, superfluous elements are not moved";
    /// "if the sending table has a lower current capacity … all the remaining elements of the receiving table are
    /// space filled") — and the fixed run is fitted to what is left, which is §14.9.25.4 GR9b's excess rule for
    /// the fixed material (space fill when short, ignore when long).</summary>
    public static string ToFixedImage(CobolVarGroup v, int totalWidth, int[] spans)
    {
        var outp = new System.Text.StringBuilder(totalWidth);
        int fixedAt = 0;
        for (int k = 0; k < spans.Length / 2; k++)
        {
            int off = spans[2 * k];
            int width = spans[2 * k + 1] < 0 ? Math.Max(0, totalWidth - off) : spans[2 * k + 1];
            int take = Math.Max(0, off - outp.Length);
            outp.Append(CobolString.Store(
                fixedAt >= v.Fixed.Length ? "" : v.Fixed[fixedAt..Math.Min(v.Fixed.Length, fixedAt + take)], take));
            fixedAt += take;
            outp.Append(CobolString.Store(v.Dyn(k), width));
        }
        outp.Append(fixedAt >= v.Fixed.Length ? "" : v.Fixed[fixedAt..]);
        return CobolString.Store(outp.ToString(), totalWidth);
    }

    /// <summary>Split a dynamic-capacity table's carried content into its occurrences at
    /// <paramref name="elementWidth"/> character positions each — the read half of the concatenation the
    /// composer emits. A trailing partial occurrence is padded, so a sender whose capacity ended mid-element
    /// (only reachable through the rule-1 size latitude) still yields well-formed elements.</summary>
    public static string[] Occurrences(string content, int elementWidth)
    {
        if (elementWidth <= 0 || content.Length == 0) return [];
        int n = (content.Length + elementWidth - 1) / elementWidth;
        var parts = new string[n];
        for (int k = 0; k < n; k++)
        {
            int at = k * elementWidth;
            parts[k] = CobolString.Store(content[at..Math.Min(content.Length, at + elementWidth)], elementWidth);
        }
        return parts;
    }
}
