*>--- conformance-case
*> id: pb5706-prototype-special-names-alphabet-2023
*> rules: [SR-10.6.2-4]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5706. A program prototype's SPECIAL-NAMES paragraph may
      *> hold "only" the clauses §2002:10.5.2 SR5c and §2014:10.6.2 SR4c
      *> list: LOCALE, CURRENCY and DECIMAL-POINT. §2023:10.6.2 SR4c adds
      *> the ALPHABET and SYMBOLIC-CHARACTERS clauses. So the ALPHABET clause
      *> below is refused at 2002 and 2014 and legal at 2023, where the
      *> run unit's main displays OK (the prototype itself runs nothing).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PRAL IS PROTOTYPE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET AL IS NATIVE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 P1 PIC X.
       PROCEDURE DIVISION USING P1.
       END PROGRAM PRAL.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PRALM.
       PROCEDURE DIVISION.
           DISPLAY "OK".
           STOP RUN.
       END PROGRAM PRALM.
