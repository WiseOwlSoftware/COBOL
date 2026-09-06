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
            // ⛔ THE RECORD-LESS ARM IS DECIDED ONCE, ABOVE THE ORGANIZATION SPLIT (kb/Work PB345). It used to be
            // asked TWICE and answered differently — here it registered only a REPORT file, and
            // KeyedIoEmitter.EmitRegistration returned outright — so a legal record-description-less FD
            // (ISO §13.4.5.3 SR3) produced NO file connector on either arm and its first I-O verb aborted the run
            // unit. Since PB345 every such FD carries §14.9.30.4 GR6's implied record description, synthesized at
            // bind time (DataBinder.MaterializeImpliedRecord), so exactly TWO shapes reach here with no record:
            //   • a REPORT FILE, which legally has none (ISO §9.1.22 / §13.18.46 / §13.4.5.3 SR8) and MUST still
            //     register, or its OPEN falls through to the keyed registries and the report engine's writes go
            //     into a void (the silent-OPEN-no-op hazard, COBOLNET_REPORT_WRITER_DESIGN §7); its record width
            //     is the widest hosted report's line width;
            //   • a SELECT with NO file description entry at all, which §13.4.5.4 GR1 leaves connector-less and
            //     the front end has already diagnosed — there is nothing to register.
            if (file.Records.Count == 0)
            {
                if (file.ReportNames.Count > 0)
                {
                    int width = Math.Max(1, ctx.Data.Reports
                        .Where(r => ReferenceEquals(r.File, file))
                        .Select(r => r.LineWidth).DefaultIfEmpty(1).Max());
                    w.Line($"{RuntimeApi.FileRegister(FileKeyExpr(file), CsLiteral(file.AssignTarget), $"{width}", "false", file.Optional ? "true" : "false", selectName: CsLiteral(file.SelectName))};");
                }
                continue;
            }
            if (!file.IsSequential) { keyedIo.EmitRegistration(w, file); EmitSharingRegistration(w, file); continue; }   // relative/indexed connectors
            bool lineSeq = file.Organization == FileOrganization.LineSequential;
            // A variable-length file registers its record-size bounds (ISO §13.18.43 GR9/GR10) — the connector
            // length-frames its records and enforces the GR14 '44' boundary checks.
            string vary = file.Varying is not null ? $", {file.VaryMin}, {file.VaryMax}" : "";
            w.Line($"{RuntimeApi.FileRegister(FileKeyExpr(file), CsLiteral(file.AssignTarget), $"{file.RecordWidth}", lineSeq ? "true" : "false", file.Optional ? "true" : "false", vary, CsLiteral(file.SelectName))};");
            EmitNationalAreaRegistration(w, file);
            EmitSharingRegistration(w, file);
        }
    }

    /// <summary>⛔ THE EXECUTING RUNTIME ELEMENT'S OWN FILE-STATEMENT OPERANDS — the <c>assign, assignDynamic,
    /// page</c> triple every emitted OPEN (and the SORT/MERGE implicit opens) passes, rendered HERE, inside the
    /// element that runs the statement (kb/Work PB673).
    /// <para><b>The rule.</b> ISO §12.4.5.3 GR3 makes the association an act of the STATEMENT — <i>"The
    /// association occurs at the time of execution of an OPEN, SORT, or MERGE statement that referenced
    /// file-name-1"</i> — and both arms name the element that performs it: a) <i>"identified by the specification
    /// of device-name-1 or the value of literal-1 in the source unit that specifies the OPEN, SORT, or MERGE
    /// statement"</i>, b) <i>"identified by the content of the data item referenced by data-name-1 in the runtime
    /// element that executes the OPEN, SORT, or MERGE statement"</i>. §13.18.34 GR6 b) 1 does the same for the
    /// LINAGE operands at the completion of an OPEN OUTPUT.</para>
    /// <para><b>Why it cannot be installed on the connector.</b> A file connector is not per-element and not
    /// per-activation: an EXTERNAL one is a single object per run unit shared by every describing element
    /// (§13.18.22.4 GR4 a, whose file control entries §12.4.5.3 GR1 b requires only to be CONSISTENT — unlike
    /// GR1 i's FILE STATUS, which must name the same external item), and a RECURSIVE unit's internal one is
    /// unit-scoped last-used state across activations (§8.6.4 / §14.6.2.3.3, kb/Work PB168). A connector-held
    /// closure therefore answered with whichever element/activation installed it LAST, which is the executing one
    /// only by accident. Rendering the operands at the statement makes the right answer structural: the emitting
    /// unit IS the executing element, and the emitted expression reads the LIVE activation's storage.</para>
    /// <para>An unresolvable USING operand falls back to the static specification — <c>COBOLNET1810</c>/
    /// <c>COBOLNET1811</c> (§12.4.5.2 SR7) have already rejected the program, so this only keeps emission
    /// total.</para></summary>
    internal string ExecutingElementArgs(FileModel file)
    {
        string page = LinageArg(file);
        if (file.AssignUsingItem is { } item && refs.ResolveItem(item) is { } p)
            // OperandText.FieldImage is THE operand-to-character-image renderer for every shape — an elementary
            // alphanumeric leaf reads its carrier, an alphanumeric group its generated AsImage(). No second arm.
            return RuntimeApi.ExecutingElementArgs(OperandText.FieldImage(p), assignDynamic: true, page);
        return RuntimeApi.ExecutingElementArgs(CsLiteral(file.AssignTarget), assignDynamic: false, page);
    }

    /// <summary>The executing element's LINAGE operand values (ISO §13.18.34 GR6) as a <c>LinagePage</c>, or
    /// <c>null</c> for an FD with no LINAGE clause. ONE rendering for both operand forms — GR6 a) literals fold to
    /// constants, GR6 b) data-names render this element's field reads, evaluated by the runtime at the three GR6 b)
    /// times (OPEN OUTPUT completion, WRITE ADVANCING PAGE, page-overflow WRITE). Passed by every OPEN and every
    /// sequential WRITE entry, which is what makes the OO path (a class's FD) carry LINAGE for free: the statement
    /// supplies it, so there is no per-registration install for a second emit path to forget.</summary>
    internal string LinageArg(FileModel file) =>
        file.Linage is { } lin
            ? RuntimeApi.LinagePageExpr(LinageOpExpr(lin.Body), LinageOpExpr(lin.Footing),
                LinageOpExpr(lin.Top), LinageOpExpr(lin.Bottom))
            : "null";

    /// <summary>⛔ §14.9.30.4 GR15's RECORD-AREA CATEGORY, told to the connector (kb/Work PB327): "If the
    /// record-area associated with file-name-1 is specified implicitly or explicitly as ALPHANUMERIC, a trailing
    /// space is defined to be the alphanumeric space character. If the record-area associated with file-name-1 is
    /// specified implicitly or explicitly as NATIONAL, a trailing space is defined to be the national space
    /// character." The connector pads a short record to the record width, and it is the only place that knows how
    /// long the physical record was — so it, not the emitter, must know which space to use. The area is national
    /// exactly when its OPERAND category is (an elementary national record, or a GROUP-USAGE NATIONAL group,
    /// §13.18.29.4 GR2b) — THE ONE category reader, never a re-derivation from the leaves. Emitted only for a
    /// national area, so every other program's registration is unchanged byte for byte.
    /// <para>⛔ ANY record description of the FD, not merely the WIDEST one (kb/Work PB329). §13.18.33.4 GR3 —
    /// "Multiple level 1 entries subordinate to a FD or SD entry represent implicit redefinitions of the same
    /// area" (§9.1.2's NOTE says the same) — makes every record description of an FD describe the SAME record
    /// area, so an area one of them declares national IS "specified … as national"; and §14.9.51.4 GR21/GR22 —
    /// the trailing-space rule <c>TrimRecordEnd</c>
    /// implements — key on <i>record-name-1</i>, so a WRITE naming the national record must shed national
    /// spaces whatever the sibling descriptions say. Keying on <see cref="FileModel.AreaRecord"/> alone read the
    /// category off whichever description happened to be widest: <c>01 L-REC PIC N(4). 01 L-BYTES PIC X(8).</c>
    /// (the shape of golden <c>2002/pb327_national_line_sequential_fill</c>) registered NOTHING, so that file's
    /// line-sequential WRITE shed one 0x20 with <c>string.TrimEnd</c> and left a SEVEN-byte line ending in half
    /// a national position — the exact trap PB327's own header describes — and the READ then re-padded it
    /// alphanumerically back to the same eight bytes, so the defect was invisible in the golden's output.
    /// It became visible the moment §14.9.51.4 GR23's character-set test needed the same flag.</para>
    /// <para>⚠ RESIDUAL, and deliberately not modelled here: the flag is per-CONNECTOR while GR21/GR22 are per
    /// record-name-1, so an FD carrying BOTH a national and an alphanumeric record description answers national
    /// for both. Distinguishing them means carrying the category on the statement; no corpus program writes the
    /// alphanumeric sibling of a national area, and inventing a second national axis to say so would be the
    /// two-mechanism anti-pattern.</para></summary>
    internal static void EmitNationalAreaRegistration(CodeWriter w, FileModel file)
    {
        if (file.Records.Any(r => r.OperandPic is { Category: PicCategory.National }))
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
                w.Line($"{RuntimeApi.FileOpenShared(FileKeyExpr(file), $"{modeEnum}, {shHas}, {shVal}, {retryKind}, {retryAmount}, {(noRewind ? "true" : "false")}, {ExecutingElementArgs(file)}")};");
            }
            else
            {
                w.Line($"{RuntimeApi.FileOpen(FileKeyExpr(file), mode, noRewind, ExecutingElementArgs(file))};");
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

    // ⛔ THERE IS NO COMPILE-TIME LOCK-GOVERNANCE PREDICATE, AND THERE MUST NEVER BE ONE AGAIN (kb/Work PB683).
    // Whether a connector is open FOR FILE SHARING is a RUN-TIME fact: ISO §9.1.15 — "The SHARING phrase on an
    // OPEN statement overrides the SHARING clause in the file control entry for establishing the sharing mode"
    // — so `OPEN INPUT SHARING WITH READ ONLY F` makes F a sharing participant whatever its SELECT says, and no
    // property of the file control entry or of the READ/WRITE/REWRITE/DELETE statement can see it. The emitter
    // used to guess it from (SHARING clause | LOCK MODE clause | the statement's own lock/RETRY/IGNORING
    // phrases) and routed every unphrased verb on such a connector to the UNGOVERNED entry, which reads a record
    // another connector has locked with '00' where §14.9.30.4 GR9/GR10 b) require the record operation conflict
    // status '51' — and writing `RETRY 0 TIMES`, a behavioural no-op by §14.7.9.3 GR4 a), flipped the answer.
    // Every record verb now renders its GOVERNED runtime entry unconditionally; FileRegistry's
    // `_connectorShares` probe is the ONE place governance is decided, one layer below, where the OPEN's own
    // phrase has already been recorded. The ungoverned entries no longer exist to route to.

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

    /// <summary>⛔ THE ONE RENDERER OF ISO §9.1.14's transfer-of-control contract, over a captured status local —
    /// shared by every I-O verb that can carry the phrase, sequential arm and keyed arm alike (this class is the
    /// home of the file-I/O common services KeyedIo consumes; a second copy there was how the sequential WRITE
    /// came to have no branch at all — kb/Work PB691). The INVALID KEY imperative runs on the <c>'2x'</c> family
    /// ONLY (§9.1.13.5 — 3x/4x route to exception processing, never this branch); the NOT INVALID KEY imperative
    /// ONLY on successful completion (<c>'0x'</c>, §9.1.14 final rule item 2). On a sequential-organization file
    /// no '2x' status is reachable at all (§9.1.13.5 items 1–4 all name a relative or indexed file), so the
    /// INVALID arm renders as a branch that provably never fires — dead, never silently rerouted.</summary>
    public void EmitInvalid(string st, KeyedInvalidKey? ik)
    {
        if (ik is null) return;
        var w = ctx.Writer;
        if (ik.Invalid is { } inv)
        {
            using (w.Block($"if ({st}[0] == '2')")) Statements.EmitStatementList(inv);
            if (ik.NotInvalid is { } not)
                using (w.Block($"else if ({st}[0] == '0')")) Statements.EmitStatementList(not);
        }
        else if (ik.NotInvalid is { } not)
            using (w.Block($"if ({st}[0] == '0')")) Statements.EmitStatementList(not);
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
        // ⛔ ONE CALL FOR EVERY WRITE SHAPE (kb/Work PB683). §9.1.16/§14.9.51 GR10-GR11 (P10 Step 8): the governed
        // WRITE — single locking releases the connector's prior lock, WITH LOCK locks the record written — and
        // both are ALL FILES rules, so the ADVANCING phrases are the statement's PRESENTATION shape and travel
        // as an argument, never as a choice of runtime entry. This used to be a three-arm dispatch whose two
        // print-control arms rendered `WriteAdvancing`/`WriteBeforeAndAfter`, which have no lock or RETRY
        // parameter, so `WRITE R AFTER ADVANCING 1 LINE WITH LOCK RETRY 5 TIMES` — one legal statement of
        // §14.9.51.2's Format 1, which prints the ADVANCING phrase, the retry-phrase and the WITH LOCK bracket
        // together — silently dropped both phrases. UNCONDITIONAL: the runtime falls through to the same plain
        // body for a connector that is not sharing-active, which is the decision made where the OPEN's own
        // SHARING phrase is visible (§9.1.15). Status lands on the connector either way.
        var (retryKind, retryAmount) = RenderRetry(wr.Retry);
        string lenArg = VaryingLengthArg(wr.File) ?? "-1";
        w.Line($"{RuntimeApi.FileWriteShared(name, image, lenArg, RuntimeRecordLock(wr.Lock), retryKind, retryAmount, LinageArg(wr.File), AdvanceArg(wr))};");
        // The §9.1.14 status SNAPSHOT for a --permissive INVALID KEY phrase (kb/Work PB691). Taken HERE, before
        // the FILE STATUS store and the USE hook, for the same reason the end-of-page flag is read in the `if`
        // header below: a declarative or a phrase body may operate on this same connector and move its status.
        // It reads the CONNECTOR rather than capturing the entry's return value — that is exactly what §9.1.14
        // means by "the I-O status of the file connector associated with the statement", and it keeps the
        // snapshot independent of which runtime entry the write rendered. Emitted ONLY when the forbidden phrase
        // is present, so the legal Format-1 WRITE renders byte-for-byte as before.
        string? wst = null;
        if (wr.InvalidKey is not null)
        {
            wst = $"__wst{ctx.Names.NextKeyedSeq()}";
            w.Line($"var {wst} = {RuntimeApi.FileStatus(name)};");
        }
        EmitStoreFileStatus(wr.File);
        // invalidKeyHandled stays FALSE even with the phrase present: §9.1.14 item 2 suppresses exception
        // processing only "if the invalid key condition exists", and on a sequential organization it never can
        // (§9.1.13.5 items 1–4 all name a relative or indexed file), so every unsuccessful status here is a
        // §9.1.14 final-rule item 1 completion that the declarative must still see.
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
        // The forbidden-but-tolerated INVALID KEY pair, through THE ONE §9.1.14 renderer the keyed arm uses.
        // Last, after the END-OF-PAGE branches: both are end-of-statement phrase transfers, and GR27b's EOP
        // imperative belongs to the WRITE's own general rules while §9.1.14 is the outer transfer contract.
        if (wst is not null) EmitInvalid(wst, wr.InvalidKey);
    }

    /// <summary>The ADVANCING phrases of ONE WRITE statement as the runtime's <c>WriteAdvance</c> descriptor
    /// (ISO §14.9.51.2 Format 1 — the print-control bracket). Three shapes, one argument: no phrase; a single
    /// BEFORE/AFTER phrase, whose <c>-1</c> line count is ADVANCING PAGE; and COBOL-2023's combined
    /// <c>BEFORE ADVANCING n AFTER ADVANCING m</c> (§14.9.51.4 GR25 e/f), where the record is presented once at
    /// the current line and the medium then advances by both amounts (SR17 forbids PAGE there, so neither is a
    /// form feed) and LINAGE-COUNTER increments by n+m.</summary>
    private string AdvanceArg(BoundWrite wr)
    {
        if (wr.AfterAdvancing is { } aft && wr.Advancing is { } bfr)
            return $"new WriteAdvance(WriteAdvanceKind.BeforeAndAfter, {LinesExpr(bfr.Lines!)}, {LinesExpr(aft.Lines!)})";
        if (wr.Advancing is { } adv)
            return $"new WriteAdvance(WriteAdvanceKind.{(adv.Before ? "Before" : "After")}, "
                + $"{(adv.Page ? "-1" : LinesExpr(adv.Lines!))}, 0)";
        return "WriteAdvance.None";
    }

    /// <summary>Render the governed sequential-organization READ call (§14.9.30.4 GR9–GR12 over the ordinal lock
    /// identity, plus the GR22 skip-scan) as the plain read's BOOL. The runtime entry is the ONE governed
    /// Format-1 read shared with the keyed emitter, so <c>previous</c> is passed explicitly — taken from the
    /// <see cref="BoundRead.Kind"/> §14.9.30.4 GR19 carries (kb/Work PB334).</summary>
    private string EmitReadSharedCall(BoundRead rd, string name, string tmp)
    {
        var (retryKind, retryAmount) = RenderRetry(rd.Retry);
        return RuntimeApi.FileReadSharedOk(name, rd.Kind == ReadKind.Previous ? "true" : "false", RuntimeRecordLock(rd.Lock),
            rd.AdvancingOnLock ? "true" : "false", rd.IgnoringLock ? "true" : "false",
            retryKind, retryAmount, tmp);
    }

    /// <summary>The resolved <c>RECORD VARYING … DEPENDING ON</c> item of <paramref name="file"/> (ISO §13.18.43
    /// data-name-1), or null when the phrase is absent or the name did not resolve. ⛔ THE ONE resolution: GR13 a)
    /// (the length WRITTEN), GR15 (the length READ BACK) and GR16 a) (the INTO sending size) are three rules over
    /// the SAME data item, and they were re-spelling the same four-part pattern independently.</summary>
    public Place? VaryingDepending(FileModel file) =>
        file is { Varying.DependingName: not null, VaryingDependingItem: { } d } ? refs.ResolveItem(d) : null;

    /// <summary>The record-length argument for a WRITE/REWRITE on a RECORD VARYING … DEPENDING file (ISO
    /// §13.18.43 GR13a — the DEPENDING item's content names the record length), or null when the statement
    /// writes the record's own size (GR13b/c — on a varying file the runtime takes the image's length; on a
    /// fixed file it pads to the record width).</summary>
    public string? VaryingLengthArg(FileModel file) =>
        VaryingDepending(file) is { } dep ? $"(int){RuntimeApi.TableOcc(PlaceRenderer.Read(dep))}" : null;

    /// <summary>After a SUCCESSFUL read of a RECORD VARYING … DEPENDING file, store the just-read record's length
    /// into the DEPENDING item (ISO §13.18.43 GR15; GR12 — an unsuccessful READ leaves it unchanged, so the call
    /// site sits inside the success branch).</summary>
    public void EmitReadLengthStore(FileModel file)
    {
        if (VaryingDepending(file) is not { } dep) return;
        arith.StoreArith(dep, new NumX(RuntimeApi.FileLastReadLength(FileKeyExpr(file)), 0), CobolRounding.Truncation);
    }

    /// <summary>THE sending operand of every <c>READ … INTO</c> / <c>RETURN … INTO</c> implicit MOVE — built in
    /// ONE place so the three INTO arms (sequential READ, keyed READ, sort RETURN) cannot drift (kb/Work PB339;
    /// the emitter's own comment used to argue the padded area was "observationally identical", which holds only
    /// for a LEFT-justified receiver).
    /// <para>A variable-length RECORD clause makes the sender <see cref="BoundCurrentRecord"/> — the record area
    /// sliced to the §13.18.43.4 GR16 byte count, and, for FORMAT 2 only, an alphanumeric group move by
    /// §14.9.30.4 GR4 b)'s explicit designation. A FIXED-length file keeps the plain record-area operand: its
    /// current record IS the whole area (§13.18.43.4 GR6 — integer-1 bytes in every record), including the
    /// short-final-record '04' case of §14.9.30.4 GR14, where the area right of the last valid character is
    /// undefined rather than short.</para></summary>
    public static BoundOperand IntoSender(FileModel file, Place area, Place? depending) =>
        file.Varying is { } v
            ? new BoundCurrentRecord(area, file, depending, v.VaryingClause)
            : new BoundFieldOperand(area);

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
        // §9.1.16 record locking on the sequential organization (P10 Step 8): EVERY READ routes through the
        // governed runtime entry — the next ordinal's pre-read conflict check (§14.9.30 GR9, FPI unchanged on a
        // 51 per GR10a), the GR11 lock discipline, and the GR22 ADVANCING ON LOCK skip-scan. Unconditional
        // (kb/Work PB683): the runtime falls through to the plain retrieval for a connector that is not
        // sharing-active, and only the runtime can see an OPEN statement's own SHARING phrase (§9.1.15).
        // §14.9.30.4 GR19's read kind rides INSIDE that one call as the direction of the retrieval; GR21's
        // sequential-file rules b)/c) then select the record NUMBER from it (kb/Work PB334). With one call shape
        // there is no longer a second place to drop it — which is how `READ … PREVIOUS` became a forward read.
        string readCall = EmitReadSharedCall(rd, name, tmp);
        using (w.Block($"if ({readCall})"))
        {
            if (area is not null) EmitImageInto(area, tmp);
            EmitReadLengthStore(rd.File);   // §13.18.43 GR15 — the just-read length into DEPENDING
            EmitStoreFileStatus(rd.File);
            // READ … INTO is READ then MOVE THE CURRENT RECORD to the target (ISO §14.9.30.4 GR4 b)). The sender
            // is the record sliced to its §13.18.43.4 GR16 byte count, NOT the padded area — through the ONE
            // IntoSender builder the keyed READ and the sort RETURN also use (kb/Work PB339).
            if (rd.Into is { } into && area is not null)
                move.Emit(new BoundMove(IntoSender(rd.File, area, VaryingDepending(rd.File)), [into]));
            if (rd.NotAtEnd is { } not) Statements.EmitStatementList(not);
            // §14.9.30.4 GR13c — on a successful READ "control is transferred to the end of the READ statement,
            // or, if the NOT AT END phrase or NOT INVALID KEY phrase is specified, to imperative-statement-2".
            // The phrase is never conforming source on this arm (Format 1 has no INVALID KEY bracket — the
            // binder reports COBOLNET1720), so this renders only under --permissive, where the bind stands and
            // the block has to MEAN something rather than vanish. There is deliberately NO invalid-key arm: a
            // sequential-organization READ raises no '2x' status (§9.1.13.5), so the condition cannot exist.
            if (rd.InvalidKey?.NotInvalid is { } notInvalid) Statements.EmitStatementList(notInvalid);
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
        // §9.1.16/§14.9.35 GR11-GR12 (P10 Step 8): EVERY sequential REWRITE routes through the governed runtime
        // entry — the pre-operation conflict check on the last-read record (51 leaves the record unrewritten)
        // and the GR12 lock discipline. Unconditional (kb/Work PB683): the runtime falls through to the plain
        // body for a connector that is not sharing-active. The status lands on the connector either way.
        var (retryKind, retryAmount) = RenderRetry(rw.Retry);
        string rwLenArg = VaryingLengthArg(rw.File) ?? "-1";
        w.Line($"{RuntimeApi.FileRewriteShared(FileKeyExpr(rw.File), image, rwLenArg, RuntimeRecordLock(rw.Lock), retryKind, retryAmount)};");
        // The §9.1.14 status snapshot for a --permissive INVALID KEY phrase, taken before the status store and
        // the USE hook — the WRITE arm above carries the full reasoning (kb/Work PB691).
        string? rst = null;
        if (rw.InvalidKey is not null)
        {
            rst = $"__rwst{ctx.Names.NextKeyedSeq()}";
            w.Line($"var {rst} = {RuntimeApi.FileStatus(FileKeyExpr(rw.File))};");
        }
        EmitStoreFileStatus(rw.File);
        EmitUseHook(rw.File);   // invalidKeyHandled stays false: no '2x' status is reachable here (§9.1.13.5)
        if (rst is not null) EmitInvalid(rst, rw.InvalidKey);
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
