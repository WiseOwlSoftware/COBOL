      *>--- conformance-case
      *> id: g85_f06_03
      *> rules: [1985-GR-XVI.2.2.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XVI.2.2.4 GR1 - the Segmentation module's
      *>   OBJECT-COMPUTER paragraph (with its SEGMENT-LIMIT clause)
      *>   is written once, in the container G85F0603; all its
      *>   clauses apply to the contained program G85F0603I, which
      *>   has no OBJECT-COMPUTER paragraph of its own and whose
      *>   procedure division is segmented (sections 05, 20 and 60).
      *> The PROGRAM COLLATING SEQUENCE clause is the observable one
      *> (§1985:VI.4.4.4 GR5): REV puts "Z" before "B" before "A", so
      *> each comparison in the contained program answers YES only if
      *> the container's sequence reached it; native order answers NO.
      *> SEGMENT-LIMIT IS 10 makes section 20 an overlayable fixed
      *> segment, which keeps the logical properties of a fixed one
      *> (§1985:XVI.2.3.1; §1985:XVI.3.3.1): the GO TO altered in
      *> section 05 stays
      *> altered after the independent section 60 runs, so the
      *> contained program reaches S20-ALTERED, not S20-ORIGINAL.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0603.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. XX.
       OBJECT-COMPUTER. XX
           PROGRAM COLLATING SEQUENCE IS REV
           SEGMENT-LIMIT IS 10.
       SPECIAL-NAMES.
           ALPHABET REV IS "Z" "B" "A".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-A PIC X VALUE "A".
       01  WS-B PIC X VALUE "B".
       PROCEDURE DIVISION.
       P-MAIN.
           IF WS-A > WS-B
               DISPLAY "OUTER A>B YES"
           ELSE
               DISPLAY "OUTER A>B NO".
           CALL "G85F0603I".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0603I.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-IA PIC X VALUE "A".
       01  WS-IB PIC X VALUE "B".
       01  WS-IZ PIC X VALUE "Z".
       PROCEDURE DIVISION.
       S05 SECTION 05.
       S05-P.
           ALTER S20-SWITCH TO PROCEED TO S20-ALTERED.
           PERFORM S60.
           GO TO S20-START.
       S20 SECTION 20.
       S20-START.
           IF WS-IA > WS-IB
               DISPLAY "INNER A>B YES"
           ELSE
               DISPLAY "INNER A>B NO".
       S20-SWITCH.
           GO TO S20-ORIGINAL.
       S20-ORIGINAL.
           DISPLAY "S20-ORIGINAL".
           GO TO S20-EXIT.
       S20-ALTERED.
           DISPLAY "S20-ALTERED".
       S20-EXIT.
           EXIT PROGRAM.
       S60 SECTION 60.
       S60-P.
           IF WS-IZ < WS-IA
               DISPLAY "INNER Z<A YES"
           ELSE
               DISPLAY "INNER Z<A NO".
       END PROGRAM G85F0603I.
       END PROGRAM G85F0603.
