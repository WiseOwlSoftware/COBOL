      *>--- conformance-case
      *> id: pb5804-class-id-computer-name-reserved-2002
      *> rules: [SR-8.3.2.1-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5804, the other edition of the rule. §2023:8.3.2.1
      *> SR1: "Reserved words shall not be used as user-defined words or
      *> system-names." A computer-name is a system-name, and
      *> §2023:8.9 reserves CLASS-ID from COBOL-2002, so OBJECT-COMPUTER. CLASS-ID.
      *> is refused there with COBOL0901 at the word, where the same
      *> program is legal COBOL-85
      *> (pb5804_later_reserved_words_as_computer_names_85). The empty
      *> OBJECT-COMPUTER paragraph followed by a class definition is no
      *> reading either: CLASS-ID. needs a class-name.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5804R.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. CLASS-ID.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "DONE".
           STOP RUN.
