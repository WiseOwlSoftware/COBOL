*>--- conformance-case
*> id: pb3505_display_numeric_views
*> rules: [GR-14.9.11.4-1, DOC-A.1-56]
*> accept: [85, 2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.11.4 GR1 (Annex A.1 item 56; kb/Work PB3505, owner decision PB3467) under the default
      *> `--numeric-display decimal`: ONE storage, ONE form. A numeric item shows the same text whether it
      *> is read through its own description, through a REDEFINES description of the same storage
      *> (§13.18.44.4 GR2: either data-name references the storage area), as a table element or after
      *> a MOVE into the character view of its group; the alphanumeric view of the same storage is
      *> NOT a numeric item and shows the stored characters.
      *>
      *> EXPECTED VALUES from the declarations, not observed: -12.5 in S9(5)V99 is -00012.50 whether
      *> it is stored zoned (BASE, BR) or packed (PK, PKR), and in S9(4)V9 COMP -0012.5; the
      *> alphanumeric view BASE-S shows the seven stored characters: -12.5 is the digits 0001250
      *> with the sign over-punched on the last digit, a 0 made negative, which the default sign
      *> encoding writes "}" (DOC-A.1-177), so 000125}; a table element is
      *> 1.25 in 9V99 and an element of S9(3) holding -7 is -007; the group REC shows its stored
      *> characters 00150 and -005 (SIGN LEADING SEPARATE); after MOVE "00250-007" TO the character
      *> view of REC, RA is 002.50 and RB is -007.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3505V.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BASE PIC S9(5)V99 VALUE -12.5.
       01 BASE-R REDEFINES BASE.
          05 BR PIC S9(5)V99.
       01 BASE-S REDEFINES BASE PIC X(7).
       01 PK PIC S9(5)V99 COMP-3 VALUE -12.5.
       01 PK-R REDEFINES PK.
          05 PKR PIC S9(5)V99 COMP-3.
       01 TBL.
          05 EL PIC 9V99 OCCURS 3 VALUE 1.25.
       01 TBL2.
          05 EL2 PIC S9(3) OCCURS 2 VALUE -7.
       01 I PIC 9 VALUE 2.
       01 BN PIC S9(4)V9 COMP VALUE -12.5.
       01 BN-R REDEFINES BN.
          05 BNR PIC S9(4)V9 COMP.
       01 REC.
          05 RA PIC 9(3)V99 VALUE 1.5.
          05 RB PIC S9(3) SIGN LEADING SEPARATE VALUE -5.
       01 RECX REDEFINES REC PIC X(9).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "BASE=" BASE "|" BR "|" BASE-S "|"
           DISPLAY "PK=" PK "|" PKR "|"
           DISPLAY "EL=" EL (1) "|" EL (I) "|" EL (3) "|"
           DISPLAY "EL2=" EL2 (I) "|"
           DISPLAY "BN=" BN "|" BNR "|"
           DISPLAY "REC=" RA " " RB "|" REC "|"
           MOVE "00250-007" TO RECX
           DISPLAY "RECX=" RA " " RB "|"
           ADD 1 TO EL (1)
           DISPLAY "EL1=" EL (1) "|"
           STOP RUN.
