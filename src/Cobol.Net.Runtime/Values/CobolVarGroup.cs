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
        var d = new string[dynCount];
        for (int k = 0; k < dynCount; k++) d[k] = Dyn(dynAt + k);
        return new CobolVarGroup(f, d);
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
