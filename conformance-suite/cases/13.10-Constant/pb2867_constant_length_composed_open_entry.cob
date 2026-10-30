*>--- conformance-case
*> id: pb2867_constant_length_composed_open_entry
*> rules: [SR-13.10.3-4]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2867 - a constant's LENGTH OF / BYTE-LENGTH OF operand may be a subordinate that the TYPE or
      *> SAME AS clause of the very entry whose OCCURS clause demands the constant supplies (ISO 13.10, 2002).
      *> 13.18.57.4 GR1: "The effect of the TYPE clause is as though the data description identified by
      *> type-name-1 had been coded in place of the TYPE clause" (13.18.49.4 GR1 says the same of SAME AS), so
      *> 05 V TYPE T OCCURS K is 05 V OCCURS K with T's subordinates under it, and 13.10.3 SR4 ("The length of
      *> data-name-1 or data-name-2 shall not be dependent, directly or indirectly, upon the value of
      *> constant-name-1") is not violated: an element's subordinate does not depend on the occurrence count.
      *> Each operand is a table element, subscripted by literals (8.4.2.3.3 SR5, 13.10.3 SR3). Expected values
      *> (13.10.4 GR6 the LENGTH value, GR5 the BYTE-LENGTH value):
      *>   K  = 3   LENGTH OF M OF V (1), T's M PIC X(3) - so V OCCURS 3 TIMES
      *>   KB = 2   BYTE-LENGTH OF N OF W (1), T's N PIC 9(4) COMP-5 - so W OCCURS 2 TIMES
      *>   KU = 5   LENGTH OF Q OF S (1), P's Q PIC X(5) under S SAME AS P - so S OCCURS 5 TIMES
      *>   G = V 3 x (3 + 2) + W 2 x (3 + 2) + S 5 x 5 = 15 + 10 + 25 = 50 bytes
      *> The negative half: pb2867-constant-length-composed-cycle (SR4's circular shape through a TYPE clause).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2867CMP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T TYPEDEF.
          05 M PIC X(3).
          05 N PIC 9(4) COMP-5.
       01 P.
          05 Q PIC X(5).
       01 G.
          05 V TYPE T OCCURS K.
          05 W TYPE T OCCURS KB.
          05 S SAME AS P OCCURS KU.
       01 K CONSTANT AS LENGTH OF M OF V (1).
       01 KB CONSTANT AS BYTE-LENGTH OF N OF W (1).
       01 KU CONSTANT AS LENGTH OF Q OF S (1).
       PROCEDURE DIVISION.
           DISPLAY K " " KB " " KU
           DISPLAY FUNCTION BYTE-LENGTH(G)
           MOVE "ABC" TO M OF V (3)
           DISPLAY M OF V (3)
           STOP RUN.
