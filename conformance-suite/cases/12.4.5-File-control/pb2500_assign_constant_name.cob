      *>--- conformance-case
      *> id: pb2500_assign_constant_name
      *> rules: [SR-13.10.3-2, GR-13.10.4-1, SR-12.4.5.2-4, SR-12.4.5.2-5,
      *>     SR-8.3.2.1-4, DOC-A.1-71]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2500 - a word that is BOTH a declared constant-name and
      *> an implementor system-name, written where the ASSIGN clause admits
      *> either ({device-name-1 | literal-1}), names the CONSTANT'S file.
      *>   cite.py --check 13.10.3 "constant-name-1 may be used anywhere
      *>     that a format specifies a literal" -> OK 13.10.3 2)
      *>   cite.py --check 13.10.4 "as if literal-1" -> OK 13.10.4 1)
      *>   cite.py --check 8.3.2.1 "A given word may be used as a
      *>     system-name and as a user-defined word" -> OK 8.3.2.1 4)
      *>   cite.py --check 12.4.5.2 "The meaning and rules for the
      *>     allowable specification of device-name-1 and the value of
      *>     literal-1 are defined by the implementor" -> OK 12.4.5.2 5)
      *> The standard does not say which reading a word that is both gets;
      *> the implementor's determination (docs/CONFORMANCE.md section 7,
      *> DOC-A.1-71; CLAUDE.md rule 1: ISO leaves it open, GnuCOBOL
      *> substitutes the constant) is that the program's own declaration
      *> of the word wins, as it does for the LOCALE clause and the
      *> ALPHABET code-name. Before, PB2500KN mapped to the file
      *> pb2500kn.txt (its device-name-1 spelling) and the constant was
      *> never consulted. A word that names no constant is still
      *> device-name-1 (PB2500DV below).
      *> Each leg can fail: WR and RR are the SAME file only if the
      *> constant was substituted (RR names it by literal); SP is the file
      *> the old reading would have written, so it is absent (an
      *> OPTIONAL file that is not present answers status 05 on OPEN
      *> INPUT, 9.1.13.2 4) / 14.9.27.4 13)); CL shows a device class
      *> followed by a constant; the absent files below are the
      *> abnormal paths, each answering the status the rule gives:
      *>   MS - OPEN INPUT on a file that is not described as optional and
      *>     is not present: status 35 (9.1.13.6 5)), checked through its
      *>     FILE STATUS item.
      *>   DC - the same OPEN with NO FILE STATUS item and a USE AFTER
      *>     STANDARD ERROR declarative (14.6.13.1.3 3): the fatal
      *>     EC-I-O condition applies the I-O status rules, and the
      *>     declarative handles it, so the run continues.
      *>     The third arm, NEITHER a FILE STATUS item NOR a USE
      *>     declarative, ends the run unit (14.6.13.1.3 3) and 7), Annex
      *>     A.1 item 103), which a golden cannot show because the harness
      *>     requires exit 0: AssignConstantNameTests (kb/Work PB3438).
      *>   DV - a word naming NO constant is device-name-1, the file
      *>     pb2500dv.txt, which is absent: status 35.
      *>   RR - a sequential READ past the last record: AT END, status
      *>     10 (9.1.13.4 1)).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2500AC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT WR ASSIGN TO PB2500KN
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-WR.
           SELECT RR ASSIGN TO "pb2500a.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-RR.
           SELECT OPTIONAL SP ASSIGN TO "pb2500kn.txt"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-SP.
           SELECT CL ASSIGN TO DISK KC
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-CL.
           SELECT CR ASSIGN TO "pb2500b.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-CR.
           SELECT MS ASSIGN TO KM
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-MS.
           SELECT DC ASSIGN TO KD
               ORGANIZATION IS SEQUENTIAL.
           SELECT DV ASSIGN TO PB2500DV
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-DV.
       DATA DIVISION.
       FILE SECTION.
       FD  WR.
       01  WR-REC PIC X(5).
       FD  RR.
       01  RR-REC PIC X(5).
       FD  SP.
       01  SP-REC PIC X(5).
       FD  CL.
       01  CL-REC PIC X(5).
       FD  CR.
       01  CR-REC PIC X(5).
       FD  MS.
       01  MS-REC PIC X(5).
       FD  DC.
       01  DC-REC PIC X(5).
       FD  DV.
       01  DV-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01  PB2500KN CONSTANT AS "pb2500a.dat".
       01  KC       CONSTANT AS "pb2500b.dat".
       01  KM       CONSTANT AS "pb2500m.dat".
       01  KD       CONSTANT AS "pb2500d.dat".
       01  ST-WR PIC XX VALUE "??".
       01  ST-RR PIC XX VALUE "??".
       01  ST-SP PIC XX VALUE "??".
       01  ST-CL PIC XX VALUE "??".
       01  ST-CR PIC XX VALUE "??".
       01  ST-MS PIC XX VALUE "??".
       01  ST-DV PIC XX VALUE "??".
       PROCEDURE DIVISION.
       DECLARATIVES.
       DC-ERR SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON DC.
       DC-ERR-P.
           DISPLAY "USE-DC".
       END DECLARATIVES.
       MAIN-SECT SECTION.
       MAIN-P.
           OPEN OUTPUT WR.
           DISPLAY "WR-OPEN=" ST-WR.
           MOVE "ALPHA" TO WR-REC.
           WRITE WR-REC.
           MOVE "BETA " TO WR-REC.
           WRITE WR-REC.
           CLOSE WR.
           OPEN INPUT RR.
           DISPLAY "RR-OPEN=" ST-RR.
           READ RR.
           DISPLAY "RR-1=" RR-REC " " ST-RR.
           READ RR.
           DISPLAY "RR-2=" RR-REC " " ST-RR.
           READ RR
               AT END DISPLAY "RR-AT-END " ST-RR
               NOT AT END DISPLAY "RR-3 UNEXPECTED"
           END-READ.
           CLOSE RR.
           OPEN INPUT SP.
           DISPLAY "SP-OPEN=" ST-SP.
           CLOSE SP.
           OPEN OUTPUT CL.
           MOVE "DISKC" TO CL-REC.
           WRITE CL-REC.
           CLOSE CL.
           OPEN INPUT CR.
           READ CR.
           DISPLAY "CR=" CR-REC " " ST-CR.
           CLOSE CR.
           OPEN INPUT MS.
           DISPLAY "MS-OPEN=" ST-MS.
           OPEN INPUT DC.
           DISPLAY "DC-OPEN-RETURNED".
           OPEN INPUT DV.
           DISPLAY "DV-OPEN=" ST-DV.
           STOP RUN.
