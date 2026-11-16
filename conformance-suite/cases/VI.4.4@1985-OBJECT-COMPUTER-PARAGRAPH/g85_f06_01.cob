      *>--- conformance-case
      *> id: g85_f06_01
      *> rules: [1985-GR-VI.4.4.4-2, 1985-GR-VI.4.4.4-5, 1985-GR-VI.4.4.4-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.4.4.4 GR5 - PROGRAM COLLATING SEQUENCE IS REV makes
      *>   the alphabet REV the program collating sequence. By
      *>   §1985:VI.4.5.4 GR4d, "Z" is position 1, "B" 2, "A" 3, "K"
      *>   and "Q" (ALSO) share position 4, and every other character
      *>   follows in native order: Z < B < A < K = Q < the rest.
      *> §1985:VI.4.4.4 GR7 - that sequence decides
      *>   (a) the relation conditions (A > B, Z < A, K = Q);
      *>   (b) the condition-name condition AC-RANGE (VALUE "A" THRU
      *>       "C": under REV "B" lies below "A", so it is NOT in the
      *>       range; native order would put it inside);
      *>   (c) the comparison the CONTROL clause implies: equality of
      *>       control data items follows the relation-condition rules
      *>       (§1985:XIII.4.3.4 GR6a), so WS-CTL changing from "K" to
      *>       "Q" (one position) is NOT a control break and no CF
      *>       line follows "C GEN Q"; "Q" to "Z" is a break, so one
      *>       CF line follows "C GEN Z"; TERMINATE presents the
      *>       control footing once more (§1985:XIII.3.20.4 GR6).
      *> §1985:VI.4.4.4 GR2 - the contained program G85F0601I has no
      *>   OBJECT-COMPUTER paragraph; the container's clause applies
      *>   to it, so its own comparisons give the same answers.
      *> A leg fails if a comparison falls back to native order (the
      *> YES lines read NO, the NO lines read YES, a CF line follows
      *> "C GEN Q") or if the contained program does not inherit the
      *> sequence.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0601.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. XX.
       OBJECT-COMPUTER. XX
           PROGRAM COLLATING SEQUENCE IS REV.
       SPECIAL-NAMES.
           ALPHABET REV IS "Z" "B" "A" "K" ALSO "Q".
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT C-RPT-FILE ASSIGN TO G85F01R.
       DATA DIVISION.
       FILE SECTION.
       FD  C-RPT-FILE
           LABEL RECORDS ARE STANDARD
           REPORT IS C-RPT.
       WORKING-STORAGE SECTION.
       01  WS-A PIC X VALUE "A".
       01  WS-B PIC X VALUE "B".
       01  WS-Z PIC X VALUE "Z".
       01  WS-K PIC X VALUE "K".
       01  WS-Q PIC X VALUE "Q".
       01  WS-L PIC X.
           88  AC-RANGE VALUE "A" THRU "C".
       01  WS-CTL PIC X.
       REPORT SECTION.
       RD  C-RPT
           CONTROL IS WS-CTL
           PAGE LIMIT IS 20 LINES
           HEADING 1
           FIRST DETAIL 3
           LAST DETAIL 15.
       01  C-DET TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           02  COLUMN 1 PIC X SOURCE WS-CTL.
       01  C-CF TYPE IS CONTROL FOOTING WS-CTL
               LINE NUMBER IS PLUS 1.
           02  COLUMN 1 PIC X(5) VALUE "BREAK".
       PROCEDURE DIVISION.
       DECLARATIVES.
       C-USE SECTION.
           USE BEFORE REPORTING C-CF.
       C-USE-P.
           DISPLAY "C CF".
       END DECLARATIVES.
       P-MAIN SECTION.
       P-MAIN-P.
           IF WS-A > WS-B
               DISPLAY "OUTER A>B YES"
           ELSE
               DISPLAY "OUTER A>B NO".
           IF WS-Z < WS-A
               DISPLAY "OUTER Z<A YES"
           ELSE
               DISPLAY "OUTER Z<A NO".
           IF WS-K = WS-Q
               DISPLAY "OUTER K=Q YES"
           ELSE
               DISPLAY "OUTER K=Q NO".
           MOVE "B" TO WS-L.
           IF AC-RANGE
               DISPLAY "OUTER B IN A-C YES"
           ELSE
               DISPLAY "OUTER B IN A-C NO".
           MOVE "C" TO WS-L.
           IF AC-RANGE
               DISPLAY "OUTER C IN A-C YES"
           ELSE
               DISPLAY "OUTER C IN A-C NO".
           OPEN OUTPUT C-RPT-FILE.
           INITIATE C-RPT.
           MOVE "K" TO WS-CTL.
           DISPLAY "C GEN K".
           GENERATE C-DET.
           MOVE "Q" TO WS-CTL.
           DISPLAY "C GEN Q".
           GENERATE C-DET.
           MOVE "Z" TO WS-CTL.
           DISPLAY "C GEN Z".
           GENERATE C-DET.
           DISPLAY "C TERMINATE".
           TERMINATE C-RPT.
           CLOSE C-RPT-FILE.
           CALL "G85F0601I".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0601I.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-IA PIC X VALUE "A".
       01  WS-IB PIC X VALUE "B".
       01  WS-IZ PIC X VALUE "Z".
       01  WS-M PIC X.
           88  AC-RANGE-I VALUE "A" THRU "C".
       PROCEDURE DIVISION.
       P-INNER.
           IF WS-IA > WS-IB
               DISPLAY "INNER A>B YES"
           ELSE
               DISPLAY "INNER A>B NO".
           IF WS-IZ < WS-IA
               DISPLAY "INNER Z<A YES"
           ELSE
               DISPLAY "INNER Z<A NO".
           MOVE "B" TO WS-M.
           IF AC-RANGE-I
               DISPLAY "INNER B IN A-C YES"
           ELSE
               DISPLAY "INNER B IN A-C NO".
           EXIT PROGRAM.
       END PROGRAM G85F0601I.
       END PROGRAM G85F0601.
