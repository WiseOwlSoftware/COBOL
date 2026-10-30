*>--- conformance-case
*> id: pb2867-constant-length-composed-cycle
*> rules: [SR-13.10.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2867 - 13.10.3 SR4: "The length of data-name-1 or data-name-2 shall not be dependent, directly
      *> or indirectly, upon the value of constant-name-1". V's OCCURS clause demands K while G is being
      *> described. Measuring a subordinate V's TYPE clause supplies is legal by itself
      *> (pb2867_constant_length_composed_open_entry), but K measures G, the group V is subordinate to, and
      *> G's length is V's occurrence count times T's length - so the length K measures depends on K.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2867CYC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T TYPEDEF.
          05 M PIC X(3).
       01 G.
          05 V TYPE T OCCURS K.
       01 K CONSTANT AS LENGTH OF G.
       PROCEDURE DIVISION.
           DISPLAY K
           STOP RUN.
