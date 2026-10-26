## Entry 2622 — 2026-10-26 12:07 PDT — PB3838 filed: every GnuCOBOL defect and ISO departure our differentials find gets a reproducible record for upstream

The owner asked for a plan to report GnuCOBOL's inconsistencies with the ISO standard and its bugs upstream, and set the
bar: at the least, track everything needed to file an issue or, ideally, a patch later, "detailed enough to repro the
issue. Minimal test cases, cites to the ISO spec, etc." Today the findings exist only as prose in the fuzz nights'
evidence documents (`GNUCOBOL-SIDE` 54 signatures on the fourth night, 50 on the fifth), and their witness programs only in
git-ignored run directories on one machine.

PB3838 is the campaign's lead note. It states the eight items every record carries (minimal repro, exact cobc command and
version, observed result, checked ISO citation and edition, class, our compiler's result, the GnuCOBOL fix site, the
upstream state with a draft issue), what is excluded (latitude, harness artifacts, our documented extensions), and the
plan in tier order: an `upstream` note kind in `work.py`, a harvest of every night first (the repros are on one machine),
reduction and citation by Opus adjudicators with a refuter on every published claim, filing, the fuzz triage brief filing
to this bar from then on, and the owner-gated filing itself (re-run on GnuCOBOL's development head, duplicate search,
the owner's approval of each text; the FSF copyright-assignment question before any patch).

Rows re-verdicted: none (`closes_rows: []` with a reason). No code changed.

