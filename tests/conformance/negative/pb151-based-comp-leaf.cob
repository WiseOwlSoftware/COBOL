      *> reject-at: 2023
      *> kb/Work PB151: a BASED record with a non-DISPLAY leaf previously
      *> compiled clean (a silent bind-time bare-continue) and CRASHED at
      *> run time on its first ALLOCATE, while the EXTERNAL twin of the
      *> identical failure diagnosed at bind. Now the bind-time
      *> diagnostic; the character cell model for such leaves is the
      *> Tier-C island (kb/Work PB164).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB151N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R BASED.
          05 N PIC S9(4) COMP.
       PROCEDURE DIVISION.
       MAIN.
           ALLOCATE R
           STOP RUN.
