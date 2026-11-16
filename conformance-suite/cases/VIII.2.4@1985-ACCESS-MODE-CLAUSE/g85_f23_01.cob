      *>--- conformance-case
      *> id: g85_f23_01
      *> rules: [1985-SR-VIII.2.4.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.2.4.3 SR1 - the RELATIVE KEY phrase's data-name-1 is
      *> qualified (IN in the SELECT, OF in the procedure) and names one
      *> of two same-named items. §1985:VIII.2.4.4 GR7 makes that item
      *> the relative key; §1985:VIII.4.9.4 GR11b and §1985:VIII.4.5.4
      *> GR15 then place and fetch the record by the item of KEY-GROUP-A
      *> alone. KEY-GROUP-B's item holds 7 at the WRITE, 3 at READ R3 and
      *> 7 at READ R7. Fails if the qualified name is refused, or if
      *> KEY-GROUP-B's item is used: W3 would land at 7, R3 would be
      *> invalid (23) and R7 would read THREE with status 00.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2301.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT REL-FILE ASSIGN TO F2301REL
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS REL-KEY IN KEY-GROUP-A
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  REL-FILE
           LABEL RECORDS ARE STANDARD.
       01  REL-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01  KEY-GROUP-A.
           05  REL-KEY PIC 9(4).
       01  KEY-GROUP-B.
           05  REL-KEY PIC 9(4).
       01  FS PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT REL-FILE.
           MOVE 3 TO REL-KEY OF KEY-GROUP-A.
           MOVE 7 TO REL-KEY OF KEY-GROUP-B.
           MOVE "THREE" TO REL-REC.
           WRITE REL-REC INVALID KEY DISPLAY "W3 INVALID".
           DISPLAY "W3 " FS.
           CLOSE REL-FILE.
           OPEN INPUT REL-FILE.
           MOVE 3 TO REL-KEY OF KEY-GROUP-A.
           MOVE 3 TO REL-KEY OF KEY-GROUP-B.
           MOVE SPACES TO REL-REC.
           READ REL-FILE INVALID KEY DISPLAY "R3 INVALID".
           DISPLAY "R3 " FS " " REL-REC.
           MOVE 7 TO REL-KEY OF KEY-GROUP-A.
           MOVE 7 TO REL-KEY OF KEY-GROUP-B.
           READ REL-FILE INVALID KEY DISPLAY "R7 INVALID".
           DISPLAY "R7 " FS.
           CLOSE REL-FILE.
           STOP RUN.
