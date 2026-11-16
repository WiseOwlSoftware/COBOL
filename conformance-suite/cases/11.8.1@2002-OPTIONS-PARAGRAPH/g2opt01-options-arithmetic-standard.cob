      *>--- conformance-case
      *> id: g2opt01-options-arithmetic-standard
      *> rules: [2002-FMT-11.8.1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:11.8.1 - the general format "OPTIONS. [ arithmetic-clause ]
      *> [ entry-convention-clause ] ." with the 2002 arithmetic-clause of
      *> §2002:11.8.4.1, ARITHMETIC IS { NATIVE | STANDARD }. STANDARD is
      *> the 2002-only term (2014 and 2023 replace it with STANDARD-BINARY
      *> and STANDARD-DECIMAL), so this source is legal at 2002 alone.
      *> Three source elements, each with an OPTIONS paragraph form the
      *> format grants:
      *>  - G2OPT02: "OPTIONS." then "ARITHMETIC IS STANDARD." - the
      *>    clause and the terminating period;
      *>  - G2OPT01N (contained): "ARITHMETIC IS NATIVE." - the other term,
      *>    overriding the containing element's clause (§2002:11.8.3 GR1);
      *>  - G2OPT01E (contained): "OPTIONS." alone - no clause, one of the
      *>    two separator periods omitted (§2002:11.8.2 SR1).
      *> The ENTRY-CONVENTION slot is not written: its names are
      *> implementor-defined (§2002:11.8.5.3 GR2) and optional
      *> (§2002:B.1 item 44), and this implementor defines none at 2002.
      *> Each element computes an exact quotient, so the values do not
      *> depend on the method of arithmetic, and integer receivers, so the
      *> DISPLAY image is the digits alone: 100 / 4 = 25 into PIC 99
      *> displays "25"; 7 * 3 / 3 = 7 into PIC 99 displays "07"; 9 / 3 = 3
      *> into PIC 9 displays "3". Fails if any of the three
      *> OPTIONS forms is refused, or a contained element does not run.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2OPT02.
       OPTIONS.
           ARITHMETIC IS STANDARD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W PIC 99.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE W = 100 / 4.
           DISPLAY "S=" W.
           CALL "G2OPT01N".
           CALL "G2OPT01E".
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2OPT01N.
       OPTIONS.
           ARITHMETIC IS NATIVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W PIC 99.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE W = 7 * 3 / 3.
           DISPLAY "N=" W.
           GOBACK.
       END PROGRAM G2OPT01N.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2OPT01E.
       OPTIONS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W PIC 9.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE W = 9 / 3.
           DISPLAY "E=" W.
           GOBACK.
       END PROGRAM G2OPT01E.
       END PROGRAM G2OPT02.
