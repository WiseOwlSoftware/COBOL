      *>--- conformance-case
      *> id: 1985-FMT-VI_2_4_2
      *> rules: [1985-FMT-VI.2.4.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.2.4.2 (END PROGRAM HEADER), the general format
      *> "END PROGRAM program-name.", ends each of two separately
      *> compiled programs below. The program must translate and run at
      *> 85 (kb/Work PB5740: a general format is witnessed by an accepted
      *> case, never by refusals alone). §1985:VI.2.4.4 GR2: the next
      *> source statement after a program its end program header
      *> terminates "must be the Identification Division header of a
      *> program to be compiled separately", which ENDPGM2 is; ENDPGM1
      *> CALLs it, so the run prints FIRST BEGIN, SECOND, FIRST END.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ENDPGM1.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "FIRST BEGIN".
           CALL "ENDPGM2".
           DISPLAY "FIRST END".
           STOP RUN.
       END PROGRAM ENDPGM1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ENDPGM2.
       PROCEDURE DIVISION.
       P2.
           DISPLAY "SECOND".
           EXIT PROGRAM.
       END PROGRAM ENDPGM2.
