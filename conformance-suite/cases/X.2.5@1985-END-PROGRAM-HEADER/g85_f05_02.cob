      *>--- conformance-case
      *> id: g85_f05_02
      *> rules: [1985-GR-X.2.5.4-1, 1985-FMT-X.2.5.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> Also the accepted witness of §1985:X.2.5.2, the general format
      *> END PROGRAM program-name.: each program below ends with one
      *> (kb/Work PB5740).
      *> §1985:X.2.5.4 GR1 - three levels of nesting, each program with
      *> its end program header: G85F0502 contains, G85F05M2 both is
      *> contained and contains, G85F05N2 is contained. Each program CALLs
      *> the program it directly contains. A compiler that misplaces a
      *> program's end, or refuses the headers, fails to translate or
      *> prints other lines.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0502.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "OUTER BEGIN".
           CALL "G85F05M2".
           DISPLAY "OUTER END".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F05M2.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "MIDDLE BEGIN".
           CALL "G85F05N2".
           DISPLAY "MIDDLE END".
           EXIT PROGRAM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F05N2.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "INNER".
           EXIT PROGRAM.
       END PROGRAM G85F05N2.
       END PROGRAM G85F05M2.
       END PROGRAM G85F0502.
