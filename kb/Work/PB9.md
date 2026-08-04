---
title: "PB9 — LANDED (DEVLOG 1133) — a RESERVED intrinsic name in the KEYWORD-OMITTE"
id: PB9
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
spec_refs: [13.14, 13.18.53, 15.75.2, 15.81.2, 15.88.2, 8.11, 8.3.2.4.1, 8.4.3.2.3, 8.9]
tags: [cobolsharp, work, defect]
---

# PB9 — LANDED (DEVLOG 1133) — a RESERVED intrinsic name in the KEYWORD-OMITTED form

> **⛔ THE SCOPE IN THIS ENTRY WAS WRONG — IT IS FOUR WORDS, NOT ONE — AND THE MEASUREMENT THAT CORRECTED IT ALSO
> FOUND A TRANSCRIPTION DEFECT.** The entry said "measured scope is exactly one word (RANDOM)". That sweep covered
> the nine ZERO-ARGUMENT intrinsics, which structurally could not see SIGN or SUM because they take arguments.
> The set that matters is **§8.9 reserved words ∩ §8.11 intrinsic function names** — a reserved word cannot be a
> data name (§8.3.2.4.1), so it can never arrive through the ordinary keyword-omitted route — and that
> intersection is **LENGTH, RANDOM, SIGN, SUM**. LENGTH already worked (it is in the `cobolWord` name slot for
> `START WITH LENGTH`); the other three were rejected.
> ⚠ The first intersection returned 88 names because **§8.11's transcribed list ran six names together on one
> line** (`SUM TAN TEST-DATE-YYYYMMDD …`) — a PAGE-BOUNDARY join, confirmed by rendering printed folios 213/214
> where each sits on its own line. Repaired under a word-conservation assert; the list now reads 94.
>
> **⛔ TWO WRONG FIXES BEFORE THE RIGHT ONE, both caught by NIST NC116A, both the SAME collision:**
> ```
> 01  WRK-DS-LS-5 PICTURE S99999   VALUE ZERO
>         SIGN LEADING SEPARATE.        ->  COBOLNET1585 "takes exactly one literal"
> ```
> · **Attempt 1 — `nameSlot: true`** (copying LENGTH). A name-slot word is admissible as a VALUE-clause literal,
>   and the operand loop is greedy, so it swallowed the SIGN CLAUSE of the next line as a second literal. LENGTH
>   is safe there only because LENGTH does not BEGIN a data-description clause; SIGN (§13.18.53) and SUM (§13.14)
>   both do.
> · **Attempt 2 — a dedicated `functionCall` alternative**, described in its own comment as "confined to
>   expression positions, so it cannot reach a data description". It reaches one in a single hop:
>   `valueClauseOperand : unaryExpression` → `primaryExpression` → `functionCall`. Identical failure. Confinement
>   was ASSERTED rather than traced.
>
> **THE FIX IS DERIVED FROM THE FUNCTIONS' OWN GENERAL FORMATS, NOT GUARDED.** §15.81.2 writes
> `FUNCTION SIGN ( argument-1 )` and §15.88.2 `FUNCTION SUM ( { argument-1 } … )` — neither has a no-argument
> form, so a BARE `SIGN` or `SUM` is never a function reference. Requiring the argument group makes
> `SIGN LEADING` unmatchable as a functionCall, so the collision is **structurally impossible** rather than
> defended against, and the report-writer `SUM OF` clause is covered by the same fact. RANDOM keeps its bare form
> (§15.75.2 brackets the whole parenthesised part) and begins no data-description clause.
> **The argument carrier came from a TOKEN DUMP:** these words carry `subscriptTrigger=true`, so with no FUNCTION
> keyword the lexer pushes SUBSCRIPT mode — `SUM(1 2 3)` lexes `SUM LPAREN SUB_INTEGERLIT×3 SUB_RPAREN` — so the
> alternative takes a `subscriptPart` and re-parses through the ONE D2 `ReparseArgs` path. Pinned by
> `CobolLexerModeDriftTests`.
> **GOLDEN** `conformance:2023/pb9_reserved_intrinsic_names` — all four words in BOTH reference forms, asserted
> equal, plus the NC116A construct itself so the regression that cost two attempts is pinned inside the test.
> **NEGATIVE** `pb9-reserved-fn-no-repository` — §8.4.3.2.3 SR2: without a REPOSITORY declaration the omission is
> not permitted, and since a reserved word cannot be a data name the rejection is unambiguous.
>
> ---
> *The original entry follows, kept because the scope correction is only legible beside it.*

### PB9 (as originally filed) · `RANDOM` cannot be written in the KEYWORD-OMITTED form at all

> **We reject legal COBOL** (the CLAUDE.md rule 4 red line), and it is the last survivor of PB7's family.
> ```
> REPOSITORY. FUNCTION ALL INTRINSIC.
> COMPUTE N = RANDOM        ->  COBOL0001: no viable alternative at input 'COMPUTEN=RANDOM'
> MOVE RANDOM TO T          ->  COBOL0001: no viable alternative at input 'MOVERANDOM'
> ```
> ✅ **§8.4.3.2.3 SR2** permits omitting FUNCTION for any REPOSITORY-declared intrinsic, and **§15.75.2**'s
> general format brackets the whole parenthesised part, so a bare `RANDOM` is a legal zero-argument reference.
>
> **MEASURED SCOPE — exactly one word.** A sweep of all nine zero-argument intrinsics in the keyword-omitted
> form: CURRENT-DATE · WHEN-COMPILED · PI · E · EXCEPTION-STATUS · EXCEPTION-LOCATION · EXCEPTION-STATEMENT ·
> SECONDS-PAST-MIDNIGHT all bind (PB7); **RANDOM alone fails.**
>
> **ROOT CAUSE — a DIFFERENT one from PB7's**, which is why PB7's fix did not reach it. PB7 was about suffix
> ARITY in `KeywordOmittedFunction`; this is about TOKENIZATION. `RANDOM` lexes as its own reserved-word token,
> so `MOVE RANDOM` never reaches `dataReference`/`cobolWord` and the keyword-omitted router is never consulted.
> The fix is a `cobol-words.json` row, so it carries the `new-construct` skill's mandatory edition-gate sweep —
> deliberately NOT folded into PB8, whose root cause it does not share.
> ⚠ **Not a regression:** verified identical on the pre-PB8 compiler by stashing the change.
