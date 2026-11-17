      *>--- conformance-case
      *> id: pb5795-computer-name-is-implementor-name-85
      *> rules: [1985-SR-VI.4.3.3-1, 1985-SR-VI.4.4.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5795. "Computer-name is a system-name."
      *> (§1985:VI.4.3.3 SR1, §1985:VI.4.4.3 SR1), and of the three
      *> types of system-names, computer-name, implementor-name and
      *> language-name, §1985:IV.4.2.2.1.2 says: "Within a given
      *> implementation these three types of system-names form disjoint
      *> sets; a given system-name may belong to one and only one of
      *> them." CONSOLE is this implementation's device-name and EBCDIC
      *> its ALPHABET code-name (DOC-A.1-198, items 183 and 189), both
      *> implementor-names, so neither is a computer-name at 85 and each
      *> paragraph is refused with COBOL7624 at its name; SPECIAL-NAMES
      *> uses CONSOLE as the implementor-name it is. From 2002 the same
      *> words are computer-names
      *> (pb5795_computer_name_spelled_as_device_name).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5795R.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. CONSOLE.
       OBJECT-COMPUTER. EBCDIC.
       SPECIAL-NAMES. CONSOLE IS MYCON.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "RAN" UPON MYCON.
           STOP RUN.
