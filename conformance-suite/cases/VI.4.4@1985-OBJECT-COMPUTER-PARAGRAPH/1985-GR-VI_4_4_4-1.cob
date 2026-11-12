      *>--- conformance-case
      *> id: 1985-GR-VI_4_4_4-1
      *> rules: [1985-GR-VI.4.4.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.4.4.4 GR1 (THE OBJECT-COMPUTER PARAGRAPH): "The
      *> computer-name may provide a means for identifying equipment
      *> configuration, in which case the computer-name and its implied
      *> configuration are specified by each implementor. The
      *> configuration definition contains specific information
      *> concerning the memory size. The implementor defines what is to
      *> be done if the subset specified by the user is less than the
      *> minimum configuration required for running the object program."
      *> The rule asks nothing a compiler could fail: it leaves the
      *> names, the configuration and the undersized-subset action to
      *> the implementor. WiseOwl names no equipment configuration
      *> (docs/CONFORMANCE.md section 7, DOC-A.1-127: any computer-name
      *> is accepted and identifies nothing; one object computer), so
      *> the observable obligation is that an arbitrary computer-name,
      *> with a MEMORY SIZE of one word (far below any minimum
      *> configuration), is accepted and the program runs as written.
      *> A compiler that refused the name or the size would fail this
      *> test (kb/Work PB5373 class 5).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OBJCNAME.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. ANY-SOURCE-9.
       OBJECT-COMPUTER. NO-SUCH-MACHINE-0
           MEMORY SIZE 1 WORDS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-A PIC 99 VALUE 7.
       PROCEDURE DIVISION.
       MAIN-PARA.
           ADD 5 TO WS-A.
           DISPLAY "SIZE " WS-A.
           STOP RUN.
