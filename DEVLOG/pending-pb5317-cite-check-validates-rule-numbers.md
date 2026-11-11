## PB5317: cite.py --check validates every part of a citation, in every edition, and the citation audit sweeps the repository with it

kb/Work PB5317. `python scripts/spec/cite.py --check "§1985:XI.4.4.4 GR99" "x"` printed `OK` on main: a 1985 citation's rule
number was never compared, and a one-character quotation matched almost any clause. The 2023 arm was looser than it looked
too: it compared the top-level ordinal one line at a time and never the rule's kind, its sub-item or a restarted list, so
`§2014:13.18.14.3 GR6c` passed with item b)'s text.

**The one checker.** `cite.resolve` checks the clause, the rule list of the cited kind, the ordinal, the sub-item path, a
`Format n` or `Table n`, and that a quotation of at least four real words lies inside the cited unit. Each failure is a
named kind (`no-ordinal`, `text-not-in-rule`, ...) and, where the checker finds it, carries the right citation: GR10 with
rule 7's text now answers `under GR7, not GR10`. Rule boundaries are the rule catalog's rows, anchored in the transcription
(2023 and 1985: every row anchors); 2002 and 2014, which have no catalog yet, run in a marked LABEL MODE. Sub-items are read
by label style, since the transcription flattens some nested lists onto column 0. `citation.py` now reads `GRLm.n` in every
edition, because the 2023 catalog restarts lists too.

**The sweep.** The CITATIONS audit gains RESOLVE, which runs the checker over every edition-form citation it reads, plus the
inventory's 6,474 store records: 99,616 citations, 1,590 refused (2023 1,578, 1985 8, 2002 4), 1,484 of them a rule
block's clause written as its statement's (`§2023:14.9.39 SR17` for `14.9.39.3 SR17`). The 1,589 in the gate's population are
`scripts/spec/data/cite-resolve-baseline.json`, debt that may only shrink: a new failure is RED, and so is a listed one that
no longer fails. Measured: 2,709 of the 2,843 1985 rule citations carry no quotation, so an ordinal that names a real but
wrong rule is still invisible there.

Filed: PB5318 (the audit's 2023 ordinal family is now a second reading of RESOLVE) and PB5319 (the catalog's general-format
rows miscount their formats and cut their text at 4,000 characters).
