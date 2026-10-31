*>--- conformance-case
*> id: pb4109-level-01-multiple-line
*> rules: [GR-13.15.4-3, SR-13.18.35.3-10, GR-13.18.35.4-9]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
*> ⛔ A MULTIPLE LINE CLAUSE ON A LEVEL-01 REPORT GROUP ENTRY IS ONE REPORT GROUP WITH SEVERAL REPORT LINES
*> (kb/Work PB4109). The compiler replayed the entry once per LINE operand, and at level 01 every replay opened
*> another report group of the same name, so GENERATE of the group was refused with a false COBOL1920 ("report
*> group is described in more than one report description entry") although the source describes it once.
*>
*> THE RULE. ISO/IEC 1989:2023 §13.18.35.3 SR10: "If more than one integer-1 or integer-2 operand is specified,
*> the clause is referred to as a multiple LINE clause" and its additional rules a) to d) state no level
*> restriction; the only one about another clause (d) is "An OCCURS clause shall not also be present in the same
*> entry". §13.18.35.4 GR9 gives its meaning: "A multiple LINE clause is functionally equivalent to a LINE clause
*> with a single operand, together with a simple OCCURS clause whose integer is equal to the number of operands
*> of the LINE clause, except that the multiple LINE clause allows the report lines to be defined at unequal
*> vertical intervals." The equivalence is of FUNCTION (the report lines it defines), not of syntax: the OCCURS
*> clause's own syntax rules do not carry over (SR25 would demand a STEP phrase of every absolute multiple LINE
*> clause, and none exists), so §13.18.38.3 SR1 a)'s bar on an OCCURS clause at level 01 is not a bar on this
*> clause, and the entry is a repeating entry (§13.15.4 GR3) whose subtree supplies each report line's content.
*>
*> DERIVATION OF THE EXPECTED OUTPUT. The report has no heading groups; D-ABS is the chronologically first body
*> group since the INITIATE, so no page fit test takes place (§13.18.35.4 GR4) and its first line is given by
*> GR5a, the first LINE clause being absolute: integer-1 = 3. GR7a puts the next line at integer-1 = 5. The
*> successive VALUE operands go to the successive repetitions (§13.18.63.4 GR23): A on line 3, B on line 5.
*> D-REL is a body group that is not the first, so the page fit test applies (GR4c): the first LINE clause is
*> relative, the trial sum is LINE-COUNTER (5) plus integer-2 of the one subsequent LINE clause (3) = 8, not
*> past the lower limit 20 (§13.18.57.4), so the fit is successful. Its first line is LINE-COUNTER + integer-2
*> = 5 + 2 = 7 (GR5b3) and the next is 7 + 3 = 10 (GR7b): C on line 7, D on line 10.
*>
*> THE READ-BACK IS BYTE-WISE AND NUMBERS THE LINES (as pb565_report_vertical_repetition): a one-character
*> record on a second SELECT over the same file reassembles the newline-delimited report at every edition, and
*> each printed line is shown with the page line it landed on.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4109L1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb4109l1.txt".
           SELECT CHK ASSIGN TO "pb4109l1.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-L01.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X VALUE "N".
       01  WS-I    PIC 99 VALUE 0.
       01  WS-L    PIC 99 VALUE 1.
       01  WS-LINE PIC X(30) VALUE SPACES.
       REPORT SECTION.
       RD  R-L01 PAGE LIMIT 20 LINES.
       01  D-ABS TYPE DE LINE NUMBERS ARE 3 5.
           02  COLUMN 1 PIC X(1) VALUE "A" "B".
       01  D-REL TYPE DE LINE PLUS 2 PLUS 3.
           02  COLUMN 1 PIC X(1) VALUE "C" "D".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRT.
           INITIATE R-L01.
           GENERATE D-ABS.
           GENERATE D-REL.
           TERMINATE R-L01.
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
           IF CHK-REC = X"0A"
               PERFORM SHOW-LINE
               ADD 1 TO WS-L
           ELSE
               IF CHK-REC NOT = X"0D"
                   ADD 1 TO WS-I
                   MOVE CHK-REC TO WS-LINE(WS-I:1)
               END-IF
           END-IF.
       SHOW-LINE.
           IF WS-I > 0
               DISPLAY WS-L " [" WS-LINE(1:WS-I) "]"
               MOVE SPACES TO WS-LINE
               MOVE 0 TO WS-I
           END-IF.
