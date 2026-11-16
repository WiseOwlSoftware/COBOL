      *>--- conformance-case
      *> id: pb5804_interface_id_repository_as_computer_names_85
      *> rules: [1985-SR-VI.4.3.3-1, 1985-SR-VI.4.4.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5804. "Computer-name is a system-name."
      *> (§1985:VI.4.3.3 SR1, §1985:VI.4.4.3 SR1). INTERFACE-ID and
      *> REPOSITORY are reserved from COBOL-2002, not by 1985
      *> (§1985:IV.4.2.2.1.3 bars only a reserved word from a
      *> system-name), so at 85 each is a legal computer-name: the
      *> SOURCE-COMPUTER name is INTERFACE-ID and the OBJECT-COMPUTER
      *> name is REPOSITORY. A leg fails if either paragraph ends at the
      *> word (INTERFACE-ID opened an interface definition, COBOL5506;
      *> REPOSITORY opened the 2002 paragraph, COBOL0900), or if the
      *> program does not run and print DONE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5804B.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. INTERFACE-ID.
       OBJECT-COMPUTER. REPOSITORY.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "DONE".
           STOP RUN.
