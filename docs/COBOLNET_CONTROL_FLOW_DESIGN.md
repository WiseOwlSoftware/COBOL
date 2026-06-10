# COBOL.NET — Control Flow (the PC dispatcher) (deep-dive design)

> **Status: LIVE / authoritative subsystem design** for the COBOL.NET rewrite (COBOL -> idiomatic
> typed-native C# via Roslyn; no byte substrate). The condensed cross-referenced view is
> `docs/COBOLNET_DESIGN.md` §5; THIS is the full design (decisions + rationale + C# mapping + hard
> problems + edge cases). The locked invariants and cross-cutting consistency live in the SSOT.
> **IMPLEMENTED** (G4, DEVLOG 485) as `__Dispatch` in `src/Cobol.Net.Compiler/CodeGen/CSharpEmitter.cs` — emitted names
> are `__`-prefixed (`__Dispatch`/`__pc`/`__startPc`/`__exitPc`/`__atExit`/`__N`) to avoid collision with translated
> COBOL names; this doc's unprefixed `Dispatch`/`pc` sketches denote the same shapes.

## Summary

A decision-complete design for COBOL.NET's control-flow engine: a single program-counter (PC) state machine emitted as ONE C# method `int Dispatch(int startPc, int exitPc)` per program unit, in which every paragraph (and, for sectioning, every paragraph of every section, flattened into one source-order PC space) is a `case` in one `switch (pc)`, NOT a separate method. This ports the LEGACY compiler's PROVEN return-address / exit-bounded dispatch semantics (which passes 364 NIST tests) but realizes them in idiomatic C# (switch + structured loops + goto) instead of CIL, and replaces the legacy's ambiguous "-1 return AND a catch" run-unit termination with the new runtime's clean exception signals. The dispatcher backbone is a deliberate state machine (the task explicitly accepts not-pretty for irregular control flow); "idiomatic" applies to CASE CONTENTS — IF→if/else, EVALUATE→switch, inline PERFORM→real for/while/do-while loops, typed-native field ops — not to the backbone. Sequential fall-through is `pc=i+1`, GO TO is `pc=idxTarget`, out-of-line PERFORM/THRU is a recursive `Dispatch(idxEntry, idxExit)`, STOP RUN/GOBACK throw distinct exceptions. Key invariant the design preserves verbatim from legacy: the exit-bounded full-switch dispatch gives correct handling of inverted THRU ranges (proc-2 physically before proc-1) and GO-TO-out-of-and-back-into a PERFORM range FOR FREE, because control is followed by PC value, never by physical block extent.

## Backend neutrality (G4 — dual backend)

This deep-dive fixes the SEMANTIC control-flow model — the flattened source-order PC space, the ParagraphTable /
EntryParagraphIndex layout, the exit-bounded recursive dispatch contract, and the two termination exceptions
(StopRun/ProgramReturn). That model lives in the binder/bound tree and is BACKEND-NEUTRAL behind `ICodeGenBackend`
(`--backend roslyn|cil`). The C# realizations shown here (one `switch` method, `for`/`while`/`do-while` for inline
PERFORM, `goto __sent_<n>` for NEXT SENTENCE) are the Roslyn backend's RENDERING of that model; the future Cecil/CIL
backend implements the SAME bound contract with its OWN private structure-to-branch lowering (NO shared lowered IR).
Bound control-flow nodes carry structured forms (paragraph symbols/PC indices, loop descriptors, sentence-boundary
markers) — never pre-rendered C#-specific fragments (SSOT §18 #23).

## Decisions

### D1. Single PC-dispatch method per program unit: paragraphs/sections are `case` labels in one `int Dispatch(int startPc, int exitPc) { int pc=startPc; while ((uint)pc < (uint)N) { bool atExit = pc==exitPc; switch(pc){ case 0:...pc=1;break; ... } if (atExit && pc==exitPc+1) return pc; } return pc; }`. NOT paragraph-per-method.

**Rationale.** Owner-LOCKED: 'paragraphs/sections are LABELS (PC cases) in one flow, NOT separate methods. GO TO sets the PC; fall-through is PC++; PERFORM range is a recursive bounded dispatch.' The legacy semantics (return-address model, exit-bounded Dispatch) are proven (364 NIST) and port directly; only the realization changes from CIL-with-method-per-paragraph to one C# switch. The exit-bounded full switch handles arbitrary GO TO and inverted THRU ranges by construction (follow pc, not physical extent).

**Rejected alternatives.** (1) Paragraph-per-method + a dispatcher that CALLS them returning next-pc (the legacy CIL realization, CilEmitter.EmitDispatchHelper, and the pre-G4 entry-463 sequential-call-chain stopgap, retired by the G4 PC dispatcher at DEVLOG 485) — rejected: owner locked NOT-separate-methods; and the call-chain cannot express GO TO out of a range, ALTER, or VARYING. (2) Structured-only reconstruction (relooper/Stackifier producing pure if/while/goto-free C#) — rejected for v1: arbitrary COBOL GO TO/ALTER is irreducible; task says correctness over prettiness for irregular flow. Recorded as a deferred open question (well-behaved paragraph → real C# method).

### D2. Sequential fall-through emits `pc = i+1; break;` as the LAST statement of each non-terminating case; a paragraph that ends by reaching the next paragraph just advances the PC.

**Rationale.** Direct port of legacy `IrReturnConst(myIndex+1)` (Binder.cs:243). COBOL paragraphs are not isolated — control falls from one into the next textually (ISO §14.4 sequential execution). pc=i+1 with the switch covering [0,N) makes the last paragraph's fall-through (pc==N) exit the loop = run-unit end.

**Rejected alternatives.** Emitting the next paragraph's body inline after the current (textual concatenation) — rejected: breaks PERFORM THRU exit detection (no clean boundary for the named exit paragraph) and breaks GO TO targeting a mid-program paragraph.

### D3. GO TO (simple) → `pc = idxTarget; break;`. GO TO ... DEPENDING ON selector → `switch ((int)selector) { case 1: pc=idx0; break; ... case k: pc=idx(k-1); break; default: /* out of range: no transfer, fall through */ }` then if no case matched continue to the paragraph's normal fall-through.

**Rationale.** ISO §14.9.17: GO TO transfers control (set pc). DEPENDING ON with selector outside 1..k is a no-op (control passes to the next statement) — modeled by letting the switch fall out without setting pc, so the paragraph's trailing pc=i+1 runs. Selector resolved to int via the typed-native numeric value (the unscaled long / its integer reading), no byte decode.

**Rejected alternatives.** C# `goto caseLabel` between cases — rejected: C# forbids goto into a different switch section's label and the PC-return model composes with PERFORM/Dispatch recursion which raw goto cannot. A jagged if-else cascade for DEPENDING ON — rejected: switch is clearer and is what idiomatic C# uses for a dense 1..k dispatch.

### D4. ALTER + alterable GO TO → a per-alter-target mutable C# field `private static int _alter_<para> = <defaultTargetIdx>;`. A bare/alterable `GO TO` in that paragraph emits `pc = _alter_<para>; break;`. ALTER proc TO PROCEED TO new emits `_alter_<para> = idxNew;`.

**Rationale.** C#-NATIVE per owner decision ('ALTER → a mutable target var'), replacing the legacy `_alterTable[slot]` int array (CilControlFlowEmitter.EmitAlter/EmitReturnAlterable). A named int field is readable and exactly the legacy semantics. Default value = the GO TO's static written target (or -1/STOP if the bare GO TO was never given a target, ISO §14.9.17 undefined-then archaic).

**Rejected alternatives.** The legacy shared `int[] _alterTable` indexed by slot — rejected: owner wants a mutable var; an array index is opaque. ALTER is edition-varying — supported per §18 #10 (gated ON through 2014, flagged obsolete in strict 2023); see the per-edition gating & diagnostics section. The NIST corpus exercises it at `--std 85`.

### D5. Out-of-line PERFORM p [THRU q] → recursive `Dispatch(idxP, idxQ)` (idxQ=idxP when no THRU); the returned pc is discarded (PERFORM is a call, not a branch). PERFORM ... n TIMES / UNTIL / VARYING wrap that call in a real C# loop. The body of the case becomes e.g. `Dispatch(idxP, idxQ); pc = i+1; break;` for simple PERFORM, or a loop containing `Dispatch(...)`.

**Rationale.** ISO §14.9.28: PERFORM transfers control to a procedure and returns implicitly when it completes. Recursive Dispatch with exitPc=idxQ is the legacy's proven mechanism (EmitPerformThru → Dispatch(start,end)): it follows control flow ANYWHERE within/leaving/re-entering the range and returns only when paragraph idxQ falls through (pc becomes idxQ+1) — handling inverted ranges (idxQ<idxP) and GO-TO-out for free. Recursion (a real C# call stack) gives correct nesting of overlapping/recursive PERFORM ranges automatically.

**Rejected alternatives.** Inlining the THRU range's paragraph bodies into the call site (the pre-G4 stopgap formerly in CSharpEmitter.EmitPerform, retired at DEVLOG 485) — rejected: cannot express a GO TO that leaves and re-enters the range, breaks on inverted ranges, and duplicates code. A return-address stack data structure managed by hand — rejected: the C# call stack already IS the return-address stack; recursion is simpler and re-entrant.

### D6. Inline PERFORM (... END-PERFORM, no procedure-name) → REAL idiomatic C# loop INSIDE the case, never a Dispatch call: PERFORM TIMES→`for`, PERFORM UNTIL TEST BEFORE→`while(!(cond))`, TEST AFTER→`do{...}while(!(cond))`, PERFORM VARYING→`for`-style with COBOL FROM/BY/UNTIL, PERFORM (once)→`do{...}while(false)`. EXIT PERFORM→`break;`, EXIT PERFORM CYCLE→`continue;` (for VARYING/UNTIL-with-increment, continue must hit the increment — emit via a labeled-continue or a do/while-with-increment-at-bottom).

**Rationale.** Inline PERFORM scope is lexically bounded (END-PERFORM) and structured, so it maps to a native C# loop — this is exactly where 'idiomatic' applies. EXIT PERFORM/CYCLE are scoped to the nearest inline PERFORM (ISO §14.9.14 GR for the PERFORM phrase) → C# break/continue. Two DISTINCT mechanisms (inline=loop, out-of-line=Dispatch) because their scoping and EXIT semantics differ.

**Rejected alternatives.** Routing inline PERFORM through Dispatch too (uniformity) — rejected: there are no paragraphs to dispatch, EXIT PERFORM would need PC plumbing, and it would produce unreadable output for the common structured case. Using C# goto for EXIT PERFORM CYCLE — acceptable fallback only when the increment block can't be reached by `continue` (TEST BEFORE VARYING).

### D7. STOP RUN → `throw new StopRun();` caught ONLY at the run-unit Main; GOBACK / EXIT PROGRAM → `throw new ProgramReturn();` (a DISTINCT exception) caught at the current program's Entry wrapper. Normal transfers (GO TO, fall-through, range completion, EXIT PARAGRAPH/SECTION) stay integer-pc; exceptions are used ONLY for program/run-unit termination.

**Rationale.** A recursive Dispatch means a STOP RUN N PERFORMs deep must unwind ALL frames AND cross CALL boundaries — only an exception does that cleanly (ISO §14.9.42 STOP terminates the run unit). GOBACK/EXIT PROGRAM must unwind THIS program's PERFORM frames and return to the CALL site WITHOUT killing the run unit (ISO §14.9.18, §14.9.14) — a separate exception caught at Entry. EXIT PROGRAM/GOBACK in the main program is a no-op (caught and ignored, or falls to run-unit end). The new runtime already provides StopRun; add ProgramReturn.

**Rejected alternatives.** The legacy pc=-1 return-code propagation (CilControlFlowEmitter.EmitStopRun/EmitGoBack both return -1) — rejected and explicitly called out as the legacy's internal inconsistency (it returns -1 yet also has a 'StopRunException catch' comment): a -1 return unwinds only ONE Dispatch frame, so a STOP RUN inside a PERFORM would wrongly resume the caller; and -1 cannot cross a real C# CALL boundary without a re-check at every call site. The discriminator: which mechanism unwinds ARBITRARY nesting + crosses CALL for STOP RUN while stopping at the program boundary for GOBACK = exceptions.

### D8. EXIT PARAGRAPH → `pc = myIdx+1; break;` (return from current paragraph). EXIT SECTION → `pc = lastParaIdxInThisSection+1; break;` (skip remaining paragraphs of the section). EXIT (bare) is a no-op (ISO §14.9.14: common end point). CONTINUE → no statement emitted (no-op).

**Rationale.** Direct port of legacy SectionExitReturnIndex = lastParaInSection+1 (Binder.cs:213-221, ControlFlowLowerer.LowerExitParagraph/LowerExitSection). EXIT PARAGRAPH = fall through to next paragraph; EXIT SECTION = jump pc to the first paragraph after the section. Both are pure pc moves. ISO §14.9.14 GR9/10: SECTION phrase only in a section, PARAGRAPH phrase only in a paragraph.

**Rejected alternatives.** Emitting bare EXIT as control flow — rejected: ISO §14.9.14 bare EXIT is a no-op placeholder (the only sentence in its paragraph), control falls through normally.

### D9. NEXT SENTENCE → forward `goto __sent_<n>;` to a label placed at the next sentence boundary within the same case (last sentence → equivalent to paragraph fall-through). Statements after an unconditional NEXT SENTENCE in the same sentence are unreachable and are NOT emitted.

**Rationale.** ISO IF-statement GR (line 27834/27838) + Annex F.1: NEXT SENTENCE transfers control to an implicit CONTINUE immediately preceding the next separator period (sentence boundary), NOT past a scope delimiter. A C# forward goto to a sentence-boundary label models this exactly and stays within one switch case (legal C# goto). Legacy used a CurrentSentenceEnd block (ControlFlowLowerer.LowerNextSentence).

**Rejected alternatives.** Treating NEXT SENTENCE like END-IF scope exit — rejected: ISO explicitly warns this is the common MISunderstanding (line 50371); it goes to the period, not the scope delimiter.

### D10. DECLARATIVES + USE procedures: declarative sections occupy PC indices [0..declEnd); `EntryParagraphIndex` = first NON-declarative paragraph, so normal flow starts AFTER declaratives (the main Dispatch call uses startPc=EntryParagraphIndex, exitPc=-1). A USE procedure is invoked from the runtime I/O/error path as its own `Dispatch(useStart, useEnd)` call, then control returns to the statement after the triggering I/O (ISO §14.3, §14.6.13).

**Rationale.** Port of legacy ir.EntryParagraphIndex (CilEmitter.EmitEntryMethodBody:723) which skips the declaratives region, and the GlobalUse declarative dispatch (CilEmitter EmitDispatchGlobalUse). Declaratives must never run by fall-through, only by event. Sketched now; full USE-from-runtime wiring is G5/G7 but the PC layout (declaratives first, EntryParagraphIndex marker) must be designed in from the start so it is not a retrofit.

**Rejected alternatives.** Emitting declaratives as a separate dispatch space/method — viable but inconsistent with the single-Dispatch model; keeping them as low PC indices reuses the one switch and one set of paragraph labels.

### D11. PERFORM VARYING ... AFTER: on each increment of an outer varying identifier, ALL inner (AFTER) identifiers are RESET to their FROM values before the outer UNTIL is re-tested; ALL identifiers (including AFTER levels) are set to FROM before the first UNTIL test. Emitted as nested C# loops where the outer loop body re-initializes the inner index(es).

**Rationale.** ISO §14.9.28 + illustrated in Annex D.26 figures D.11-D.14 (the canonical VARYING flow diagrams). Direct port of legacy EmitAfterReinitialization (ControlFlowLowerer.cs:237-249). This is the #1 VARYING correctness gotcha a naive nested-for misses.

**Rejected alternatives.** Plain nested C# for-loops without re-init (relying on C# for-init) — works ONLY if the inner FROM is a constant; COBOL FROM can be a data item re-read each outer pass and AFTER reset is mandatory, so explicit reset is required.

## C# mapping

Generated shape (one Dispatch per program unit; paragraphs flattened to a single source-order PC space, declaratives first):

```csharp
// COBOL:
//   PROCEDURE DIVISION.
//   MAIN-PARA.        DISPLAY "A". PERFORM SUB THRU SUB-END. DISPLAY "B". STOP RUN.
//   SUB.              DISPLAY "S1". IF X = 1 GO TO SKIP.
//   MID.              DISPLAY "MID".
//   SKIP.             CONTINUE.
//   SUB-END.          DISPLAY "SE".
private const int N = 5;               // paragraph count
private static int Dispatch(int startPc, int exitPc) {
  int pc = startPc;
  while ((uint)pc < (uint)N) {
    bool atExit = pc == exitPc;
    switch (pc) {
      case 0:   // MAIN-PARA
        System.Console.WriteLine("A");
        Dispatch(1, 4);                // PERFORM SUB THRU SUB-END  (idxSUB=1, idxSUB-END=4)
        System.Console.WriteLine("B");
        throw new StopRun();           // STOP RUN
      case 1:   // SUB
        System.Console.WriteLine("S1");
        if (X == 1L) { pc = 3; break; }            // IF X=1 GO TO SKIP (idxSKIP=3) — scale-aligned native long compare (COBOLNET_NUMERIC_DESIGN; never decimal)
        pc = 2; break;                 // fall-through to MID
      case 2:   // MID
        System.Console.WriteLine("MID");
        pc = 3; break;                 // fall-through
      case 3:   // SKIP
        /* CONTINUE = no-op */
        pc = 4; break;                 // fall-through
      case 4:   // SUB-END
        System.Console.WriteLine("SE");
        pc = 5; break;                 // fall-through (pc==N) -> loop exits
      default: pc = N; break;
    }
    if (atExit && pc == exitPc + 1) return pc;   // named THRU exit paragraph fell through
  }
  return pc;
}
internal static void Main() {
  try { Dispatch(/*EntryParagraphIndex*/0, -1); }   // exitPc=-1: run to STOP RUN / off-end
  catch (StopRun) { }
  // (run-unit cleanup: implicit CLOSE of open files, ISO 14.6.11)
}
```

Inline PERFORM (idiomatic loops, no Dispatch):
```csharp
// PERFORM VARYING I FROM 1 BY 1 UNTIL I > 3  ... END-PERFORM
for (long _v = 1; ; ) { I = CobolNum.Store(_v, 0, _P_I); if (I > 3L) break; /*body*/ _v += 1; }
// PERFORM UNTIL DONE  ... END-PERFORM (TEST BEFORE)     -> while (!(DONE)) { /*body; EXIT PERFORM=break*/ }
// PERFORM 3 TIMES ... END-PERFORM                       -> for (long _i=0;_i<3;_i++) { /*body*/ }
```

ALTER (C#-native mutable var):
```csharp
private static int _alter_GATE = 6;          // default GO TO target paragraph index
// paragraph GATE body:  case k: pc = _alter_GATE; break;   // alterable GO TO
// ALTER GATE TO PROCEED TO OTHER:            _alter_GATE = 9;   // emitted at the ALTER site
```

GO TO DEPENDING ON:
```csharp
// GO TO P1 P2 P3 DEPENDING ON SEL
switch ((int)SEL) { case 1: pc=idxP1; break; case 2: pc=idxP2; break; case 3: pc=idxP3; break;
  default: pc = thisIdx+1; break; }            // out of range -> normal fall-through
break;
```

GOBACK / EXIT PROGRAM in a CALLed program:
```csharp
internal static int Entry(ManagedPointer[] args) {
  try { Dispatch(EntryParagraphIndex, -1); }
  catch (ProgramReturn) { }                     // GOBACK/EXIT PROGRAM -> return to caller, run unit lives
  return 0;                                      // StopRun is NOT caught here -> propagates to run-unit Main
}
// GOBACK body:        throw new ProgramReturn();
// EXIT PROGRAM body:   throw new ProgramReturn();   (no-op if this IS the main program: Main catches+ignores)
```

EXIT PARAGRAPH / EXIT SECTION:
```csharp
// EXIT PARAGRAPH:   pc = thisIdx + 1; break;
// EXIT SECTION:     pc = lastParaIdxInSection + 1; break;
```

New runtime signal (add alongside StopRun.cs):
```csharp
public sealed class ProgramReturn : Exception;   // GOBACK / EXIT PROGRAM (program boundary, not run unit)
```
Emitter implementation note: build a per-unit `ParagraphTable` (flattened source-order list of (cobolName, symbol, index, sectionName), declaratives first), a `nameToIndex`/`symbolToIndex` map (symbol-keyed first for duplicate names across sections — port of ParagraphSymbolIndices), `EntryParagraphIndex` (first non-declarative), and per-section `lastIndex`. A per-case `bool terminated` flag stops emitting after an unconditional transfer (GO TO/STOP/GOBACK/EXIT *) so no unreachable C# is generated (warnings-as-errors safe).

## Hard problems

### STOP RUN / GOBACK unwinding nested PERFORMs and CALL frames in a RECURSIVE Dispatch (the central correctness fork; the legacy is internally inconsistent here — returns -1 yet has a stop-run catch comment).

Two distinct exceptions, never integer-pc: STOP RUN throws StopRun (caught only at run-unit Main → unwinds ALL Dispatch frames in ALL programs); GOBACK/EXIT PROGRAM throws ProgramReturn (caught at the current program's Entry → unwinds this program's frames, returns to the CALL site, run unit survives). Integer pc is used ONLY for in-program transfers. ISO §14.9.42 (STOP terminates run unit), §14.9.18 (GOBACK = logical end of THIS program/method).

### GO TO that leaves a PERFORM range and later re-enters it; inverted THRU range (proc-2 physically precedes proc-1).

Inherited FREE from the exit-bounded full-switch model: Dispatch(idxP, idxQ) follows pc anywhere in [0,N), and returns ONLY when paragraph idxQ falls through (atExit && pc==idxQ+1). It is a RETURN-ADDRESS model, not a physical-range model — never iterate a contiguous [min,max] block. Verbatim port of the legacy invariant (CilEmitter.EmitDispatchHelper doc; NC102A). The atExit flag is captured BEFORE the case body runs because the body overwrites pc.

### Overlapping / recursive PERFORM ranges (range B PERFORMed from inside range A, or a paragraph PERFORMing a range that contains itself).

Each PERFORM is a recursive C# `Dispatch(...)` call; the C# call stack IS the return-address stack, so arbitrary nesting and re-entrancy compose automatically with correct restore of the enclosing range's exit bound. No hand-managed stack.

### Dead/unreachable C# after an unconditional COBOL transfer (GO TO / STOP RUN / EXIT mid-sentence). Generated C# compiled warnings-as-errors would FAIL on unreachable code.

Track a `terminated` flag while emitting a case's statements; once an unconditional transfer (GO TO, STOP RUN, GOBACK, EXIT PROGRAM/PARAGRAPH/SECTION, bare GO TO) is emitted, stop emitting the rest of that sentence/case (it is unreachable-but-legal COBOL). Conditional transfers (IF c GO TO P) do not set terminated. Legacy modeled this with dead IrBasicBlocks.

### EXIT PERFORM CYCLE must reach the loop INCREMENT (not just re-test) for PERFORM VARYING / UNTIL-with-step.

Inline VARYING is emitted so the increment is reachable by `continue` (loop tail) OR, where C# `continue` would skip a needed increment block (TEST BEFORE with a separate increment region), emit a labeled structure / do-while-with-increment-at-bottom so EXIT PERFORM CYCLE = jump-to-increment. Port of legacy PerformContinueStack (increment block target) vs PerformExitStack (break target).

### PERFORM VARYING ... AFTER reinitialization of inner indices on every outer increment.

Nested C# loops where the OUTER loop body re-MOVEs each AFTER index to its FROM value before re-testing the outer UNTIL; all indices set to FROM before the first test. ISO §14.9.28 + Annex D.26 figs D.11-D.14; port of EmitAfterReinitialization. A plain nested-for is WRONG when FROM is a data item or any AFTER level exists.

### Duplicate paragraph names in different sections (legal COBOL); a GO TO / PERFORM must resolve to the correct one.

Index the PC table by paragraph SYMBOL (bound identity), not by name; keep a name→index fallback (first-definition-wins) only for unqualified references the binder couldn't disambiguate. Port of ParagraphSymbolIndices/ParagraphSymbolMethods. The switch is symbol-ordered so symbol-based resolution and the case order agree.

### NEXT SENTENCE semantics (archaic, commonly misunderstood) — goes to the next separator period, NOT past a scope delimiter.

Forward C# goto to a `__sent_<n>` label emitted at each sentence boundary within the case; last sentence's NEXT SENTENCE = paragraph fall-through. ISO IF GR (lines 27834/27838) + Annex F.1; the goto stays inside one switch case (legal C#). Explicitly NOT modeled as END-scope exit (ISO line 50371 warns of that misconception).

### DECLARATIVES must never execute by fall-through; only via the runtime error/I-O event path.

Lay declarative paragraphs at the LOW PC indices [0..declEnd); set EntryParagraphIndex = first non-declarative paragraph so the main Dispatch(start=EntryParagraphIndex) skips them. A USE procedure runs as its own Dispatch(useStart,useEnd) invoked from the runtime I/O path, returning to the statement after the triggering operation. Designed in now (PC layout) even though full USE wiring is G5/G7.

## Edge cases

- PERFORM proc THRU proc where the two endpoints are the same paragraph: idxQ==idxP, Dispatch(i,i) returns when paragraph i falls through (pc==i+1).
- Inverted THRU: PERFORM proc-2 THRU proc-1 where proc-1 physically follows proc-2 in source but is the named exit — handled by following pc, exit on the NAMED exit paragraph's fall-through, never by [min,max] block iteration (NIST NC102A).
- A paragraph that is BOTH fallen into sequentially AND a PERFORM target: identical case body serves both; PERFORM uses Dispatch(idx,idx) (exit-bounded return), fall-through uses pc=idx (continues).
- Last paragraph falls off the end: pc becomes N, (uint)pc<(uint)N is false, loop exits = normal run-unit termination (no STOP RUN needed; ISO §14.6.11 then does implicit CLOSE of open files).
- Empty paragraph (no statements): case body is just `pc = i+1; break;` (pure fall-through).
- Empty/absent PROCEDURE DIVISION or zero paragraphs: emit a Main with an empty try (N==0 → no Dispatch).
- GO TO DEPENDING ON selector value 0 or > number of targets: no transfer, control passes to the next statement (fall-through) — ISO §14.9.17.
- Bare GO TO (alterable) never targeted by an ALTER and never given a written target: default _alter_X = -1 → on dispatch pc=-1 → treated as run-unit end (undefined behavior; ALTER is archaic Annex F.1).
- ALTER targeting a GO TO that was written with an explicit target: the explicit target is the _alter_X initial value, ALTER overwrites it at runtime.
- EXIT PROGRAM / GOBACK executed in the MAIN program (not a CALLed subprogram): no-op — ProgramReturn is caught at Main's wrapper and ignored, or treated as run-unit end if main; never returns to a non-existent caller (ISO §14.9.14).
- STOP RUN inside a USE declarative or inside the deepest of several nested PERFORMs: StopRun exception unwinds every frame up to the run-unit Main regardless of depth.
- Sentence with multiple statements where an early one is IF...GO TO and the rest are unconditional: only the GO TO branch is conditionally terminating; emit the remaining statements (reachable when the IF is false).
- PERFORM of a SECTION name (not a paragraph): Dispatch(firstParaInSection, lastParaInSection) — sections are contiguous PC ranges in flattened order.
- EXIT SECTION in a section that is the last in the program: lastParaIdxInSection+1 may equal N → loop exits cleanly.
- Recursive PERFORM (a range that re-PERFORMs a range containing itself): each Dispatch is a fresh C# frame with its own exitPc; correct because the call stack restores the enclosing exit bound — but unbounded COBOL recursion → C# stack overflow (acceptable, mirrors real COBOL stack exhaustion).
- CONTINUE as the sole statement of a paragraph (common with SKIP-style targets): no-op body, pure fall-through.
- NEXT SENTENCE inside an inline PERFORM or EVALUATE WHEN: still targets the enclosing sentence period (the inline PERFORM/EVALUATE is within one sentence) — the goto-to-sentence-end label is correct.

## Per-edition gating & diagnostics (G1 — four compilers in one executable)

Control flow is edition-varying. Every edition-varying construct carries TWO co-equal obligations: (1) the complete
per-edition ISO-spec behavior in every edition that HAS it; (2) the correct DIAGNOSTIC in every edition that LACKS it
(not-yet-introduced or removed). Tests (NIST etc.) only VERIFY; they never SCOPE. Gating sources:
`docs/VERSION_CHANGE_REFERENCE.md` (the 130-row edition-change checklist; 2002→2023 deltas ONLY — it has NO 85→2002
rows; derive 85↔2002 gating from the 2002 standard / the ISO2023_CONFORMANCE_PLAN M2 catalog) and
`docs/VERSION_TEST_MATRIX_DESIGN.md` (the (construct × edition) matrix; Phase 0 done).

| Construct | 85 | 2002 | 2014 | 2023 | Gating source |
|---|---|---|---|---|---|
| GO TO (simple/DEPENDING), out-of-line + inline PERFORM (TIMES/UNTIL/VARYING/AFTER, TEST BEFORE/AFTER), bare EXIT, CONTINUE, NEXT SENTENCE, STOP RUN | ✔ | ✔ | ✔ | ✔ (NEXT SENTENCE archaic-flagged) | invariant; NEXT SENTENCE archaic = row 127 (Annex F.1 #2) |
| GOBACK | ✘ diagnose not-in-edition | ✔ | ✔ | ✔ | 2002 introduction (derive from the 2002 standard) |
| GOBACK with STOP-style status phrase (main program only) | ✘ | ✘ | ✘ | ✔ | row 75 (E.3.3 item 32) |
| EXIT PERFORM [CYCLE], EXIT PARAGRAPH, EXIT SECTION | ✘ diagnose not-in-edition | ✔ | ✔ | ✔ | 2002 introduction (derive from the 2002 standard) |
| PERFORM … UNTIL EXIT | ✘ | ✘ | ✘ | ✔ | row 80 (E.3.3 item 37) |
| Exception-checking PERFORM variant | ✘ | ✘ | ✘ | ✔ (owned by the EC deep-dive) | row 79 (E.3.3 item 36) |
| ALTER + alterable GO TO | ✔ (obsolete-element flag) | per §18 #10 gated ON — ⚠ the 2002 standard DELETED ALTER; strict 2002/2014 may need reject-as-removed (reconcile in the SSOT) | same as 2002 | flagged obsolete in strict 2023 | §18 #10; Annex F.1 |
| EXIT PROGRAM | ✔ | ✔ | ✔ | ✔ + archaic flag | rows 89/126 (Annex F.1 #1) |

Diagnostics: use below the introduction edition ⇒ reject with the standard not-in-this-edition diagnostic (the
negative half of the version test matrix); archaic/obsolete elements flag under the flagging options. Every row
above requires its (construct × edition) matrix case — tests verify, they never scope.

## ISO citations

- ISO/IEC 1989:2023 §14.9.28 — PERFORM statement (control transfer to a procedure and implicit return on completion; VARYING/TIMES/UNTIL/TEST BEFORE-AFTER; the range-completion = exit-paragraph-falls-through rule)
- ISO/IEC 1989:2023 §14.9.17 — GO TO statement (simple transfer = set PC; GO TO DEPENDING ON selector, out-of-range = no transfer; bare/alterable GO TO)
- ISO/IEC 1989:2023 §14.9.18 — GOBACK statement (logical end of a function/method/program; returns to caller, does not terminate the run unit unless main)
- ISO/IEC 1989:2023 §14.9.14 — EXIT statement (bare EXIT = common end point/no-op; EXIT PERFORM [CYCLE]; EXIT PARAGRAPH; EXIT SECTION; GR9 SECTION-only, GR10 PARAGRAPH-only)
- ISO/IEC 1989:2023 §14.9.42 — STOP statement (STOP RUN terminates execution of the run unit)
- ISO/IEC 1989:2023 §14.9.9 — CONTINUE statement (no operation; control passes to the next executable statement)
- ISO/IEC 1989:2023 §14.9.13 — EVALUATE statement (multi-subject WHEN matching → C# switch/if-cascade in case contents)
- ISO/IEC 1989:2023 §14.3 — Declaratives (USE procedures executed only on the associated condition, never by fall-through) and §14.6.13 (declarative procedure execution/return)
- ISO/IEC 1989:2023 §14.4 / §14.2 — Procedure division structure and sequential execution of statements/sentences/paragraphs (fall-through semantics)
- ISO/IEC 1989:2023 §14.9.38 — SEARCH / SEARCH ALL (AT END, WHEN, index varying; control transfer including NEXT SENTENCE form)
- ISO/IEC 1989:2023 Annex D.26, Figures D.11–D.14 — canonical PERFORM VARYING (TEST BEFORE/AFTER, one/two conditions) flow diagrams establishing the AFTER-index reinitialization-on-outer-increment rule
- ISO/IEC 1989:2023 Annex F.1 — Archaic language elements (NEXT SENTENCE; ALTER), and the IF-statement general rules (NEXT SENTENCE transfers to the implicit CONTINUE before the next separator period, NOT past a scope delimiter)
- ISO/IEC 1989:2023 §14.6.11 — implicit CLOSE of files still open at normal run-unit termination

## Resolved questions (settled in `COBOLNET_DESIGN.md` §18; edition gating in the per-edition section above)

- Should well-behaved paragraphs (no GO TO out, not a THRU exit target, only fallen into / simple-PERFORMed) be emitted as REAL standalone C# methods for readability, with only the irregular subset using the PC switch? Owner LOCKED not-separate-methods for v1; this is a deferred post-conformance prettiness optimization (a hybrid emitter that proves a paragraph is structured and lifts it to a method). SETTLED (§18 #9): v1 always emits the PC dispatcher; the structured lift is a deferred post-conformance pretty pass — no owner decision currently pending.
- GOBACK status phrase: ISO 2023 (change item 32, line 50308) lets GOBACK carry the same status phrase as STOP, but only in a main program. SETTLED (§18 #10): a main-program GOBACK-with-status throws `ProgramReturn` carrying the status, surfaced as the process exit code via RETURN-CODE (§18 #20). The status phrase is 2023-only (VERSION_CHANGE_REFERENCE row 75, E.3.3 item 32) — diagnosed as not-in-edition at `--std` 85/2002/2014.
- ALTER is an archaic feature (ISO Annex F.1, removed/obsolete in strict 2023). SETTLED (§18 #10): ALTER is dialect-gated ON through 2014 and flagged obsolete in strict 2023. ⚠ G1 reconciliation needed in the SSOT: the 2002 standard DELETED ALTER, so strict `--std 2002/2014` may need reject-as-removed (VERSION_CHANGE_REFERENCE carries no 85→2002 rows — derive from the 2002 standard). The NIST corpus exercises ALTER at `--std 85`.
- UNTIL EXIT phrase (ISO 2023 change item 37, line 50318: PERFORM UNTIL EXIT = infinite loop until EXIT PERFORM) — SETTLED (§18 #10): in scope — `while(true){...}` with EXIT PERFORM=`break`. 2023-only (VERSION_CHANGE_REFERENCE row 80, E.3.3 item 37): diagnosed as not-in-edition at `--std` 85/2002/2014.
- Exact CALL boundary realization (how a C# CALL site catches ProgramReturn vs lets StopRun propagate, and how RETURNING / BY REFERENCE args cross) is owned by the CALL/inter-program subsystem; this design only fixes that GOBACK=ProgramReturn-at-Entry and STOP RUN=StopRun-at-Main. The seam is owned by `docs/COBOLNET_INTERPROGRAM_DESIGN.md` (the CALL/interprogram deep-dive); this design fixes only GOBACK=ProgramReturn-at-Entry and STOP RUN=StopRun-at-Main.
