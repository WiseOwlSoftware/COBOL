// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Common;
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Binding.Bound;
using CobolNet.CodeGen.Emit;
using CobolNet.Runtime;

namespace CobolNet.CodeGen;

using static CobolNet.CodeGen.Emit.EmitText;

/// <summary>The sequential file-I/O emitter (P7 Step 9j — a real collaborator over the per-unit
/// <see cref="EmitContext"/>): registration (incl. LINAGE/SHARING), OPEN/CLOSE/UNLOCK/WRITE/READ/REWRITE, and
/// the file-I/O common services every I/O verb shares — the post-status USE-declarative hook, the FILE STATUS
/// store (incl. the §12.4.5.8.4 GR1 NOTE 1 inherited-GLOBAL routing via <see cref="CallUnitState"/>), the
/// record-area image splice, and the RETRY/lock renders KeyedIo consumes.</summary>
internal sealed class SequentialIoEmitter(EmitContext ctx, NumericRenderer num, ReferenceResolver refs,
    DispatchState dispatch, EcState ecState, CallUnitState callState, KeyedIoEmitter keyedIo,
    ArithmeticEmitter arith, EcEmitter ec, MoveEmitter move)
{
    /// <summary>The statement dispatcher — property-wired by <see cref="UnitEmitters"/> (the AT END / AT EOP /
    /// NOT-ON phrase bodies nest arbitrary statement lists, a cyclic edge no ctor order can satisfy).</summary>
    internal StatementEmitter Statements { get; set; } = null!;

    /// <summary>The declarative hook after a verb's FILE STATUS store (GR6 — after the standard status routine,
    /// BEFORE the statement's phrase branches). A statement with ENABLED EC-I-O checking for this file (>>TURN,
    /// ISO §7.3.25) calls the EC-aware <c>__IoCheckEc</c> variant instead — same F1 behavior plus the §9.1.13.1
    /// status→EC raise, F3 selection and fatal default, returning a RESUME transfer pc when a declarative's
    /// RESUME redirected control (§14.9.33). A no-op for a declarative-free, checking-off program.</summary>
    public void EmitUseHook(FileModel file, bool atEndHandled = false, bool invalidKeyHandled = false)
    {
        var w = ctx.Writer;
        if (ec.IoMaskFor(file) is not 0 and var mask)
        {
            int id = ctx.Names.NextEc();
            var (stmt, loc) = ec.EcStmtLoc(ecState.Info!);
            w.Line($"int __ior{id} = __IoCheckEc({FileKeyExpr(file)}, {(atEndHandled ? "true" : "false")}, "
                + $"{(invalidKeyHandled ? "true" : "false")}, {mask}, {stmt}, {loc});");
            w.Line($"if (__ior{id} >= 0) {{ __pc = __ior{id}; break; }}   // RESUME AT procedure-name (§14.9.33.4 GR3)");
            return;
        }
        if (!dispatch.UseDecls) return;
        w.Line($"__IoCheck({FileKeyExpr(file)}, {(atEndHandled ? "true" : "false")}, {(invalidKeyHandled ? "true" : "false")});");
    }



    /// <summary>Emit the file registry init + one <c>Register</c> per SELECTed sequential file (at <c>Main</c> start).
    /// The ASSIGN target becomes a host path at run time (the runtime's <c>ResolveHostPath</c>); the record width is the
    /// FD record-area image width. A non-sequential file is skipped here (its verbs emit loud guards).</summary>
    public void EmitFileRegistration(CodeWriter w)
    {
        foreach (var file in ctx.Data.Files)
        {
            if (file.IsSortMerge) continue;   // an SD is the in-memory sort store (ISO §13.4.6) — never a host file
            if (!file.IsSequential) { keyedIo.EmitRegistration(w, file); EmitSharingRegistration(w, file); continue; }   // relative/indexed connectors
            if (file.Records.Count == 0)
            {
                // A REPORT FILE legally has no record description (ISO §9.1.22 / §13.18.46) — it MUST still
                // register, or its OPEN falls through to the keyed registries and the report engine's writes go
                // into a void (the silent-OPEN-no-op hazard, COBOLNET_REPORT_WRITER_DESIGN §7). The record
                // width is the widest hosted report's line width.
                if (file.ReportNames.Count > 0)
                {
                    int width = Math.Max(1, ctx.Data.Reports
                        .Where(r => ReferenceEquals(r.File, file))
                        .Select(r => r.LineWidth).DefaultIfEmpty(1).Max());
                    w.Line($"{RuntimeApi.FileRegister(FileKeyExpr(file), CsLiteral(file.AssignTarget), $"{width}", "false", file.Optional ? "true" : "false")};");
                }
                continue;
            }
            bool lineSeq = file.Organization == FileOrganization.LineSequential;
            // A variable-length file registers its record-size bounds (ISO §13.18.43 GR9/GR10) — the connector
            // length-frames its records and enforces the GR14 '44' boundary checks.
            string vary = file.Varying is not null ? $", {file.VaryMin}, {file.VaryMax}" : "";
            w.Line($"{RuntimeApi.FileRegister(FileKeyExpr(file), CsLiteral(file.AssignTarget), $"{file.RecordWidth}", lineSeq ? "true" : "false", file.Optional ? "true" : "false", vary)};");
            // A LINAGE file registers its logical-page evaluator (ISO §13.18.34 GR6): ONE closure for both the
            // literal (GR6a — a constant lambda) and data-name (GR6b — the connector re-reads at OPEN OUTPUT /
            // ADVANCING PAGE / page overflow) forms. The lambda READS the program fields at call time — it is
            // emitted in __Activate (an instance method), so they are in scope and never captured by value.
            if (file.Linage is { } lin)
                w.Line($"{RuntimeApi.FileSetLinage(FileKeyExpr(file), $"() => ({LinageOpExpr(lin.Body)}, {LinageOpExpr(lin.Footing)}, {LinageOpExpr(lin.Top)}, {LinageOpExpr(lin.Bottom)})")};");
            EmitSharingRegistration(w, file);
        }
    }

    /// <summary>Emit the sharing registration for a file that declares a SHARING and/or LOCK MODE
    /// clause (Phase 4d M2-FILE-1) — this marks the connector "sharing-active" so its OPEN routes through the
    /// physical-file registry (Table-19 → 61) and its READs through record-lock governance. Files without either
    /// clause emit nothing and keep the legacy exclusive path byte-for-byte (ISO §14.9.27 GR23 implementor
    /// default). A LOCK-MODE-only file has no sharing conflict posture, so its sharing maps to the neutral
    /// ALL OTHER (record locking observable, no spurious 61).</summary>
    private void EmitSharingRegistration(CodeWriter w, FileModel file)
    {
        if (file.Sharing == SharingMode.None && file.LockMode is null) return;
        string sharing = file.Sharing switch
        {
            SharingMode.NoOther => "FileSharing.NoOther",
            SharingMode.ReadOnly => "FileSharing.ReadOnly",
            _ => "FileSharing.AllOther",   // AllOther, or None (LOCK-MODE-only) → neutral default
        };
        string lockMode = (file.LockMode?.Kind ?? LockKind.None) switch
        {
            LockKind.Manual => "FileLockMode.Manual",
            LockKind.Automatic => "FileLockMode.Automatic",
            _ => "FileLockMode.None",
        };
        bool multiple = file.LockMode?.Multiple ?? false;
        w.Line($"{RuntimeApi.FileRegisterSharing(FileKeyExpr(file), $"{sharing}, {lockMode}, {(multiple ? "true" : "false")}")};");
    }

    /// <summary>The C# <c>int</c> expression for one LINAGE clause operand (ISO §13.18.34 GR6): the fixed literal
    /// (GR6a), the data item's current value (GR6b — scale 0 by SR2's elementary-unsigned-integer rule, with a
    /// defensive rescale), or <c>0</c> for an absent TOP/BOTTOM/FOOTING phrase (GR1 — margins zero; footing 0 =
    /// no footing area). A declared data-name that does not resolve to storage fails loud (§1.4).</summary>
    private string LinageOpExpr(LinageOperand? op)
    {
        if (op is null) return "0";
        if (op.Literal is { } lit) return lit.ToString();
        if (op.Item is { } item && refs.ResolveItem(item) is { } p)
        {
            var nx = num.FieldNum(p);
            return nx.Scale == 0 ? $"(int)({nx.Expr})"
                : $"(int){RuntimeApi.NumRescale(nx.Expr, $"{nx.Scale}", "0", CobolRounding.Truncation)}";
        }
        return LoudValue("int", $"LINAGE operand '{op.DataName}' is not resolvable to storage (ISO §13.18.34 SR2)");
    }

    public void EmitOpen(BoundOpen o)
    {
        var w = ctx.Writer;
        // A SHARING override or RETRY phrase on the OPEN (ISO §14.9.27, COBOL-2002) routes every file in the
        // statement's mode group through the sharing-aware facade; a plain OPEN keeps the direct entry points.
        bool shared = o.SharingOverride is not null || o.Retry is not null;
        foreach (var (file, mode, unsupported) in o.Files)
        {
            if (unsupported is { } u) { w.Line(LoudStmt(u)); continue; }
            if (shared)
            {
                string modeEnum = mode switch
                {
                    BoundOpenMode.Output => "FileOpenMode.Output",
                    BoundOpenMode.Extend => "FileOpenMode.Extend",
                    BoundOpenMode.IO => "FileOpenMode.IO",
                    _ => "FileOpenMode.Input",
                };
                var (retryKind, retryAmount) = RenderRetry(o.Retry);
                string shHas = o.SharingOverride is not null ? "true" : "false";
                string shVal = o.SharingOverride is { } sm ? RuntimeSharing(sm) : "FileSharing.AllOther";
                w.Line($"{RuntimeApi.FileOpenShared(FileKeyExpr(file), $"{modeEnum}, {shHas}, {shVal}, {retryKind}, {retryAmount}")};");
            }
            else
            {
                w.Line($"{RuntimeApi.FileOpen(FileKeyExpr(file), mode)};");
            }
            EmitStoreFileStatus(file);
            EmitUseHook(file);   // a failed OPEN reaches a mode-scoped USE via the being-opened mode (GR6b)
        }
    }

    /// <summary>Map a bound SHARING mode to the runtime <c>FileSharing</c> enum member (Phase 4d).</summary>
    private static string RuntimeSharing(SharingMode m) => m switch
    {
        SharingMode.NoOther => "FileSharing.NoOther",
        SharingMode.ReadOnly => "FileSharing.ReadOnly",
        _ => "FileSharing.AllOther",
    };

    /// <summary>Render a bound RETRY phrase (ISO §14.7.9) to the runtime <c>(FileRetryKind, int amount)</c> pair —
    /// the amount is the n-TIMES count (rendered as a C# int); SECONDS/FOREVER pass 0 (their amount is a
    /// single-run-unit no-op, deadlock-bailing to status 52).</summary>
    public (string Kind, string Amount) RenderRetry(RetrySpec? retry) => retry switch
    {
        null => ("FileRetryKind.None", "0"),
        { Kind: RetryKind.Forever } => ("FileRetryKind.Forever", "0"),
        { Kind: RetryKind.Seconds } => ("FileRetryKind.Seconds", RetryAmount(retry.Amount)),
        _ => ("FileRetryKind.Times", RetryAmount(retry.Amount)),
    };

    private string RetryAmount(BoundExpr? amount) =>
        amount is null ? "0" : $"(int)({NumericRenderer.Align(num.Render(amount, ReceiverContext.None), 0)})";

    /// <summary>Map a bound record-lock phrase to the runtime <c>FileRecordLock</c> enum member (Phase 4d).</summary>
    public static string RuntimeRecordLock(BoundRecordLock l) => l switch
    {
        BoundRecordLock.WithLock => "FileRecordLock.WithLock",
        BoundRecordLock.WithNoLock => "FileRecordLock.WithNoLock",
        BoundRecordLock.IgnoringLock => "FileRecordLock.Ignoring",
        _ => "FileRecordLock.None",
    };

    /// <summary>The ONE lock-relevance predicate (§9.1.16): a statement routes through the runtime's governed
    /// verb entry when its file declares SHARING or LOCK MODE, or the statement itself carries a record-lock or
    /// RETRY phrase. Everything else keeps the plain entry — the pre-sharing emission byte-for-byte.</summary>
    public static bool LockGoverned(FileModel file, BoundRecordLock lockPhrase, RetrySpec? retry) =>
        file.Sharing != SharingMode.None || file.LockMode is not null
        || lockPhrase != BoundRecordLock.None || retry is not null;

    public void EmitClose(BoundClose c)
    {
        var w = ctx.Writer;
        foreach (var (file, kind) in c.Files)
        {
            w.Line($"{RuntimeApi.FileClose(FileKeyExpr(file), kind)};");
            EmitStoreFileStatus(file);
            EmitUseHook(file);
        }
    }

    /// <summary>UNLOCK file [RECORD[S]] (ISO §14.9.47, COBOL-2002): release the connector's record locks and set
    /// the I-O status (00, or 42 if not open). The two hooks let a USE declarative see the status like any I-O.</summary>
    public void EmitUnlock(BoundUnlock ul)
    {
        var w = ctx.Writer;
        w.Line($"{RuntimeApi.FileUnlock(FileKeyExpr(ul.File), ul.Records ? "true" : "false")};");
        EmitStoreFileStatus(ul.File);
        EmitUseHook(ul.File);
    }

    /// <summary>WRITE record [FROM x] [ADVANCING …] (ISO §14.9.46): a FROM operand first MOVEs into the record area,
    /// then the record's character image is written (plain, or with print-control advancing).</summary>
    public void EmitWrite(BoundWrite wr)
    {
        var w = ctx.Writer;
        if (wr.Unsupported is { } u) { w.Line(LoudStmt(u)); return; }
        if (wr.From is { } from) move.Emit(new BoundMove(from, [wr.Record]));
        string name = FileKeyExpr(wr.File);
        string image = OperandText.AsString(new BoundFieldOperand(wr.Record), num);
        if (wr.AfterAdvancing is { } aft && wr.Advancing is { } bfr)
        {
            // COBOL-2023 combined BEFORE AND AFTER ADVANCING (§14.9.51 GR25e/GR25f): present the line at the current
            // position, then advance by the BEFORE amount and by the AFTER amount (both after presentation; SR17
            // forbids PAGE, so neither is a form feed). LINAGE-COUNTER increments by before+after.
            w.Line($"{RuntimeApi.FileWriteBeforeAndAfter(name, image, LinesExpr(bfr.Lines!), LinesExpr(aft.Lines!))};");
        }
        else if (wr.Advancing is { } adv)
        {
            // Print-control writes keep the plain entry: an ADVANCING stream is a presentation surface, not a
            // record store — its lines carry no record-lock identity (§9.1.16 locks LOGICAL RECORDS).
            string lines = adv.Page ? "-1" : LinesExpr(adv.Lines!);
            w.Line($"{RuntimeApi.FileWriteAdvancing(name, image, lines, adv.Before ? "true" : "false")};");
        }
        else if (LockGoverned(wr.File, wr.Lock, wr.Retry))
        {
            // §9.1.16/§14.9.51 GR10-GR11 (P10 Step 8): the governed WRITE — single locking releases the
            // connector's prior lock, WITH LOCK locks the record written. Status lands on the connector.
            var (retryKind, retryAmount) = RenderRetry(wr.Retry);
            string lenArg = VaryingLengthArg(wr.File) ?? "-1";
            w.Line($"{RuntimeApi.FileWriteShared(name, image, lenArg, RuntimeRecordLock(wr.Lock), retryKind, retryAmount)};");
        }
        else
            w.Line($"{RuntimeApi.FileWrite(name, image, VaryingLengthArg(wr.File))};");
        EmitStoreFileStatus(wr.File);
        EmitUseHook(wr.File);
        // END-OF-PAGE branches (ISO §14.9.51 GR27b/GR28): an end-of-page WRITE is SUCCESSFUL — the branch runs
        // after the status store (status 00, so no USE declarative competes). The flag is read in the `if`
        // HEADER before either body runs: a branch body may WRITE the same file again (SQ208M's footing loop
        // inside the AT phrase), which clobbers the connector's per-write flag.
        if (wr.AtEop is not null || wr.NotAtEop is not null)
        {
            using (w.Block($"if ({RuntimeApi.FileEndOfPage(name)})"))
            {
                if (wr.AtEop is { } at) Statements.EmitStatementList(at);
            }
            if (wr.NotAtEop is { } not)
                using (w.Block("else"))
                    Statements.EmitStatementList(not);
        }
    }

    /// <summary>Render the governed sequential-READ call (§14.9.30 GR9–GR12/GR22 over the ordinal lock identity).</summary>
    private string EmitReadSharedCall(BoundRead rd, string name, string tmp)
    {
        var (retryKind, retryAmount) = RenderRetry(rd.Retry);
        return RuntimeApi.FileReadShared(name, RuntimeRecordLock(rd.Lock),
            rd.AdvancingOnLock ? "true" : "false", retryKind, retryAmount, tmp);
    }

    /// <summary>The record-length argument for a WRITE/REWRITE on a RECORD VARYING … DEPENDING file (ISO
    /// §13.18.43 GR13a — the DEPENDING item's content names the record length), or null when the statement
    /// writes the record's own size (GR13b/c — on a varying file the runtime takes the image's length; on a
    /// fixed file it pads to the record width).</summary>
    public string? VaryingLengthArg(FileModel file) =>
        file is { Varying.DependingName: not null, VaryingDependingItem: { } d } && refs.ResolveItem(d) is { } dep
            ? $"(int){RuntimeApi.TableOcc(PlaceRenderer.Read(dep))}" : null;

    /// <summary>After a SUCCESSFUL read of a RECORD VARYING … DEPENDING file, store the just-read record's length
    /// into the DEPENDING item (ISO §13.18.43 GR15; GR12 — an unsuccessful READ leaves it unchanged, so the call
    /// site sits inside the success branch).</summary>
    public void EmitReadLengthStore(FileModel file)
    {
        if (file is not { Varying.DependingName: not null, VaryingDependingItem: { } d }
            || refs.ResolveItem(d) is not { } dep) return;
        arith.StoreArith(dep, new NumX(RuntimeApi.FileLastReadLength(FileKeyExpr(file)), 0), CobolRounding.Truncation);
    }

    /// <summary>READ file [INTO x] [AT END …][NOT AT END …] (ISO §14.9.30): on success the record image is
    /// distributed into the FD record area (and, with INTO, MOVEd to the target); the AT END / NOT AT END imperative
    /// branches on the at-end condition. After an UNSUCCESSFUL read the record area's content is spec-UNDEFINED
    /// (§14.9.30 GR18 "unless otherwise specified…"); COBOL.NET's documented refinement is that the area is
    /// UNCHANGED — the store sits in the success branch only — extending the spec's own rule for every other
    /// unsuccessful I-O verb (REWRITE GR14 / WRITE GR15 / DELETE GR8 / START GR2 all say "unaffected"). The
    /// legacy's LOW-VALUE fill there was a byte-engine artifact (ST146A's golden is re-baselined over it,
    /// DEVLOG 570).</summary>
    public void EmitRead(BoundRead rd)
    {
        var w = ctx.Writer;
        if (rd.Unsupported is { } u) { w.Line(LoudStmt(u)); return; }
        string name = FileKeyExpr(rd.File);
        string tmp = $"__rd{ctx.Names.NextRead()}";
        // The read record is made available in the WHOLE record area — store through the LARGEST record's view
        // (FileModel.AreaRecord, ISO §13.4.2); a shorter Records[0] window would truncate the splice (ST111A).
        Place? area = rd.File.AreaRecord is { } ar ? refs.ResolveItem(ar) : null;
        // §9.1.16 record locking on the sequential organization (P10 Step 8): a lock-relevant READ routes
        // through the governed runtime entry — the next ordinal's pre-read conflict check (§14.9.30 GR9,
        // FPI unchanged on a 51 per GR10a), the GR11 lock discipline, and GR22 ADVANCING ON LOCK skip-scan.
        // Same bool contract as the plain read, so the two branches below are shared.
        string readCall = LockGoverned(rd.File, rd.Lock, rd.Retry)
            ? EmitReadSharedCall(rd, name, tmp)
            : RuntimeApi.FileRead(name, tmp);
        using (w.Block($"if ({readCall})"))
        {
            if (area is not null) EmitImageInto(area, tmp);
            EmitReadLengthStore(rd.File);   // §13.18.43 GR15 — the just-read length into DEPENDING
            EmitStoreFileStatus(rd.File);
            // READ … INTO is READ then MOVE the record area to the target (ISO §14.9.30 GR — group move).
            // §13.18.43 GR16a (a varying sender is the first DEPENDING-many bytes) is observationally identical
            // here: Read space-fills the area beyond the record, and the implicit MOVE of the category-
            // alphanumeric area space-fills the receiver the same way.
            if (rd.Into is { } into && area is not null)
                move.Emit(new BoundMove(new BoundFieldOperand(area), [into]));
            if (rd.NotAtEnd is { } not) Statements.EmitStatementList(not);
        }
        using (w.Block("else"))
        {
            EmitStoreFileStatus(rd.File);
            EmitUseHook(rd.File, atEndHandled: rd.AtEnd is not null);
            // The AT END imperative runs ONLY for the at-end status family (ISO 14.9.30 GR24c/d + 9.1.13.1 -
            // a 3x/4x failure is NOT an at-end condition; it reaches a USE declarative instead).
            if (rd.AtEnd is { } at)
                using (w.Block($"if ({RuntimeApi.FileStatus(name)}[0] == '1')"))
                    Statements.EmitStatementList(at);
        }
    }

    public void EmitRewrite(BoundRewrite rw)
    {
        var w = ctx.Writer;
        if (rw.Unsupported is { } u) { w.Line(LoudStmt(u)); return; }
        if (rw.From is { } from) move.Emit(new BoundMove(from, [rw.Record]));
        string image = OperandText.AsString(new BoundFieldOperand(rw.Record), num);
        // §9.1.16/§14.9.35 GR11-GR12 (P10 Step 8): a lock-relevant sequential REWRITE routes through the
        // governed runtime entry — the pre-operation conflict check on the last-read record (51 leaves the
        // record unrewritten) and the GR12 lock discipline. The status lands on the connector either way.
        if (LockGoverned(rw.File, rw.Lock, rw.Retry))
        {
            var (retryKind, retryAmount) = RenderRetry(rw.Retry);
            string lenArg = VaryingLengthArg(rw.File) ?? "-1";
            w.Line($"{RuntimeApi.FileRewriteShared(FileKeyExpr(rw.File), image, lenArg, RuntimeRecordLock(rw.Lock), retryKind, retryAmount)};");
        }
        else
            w.Line($"{RuntimeApi.FileRewrite(FileKeyExpr(rw.File), image, VaryingLengthArg(rw.File))};");
        EmitStoreFileStatus(rw.File);
        EmitUseHook(rw.File);
    }

    /// <summary>Store a read record image into the FD record area: a character-image group distributes via FromImage;
    /// an elementary / view record takes the image padded to its width.</summary>
    public void EmitImageInto(Place record, string imageExpr)
    {
        var w = ctx.Writer;
        // A character-image group record distributes the read image into its typed leaves via the generated FromImage.
        // A Tier-B view record (a multi-01 FD whose shared area is a synthesized REDEFINES) has no struct to call
        // FromImage on — its Read() is a string window — so splice the padded image into its backing via Write, as for
        // an elementary record. (Mirrors EmitGroupImage's RedefViewPlace handling.)
        if (record is not RedefViewPlace && record.Item.IsGroup)
        {
            // A mixed-usage record area distributes through the same generated FromImage — its BINARY/PACKED
            // leaves decode their zoned digit slices (the §13.18.60 USAGE GR4 implementor representation,
            // COBOLNET_DESIGN §14.4/§8.2: the record codec IS the AsImage/FromImage pair). Only a record with a
            // float/COMP-5/INDEX leaf stays the loud Tier-C island (§1.4) rather than emit a string-into-struct
            // assignment that fails the backend compile (the old ST133A/ST134A/SQ203A CS0029 class).
            if (!record.Item.IsImageCapable)
            {
                w.Line(LoudStmt(TierCIsland.Reason($"record area '{record.Item.CobolName}' contains float/COMP-5/INDEX leaves")));
                return;
            }
            w.Line($"{PlaceRenderer.Read(record)}.FromImage({RuntimeApi.StrStore(imageExpr, $"{record.Item.ImageWidth}")});");
            return;
        }
        w.Line(PlaceRenderer.Write(record, RuntimeApi.StrStore(imageExpr, $"{record.Item.Pic?.Length ?? record.Item.ImageWidth}")));
    }

    /// <summary>After an I/O verb, store the file's two-character I-O status into its FILE STATUS item (ISO §9.1.13),
    /// when the SELECT declared one.</summary>
    public void EmitStoreFileStatus(FileModel file)
    {
        // ISO §12.4.5.8 / §9.1.13.1 — the two-character status is stored into the FILE STATUS item as part of
        // the I/O statement's execution, BEFORE any exception processing.
        if (file.FileStatusName is null) return;   // no FILE STATUS clause — nothing to store
        // An INHERITED GLOBAL file stores into the OWNER's status item through the __outer chain
        // (§12.4.5.8.4 GR1 NOTE 1 — the item is updated by contained-program references to the global
        // file-name even though it is a LOCAL name of the owner; map built per unit in ProgramEmitter.EmitProgramClass).
        Place? place = callState.InheritedStatusPlace.TryGetValue(file, out var inherited)
            ? inherited
            : file.FileStatusItem is { } own ? refs.ResolveItem(own) : null;
        if (file.FileStatusItem is not { } item || place is null)
        {
            // §1.4 loud-guard doctrine: a DECLARED FILE STATUS name that did not resolve is never silent.
            ctx.Writer.Line(LoudStmt($"FILE STATUS item '{file.FileStatusName}' is not resolvable to storage (ISO §12.4.5.8)"));
            return;
        }
        string status = RuntimeApi.StrStore(RuntimeApi.FileStatus(FileKeyExpr(file)), $"{item.Pic?.Length ?? item.ImageWidth}");
        if (item.IsGroup && place is not RedefViewPlace)
        {
            // Same image-capability rule as every other group receiver (COBOLNET_DESIGN §14.4): a mixed-usage
            // status group distributes via FromImage; only a float/COMP-5/INDEX leaf stays loud (§1.4).
            if (!item.IsImageCapable)
            {
                ctx.Writer.Line(LoudStmt(TierCIsland.Reason(item, "FILE STATUS into group")));
                return;
            }
            // A GROUP status item fills without conversion through the image facility (§14.9.25.4 GR4 — the
            // CCVS shape `01 SQ-FS2-STATUS. 03 KEY-1 PIC X. 03 KEY-2 PIC X.`); a struct field cannot take the
            // raw string write.
            ctx.Writer.Line($"{PlaceRenderer.Read(place)}.FromImage({status});");
            return;
        }
        ctx.Writer.Line(PlaceRenderer.Write(place, status));
    }

    /// <summary>The C# <c>int</c> expression for an ADVANCING line count (a literal or a numeric data-name).</summary>
    private string LinesExpr(BoundOperand lines) => lines switch
    {
        BoundNumericLiteral n => $"(int)({n.Text})",
        BoundFieldOperand f => $"(int)({num.AsNum(f, ReceiverContext.None).Expr})",
        _ => "1",
    };
}
