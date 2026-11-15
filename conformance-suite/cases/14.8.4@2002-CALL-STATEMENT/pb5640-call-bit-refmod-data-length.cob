*>--- conformance-case
*> id: pb5640-call-bit-refmod-data-length
*> rules: [2002-SR-14.8.4.2-6]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5640. §2002:14.8.4.2 SR6 (and §2014:14.9.4.2 SR6): a bit identifier-2 passed BY REFERENCE is described such that
      *> "subscripting and reference modification in identifier-2 consist of only fixed-point numeric literals or arithmetic expressions", the
      *> leftmost position AND the length, so B8(1:L) with the data-name L is refused at 2002 and 2014 (COBOL1683). §2023:14.9.4.3 SR6 narrows
      *> the rule to "subscripting and the leftmost position in a reference modification", so the program is accepted at 2023 (pb5640-call-bit-refmod-data-length-2023), where the
      *> called program receives the eight bits (1:8) and displays them.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5640D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 L PIC 9(2) VALUE 8.
       01 BG.
          05 B8 PIC 1(16) USAGE BIT VALUE B"1111111100000000".
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB5640S" USING BY REFERENCE B8(1:L)
           STOP RUN.
       END PROGRAM PB5640D.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5640S.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LB PIC 1(8) USAGE BIT.
       PROCEDURE DIVISION USING LB.
       MAIN.
           DISPLAY LB.
           GOBACK.
       END PROGRAM PB5640S.
