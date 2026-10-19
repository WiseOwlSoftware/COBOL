## Entry 2256 — 2026-10-19 11:32 PDT — PB3396: a keyed store's retirement is refused only at the page it writes past the format's last, never for its reservation's bound

The keyed store engine's retirement (the OPEN I-O fence and every promotion: the CLOSE's, the generation ceiling's, the ledger
checkpoint's) sized its reservation to `Ledger.WriteBound`, an upper bound, and `PagedFile.Reserve` refused it `Boundary`
(answered '30') when the bound passed the format's 2^32 pages. A store a few pages short of the end could therefore not be
opened I-O although its fence wrote no page past the page count, and a store whose last statement left fewer pages than the
bound could not be promoted. The note (a review-round lead) named only the OPEN fence; the re-probe showed every
retirement.

**Re-probe (RED on the old build).** Three `PagedFileBoundaryTests` cases: a store whose recorded count left 3 pages refused
its fence '30', and the first headroom that let it complete left the file 3 pages short of the format's end (the bound's
slack, never a page written); a CLOSEd store at exactly 2^32 recorded pages, whose fence writes no page past the page count,
answered '30'.

**Fix, one place.** `Reserve` takes a `BoundaryAt`. A changing statement's boundary stays its reservation
(`BoundaryAt.Reservation`: refused before its claim, '24' for a WRITE or REWRITE; the implementor's definition that §9.1.13.5
item 4 leaves to it). A retirement has no '24' and its claim is already taken, so its boundary is the page
(`BoundaryAt.Take`): the pages of its reservation past the format's last are `PageOrigin.Beyond`, carry no page number
(`Reservation.TryTake` answers false), and the retirement is refused ('30') only if it writes one. `Retire` is the one
`Take` caller, `Begin` the one `Reservation` caller, and no other site compares with `FormatPages`. Design 2.2.1.9 now states
the two arms in place of the "measured" caveat.

**Tests (the owner's abnormal-path rule).** `PagedFileBoundaryTests`: the fence at every headroom below what it writes ('30',
never the boundary; the committed records readable by an INPUT open; R1 and stamp uniqueness hold) and at exactly the
headroom it needs (it completes and the store fills the format's last page); a fence that writes nothing past the page count
completes at the last page, coherent and NO OTHER. `KeyedFenceBoundaryTests`: INDEXED and RELATIVE OPEN I-O '30' on a real
host file (a never-closed file read beside its open handle under SHARING WITH ALL OTHER), OPEN INPUT still reading it.
`IndexedEnginePermanentErrorWitnessTests.FenceRefused*`: the compiled program after the OPEN's '30' with each USE and FILE
STATUS combination and EC-I-O checking on and off. `BoundaryFile` there now opens I-O at all 2^32 recorded pages in one
step instead of searching for the fewest pages that let the fence complete.

**Branch review round 1.** The reviewer found the CLOSE promotion (the `Take` arm's other callers) untested. Re-probed
on the fix: a CLOSE after statements cannot be made to answer '30' by any headroom, because every statement leaves its
retirement the pages it released (a sweep of 124 single-record cases and the two sweeps in `PagedFileBoundaryTests` met
no refusal), so the design's "the CLOSE answers '30' and the next OPEN I-O the same" was a claim about a state no
statement reaches, and is rewritten to what is measured. The success arm is the regression witness: a CLOSE behind the one
statement that fits, at every headroom (fails on the bound-sized reservation, passes on the page rule), and a CLOSE
after statements spent every FREE page. The design's "fewer than the pages the format has left" is now "no more".

Rows: none claimed, none closed (`closes_rows: []`, with its reason).

