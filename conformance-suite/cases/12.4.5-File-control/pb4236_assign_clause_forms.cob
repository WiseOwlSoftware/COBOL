      *>--- conformance-case
      *> id: pb4236_assign_clause_forms
      *> rules: [FMT-12.4.5.1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §12.4.5.1 General formats - the ASSIGN clause is printed with
      *>   NO bracket around it in all four formats, and the figure notes
      *>   say "every stacked group is a plain brace (exactly one) or a
      *>   plain bracket (zero or one)", so every SELECT entry specifies
      *>   one. The complement is the point of this witness: an entry
      *>   that DOES write the clause is read as written wherever the
      *>   clause stands (§12.4.5.2 SR1: any order after SELECT) and
      *>   with the optional word TO omitted (TO is not underlined).
      *>   cite.py --check 12.4.5.1 "every stacked group is a plain
      *>   brace (exactly one) or a plain bracket (zero or one)"
      *>   -> OK  §12.4.5.1  (General formats)
      *> F-SQ is Format 3, ASSIGN after ORGANIZATION with TO omitted.
      *> F-RL is Format 2, ASSIGN in the middle of its clauses.
      *> SRT is Format 4 (the sort-merge file of an SD): SELECT and
      *> ASSIGN alone.
      *> Each file is bound to the name its own ASSIGN clause wrote, so a
      *> record written and read back arrives intact, and the sort
      *> returns the three released keys in ascending order:
      *>   SQ=SEQ01  record written to F-SQ and read back.
      *>   RL=REL01  record written to F-RL and read back.
      *>   SORT=A SORT=B SORT=C  keys released C, A, B, returned A, B, C.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P4236A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F-SQ ORGANIZATION IS SEQUENTIAL
               ASSIGN "P4236A.DAT".
           SELECT F-RL ACCESS MODE IS SEQUENTIAL
               ASSIGN TO "P4236A.REL"
               ORGANIZATION IS RELATIVE.
           SELECT SRT ASSIGN TO "P4236A.SRT".
       DATA DIVISION.
       FILE SECTION.
       FD F-SQ.
       01 SQ-REC PIC X(5).
       FD F-RL.
       01 RL-REC PIC X(5).
       SD SRT.
       01 SRT-REC.
          05 SRT-KEY PIC X.
       PROCEDURE DIVISION.
       MAIN-S SECTION.
       MAIN-P.
           OPEN OUTPUT F-SQ.
           MOVE "SEQ01" TO SQ-REC.
           WRITE SQ-REC.
           CLOSE F-SQ.
           OPEN INPUT F-SQ.
           READ F-SQ AT END MOVE "EOF" TO SQ-REC END-READ.
           DISPLAY "SQ=" SQ-REC.
           CLOSE F-SQ.
           OPEN OUTPUT F-RL.
           MOVE "REL01" TO RL-REC.
           WRITE RL-REC.
           CLOSE F-RL.
           OPEN INPUT F-RL.
           READ F-RL AT END MOVE "EOF" TO RL-REC END-READ.
           DISPLAY "RL=" RL-REC.
           CLOSE F-RL.
           SORT SRT ON ASCENDING KEY SRT-KEY
               INPUT PROCEDURE IS SORT-IN
               OUTPUT PROCEDURE IS SORT-OUT.
           STOP RUN.
       SORT-IN SECTION.
       SI-1.
           MOVE "C" TO SRT-KEY.
           RELEASE SRT-REC.
           MOVE "A" TO SRT-KEY.
           RELEASE SRT-REC.
           MOVE "B" TO SRT-KEY.
           RELEASE SRT-REC.
       SORT-OUT SECTION.
       SO-1.
           RETURN SRT AT END GO TO SO-X END-RETURN.
           DISPLAY "SORT=" SRT-KEY.
           GO TO SO-1.
       SO-X.
           EXIT.
