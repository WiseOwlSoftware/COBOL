      *>--- conformance-case
      *> id: pb5217_release_beside_odo_record
      *> rules: [GR-14.9.32.4-2, GR-14.9.32.4-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5217 - a RELEASE into a sort file one of whose record
      *> descriptions holds an OCCURS DEPENDING ON table (the RELEASE arm
      *> of pb5217_write_beside_odo_record). Every record of SW shares
      *> one area, so S-R's DEPENDING item sizes S-R alone: its current
      *> extent is not the area's, nor S-R4's.
      *>
      *> SW has no RECORD clause, so its records are fixed length 10,
      *> the largest record (docs/CONFORMANCE DOC-A.1-147). A RELEASE of
      *> a record shorter than that releases 10 bytes (14.9.32.4 GR2);
      *> the bytes past record-name-1 are undefined by GR6, and here are
      *> the record area's at those positions (docs/CONFORMANCE.md 3,
      *> D-WRT1). The key SK is at positions 5-8, past S-R's current
      *> extent of 5 when N = 1, so it sorts only if released whole.
      *>
      *> EXPECTED VALUES (derived from the rules, not from a run):
      *>   S-R4 aaaaKEY3, bbbbKEY1, ccccKEY2 released at N = 1, then S-R
      *>   at N = 1 after ddddKEY0zz was moved to it at N = 6: S-R is 5
      *>   bytes (13.18.43.4 GR13 c), the area holds EY0zz past it.
      *>   Returned in ascending SK (14.9.40.4 GR8 a): KEY0 dddd,
      *>   KEY1 bbbb, KEY2 cccc, KEY3 aaaa, then AT END.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5217RL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "pb5217rl-w.dat".
       DATA DIVISION.
       FILE SECTION.
       SD  SW.
       01  S-R.
           05 S-RK PIC X(4).
           05 S-RT PIC X OCCURS 1 TO 6 DEPENDING ON N.
       01  S-R4.
           05 S-A PIC X(4).
           05 SK  PIC X(4).
       WORKING-STORAGE SECTION.
       01  N PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SORT SW ON ASCENDING KEY SK
               INPUT PROCEDURE IS IP
               OUTPUT PROCEDURE IS OP.
           STOP RUN.
       IP.
           MOVE 1 TO N.
           MOVE "aaaaKEY3" TO S-R4.
           RELEASE S-R4.
           MOVE "bbbbKEY1" TO S-R4.
           RELEASE S-R4.
           MOVE "ccccKEY2" TO S-R4.
           RELEASE S-R4.
           MOVE 6 TO N.
           MOVE "ddddKEY0zz" TO S-R.
           MOVE 1 TO N.
           RELEASE S-R.
       OP.
           PERFORM 5 TIMES
               RETURN SW AT END DISPLAY "EOF"
                   NOT AT END DISPLAY SK " " S-A
               END-RETURN
           END-PERFORM.
