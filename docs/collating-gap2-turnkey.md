# Gap 2 — FUNCTION CHAR / ORD under a program collating sequence (turn-key spec)

_Authored 2026-05-29. Gap 1 (SORT/MERGE collating) + the numeric-key fix are DONE and committed
(0a7caae, 8900437, 70182cb); guard ALL GREEN. This file is the ready-to-execute spec for the ONLY
remaining collating work. Implement with a healthy tool channel (this session's render channel
degraded mid-task, so the structural edits were deferred rather than risked against unreliable reads)._

## Goal
`FUNCTION CHAR(n)` and `FUNCTION ORD(c)` must use the **alphanumeric program collating sequence**
(ISO/IEC 1989:2023 §15.15 CHAR, §15.36 ORD; ordinal positions are 1-based). Today they are native
ASCII only. Use the SAME compile-time-baked-`byte[]` pattern as comparisons and SORT (no runtime
global; AOT-safe).

## Verified facts (reliable reads, this session)
- Runtime dispatcher: `IntrinsicFunctions.Call(string functionName, object[] args)` —
  `src/CobolSharp.Runtime/Intrinsics/IntrinsicFunctions.cs:742`. CHAR/ORD dispatch at lines 808-809:
  `"CHAR" => Char(numArgs[0]),` and `"ORD" => Ord(strArgs[0]),`.
- Leaf fns: `Char(decimal code)` (line 196, returns code n-1 as 1-char string),
  `Ord(string value)` (line 203, returns code+1). CHAR-NATIONAL (line 648/863) — leave native
  (national PCS is out of scope; no NIST coverage).
- Emitter: `CilExpressionEmitter` builds the `object[]`, then
  `il.Call(IntrinsicFunctions.GetMethod("Call"))` (~line 319), then casts to string / unboxes decimal
  per `call.ReturnsString`.
- Table semantics (`seq[code] = weight`, 0-based): `ORD(c) = seq[(byte)c[0]] + 1`;
  `CHAR(n) = first (lowest) code with seq[code] == n-1` (§15.15.4 rule 2: first defined char for the
  position). Native fast path = `collating == null`.
- The collating table is resolved into `SemanticModel.ProgramCollatingSequence` (byte[]?, null=native)
  before lowering — see `Compilation.cs:233-238`.

## Design (mirror the comparison subsystem — bake byte[] into the IR node)
1. **IR node** `IrIntrinsicCall` (`src/CobolSharp.Compiler/IR/IrExpression.cs:1543`): add
   `public byte[]? CollatingSequence { get; }` + ctor param (nullable, default last).
   READ THE CLASS FIRST — it has `ReturnsString` and `Arguments`; match the existing ctor style.
2. **Lowerer** (the site doing `new IrIntrinsicCall(...)` — grep `new IrIntrinsicCall`; it's in
   `CodeGen/Lowering/DataMovementLowerer.cs`, the same place that sets `ReturnsString`): set
   `CollatingSequence = _ctx.Semantic.ProgramCollatingSequence` (null when native). Setting it
   unconditionally is fine — only CHAR/ORD read it downstream.
3. **Emitter** `CilExpressionEmitter` intrinsic method (~line 270-320): after building the args
   `object[]`, push the 3rd arg — `EmitByteArrayLiteral(il, call.CollatingSequence)` when non-null
   else `il.Create(OpCodes.Ldnull)` (reuse the `EmitCollatingArg` helper idea from
   `CilFileIoEmitter`). Bind `Call` with the 3-arg overload:
   `GetMethod("Call", new[]{ typeof(string), typeof(object[]), typeof(byte[]) })`.
4. **Runtime** `IntrinsicFunctions.cs`:
   - `public static object Call(string functionName, object[] args, byte[]? collating = null)`
     (default keeps any direct test callers compiling).
   - Dispatch: `"CHAR" => Char(numArgs[0], collating),` and `"ORD" => Ord(strArgs[0], collating),`.
   - `public static string Char(decimal code, byte[]? collating = null)`:
     if `collating == null` keep current (`(char)(ToInt(code)-1)`); else find first index `i` in
     0..255 with `collating[i] == ToInt(code)-1`, return `((char)i).ToString()` (or " " if none /
     out of range).
   - `public static decimal Ord(string value, byte[]? collating = null)`:
     if `value.Length == 0` return 0; let `b=(byte)value[0]`; return
     `collating == null ? (decimal)b + 1 : (decimal)collating[b] + 1`.
   - Keep the existing 1-arg `Char`/`Ord` only if other callers need them; otherwise the default
     param covers it (prefer the default-param single definition; delete the old overloads to avoid
     dead code).

## Test (add to tests/CobolSharp.Tests.Integration/SortMergeCollatingTests.cs or a new
IntrinsicCollatingTests.cs)
Program with `OBJECT-COMPUTER. X PROGRAM COLLATING SEQUENCE IS REV` and
`ALPHABET REV IS "B", "A".` Then under REV: ORD of "A" = 2, ORD of "B" = 1 (B has weight 0 →
position 1); CHAR(1) = "B", CHAR(2) = "A". Assert all four via DISPLAY. Also a no-PCS control:
ORD("A")=66, CHAR(66)="A" (native).

## Then
- Build → `bash scripts/guard.sh` (expect ALL GREEN; CHAR/ORD have no NIST coverage so baselines
  are unaffected). Commit with a DEVLOG entry (226).
- Update `docs/collating-subsystem-plan.md` FINAL STATUS + memory `project_collating_gap` to mark
  Gap 2 done (subsystem then complete except national CHAR-NATIONAL, intentionally native).

## Also pending: zero-dead-code cleanup (separate commit, do NOT mix with Gap 2)
`SemanticModel.RegisterPicDescriptor` / `GetPicDescriptor` / `_picDescriptors`
(`src/CobolSharp.Compiler/Semantics/SemanticModel.cs:181,268-269,279-280`) are now provably unused
(the Gap-1 numeric-key fix switched `FileIoLowerer.BuildKeySpecField` to the live
`ResolveLocation().GetPic()` path). Delete all three members; build to confirm no references.
