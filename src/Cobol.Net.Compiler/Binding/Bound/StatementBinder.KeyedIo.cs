// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Validation;
using CobolSharp.Compiler.Generated;

namespace CobolNet.Binding.Bound;

using Core = CobolParserCore;

// ═══════════════════════════════════════════════════════════════════════════════════════════════════════════════
//  RELATIVE + INDEXED file organizations — the keyed-I/O bound nodes and binder (ISO/IEC 1989:2023 §9.1.7.3/.4,
//  §12.4.5, §14.9.10 DELETE, §14.9.30 READ F1/F2, §14.9.35 REWRITE, §14.9.41 START, §14.9.51 WRITE; COBOLNET_DESIGN
//  §8). The sequential subsystem is extended, never forked: BindRead/BindWrite/BindRewrite route non-sequential
//  organizations here; OPEN/CLOSE flow through the existing nodes (the CobolFile facade dispatches by connector).
// ═══════════════════════════════════════════════════════════════════════════════════════════════════════════════

/// <summary>The <c>INVALID KEY</c> / <c>NOT INVALID KEY</c> phrase pair of a keyed I/O statement (ISO §9.1.14):
/// the INVALID imperative runs exactly when the invalid key condition exists (an I-O status in the <c>2x</c>
/// family, §9.1.13.5); the NOT INVALID imperative runs ONLY on successful completion (§9.1.14 final rule item 2 —
/// a non-invalid-key UNSUCCESSFUL completion takes NEITHER branch, it goes to exception processing).</summary>
public sealed record KeyedInvalidKey(
    IReadOnlyList<BoundStatement>? Invalid, IReadOnlyList<BoundStatement>? NotInvalid);

/// <summary>How a keyed READ retrieves its record (ISO §14.9.30 GR19): a sequential Format-1 NEXT/PREVIOUS walk,
/// or the Format-2 random retrieval by key value.</summary>
public enum KeyedReadKind { Next, Previous, Random }

/// <summary>The positioning basis of a START (ISO §14.9.41 general format): FIRST / LAST (2002+), or the KEY
/// relational phrase (the COBOL-85 form; KEY omitted ⇒ EQUAL on the relative key / prime key, GR8/GR15).</summary>
public enum KeyedStartMode { First, Last, Key }

/// <summary><c>READ file [NEXT|PREVIOUS] [INTO x] [KEY IS k] [AT END …|INVALID KEY …]</c> on a RELATIVE or INDEXED
/// file (ISO §14.9.30). <paramref name="KeyIndex"/> is the key of reference for a Format-2 read: −1 = the prime
/// record key (GR31) / the relative key (GR29), ≥0 = the i-th ALTERNATE RECORD KEY (GR30). The emitter renders the
/// SSOT status-first shape (COBOLNET_DESIGN §8.3): store the status, branch AT END on <c>'1'</c> (10/14,
/// §9.1.13.4), INVALID KEY on <c>'2'</c> (§9.1.13.5), success phrases on <c>'0'</c>.</summary>
public sealed record BoundKeyedRead(
    FileModel File, KeyedReadKind Kind, int KeyIndex, Place? Into,
    IReadOnlyList<BoundStatement>? AtEnd, IReadOnlyList<BoundStatement>? NotAtEnd,
    KeyedInvalidKey? InvalidKey) : BoundStatement
{
    /// <summary>The explicit record-lock phrase (ISO §14.9.30 — WITH LOCK / WITH NO LOCK / IGNORING LOCK), or
    /// None; combined at runtime with the file's declared LOCK MODE (AUTOMATIC auto-locks on any READ).</summary>
    public BoundRecordLock Lock { get; init; } = BoundRecordLock.None;
    /// <summary>The RETRY phrase (§14.7.9): n TIMES loops the registry lock-check; SECONDS/FOREVER deadlock-bail
    /// to status 52 in one run unit (GR4a — no external releaser).</summary>
    public RetrySpec? Retry { get; init; }
    /// <summary>ADVANCING ON LOCK (§14.9.30 GR22, sequential NEXT/PREVIOUS only): skip-scan locked records.</summary>
    public bool AdvancingOnLock { get; init; }
}

/// <summary><c>WRITE record [FROM x] [INVALID KEY …]</c> on a RELATIVE or INDEXED file (ISO §14.9.51 GR29–GR33
/// relative / GR34–GR42 indexed). For a sequential-access relative file the released RRN is MOVEd back into the
/// RELATIVE KEY item on success (GR29a/GR30); for random/dynamic access the key item is read first (GR29b).</summary>
public sealed record BoundKeyedWrite(
    FileModel File, Place Record, BoundOperand? From, KeyedInvalidKey? InvalidKey) : BoundStatement;

/// <summary><c>REWRITE record [FROM x] [INVALID KEY …]</c> on a RELATIVE or INDEXED file (ISO §14.9.35 GR18–GR25):
/// relative random/dynamic replaces the slot named by the relative key (absent → '23', GR21); indexed sequential
/// requires the prime key to equal the last-read key ('21', GR22), random/dynamic an existing prime key ('23',
/// GR23).</summary>
public sealed record BoundKeyedRewrite(
    FileModel File, Place Record, BoundOperand? From, KeyedInvalidKey? InvalidKey) : BoundStatement;

/// <summary><c>DELETE file RECORD [INVALID KEY …]</c> (ISO §14.9.10 Format 1): sequential access removes the
/// record of the prior successful READ (GR2, else '43'); indexed random/dynamic deletes by the prime record key
/// (GR3), relative by the relative key item (GR4) — absent record → invalid key '23'. The FPI is unaffected (GR9).</summary>
public sealed record BoundKeyedDelete(FileModel File, KeyedInvalidKey? InvalidKey) : BoundStatement;

/// <summary><c>DELETE FILE file [ON EXCEPTION …]</c> (ISO §14.9.10 Format 2 — COBOL-2023, grammar-gated
/// <c>{is2023()}?</c>): removes the physical file. An open connector → '41' (GR13); an ABSENT file is a
/// SUCCESSFUL completion with status '05' (GR14 — the legacy returned '35'; the spec wins); insufficient
/// authority → '37' (GR16).</summary>
public sealed record BoundKeyedDeleteFile(
    FileModel File, IReadOnlyList<BoundStatement>? OnException, IReadOnlyList<BoundStatement>? NotOnException) : BoundStatement;

/// <summary><c>START file [FIRST|LAST|KEY rel-op k [WITH LENGTH n]] [INVALID KEY …]</c> (ISO §14.9.41).
/// <paramref name="Op"/> is the mapped C# relational operator (EQUAL when the KEY phrase or its operator is
/// omitted, GR8/GR15; NOT EQUAL is rejected, SR3). <paramref name="KeyIndex"/> = −1 prime/relative, ≥0 alternate.
/// <paramref name="Operand"/> is the comparison data item — the RELATIVE KEY item (SR5/GR10) or the (possibly
/// generic, leftmost-coincident shorter) indexed key item (SR6); <paramref name="Length"/> is the 2002+ WITH
/// LENGTH partial-key character count (SR8/GR13–GR14, indexed only).</summary>
public sealed record BoundKeyedStart(
    FileModel File, KeyedStartMode Mode, string Op, int KeyIndex, Place? Operand, BoundExpr? Length,
    KeyedInvalidKey? InvalidKey) : BoundStatement;

public sealed partial class StatementBinder
{
    /// <summary>Files already semantically checked by <see cref="KeyedValidateFile"/> (one report per file).</summary>
    private readonly HashSet<FileModel> _keyedCheckedFiles = [];

    // ── READ (ISO §14.9.30 Formats 1 and 2 on relative/indexed organizations) ─────────────────────────────────

    /// <summary>Bind a READ of a RELATIVE/INDEXED file. The format decision follows the syntax rules: an explicit
    /// NEXT/PREVIOUS is sequential (GR19); ACCESS SEQUENTIAL implies NEXT (SR8); ACCESS DYNAMIC implies NEXT only
    /// when an AT END / NOT AT END phrase is present — otherwise it is a Format-2 random read (SR9); ACCESS RANDOM
    /// is always Format 2 and forbids NEXT/PREVIOUS/AT END (SR6).</summary>
    private BoundStatement KeyedBindRead(Core.ReadStatementContext r, FileModel file)
    {
        KeyedValidateFile(file);
        Place? into = r.readInto()?.dataReference() is { } d ? refs.Resolve(d) : null;
        List<BoundStatement>? atEnd = null, notAtEnd = null;
        if (r.readAtEnd() is { } ae)
        {
            var blocks = ae.statementBlock();
            if (blocks.Length >= 1) atEnd = BindBlocks([blocks[0]]);
            if (blocks.Length >= 2) notAtEnd = BindBlocks([blocks[1]]);
        }
        KeyedInvalidKey? invalid =
            r.readInvalidKey() is { } ik ? KeyedInvalidPhrase(ik.statementBlock(), StartsWithNot(ik)) : null;

        bool next = r.readDirection()?.NEXT() is not null;
        bool previous = r.readDirection()?.PREVIOUS() is not null;
        // READ PREVIOUS was introduced by ISO/IEC 1989:2002 (§14.9.30 Format 1) — routed through the
        // registry (0900 band; W1.5): the former ad-hoc COBOLNET0860 collided with the WRITE END-OF-PAGE
        // diagnostic's 0860 and was not in the P2.3 pinned-code set.
        if (previous)
            ConstructRegistry.Check(data.Edition, "read-previous-2002", "READ … PREVIOUS");

        // §14.9.30 SR6 forbids NEXT/PREVIOUS/AT END under ACCESS RANDOM and the formats keep INVALID KEY off the
        // sequential read — but the CCVS-85 corpus is lenient about phrase placement (the L1–L3 leniency family),
        // so misplaced phrases are TOLERATED and bound: the emitter's status-first branches make a phrase that
        // cannot fire ('2x' on a sequential read, '1x' on a random read) simply dead, never silently rerouted.
        KeyedReadKind kind = file.AccessMode switch
        {
            FileAccessMode.Random => KeyedReadKind.Random,
            // §14.9.30 SR9 — dynamic: NEXT implied only when an AT END / NOT AT END phrase is present; a bare
            // READ under dynamic access is the Format-2 random read.
            FileAccessMode.Dynamic => previous ? KeyedReadKind.Previous
                : next || r.readAtEnd() is not null ? KeyedReadKind.Next
                : KeyedReadKind.Random,
            // §14.9.30 SR8 — sequential access: NEXT implied.
            _ => previous ? KeyedReadKind.Previous : KeyedReadKind.Next,
        };

        int keyIndex = -1;   // the prime record key (GR31) / the relative key item (GR29) when no KEY phrase
        if (r.readKey()?.dataReference() is { } keyRef)
        {
            // SR10: the KEY phrase only for indexed organization; SR11: it names a declared (prime or alternate)
            // key — matched by STORAGE POSITION, not name (§12.4.5.12 GR4: the key's character positions are
            // implicitly keys in EVERY record description of the file; covers REDEFINES and duplicate names).
            if (file.Organization != FileOrganization.Indexed)
                data.Edition.Error("COBOLNET0864", $"READ … KEY on '{file.CobolName}': the KEY phrase may be "
                    + "specified only when ORGANIZATION IS INDEXED (ISO §14.9.30 SR10)");
            else if (kind != KeyedReadKind.Random)
                data.Edition.Error("COBOLNET0864", $"READ … KEY on '{file.CobolName}' is a Format-2 phrase and "
                    + "cannot combine with NEXT/PREVIOUS/AT END (ISO §14.9.30 general formats)");
            else if (refs.Resolve(keyRef) is not { } keyPlace || KeyedKeyIndex(file, keyPlace.Item) is not { } ki)
            {
                data.Edition.Error("COBOLNET0864", $"READ … KEY IS {keyRef.GetText()} on '{file.CobolName}': the "
                    + "operand shall be the RECORD KEY or an ALTERNATE RECORD KEY of the file (ISO §14.9.30 SR11)");
                return new BoundUnsupported($"READ KEY '{keyRef.GetText()}' (no matching key of reference)");
            }
            else keyIndex = ki;
        }
        return new BoundKeyedRead(file, kind, keyIndex, into, atEnd, notAtEnd, invalid)
        {
            Lock = CheckRecordLockPhrase(file, r.recordLockPhrase(), "READ"),
            Retry = BindVerbRetry(r.retryPhrase()),
            AdvancingOnLock = r.readAdvancingOnLock() is not null,
        };
    }

    // ── WRITE / REWRITE (ISO §14.9.51 / §14.9.35 on relative/indexed organizations) ────────────────────────────

    /// <summary>Bind a WRITE on a RELATIVE/INDEXED file (called from <c>BindWrite</c> with the record and owning
    /// file already resolved). The print-control phrases (ADVANCING / END-OF-PAGE) apply to sequential print files
    /// only and fail loud here.</summary>
    private BoundStatement KeyedBindWrite(Core.WriteStatementContext w, FileModel file, Place record)
    {
        KeyedValidateFile(file);
        if (w.writeBeforeAfter() is not null || w.writeAtEndOfPage() is not null)
            return new BoundUnsupported($"WRITE ADVANCING / END-OF-PAGE on {file.Organization} file "
                + $"'{file.CobolName}' (ISO §14.9.51 — print-control phrases are for sequential print files)");
        KeyedInvalidKey? invalid =
            w.writeInvalidKey() is { } ik ? KeyedInvalidPhrase(ik.statementBlock(), StartsWithNot(ik)) : null;
        return new BoundKeyedWrite(file, record,
            WriteSource(w.writeFrom()?.dataReference(), w.writeFrom()?.literal()), invalid);
    }

    /// <summary>Bind a REWRITE on a RELATIVE/INDEXED file. §14.9.35 SR2: the INVALID KEY phrases shall not be
    /// specified for a relative-organization file in SEQUENTIAL access mode (its rewrite has no key condition —
    /// only '43'/'49' logic errors, which route to exception processing).</summary>
    private BoundStatement KeyedBindRewrite(Core.RewriteStatementContext rw, FileModel file, Place record)
    {
        KeyedValidateFile(file);
        // §14.9.35 SR2 forbids INVALID KEY for a relative file in sequential access — tolerated in the default
        // (CCVS-lenient) mode like the L1 leniency that parsed it: a sequential-access relative REWRITE can only
        // raise 4x statuses, so the bound phrase is dead in the status-first branches, never silently rerouted.
        KeyedInvalidKey? invalid =
            rw.rewriteInvalidKeyPhrase() is { } ik ? KeyedInvalidPhrase(ik.statementBlock(), StartsWithNot(ik)) : null;
        return new BoundKeyedRewrite(file, record,
            WriteSource(rw.rewriteFrom()?.dataReference(), rw.rewriteFrom()?.literal()), invalid);
    }

    // ── DELETE (ISO §14.9.10 Formats 1 and 2) ──────────────────────────────────────────────────────────────────

    /// <summary>Bind <c>DELETE file RECORD</c> (ISO §14.9.10 Format 1). SR1: never for sequential organization;
    /// SR2: no INVALID KEY phrases in sequential access mode (the deletion target is the prior READ's record, GR2
    /// — there is no key condition to raise).</summary>
    private BoundStatement KeyedBindDelete(Core.DeleteStatementContext del)
    {
        string name = del.fileName().GetText();
        if (!data.FilesByName.TryGetValue(name, out var file))
            return new BoundUnsupported($"DELETE of undeclared file '{name}'");
        if (file.IsSequential)
        {
            data.Edition.Error("COBOLNET0865", $"DELETE RECORD shall not be specified for sequential-organization "
                + $"file '{name}' (ISO §14.9.10 SR1)");
            return new BoundUnsupported($"DELETE on sequential file '{name}'");
        }
        KeyedValidateFile(file);
        // §14.9.10 SR2 forbids INVALID KEY in sequential access mode — tolerated in the default (CCVS-lenient)
        // mode: a sequential-access DELETE raises only 4x statuses, so the phrase is dead, never misrouted.
        KeyedInvalidKey? invalid =
            del.deleteInvalidKeyPhrase() is { } ik ? KeyedInvalidPhrase(ik.statementBlock(), StartsWithNot(ik)) : null;
        return new BoundKeyedDelete(file, invalid);
    }

    /// <summary>Bind <c>DELETE FILE file</c> (ISO §14.9.10 Format 2 — COBOL-2023; the grammar's <c>{is2023()}?</c>
    /// predicate already rejects it below 2023, the four-compilers rule's parse-side gate).</summary>
    private BoundStatement KeyedBindDeleteFile(Core.DeleteFileStatementContext df)
    {
        string name = df.fileName().GetText();
        if (!data.FilesByName.TryGetValue(name, out var file))
            return new BoundUnsupported($"DELETE FILE of undeclared file '{name}'");
        List<BoundStatement>? on = null, notOn = null;
        if (df.deleteFileOnException() is { } ex)
        {
            var blocks = ex.statementBlock();
            if (blocks.Length >= 1) on = BindBlocks([blocks[0]]);
            if (blocks.Length >= 2) notOn = BindBlocks([blocks[1]]);
        }
        return new BoundKeyedDeleteFile(file, on, notOn);
    }

    // ── START (ISO §14.9.41) ───────────────────────────────────────────────────────────────────────────────────

    /// <summary>Bind START. SR1: access shall be sequential or dynamic; SR3: NOT EQUAL is not a valid operator;
    /// SR5: a relative operand shall be the RELATIVE KEY item; SR6: an indexed operand names a key or a
    /// leftmost-coincident shorter item (a generic key, matched by storage position); GR8/GR15: a missing KEY
    /// phrase means EQUAL on the relative key / prime key. FIRST/LAST are 2002+ (edition-gated here).</summary>
    private BoundStatement KeyedBindStart(Core.StartStatementContext st)
    {
        string name = st.fileName().GetText();
        if (!data.FilesByName.TryGetValue(name, out var file))
            return new BoundUnsupported($"START of undeclared file '{name}'");
        if (file.IsSequential)
            return new BoundUnsupported($"START on sequential-organization file '{name}' "
                + "(ISO §14.9.41 SR2 FIRST/LAST positioning — a later slice)");
        KeyedValidateFile(file);
        if (file.AccessMode == FileAccessMode.Random)
            data.Edition.Error("COBOLNET0862", $"START on '{name}': the access mode shall be sequential or "
                + "dynamic (ISO §14.9.41 SR1)");
        KeyedInvalidKey? invalid =
            st.startInvalidKeyPhrase() is { } ik ? KeyedInvalidPhrase(ik.statementBlock(), StartsWithNot(ik)) : null;

        if (st.FIRST() is not null || st.LAST() is not null)
        {
            // START FIRST/LAST entered the standard with ISO/IEC 1989:2002 (§14.9.41 general format) —
            // routed through the registry (0900 band; W1.5): the former ad-hoc COBOLNET0861 collided with
            // the WRITE ADVANCING PAGE/END-OF-PAGE diagnostic's 0861 and was not in the P2.3 pinned set.
            ConstructRegistry.Check(data.Edition, "start-first-last-2002",
                $"START {(st.LAST() is not null ? "LAST" : "FIRST")}");
            return new BoundKeyedStart(file, st.LAST() is not null ? KeyedStartMode.Last : KeyedStartMode.First,
                "==", -1, null, null, invalid);
        }

        var kp = st.startKeyPhrase();
        string op = kp?.comparisonOperator() is { } oc ? MapOperator(oc.GetText()) : "==";   // GR8/GR15 — EQUAL
        if (op == "!=")
        {
            data.Edition.Error("COBOLNET0862", $"START on '{name}': the relational operator shall not be "
                + "'IS NOT EQUAL TO' (ISO §14.9.41 SR3)");
            op = "==";
        }
        Place? operand = kp?.dataReference() is { } dref ? refs.Resolve(dref) : null;
        if (kp is not null && operand is null)
            return new BoundUnsupported($"START KEY operand '{kp.dataReference().GetText()}'");

        // WITH LENGTH (2002+, grammar-gated {is2002()}?): the partial-key character count (§14.9.41 GR13–GR14).
        BoundExpr? length = kp?.startWithLength()?.arithmeticExpression() is { } le ? BindExpr(le) : null;
        if (length is not null && file.Organization != FileOrganization.Indexed)
            data.Edition.Error("COBOLNET0862", $"START … WITH LENGTH on '{name}': the LENGTH phrase requires "
                + "indexed organization (ISO §14.9.41 SR8)");

        if (file.Organization == FileOrganization.Relative)
        {
            if (operand is not null && !ReferenceEquals(operand.Item, file.RelativeKeyItem))
                data.Edition.Error("COBOLNET0862", $"START on '{name}': data-name-1 shall be the RELATIVE KEY "
                    + $"item '{file.RelativeKeyItem?.CobolName ?? "(none)"}' (ISO §14.9.41 SR5)");
            operand ??= file.RelativeKeyItem is { } rk ? refs.ResolveItem(rk) : null;
            if (operand is null)
                return new BoundUnsupported($"START on '{name}' with no resolvable RELATIVE KEY item");
            return new BoundKeyedStart(file, KeyedStartMode.Key, op, -1, operand, null, invalid);
        }

        int keyIndex = -1;
        if (operand is not null)
        {
            if (KeyedKeyIndex(file, operand.Item) is not { } ki)
            {
                data.Edition.Error("COBOLNET0862", $"START on '{name}': '{operand.Item.CobolName}' neither names "
                    + "a record key nor begins at the leftmost character position of one with a length not "
                    + "greater than that key (ISO §14.9.41 SR6)");
                return new BoundUnsupported($"START KEY operand '{operand.Item.CobolName}' (not a key of '{name}')");
            }
            keyIndex = ki;
        }
        else
            operand = file.RecordKeyItem is { } pk ? refs.ResolveItem(pk) : null;   // GR15 — prime key EQUAL
        if (operand is null)
            return new BoundUnsupported($"START on '{name}' with no resolvable RECORD KEY item");
        return new BoundKeyedStart(file, KeyedStartMode.Key, op, keyIndex, operand, length, invalid);
    }

    // ── Shared keyed-I/O helpers ───────────────────────────────────────────────────────────────────────────────

    /// <summary>Build the INVALID/NOT INVALID pair from the phrase's statement blocks. All five phrases share the
    /// grammar shape <c>INVALID KEY? b1 (NOT INVALID KEY? b2)? | NOT INVALID KEY? b1</c>; the NOT-only alternative
    /// is detected by its leading NOT token (the same discriminator the ON SIZE ERROR binder uses).</summary>
    private KeyedInvalidKey KeyedInvalidPhrase(Core.StatementBlockContext[] blocks, bool notOnly)
    {
        if (notOnly) return new KeyedInvalidKey(null, BindBlocks([blocks[0]]));
        var inv = blocks.Length >= 1 ? BindBlocks([blocks[0]]) : null;
        var not = blocks.Length >= 2 ? BindBlocks([blocks[1]]) : null;
        return new KeyedInvalidKey(inv, not);
    }

    /// <summary>Resolve a key-of-reference operand to its key index by STORAGE POSITION (−1 = prime, i = the i-th
    /// alternate, null = no match): the operand qualifies when its leftmost character position within the record
    /// area coincides with the key's leftmost position and it is no longer than the key (ISO §14.9.41 SR6 generic
    /// keys; §14.9.30 SR11; §12.4.5.12 GR4 — key positions are implicitly keys in ALL record descriptions, so a
    /// REDEFINES of the key or a same-named item in another 01 matches by position, never by name).</summary>
    private static int? KeyedKeyIndex(FileModel file, DataItem operand)
    {
        if (KeyedAreaOffset(operand) is not { } off) return null;
        if (file.RecordKeyItem is { } pk && KeyedAreaOffset(pk) == off && operand.ImageWidth <= pk.ImageWidth)
            return -1;
        for (int i = 0; i < file.AlternateKeys.Count; i++)
        {
            var alt = file.AlternateKeys[i].Item;
            if (KeyedAreaOffset(alt) == off && operand.ImageWidth <= alt.ImageWidth) return i;
        }
        return null;
    }

    /// <summary>The item's character offset within its record AREA: the offset inside its own 01 root, which IS
    /// the area offset because every secondary 01 under an FD is a synthesized REDEFINES of the first (starting at
    /// position 0, ISO §13.18.44 GR1). A REDEFINES subordinate takes its target's offset and contributes no width
    /// — mirroring the layout the generated record codec (and <c>DataBinder.AssignClassOffsets</c>) produces.</summary>
    private static int? KeyedAreaOffset(DataItem item)
    {
        DataItem root = item;
        while (root.Parent is { } p) root = p;
        int? found = null;
        var offsets = new Dictionary<DataItem, int>();
        Walk(root, 0);
        return found;

        void Walk(DataItem node, int off)
        {
            if (found is not null) return;
            offsets[node] = off;
            if (ReferenceEquals(node, item)) { found = off; return; }
            int running = off;
            foreach (var c in node.Children)
            {
                int cOff = c.RedefinesTarget is { } t && offsets.TryGetValue(t, out int tOff) ? tOff : running;
                Walk(c, cOff);
                if (found is not null) return;
                if (c.RedefinesTarget is null) running += c.ImageWidth * (c.Occurs ?? 1);
            }
        }
    }

    /// <summary>One-time semantic checks of a keyed file's control entry, run on its first keyed verb (the
    /// FILE-CONTROL clauses are captured by DataBinder; the rules live with the verbs that need them):
    /// an INDEXED file requires a RECORD KEY (ISO §12.4.5.1 Format 1 — RECORD KEY is a required clause);
    /// a RELATIVE file in random/dynamic access requires a RELATIVE KEY (§12.4.5.13); the RELATIVE KEY item is an
    /// unsigned integer without 'P' (§12.4.5.13 SR2) and shall NOT be defined within a record of the file
    /// (§12.4.5.13 SR3 — it lives outside, typically WORKING-STORAGE).</summary>
    private void KeyedValidateFile(FileModel file)
    {
        if (!_keyedCheckedFiles.Add(file)) return;
        if (file.Organization == FileOrganization.Indexed && file.HasFd && file.RecordKeyItem is null)
            data.Edition.Error("COBOLNET0863", $"indexed file '{file.CobolName}' has no RECORD KEY clause "
                + "(ISO §12.4.5.1 Format 1 — RECORD KEY is required for ORGANIZATION INDEXED)");
        if (file.Organization != FileOrganization.Relative) return;
        if (file.RelativeKeyItem is null && file.AccessMode != FileAccessMode.Sequential)
            data.Edition.Error("COBOLNET0863", $"relative file '{file.CobolName}' is ACCESS {file.AccessMode} "
                + "but has no RELATIVE KEY clause (ISO §12.4.5.13 — required for random/dynamic access)");
        if (file.RelativeKeyItem is not { } rk) return;
        if (rk.Pic is not { Category: PicCategory.Numeric, Scale: 0, Signed: false })
            data.Edition.Error("COBOLNET0863", $"RELATIVE KEY '{rk.CobolName}' shall be an unsigned integer "
                + "without the symbol 'P' (ISO §12.4.5.13 SR2)");
        DataItem root = rk;
        while (root.Parent is { } p) root = p;
        if (file.Records.Contains(root))
            data.Edition.Error("COBOLNET0863", $"RELATIVE KEY '{rk.CobolName}' shall not be defined within a "
                + $"record description of file '{file.CobolName}' (ISO §12.4.5.13 SR3)");
    }
}
