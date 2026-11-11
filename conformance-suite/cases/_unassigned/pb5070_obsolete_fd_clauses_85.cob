      *>--- conformance-case
      *> id: pb5070_obsolete_fd_clauses_85
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5070 - the three obsolete COBOL-85 file description clauses in their general formats: LABEL
      *> {RECORD IS | RECORDS ARE} {STANDARD | OMITTED} (1985:VII.3.6.2), VALUE OF {implementor-name-1 IS {data-name-1 |
      *> literal-1}} ... (1985:VII.3.9.2) with data-name-1 a Working-Storage item (1985:VII.3.9.3 SR2), and DATA RECORD IS
      *> naming the entry's 01 record (1985:VII.3.5.3 SR1). Deleted from COBOL-2002; labels are not processed, so the
      *> clauses change no I-O.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5070FD.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "PB5070FD.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD  F1
           LABEL RECORD IS OMITTED
           VALUE OF FILE-ID IS WS-NAME VOL-ID "PB5070"
           DATA RECORD IS R1.
       01  R1.
           02  OUT-FLD PIC X(10).
       WORKING-STORAGE SECTION.
       01  WS-NAME PIC X(8) VALUE "PB5070".
       PROCEDURE DIVISION.
       P1.
           DISPLAY "OK".
           STOP RUN.
