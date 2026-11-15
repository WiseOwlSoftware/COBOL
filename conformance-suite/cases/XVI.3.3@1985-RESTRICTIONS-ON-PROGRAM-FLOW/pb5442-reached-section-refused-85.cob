      *>--- conformance-case
      *> id: pb5442-reached-section-refused-85
      *> rules: [1985-SR-XVI.3.3.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5442 - the control of pb5442-unreached-section-s1:
      *> A-1 goes to B-1, so the executed range of PERFORM A-SEC THRU
      *> C-SEC holds B-SEC (segment 10) beside A-SEC and C-SEC
      *> (independent segment 60): neither wholly non-independent
      *> (§1985:XVI.3.3.2 SR1) nor wholly one independent segment
      *> (SR2). Refused COBOL6877.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5442RR.
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       M-1.
           PERFORM A-SEC THRU C-SEC.
           DISPLAY "DONE".
           STOP RUN.
       A-SEC SECTION 60.
       A-1. DISPLAY "A".
           GO TO B-1.
       B-SEC SECTION 10.
       B-1. DISPLAY "B".
       C-SEC SECTION 60.
       C-1. DISPLAY "C".
