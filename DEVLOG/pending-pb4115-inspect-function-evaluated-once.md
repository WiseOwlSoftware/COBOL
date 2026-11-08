## PB4115: an INSPECT TALLYING evaluates a function-identifier once, whatever its counters overlap

kb/Work PB4115 was filed as a duplication analysis: `InspectEmitter.MayReadStorageOf` answered "may" for every
function-identifier, so the D-INS1 re-run of the tallying cycle (docs/CONFORMANCE.md) re-read the image and the operands
after each counter store, while STRING's private `ReachOf` walk proved a function disjoint where it could. The re-probe
on this build showed a wrong answer, not only a redundant re-read. `INSPECT FUNCTION UPPER-CASE(G) TALLYING T FOR ALL
"A" T FOR ALL "3"` with T inside G over `0A1A1A` gave `4A1A1A`: the function ran again after T stored 3, and the new "3"
was counted. A pattern `FUNCTION UPPER-CASE(Q)` over its own counter gave `2X`, and `INSPECT FUNCTION CURRENT-DATE` with
two counters read the clock twice.

The root cause was the premise, not how coarse the walk was. A function-identifier "references a temporary data item
whose value is determined when the function is referenced at runtime" (§2023:8.4.3.2.4 GR1), and INSPECT identifies each
identifier once (§2023:14.9.22.4 GR6). No counter can occupy a temporary item, so GR13's undefined overlap never reaches a
function, whatever its arguments. The new `InspectEmitter.TallyOperandTexts` evaluates every TALLYING operand that is
neither a data item nor a literal once, in source order, into a local ahead of the first cycle. `MayReadStorageOf` now
answers only for a data-item reference (`StorageOverlap.MayShareStorage`), and the re-run reads identifier-1 again only
when the previous counter may overlap it. INSPECT therefore needs no function walk, so the note's proposed fix (move
`StringEmitter.ReachOf` beside `StorageOverlap` and share it) was not needed. INSPECT and UNSTRING now both ask only
whether a data item may share storage. The note records the corrected estimate.

GnuCOBOL 3.2 (measured) evaluates identifier-1's function once. It evaluates a pattern's or a delimiter's function when
its phrase runs, after the counters before it stored, and gives `2X` and `3B` where this compiler gives `1X` and `4B`.
The standard controls here, and D-INS1 records the divergence. Pinned by
`conformance:cases/14.9.22-INSPECT/pb4115_inspect_function_evaluated_once`, which runs at 85, 2002, 2014 and 2023:
F1 identifier-1, F2 pattern, F3 BEFORE delimiter, and the D2 control showing that the data-item arm still re-reads. The
clock case, which no golden can observe deterministically, is held by `unit:InspectFunctionOnceDriftTests`. No
inventory row moved: GR-14.9.22.4-6 and GR-8.4.3.2.4-1 were already CONFORMS, and the golden witnesses them.

The self-review covered the sibling arms. REPLACING and CONVERTING already evaluate each operand once in a single
runtime call, and UNSTRING's `LiveReads.Of` already treats a function result as out of every store's reach. STRING's
D-STR4 still evaluates a function a second time at its transfer. The same GR1 reading questions that, so it is reported
as a lead and left unchanged here, because it reverses a documented determination that a golden pins. The two
may-share predicates (`StorageSharing` and `StorageOverlap`) stay kb/Work PB1911's job.
