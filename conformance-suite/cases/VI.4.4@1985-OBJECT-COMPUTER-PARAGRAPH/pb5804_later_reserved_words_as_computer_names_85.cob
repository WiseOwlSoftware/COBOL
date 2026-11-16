      *>--- conformance-case
      *> id: pb5804_later_reserved_words_as_computer_names_85
      *> rules: [1985-SR-VI.4.3.3-1, 1985-SR-VI.4.4.3-1,
      *>     1985-SR-XVI.2.2.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5804. "Computer-name is a system-name."
      *> (§1985:VI.4.3.3 SR1, §1985:VI.4.4.3 SR1,
      *> §1985:XVI.2.2.3 SR1), and §1985:IV.4.2.2.1.2: "A system-name is a COBOL word which
      *> is used to communicate with the operating environment". A
      *> reserved word "must not appear in the programs as user-defined
      *> words or system-names" (§1985:IV.4.2.2.1.3), and CLASS-ID and
      *> FUNCTION-ID are reserved from COBOL-2002, not by 1985, so at
      *> 85 each is a legal computer-name. The OBJECT-COMPUTER entry
      *> goes on to the Segmentation clause SEGMENT-LIMIT, so the name
      *> must be read as the name and the clause as its clause.
      *> A leg fails if either paragraph ends at the word (it used to
      *> open a class or function definition, COBOL5506), or if the
      *> program does not run and print DONE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5804A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. CLASS-ID.
       OBJECT-COMPUTER. FUNCTION-ID
           SEGMENT-LIMIT IS 10.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "DONE".
           STOP RUN.
