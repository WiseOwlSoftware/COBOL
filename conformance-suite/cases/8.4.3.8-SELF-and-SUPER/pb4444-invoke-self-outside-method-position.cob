      *>--- conformance-case
      *> id: pb4444-invoke-self-outside-method-position
      *> rules: [SR-8.4.3.8.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §2023:8.4.3.8.3 SR1 - "This identifier format may be
      *> specified only in a method definition." SELF written as the
      *> receiver of an INVOKE in a PROGRAM is refused at every edition
      *> that has the format (COBOL-2002 introduced it), and kb/Work
      *> PB4444 pins WHERE: the refusal is about the word SELF, so it
      *> is reported at the word (column 19 of the statement below),
      *> not at the INVOKE keyword that opens the statement (column 12).
      *> A bare SELF has no qualifier node, which is what used to leave
      *> the position at the enclosing statement.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4444A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X.
       PROCEDURE DIVISION.
           INVOKE SELF "M".
           STOP RUN.
