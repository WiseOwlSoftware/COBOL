## Entry 3902 — 2026-11-21 08:19 PST — The 1985 reads' compiler defects: a blank debugging line is a blank line, and a recovered token is no operator

kb/Work PB5453's 1985 reads turned up ten compiler leads (triage `E:/COBOL-coord/scratch/pb5453/out/leads-triage.md`). One
branch, one gate, fixed at the root where the deadline allowed, the rest filed.

**Fixed.** PB6004: at `--std 85` a debugging line of spaces before OBJECT-COMPUTER was refused COBOL7060, though §1985:VI.7.3 p1
makes it "the same as a blank line" and §1985:IV.7.2.3 lets a blank line stand anywhere (both `cite.py --check` OK). Legal
source refused; `DebuggingLineRewriter` now marks a blank extent and never places it. Witness
`pb5453-blank-debugging-line-before-object-computer` (refused COBOL7060 before the fix, runs after). PB6005: after the right
COBOL5506 for `AND NOT NOT 2`, COBOL7672 judged the END-IF that error recovery consumed as a relational operator;
`SeparatorRule.ConditionOperatorViolations` now skips error nodes (`OperatorSpacingDiagnosticTests`). PB6006: two comments
said the object-time debug switch is permanently off; it is on by default (PB4997).

**Filed open, not fixed in this pass:** PB6007 (segmentation reach walk drops an out-and-back path), PB6008 (COBOL7081 depends on
physical order), PB6009 (COBOL2987 drops comment-entry text in its quotation), PB6010 (seven 85 refusals cite the wrong clause),
PB6011 (weak diagnostics for a missing period and `SOURCE-COMPUTER. MERGE.`). Each needs a derivation or a redesign that one
pass under a deadline would have shrunk. The LINAGE read-back lead was ruled CORRECT (PB5815's documented choice), so nothing
was changed. The three stale prose85 goldens were PB5924 scratch products, corrected outside the repository.

