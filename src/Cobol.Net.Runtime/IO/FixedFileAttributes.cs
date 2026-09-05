// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using System.Text;

namespace CobolNet.Runtime.IO;

/// <summary>
/// The PERSISTED fixed-file-attribute catalog (ISO/IEC 1989:2023 §9.1.6) — the attributes a physical file was
/// CREATED with, recorded beside the file so a later OPEN can perform the §14.9.27.4 GR10 comparison.
/// <para>
/// ⛔ WHY A CATALOG EXISTS AT ALL. §9.1.6: "A physical file has several attributes that apply to the file at the
/// time it is created and cannot be changed throughout the lifetime of the file." A COBOL.NET data file on the
/// host is bytes; nothing in those bytes says which organization, record type or record size produced them. GR10
/// compares the connector's declared attributes with "the fixed file attributes of the file", so without a
/// recorded set there is nothing to compare and a program that opens a file under a contradicting FD reads
/// SILENTLY WRONG DATA instead of the '39' the rule requires (kb/Work PB193; the reproduction: a RELATIVE file of
/// 10-byte records reopened INPUT through a LINE SEQUENTIAL FD delivered an empty record with status '00').
/// </para>
/// <para>
/// ⛔ THE STORE IS A SIDECAR, NOT A HEADER (<see cref="SidecarPath"/> = the data file's path + <c>.cbattr</c>).
/// A header inside the data file would change the on-disk layout of every organization: line-sequential files
/// would stop being plain text — the interchange property that whole facility exists for — and every data file
/// written by an earlier build would become unreadable. A sidecar is additive: it travels with the file, is
/// removed with it (DELETE FILE, <see cref="Remove"/>), and its ABSENCE is a meaningful, safe state — see
/// <see cref="Load"/>.
/// </para>
/// <para>
/// ⛔ THIS TYPE IS THE ONE PLACE THE A.1 ITEM 129 VALIDATED SET IS DEFINED (<see cref="Conflicts"/>), exactly as
/// <c>FileRegistry.ValidateFixedFileAttributes</c> is the one place the §14.9.10.4 GR19 DELETE FILE set is
/// defined. The two are deliberately separate: §A.1 makes them two required determinations, and they differ —
/// DELETE FILE's is empty (nothing downstream depends on the description having been right; the file is
/// destroyed), OPEN's is not (everything after the OPEN reads the file THROUGH that description).
/// The determination is documented in <c>docs/CONFORMANCE.md</c> §7, row <c>DOC-A.1-129</c>.
/// </para>
/// </summary>
/// <param name="Organization">The §9.1.6 primary attribute, and ONLY that: §9.1.6 names exactly three
/// organizations — "There are three organizations: sequential, relative, and indexed" — so this field carries
/// exactly three values (<see cref="Sequential"/> · <see cref="Relative"/> · <see cref="Indexed"/>). §9.1.7.2's
/// record-sequential/line-sequential distinction is NOT a fourth organization; it is §9.1.6's separately listed
/// <i>record delimiter</i>, which is deliberately outside the validated set — see <see cref="Conflicts"/>.</param>
/// <param name="Varying">The §9.1.6 "record type (fixed or variable)": true = RECORD IS VARYING.</param>
/// <param name="MinRecordSize">The §9.1.6 "minimum ... logical record size in bytes".</param>
/// <param name="MaxRecordSize">The §9.1.6 "maximum logical record size in bytes".</param>
/// <param name="Keys">The §9.1.6 "prime record key, alternate record keys, SUPPRESS WHEN attribute ... the
/// collating sequence of the keys for indexed files": index 0 is the prime key, 1.. the alternates in
/// declaration order. Empty for a non-indexed file.</param>
public sealed record FixedFileAttributes(
    string Organization,
    bool Varying,
    int MinRecordSize,
    int MaxRecordSize,
    IReadOnlyList<FixedFileAttributes.KeyDescriptor> Keys)
{
    /// <summary>One record key's fixed attributes (§12.4.5.12 / §12.4.5.6 / §12.4.5.7): the byte window into the
    /// record image, the WITH DUPLICATES phrase, the §12.4.5.6.4 GR6 SUPPRESS WHEN value (null = no phrase) and
    /// the key's collating sequence, identified by <see cref="Fingerprint"/>.</summary>
    public readonly record struct KeyDescriptor(int Offset, int Length, bool Duplicates, string? Suppress, string Collation);

    /// <summary>§9.1.6's primary attribute, sequential — "There are three organizations: sequential, relative,
    /// and indexed". Both §9.1.7.2 types of sequential file (record sequential and line sequential) are THIS
    /// organization; the delimiter that separates them is §9.1.6's own <i>record delimiter</i> attribute and is
    /// not validated (<see cref="Conflicts"/>).</summary>
    public const string Sequential = "SEQUENTIAL";

    /// <summary>§9.1.6's primary attribute, relative (§9.1.7.3).</summary>
    public const string Relative = "RELATIVE";

    /// <summary>§9.1.6's primary attribute, indexed (§9.1.7.4).</summary>
    public const string Indexed = "INDEXED";

    /// <summary>The format tag + version of a catalog sidecar. A sidecar whose first line is anything else —
    /// a future version, a truncated write, an unrelated file that happens to sit at this path — is NOT a
    /// catalog and <see cref="Load"/> answers null for it.</summary>
    private const string FormatTag = "COBOLNET-FIXED-FILE-ATTRIBUTES 1";

    /// <summary>The catalog sidecar for a physical file: the data file's own path with <c>.cbattr</c> appended,
    /// so the two sort together, copy together and are obviously related to a human reading the directory.</summary>
    public static string SidecarPath(string hostPath) => hostPath + ".cbattr";

    // ── The A.1 item 129 determination ───────────────────────────────────────────────────────────────────────

    /// <summary>⛔ THE §14.9.27.4 GR10 COMPARISON, and therefore the DEFINITION of the Annex A.1 item 129
    /// validated set: true when this RECORDED set (the physical file's) and <paramref name="declared"/> (the
    /// connector's, from the file control paragraph and the file description entry) do not match, which GR10
    /// makes a file attribute conflict condition — the OPEN is unsuccessful with I-O status '39'
    /// (§9.1.13.6 item 7).
    /// <para>
    /// ⛔ <b>THE VALIDATED SET VARIES BY ORGANIZATION, and GR10 says in so many words that it may:</b> "The
    /// implementor defines which of the fixed-file attributes are validated during the execution of the OPEN
    /// statement. The validation of fixed-file attributes may vary depending on the organization or storage
    /// medium of the file." COBOL.NET validates exactly what the file's own storage FIXES — the attributes a
    /// disagreeing file description could not read the file back through:
    /// </para>
    /// <list type="bullet">
    /// <item><b>Every organization — the ORGANIZATION itself</b>, §9.1.6's "primary attribute", of which §9.1.6
    /// names exactly three: "There are three organizations: sequential, relative, and indexed". A relative or
    /// indexed store opened as a sequential byte stream, or the reverse, is not a record-length disagreement but
    /// a different physical structure, and it is the silently-wrong-data defect this catalog exists for
    /// (kb/Work PB193: a RELATIVE file of 10-byte records reopened through a LINE SEQUENTIAL FD delivered an
    /// EMPTY record with status '00').</item>
    /// <item><b>RELATIVE and INDEXED — additionally the RECORD TYPE, the MINIMUM and MAXIMUM LOGICAL RECORD
    /// SIZE, and for an indexed file the NUMBER OF KEYS and each key's WINDOW, DUPLICATES, SUPPRESS WHEN value
    /// and COLLATING SEQUENCE.</b> Those two organizations live in an implementor-defined store — the framed
    /// whole-store layout of <c>RecordFraming</c>, addressed by relative record number or by key value — whose
    /// STRUCTURE is those attributes, so a description that disagrees cannot interpret the store at all.</item>
    /// <item><b>SEQUENTIAL — nothing beyond the organization, and that is a DETERMINATION, not an omission.</b>
    /// §9.1.7.2 makes a sequential file's record lengths a property of the data and of the READING program
    /// rather than of the file: "In record sequential files the length of each record is determined by any
    /// information the implementor may add to the record on the physical storage medium (such as record length
    /// headers)" — and COBOL.NET adds none to a fixed-length record sequential file, which is plain bytes — and
    /// "In line sequential files the length of each record is determined by the number of characters between the
    /// preceding line delimiter and the following line delimiter or the end of file if no line delimiter is
    /// present". The standard then answers every disagreement such a re-read can produce with a SUCCESSFUL
    /// completion rather than a refused OPEN: §9.1.13.2 item 3's '04' — "A READ statement is successfully
    /// executed but the physical record from the file is shorter than or longer than the minimum or maximum
    /// length of records allowed for the fixed file attributes for that file" — item 5's '06' ("A READ statement
    /// for a line sequential file has successfully executed but a line delimiter or the end-of-file has not been
    /// detected") and item 7's '09'. Writing a print, report or extract file and reading it back under a
    /// different record description is exactly the idiom those three statuses exist for; a '39' there would
    /// reject legal source, and it would also be a SECOND MECHANISM for a job
    /// <see cref="RecordLayoutNotice"/> already does — the stderr notice that leaves the I-O status alone.</item>
    /// </list>
    /// <para>
    /// It follows that the §9.1.6 <b>record delimiter</b> — §9.1.7.2's record-sequential vs line-sequential
    /// distinction — is neither folded into <see cref="Organization"/> nor validated on its own: it is the same
    /// sequential re-read, and item 5's '06' is the standard's answer to it.
    /// </para>
    /// <para>
    /// The three §9.1.6 attributes deliberately NOT in the set, each because this processor cannot give it two
    /// different values rather than because checking was skipped: the <b>code set</b> — §13.18.13.4 GR7 makes it
    /// the NATIVE character set when no CODE-SET clause is written, GR2/GR6 make it the named alphabet's when one
    /// is, and COBOL.NET accepts only the alphabets whose implementor correspondence is the IDENTITY, refusing
    /// every other loudly (COBOLNET1672, the A.3 item 27 non-support in CONFORMANCE.md §2); so on every program
    /// this processor accepts the medium's code set is the native one, and the comparison is a constant with no
    /// second value to take; the <b>minimum and maximum
    /// physical record size</b> — the managed I-O model does no blocking and BLOCK CONTAINS is accepted inert
    /// under A.3 item 5, so no physical record size exists to record; and the <b>record delimiter</b> — the
    /// RECORD DELIMITER clause has no compiler surface, and the delimiter distinction this processor does
    /// realize (§9.1.7.2's record-sequential vs line-sequential) is a SEQUENTIAL-organization attribute, which
    /// the bullet list above puts outside the validated set for the reason given there.
    /// </para>
    /// <para>
    /// §9.1.6's last sentence — "The implementor shall specify whether the ability to share a physical file is a
    /// fixed file attribute" — is answered NO: a sharing mode is established per FILE CONNECTOR, by the SHARING
    /// clause or the OPEN's own SHARING phrase (§14.9.27.4 GR21–GR23), and is arbitrated live by
    /// <c>PhysicalFileTable</c> against the connectors currently open (§9.1.15 Table 19). It is not a property
    /// the physical file carries between run units, so it is neither recorded nor validated here.
    /// </para></summary>
    public bool Conflicts(FixedFileAttributes declared)
    {
        // §9.1.6's primary attribute — validated for every organization and every storage medium.
        if (!string.Equals(Organization, declared.Organization, StringComparison.Ordinal)) return true;
        // GR10's third sentence — the rest is validated only where the file's own store fixes it.
        if (!MediumFixesRecordLayout(Organization)) return false;
        if (Varying != declared.Varying) return true;
        if (MinRecordSize != declared.MinRecordSize || MaxRecordSize != declared.MaxRecordSize) return true;
        if (Keys.Count != declared.Keys.Count) return true;
        for (int i = 0; i < Keys.Count; i++)
            if (Keys[i] != declared.Keys[i]) return true;
        return false;
    }

    /// <summary>⛔ THE ONE PLACE GR10's "may vary depending on the organization or storage medium of the file"
    /// IS EXERCISED: does <paramref name="organization"/>'s storage physically FIX the record layout — the record
    /// type, the logical record sizes and the keys — so that a file description disagreeing with it could not
    /// read the file back? See <see cref="Conflicts"/> for the per-organization reasoning and its citations.
    /// <para>An organization no build knows answers FALSE, the conservative arm: an unrecognized store is not
    /// evidence that anything is fixed, and a validated set must never MANUFACTURE a '39'. (It cannot be reached
    /// today — <see cref="Load"/> refuses a sidecar naming an organization outside the §9.1.6 three — but the
    /// arm states the rule for whoever adds the fourth.)</para></summary>
    public static bool MediumFixesRecordLayout(string organization) => organization switch
    {
        Relative or Indexed => true,
        Sequential => false,
        _ => false,
    };

    // ── The collating-sequence identity ──────────────────────────────────────────────────────────────────────

    /// <summary>The identity of a key's collating sequence (§9.1.6 "the collating sequence of the keys for
    /// indexed files"), as a short stable fingerprint of the WEIGHTS the sequence assigns — not of the
    /// alphabet-name that produced them. Two sequences that order every key value identically ARE the same
    /// fixed attribute however they were spelled, and two spellings of one name that weigh differently (a
    /// LOCALE sequence under a different locale) are NOT — which is exactly the property GR10 needs, since what
    /// the physical file's index order depends on is the weights.
    /// <para>A null sequence is the native ordinal one (§12.4.5.3 GR6 — no applicable COLLATING SEQUENCE
    /// clause), reported as <c>NATIVE</c>. A sequence that cannot be probed answers <c>UNKNOWN</c>, which
    /// compares equal to itself and so can never manufacture a '39'.</para></summary>
    public static string Fingerprint(CobolCollation? collation)
    {
        if (collation is null) return "NATIVE";
        try
        {
            // FNV-1a over the sequence's position count and the weights of the Latin-1 repertoire — the byte
            // range every record key of an indexed file is sliced from (the connectors compare key images as
            // Latin-1 characters).
            ulong h = 14695981039346656037UL;
            void Mix(long v)
            {
                for (int b = 0; b < 8; b++) { h ^= (byte)(v >> (b * 8)); h *= 1099511628211UL; }
            }
            Mix(collation.PositionCount);
            for (int c = 0; c <= 0xFF; c++) Mix(collation.Weight((char)c));
            return h.ToString("x16", CultureInfo.InvariantCulture);
        }
        catch (Exception)   // a sequence that refuses to be probed is not evidence of a conflict
        {
            return "UNKNOWN";
        }
    }

    // ── Persistence ──────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The recorded fixed file attributes of the physical file at <paramref name="hostPath"/>, or
    /// <b>null when they are not recorded</b> — no sidecar, an unreadable one, or one written in a format
    /// version this build does not know.
    /// <para>⛔ NULL IS NOT A CONFLICT, AND MUST NEVER BECOME ONE. GR10 compares the connector against "the
    /// fixed file attributes of THE FILE"; a file whose attributes this processor cannot determine — one written
    /// by an external tool, or by a build older than the catalog — supplies no value to compare, so no attribute
    /// of it is validated. Answering '39' there would reject legal programs over a missing implementor artifact.
    /// The arithmetic fallback for exactly that file is <see cref="RecordLayoutNotice"/>.</para></summary>
    public static FixedFileAttributes? Load(string hostPath)
    {
        string[] lines;
        try
        {
            string sidecar = SidecarPath(hostPath);
            if (!File.Exists(sidecar)) return null;
            lines = File.ReadAllLines(sidecar);
        }
        catch (IOException) { return null; }
        catch (UnauthorizedAccessException) { return null; }
        catch (ArgumentException) { return null; }        // a host path the OS will not accept as a file name
        catch (NotSupportedException) { return null; }
        if (lines.Length == 0 || !string.Equals(lines[0].Trim(), FormatTag, StringComparison.Ordinal)) return null;

        string org = "";
        bool varying = false;
        int min = -1, max = -1;
        var keys = new List<KeyDescriptor>();
        foreach (string raw in lines.Skip(1))
        {
            string line = raw.Trim();
            if (line.Length == 0) continue;
            int eq = line.IndexOf('=');
            if (eq <= 0) return null;
            string name = line[..eq], value = line[(eq + 1)..];
            switch (name)
            {
                // The SIBLING of the record-type arm below, and the same rule: §9.1.6 names exactly three
                // organizations, so an "organization" this build does not recognize means the file's primary
                // attribute is unknown — which must land on "not recorded" (null), never on a value that
                // compares unequal to every declared organization and so MANUFACTURES a '39'.
                case "organization" when value is Sequential or Relative or Indexed: org = value; break;
                case "organization": return null;
                // A record-type this build does not recognize is NOT quietly read as FIXED: an unrecognized
                // value means the file's record type is unknown, and an unknown attribute must land on
                // "not recorded" (null), never on a guess that could manufacture — or suppress — a '39'.
                case "record-type" when value is "FIXED" or "VARIABLE": varying = value == "VARIABLE"; break;
                case "record-type": return null;
                case "record-min": if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out min)) return null; break;
                case "record-max": if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out max)) return null; break;
                case "key": if (ParseKey(value) is { } k) keys.Add(k); else return null; break;
                default: break;   // an attribute a LATER build records: ignored, so an old build still reads the rest
            }
        }
        return org.Length == 0 || min < 0 || max < 0 ? null : new FixedFileAttributes(org, varying, min, max, keys);
    }

    /// <summary>Record <paramref name="attributes"/> as the physical file's fixed file attributes — called at
    /// the moments §14.9.27.4 makes the OPEN statement CREATE the file (GR18's OUTPUT; GR17's absent OPTIONAL
    /// I-O/EXTEND), which §9.1.6 makes the one moment the attributes are established.
    /// <para>Best-effort by design: the standard defines no I-O status for a failure to record implementor
    /// metadata, and GR18 makes the OPEN OUTPUT itself successful, so a sidecar this process cannot write leaves
    /// the file in the well-defined "attributes not recorded" state of <see cref="Load"/> rather than failing an
    /// OPEN the standard says succeeded.</para></summary>
    public static void Store(string hostPath, FixedFileAttributes attributes)
    {
        var sb = new StringBuilder();
        sb.Append(FormatTag).Append('\n');
        sb.Append("organization=").Append(attributes.Organization).Append('\n');
        sb.Append("record-type=").Append(attributes.Varying ? "VARIABLE" : "FIXED").Append('\n');
        sb.Append("record-min=").Append(attributes.MinRecordSize.ToString(CultureInfo.InvariantCulture)).Append('\n');
        sb.Append("record-max=").Append(attributes.MaxRecordSize.ToString(CultureInfo.InvariantCulture)).Append('\n');
        foreach (var k in attributes.Keys) sb.Append("key=").Append(FormatKey(k)).Append('\n');
        try { File.WriteAllText(SidecarPath(hostPath), sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        catch (ArgumentException) { }
        catch (NotSupportedException) { }
    }

    /// <summary>Forget a physical file's recorded attributes — called when the file itself is destroyed
    /// (DELETE FILE, §14.9.10 Format 2). A catalog that outlived its file would be compared against a
    /// DIFFERENT file later created at the same path by something other than a COBOL.NET OPEN OUTPUT, which is
    /// precisely the state <see cref="Load"/>'s null is for.</summary>
    public static void Remove(string hostPath)
    {
        try { File.Delete(SidecarPath(hostPath)); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        catch (ArgumentException) { }
        catch (NotSupportedException) { }
    }

    // ── The key descriptor's text form ───────────────────────────────────────────────────────────────────────
    //    offset,length,D|N,<suppress: '-' or the Latin-1 bytes in hex>,<collation fingerprint>
    //    The SUPPRESS WHEN value is hex-encoded because §12.4.5.6.4 GR6 admits any figurative-constant or
    //    literal value, including one carrying the delimiter or a line ending.

    private static string FormatKey(KeyDescriptor k) => string.Join(',',
        k.Offset.ToString(CultureInfo.InvariantCulture),
        k.Length.ToString(CultureInfo.InvariantCulture),
        k.Duplicates ? "D" : "N",
        k.Suppress is null ? "-" : Convert.ToHexString(Encoding.Latin1.GetBytes(k.Suppress)),
        k.Collation);

    private static KeyDescriptor? ParseKey(string text)
    {
        string[] f = text.Split(',');
        if (f.Length != 5) return null;
        if (!int.TryParse(f[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int off)) return null;
        if (!int.TryParse(f[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int len)) return null;
        if (f[2] is not ("D" or "N")) return null;
        string? suppress;
        if (f[3] == "-") suppress = null;
        else
        {
            try { suppress = Encoding.Latin1.GetString(Convert.FromHexString(f[3])); }
            catch (FormatException) { return null; }
        }
        return new KeyDescriptor(off, len, f[2] == "D", suppress, f[4]);
    }
}
