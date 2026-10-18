## PB1571: the survey behind D-SMA (a) confirms EC-SORT-MERGE-ACTIVE first, and the order now has source-level goldens

kb/Work PB1571 asked for the GnuCOBOL (and IBM, Micro Focus) survey that CLAUDE.md rule 1 requires and that the
PB1036 implementer could not run for want of a `cobc`. When a RETURN is executed in the range of an input procedure,
or a RELEASE or a format 1 SORT in the range of an output procedure, two exceptions are detected: EC-SORT-MERGE-ACTIVE
(14.9.40.4 GR10 / GR13) and the general EC-FLOW-RETURN / EC-FLOW-RELEASE (14.9.34.4 GR1, 14.9.32.4 GR1).
14.6.13.1.1 leaves the one set to exist undefined, and D-SMA (a) chose -ACTIVE first.

Re-probe, on this build and on `cobc` 3.2.0 under WSL (plain and `-debug`). GnuCOBOL lists all seven EC-SORT-MERGE
names and raises one, EC-SORT-MERGE-FILE-OPEN, in `cob_file_sort_using` and `cob_file_sort_giving`; the MERGE sequence
test is a FIXME in its source and nothing raises -ACTIVE, -RELEASE, -RETURN, EC-FLOW-RELEASE or EC-FLOW-RETURN. With no
check, what it does is the state of its store: a RETURN in an input procedure hands back the record the output
procedure is then owed (the output procedure meets end at once), a RELEASE in an output procedure is kept when it
precedes the first RETURN and is a run-time error '30' after one, and a nested SORT of another file just runs. IBM
Enterprise COBOL documents the restrictions without a condition. Micro Focus documents one fatal error, COBRT099, for
a RELEASE outside the input procedure and a RETURN outside the output procedure. The IBM and Micro Focus findings are
documentation reads from the vendors' pages (the Micro Focus page text could not be fetched; its description is from
a search summary of it), not measured.

Result: no compiler orders the two conditions, so the determination stands and the PB1036 golden and runtime are
unchanged. The survey and the unchecked-behaviour comparison are recorded in `docs/CONFORMANCE.md` §3 D-SMA (a); this
compiler keeps its deterministic checking-off answers (D-SMA (c)) rather than GnuCOBOL's state-dependent ones.

Goldens (the order was pinned at unit level only before): `pb1571_sort_merge_active_vs_flow` (all four paths of the
order, a nested SORT in an output range, and the GR3 EC-SORT-MERGE-RETURN that neither rule pre-empts; every
declarative RESUMEs, which terminates the active SORT under 14.9.40.4 GR17), `pb1571_sort_merge_stray_unchecked` (the
checking-off answers) and the negative `pb1571-sort-merge-active-turn-at-85`. The register note PB1571 is landed with
`closes_rows: []` (an analysis claiming no row).

Review round 1 (one finding): both goldens' header comments said a RETURN written in an input procedure is refused at
compile time under 14.9.40.3 SR3. SR3 refuses only a SORT statement there; a RETURN or RELEASE written in the range is
legal source and raises at run time (probed). The comments are corrected, and the active-versus-flow golden gains steps
7 and 8, a RETURN and a RELEASE written directly in the input and output procedure (EC-SORT-MERGE-ACTIVE, the same
answer as the PERFORMed arms), so the written arm is witnessed rather than described.
