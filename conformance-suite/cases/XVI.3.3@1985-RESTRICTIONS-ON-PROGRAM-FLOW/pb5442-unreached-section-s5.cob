      *>--- conformance-case
      *> id: pb5442-unreached-section-s5
      *> rules: [1985-SR-XVI.3.3.2-2]
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
      *> S5: from segment 5; A and C in independent 60, B in 10.
      *> SR2: wholly in a single independent segment.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. s5.
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       M-1.
           PERFORM X-SEC.
           DISPLAY "DONE".
           STOP RUN.
       X-SEC SECTION 05.
       X-1.
           PERFORM A-SEC THRU C-SEC.
       X-2.
           EXIT.
       A-SEC SECTION 60.
       A-1. DISPLAY "A".
           GO TO C-1.
       B-SEC SECTION 10.
       B-1. DISPLAY "B".
       C-SEC SECTION 60.
       C-1. DISPLAY "C".
