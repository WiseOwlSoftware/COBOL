      *>--- conformance-case
      *> id: pb5627-linage-occurs-85
      *> rules: [1985-SR-IV.4.3.8.2.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5627. §1985:IV.4.3.8.2.3 SR4 (cite.py: OK): "Each table
      *> element reference must be subscripted except when such reference
      *> appears" in a USE FOR DEBUGGING statement, as the subject of a
      *> SEARCH, in a REDEFINES clause or in the KEY IS phrase of an
      *> OCCURS clause. The LINAGE clause is none of them, so LINAGE IS
      *> T-LINES, a table element named without a subscript, is refused
      *> at COBOL-85 under that rule. COBOL-85 has no rule that an operand
      *> "shall not be subject to any OCCURS clauses" (that is
      *> §2023:13.18.34.3 SR1), so the refusal must name the 1985 rule.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5627A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PFILE ASSIGN TO "pb5627a.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PFILE LINAGE IS T-LINES LINES.
       01  PREC PIC X(10).
       WORKING-STORAGE SECTION.
       01  W-T.
           05  T-LINES PIC 99 OCCURS 3 TIMES.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
