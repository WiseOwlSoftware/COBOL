## Entry 2765 — 2026-10-29 20:11 PDT — PB1908: STRING with overlapping operands runs in place, as GnuCOBOL's libcob does; documented as D-STR1 to D-STR4 and pinned by five goldens

**The case.** §14.9.43.4 GR10 and Annex A.2 item 60 leave a STRING whose sending, delimiter, receiving or pointer operands
share storage undefined; §4.4 2) makes a run unit that allows it conforming. The owner's PB1907 decision (document what we do,
follow GnuCOBOL where it measurably differs, pin it with a golden) had not been applied to STRING: the row `GR-14.9.43.4-10` closed
on an owner-signed derivation (kb/Work PB1636, "any behavior conforms") and no witness pinned the behavior.

**Re-probe, on this build and on cobc 3.2.0 under WSL.** Before the change `StringEmitter.EmitString` read every sender and
identifier delimiter once, before the first character moved. libcob (`cob_string_append`) moves with `memcpy` into
`string_dst->data` and reads `src->data` and the delimiter's storage on each call. Measured differences: `STRING "XY" R1(1:2)
R1(3:2) INTO R1` over `ABCDEFGHIJKL` gave `XYABCDGHIJKL` (libcob `XYXYXYGHIJKL`); a delimiter `R3(1:1)` the first transfer overwrote
gave `x12x34ghijkl` (libcob `x12defghijkl`); a table element, a numeric DISPLAY item and a group that contain the receiver differed
the same way; `FUNCTION UPPER-CASE(F)` over the receiver gave `xyABCDEF` (libcob `xyXYCDEF`). The pointer sub-case already agreed
(`cob_string_init` reads it once, `cob_string_finish` stores it last), and so did one sender overlapping its own transfer (glibc's
`memcpy` moves it as a snapshot: `ABABCDEFIJKL`).

**The fix, the UNSTRING shape.** `EmitString` stores the working image into identifier-3 before each transfer after the first whose
sender or delimiter `StorageOverlap.MayShareStorage` cannot prove disjoint from it (`ReadsStorageOf`), and reads that operand
again (`ReadOperand` records the locals to re-assign). A function operand whose argument may share the receiver's storage is
rendered inline, so it runs once, at its transfer (RANDOM or a user-defined function is never run twice); a function over disjoint
fields keeps the up-front evaluation of §14.6.4 7). A STRING over disjoint operands, or whose only reachable operand is its first
sending, emits the single store it always did. The decision rule is the finer predicate that kb/Work PB1911 keeps; UNSTRING still
asks `StorageSharing.MayShare` until PB1911 moves it.

**Documentation and witnesses.** `docs/CONFORMANCE.md` §3.1 gains D-STR1 (sender), D-STR2 (delimiter), D-STR3 (pointer read once,
stored last) and D-STR4 (function operands); `docs/STRING_OPS_DESIGN.md` D4 and `docs/DESIGN.md` describe the in-place model.
Five goldens under `conformance-suite/cases/14.9.43-STRING/` (`pb1908_string_overlap_{sender,delimiter,pointer,linkage,function}`,
accepted from 2002 where the rule exists) and `StringLiveReadDriftTests` (twelve tests over the emitted C#: disjoint, sibling,
first-sending-only, later sender, delimiter, inline sender, REDEFINES, RENAMES, LINKAGE, function over the receiver, function over a
disjoint field, and the one-predicate rule). Every golden leg gave identical lines on cobc 3.2.0 and this build; 1,350 random
STRING statements over one record (reference-modified senders, delimiters and pointers of the receiver, overflow phrases) did too.

**Rows.** `GR-14.9.43.4-10` is recorded CONFORMS with the five goldens and the drift tests as witnesses. A row with a spec-derived
test may not also carry a derivation (design doc 1.1 refusal 1), so DRV-GR-14.9.43.4-10 left: its register row in §8 and its place in
`SpecTraceabilityInventoryDriftTests.TheRowsClosedOnADerivation_AreTheEnumeratedPopulation`. That is an owner-signed derivation
retired by a later owner-directed documentation campaign; it is named in the branch report for the owner to confirm. No GAP moved
(the row was closed before). kb/Work PB1908 is landed with `closes_rows`.

**Found and left.** STRING of non-digit characters into a numeric DISPLAY item stores a decoded number where libcob keeps the bytes
(`STRING "AB" INTO N` over 123456: `003456` against `AB3456`); that is the typed-native numeric representation and the documented
decode of invalid content (§14.6.13.2), independent of overlap, and is recorded in the branch report.

**Review round 1 (two blocking findings, one root).** `ReadOperand` rendered a function operand whose argument
`MayShareStorage` could not prove disjoint inline into its transfer, after identifier-3 was read. A function is an identifier of
the statement, so §14.6.4 7) and §14.9.49.4 GR11 put its range tests before identifier-3's subscript test: a `STRING FUNCTION
UPPER-CASE(LK-X(1:L)) INTO T(I)` over a LINKAGE `LK-X` raised EC-BOUND-SUBSCRIPT where `main` (and the same statement over a working-storage
argument or a plain `LK-X(1:L)`) raised EC-BOUND-REF-MOD (R1-1); and `ReadsStorageOf` answered "may share" for every shape it did not
walk, including `FUNCTION UPPER-CASE(WX)(1:L)` over disjoint working storage (R1-2). Fix: the first sending is never reachable (no
store precedes it, so GR10 cannot apply), so its operands are always read once, in order; `ReadsStorageOf` also walks arithmetic
arguments and the positions of a reference-modified result (`StorageOverlap.MayShareStorage(DataItem, Place)` judges a position's item).
A later sending's function that may share identifier-3 is identified up front like the rest (its failure interrupts the statement before
"XY" moves: the second review's K2 and B1 legs) and evaluated again at its transfer (D-STR4); only FUNCTION RANDOM (§15.75.3 5) and a
shape the walk does not know run once, inline (`Reach.Repeatable`). Golden `pb1908_string_function_order` (legs F1 to F7 and a value
leg; mutating the fix back gave EC-BOUND-SUBSCRIPT on F2, F4 and F5) and seven new `StringLiveReadDriftTests` tests.

**Review round 2 (one blocking finding).** The round-1 fix replaced the notes of row `GR-14.9.43.4-10` with a review narrative that said a later function is "evaluated once at its transfer" (stale after the second review) and dropped the row rationale. The row now carries the current determination once: the GR10 / Annex A.2 item 60 citations, the owner decision (PB1907/PB1908), the GnuCOBOL 3.2 measurement, the function rule (identified up front and evaluated again at its transfer; only `FUNCTION RANDOM` and unwalked shapes run once) and why `DRV-GR-14.9.43.4-10` left (`record_verdicts` batch, GAP 21 to 21). The lead that the operand walk belongs beside `StorageOverlap` with `InspectEmitter.MayReadStorageOf` (the report's lead 2) stays for the clerk: neither file is changed here.

