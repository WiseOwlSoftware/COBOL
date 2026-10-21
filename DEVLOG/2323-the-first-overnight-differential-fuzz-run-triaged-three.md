## Entry 2323 — 2026-10-21 11:13 PDT — The first overnight differential fuzz run, triaged: three defects filed, one owner decision gains its count

The owner started kb/Work PB3488's harness at 22:45 last night (`qwen3.6:35b`, 8 hours, 4 in flight, seed 20261021) and
handed over the report this morning: 5,599 programs, 3,348 usable, 2,325 survivors in 378 signatures. This entry is the
triage, done in the Mythos session against the standard on a fresh build of main (62cd0d954), with every citation
through `cite.py --check`. The record is
`docs/rearchitecture/evidence/fleet-optimization/2026-10-21-differential-fuzz-overnight.md`; this entry is the short form.

**Filed.** PB3491 (MAJOR, wrong answer, silent): `GO TO procedure-name DEPENDING ON identifier` executed inside a performed
paragraph, with the selected target outside the PERFORM range, drops the transfer — the target never runs, control
returns after the PERFORM, the run ends with exit 0 and no output (§14.9.17.4 GR2; `ControlFlowEmitter.EmitGoToDepending`
leaves through `TransferJump()` where the format-1 and ALTERed GO TO leave through `ExplicitTransferOut`). Five variants
pin the shape to exactly that combination. PB3489 (MAJOR, crash class): the compiler does not finish on a syntax error
inside three or more nested function-identifier argument lists, while the same error one or two calls deep is reported in
under a second; the growth with depth points at the parser's prediction or recovery. PB3490 (MINOR, tooling):
`differential_fuzz.classify()` tests `we.hung` before `they.hung` and has no both-hung class, so three programs both
compilers looped on (unsigned PERFORM VARYING counters storing the absolute value, §14.9.25.4) were reported as WiseOwl
hangs.

**Not defects, and why.** 615 of the 1,141 output differences are one latitude — GnuCOBOL's leading sign and decimal point
against WiseOwl's digit image — settled for the sign (PB803) and open for the decimal point (PB3467, which now carries the
count). The 27 `EXCEPTION-STATUS` rows are WiseOwl correct: an explicit SIZE ERROR phrase takes precedence and the EC-SIZE
condition is not raised. `0 ** 0` is a size error (§8.8.1.2 item 6 a), `2 ** 3 ** 4` is 4096 (left to right, item 3), the
date functions' zero argument is an argument violation (§15.23.3 / §15.25.3 rule 1, "positive"), and the zero divide with
no phrase ends the run unit as DOC-A.1-70 / PB1605 chose. Every `WE_REJECT` signature of five rows or more is WiseOwl
following a syntax rule GnuCOBOL's dialect does not enforce, `NOT GREATER OR EQUAL` included (no `[NOT]` on that arm of
§8.8.4.2.1's format). `FUNCTION E` and `FUNCTION PI` are binary64 approximations under native arithmetic (latitude,
§15.27.3 / §15.73.3 rule 1) and exact under STANDARD-DECIMAL (rule 3), verified.

**Lessons for the harness** (for PB3488's owner, after PB3467): fold the decided display latitude before forming a
signature; mark programs that display an item nothing stored into (both compilers' outputs are undefined there, and the
suite normalizer strips WiseOwl's trailing spaces while keeping GnuCOBOL's NUL bytes, which made those rows look
one-sided); `BOTH_HANG`. With those, the report of a night like this one is a few hundred rows, most of them GnuCOBOL's.

