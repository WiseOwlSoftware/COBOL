      *>--- conformance-case
      *> id: g2-file-section-in-method
      *> rules: [2002-SR-13.3.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.3.2 SR1 - "Within a class definition, the file
      *> section may be specified only in a factory definition or an
      *> instance definition, but not in a method definition." Method
      *> M1 of class G2FS02C declares a FILE SECTION in its own data
      *> division, so the case is refused at 2002 (COBOL1519). The
      *> permitted placements are g2-file-section-placement. Why it can
      *> fail: the method's FILE SECTION is accepted, or another code
      *> is reported.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2FS02.
       PROCEDURE DIVISION.
           DISPLAY "COMPILED"
           STOP RUN.
       END PROGRAM G2FS02.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2FS02C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. M1.
       DATA DIVISION.
       FILE SECTION.
       PROCEDURE DIVISION.
           CONTINUE.
       END METHOD M1.
       END OBJECT.
       END CLASS G2FS02C.
