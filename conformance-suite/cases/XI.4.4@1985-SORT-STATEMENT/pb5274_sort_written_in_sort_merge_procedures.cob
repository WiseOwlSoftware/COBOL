      *>--- conformance-case
      *> id: pb5274_sort_written_in_sort_merge_procedures
      *> rules: [1985-SR-XI.4.4.3-1, 1985-GR-XI.4.4.4-7,
      *>     1985-GR-XI.4.4.4-10, 1985-GR-XI.4.1.4-8]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5274. A file SORT WRITTEN in an input or output
      *> procedure, on a path that never runs, is legal at 85.
      *> §1985:XI.4.4.3 SR1: "A SORT statement may appear anywhere in
      *> the Procedure Division except in the declarative portion."
      *> The procedure rules forbid only EXECUTING one: §1985:XI.4.4.4
      *> GR7 (input procedure) "must not cause the execution of any
      *> MERGE, RETURN, or SORT statement", GR10 (output procedure) and
      *> §1985:XI.4.1.4 GR8 (MERGE output procedure) "must not cause
      *> the execution of any MERGE, RELEASE, or SORT statement".
      *> The lexical ban is §2023:14.9.40.3 SR3, a COBOL-2023 rule
      *> (2002 and 2014 keep the 1985 placement rule); the 2023
      *> refusal is pb1139-sort-in-sort-procedure. A compiler that
      *> applies the 2023 ban at 85 refuses this program (COBOL2700)
      *> and fails; one that runs the guarded SORTs prints NESTED.
      *> Expected: the SORT returns AAA BBB in order, the MERGE
      *> returns CCC DDD in order, then DONE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5274S.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO PB5274SW.
           SELECT SG ASSIGN TO PB5274SG.
           SELECT FI ASSIGN TO PB5274FI.
           SELECT FJ ASSIGN TO PB5274FJ.
           SELECT FO ASSIGN TO PB5274FO.
       DATA DIVISION.
       FILE SECTION.
       SD  SF.
       01  SR PIC X(3).
       SD  SG.
       01  GR PIC X(3).
       FD  FI LABEL RECORDS ARE STANDARD.
       01  FIR PIC X(3).
       FD  FJ LABEL RECORDS ARE STANDARD.
       01  FJR PIC X(3).
       FD  FO LABEL RECORDS ARE STANDARD.
       01  FOR1 PIC X(3).
       WORKING-STORAGE SECTION.
       01  NEVER PIC X VALUE "N".
       PROCEDURE DIVISION.
       M SECTION.
       M1.
           OPEN OUTPUT FI. MOVE "CCC" TO FIR. WRITE FIR. CLOSE FI.
           OPEN OUTPUT FJ. MOVE "DDD" TO FJR. WRITE FJR. CLOSE FJ.
           SORT SF ON ASCENDING KEY SR
               INPUT PROCEDURE IS I-S
               OUTPUT PROCEDURE IS O-S.
           MERGE SG ON ASCENDING KEY GR
               USING FI FJ
               OUTPUT PROCEDURE IS MO-S.
           DISPLAY "DONE".
           STOP RUN.
       I-S SECTION.
       I1.
           MOVE "BBB" TO SR. RELEASE SR.
           MOVE "AAA" TO SR. RELEASE SR.
           IF NEVER = "Y"
               DISPLAY "NESTED"
               SORT SG ON ASCENDING KEY GR USING FI GIVING FO.
       O-S SECTION.
       O1.
           RETURN SF AT END GO TO O9.
           DISPLAY SR.
           IF NEVER = "Y"
               DISPLAY "NESTED"
               SORT SG ON ASCENDING KEY GR USING FI GIVING FO.
           GO TO O1.
       O9.
           EXIT.
       MO-S SECTION.
       MO1.
           RETURN SG AT END GO TO MO9.
           DISPLAY GR.
           IF NEVER = "Y"
               DISPLAY "NESTED"
               SORT SF ON ASCENDING KEY SR USING FI GIVING FO.
           GO TO MO1.
       MO9.
           EXIT.
