*>--- conformance-case
*> id: pb3441_vlg_display_national_members_cell
*> rules: [GR-14.9.11.4-7, DOC-A.1-57, DOC-A.1-63]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3441 - the CELL-BACKED twin of pb3441_vlg_display_national_members: an EXTERNAL record holding a
      *> DYNAMIC-LENGTH NATIONAL item and a DYNAMIC-CAPACITY TABLE of national elements composes the same image a declared
      *> group does - the national member is the two bytes a position it occupies (DOC-A.1-57, DOC-A.1-63; 13.18.60.4 GR8,
      *> D-N1), so DISPLAY, LENGTH and BYTE-LENGTH agree (15.50.4 rule 7) - and moves, element by element, to and from a
      *> declared group of the same shape and to another cell (14.9.25.4 GR9; 14.6.9.2).
      *>
      *> The SHORTER sender's MOVE reaches the receiver's table through 14.9.25.4 GR9b: a dynamic-capacity table that lies
      *> beyond the sender's last character is in the excess part, and 14.6.9.4 says of it "the current capacity of the
      *> dynamic table is unaffected, and each element of the dynamic table is space-filled". The elements of a NATIONAL
      *> table are filled with the national space (the UTF-16BE pair 00 20, ISO 13.18.60.4 GR8 and D-N1), which is what
      *> comparison with SPACES asks - the byte 0x20 twice would be U+2020, not a space.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE DETERMINATION - NOT FROM A RUN (the national literals are hexadecimal so each
      *> position's two bytes are printable ASCII: NX"4142" is ONE position, "AB"; every table element is N(2), 4 bytes):
      *>   X1  XF "a" + XD "ABCD" + XT(1) "EFGH" + XT(2) "IJKL"                                  1 + 4 + 8 = 13
      *>   X2  XD shortened to one position "MN"                                                 1 + 2 + 8 = 11
      *>   Y   MOVE PB3441XE TO PB3441YE: the same members at the same extents                   11
      *>   D   MOVE PB3441YE TO D (a declared group of the same shape)                           11, its table 2 elements
      *>   Z   MOVE SG TO PB3441YE (SG = "b" + S2 "QR", no table): the table keeps its capacity 2 and each element is
      *>       the national space; 1 + 2 + 2 x 4 = 11
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3441B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PB3441XE EXTERNAL.
          05 XF PIC X.
          05 XD PIC N DYNAMIC LENGTH LIMIT IS 5.
          05 XT PIC N(2) OCCURS DYNAMIC CAPACITY IN XC FROM 2.
       01 PB3441YE EXTERNAL.
          05 YF PIC X.
          05 YD PIC N DYNAMIC LENGTH LIMIT IS 5.
          05 YT PIC N(2) OCCURS DYNAMIC CAPACITY IN YC FROM 2.
       01 D.
          05 DF PIC X.
          05 DD PIC N DYNAMIC LENGTH LIMIT IS 5.
          05 DT PIC N(2) OCCURS DYNAMIC CAPACITY IN DC FROM 2.
       01 SG.
          05 S1 PIC X.
          05 S2 PIC N DYNAMIC LENGTH LIMIT IS 5.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "a" TO XF
           MOVE NX"41424344" TO XD
           MOVE NX"45464748" TO XT (1)
           MOVE NX"494A4B4C" TO XT (2)
           DISPLAY "X1=[" PB3441XE "] " FUNCTION LENGTH (PB3441XE) " " FUNCTION BYTE-LENGTH (PB3441XE)
           MOVE NX"4D4E" TO XD
           DISPLAY "X2=[" PB3441XE "] " FUNCTION LENGTH (PB3441XE) " " FUNCTION BYTE-LENGTH (PB3441XE)
           MOVE PB3441XE TO PB3441YE
           DISPLAY "Y=[" PB3441YE "] " FUNCTION LENGTH (PB3441YE) " " FUNCTION BYTE-LENGTH (PB3441YE)
           IF YD = NX"4D4E" DISPLAY "YD-OK" ELSE DISPLAY "YD-BAD" END-IF
           IF YT (2) = NX"494A4B4C" DISPLAY "YT2-OK" ELSE DISPLAY "YT2-BAD" END-IF
           MOVE PB3441YE TO D
           DISPLAY "D=[" D "] " FUNCTION LENGTH (D) " " FUNCTION BYTE-LENGTH (D)
           IF DT (1) = NX"45464748" DISPLAY "DT1-OK" ELSE DISPLAY "DT1-BAD" END-IF
           MOVE "b" TO S1
           MOVE NX"5152" TO S2
           MOVE SG TO PB3441YE
           DISPLAY "Z " FUNCTION LENGTH (PB3441YE) " " FUNCTION BYTE-LENGTH (PB3441YE)
           IF YD = NX"5152" DISPLAY "YD-OK" ELSE DISPLAY "YD-BAD" END-IF
           IF YT (1) = SPACES AND YT (2) = SPACES DISPLAY "YT-SPACES" ELSE DISPLAY "YT-NOT-SPACES" END-IF
           STOP RUN.
       END PROGRAM PB3441B.
