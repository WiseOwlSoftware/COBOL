      *>--- conformance-case
      *> id: g85_bf22_01
      *> rules: [1985-SR-VII.3.2.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.3.2.3 SR2 - the clauses after file-name-1 in any order
      *> (sequential file). RULE (cite.py OK): "The clauses which follow
      *> file-name-1 may appear in any order." The printed order
      *> (§1985:VII.3.2.2) is BLOCK, RECORD, LABEL, VALUE OF, DATA, LINAGE,
      *> CODE-SET. Both FDs write their clauses in the REVERSE order:
      *>  SF  CODE-SET / DATA / LABEL / RECORD / BLOCK
      *>  PRT-F  LINAGE / DATA / LABEL / RECORD
      *> The program must translate at 85 and every clause must still
      *> take effect. DERIVATION:
      *>  SF: two 4-character records ABCD, WXYZ written, read back in
      *>   order, then AT END: R1=ABCD R2=WXYZ EOF. CODE-SET STANDARD-1
      *>   is a round trip, so the content is unchanged.
      *>  PRT-F: §1985:VII.3.7.4 GR9d (cite.py OK) "The value of
      *>   LINAGE-COUNTER is automatically set to one at the time an
      *>   OPEN statement with the OUTPUT phrase is executed"; GR9c 3)
      *>   (cite.py OK) "When the ADVANCING phrase of the WRITE statement
      *>   is not specified, the LINAGE-COUNTER is incremented by the
      *>   value one." OPEN -> 1, two WRITEs -> 3 (below the footing
      *>   at 8, so no page change): LC=0003.
      *> A leg fails if a reversed clause is refused, ignored (no
      *> LINAGE-COUNTER, wrong record size) or the records differ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2201.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET BF-STD IS STANDARD-1.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "G85BF221.DAT"
               ORGANIZATION IS SEQUENTIAL.
           SELECT PRT-F ASSIGN TO "G85BF221.TXT"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  SF
           CODE-SET IS BF-STD
           DATA RECORD IS SF-REC
           LABEL RECORDS ARE STANDARD
           RECORD CONTAINS 4 CHARACTERS
           BLOCK CONTAINS 2 RECORDS.
       01  SF-REC PIC X(4).
       FD  PRT-F
           LINAGE IS 10 LINES WITH FOOTING AT 8
               LINES AT TOP 1 LINES AT BOTTOM 1
           DATA RECORD IS PRT-F-REC
           LABEL RECORDS ARE OMITTED
           RECORD CONTAINS 6 CHARACTERS.
       01  PRT-F-REC PIC X(6).
       WORKING-STORAGE SECTION.
       01  WS-LC PIC 9(4).
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT SF.
           MOVE "ABCD" TO SF-REC.
           WRITE SF-REC.
           MOVE "WXYZ" TO SF-REC.
           WRITE SF-REC.
           CLOSE SF.
           OPEN INPUT SF.
           READ SF AT END DISPLAY "EOF1".
           DISPLAY "R1=" SF-REC.
           READ SF AT END DISPLAY "EOF2".
           DISPLAY "R2=" SF-REC.
           READ SF AT END DISPLAY "EOF".
           CLOSE SF.
           OPEN OUTPUT PRT-F.
           MOVE "LINE-1" TO PRT-F-REC.
           WRITE PRT-F-REC.
           MOVE "LINE-2" TO PRT-F-REC.
           WRITE PRT-F-REC.
           MOVE LINAGE-COUNTER TO WS-LC.
           DISPLAY "LC=" WS-LC.
           CLOSE PRT-F.
           STOP RUN.
