      *>--- conformance-case
      *> id: g2-type-cd-strong-level
      *> rules: [2002-SR-13.16.55.2-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.55.2 SR6 - "If type-name-1 is described with the
      *> STRONG phrase, the subject of the entry shall be specified only
      *> with level number 1 or be subordinate to a type declaration that
      *> includes the STRONG phrase." CD-S is at level 05 in CD-REC, a
      *> record under an output CD (a 2002-only reach, §2002:A.1.1), and
      *> CD-REC is no type declaration. Expected: COBOL1532 at line 28.
      *> CD-S also shares the implicitly redefined CD record area
      *> (§2002:A.1.2.2 SR9), so SR4 (COBOL5991) fires too; the refusal
      *> pins the level diagnostic only an SR6 check produces.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2TYP06.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T TYPEDEF STRONG.
           05  T-A PIC X(4).
           05  T-B PIC X(3).
       COMMUNICATION SECTION.
       CD  CD-OUT FOR OUTPUT
           TEXT LENGTH IS CO-LEN
           STATUS KEY IS CO-STAT.
       01  CD-REC.
           05  CD-S TYPE T.
       PROCEDURE DIVISION.
           DISPLAY "BAD".
           STOP RUN.
