# PB3227, 2026-11-23: the CALL pass mode is one rule, and the storage claim asks it (wave 20262154 group F)

**The open halves at the start (base 7844ab147d).** `procedure-binding-5--modern-csharp--f2#1` had already landed as
PB3229 (`ItemCategory.CategoryName` and `UsageFamilies.UsageWord` read by every message site; the sweep at this base finds
no `ToString().ToLowerInvariant()` of a `PicCategory` or `CobolClass` in `src/WiseOwl.Cobol.Compiler/Binding`), so the
one open half was `data-binding-2--duplication--f1#5`.

**Re-probe: a wrong answer, not only a duplication.** `StorageClaimScan` applied §2023:14.9.4.4 GR5 (printed under FORMAT 1)
to every CALL, while `CallBinder.BindCall` applies GR9 to a Format-2 keyword-less argument. With a contained callee
`PROCEDURE DIVISION USING BY VALUE V BY REFERENCE A B` and `01 AX REDEFINES A`, the caller's
`CALL "PB3227B" AS NESTED USING BY VALUE N NUM NUM` (NUM `PIC 9(4)`) printed `B: 1234` where the callee's
`MOVE "5678" TO AX` must be B's value at once (§2023:14.2.3 GR8, one storage area); the same program with the first
argument keyword-less printed `B: 5678`. NUM was never claimed onto a cell, so the area formal A held a private copy.
The same miss: a Format-2 keyword-less identifier after BY CONTENT (claimed only as `SlotsOnly`), the bare
program-prototype-name spelling of Format 2 (no AS phrase), and a keyword-less identifier the parse put in the boolean node
because a LATER argument writes B-AND (the scan read only `arithmeticExpression`; §2023:14.9.4.4 GR8 still makes it
identifier-2): `CALL "PB3227G" AS NESTED USING NUM NUM BY CONTENT B1 B-AND B2` printed `B: 1234` on the pre-fix build.

**Fix.** `CallArgumentModes` (new, `Binding/Procedure/Verbs/CallArgumentModes.cs`) is the one statement of GR5 and GR9:
`WrittenPhrase`, `CarriedAfter` (was `CallBinder.CarriedModeAfter`), `KeywordLessIdentifier`, `KeywordLessValue` (was the
local `Gr9NonIdentifierMode`) and `FormalIsByValue`. `CallBinder.BindCall` asks it for every keyword-less argument (the
identifier, address-identifier, literal, temporary and expression arms). `StorageClaimScan` asks it too: since a Format-2
callee's formals are bound after the claim, a Format-2 keyword-less identifier is claimed as GR9 a) 1. passes it to a BY
REFERENCE formal (an over-claim costs a cell; a missing one breaks GR8). Format 2 is the AS phrase, or a bare target word
(`CallBinder.BarePrototypeWord`, now internal) the asking element names a program prototype (§2023:8.4.6.8,
`SymbolTable.DeclaresPrototypeName(word, function: false)`), recorded with both formats' claims and decided at the ask,
as the user-function argument already was. Every operand reduction of the scan (CALL, INVOKE, user function) is GR8's one
reduction, `ConditionBinder.ReduceArgument`; the scan's own text-matching `OperandIdentifier` is deleted.

**Tests.** `StorageClaimScanTests.AFormat2KeywordLessIdentifierIsClaimedAsGr9PassesIt_WhateverPhraseCameBefore`; golden
`conformance-suite/cases/14.9.4-CALL/pb3227_format2_keywordless_shares_storage` (accept 2002, 2023; four legs: after BY
VALUE, after BY CONTENT, in the boolean node, the bare prototype name) and its refusal at 85,
`pb3227_format2_keywordless_85` (the CALL AS phrase requires COBOL-2002).

**Review R1-1 (contained program's own name).** A container claims its GLOBAL items' cells from its scan of its contained
programs, and judged each bare CALL target with its OWN prototype scope. §2023:8.4.6.8 makes "the program-name of a
containing program definition" a program-prototype-name of the REFERENCING source element, so a contained program's own
name (and every contained program between it and the asker) is one for that program's CALLs but was missing from the
container's set. Probe (the reviewer's `rec.cob`): RECURSIVE INNERP's `CALL INNERP USING BY CONTENT N NUM NUM`, NUM the
container's GLOBAL item, gave `B: 1234`; expected `B: 5678`. The scan now records, with each asked CALL word, whether it
spells the program-name of a contained program that holds the CALL (`AskedWord.NamesContainingProgram`), a fact of the parse
tree alone; the asker's predicate covers itself, its containers and the REPOSITORY (a contained program inherits its
container's, §2023:12.3.3 SR1). Golden `14.9.4-CALL/pb3227_contained_own_name_shares_storage` (2002, 2023): `B: 5678`,
red without the fix (`expected 'B: 5678', got 'B: 1234'`).

## Landing

- `src/WiseOwl.Cobol.Compiler/Binding/Procedure/Verbs/CallArgumentModes.cs#CallArgumentModes`: the one GR5/GR9 CALL pass-mode rule; REUSE `KeywordLessIdentifier` / `KeywordLessValue` / `FormalIsByValue` / `CarriedAfter`, never re-derive a mode from the phrases.
- `src/WiseOwl.Cobol.Compiler/Binding/StorageClaimScan.cs#RecordCallArguments`: the scan's CALL arm; a Format-2 keyword-less identifier is claimed as a BY REFERENCE formal takes it (formals bind after the claim). Trap: GR5 is printed under FORMAT 1 only.
- `src/WiseOwl.Cobol.Compiler/Binding/StorageClaimScan.cs#ByReferenceOperands`: two asker predicates (user function, program prototype); an `Entry` holds both formats' claims when the bare target word decides the format.
- `src/WiseOwl.Cobol.Compiler/Binding/Passes/PtrBindBasedAndAddressablesPass.cs#IsProgramPrototype`: `SymbolTable.DeclaresPrototypeName(word, function: false)` (§2023:8.4.6.8).
- `src/WiseOwl.Cobol.Compiler/Binding/Procedure/Verbs/CallBinder.cs#BarePrototypeWord`: internal; the one shape test of a program-prototype-name operand. Trap: renaming a member an inventory row cites needs a `record_verdicts` retirement (SpecTraceabilityInventoryDriftTests).
- `src/WiseOwl.Cobol.Compiler/Binding/StorageClaimScan.cs#NamesContainedProgramAbove`: a contained CALL's own program and the contained programs above it are its containing program definitions (§2023:8.4.6.8); the asker's `IsProgramPrototype` covers only its own scope. Trap: the container judges its contained programs' CALLs.
