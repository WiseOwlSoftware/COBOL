      *>--- conformance-case
      *> id: g85_bf04_01
      *> rules: [1985-GR-IV.4.3.7-1, 1985-GR-IV.4.3.7-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.7 GR1 - synchronization by the SYNCHRONIZED clause
      *> §1985:IV.4.3.7 GR2 - synchronization without the clause
      * R1, R2 and R3 carry SYNC, SYNC LEFT and SYNCHRONIZED RIGHT
      * (GR1): the program must be accepted at 85 and each synchronized
      * item must hold its own value while its neighbours keep theirs
      * ("no other data item occupies any of the character positions",
      * VI.5.13.4 GR1), and "the original size of the item, as shown in
      * the PICTURE clause" is used for truncation (VI.5.13.4 GR5):
      * 12345 into S9(4) gives +2345. R4 and R5 carry no clause (GR2):
      * the programmer organizes the data (binary item first in R4);
      * both store and return +4321. The LENGTH lines are what GR2's
      * last paragraph leaves to the implementor ("Each implementor who
      * provides for special types of alignment will specify the precise
      * interpretations"); the returned value "includes implicit FILLER
      * characters, if any" (A.2.19.4 RV3). WiseOwl COBOL documents NO
      * alignment and NO implicit FILLER (docs/conformance DOC-A.1-195)
      * and a 2-byte S9(4) COMP item (DOC-A.1-205, DOC-A.1-208), so
      * ITEM=2 and every record is 1 + 2 + 1 = 4. A layout that padded
      * the SYNC item to a 2-byte boundary answers 5 or 6 on LEN1-LEN3;
      * a truncation by storage width instead of PICTURE answers +12345
      * on R1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0401.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R1.
           05 R1A PIC X VALUE "A".
           05 R1B PIC S9(4) COMP SYNC.
           05 R1C PIC X VALUE "C".
       01 R2.
           05 R2A PIC X VALUE "A".
           05 R2B PIC S9(4) COMP SYNC LEFT.
           05 R2C PIC X VALUE "C".
       01 R3.
           05 R3A PIC X VALUE "A".
           05 R3B PIC S9(4) COMP SYNCHRONIZED RIGHT.
           05 R3C PIC X VALUE "C".
      * R4: no SYNCHRONIZED clause, the binary item placed first by
      * the programmer (way (2) of IV.4.3.7).
       01 R4.
           05 R4B PIC S9(4) COMP.
           05 R4A PIC X VALUE "A".
           05 R4C PIC X VALUE "C".
      * R5: no SYNCHRONIZED clause and no organizing.
       01 R5.
           05 R5A PIC X VALUE "A".
           05 R5B PIC S9(4) COMP.
           05 R5C PIC X VALUE "C".
       01 W-NUM PIC S9(4) SIGN LEADING SEPARATE.
       01 W-LEN PIC 9(4).
       PROCEDURE DIVISION.
       P1.
           MOVE 12345 TO R1B.
           MOVE R1B TO W-NUM.
           DISPLAY "R1=" R1A " " W-NUM " " R1C.
           MOVE -17 TO R2B.
           MOVE R2B TO W-NUM.
           DISPLAY "R2=" R2A " " W-NUM " " R2C.
           MOVE 999 TO R3B.
           ADD 1 TO R3B.
           MOVE R3B TO W-NUM.
           DISPLAY "R3=" R3A " " W-NUM " " R3C.
           MOVE 4321 TO R4B R5B.
           MOVE R4B TO W-NUM.
           DISPLAY "R4=" R4A " " W-NUM " " R4C.
           MOVE R5B TO W-NUM.
           DISPLAY "R5=" R5A " " W-NUM " " R5C.
           COMPUTE W-LEN = FUNCTION LENGTH (R1B).
           DISPLAY "ITEM=" W-LEN.
           COMPUTE W-LEN = FUNCTION LENGTH (R1).
           DISPLAY "LEN1=" W-LEN.
           COMPUTE W-LEN = FUNCTION LENGTH (R2).
           DISPLAY "LEN2=" W-LEN.
           COMPUTE W-LEN = FUNCTION LENGTH (R3).
           DISPLAY "LEN3=" W-LEN.
           COMPUTE W-LEN = FUNCTION LENGTH (R4).
           DISPLAY "LEN4=" W-LEN.
           COMPUTE W-LEN = FUNCTION LENGTH (R5).
           DISPLAY "LEN5=" W-LEN.
           STOP RUN.
