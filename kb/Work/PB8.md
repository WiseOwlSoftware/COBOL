---
title: "PB8 — LANDED (DEVLOG 1131) — reference-modifying a FUNCTION result"
id: PB8
kind: defect
status: landed
severity: MAJOR
area: reference-modification
wrong_answer: false
crashes: false
silent: false
rejects_legal_source: false
under_rejects: false
process_only: false
blocked: false
blocked_by: []
spec_refs: [15.100.3, 15.21.1, 15.23.3, 15.25.3, 8.4.3.2.3, 8.4.3.3.3, 8.4.3.3.4]
tags: [cobolsharp, work, defect]
---

# PB8 — LANDED (DEVLOG 1131) — reference-modifying a FUNCTION result

> **⛔ THE ROOT CAUSE RECORDED BELOW WAS WRONG, AND THE CORRECTION IS THE VALUABLE PART.** This entry said the
> defect was a LEXER-MODE decision and that the fix was "a lexer-mode change PLUS a `functionCall` grammar tail —
> the riskiest category in this codebase". **A token dump refuted it before a line was written** (the entry was
> reasoned from `OnDefaultLParen`'s source, never measured): in BOTH keyword-present shapes the ref-mod paren is
> **already lexed in DEFAULT mode** — after a function NAME the lexer's own FUNCTION suppression keeps it out of
> SUBSCRIPT mode, and after the argument list's `)` the previous token is not a data-name so the SUBSCRIPT
> trigger never fires. Both therefore reach the DEFAULT-mode `refModPart` that had existed all along for
> `dataReference`. **THE LEXER WAS NEVER TOUCHED.** `CobolLexerModeDriftTests` now pins that per shape, so the
> conclusion cannot rot. `use_antlr_tree_dump` is in memory for exactly this reason — the budgeted risk was an
> artifact of not having dumped.
>
> **THE DEFECT WAS ALSO WIDER THAN THE TWO REPROS**, and the widest case was the one nobody had written down:
> | shape | before |
> |---|---|
> | `FUNCTION CURRENT-DATE (1:4)` | COBOL0001 parse error |
> | `FUNCTION UPPER-CASE("abc") (1:2)` | COBOL0001 parse error |
> | `CURRENT-DATE (1:8)` (keyword-omitted) | COBOLNET1543 — the group was read as an ARGUMENT LIST |
> | `UPPER-CASE("abc") (2:3)` (keyword-omitted) | ⛔ **COMPILED CLEAN, threw at RUN TIME** — the PB7/DA7 wrong-stage family |
> | `FUNCTION LOCALE-DATE(CURRENT-DATE (1:8))` — **the standard's own §D.14.3.6 example** | COBOLNET1543 |
>
> ⚠ **AND THE GRAMMAR TAIL ALONE WOULD HAVE MADE IT WORSE.** With `refModPart` parsing but the binder ignoring
> it, `FUNCTION UPPER-CASE("abcdefgh") (2:3)` compiled and printed **ABC** instead of BCD — the ref-mod silently
> DROPPED. A loud parse error had become a silent wrong answer. Measured before proceeding, not after.
>
> **THE FIX, and where each rule lives:**
> · grammar — `functionCall : FUNCTION functionName (LPAREN functionArgList? RPAREN)? refModPart*`;
> · §8.4.3.3.3 SR2 (alphanumeric/boolean/national only) — `IntrinsicBinder.ResultRefMod`, **`COBOLNET1629`**;
> · §8.4.3.2.3 SR6 (a `(` after an argument-PERMITTING function name is ALWAYS the argument list — the
>   standard's own RANDOM trap) — same site, reported as the argument-list error it is (`COBOLNET1543`), not as
>   a class error about a ref-mod that was never written;
> · §8.4.3.3.3 SR3 (no ref-mod of a ref-mod) — **`COBOLNET1630`**, counted in the binder rather than expressed as
>   the grammar's arity, so the function and data-reference sides report the SAME rule;
> · the two source carriers (parsed `refModPart` · SUBSCRIPT-mode capture) now reduce through ONE reader,
>   `ReferenceResolver.ReadRefMod` → `RefModSpec`, and the slice reuses `CobolString.RefMod` — so the
>   §8.4.3.3.4 item-5c bounds check and EC-BOUND-REF-MOD are shared, not re-implemented.
> **A RIDER, NOT A WRAPPER:** the ref-mod hangs on `BoundIntrinsicCall.RefMod` because the alphanumeric string
> channel is selected by pattern-matching that node at several sites; a wrapper would have silently stopped
> matching at each one, and the failure mode is a DROPPED ref-mod, not a compile error.
> **GOLDEN** `conformance:2023/pb8_refmod_function_result` (9 cases incl. §D.14.3.6) + four negative fixtures.
> **INVENTORY** `SR-8.4.3.3.3-2` and `SR-8.4.3.3.3-3` CLOSED; `SR-8.4.3.2.3-6` PARTIAL (see PB9).
>
> ---
> *The original entry follows, kept because the correction above is only legible beside it.*
>
> **We reject legal COBOL — the CLAUDE.md rule 4 red line.** Both forms, hand-verified at `--std 2023`:
> ```
> MOVE FUNCTION CURRENT-DATE (1:4)      TO WS-YR  ->  COBOL0001: no viable alternative at input '('
> MOVE FUNCTION UPPER-CASE("abc") (1:2) TO T      ->  COBOL0001: no viable alternative at input '('
> ```
> ✅ **§8.4.3.3.3 SR2, validated verbatim:** "If identifier-1 is a function-identifier, it shall reference an
> alphanumeric, boolean, or national function." The standard explicitly contemplates reference-modifying a
> function-identifier and constrains only WHICH functions qualify — CURRENT-DATE (§15.21.1, alphanumeric) and
> UPPER-CASE both do. The shape is normative at §15.23.3 r5, §15.25.3 r5 and §15.100.3 r5, and written out at
> §D.14.3.6.
>
> **⛔ ROOT CAUSE IS IN THE LEXER, AND THE FILE WARNS IT CANNOT BE REPAIRED DOWNSTREAM.**
> `CobolLexer.g4`'s `OnDefaultLParen` pushes SUBSCRIPT mode — which is what captures a ref-mod — only when
> `PreviousTokenCouldBeDataName() && !PreviousIsFunctionName()`. The two failing shapes miss it for DIFFERENT
> reasons:
> · after a zero-argument function NAME, `PreviousIsFunctionName()` is TRUE, so the trigger is suppressed;
> · after a function call's closing `)`, the previous token is not a data name at all, so it never fires.
> The lexer's own comment: "the SUBSCRIPT-mode decision at '(' is frozen at lex time and cannot be repaired
> later." The fix is therefore a lexer-mode change PLUS a `functionCall` ref-mod tail in the grammar — the
> riskiest category in this codebase, and deliberately NOT attempted in the same pass that landed PB7.
>
> ⚠ Interaction worth knowing: PB7 made zero-argument keyword-omitted references bind, so the standard's own
> §D.14.3.6 example `CURRENT-DATE (1:8)` gains a second reason to work once this lands.
