      *>--- conformance-case
      *> id: pb3830_phrase_association
      *> rules: [GR-14.5.3.3-4]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3830. ISO 14.5.3.3 (its closing paragraph, after list item 4):
      *> "Any phrase encountered is the next phrase of the most-recently
      *> preceding unterminated statement with which that phrase may be
      *> syntactically associated. If all permitted occurrences of a phrase
      *> have already been specified for a given statement, a subsequent
      *> occurrence of that phrase is not syntactically associated with that
      *> statement." (cite.py --check 14.5.3.3 -> OK; cite.py labels the
      *> paragraph "4)", the rendered page prints it unindented after 4) b).)
      *> ISO 14.5.1: "Any statement with a conditional phrase that is not
      *> terminated by its explicit scope terminator is a conditional
      *> statement" (cite.py --check 14.5.1 -> OK).
      *>
      *> Each leg nests a statement in a [NOT] phrase of another. Legs 1-6:
      *> the inner statement cannot take the phrase that follows it (STRING
      *> has no SIZE ERROR phrase, ADD no OVERFLOW, EXCEPTION, AT END or WHEN
      *> phrase), so the phrase is the OUTER statement's and the program is
      *> legal; a compiler that gave it to the inner statement refuses it.
      *> Legs 7-8: the inner ADD CAN take NOT ON SIZE ERROR, so the phrase
      *> and the END-ADD after it are the INNER ADD's, and the outer ADD's
      *> ON SIZE ERROR runs to the period (14.5.3.3 3)). The outward reading
      *> prints "7 C=1 F=-" and "8 NOT REACHED"; the rule prints the .out.
      *> Expected values: 5 + 9 is too large for PIC 9, so ADD A TO B takes
      *> ON SIZE ERROR and leaves B unchanged (14.7.5); "ABCD" overflows the
      *> two-character S2 (14.9.43.4); a CALL of a program that does not
      *> exist takes ON EXCEPTION (14.9.4.4); READ of an empty file takes
      *> AT END; the SEARCH finds "C" at the third element.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3830PA.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "PB3830PAS.TMP"
               ORGANIZATION IS SEQUENTIAL.
           SELECT RELF ASSIGN TO "PB3830PAR.TMP"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RK.
           SELECT PRTF ASSIGN TO "PB3830PAP.TMP"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD SF.
       01 SF-REC PIC X(4).
       FD RELF.
       01 RELF-REC PIC X(4).
       FD PRTF LINAGE IS 2 LINES.
       01 PRT-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 RK   PIC 9(4) VALUE 5.
       01 A    PIC 9 VALUE 5.
       01 B    PIC 9 VALUE 9.
       01 C    PIC 9 VALUE 0.
       01 D    PIC 9 VALUE 0.
       01 F    PIC X VALUE "-".
       01 G    PIC X VALUE "-".
       01 S1   PIC X(4) VALUE "ABCD".
       01 S2   PIC X(2) VALUE SPACES.
       01 T-AREA VALUE "ABC".
          05 T-E PIC X OCCURS 3 INDEXED BY TI.
       PROCEDURE DIVISION.
       MAIN-P.
      * 1-2. NOT ON SIZE ERROR after a STRING is the ADD's phrase.
           ADD A TO B
               ON SIZE ERROR STRING S1 DELIMITED BY SIZE INTO S2
               NOT ON SIZE ERROR MOVE "N" TO F
           END-ADD
           DISPLAY "1 B=" B " F=" F " S2=" S2
           ADD 1 TO C
               ON SIZE ERROR STRING S1 DELIMITED BY SIZE INTO S2
               NOT ON SIZE ERROR MOVE "N" TO F
           END-ADD
           DISPLAY "2 C=" C " F=" F
      * 3. NOT ON OVERFLOW after an ADD is the STRING's phrase.
           MOVE SPACES TO S2
           STRING S1 DELIMITED BY SIZE INTO S2
               ON OVERFLOW ADD 1 TO D
               NOT ON OVERFLOW MOVE "N" TO G
           END-STRING
           DISPLAY "3 D=" D " G=" G
      * 4. NOT ON EXCEPTION after an ADD is the CALL's phrase.
           CALL "PB3830XX"
               ON EXCEPTION ADD 1 TO D
               NOT ON EXCEPTION MOVE "N" TO G
           END-CALL
           DISPLAY "4 D=" D " G=" G
      * 5. NOT AT END after an ADD is the READ's phrase.
           OPEN OUTPUT SF
           CLOSE SF
           OPEN INPUT SF
           READ SF
               AT END ADD 1 TO D
               NOT AT END MOVE "N" TO G
           END-READ
           CLOSE SF
           DISPLAY "5 D=" D " G=" G
      * 6. WHEN after an ADD in SEARCH's AT END is the SEARCH's phrase.
           SET TI TO 1
           SEARCH T-E
               AT END ADD 1 TO D
               WHEN T-E (TI) = "C" MOVE "W" TO G
           END-SEARCH
           DISPLAY "6 D=" D " G=" G
           MOVE 0 TO C
           MOVE "-" TO F.
      * 7. The inner ADD takes NOT ON SIZE ERROR and END-ADD; the
      *    DISPLAY is in the outer ON SIZE ERROR (size error here).
           ADD A TO B
               ON SIZE ERROR ADD 1 TO C
                   NOT ON SIZE ERROR MOVE "N" TO F
               END-ADD
               DISPLAY "7 C=" C " F=" F.
      * 8. The same shape with no size error: nothing in the outer
      *    ON SIZE ERROR runs, so F keeps "N" and no line 8 prints.
           MOVE 1 TO B.
           ADD A TO B
               ON SIZE ERROR ADD 1 TO C
                   NOT ON SIZE ERROR MOVE "Y" TO F
               END-ADD
               DISPLAY "8 NOT REACHED".
           DISPLAY "8 B=" B " F=" F.
           STOP RUN.
