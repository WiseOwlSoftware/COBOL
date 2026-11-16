# PB5824 - the vendor step of the ruling (IBM's Report Writer manual), 2026-11-16

# For kb/Work PB5824 and `docs/conformance/3/D-85-TERMINATE-BREAK.md`: the vendor step the ruling skipped

Written on Monday 2026-11-16 at 14:50 PST by the Mythos lane, after the review of `records-1985-code-reads-3` found
(R1-1) that the determination was recorded as "Decided by the lane" once GnuCOBOL was found not to model the rule,
which skips the next step of CLAUDE.md rule 1's order (IBM Enterprise COBOL or Micro Focus). The finding is right.
This is that step.

## The question (unchanged)
At TERMINATE, is the control break at the highest level of the control hierarchy (FINAL when FINAL is written in
the CONTROL clause), so that the NEXT GROUP clause of a control footing below that level is ignored
(`§1985:XIII.3.16.4 GR3`), or at the most major control data-name (`§1985:XIII.4.7.4 GR1`)? The 1985 text admits
both, and GnuCOBOL does not model the rule (`libcob/reportio.c`: a footing's NEXT GROUP is saved and applied only
when a later line is printed, and `cob_report_terminate` prints every control footing with no level test).

## The vendor's manual
IBM's Report Writer is its own product, and its manual states the rule and shows the case: *COBOL Report Writer
Precompiler Programmer's Manual*, IBM Publication SC26-4301-04, Eighth Edition, January 2002 (read from IBM's
published PDF, `https://publibfp.dhe.ibm.com/epubs/pdf/c2643014.pdf`; a copy is at
`E:/COBOL-coord/scratch/my-20261115/ibm-rw-manual/c2643014.pdf`):
- section 3.13.3, "Effect of NEXT GROUP on Body Groups", rule 5: "If you write a NEXT GROUP clause in a CONTROL
  FOOTING group, report writer checks the level of the control break being processed before putting the clause
  into effect. If the level of the break is higher than the level of this group, the NEXT GROUP clause is ignored."
- the same rule's example ends a report: the last state's footing (`CF FOR STATE` with `NEXT GROUP NEXT PAGE`) is
  followed on the same page by the grand total (`CF FOR REPORT`), with the manual's remark "NEXT GROUP has no
  effect on next TYPE CF group" and the sentence "the CONTROL FOOTING FOR REPORT (the grand total) will not be
  forced onto a new page". The grand total is produced by TERMINATE, so at TERMINATE the break being processed is
  above the state level: it is the report's (FINAL's) level.
- section 4.6.4, "TERMINATE Processing Cycle", step 3: at TERMINATE "each CONTROL FOOTING group is produced, from
  lowest to highest"; and the tutorial (section 1.2) says TERMINATE produces "All CONTROL FOOTINGs up to the
  highest level defined".
Micro Focus's pages for the same clause were not readable from this host (the server refused the fetch), so
Micro Focus is not cited.

## The decision, restated on its proper ground
The order of CLAUDE.md rule 1 decides it: the 1985 text does not control; GnuCOBOL has nothing; IBM's manual gives
the compiler's reading. At TERMINATE the control break is at the highest level of the control hierarchy, FINAL
when FINAL is written in the CONTROL clause, and the NEXT GROUP clause of a control footing below that level is
ignored. Nothing in the compiler changes.

**The register sentence, corrected (it replaces the body of `docs/conformance/3/D-85-TERMINATE-BREAK.md`; keep the
entry's frontmatter and form):** "At TERMINATE the control break is at the highest level of the control hierarchy:
FINAL when FINAL is written in the CONTROL clause, otherwise the major control. The NEXT GROUP clause of a control
footing below that level is ignored (`§1985:XIII.3.16.4 GR3`). The 1985 text also supports taking the level from
the most major control data-name (`§1985:XIII.4.7.4 GR1`), and GnuCOBOL does not model the rule; the choice follows
IBM's COBOL Report Writer (Programmer's Manual SC26-4301-04, section 3.13.3 rule 5: 'If the level of the break is
higher than the level of this group, the NEXT GROUP clause is ignored', with its end-of-report example)."
