- **D-DEBUG-PROCEDURE — when a procedure-name or ALL PROCEDURES USE FOR DEBUGGING section runs, and what DEBUG-ITEM holds** (kb/Work PB5083). The
  text is X3.23-1985's Debug module, USE FOR DEBUGGING general rules 3, 4, 9 and 13 to 22 (§1985:XV.3.2.4); the design is `docs/CONTROL_FLOW_DESIGN.md`
  D20. **Determination (the text decides, no latitude):** one procedure-name is one trigger point. A paragraph-name subject runs the section immediately
  before that paragraph executes; a section-name subject runs it once each time control reaches the section from OUTSIDE it, with DEBUG-NAME the
  section (GR3 a, GR13): by a transfer made in another section, by the previous section's last paragraph falling through, or by a CALL that enters
  the program there. A transfer made from one of the section's own paragraphs, a loop `GO TO P1` back to its first paragraph, is no new
  execution of the section and runs nothing (kb/Work PB5414), and neither does control entering the section at a later paragraph, by a GO TO or by
  a qualified reference `P2 OF S2` (GR9: a qualifier is no reference). A PERFORM or SORT/MERGE procedure that names only part of the section
  executes some of its paragraphs and not the section, so it runs nothing either; one whose range runs from the section's first paragraph to its
  last does (PERFORM S2, PERFORM S2-P1 THRU S2-P9). ALL PROCEDURES is every procedure-name outside a debugging section, sections and paragraphs, the declarative
  sections that are not debugging sections included (GR4), so a section and its first paragraph are two triggers, the section first (the order CCVS
  DB105A's name list shows); a paragraph-name omitted is no procedure-name. DEBUG-NAME is the name as the USE statement wrote it, qualifiers joined
  by OF or IN (GR13). DEBUG-CONTENTS by cause: 'START PROGRAM' for the first execution of the first nondeclarative procedure (GR16), that is once for a program instance
  (its first CALL, the first after a CANCEL, and every CALL of an INITIAL program): a CALL of the instance in its last-used state enters its first
  procedure as a transfer with no causing statement of the program, DEBUG-CONTENTS and DEBUG-LINE spaces (GR11; kb/Work PB5415); the TO phrase's
  procedure-name as the ALTER wrote it, for the section run immediately after an ALTER that references the subject, DEBUG-LINE the ALTER (GR3 b,
  GR17; DEBUG-CONTENTS is widened to the longest such name, GR15). An ALTER references the subject only when the subject is an ALTERED operand
  (GR3 b, "Immediately after the execution of an ALTER statement which references procedure-name-1"): GR17 b and c make DEBUG-NAME procedure-name-1
  and DEBUG-CONTENTS "the applicable procedure-name associated with the TO phrase of the ALTER statement", which describes the altered name paired
  with its TO name and would be vacuous for a TO operand. A subject that is the TO operand runs its section when control enters it, not at the
  ALTER; GnuCOBOL 3.2 (codegen.c `output_alter`) does the same (held-row adjudication of `1985-GR-XV.3.2.4-17`, kb/Work PB4996). DEBUG-CONTENTS
  by the other causes: spaces for a GO TO (GR18); 'SORT INPUT', 'SORT OUTPUT' and 'MERGE OUTPUT' for the
  implicit transfer to the procedure a SORT or MERGE names, DEBUG-LINE the statement (GR19); 'PERFORM LOOP' on EVERY execution of a PERFORM's range, the
  first included (GR20: §1985:IV.4.4.2 puts the control mechanism's transfer on each execution, and CCVS DB101A's SIMPLE-PERFORM-TEST and
  PERF-ITERATION-TEST read 'PERFORM LOOP' for the first); 'USE PROCEDURE' for a USE procedure about to execute, DEBUG-LINE the statement that causes it
  (GR21); 'FALL THROUGH' for the implicit transfer from the previous paragraph (GR22). **Latitude (the text is silent; CLAUDE.md rule 1):** where a
  section and its first paragraph are both subjects they share the cause of the one transfer that reached them, START PROGRAM included. Witnesses:
  `conformance:cases/_unassigned/pb5083-debug-section-subject`, `-alter`, `-sort-merge`, `-perform-loop`, `-use-procedure`, and the refusal below 1985
  `conformance:wiseowl/diagnostics/pb5083-debug-section-subject-removed-2002`.
