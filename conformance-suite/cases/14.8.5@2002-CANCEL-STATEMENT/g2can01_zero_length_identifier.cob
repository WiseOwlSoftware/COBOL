      *>--- conformance-case
      *> id: g2can01_zero_length_identifier
      *> rules: [2002-GR-14.8.5.3-12]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.5.3 GR12 -- "If identifier-1 references a
      *> zero-length item, the CANCEL statement has no effect."
      *> §2002:8.5.4: a zero-length item is "A group data item
      *> containing only a variable-occurrence data item in which the
      *> number of occurrences is zero". G (working storage) and CG (in
      *> the record under an output CD) are such groups at 0 occurrences.
      *> §2002:8.6.5: G2K1H (neither INITIAL nor RECURSIVE) has its
      *> data "in the last-used state on other than the first
      *> activation"; §2002:14.8.5.3 GR3: after a successful CANCEL the
      *> next CALL finds it "in its initial state". G2K1H counts calls.
      *>   CALL                                   H=0001
      *>   CANCEL G   (0 occurrences: no effect)  CALL  H=0002
      *>   CANCEL CG  (CD record, 0 occ.: none)   CALL  H=0003
      *>   CANCEL QN  (CD item holding the name)  CALL  H=0001
      *>   CALL                                   H=0002
      *>   CANCEL G   (5 occurrences "G2K1H")     CALL  H=0001
      *> The last two CANCELs are the controls: the same operands with
      *> a length do cancel. Fails if a zero-length CANCEL cancels
      *> (H=0001 on line 2 or 3) or raises an error. COBOL0903
      *> (obsolete communication facility, §2002:G.2) is a warning.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2K1M.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 0.
       01 G.
          05 E PIC X OCCURS 0 TO 8 DEPENDING ON N.
       COMMUNICATION SECTION.
       CD CDOUT FOR OUTPUT.
       01 CDREC.
          02 QN PIC X(8).
          02 M PIC 9.
          02 CG.
             03 CE PIC X OCCURS 0 TO 8 DEPENDING ON M.
       PROCEDURE DIVISION.
       MAIN.
           CALL "G2K1H"
           MOVE 0 TO N
           CANCEL G
           CALL "G2K1H"
           MOVE 0 TO M
           CANCEL CG
           CALL "G2K1H"
           MOVE "G2K1H" TO QN
           CANCEL QN
           CALL "G2K1H"
           CALL "G2K1H"
           MOVE 5 TO N
           MOVE "G2K1H" TO G
           CANCEL G
           CALL "G2K1H"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2K1H.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       H1.
           ADD 1 TO K
           DISPLAY "H=" K
           GOBACK.
       END PROGRAM G2K1H.
       END PROGRAM G2K1M.
