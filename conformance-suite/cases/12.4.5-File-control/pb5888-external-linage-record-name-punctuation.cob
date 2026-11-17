      *>--- conformance-case
      *> id: pb5888-external-linage-record-name-punctuation
      *> rules: [GR-12.4.5.3-1]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5888, review R2-1. Both programs describe the external
      *> record externalized AS "A;!|1:B" (§2023:13.18.22.4 GR5: literal-1
      *> is the externalized name, any character-string) and name its L
      *> in the LINAGE clause of one EXTERNAL file: the same external data
      *> item, "the same corresponding storage in each runtime element"
      *> (§2023:14.8.4.2), so the CALL raises no
      *> EC-EXTERNAL-DATA-MISMATCH and prints IN-B then CALL-OK. The
      *> name holds ';' and '!' (the old run-time reader split the
      *> reference on ';' and took a '!' token for a non-external item)
      *> and '|' and ':' (the separators of the identity key, which its
      *> length prefixes guard). The leg fails on CALL-EXCEPTION.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5888L.
       >>TURN EC-EXTERNAL-DATA-MISMATCH CHECKING ON
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRF ASSIGN TO "pb5888l.txt".
       DATA DIVISION.
       FILE SECTION.
       FD PRF IS EXTERNAL LINAGE IS L LINES.
       01 PRF-REC PIC X(8).
       WORKING-STORAGE SECTION.
       01 LREC IS EXTERNAL AS "A;!|1:B".
          05 L PIC 9(4).
       PROCEDURE DIVISION.
       P1.
           MOVE 20 TO L.
           CALL "PB5888M"
               ON EXCEPTION DISPLAY "CALL-EXCEPTION"
               NOT ON EXCEPTION DISPLAY "CALL-OK"
           END-CALL.
           GOBACK.
       END PROGRAM PB5888L.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5888M.
       >>TURN EC-EXTERNAL-DATA-MISMATCH CHECKING ON
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRF ASSIGN TO "pb5888l.txt".
       DATA DIVISION.
       FILE SECTION.
       FD PRF IS EXTERNAL LINAGE IS L LINES.
       01 PRF-REC PIC X(8).
       WORKING-STORAGE SECTION.
       01 LREC IS EXTERNAL AS "A;!|1:B".
          05 L PIC 9(4).
       PROCEDURE DIVISION.
       P1.
           DISPLAY "IN-B".
           GOBACK.
       END PROGRAM PB5888M.
