*>--- conformance-case
*> id: pb4865-perform-varying-signed-identifier
*> rules: [SR-14.9.28.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> PB4865 - the same rule for a separated sign: + K is an arithmetic expression (ISO §2023:8.8.1.1), not the identifier-4 that §2023:14.9.28.2 prints
      *> for the BY phrase.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPV4865J.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 I PIC 99.
       01 K PIC 99 VALUE 2.
       PROCEDURE DIVISION.
           PERFORM VARYING I FROM 1 BY + K UNTIL I > 5
               DISPLAY I
           END-PERFORM
           STOP RUN.
       END PROGRAM NEGPV4865J.
