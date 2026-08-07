      *> reject-at: 2002 2014 2023
      *> ISO 14.9.4.3 SR16: "Program-prototype-name-1 shall be specified in a
      *> program-specifier in the REPOSITORY paragraph." 12.3.8.2's
      *> program-specifier is `PROGRAM program-prototype-name-1 [AS literal-3]`,
      *> and repositoryEntry has no such alternative - so no source can declare
      *> one and this arm is genuinely blocked on the P13 prototype registry.
      *>
      *> It is pinned as a NAMED diagnostic rather than left to fail as an
      *> unresolved call: the other arm of the same brace (AS NESTED) is
      *> supported, so a reader needs to be told which half is missing and why.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB46ASPROTO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC S9(4) VALUE 5.
       PROCEDURE DIVISION.
       MAIN.
           CALL "SUB" AS MY-PROTO USING BY CONTENT N.
           STOP RUN.
