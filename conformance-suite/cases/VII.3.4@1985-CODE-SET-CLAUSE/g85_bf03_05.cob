      *>--- conformance-case
      *> id: g85_bf03_05
      *> rules: [1985-GR-VII.3.4.4-1, 1985-GR-VII.3.4.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.3.4.4 GR1 - with CODE-SET, the character set on the
      *> external media is alphabet-name-1's (a) and the clause is the
      *> conversion to and from native on each input and output (b).
      *> §1985:VII.3.4.4 GR2 - without CODE-SET the native character set
      *> is assumed on the media. FE (CODE-SET EBCDIC, an implementor-
      *> name-2 alphabet, §1985:VI.4.5.4 GR4c) and FR (no CODE-SET)
      *> share one physical file. FE writes "AB012": FE reads it back
      *> unchanged (GR1b), FR reads the EBCDIC codes C1 C2 F0 F1 F2 as
      *> the native characters of those codes (GR1a, GR2: ORD under the
      *> native sequence is code + 1). FR then writes and reads "AB012"
      *> unchanged (GR2). Skipping the conversion prints 66 67 49 50 51.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0305.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET A-EBC IS EBCDIC.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FE ASSIGN TO "g85bf0305.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT FR ASSIGN TO "g85bf0305.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  FE
           CODE-SET IS A-EBC.
       01  RE PIC X(5).
       FD  FR.
       01  RR PIC X(5).
       WORKING-STORAGE SECTION.
       01  I  PIC 9.
       01  N3 PIC 999.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT FE.
           MOVE "AB012" TO RE.
           WRITE RE.
           CLOSE FE.
           OPEN INPUT FE.
           MOVE SPACES TO RE.
           READ FE AT END DISPLAY "FE-EOF".
           DISPLAY "FE-READ=[" RE "]".
           CLOSE FE.
           OPEN INPUT FR.
           MOVE SPACES TO RR.
           READ FR AT END DISPLAY "FR-EOF".
           CLOSE FR.
           PERFORM P2 VARYING I FROM 1 BY 1 UNTIL I > 5.
           OPEN OUTPUT FR.
           MOVE "AB012" TO RR.
           WRITE RR.
           CLOSE FR.
           OPEN INPUT FR.
           MOVE SPACES TO RR.
           READ FR AT END DISPLAY "FR-EOF".
           DISPLAY "FR-READ=[" RR "]".
           CLOSE FR.
           STOP RUN.
       P2.
           COMPUTE N3 = FUNCTION ORD(RR(I:1)).
           DISPLAY "MEDIUM-" I "=" N3.
