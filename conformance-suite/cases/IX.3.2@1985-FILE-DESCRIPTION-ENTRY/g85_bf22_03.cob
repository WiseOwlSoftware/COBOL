      *>--- conformance-case
      *> id: g85_bf22_03
      *> rules: [1985-SR-IX.3.2.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.3.2.3 SR2 - the clauses after file-name-1 in any order
      *> (indexed file). RULE (cite.py OK): "The clauses which follow
      *> file-name-1 may appear in any order." The printed order
      *> (§1985:IX.3.2.2) is BLOCK, RECORD, LABEL, VALUE OF, DATA. XF
      *> writes them REVERSED: DATA / LABEL / RECORD / BLOCK. The program
      *> must translate at 85 and the file must work. DERIVATION: two
      *> 6-character records with keys 01 and 02 written in ascending key
      *> order, read back sequentially, then AT END:
      *> R1=01AAAA R2=02BBBB EOF.
      *> A leg fails if the reversed order is refused or the records
      *> differ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2203.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XF ASSIGN TO "G85BF223.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS XF-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  XF
           DATA RECORD IS XF-REC
           LABEL RECORDS ARE STANDARD
           RECORD CONTAINS 6 CHARACTERS
           BLOCK CONTAINS 1 RECORDS.
       01  XF-REC.
           02  XF-KEY PIC XX.
           02  XF-DATA PIC X(4).
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT XF.
           MOVE "01AAAA" TO XF-REC.
           WRITE XF-REC INVALID KEY DISPLAY "BAD1".
           MOVE "02BBBB" TO XF-REC.
           WRITE XF-REC INVALID KEY DISPLAY "BAD2".
           CLOSE XF.
           OPEN INPUT XF.
           READ XF AT END DISPLAY "EOF1".
           DISPLAY "R1=" XF-REC.
           READ XF AT END DISPLAY "EOF2".
           DISPLAY "R2=" XF-REC.
           READ XF AT END DISPLAY "EOF".
           CLOSE XF.
           STOP RUN.
