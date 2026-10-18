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
      *> The pointers here are NULL (their initial value, 13.18.63.4 GR4: class pointer items are "initialized to null"), the one image the
      *> determination fixes for every run; a non-NULL pointer's token is the implementor's.
      *> Before the fix the DISPLAY compiled clean and aborted at run time with the Tier-C
      *> "pointer/object-class leaf" refusal.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE DETERMINATION - NOT FROM A RUN (Z8 = eight X"00"):
      *>   [hello Z8 Z8 Z8 05]   A "hello" + TP (Z8) + two program-pointer occurrences (Z8 Z8) + TN "05"
      *>   LEN 0031              5 + 8 + 16 + 2
      *>   [Z8 ab]               a variable-length group whose dynamic item is EMPTY then "ab": the pointer first
      *>   LEN 0010              0 + 8 + 2
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3358E.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TG IS TYPEDEF STRONG.
          05 TA PIC X DYNAMIC LENGTH LIMIT IS 10.
          05 TP USAGE POINTER.
          05 TQ USAGE PROGRAM-POINTER OCCURS 2.
          05 TN PIC 9(2).
       01 WS-G TYPE TG.
       01 TH IS TYPEDEF STRONG.
          05 HA PIC X DYNAMIC LENGTH LIMIT IS 10.
          05 HP USAGE POINTER.
          05 HB PIC X(2).
       01 WS-H TYPE TH.
       01 WS-L PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "hello" TO TA OF WS-G
           MOVE 5 TO TN OF WS-G
           DISPLAY "[" WS-G "]"
           MOVE FUNCTION LENGTH (WS-G) TO WS-L
           DISPLAY "LEN " WS-L
           MOVE "ab" TO HB OF WS-H
           DISPLAY "[" WS-H "]"
           MOVE FUNCTION BYTE-LENGTH (WS-H) TO WS-L
           DISPLAY "LEN " WS-L
           STOP RUN.
       END PROGRAM PB3358E.
