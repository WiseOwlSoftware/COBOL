*>--- conformance-case
*> id: pb5621-unstring-any-length-below-2002
*> rules: [2002-GR-14.8.44.3-2, 2002-GR-14.8.44.3-9]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5621 - the ANY LENGTH clause is a COBOL-2002 introduction, so a zero-length formal parameter as
      *> identifier-1 or identifier-2 of UNSTRING (§2002:14.8.44.3 GR2, GR9) cannot be written at COBOL-85: the
      *> program draws the ANY LENGTH edition diagnostic (as any-length-at-85 does for the clause alone).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5621NG.
       PROCEDURE DIVISION.
           CALL "PB5621NI" USING BY CONTENT "X"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5621NI.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A1 PIC X(4).
       LINKAGE SECTION.
       01 L PIC X ANY LENGTH.
       PROCEDURE DIVISION USING L.
           UNSTRING L DELIMITED BY ";" INTO A1
           GOBACK.
       END PROGRAM PB5621NI.
       END PROGRAM PB5621NG.
