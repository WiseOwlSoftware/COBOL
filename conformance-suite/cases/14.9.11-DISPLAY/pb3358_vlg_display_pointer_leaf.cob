*>--- conformance-case
*> id: pb3358_vlg_display_pointer_leaf
*> rules: [GR-14.9.11.4-7, DOC-A.1-57, DOC-A.1-216]
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3358 - DISPLAY and FUNCTION LENGTH of a STRONGLY-TYPED VARIABLE-LENGTH GROUP that holds POINTER
      *> and PROGRAM-POINTER leaves.
      *>
      *> ISO 1989:2023 14.9.11.4 GR7: "If identifier-1 references a variable-length group, the format in which its
      *> contents are displayed is defined by the implementor." DOC-A.1-57 makes it the members' images in
      *> declaration order at their current extents; 13.18.60.3 SR14 admits a pointer as an elementary item
      *> subordinate to a type declaration with the STRONG phrase; and a pointer's 8 storage positions hold the
      *> image DOC-A.1-216 defines - NULL of every category is eight X"00" - the characters a fixed-length group
      *> shows (DOC-A.1-56). 15.50.4 rule 7: FUNCTION LENGTH is the sum of the members' current extents.
      *>
      *> EVERY BYTE THIS GOLDEN PINS IS PRINTABLE (NulByteDriftTests): a pointer's image always holds X"00"
      *> positions (NULL is eight of them, and an address token's high-order positions are zero too), so the
      *> DISPLAYs here show each group at an extent where its pointer table has NO occurrence, while FUNCTION
      *> LENGTH counts each pointer's 8 positions at a non-zero extent. The pointer positions' own characters (the
      *> NULL image inside the displayed group) are pinned byte for byte by
      *> tests/WiseOwl.Cobol.Tests.Conformance/VariableLengthGroupPointerImageTests.cs, which can spell them.
      *> Before the fix every DISPLAY below compiled clean and aborted at run time with the Tier-C
      *> "pointer/object-class leaf" refusal (the gate is the group's declared shape, not its current extent), and
      *> a dynamic-capacity table of pointers (WS-H) then failed the backend compile.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE DETERMINATION - NOT FROM A RUN:
      *>   [hello050]   WS-G: TA "hello" + TN "05" + TC "0" + no TP occurrence. TC is in the group and DISPLAY
      *>                sends it, so the table is used to TC's value (13.18.38.4 GR8 b; 13.18.38.3 SR16 admits
      *>                the minimum zero)
      *>   LEN 0008     5 + 2 + 1
      *>   LEN 0024     TC = 2: 5 + 2 + 1 + 2 x 8
      *>   LEN 0031     WS-F: TA "hello" + TP (8) + two program-pointer occurrences (16) + TN "05": 5 + 8 + 16 + 2
      *>   [ab]         WS-H: HA empty + no HQ occurrence (a dynamic-capacity table with no FROM phrase and no
      *>                VALUE starts at capacity zero, 8.5.1.9.1 item 3) + HB "ab"
      *>   LEN 0002     0 + 0 + 2
      *>   LEN 0018     after SET HQ (1) and HQ (2) TO NULL, the capacity is 2 (8.5.1.9.3): 0 + 2 x 8 + 2
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3358E.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TG IS TYPEDEF STRONG.
          05 TA PIC X DYNAMIC LENGTH LIMIT IS 10.
          05 TN PIC 9(2).
          05 TC PIC 9.
          05 TP USAGE POINTER OCCURS 0 TO 3 DEPENDING ON TC.
       01 WS-G TYPE TG.
       01 TF IS TYPEDEF STRONG.
          05 TA PIC X DYNAMIC LENGTH LIMIT IS 10.
          05 TP USAGE POINTER.
          05 TQ USAGE PROGRAM-POINTER OCCURS 2.
          05 TN PIC 9(2).
       01 WS-F TYPE TF.
       01 TH IS TYPEDEF STRONG.
          05 HA PIC X DYNAMIC LENGTH LIMIT IS 10.
          05 HQ USAGE PROGRAM-POINTER OCCURS DYNAMIC CAPACITY IN HCAP.
          05 HB PIC X(2).
       01 WS-H TYPE TH.
       01 WS-L PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "hello" TO TA OF WS-G
           MOVE 5 TO TN OF WS-G
           MOVE 0 TO TC OF WS-G
           DISPLAY "[" WS-G "]"
           MOVE FUNCTION LENGTH (WS-G) TO WS-L
           DISPLAY "LEN " WS-L
           MOVE 2 TO TC OF WS-G
           MOVE FUNCTION LENGTH (WS-G) TO WS-L
           DISPLAY "LEN " WS-L
           MOVE "hello" TO TA OF WS-F
           MOVE 5 TO TN OF WS-F
           MOVE FUNCTION LENGTH (WS-F) TO WS-L
           DISPLAY "LEN " WS-L
           MOVE "ab" TO HB OF WS-H
           DISPLAY "[" WS-H "]"
           MOVE FUNCTION BYTE-LENGTH (WS-H) TO WS-L
           DISPLAY "LEN " WS-L
           SET HQ OF WS-H (1) TO NULL
           SET HQ OF WS-H (2) TO NULL
           MOVE FUNCTION BYTE-LENGTH (WS-H) TO WS-L
           DISPLAY "LEN " WS-L
           STOP RUN.
       END PROGRAM PB3358E.
