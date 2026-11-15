      *>--- conformance-case
      *> id: pb5628_release_in_called_contained_program_85
      *> rules: [1985-GR-XI.4.4.4-7, 1985-SR-XI.4.2.3-2, 1985-SR-X.4.6.3-1,
      *>    1985-GR-X.4.6.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5628 - at COBOL-85 a RELEASE in a contained program that a CALL in the input procedure
      *> activates is in the input procedure's range, and a contained program may name a GLOBAL record of a
      *> file description entry that has no GLOBAL clause. THE RULES (cite.py --check, each OK):
      *>   §1985:XI.4.4.4 GR7 "The range includes all statements that are executed as the result of a
      *>     transfer of control by CALL, EXIT, GO TO, and PERFORM statements in the range of the input
      *>     procedure", so the RELEASE of P2N, which IN-P CALLs, is in the range §1985:XI.4.2.3 SR2 asks.
      *>   §1985:X.4.6.3 SR1 "The GLOBAL clause may be specified only in data description entries whose
      *>     level-number is 01 in the File Section or the Working-Storage Section, file description
      *>     entries, or report description entries": REC-A, a record of the SD, is GLOBAL (an SD takes no
      *>     GLOBAL clause), and §1985:X.4.6.4 GR2 "A statement in a program contained directly or indirectly
      *>     within a program which describes a global name may reference that name without describing it
      *>     again". 1985 has no rule tying a contained program's input-output statement to a GLOBAL FILE
      *>     (from COBOL-2002, §2002:13.16.25.2 SR3), so P2W's WRITE of OUT-R, the GLOBAL record of the
      *>     non-GLOBAL FD OUT-F, writes OUT-F.
      *> Expected: the three released records sorted (AAA, GGG, ZZZ), each an output line, and the record
      *> P2W wrote. Fails if either contained program's statement is refused or releases or writes nowhere.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5628M.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SORT-A ASSIGN TO "PB5628SA".
           SELECT SRT-OUT ASSIGN TO "PB5628SO".
           SELECT OUT-F ASSIGN TO "PB5628OF".
       DATA DIVISION.
       FILE SECTION.
       SD  SORT-A.
       01  REC-A IS GLOBAL.
           05  KEY-A PIC X(3).
       FD  SRT-OUT.
       01  SRT-R PIC X(3).
       FD  OUT-F.
       01  OUT-R IS GLOBAL PIC X(3).
       PROCEDURE DIVISION.
       M SECTION.
       M1. SORT SORT-A ON ASCENDING KEY KEY-A INPUT PROCEDURE IS IN-P
               GIVING SRT-OUT.
           OPEN INPUT SRT-OUT.
       M2. READ SRT-OUT AT END GO TO M3.
           DISPLAY "S " SRT-R. GO TO M2.
       M3. CLOSE SRT-OUT.
           OPEN OUTPUT OUT-F.
           CALL "P2W".
           CLOSE OUT-F.
           OPEN INPUT OUT-F.
           READ OUT-F AT END DISPLAY "NO RECORD".
           DISPLAY "W " OUT-R.
           CLOSE OUT-F.
           STOP RUN.
       IN-P SECTION.
       I1. MOVE "ZZZ" TO REC-A. RELEASE REC-A.
           CALL "P2N".
           MOVE "AAA" TO REC-A. RELEASE REC-A.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2N.
       PROCEDURE DIVISION.
       N1. MOVE "GGG" TO REC-A. RELEASE REC-A.
           EXIT PROGRAM.
       END PROGRAM P2N.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2W.
       PROCEDURE DIVISION.
       W1. MOVE "WWW" TO OUT-R. WRITE OUT-R.
           EXIT PROGRAM.
       END PROGRAM P2W.
       END PROGRAM PB5628M.
