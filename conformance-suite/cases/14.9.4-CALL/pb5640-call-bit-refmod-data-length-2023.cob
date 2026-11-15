*>--- conformance-case
*> id: pb5640-call-bit-refmod-data-length-2023
*> rules: [SR-14.9.4.3-6]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5640. §2023:14.9.4.3 SR6: a bit identifier-2 passed BY REFERENCE "shall be described such that it is aligned on a byte
      *> boundary and that subscripting and the leftmost position in a reference modification of identifier-2 consist of only fixed-point
      *> numeric literals or arithmetic expressions whose result is a positive integer" — the LENGTH is not constrained at 2023, so B8(1:L)
      *> with the data-name L is accepted and the called program receives the first eight bits, 11111111 (2002 and 2014 require the length to
      *> be a literal too: pb5640-call-bit-refmod-data-length). Why it can fail: the whole-reference-modification rule of 2002 and 2014 is
      *> applied at 2023 and the program is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5640F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 L PIC 9(2) VALUE 8.
       01 BG.
          05 B8 PIC 1(16) USAGE BIT VALUE B"1111111100000000".
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB5640T" USING BY REFERENCE B8(1:L)
           STOP RUN.
       END PROGRAM PB5640F.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5640T.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LB PIC 1(8) USAGE BIT.
       PROCEDURE DIVISION USING LB.
       MAIN.
           DISPLAY LB.
           GOBACK.
       END PROGRAM PB5640T.
