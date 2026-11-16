      *>--- conformance-case
      *> id: g2-type-cd-strong-renames
      *> rules: [2002-SR-13.16.55.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.55.2 SR3 - "If type-name-1 is described with the
      *> STRONG phrase, the subject of the entry shall not be renamed in
      *> whole or in part." The subject here is CD-REC, a record under an
      *> OUTPUT CD (a 2002-only reach, §2002:A.1.1), and the level-66
      *> CD-RN renames T-A OF CD-REC, part of it. Expected: COBOL5992 at
      *> the 66 entry (line 29). Every strongly-typed record under a CD
      *> also implicitly redefines the CD's record area (§2002:A.1.2.2
      *> SR9), so SR4 (COBOL5991) is unavoidably violated as well; the
      *> refusal pins the RENAMES diagnostic, which only an SR3 check
      *> produces. Fails if a RENAMES in a CD record is not checked.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2TYP03.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T TYPEDEF STRONG.
           05  T-A PIC X(4).
           05  T-B PIC X(3).
       COMMUNICATION SECTION.
       CD  CD-OUT FOR OUTPUT
           TEXT LENGTH IS CO-LEN
           STATUS KEY IS CO-STAT.
       01  CD-REC TYPE T.
       66  CD-RN RENAMES T-A.
       PROCEDURE DIVISION.
           DISPLAY "BAD".
           STOP RUN.
