*>--- conformance-case
*> id: pb5605-fd-second-record-odo-bound-2023
*> rules: [GR-13.18.38.4-7]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5605 - EC-BOUND-ODO AT A REFERENCE THROUGH THE SECOND RECORD OF AN FD.
      *> RULE (2023): "If the value of the data item does not fall within the specified bounds, the EC-BOUND-ODO exception condition is set to exist"
      *>   cite.py --check "§2023:13.18.38.4 GR7" "If the value of the data item does not fall within the specified bounds, the EC-BOUND-ODO"
      *> The second level-01 entry of an FD implicitly redefines the first's area (§2023:13.18.33.4 GR3, --check validated:
      *> "Multiple level 1 entries subordinate to a FD or SD entry represent implicit redefinitions of the same area"),
      *> so a reference to its table element is made through a window onto the shared storage; GR7's bound test of
      *> data-name-1 is owed there as at every other reference. >>TURN is COBOL-2002 (§2023:7.3.25); the refusal at 85
      *> is pinned by pb5605-fd-second-record-odo-bound-below-2002 (COBOL0900).
      *>
      *> WHY EACH LEG CAN FAIL (before PB5605 the window carried no bound test, so legs 1 and 2 raised nothing):
      *>   1  N = 7 (> integer-2 5): DISPLAY UX(1), UX SUBORDINATE to the subject U      -> RAISED EC-BOUND-ODO
      *>   2  N = 1 (< integer-1 2): MOVE "Z" TO U(1), the SUBJECT itself, a store       -> RAISED EC-BOUND-ODO
      *>   3  N = 3 (in bounds): DISPLAY UX(2) -> B, no condition: a bound test, not a raise on every reference.
      *>   4  CNT = 02.
       >>TURN EC-BOUND-ODO CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5605F23.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb5605.dat".
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R1 PIC X(11).
       01 R2.
          05 H PIC X.
          05 U OCCURS 2 TO 5 DEPENDING ON N.
             10 UX PIC X.
             10 UE PIC 9.
       WORKING-STORAGE SECTION.
       01 N PIC 99 VALUE 5.
       01 CNT PIC 99 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-ODO.
       D1P.
           ADD 1 TO CNT
           DISPLAY "RAISED " FUNCTION EXCEPTION-STATUS.
           RESUME NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           MOVE "HA1B2C3D4E5" TO R2
           MOVE 7 TO N
           DISPLAY "1 " UX(1)
           MOVE 1 TO N
           MOVE "Z" TO U(1)
           MOVE 3 TO N
           DISPLAY "3 " UX(2)
           DISPLAY "4 CNT=" CNT
           STOP RUN.
