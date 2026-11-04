## Entry 3109 — 2026-11-04 14:51 PST — Filed 38 kb/Work notes (PB4616 to PB4653) from the new-defect leads of 33 landed branches

The enqueue unit's clerk read the 33 landed branches' reports and filed every open lead as a kb/Work note, one note per mechanism, each re-read against origin/main before filing. No compiler code changed; repros are recorded "as reported, not re-run".

Notes, by id:
- PB4616 land_lane.py status keeps 15 rows reading HALT or retry for branches whose pull request merged long ago (tooling, process_only).
- PB4617 semgrep verify.py names DiagnosticCatalog.cs unreadable and answered differently on two runs (leads of PB3600, PB4602, PB4606 R1-2; one note).
- PB4618 partial multi-character currency position in NvScan (analysis, PB4511).
- PB4619 InitializeBinder.ValueIsNotAMoveResult cites section 8.6.4 wrongly (comment).
- PB4620 the pb4513 CONTROL golden does not witness the TERMINATE restore.
- PB4621 CobolEdit.DeEdit reads a '-' character-1 left of a floating string as the negative sign (wrong_answer, PB4514 R3-1).
- PB4622 value-preserving functions over a float item are classified Approximate (analysis, wrong_answer, owner determination, PB4516).
- PB4623 an infinite approximation lands differently alone and beside an operator (analysis, PB4516).
- PB4624 stale saturation comments in FromDouble and IntrinsicRenderer (PB4516 R1-1).
- PB4625 native integer power with a literal exponent always takes the 256-bit path, 38 percent slower (PB4569 R1-3).
- PB4626 SR14 recovery cascade, `01 X TYPE T ALIGNED.` draws COBOL2150 + COBOL1942 + COBOL0881 (misleads_diagnosis, PB1654).
- PB4627 BindBareOperandStandalone over-wide reading hides a COBOL2318 (misleads_diagnosis, PB4606 R1-1).
- PB4628 F.1 names LAST while GnuCOBOL's 85 list reserves it (analysis, PB3957).
- PB4629 ORDER-flip leftovers: five comments and VCR row 7.19 (PB3957 R1-1, R1-2).
- PB4630 guard-verdict.sh header describes the wrong comparison basis (PB2509 R1-1).
- PB4631 CobolParserCore.g4 comment names the renamed Format2SendingOperand (PB2144 R1-1).
- PB4632 DESIGN-frontend-grammar.md narrates pre-PB2144 history (PB2144 R1-2).
- PB4633 COBOL1760 text false for a REPOSITORY PROGRAM specifier no provider answered (misleads_diagnosis, PB4509).
- PB4634 CallBinder argument-loop comment on the free-NULL order (PB4508).
- PB4635 parenthesized bare data reference as an intrinsic argument accepted (under_rejects, PB4507).
- PB4636 LENGTH / BYTE-LENGTH arms still read the tree shape, not FunctionValue (PB4507 R2-1).
- PB4637 UDF activation channel behavior change unpinned (PB4507 R2-2).
- PB4638 kept TYPEDEF entry's SAME AS not expanded (misleads_diagnosis, PB4602 R2-1).
- PB4639 two DataBinder comments say a refused clause is dropped (PB4602 R2-2).
- PB4640 DIVIDE ... REMAINDER with a quotient past 34 digits (wrong_answer, PB4599).
- PB4641 SDIDI-by-SDIDI final-transfer quotient double rounding at a 35th-digit tie (analysis, unprobed, PB4599).
- PB4642 INTRINSICS_DESIGN.md still names TryQuotientDec (PB4599 R2-1).
- PB4643 ExactFinalQuotient summary still says "quotient of their images" (PB4599 R2-2).
- PB4644 "NOT rejected at 85" comment false in six ALIGNED goldens and CheckAlignedClausesPass (PB4603).
- PB4645 COBOL1941 without the entry name for `01 W-A MOVE.` (misleads_diagnosis, PB4604).
- PB4646 dangling level-number before a section header draws COBOL0901 over COBOL0307 (misleads_diagnosis, PB4604 R1-1).
- PB4647 RunTextStages re-runs per comment-entry candidate, quadratic (PB4487).
- PB4648 vacuous negative assertion in a DiagnosticPositionTests theory case (PB4485).
- PB4649 token screens skip repository expansion text (PB4483).
- PB4650 placement-(b) CS0012 import backend error (crashes, PB4483).
- PB4651 pair mode compares only the clauses of TYPE formals (analysis, PB4491).
- PB4652 other SR1 SELF refusals report at the statement, not the word (misleads_diagnosis, PB4444 R1-1).
- PB4653 OoPredefinedSearchRoot comment and the SR4 cursor (PB4444 R1-2).

Also: PB4484 item 1 (the FragmentParse.ParseTokens comment) struck, because PB4483 made it accurate.

Leads not filed, with the reason:
- Fixed on their own branch: every "fixed" or "FIXED in" item of PB4514, PB4573 R1/R2, PB4239 R1, PB4609, PB4589 R1, PB4602 R1, PB4599 R1 and R2-fixed items, PB4485 R1-1, PB4494, PB4491 R1-1.
- Already filed or landed: PB4573 lead 1 is PB4589 (landed); PB4573 R3-1 is PB4590 (landed); PB4239 lead 1 is PB4604 (landed) and lead 2 is PB4605; PB4446 R1-1 is PB4606 (landed); PB4569 leads 2 and R1-1 are PB4600; PB4569 lead 3 is PB4570; PB4488 R1-4 is PB4574; PB4567 is pb-fix-lane-unsited's own note.
- Stale (a later branch fixed the named text): PB4572 R1-1, R1-2, R1-3 and PB4488 R1-1, R1-2, R1-3 (PB4573 and PB4572 landed the withdrawn-word and synonym fixes); PB1654 R1-1 (PB4603 rewrote DescribeAlignedSubject); PB4569 lead 1 and R1-2 (PB4599 landed the exact final-transfer quotient and its goldens); PB4491 R1-2 and R2-1 (the BinderDriver and drift-test comments no longer carry the quoted text).
- Observations the reports say carry no note: PB4512 (ALL-literal VALUE), PB4509's guard-verdict observation (folded into PB4630), PB4572 (shared CobolWordsOp description), PB4589 lead 1 (COBOL1639 noise), PB3957 lead 2 (declaration not drawing COBOL0901 beside a reference), PB4494 (verb-worded message).
- History kept as written: the PB3600 R1-1 gate-line lead, the PB4604 R1-2 count lead, PB4604 item 2.

