## PB3454: one COMMIT over several files commits PER FILE, as designed (owner 2026-10-19: "Per file (Recommended)")

**The question.** The scope decision of 18:58 PDT (kb/Work PB3337, "Option A": the whole commit-and-rollback module) released
the one question the PB3330 design's fifth review round had left: when one COMMIT covers several files under APPLY COMMIT and
the medium refuses the commit of one of them, does the compiler promise a PER-FILE commit, as DESIGN-runtime-library.md
2.2.1.10 states (files committed one at a time in open order; the refused file and those after it at the previous COMMIT once
item 8's unconditional ROLLBACK has run; EC-I-O-PERMANENT-ERROR with '30' on the refused file), or ALL-OR-NOTHING across files,
which needs a durable commit record outside any one file that every file's recovery consults, a two-phase commit that changes
both designs and both simulators. The spec is silent on a COMMIT that fails part way; GnuCOBOL has no rollback at all; IBM
and Micro Focus get cross-file atomicity from a transaction server or a file server's log, not from the compiler. The enqueue
unit's clerk had filed the question as kb/Work PB3454 (decision, `status: owner`) from the branch review's lead R1-1.

**The answer.** Put to the owner as one bare choice with the per-file option marked recommended, answered verbatim: **"Per file
(Recommended)"**. The design stands as written: nothing in 2.2.1.10, 2.2.2, the conformance document's commit row or the two
simulators changes, and no commit record outside any one file is built. PB3454 records the answer and is `landed`; PB3337
records it beside the question; PB3338, PB3330 and plan §0 no longer call it the owner's open question. No compiler source
changed; nothing was dispatched.
