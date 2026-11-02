      *>--- conformance-case
      *> id: pb4224-literal-where-a-statement-begins
      *> rules: [FMT-14.9.19.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4224 - a literal written where a statement must begin
      *> is named by the diagnostic, not left the bare COBOL0001. ISO
      *> 14.5.3.1 "A procedure division statement begins with the first
      *> word of the statement name" (cite.py --check 14.5.3.1 -> OK)
      *> and 14.5.1 "Statement names are identified in Table 12,
      *> Procedural statements" (cite.py --check 14.5.1 -> OK): the
      *> format of IF prints "IF condition-1 THEN statement-1", so the
      *> integer literal 5 after THEN is no statement. The same
      *> refusal at every edition: no edition's statement begins with
      *> a literal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P4224A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-N PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN.
           IF W-N = 1 THEN 5 END-IF.
           STOP RUN.
