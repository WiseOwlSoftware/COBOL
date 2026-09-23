      *> kb/Work PB244 shape (a) - a strongly-typed group with a class pointer leaf is a legal DISPLAY operand
      *> (ISO 1989:2023 14.9.11.3 SR1 bars only an identifier referencing a data item of class message-tag, object
      *> or pointer; a strongly-typed group's class is its type-name, 8.5.2.1) and a legal MOVE sender (14.9.25.3
      *> SR2 constrains only a strongly-typed RECEIVER). 14.9.11.4 GR1 leaves the device conversion to the
      *> implementor: CONFORMANCE.md A.1 item 56 - the group's storage image, each pointer leaf as its 8 reserved
      *> placeholder positions (spaces; D-SLOT), never the managed reference. Width = 3 + 8 + 2 x (2 + 8) + 2 = 33.
      *>   R1 = [abc        12        34        zz]
      *>   R2 = [34        ]                      one occurrence of GS (a subordinate group of the strong group)
      *>   R3 = {abc        12        34        zz       }   MOVE to PIC X(40): 33 characters + 7 spaces (GR4)
      *>   R4 = <abc        12        34        zz            >   MOVE to a 45-position alphanumeric group
      *>   R5 = 33                                FUNCTION LENGTH agrees with the displayed width
      *> Before the fix R1-R4 compiled and aborted at run time with the Tier-C "pointer/object-class leaf" loud.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB244PTRGRP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GPT IS TYPEDEF STRONG.
          05 GA PIC X(3).
          05 GP USAGE POINTER.
          05 GS OCCURS 2.
             10 GSN PIC 9(2).
             10 GSP USAGE PROGRAM-POINTER.
          05 GZ PIC X(2).
       01 WS-GP TYPE GPT.
       01 WS-X PIC X(4) VALUE "wxyz".
       01 WS-PX USAGE POINTER.
       01 WS-DST PIC X(40).
       01 WS-D2.
          05 WS-D2A PIC X(5).
          05 WS-D2B PIC X(40).
       PROCEDURE DIVISION.
       MAIN.
           MOVE "abc" TO GA OF WS-GP
           MOVE 12 TO GSN OF WS-GP (1)
           MOVE 34 TO GSN OF WS-GP (2)
           MOVE "zz" TO GZ OF WS-GP
           SET WS-PX TO ADDRESS OF WS-X
           SET GP OF WS-GP TO WS-PX
           DISPLAY "[" WS-GP "]"
           DISPLAY "[" GS OF WS-GP (2) "]"
           MOVE WS-GP TO WS-DST
           DISPLAY "{" WS-DST "}"
           MOVE WS-GP TO WS-D2
           DISPLAY "<" WS-D2 ">"
           DISPLAY FUNCTION LENGTH (WS-GP)
           STOP RUN.
