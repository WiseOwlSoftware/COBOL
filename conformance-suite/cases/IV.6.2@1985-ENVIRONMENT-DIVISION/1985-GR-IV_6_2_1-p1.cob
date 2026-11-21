      *>--- conformance-case
      *> id: 1985-GR-IV_6_2_1-p1
      *> rules: [1985-GR-IV.6.2.1-p1, 1985-GR-IV.6.2.2-p2, 1985-GR-IV.6.2.2-p3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.6.2.1 p1; §1985:IV.6.2.2 p2; §1985:IV.6.2.2 p3
      *> (ENVIRONMENT DIVISION). An output test: the program must
      *> translate and run at 85 and DISPLAY exactly the lines its .out
      *> holds, the outcome the rule requires. Placed from written and
      *> run by the PB5453 reader of lineage-reads-1985/s-06.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * 1985-GR-IV.6.2.1-p1 / -IV.6.2.2-p2 / -IV.6.2.2-p3: the
      * Environment Division with both sections and all five
      * paragraphs; SPECIAL-NAMES supplies the currency sign, decimal
      * point, a symbolic-character, an implementor-name mnemonic, an
      * alphabet-name and a class-name; FILE-CONTROL associates a file
      * and I-O-CONTROL names a control technique (SAME RECORD AREA:
      * F2-REC shares F1's record area, so it shows the record read).
      * Expect: AMT=1.234,50 / CUR=$ / SYM=YES / CLS=YES / ALF=YES /
      *         FILE=REC1 / ENV OK
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ENVDIV.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. WISEOWL.
       OBJECT-COMPUTER. WISEOWL
           PROGRAM COLLATING SEQUENCE IS STD.
       SPECIAL-NAMES.
           SYSOUT IS OUT-DEV
           ALPHABET STD IS STANDARD-1
           SYMBOLIC CHARACTERS STAR IS 43
           CLASS VOWEL IS "A" "E" "I" "O" "U"
           CURRENCY SIGN IS "$"
           DECIMAL-POINT IS COMMA.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "S06ENV.DAT"
               ORGANIZATION IS SEQUENTIAL.
           SELECT F2 ASSIGN TO "S06ENV2.DAT"
               ORGANIZATION IS SEQUENTIAL.
       I-O-CONTROL.
           SAME RECORD AREA FOR F1 F2.
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  F1-REC  PIC X(4).
       FD  F2.
       01  F2-REC  PIC X(4).
       WORKING-STORAGE SECTION.
       01  AMT   PIC 9.999,99.
       01  CUR   PIC $9.
       01  S     PIC X VALUE "*".
       01  V     PIC X VALUE "E".
       PROCEDURE DIVISION.
       P0.
           MOVE 1234,50 TO AMT.
           DISPLAY "AMT=" AMT UPON OUT-DEV.
           MOVE 0 TO CUR.
           DISPLAY "CUR=" CUR(1:1).
           IF S = STAR DISPLAY "SYM=YES" ELSE DISPLAY "SYM=NO".
           IF V IS VOWEL DISPLAY "CLS=YES" ELSE DISPLAY "CLS=NO".
           IF V < "F" DISPLAY "ALF=YES" ELSE DISPLAY "ALF=NO".
           OPEN OUTPUT F1.
           MOVE "REC1" TO F1-REC.
           WRITE F1-REC.
           CLOSE F1.
           OPEN INPUT F1.
           READ F1 AT END DISPLAY "EOF".
           DISPLAY "FILE=" F2-REC.
           CLOSE F1.
           DISPLAY "ENV OK".
           STOP RUN.
