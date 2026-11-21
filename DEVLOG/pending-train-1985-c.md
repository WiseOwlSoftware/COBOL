## 1985 train C: twelve fix branches in one landing; 1985 is down to one inventory GAP

Wave 20262131's second 1985 train lands in one pull request. It carries the fix branches of groups p, q (with h and c),
r (with l), x (with o, j and e), s, t, u, v, w, y and z. They were merged into `train-1985-c` on top of train B (#1932) and
gated once. `edition_status.py --edition 1985` read `195 inventory GAP · 8 code reads · 52 open work notes · 16 catalog
prose holes` on train B's head (its integrator's report). On this train's head it reads
`1 inventory GAP · 4 code reads · 32 open work notes · 16 catalog prose holes`. The one GAP left is §1985:III.2 p139
(Input-Output Statement): the compiler holds only 2023's set of eight statements (PB6032).

**Area A in the procedure division (p, PB6018).** At --std 85, a paragraph or section header after a sentence that lost its
period is reported once, at the header. Before, `MOVE "B" TO A` followed by `P1.` read P1 as a second receiver. The area-A
fact that PB6011 gave the parser now has a procedure-division arm (§1985:IV.7.3.3, §1985:IV.7.3.2; a sentence continues in
area B, §1985:IV.7.2.2). Every word operand yields to it, and the recovery reads the same fact. COBOL-2002 and later
editions have no areas and are unchanged.

**Every refusal at 85 cites the 1985 rule (q, PB5993).** The last 175 catalog descriptors were derived from the 1985 text:
126 record their 1985 rule, and 49 record why 1985 has none. An audit of the 363 "no 1985 rule" determinations reversed
four. The catalog test is now strict. Group z extended this to the run time (PB6028). Intrinsic breach messages cite the
1989 amendment's rule, and a reference modification out of range at 85 no longer names EC-BOUND-REF-MOD. Both go through
one chooser, `ExceptionEngine.EditionRule`. Six later-edition diagnostics that fired beside an 85 edition gate are silent
there now. CALL and CANCEL literal faults have their own code, COBOL7736.

**One run-time answer to an argument breach at 85 (r, PB6019).** A negative FACTORIAL argument, a zero MOD divisor or an
invalid date used to return 0 silently at 85, while a non-integer integer argument ended the run. Every breach now ends the
run through `ExceptionEngine.ArgumentError`, with the function, the value and the rule (§1989:A.2.2 p3; §1993:p.B-46
allows termination). Group z renamed the activation's flag `ICobolProgram.IsCobol85`. From 2002 nothing changes.

**The last rows of the 85w mint (x, PB5453).** Seventeen definitional and informative derivations now name the member where
the compiler gives the defined term its meaning. The Physical Page row has a self-checking witness, and Mass Storage File
closes on the controller's derivation refuter.

**Citation tooling (s, PB4976/PB5059/PB5446/PB5365).** The 1985 citation universe ends at the first amendment, and one
designator grammar serves the parser, the checker and the suggester. The audit refuses an unreadable designator. On this
train, the C# part (`--canonicalize src/ tests/`, 3,749 lines) was run and then reverted. Gate 000003 showed that the
canonicalizer damages run-time message text in three ways: it glues `SR12 a)2.` as `SR12a2.`, it drops a closing
parenthesis when a label's `)` also closed the message, and it rewrites a label that follows an interpolation hole.
None of these was visible to the audit, and 57 goldens still quoted the old text (PB6035). The run did make 14 citations
readable that named a wrong rule, and those are fixed at their text (for example, §2023:12.3.8.3 SR6 had been cited with
a sub-item b it does not have). The construct rows' citations are canonical at their source. Thirty-eight §1985
amendment-page citations from branches cut before the rule moved to §1993:p.B-46 and §1989:p.A-28. The shrink-only
baseline holds 6,029 entries. That is the debt that stood before, plus seven list-item designators that have no canonical
form (PB6015).

**The 1985 catalog's amendment attribution (t, PB5063/PB5093/PB5036/PB5094/PB1553/PB3965).** Every change row now resolves
after the mint, and a cross-check holds each Errata amendment correction to the row that holds its text: 27 rows lacked
their amendment, and none do now. The sign-group 85 to 2002 edges are quoted from both editions' texts.

**Compiler behaviour at 85 (u, PB5913/PB5965/PB5138/PB4978/PB5633/PB5559).** A PICTURE of 64M positions took 36 s to check
and now takes 0.94 s, through a run-length view. The program-name scope rule is written once. The 85 reserved-word flags
are read from §1985:IV.8 itself.

**The harnesses state their source format (v, PB1757/PB5048/PB5057).** No test harness lets the format detector choose.
`DialectBehavior.SumDataNameRules` (VCR row 7.89) replaces three inline edition arms.

**Six 1985 rulings (w, PB5790 and five more).** SIGN beside BLANK WHEN ZERO is legal at 85 (Table 1 of §1985:XIII.3.9.3
SR11). One question, `PicInfo.PictureIsSignedNumeric`, now serves every SIGN subject rule. The other five rulings are
pinned by goldens and docs/conformance entries.

**Report Writer at 85 (y, PB6024-PB6027).** A 1985 report has no page width, so only the record cuts a line. This is
`DialectBehavior.ReportPageWidth`. Groups v and y had both taken ordinal 33 and VCR row 7.89, so on this train y's became
34 and row 7.90. A relative NEXT GROUP larger than the page limit is legal. The report file's FD refuses DATA RECORDS.

**On the train itself.**
- PB6034 was root-caused by the controller. It was not a flake: another agent's `Get-Process dotnet-stack,cobol |
  Stop-Process -Force` had killed train B's gate compiles. `forbidden_commands.py` rule 8 now refuses a kill by process
  name (+21 self-test cases, 323/337 RED on the old hook), MANDATORY-PRACTICES gains P18, and the WiseOwl adapter reads
  exit 4294967295 (TerminateProcess(-1)) as a crash, so a killed compiler is NO VERDICT, not FAIL.
- Train B's review lead R1-1: `rule_moves.recorded_moves` follows the move batches in the order they were applied (an
  `applied` place in each tracked copy), not in file-name order, with self-test case 08.
- R1-2: PB904 closed, because de-editing is COBOL-85 (§1985:VI.6.19.4 GR4b; a probe at 85 gives `01234.56`).
- `backfill-editions` was run on four new notes. PB2041 is [1985, 2023], because group o's addendum adds a 1985 wrong
  citation to its screen.
