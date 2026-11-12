      *>--- conformance-case
      *> id: pb5422-replace-library-text-after-period
      *> rules: [1985-SR-XII.3.3-1]
      *> files: [PB5422LB.cpy]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.3.3 SR1 (cite.py --check: OK): a REPLACE statement "must be
      *> preceded by a separator period except when it is the first statement in
      *> a separately compiled program". PB5422LB begins with a REPLACE, and the
      *> COPY statement that brings it follows the separator period after
      *> DISPLAY "X", so the REPLACE follows that period too. The second
      *> REPLACE stands on a debugging line after the period that ends
      *> DISPLAY "Y", and the line's indicator is no text between them. Expected output from XII.3.4 GR1: X, LIB, Y, then BB
      *> replaced by "DBG" (the program declares WITH DEBUGGING MODE). The
      *> refused twin is pb5422-replace-library-text-not-after-period
      *> (kb/Work PB5422).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5422A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. COMP1 WITH DEBUGGING MODE.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "X". COPY PB5422LB.
           DISPLAY AAA.
           DISPLAY "Y".
      D    REPLACE ==BB== BY =="DBG"==.
           DISPLAY BB.
           STOP RUN.
