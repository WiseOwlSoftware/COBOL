*>--- conformance-case
*> id: pb4463-symbolic-characters-in-undeclared-alphabet
*> rules: [GR-12.3.7.4-7]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §12.3.7.4 GR7 (cite.py --check 12.3.7.4 "An alphabet-name referenced in the class condition, in the CLASS
      *> clause in the data division, in a SYMBOLIC CHARACTERS clause, or in the CODE-SET clause of a file description
      *> entry references a coded character set" -> OK 7)): the alphabet-name of the IN phrase of a SYMBOLIC CHARACTERS
      *> clause references a coded character set, so it shall be an alphabet-name the ALPHABET clause declares.
      *> NOSUCH is declared by no ALPHABET clause, so the clause is refused (COBOL5985, kb/Work PB4463).
IDENTIFICATION DIVISION.
PROGRAM-ID. PB4463SY.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
SPECIAL-NAMES.
    SYMBOLIC CHARACTERS FOR ALPHANUMERIC SC1 ARE 1 IN NOSUCH.
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
