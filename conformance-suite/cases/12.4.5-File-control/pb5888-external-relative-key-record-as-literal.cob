      *>--- conformance-case
      *> id: pb5888-external-relative-key-record-as-literal
      *> rules: [GR-12.4.5.3-1]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5888. §2023:13.18.22.4 GR5: "If literal-1 is not
      *> specified, the externalized name of the file connector or record
      *> is the name specified in the file description entry or the
      *> data-name format of the entry-name clause"; with literal-1 it is
      *> literal-1. KA in PB5888E and KB in PB5888F both write AS
      *> "PB5888KEYS", so they describe one external record, and REL-KEY
      *> is positions 1-4 of it in both (under groups GA and GB): the
      *> same corresponding external data item (§2023:14.8.4.2, "shall
      *> refer to the same corresponding storage"), so the program is
      *> accepted. PB5888E writes records 2 and 3; PB5888F moves 2 to
      *> REL-KEY and reads record 2, printing B[SECOND-REC] KEY[0002].
      *> The leg fails if the record's data-name or the group names make
      *> the two keys two items (COBOL1575) or the read differs.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5888E.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT EXT-REL ASSIGN TO "pb5888as.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM RELATIVE KEY IS REL-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  EXT-REL IS EXTERNAL.
       01  EXT-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01  KA IS EXTERNAL AS "PB5888KEYS".
           05  GA.
               10  REL-KEY PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT EXT-REL.
           MOVE 2 TO REL-KEY.
           MOVE "SECOND-REC" TO EXT-REC.
           WRITE EXT-REC INVALID KEY DISPLAY "BADW2".
           MOVE 3 TO REL-KEY.
           MOVE "THIRD-REC" TO EXT-REC.
           WRITE EXT-REC INVALID KEY DISPLAY "BADW3".
           CLOSE EXT-REL.
           CALL "PB5888F".
           STOP RUN.
       END PROGRAM PB5888E.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5888F.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT EXT-REL ASSIGN TO "pb5888as.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM RELATIVE KEY IS REL-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  EXT-REL IS EXTERNAL.
       01  EXT-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01  KB IS EXTERNAL AS "PB5888KEYS".
           05  GB.
               10  REL-KEY PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN INPUT EXT-REL.
           MOVE 2 TO REL-KEY.
           READ EXT-REL INVALID KEY DISPLAY "NOREC".
           DISPLAY "B[" EXT-REC "] KEY[" REL-KEY "]".
           CLOSE EXT-REL.
           GOBACK.
       END PROGRAM PB5888F.
