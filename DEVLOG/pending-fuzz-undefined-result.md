## The differential fuzz sets aside programs whose result the standard leaves undefined (PB3862)

The fifth fuzz night's triage found that 8 of the 17 `OUTPUT_DIFFERS:14.9.25` rows (fz01110, fz01111, fz01282, fz01933, fz01980,
fz02818, fz04400, fz00704) were programs that DISPLAY storage no statement wrote: a MOVE CORRESPONDING whose names do not
correspond, a MOVE of a group nothing had written, an item never targeted. ISO A.2 19) leaves the initial value of an item with no
VALUE clause undefined, so WiseOwl's zeros and spaces against GnuCOBOL's NULs is the difference of neither compiler, and each
such row cost a morning's adjudication.

**Re-probe.** On this branch's build, fz01111, fz01110, fz04932 and fz04934 all still classified `OUTPUT_DIFFERS` through the
harness's own `observe` and `classify`.

**The fix is one function of the program's text**, `scripts/fuzz_undefined.py#undefined_results`, and `classify` takes its yes/no.
An output difference in a program the function finds something in is the new class `UNDEFINED_RESULT`, listed among
`MODEL_FAILURE_CLASSES`: counted against the model, never a lead, and shown under its own heading in `report.md`. Definedness is
the least fixed point of the program's data flow: an item is defined when it has a VALUE (or a group above it has), when any
statement other than a DISPLAY or the sending side of a MOVE names it, or when a MOVE or MOVE CORRESPONDING (ISO 14.7.6 1, 2)
copies it from a defined item or from something that is not a data item (a literal, a figurative constant, a function). A copy
of an unwritten group is therefore unwritten, which is the shape that covered six of the eight rows. The function is conservative:
a finding is a proof, so a COPY, an OPTIONS paragraph, a directive, RENAMES or a second program makes it abstain, a
REDEFINES or EXTERNAL item is followed no further, and the analysis is flow-insensitive (a write after the DISPLAY still counts).
The pairs of a MOVE CORRESPONDING leave out the standard's exclusions (OCCURS, REDEFINES, uniqueness, 14.7.6 4 to 6), which can
only make a receiver read as written.

**The sweep the note asked for** found two more undefined results of PB3468 that a program's text proves, decided by the same
function: a literal subscript outside the OCCURS bounds (`SUBSCRIPT_RANGE`; ISO 8.4.2.3.4 2 and 14.6.13.1.1 NOTE 3: with the
exception condition not enabled the results are undefined) and an alphanumeric literal with a letter MOVEd to a numeric item
(`INCOMPATIBLE_DATA`; ISO 14.9.25.4 6; fz04932, fz02113). What a program's text cannot prove stays a lead: a variable subscript or
reference-modification position that a loop leaves past its bound (fz04934, fz04932's `WS-SRC (1:WS-LEN)`) and the content a MOVE
moves at run time.

**What it does to the record.** The row stores its findings (`undefined`); a row recorded before this change is read again from
its source under the run directory, so `--report` shows an old night under the new class. Applied to the eight kept nights'
`results.jsonl` it moves 131 of the OUTPUT_DIFFERS rows (about 4 %) to `UNDEFINED_RESULT`; the ones read by hand were all true
(an unwritten item DISPLAYed, a letter MOVEd to a `PIC 9` item). Only an OUTPUT difference moves: an exit-status difference with
the same output stays a lead.

**Tests.** `scripts/test_fuzz_undefined.py` (new, 24 arms) pairs each program that must be found with the nearest one that must not:
the three storage shapes, every statement that may have written an item (ADD, COMPUTE, ACCEPT, INITIALIZE, SET, PERFORM VARYING,
READ INTO, CALL USING), VALUE on an item and on a group, REDEFINES and EXTERNAL, subscripts of two dimensions and of an
OCCURS DEPENDING ON table, the numeric usages of the incompatible-data finding, and the abstentions; with the analysis's data-flow
edges removed 5 of its arms go red, with the function emptied 10. `differential_fuzz.py --self-test` gained arms for the class,
the row's `undefined` field through a stubbed `Fuzz._one`, `rederived` (an old row, a recorded finding, a missing source) and the
report heading. `docs/rearchitecture/DESIGN-differential-fuzz.md` §2 and §7 describe the class. No compiler code changed.

**Branch review round 1 (two blocking findings, fixed at the root).** (R1-1) UNWRITTEN_STORAGE walked only the PROCEDURE DIVISION, so
a FILE STATUS item the program only DISPLAYed after an OPEN read as storage no statement wrote, though the runtime sets it (ISO
9.1.13.1); RELATIVE KEY, CRT STATUS and CURSOR items likewise. A name anywhere before WORKING-STORAGE (the ENVIRONMENT DIVISION and the
FILE SECTION) now makes the item defined. (R1-2) `DataDivision._entry` ignored SAME AS and TYPE, so an item inheriting its VALUE read
as having none (fz01192's WS-COPY-GROUP had been misattributed this way). The entry now takes the picture, usages, VALUE and a copy of
the subordinates of the one entry it names (ISO 13.18.49.4 1, 2; 13.18.57.4 1, 2); a source it cannot resolve to one entry reads as
defined. Also fixed in the same files: a numeric literal with no integer part (`.5`, `-.25`) split the data entry at its point and
produced a bogus FILLER (R1-4), and `BakeOffScore.model_failures` named the classes without UNDEFINED_RESULT (R1-3). The test gained
arms in found and not-found pairs; seven go red against the previous module.
