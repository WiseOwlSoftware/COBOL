      *>--- conformance-case
      *> id: g2-file-section-in-interface
      *> rules: [2002-SR-13.3.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.3.2 SR1 - "The file section shall not be specified
      *> within an interface definition." The method prototype SPEAK
      *> of interface G2FS03I (a method-definition in an
      *> interface-definition, §2002:10.5.1 NOTE) declares a FILE
      *> SECTION, so the case is refused at 2002 with COBOL1519 (the
      *> file-section placement check). §2002:10.5.2 SR5 e) ("The data
      *> division may contain only a linkage section") refuses the same
      *> line as well (COBOL2272). Why it can fail: the FILE SECTION in
      *> the interface is accepted, or the placement code is missing.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2FS03.
       PROCEDURE DIVISION.
           DISPLAY "COMPILED"
           STOP RUN.
       END PROGRAM G2FS03.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. G2FS03I.
       PROCEDURE DIVISION.
       METHOD-ID. SPEAK.
       DATA DIVISION.
       FILE SECTION.
       PROCEDURE DIVISION.
       END METHOD SPEAK.
       END INTERFACE G2FS03I.
