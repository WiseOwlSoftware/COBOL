      *>--- conformance-case
      *> id: 1985-GR-III_2-p219
      *> rules: [1985-GR-III.2-p219]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p219 ((section preamble)). A self-checking output
      *> test: "A device dependent concept defined by the implementor".
      *> WiseOwl COBOL defines the physical page of a sequential file
      *> by the determination docs/conformance/7/DOC-A.1-130.md: a page
      *> boundary is a form feed, and WRITE ... AFTER ADVANCING PAGE
      *> presents its record on the next physical page
      *> (§1985:VII.4.7.4 GR16h), after one. The program reads its file
      *> back one byte per record (DOC-A.1-146) and prints PASS only
      *> when one form feed separates AAA (page 1) from BBB (page 2);
      *> any other medium layout prints FAIL with what it found.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P219PG.
      * 1985 III.2 p219: the physical page is a device dependent
      * concept defined by the implementor. WiseOwl COBOL's physical
      * page (docs/conformance DOC-A.1-130): a page boundary on a
      * sequential file is a form feed, and WRITE ... AFTER ADVANCING
      * PAGE (VII.4.7.4 GR16h) presents its record on the next
      * physical page, after a form feed. The file is read back one
      * byte per record (DOC-A.1-146: a fixed-length record
      * sequential READ takes the next integer-1 bytes) and checked.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS FORM-FEED-CHAR IS 13.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PFILE ASSIGN TO "p219pg.txt".
           SELECT BFILE ASSIGN TO "p219pg.txt".
       DATA DIVISION.
       FILE SECTION.
       FD PFILE.
       01 PREC PIC X(3).
       FD BFILE.
       01 BREC PIC X.
       WORKING-STORAGE SECTION.
       01 EOF-SW PIC X VALUE "N".
       01 N-BYTES PIC 99 VALUE 0.
       01 BUF PIC X(30) VALUE SPACES.
       01 N-FF PIC 99 VALUE 0.
       01 N-AAA PIC 99 VALUE 0.
       01 N-BEFORE PIC 99 VALUE 0.
       01 AFTER-FF PIC X(3) VALUE SPACES.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT PFILE.
           MOVE "AAA" TO PREC.
           WRITE PREC AFTER ADVANCING 1 LINE.
           MOVE "BBB" TO PREC.
           WRITE PREC AFTER ADVANCING PAGE.
           CLOSE PFILE.
           OPEN INPUT BFILE.
           PERFORM READ-ONE UNTIL EOF-SW = "Y".
           CLOSE BFILE.
           INSPECT BUF TALLYING N-FF FOR ALL FORM-FEED-CHAR.
           INSPECT BUF TALLYING N-AAA FOR ALL "AAA"
               BEFORE INITIAL FORM-FEED-CHAR.
           INSPECT BUF TALLYING N-BEFORE FOR CHARACTERS
               BEFORE INITIAL FORM-FEED-CHAR.
           IF N-FF = 1 AND N-AAA = 1 AND N-BEFORE < 26
               MOVE BUF (N-BEFORE + 2 : 3) TO AFTER-FF.
           IF N-FF = 1 AND N-AAA = 1 AND AFTER-FF = "BBB"
               DISPLAY "PHYSICAL-PAGE PASS"
           ELSE
               DISPLAY "PHYSICAL-PAGE FAIL FF=" N-FF " AAA=" N-AAA
                   " AFTER=" AFTER-FF.
           STOP RUN.
       READ-ONE.
           READ BFILE AT END MOVE "Y" TO EOF-SW.
           IF EOF-SW = "N"
               ADD 1 TO N-BYTES
               IF N-BYTES < 31
                   MOVE BREC TO BUF (N-BYTES : 1).
