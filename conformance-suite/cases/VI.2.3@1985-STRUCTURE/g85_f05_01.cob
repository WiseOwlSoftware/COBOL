      *>--- conformance-case
      *> id: g85_f05_01
      *> rules: [1985-GR-VI.2.3.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.2.3.3 GR2 - a sequence of three separately compiled
      *> programs in one source: the first two are terminated by an end
      *> program header, the LAST omits it ("except for the last program
      *> in the sequence"); §1985:VI.2.2 ends that one at the absence of
      *> further lines. Each program keeps its own W, so the calls return
      *> to a main whose W is still "MAIN". A compiler that refuses the
      *> header-less last program, or runs the sequence as one program,
      *> fails to translate or prints other lines.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0501.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W PIC X(6) VALUE "MAIN".
       PROCEDURE DIVISION.
       P1.
           DISPLAY W "START".
           CALL "G85F05S1".
           CALL "G85F05S2".
           DISPLAY W "END".
           STOP RUN.
       END PROGRAM G85F0501.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F05S1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W PIC X(6) VALUE "SECOND".
       PROCEDURE DIVISION.
       P1.
           DISPLAY W.
           EXIT PROGRAM.
       END PROGRAM G85F05S1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F05S2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W PIC X(6) VALUE "LAST-P".
       PROCEDURE DIVISION.
       P1.
           DISPLAY W.
           EXIT PROGRAM.
