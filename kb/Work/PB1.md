---
title: "PB1 — LANDED — the CLASS half (DEVLOG 1117); a named residue stays open"
id: PB1
kind: defect
status: landed
severity: MAJOR
area: intrinsics
wrong_answer: false
crashes: false
silent: false
rejects_legal_source: false
under_rejects: false
process_only: false
blocked: false
blocked_by: []
spec_refs: [15, 15.14.3, 15.3, 15.7, 15.7.3, 15.70, 15.78.3, 7, 8.5.2.1, 8.8.1.1]
tags: [cobolsharp, work, defect]
---

# PB1 — LANDED — the CLASS half (DEVLOG 1117); a named residue stays open

> **`COBOLNET1627` (`intrinsic-argument-class`), edition-invariant, strict-reject with a `--permissive` warning —
> the DA6 disposition for the sibling §8.8.1.1 question.** Five of the twelve rows CLOSE (ABS, PRESENT-VALUE,
> RANDOM, RANGE, REM — bare class constraints, now fully enforced); five go **PARTIAL** because their rule has a
> second half this screen does not reach, and one is unaffected. Half a rule enforced is PARTIAL, not CONFORMS.
>
> **⛔ THE RECIPE BELOW WAS WRONG, AND THE CORRECTION IS THE VALUABLE PART.** "Consume `sig.ArgKind(i)`" is what
> this entry prescribed. Doing exactly that made the comprehensive gate return **12 failing corpus programs, every
> one of them legal COBOL** — `FUNCTION BYTE-LENGTH(<numeric>)` rejected, though §15.14.3 admits an argument of
> ANY class, and the nine rows with an empty `ArgKinds` default to `'n'`, which would have screened `LENGTH` as
> numeric-only. **The table is not merely UNREAD, it is UNVERIFIED — and it was unverified *because* it was
> unread.** Those codes were dispatch hints that drifted freely for years precisely because nothing consulted
> them. Wiring 79 unaudited declarations into a rejection path converts one silent defect into 79 chances to
> reject valid source.
> So the screen is driven by `IntrinsicArgumentRules.Verified` — a table whose every entry carries **the ISO
> clause it was read from**. Eleven entries (§15.7, §15.70–15.79: exactly the functions Phase B adjudicated). A
> function absent from it is screened exactly as before, so landing this could not regress anything, and the table
> grows as the review adjudicates each clause. Asserting the other 68 rules from an unaudited hint column would be
> fabrication, not completeness.
>
> **RESIDUE, each recorded on its own inventory row as PARTIAL — not lost, and not silently deferred:**
> · a LENGTH half — `AR-15.78.3-1` REVERSE ("at least one character position") and `AR-15.70.3-1` ORD ("one
>   character position in length");
> · a STRONGLY-TYPED GROUP exclusion — `AR-15.71.3-1` / `AR-15.72.3-1`, which the binder has no strong-type
>   predicate to screen at this seam;
> · a CROSS-ARGUMENT rule — `AR-15.79.3-3` ("argument-2 shall have the same type as argument-1"), which a
>   per-position screen cannot express;
> · the "all arguments of the same class" rules `AR-15.71.3-3` / `AR-15.72.3-3`, and the zero-length-literal
>   rules `AR-15.71.3-2` / `AR-15.72.3-2`, all still DIVERGES.
>
> **GOLDENS** (all four registered in `tests/conformance/negative/manifest.json` in the same commit):
> `pb1-numeric-arg-alphanumeric` · `pb1-string-arg-numeric` (the two hand reproductions) ·
> `pb1-numeric-arg-numeric-edited` (§8.5.2.1 Table 2 — numeric-edited-when-display is class ALPHANUMERIC) ·
> `pb1-numeric-arg2-alphanumeric` (REM's argument-2; a screen that stopped at the first argument would pass every
> other fixture and still miss that rule's second half).
>
> **The guard that keeps it wired:** `IntrinsicArgumentClassDriftTests` — the screen is called, every verified
> rule cites a §, every verified function is really in the catalog. It caught itself going blind on its first run
> (its row-parsing regex required digits for the arity bounds; a variadic row writes `inf`, so every variadic
> function was skipped and ORD-MAX read as absent from the catalog).
>
> The original finding follows, for provenance.

### PB1 (as found) · the intrinsic ARGUMENT-CLASS table is declared 79 times and read zero times

> **ONE architectural defect presenting as at least 12 separate rule violations.** Found by the first Phase-B
> verdict batch (§15.7 + §15.70–15.79, 55 rules over 11 functions), and it is the reason that batch returned 18
> DIVERGES — most of them are not independent bugs.
>
> **THE DEFECT.** `IntrinsicCatalog.cs` gives every one of its **79** catalog rows an `ArgKinds` string declaring
> each argument's required class (`"n"` numeric · `"s"` string · `"i"` integer · `"p"` polymorphic), and exposes
> `IntrinsicSig.ArgKind(int i)` to read it. **`ArgKind` has zero callers.** The only read of `ArgKinds` anywhere
> in `src/` is one equality test:
>
> ```
> src/Cobol.Net.Compiler/Binding/Procedure/Verbs/IntrinsicBinder.cs:267
>     if (sig.ArgKinds == "p" && args.Count > 0 && args.All(IsStringOperand))
> ```
>
> — the MAX/MIN category-polymorphism switch. So the table that exists precisely to enforce §15's argument rules
> enforces nothing, and argument-class checking survives only as hand-written per-function arms for a handful of
> functions (`CheckRepertoireArgs` for DISPLAY-OF/NATIONAL-OF, `BindConvert`, CHAR, the ALGEBRAIC family). **One
> rule, written in a few places and declared-but-unread in all the others** — the shape DA3/DA5/DA6 recorded as
> `feedback_one_rule_one_place`, this time with the general mechanism already built and simply not wired in.
>
> **⛔ REPRODUCED BY HAND, both directions, at the CLI — not taken from an agent's report:**
> ```cobol
> 01 N PIC 9(4) VALUE 1234.   01 R PIC X(10).
>     MOVE FUNCTION REVERSE(N) TO R        *> §15.78.3 r1: alphabetic/alphanumeric/national ONLY
> ```
> compiles clean and displays `4321      `. And the mirror:
> ```cobol
> 01 A PIC X(4) VALUE "ABCD". 01 R PIC S9(6)V99.
>     COMPUTE R = FUNCTION ABS(A)          *> §15.7.3 r1: argument-1 shall be of class numeric
> ```
> compiles clean and displays `0000000{` — garbage from coercing `"ABCD"` through `CobolNum.FromAlphanumeric`.
> Neither emits a diagnostic at any edition.
>
> **⚠ AND IT IS A HOLE IN DA6, WHICH LANDED HOURS EARLIER.** DA6 installed the §8.8.1.1 strict reject for an
> alphanumeric ARITHMETIC operand (`COBOLNET0844`). Function arguments bypass it: `BindArgOperand` routes through
> `BindFunctionArgumentExpr`, whose `OperandContext.FunctionArgument` deliberately suppresses the
> `ExpressionBinder.OperandRef` numeric screen. So `COMPUTE R = A` is correctly rejected while
> `COMPUTE R = FUNCTION ABS(A)` is not. Sweeping DA6's siblings is part of this fix (`feedback_scan_all_similar`).
>
> **THE FIX IS THE WIRING, NOT TWELVE PATCHES** (CLAUDE.md rule 5 — prefer the shape that makes the next case
> automatic). Consume `sig.ArgKind(i)` in `IntrinsicBinder.BindIntrinsicCore` as the general per-argument class
> screen for every catalogued function, with the existing hand-written arms folded into it or kept only where a
> rule is genuinely function-specific. Pair it with a drift test asserting that every catalog row's declared
> `ArgKinds` is actually consulted — otherwise the table can go dead again exactly as it did.
>
> **SCOPE — this is bigger than the 12 rows that found it.** §15 holds **216 AR rules across 43 functions**, of
> which **47 are explicit class/category constraints**; only 11 functions have been adjudicated so far. Expect the
> remaining §15 clauses to return the same verdict, and expect the count to grow as Phase B proceeds. The
> VALUE-constraint rules ride along: §15.3 requires a bad argument VALUE to set **EC-ARGUMENT-FUNCTION**
> (`FUNCTION REM(x 0)`, `FUNCTION PRESENT-VALUE(-1 …)`, `FUNCTION RANDOM(-1)`, a zero-length literal to ORD-MAX),
> and none of those raises today either.
>
> **INSTANCES RECORDED IN THE INVENTORY** — 11 rows (each carries its own §, code-location and reproduction):
> `AR-15.7.3-1` ABS · `AR-15.70.3-1` ORD · `AR-15.71.3-1`/`-3` ORD-MAX · `AR-15.72.3-1`/`-3` ORD-MIN ·
> `AR-15.74.3-1` PRESENT-VALUE · `AR-15.75.3-1` RANDOM · `AR-15.76.3-1` RANGE · `AR-15.77.3-1` REM ·
> `AR-15.78.3-1` REVERSE · `AR-15.79.3-3` SECONDS-FROM-FORMATTED-TIME.
> The **zero-length-literal** rules ride the same dead table from the other side: `AR-15.71.3-2` /
> `AR-15.72.3-2` note the guard exists for CONVERT and the repertoire functions and for nothing else.
>
> ⚠ **These rows are AGENT-SURFACED and adversarially re-verified, but only the two reproductions above and the
> zero-callers fact were confirmed by hand.** Design doc §7 stands: verify each before it drives a code change.
> All are DIVERGES/PARTIAL, which do NOT close a GAP, so nothing in the burn-down rests on them.
