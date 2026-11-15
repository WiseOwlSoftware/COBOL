      *>--- conformance-case
      *> id: g2-sort-use-not-normal
      *> rules: [2002-GR-14.8.36.3-16]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
       >>TURN EC-SORT-MERGE-ACTIVE CHECKING ON
      *> §2002:14.8.36.3 GR16 - "If a USE procedure invoked while a
      *> format 1 SORT statement is active does not complete normally,
      *> the SORT statement is terminated."
      *>  . §2002:14.8.36.3 GR9: the input procedure's range includes
      *>    declarative statements executed as a result of its
      *>    statements, so D-NF, run by the OPEN of the absent file NF
      *>    (I-O status 35, §2002:9.1.12.5), runs while the SORT is
      *>    active.
      *>  . §2002:14.5.12.1.1 1): a RESUME executed in the declarative
      *>    means it does not complete normally; GR16 terminates the
      *>    SORT. RESUME AT L1-LANDED is a GO TO (§2002:14.8.31.3 GR3).
      *>  . GR9: a format 1 SORT executed in the range of an active
      *>    SORT's input procedure sets EC-SORT-MERGE-ACTIVE (fatal,
      *>    checked ON above, no USE for it: the run unit ends
      *>    abnormally, §2002:14.5.12.1.2). L1-LANDED is reached by a
      *>    transfer of control from that range, so the L2 SORT runs,
      *>    and its L2-OUT lines print, only because GR16 terminated
      *>    the L1 SORT.
      *> EXPECTED, derived from those rules:
      *>  L1-USE=35, L1-LANDED (no L1-IN-AFTER-OPEN, no L1-OUT line:
      *>  the terminated SORT's output procedure never runs), then
      *>  L2-OUT=Y, L2-OUT=Z (the reused sort file, ascending, GR7 a)),
      *>  L2-AFTER, DONE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SRT16.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT NF ASSIGN TO "g2srt16-absent.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS.
           SELECT SW ASSIGN TO "g2srt16w.tmp".
       DATA DIVISION.
       FILE SECTION.
       FD NF.
       01 NF-REC PIC X.
       SD SW.
       01 SR PIC X.
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       01 LEG PIC XX.
       01 EOFF PIC X.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-NF SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON NF.
       D-NF-P.
           DISPLAY LEG "-USE=" FS.
           RESUME AT L1-LANDED.
       END DECLARATIVES.
       MAIN SECTION.
       L1.
           MOVE "L1" TO LEG.
           SORT SW ASCENDING KEY SR
               INPUT PROCEDURE IS IN-OPEN
               OUTPUT PROCEDURE IS OUT-ALL.
           DISPLAY "L1-NOT-REACHED".
       L1-LANDED.
           DISPLAY "L1-LANDED".
       L2.
           MOVE "L2" TO LEG.
           SORT SW ASCENDING KEY SR
               INPUT PROCEDURE IS IN-ZY
               OUTPUT PROCEDURE IS OUT-ALL.
           DISPLAY "L2-AFTER".
           DISPLAY "DONE".
           STOP RUN.
       IN-OPEN-S SECTION.
       IN-OPEN.
           MOVE "B" TO SR.
           RELEASE SR.
           OPEN INPUT NF.
           DISPLAY LEG "-IN-AFTER-OPEN".
           MOVE "A" TO SR.
           RELEASE SR.
       IN-ZY-S SECTION.
       IN-ZY.
           MOVE "Z" TO SR.
           RELEASE SR.
           MOVE "Y" TO SR.
           RELEASE SR.
       OUT-ALL-S SECTION.
       OUT-ALL.
           MOVE "N" TO EOFF.
           PERFORM UNTIL EOFF = "Y"
               RETURN SW AT END MOVE "Y" TO EOFF
                   NOT AT END DISPLAY LEG "-OUT=" SR
               END-RETURN
           END-PERFORM.
