      *>--- conformance-case
      *> id: g2002-cd-usage-group-same
      *> rules: [2002-SR-13.16.58.2-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> At 2002 the record description entries after a CD entry are
      *> ordinary data description entries (§2002:A.1.1.2), so the
      *> USAGE rules govern the CD record CR as they do working storage.
      *> §2002:13.16.58.2 SR2 - "If the USAGE clause is written in the
      *>   data description entry for a group item, it may also be
      *>   written in the data description entry for any subordinate
      *>   elementary item or group item, but the same usage shall be
      *>   specified in both entries." GB (USAGE BINARY) has the same
      *>   usage written again on the elementary B1, on the subordinate
      *>   group BG and on the elementary B3 of BG; GD (USAGE DISPLAY)
      *>   has it written again on D1. The program is accepted.
      *> §2002:13.16.58.3 GR1 - the group usage applies to each
      *>   elementary item in the group, so B2 (no USAGE written) is
      *>   BINARY as B1 and B3 are: same picture, same usage, same
      *>   length (line BINARY SAME). B3 = -1234 + 5678 = 4444 (line
      *>   SUM= 4444, PIC -9(4) puts a space for the positive sign).
      *>   D1 holds XY and GD is its 2 positions (line D1=XY LEN=02).
      *> It fails if the repeated same usage is refused in a CD record,
      *> or if a written and an inherited usage differ in length.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2UCD01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-SUM PIC -9(4).
       01  W-LEN PIC 99.
       COMMUNICATION SECTION.
       CD  CO FOR OUTPUT
           TEXT LENGTH IS CO-LEN
           STATUS KEY IS CO-STAT.
       01  CR.
           05  GB USAGE BINARY.
               10  B1 PIC S9(4) USAGE BINARY.
               10  B2 PIC S9(4).
               10  BG USAGE BINARY.
                   15  B3 PIC S9(4) USAGE BINARY.
           05  GD USAGE DISPLAY.
               10  D1 PIC X(2) USAGE DISPLAY.
       PROCEDURE DIVISION.
       MAIN.
           MOVE -1234 TO B1
           MOVE 5678 TO B2
           COMPUTE B3 = B1 + B2
           IF FUNCTION LENGTH(B1) = FUNCTION LENGTH(B2)
              AND FUNCTION LENGTH(B2) = FUNCTION LENGTH(B3)
               DISPLAY "BINARY SAME"
           ELSE
               DISPLAY "BINARY DIFFERENT"
           END-IF
           MOVE B3 TO W-SUM
           DISPLAY "SUM=" W-SUM
           MOVE "XY" TO D1
           COMPUTE W-LEN = FUNCTION LENGTH(GD)
           DISPLAY "D1=" D1 " LEN=" W-LEN
           STOP RUN.
