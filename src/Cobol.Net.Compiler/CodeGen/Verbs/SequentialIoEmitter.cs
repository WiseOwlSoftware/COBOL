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
    public void EmitUseHook(FileModel file, bool atEndHandled = false, bool invalidKeyHandled = false,
        bool onExceptionHandled = false)
    {
        var w = ctx.Writer;
        if (ec.IoMaskFor(file) is not 0 and var mask)
        {
            int id = ctx.Names.NextEc();
            // The literals travel whenever ANY pair for this file carries WITH LOCATION; __locMask decides
            // PER RAISED NAME inside __IoCheckEc (§15.32.3 r1 per-condition — kb/Work R06).
            int locMask = ec.IoLocMaskFor(file);
            var (stmt, loc) = locMask != 0
                ? (CsLiteral(ecState.Info!.StatementName), CsLiteral(ecState.Info!.Location))
                : ("null", "null");
            w.Line($"int __ior{id} = __IoCheckEc({FileKeyExpr(file)}, {(atEndHandled ? "true" : "false")}, "
                + $"{(invalidKeyHandled ? "true" : "false")}, {(onExceptionHandled ? "true" : "false")}, {mask}, {locMask}, {stmt}, {loc});");
            w.Line($"if (__ior{id} >= 0) {{ __pc = __ior{id}; break; }}   // RESUME AT procedure-name (§14.9.33.4 GR3)");
            return;
        }
        if (!dispatch.UseDecls) return;
        // An ON EXCEPTION phrase is the statement's own handler for EVERY unsuccessful family (§14.9.10.4
        // GR20c) — no declarative runs, and the plain path has no EC to raise, so the hook is a no-op.
        if (onExceptionHandled) return;
        if (ecState.Active)
        {
            // The EC-model __IoCheck returns the declarative's RESUME action — consumed exactly like the
            // __IoCheckEc arm (the old void call discarded it, swallowing RESUME AT — kb/Work PB141).
            int id = ctx.Names.NextEc();
            w.Line($"int __ior{id} = __IoCheck({FileKeyExpr(file)}, {(atEndHandled ? "true" : "false")}, {(invalidKeyHandled ? "true" : "false")});");
            w.Line($"if (__ior{id} >= 0) {{ __pc = __ior{id}; break; }}   // RESUME AT procedure-name (§14.9.33.4 GR3)");
            return;
        }
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
                    w.Line($"{RuntimeApi.FileRegister(FileKeyExpr(file), CsLiteral(file.AssignTarget), $"{width}", "false", file.Optional ? "true" : "false", selectName: CsLiteral(file.SelectName))};");
                }
                continue;
            }
            bool lineSeq = file.Organization == FileOrganization.LineSequential;
            // A variable-length file registers its record-size bounds (ISO §13.18.43 GR9/GR10) — the connector
            // length-frames its records and enforces the GR14 '44' boundary checks.
            string vary = file.Varying is not null ? $", {file.VaryMin}, {file.VaryMax}" : "";
            w.Line($"{RuntimeApi.FileRegister(FileKeyExpr(file), CsLiteral(file.AssignTarget), $"{file.RecordWidth}", lineSeq ? "true" : "false", file.Optional ? "true" : "false", vary, CsLiteral(file.SelectName))};");
            EmitNationalAreaRegistration(w, file);
            EmitSharingRegistration(w, file);
        }
    }

    /// <summary>Install every LINAGE file's logical-page evaluator (ISO §13.18.34 GR6): ONE closure for both
    /// the literal (GR6a — a constant lambda) and data-name (GR6b — the connector re-reads at OPEN OUTPUT /
    /// ADVANCING PAGE / page overflow) forms. Emitted UNGUARDED in <c>__Activate</c> — installed on EVERY
    /// activation, so the evaluator on a run-unit-scoped connector always closes over the CURRENT activation's
    /// instance (kb/Work PB168: under the registration guard the FIRST activation's capture outlived it on a
    /// UnitStaticFiles unit's shared connector, and a LINKAGE/LOCAL-STORAGE LINAGE operand read a dead
    /// activation's data). Idempotent for a cached-singleton unit: the same delegate shape re-installs over
    /// itself.</summary>
    public void EmitLinageEvaluators(CodeWriter w)
    {
        foreach (var file in ctx.Data.Files)
            if (file.Linage is { } lin)
                w.Line($"{RuntimeApi.FileSetLinage(FileKeyExpr(file), $"() => ({LinageOpExpr(lin.Body)}, {LinageOpExpr(lin.Footing)}, {LinageOpExpr(lin.Top)}, {LinageOpExpr(lin.Bottom)})")};");
    }

    /// <summary>Install every ASSIGN … USING file's dynamic-assignment source (ISO §12.4.5.3 GR3 b — the connector is
    /// associated with "a physical file identified by the content of the data item referenced by data-name-1 in the
    /// runtime element that executes the OPEN, SORT, or MERGE statement"; §9.1.21, Dynamic file assignment). ONE
    /// closure per file, read by the runtime at every OPEN/SORT/MERGE — the association is a per-statement act, not a
    /// registration-time one, so the emitter installs a SOURCE and never a resolved path.
    /// <para>Emitted UNGUARDED beside <see cref="EmitLinageEvaluators"/>, for the same reason (kb/Work PB168): the
    /// closure captures THIS activation's instance, and a run-unit-scoped (RECURSIVE / UnitStaticFiles) connector
    /// registers only once, so a guarded install would leave a dead activation's data item naming the file.</para>
    /// <para>Every organization, SD excepted — an SD is the in-memory sort store with no host file, which is also
    /// why <see cref="EmitFileRegistration"/> skips it.</para></summary>
    public void EmitAssignSources(CodeWriter w)
    {
        foreach (var file in ctx.Data.Files)
        {
            if (file.IsSortMerge || file.AssignUsingItem is not { } item) continue;
            if (refs.ResolveItem(item) is not { } p) continue;   // unresolvable storage: COBOLNET1810 already fired
            // OperandText.FieldImage is THE operand-to-character-image renderer for every shape — an elementary
            // alphanumeric leaf reads its carrier, an alphanumeric group its generated AsImage(). No second arm.
            w.Line($"{RuntimeApi.FileSetAssignUsing(FileKeyExpr(file), $"() => {OperandText.FieldImage(p)}")};");
        }
    }

    /// <summary>⛔ §14.9.30.4 GR15's RECORD-AREA CATEGORY, told to the connector (kb/Work PB327): "If the
    /// record-area associated with file-name-1 is specified implicitly or explicitly as ALPHANUMERIC, a trailing
    /// space is defined to be the alphanumeric space character. If the record-area associated with file-name-1 is
    /// specified implicitly or explicitly as NATIONAL, a trailing space is defined to be the national space
    /// character." The connector pads a short record to the record width, and it is the only place that knows how
    /// long the physical record was — so it, not the emitter, must know which space to use. The area is national
    /// exactly when its OPERAND category is (an elementary national record, or a GROUP-USAGE NATIONAL group,
    /// §13.18.29.4 GR2b) — THE ONE category reader, never a re-derivation from the leaves. Emitted only for a
    /// national area, so every other program's registration is unchanged byte for byte.</summary>
    internal static void EmitNationalAreaRegistration(CodeWriter w, FileModel file)
    {
        if (file.AreaRecord is { } area && area.OperandPic is { Category: PicCategory.National })
            w.Line($"{RuntimeApi.FileRegisterNationalArea(FileKeyExpr(file))};");
    }

    /// <summary>Emit the RECORD-LOCKING registration for a file that declares a SHARING and/or LOCK MODE clause
    /// (Phase 4d M2-FILE-1) — it routes the file's READs through record-lock governance (§9.1.16) and records the
    /// SHARING clause's mode. Files with neither clause emit nothing: they have no LOCK MODE, so §12.4.5.9 GR1
    /// sets no record locks for them. They are NOT outside the sharing arbitration — every OPEN is arbitrated
    /// against ISO §14.9.27.4 Table 19 by <c>FileRegistry.SharedOpenAttempt</c> whether this call is emitted or
    /// not (kb/Work PB321).
    /// <para>⛔ A LOCK-MODE-ONLY FILE HAS NO SHARING MODE OF ITS OWN, so it registers <c>null</c> — the
    /// UNDETERMINED implementor default of §9.1.15 (<i>"If no specification is made in either location, the
    /// implementor defines the sharing mode in which the file is opened"</i>; a LOCK MODE clause is not such a
    /// specification). It used to register ALL OTHER, which answered kb/Work PB322's owner determination here, in
    /// the emitter, for one file shape only — while a clause-less file's default stayed undecided.</para></summary>
    private void EmitSharingRegistration(CodeWriter w, FileModel file)
    {
        if (file.Sharing == SharingMode.None && file.LockMode is null) return;
        string sharing = file.Sharing switch
        {
            SharingMode.NoOther => "FileSharing.NoOther",
            SharingMode.ReadOnly => "FileSharing.ReadOnly",
            SharingMode.AllOther => "FileSharing.AllOther",
            // SharingMode.None (LOCK-MODE-only): the undetermined implementor default — §9.1.15 / kb/Work PB322.
            _ => "(FileSharing?)null",
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
        foreach (var (file, mode, sharing, retry, noRewind, unsupported) in o.Files)
        {
            if (unsupported is { } u) { w.Line(LoudStmt(u)); continue; }
            // ⛔ PER FILE, NOT PER STATEMENT. §14.9.27.4 GR20 makes a multi-group OPEN equal to one separate
            // OPEN statement per file-name, "each hav[ing] the same open mode specification, the
            // sharing-phrase, retry-phrase, and REWIND phrase as specified in the OPEN statement" — i.e. the
            // phrases of ITS OWN group (§14.9.27.2 nests both inside the repeated group). A file whose group
            // carries neither phrase keeps the direct entry points, because the phrase-bearing facade REGISTERS
            // an unregistered connector's record-locking posture (§9.1.15's undetermined implementor default)
            // for the rest of the run, and doing that for a phrase-less file would contradict GR23's "file
            // sharing is completely specified in the file control entry" (kb/Work PB316). Both arms are
            // arbitrated against §14.9.27.4 Table 19 either way — that is not what distinguishes them
            // (kb/Work PB321).
            bool shared = sharing is not null || retry is not null;
            if (shared)
            {
                string modeEnum = RuntimeApi.FileOpenModeExpr(mode);
                var (retryKind, retryAmount) = RenderRetry(retry);
                string shHas = sharing is not null ? "true" : "false";
                string shVal = sharing is { } sm ? RuntimeSharing(sm) : "FileSharing.AllOther";
                // ⛔ THE NO REWIND PHRASE TRAVELS ON BOTH ARMS. SHARING/RETRY and NO REWIND are independent
                // phrases of one general format (§14.9.27.2), so an OPEN may carry both; a phrase honoured on
                // only the plain arm would report '07' for `OPEN INPUT F WITH NO REWIND` and '00' for the same
                // statement once a SHARING phrase was added — the two-arm dispatch this whole item is an
                // instance of (kb/Work PB317).
                w.Line($"{RuntimeApi.FileOpenShared(FileKeyExpr(file), $"{modeEnum}, {shHas}, {shVal}, {retryKind}, {retryAmount}, {(noRewind ? "true" : "false")}")};");
            }
            else
            {
                w.Line($"{RuntimeApi.FileOpen(FileKeyExpr(file), mode, noRewind)};");
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

    /// <summary>Render a bound RETRY phrase (ISO §14.7.9) to the runtime <c>(FileRetryKind, int amount)</c> pair.
    /// FOREVER carries no expression. The two that do are governed by DIFFERENT rounding rules and must keep
    /// separate renderings — this is one clause with two arms, not one helper serving both:
    /// <list type="bullet">
    /// <item>TIMES — §14.7.9.3 GR1: "if arithmetic-expression-1 does not evaluate to an integer, the value …
    /// is rounded up to the next whole number", so <c>RETRY 1.5 TIMES</c> is TWO re-attempts. That round-up is
    /// the shared rule <see cref="NumericRenderer.AlignRoundedUp"/> owns (ALLOCATE §14.9.3.4 GR1 is its only
    /// sibling).</item>
    /// <item>SECONDS — §14.7.9.3 GR2 instead stores the timeout period through an implicit COMPUTE WITHOUT the
    /// ROUNDED phrase into a 9(n)V9(m) temporary, i.e. TRUNCATION at the implementor's m. COBOL.NET's
    /// determination is n = 1, m = 0 with a maximum meaningful value of 0 (A.1 item 166, docs/CONFORMANCE.md
    /// §7), so the amount is clamped to a zero-length period at the runtime and is inert — but it is still
    /// PASSED, because it is the GR4a screen input, and it is still rendered by the truncating
    /// <see cref="NumericRenderer.Align"/>, because merging the two arms would re-merge the two rules.</item>
    /// </list></summary>
    public (string Kind, string Amount) RenderRetry(RetrySpec? retry) => retry switch
    {
        null => ("FileRetryKind.None", "0"),
        { Kind: RetryKind.Forever } => ("FileRetryKind.Forever", "0"),
        { Kind: RetryKind.Seconds } => ("FileRetryKind.Seconds", RetrySeconds(retry.Amount)),
        _ => ("FileRetryKind.Times", RetryTimes(retry.Amount)),
    };

    /// <summary>§14.7.9.3 GR1 — the n-TIMES count, ROUNDED UP to the next whole number.</summary>
    private string RetryTimes(BoundExpr? amount) =>
        amount is null ? "0"
        : $"(int)({NumericRenderer.AlignRoundedUp(num.Render(amount, ReceiverContext.None))})";

    /// <summary>§14.7.9.3 GR2 — the timeout period, truncated to the implementor's m (= 0).</summary>
    private string RetrySeconds(BoundExpr? amount) =>
        amount is null ? "0"
        : $"(int)({NumericRenderer.Align(num.Render(amount, ReceiverContext.None), 0)})";

    /// <summary>Map a bound lock-RETENTION phrase (§14.9.30.2 bracket 2) to the runtime <c>FileRecordLock</c>
    /// enum member (Phase 4d). IGNORING LOCK is NOT here — it is bracket 1 and travels as its own bool argument
    /// (kb/Work PB331), because §5.2.6.1 lets a READ select from both brackets at once.</summary>
    public static string RuntimeRecordLock(BoundRecordLock l) => l switch
    {
        BoundRecordLock.WithLock => "FileRecordLock.WithLock",
        BoundRecordLock.WithNoLock => "FileRecordLock.WithNoLock",
        _ => "FileRecordLock.None",
    };

    /// <summary>The ONE lock-relevance predicate (§9.1.16): a statement routes through the runtime's governed
    /// verb entry when its file declares SHARING or LOCK MODE, or the statement itself carries a lock-retention
    /// phrase, an IGNORING LOCK phrase or a RETRY phrase. Everything else keeps the plain entry — the
    /// pre-sharing emission byte-for-byte. (<paramref name="ignoringLock"/> defaults false for the verbs whose
    /// printed formats have no such phrase — WRITE, REWRITE and DELETE.)</summary>
    public static bool LockGoverned(FileModel file, BoundRecordLock lockPhrase, RetrySpec? retry,
        bool ignoringLock = false) =>
        file.Sharing != SharingMode.None || file.LockMode is not null
        || lockPhrase != BoundRecordLock.None || retry is not null || ignoringLock;

    public void EmitClose(BoundClose c)
    {
        var w = ctx.Writer;
        foreach (var (file, kind) in c.Files)
        {
            // §14.9.6.4 GR5: closing a report file while any associated report is ACTIVE (INITIATEd, not
            // TERMINATEd) still completes the CLOSE — and sets EC-REPORT-NOT-TERMINATED to exist (nonfatal;
            // the EC was catalogued and raised NOWHERE, kb/Work PB141). Read the active state BEFORE the
            // close; the checking state is the statement's >>TURN (bind-time, BoundClose).
            if (c.ReportNotTerminatedCheck
                && ctx.Data.Reports.Where(r => ReferenceEquals(r.File, file)).ToList() is { Count: > 0 } reports)
            {
                using (w.Block($"if ({string.Join(" || ", reports.Select(r => $"__RPT_{r.CsIndex}.IsActive"))})"))
                {
                    w.Line("ExceptionState.Set(\"EC-REPORT-NOT-TERMINATED\", fatal: false);   // §14.9.6.4 GR5");
                    int id = ctx.Names.NextEc();
                    w.Line($"int __r{id} = {ec.EcDispatchExpr("\"EC-REPORT-NOT-TERMINATED\"", "\"\"")};");
                    w.Line($"if (__r{id} >= 0) {{ __pc = __r{id}; break; }}");
                }
            }
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
        string image = OperandText.RecordAreaImage(wr.Record);   // THE ONE record-area channel (kb/Work PB327)
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

    /// <summary>Render the governed sequential-organization READ call (§14.9.30.4 GR9–GR12 over the ordinal lock
    /// identity, plus the GR22 skip-scan) as the plain read's BOOL. The runtime entry is the ONE governed
    /// Format-1 read shared with the keyed emitter, so <c>previous</c> is passed explicitly — always false here,
    /// because <see cref="BoundRead"/> carries no direction yet (kb/Work PB334).</summary>
    private string EmitReadSharedCall(BoundRead rd, string name, string tmp)
    {
        var (retryKind, retryAmount) = RenderRetry(rd.Retry);
        return RuntimeApi.FileReadSharedOk(name, "false", RuntimeRecordLock(rd.Lock),
            rd.AdvancingOnLock ? "true" : "false", rd.IgnoringLock ? "true" : "false",
            retryKind, retryAmount, tmp);
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
        string readCall = LockGoverned(rd.File, rd.Lock, rd.Retry, rd.IgnoringLock)
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
        string image = OperandText.RecordAreaImage(rw.Record);   // THE ONE record-area channel (kb/Work PB327)
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
        if (record.Item.IsGroup)
        {
            // A STORAGE-boundary write: the WHOLE record image, whatever the record area's storage shape (a
            // record struct, a Tier-B view over a shared area, an occurs-depending record — kb/Work PB80) — the
            // ONE full-image store. A mixed-usage record area distributes through the generated FromImage (its
            // BINARY/PACKED/COMP-5/float leaves decode their byte slices — COBOLNET_DESIGN §14.4/§8.2); a
            // record that is variable-length or pointer/object-leafed is the loud Tier-C island inside the helper (§1.4; kb/Work PB164 + R40).
            // ⛔ THE FIT WIDTH IS THE RECORD'S BYTE EXTENT (kb/Work PB327) — §14.9.30.4 GR14/GR15 measure a
            // record in BYTES, and FromImage distributes a byte image. ByteWidth IS ImageWidth for every leaf
            // kind but NATIONAL, so this is byte-identical for national-free records.
            w.Line(PlaceRenderer.WriteFullGroupImage(record, RuntimeApi.StrStore(imageExpr, $"{record.Item.ByteWidth}"),
                $"record area '{record.Item.CobolName}' read"));
            return;
        }
        // ⛔ AN ELEMENTARY NATIONAL RECORD AREA DECODES ITS BYTE PAIRS (kb/Work PB327) — the receiving twin of
        // OperandText.RecordAreaImage's national arm, through the ONE inverse CobolBits.NatReadWindow. It is NOT
        // fitted to the byte width first: NatReadWindow fills a position the image is too short to hold with the
        // NATIONAL space, which is precisely §14.9.30.4 GR15's "If the record-area associated with file-name-1 is
        // specified implicitly or explicitly as national, a trailing space is defined to be the national space
        // character" — a byte-level space pad would have manufactured U+2020 instead.
        if (Binding.Model.NationalWindow.PositionsOf(record.Item) is { } natPositions)
        {
            w.Line(PlaceRenderer.Write(record, RuntimeApi.NatReadWindow(imageExpr, "0", $"{natPositions}")));
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
            // status group distributes via FromImage; only a VARIABLE-LENGTH group or a group with a pointer/object-class leaf stays loud (§1.4; kb/Work PB164 + R40).
            if (!item.IsImageCapable)
            {
                ctx.Writer.Line(LoudStmt(TierCIsland.Reason(item, "FILE STATUS into group")));
                return;
            }
            // A GROUP status item fills without conversion through the image facility (§14.9.25.4 GR4 — the
            // CCVS shape `01 SQ-FS2-STATUS. 03 KEY-1 PIC X. 03 KEY-2 PIC X.`); a struct field cannot take the
            // raw string write.
            ctx.Writer.Line(PlaceRenderer.WriteFullGroupImage(place, status, "FILE STATUS group"));   // the ONE full-image store (kb/Work PB80)
            return;
        }
        ctx.Writer.Line(PlaceRenderer.Write(place, status));
    }

    /// <summary>The C# <c>int</c> expression for an ADVANCING line count (a literal or a numeric data-name).</summary>
    private string LinesExpr(BoundOperand lines) => lines switch
    {
        BoundNumericLiteral n => $"(int)({n.Text})",
        BoundOperandError e => LoudValue("int", e.Feature),
        // An integer identifier (§14.9.47.3 SR — identifier-2 an integer item) reads by VALUE through the ONE integer
        // landing (kb/Work PB86's sweep: the raw read mis-counted a P-scaled item; the `_ => "1"` default was a swallow).
        _ => $"(int)({NumericRenderer.Align(num.AsNum(lines, ReceiverContext.None), 0)})",
    };
}
