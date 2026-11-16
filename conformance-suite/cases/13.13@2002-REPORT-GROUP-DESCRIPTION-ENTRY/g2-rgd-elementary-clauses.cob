      *>--- conformance-case
      *> id: g2-rgd-elementary-clauses
      *> rules: [2002-SR-13.13.2-1, 2002-SR-13.13.2-7, 2002-SR-13.13.2-11]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.13.2 SR1 - "Report group description entries may
      *>  appear only in the report section." The granted placement:
      *>  three report groups written in the REPORT SECTION are accepted
      *>  and printed.
      *> §2002:13.13.2 SR11 - "The PICTURE, COLUMN, SOURCE, VALUE, SUM,
      *>  and GROUP INDICATE clauses may be written only in an
      *>  elementary entry." The granted form: each of the six is
      *>  written in an elementary entry (level 05 under a level 01
      *>  that carries only TYPE and LINE) and each takes effect.
      *> §2002:13.13.2 SR7 - "The data-name format of the entry-name
      *>  clause shall be specified when the data-name is referenced in
      *>  a GENERATE statement, a USE BEFORE REPORTING statement, as a
      *>  qualifier for a SUM counter, in the UPON phrase of the SUM
      *>  clause, or as an operand in a SUM clause." Every permitted
      *>  reference is written: GENERATE DE-G / D2-G, USE BEFORE
      *>  REPORTING RF-G, T1 OF RF-G (qualifier of a sum counter),
      *>  UPON DE-G, SUM X1 (operand).
      *> DERIVATION (report file read back byte by byte; empty lines
      *>  are not shown):
      *>  GENERATE DE-G (WS-A 3, WS-K "A"): first body group, printed
      *>   at the FIRST DETAIL line (§2002:13.16.33.3 GR5 b 3.).
      *>   GROUP INDICATE is true "only on the first occasion that a
      *>   GENERATE is issued for the current detail group after"
      *>   INITIATE (§2002:13.16.26.3 GR1) -> "DE A 03" (VALUE "DE" at
      *>   column 1, SOURCE WS-K at 4, SOURCE WS-A PIC 99 at 6).
      *>  GENERATE DE-G (WS-A 4, WS-K "B"): the GROUP INDICATE item is
      *>   absent, "as though the entry were omitted"
      *>   (§2002:13.16.39.3 GR2 b) -> "DE   04".
      *>  GENERATE D2-G -> "D2".
      *>  TERMINATE prints the report footing. T1 SUM X1: X1 is in a
      *>   different group, so "adding takes place when the report
      *>   group description containing data-name-1 is processed"
      *>   (§2002:13.16.52.3 GR7 a): 3 + 4 = 07. T2 SUM WS-B UPON
      *>   DE-G: adding "whenever any GENERATE statement is executed
      *>   for a detail referenced by the UPON phrase"
      *>   (§2002:13.16.52.3 GR7 c 2.): two GENERATEs of DE-G, 10 + 10
      *>   = 20 (the GENERATE of D2-G adds nothing; without UPON it
      *>   would be 30). The declarative is "invoked just before the
      *>   named report group is produced" (§2002:14.8.45.3 GR8) and
      *>   shows the sum counters through their group qualifier:
      *>   "RF 07 20"; then the footing line "TOT 07 20".
      *> Fails if any of the six clauses is refused in an elementary
      *> entry, if GROUP INDICATE prints on the second detail, if a
      *> permitted reference is refused, or if a sum is wrong.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RGD01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0D
               ARE 11 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "g2rgd01.txt".
           SELECT CHK ASSIGN TO "g2rgd01.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-A    PIC 99    VALUE 0.
       01  WS-B    PIC 99    VALUE 10.
       01  WS-K    PIC X     VALUE SPACE.
       01  WS-T1   PIC 99.
       01  WS-T2   PIC 99.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LINE PIC X(40) VALUE SPACES.
       REPORT SECTION.
       RD  R PAGE LIMIT 20 LINES HEADING 1 FIRST DETAIL 2
           LAST DETAIL 15 FOOTING 18.
       01  DE-G TYPE DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC XX VALUE "DE".
           05  COLUMN 4 PIC X SOURCE WS-K GROUP INDICATE.
           05  X1 COLUMN 6 PIC 99 SOURCE WS-A.
       01  D2-G TYPE DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC XX VALUE "D2".
       01  RF-G TYPE REPORT FOOTING LINE PLUS 1.
           05  COLUMN 1 PIC XXX VALUE "TOT".
           05  T1 COLUMN 5 PIC 99 SUM X1.
           05  T2 COLUMN 8 PIC 99 SUM WS-B UPON DE-G.
       PROCEDURE DIVISION.
       DECLARATIVES.
       U-RF SECTION.
           USE BEFORE REPORTING RF-G.
       U-RF-1.
           MOVE T1 OF RF-G TO WS-T1.
           MOVE T2 OF RF-G TO WS-T2.
           DISPLAY "RF " WS-T1 " " WS-T2.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R.
           MOVE 3 TO WS-A.
           MOVE "A" TO WS-K.
           GENERATE DE-G.
           MOVE 4 TO WS-A.
           MOVE "B" TO WS-K.
           GENERATE DE-G.
           GENERATE D2-G.
           TERMINATE R.
           CLOSE PRT.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           PERFORM SHOW-LINE.
           STOP RUN.
       TAKE-BYTE.
           IF CHK-REC = SYM-X0A
               PERFORM SHOW-LINE
           ELSE
               IF CHK-REC NOT = SYM-X0D
                   ADD 1 TO WS-I
                   MOVE CHK-REC TO WS-LINE(WS-I:1)
               END-IF
           END-IF.
       SHOW-LINE.
           IF WS-I > 0
               DISPLAY "[" WS-LINE(1:WS-I) "]"
               MOVE SPACES TO WS-LINE
               MOVE 0 TO WS-I
           END-IF.
