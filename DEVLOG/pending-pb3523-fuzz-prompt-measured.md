## The fuzz generator's prompt rebuilt and measured: first-attempt validity did not move; repeated errors now stop early and declined modules are no longer seeds (PB3523)

PB3523 asked for a prompt that makes the local model write correct COBOL at the first attempt. On the second fuzz night,
47 % of the programs were refused by both compilers, and every one of them used all three repair rounds. The note's
hypothesis was that the model lacked the clause's syntax. All six of its items are now in `scripts/differential_fuzz.py`.
Each clause reaches the model cleaned of the transcription's markup and, when over a 6,000-character budget, cut by
structure: formats first, then syntax rules, then the general rules. The system prompt gives the division skeleton, a
naming rule, the reserved words the night misused, the rules on TRUE, FALSE, NULL, OCCURS and SEARCH, and the 2023
additions, read from Annex E.2 item 25 and the Foreword. It also gives one worked example per seed family (statement,
data description, intrinsic function, expression), and both compilers ran each example with identical output. A
correction now quotes the refused source line. Every refusal round is recorded on the row. A round whose first
diagnostics repeat the previous round's ends the corrections. The report and the bake-off table give first-attempt
validity.

The first before-and-after bake-off (40 programs each, qwen3.6:35b, seed 20261022, clauses 13.18, 14.9 and 8.4, run
12:24 to 12:32 PDT on the owner's GPU) looked like a regression: 17 usable programs fell to 6. The diagnosis found two
things. First, four of that walk's fourteen seeds were screen clauses, and WiseOwl refuses the whole screen-handling
module by name (COBOLNET1560; docs/CONFORMANCE.md §5 records it as Not claimed), so a screen seed can only be refused.
Second, five of the old prompt's "usable" programs never used their clause: the PAGE and UNDERLINE programs only
DISPLAYed its words. That produced a second change, `declined_seed_titles`. A whole clause or statement of an optional
module that §5 records as Not claimed (screen handling, VALIDATE, COMMIT and ROLLBACK, FORMAT and SELECT WHEN) is no longer
a seed, unless a module §5 does not decline lists it too. The 25 titles come from Annex A.4 and the dispositions from §5,
so a module that becomes claimed gets its seeds back. On the second night those seeds wrote 450 of the 4,931 programs and
330 of the 2,326 BOTH_REJECT rows.

Four more arms ran on the walk without those seeds (12:38 to 12:59 PDT). The old prompt wrote 10 of 40 programs usable at
the first attempt; the new prompt wrote 10, 9 and 8. Two arms differ only in the early stop, which can only lower the
usable count, yet they differ by 6 usable programs, so that is the noise at this size, and the prompt's effect on
first-attempt validity is inside it. The early stop saved 12 % of the generations (98 against 110 and 112). The new
prompt's usable programs stayed on topic as often or more (23 of 24 against 16 of 19). What stays refused is the model
writing constructs it does not know, each against a rule the clause text it was given states: ALLOCATE … GIVING, EXIT
PERFORM outside an inline PERFORM, SET … TO FALSE with no WHEN SET TO FALSE. The record is
`docs/rearchitecture/evidence/fleet-optimization/2026-10-22-differential-fuzz-prompt.md`, with its raw rows. The next
levers are leads: the generator's thinking mode and seeds weighted by their measured yield.

The owner's run cadence (12:15 PDT: "We fix all compiler bugs diagnosed by each fuzz test and get the fixes into main
before the next fuzz") is now the module docstring's rule. A `--model` run refuses to start while a `fuzz`-tagged
compiler-defect note is open; today that is PB3511, PB3512 and PB3517. `--force-open-notes` starts the run anyway and
names them. The self-test grew from 63 to 90 checks: the clause cut, the examples and the families, the 2023 lists, the
declined seeds, the quoted line, the recorded rounds, the error-line recognizer, the early stop and the full budget against
a stubbed model, and the cadence refusal against a fixture register. No compiler code changed.

Branch review found one defect and two leads, all answered on the branch. R1-1: the first exclusion dropped a title when
any declined module listed it, but A.4.14 VALIDATE (declined) and A.4.11 Report Writer (claimed) both list the PRESENT
WHEN and VARYING clauses, which WiseOwl implements. A title a module §5 does not decline also lists, whole or by one
format, now stays a seed: 25 titles, not 27. Neither was on a bake-off walk, so no arm's figure moves; on the second night
their 36 programs drew 5 WE_REJECT rows, which the old exclusion would have hidden. R1-2: the first error was any line
holding the word "error", so GnuCOBOL 3.2's source-context lines ("13 > VALIDATE WS-ALPHA ON SIZE ERROR …") were recorded
as the diagnostic and fed the early stop (3 of the bake-off's 344 rounds, 19 of the second night's 5,518 first errors).
`ERROR_LINE` now takes a line whose severity is error. R1-3: the general formats the prompt quotes are already the printed
page's, since `sweep_figures.py` generates every general format in the transcription from the PDF, so nothing changed
but the docstring that says so.
