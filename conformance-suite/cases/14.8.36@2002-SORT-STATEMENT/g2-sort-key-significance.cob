      *>--- conformance-case
      *> id: g2-sort-key-significance
      *> rules: [2002-GR-14.8.36.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.36.3 GR2 - "The order of significance of the keys
      *> is the order in which they are specified in the SORT
      *> statement, without regard to their association with
      *> ASCENDING or DESCENDING phrases." Both branches: the order in
      *> which the table elements are stored (format 2) and the order
      *> in which records are returned (format 1).
      *> The data "A1B2A2B1" has ties on each key, so every leg's
      *> result depends on which key is most significant. No two
      *> elements are equal on both keys (GR4 never applies).
      *> EXPECTED, derived from GR2 with GR1 (direction transitive),
      *> GR18 a)/b)/c) (tables) and GR7 a)/b)/c) (files):
      *>  T1 DESCENDING J2 ASCENDING J1: J2 first, desc: (B2 A2)
      *>     then (A1 B1), ties by J1 asc   => A2B2A1B1
      *>     (a compiler grouping ASCENDING keys first gives A2A1B2B1)
      *>  T2 ASCENDING J1 DESCENDING J2: J1 first => A2A1B2B1
      *>  T3 ASCENDING KEY J2 J1 (both asc, J2 first) => A1B1A2B2
      *>  T4 ASCENDING KEY J1 J2 => A1A2B1B2
      *>  F1 file SORT DESCENDING K2 ASCENDING K1 => A2 B2 A1 B1
      *>  F2 file SORT ASCENDING K1 DESCENDING K2 => A2 A1 B2 B1
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SRT02.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "g2srt02w.tmp".
       DATA DIVISION.
       FILE SECTION.
       SD SW.
       01 SR.
          05 K1 PIC X.
          05 K2 PIC 9.
       WORKING-STORAGE SECTION.
       01 G3.
          02 E3 OCCURS 4.
             03 J1 PIC X.
             03 J2 PIC 9.
       01 INIT-V PIC X(8) VALUE "A1B2A2B1".
       01 LEG PIC XX.
       01 I PIC 9.
       01 EOFF PIC X.
       PROCEDURE DIVISION.
       MAIN SECTION.
       M1.
           MOVE INIT-V TO G3.
           SORT E3 DESCENDING KEY J2 ASCENDING KEY J1.
           DISPLAY "T1 " G3.
           MOVE INIT-V TO G3.
           SORT E3 ASCENDING KEY J1 DESCENDING KEY J2.
           DISPLAY "T2 " G3.
           MOVE INIT-V TO G3.
           SORT E3 ASCENDING KEY J2 J1.
           DISPLAY "T3 " G3.
           MOVE INIT-V TO G3.
           SORT E3 ASCENDING KEY J1 J2.
           DISPLAY "T4 " G3.
           MOVE "F1" TO LEG.
           SORT SW DESCENDING KEY K2 ASCENDING KEY K1
               INPUT PROCEDURE IS IN-P
               OUTPUT PROCEDURE IS OUT-P.
           MOVE "F2" TO LEG.
           SORT SW ASCENDING KEY K1 DESCENDING KEY K2
               INPUT PROCEDURE IS IN-P
               OUTPUT PROCEDURE IS OUT-P.
           STOP RUN.
       IN-P SECTION.
       IN-1.
           MOVE INIT-V TO G3.
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 4
               MOVE E3 (I) TO SR
               RELEASE SR
           END-PERFORM.
       OUT-P SECTION.
       OUT-1.
           MOVE "N" TO EOFF.
           PERFORM UNTIL EOFF = "Y"
               RETURN SW AT END MOVE "Y" TO EOFF
                   NOT AT END DISPLAY LEG " " SR
               END-RETURN
           END-PERFORM.
