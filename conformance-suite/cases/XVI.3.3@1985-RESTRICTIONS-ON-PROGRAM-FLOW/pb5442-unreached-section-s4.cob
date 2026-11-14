      *>--- conformance-case
      *> id: pb5442-unreached-section-s4
      *> rules: [1985-SR-XVI.3.3.2-L2.1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5442 - the range of a PERFORM "includes all statements
      *> that are executed as the result of a transfer of control by
      *> CALL, EXIT, GO TO, and PERFORM statements in the range"
      *> (§1985:VI.6.21.4 GR11; cite.py OK). A-1 goes to C-1 over B-SEC,
      *> so no execution of PERFORM A-SEC THRU C-SEC reaches B-SEC: the
      *> executed range is A and C only. It used to be refused COBOL6877,
      *> the physical span asked. Output: A, C, DONE.
      *> S4: from independent segment 70; A and C in 10, B in 70.
      *> SR L2.1: wholly in one or more non-independent segments.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. s4.
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       M-1.
           PERFORM X-SEC.
           DISPLAY "DONE".
           STOP RUN.
       X-SEC SECTION 70.
       X-1.
           PERFORM A-SEC THRU C-SEC.
       X-2.
           EXIT.
       A-SEC SECTION 10.
       A-1. DISPLAY "A".
           GO TO C-1.
       B-SEC SECTION 70.
       B-1. DISPLAY "B".
       C-SEC SECTION 10.
       C-1. DISPLAY "C".
