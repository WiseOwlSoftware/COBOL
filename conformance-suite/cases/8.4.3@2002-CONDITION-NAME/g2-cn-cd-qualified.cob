      *>--- conformance-case
      *> id: g2-cn-cd-qualified
      *> rules: [2002-SR-8.4.3.2-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.4.3.2 SR2 - "Qualified-condition-name-with-subscripts-1
      *> is defined by 8.4.1.1, Qualification". §2002:8.4.1.1.1 Format 2
      *> ends with the file-cd-report-qualifier, and "where
      *> file-cd-report-qualifier is" IN/OF file-name-1 | cd-name-1 |
      *> report-name-1: at 2002 a condition-name in a record description
      *> entry following a CD is qualified by the cd-name.
      *> §2002:8.4.1.1.2 SR4 - "For a condition-name, the hierarchy is
      *> that of the associated conditional variable"; SR3 - "The words
      *> IN and OF are equivalent". §2002:8.4.1.2.1 Format 2 adds the
      *> subscripts (qualified-condition-name-with-subscripts).
      *> IS-A and TE-Y are each declared twice (W and CREC), so only the
      *> qualifier chooses one. F1 OF W = "B", F1 OF CREC = "A";
      *> TE OF W (1..3) = "Y"; TE OF CDIN (1..3) = N, Y, N.
      *> C1 IS-A OF CDIN tests F1 OF CREC = "A": T.
      *> C2 IS-A IN F1 OF CREC OF CDIN (data qualifiers, cd-name): T.
      *> C3 IS-A OF W tests F1 OF W = "B": F (a cd qualifier ignored or
      *>    bound to the wrong item turns C1/C3 around).
      *> C4 TE-Y OF CDIN (2): T. C5 TE-Y IN TB OF CREC IN CDIN (1): F
      *>    (W's table is all Y, so a wrong binding prints T).
      *> C6 TE-Y OF W (1): T.
      *> C7 SET IS-A OF W TO TRUE and SET TE-Y OF CDIN (3) TO TRUE move
      *>    "A" into F1 OF W and "Y" into TE OF CDIN (3); F1 OF CREC
      *>    stays "A": "C7 A Y A".
      *> The communication facility is obsolete at 2002 (COBOL0903).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CN02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W.
           05 F1 PIC X.
              88 IS-A VALUE "A".
           05 TB OCCURS 3.
              10 TE PIC X.
                 88 TE-Y VALUE "Y".
       COMMUNICATION SECTION.
       CD  CDIN FOR INPUT.
       01  CREC.
           05 F1 PIC X.
              88 IS-A VALUE "A".
           05 TB OCCURS 3.
              10 TE PIC X.
                 88 TE-Y VALUE "Y".
           05 FILLER PIC X(83).
       PROCEDURE DIVISION.
           MOVE "BYYY" TO W.
           MOVE "A" TO F1 OF CREC.
           MOVE "N" TO TE OF CDIN (1).
           MOVE "Y" TO TE OF CDIN (2).
           MOVE "N" TO TE OF CDIN (3).
           IF IS-A OF CDIN DISPLAY "C1 T" ELSE DISPLAY "C1 F" END-IF.
           IF IS-A IN F1 OF CREC OF CDIN
               DISPLAY "C2 T" ELSE DISPLAY "C2 F" END-IF.
           IF IS-A OF W DISPLAY "C3 T" ELSE DISPLAY "C3 F" END-IF.
           IF TE-Y OF CDIN (2) DISPLAY "C4 T" ELSE DISPLAY "C4 F"
           END-IF.
           IF TE-Y IN TB OF CREC IN CDIN (1)
               DISPLAY "C5 T" ELSE DISPLAY "C5 F" END-IF.
           IF TE-Y OF W (1) DISPLAY "C6 T" ELSE DISPLAY "C6 F" END-IF.
           SET IS-A OF W TO TRUE.
           SET TE-Y OF CDIN (3) TO TRUE.
           DISPLAY "C7 " F1 OF W " " TE OF CDIN (3) " " F1 OF CREC.
           STOP RUN.
