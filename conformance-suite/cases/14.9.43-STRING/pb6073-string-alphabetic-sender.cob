      *>--- conformance-case
      *> id: pb6073-string-alphabetic-sender
      *> rules: [SR-14.9.43.3-1]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB6073 - §2023:14.9.43.3 SR1: "all identifiers, except
      *>   identifier-4, shall be described implicitly or explicitly as
      *>   usage display or national" (§2002:14.8.39.2 SR1 likewise).
      *>   The rule is about USAGE, not category: SR8 even admits an
      *>   elementary numeric integer sender. TV is PIC A, usage
      *>   display, so it is a legal sending operand at every edition
      *>   (UNSTRING's SR2 differs: it names the category, so a PIC A
      *>   sender is refused there from COBOL-2014 on, see
      *>   pb5615-alphabetic-operands-refused). STRING moves ABC to
      *>   OUT, whose remaining positions are unchanged.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6073S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TV  PIC A(3) VALUE "ABC".
       01  OUT PIC X(5) VALUE "-----".
       PROCEDURE DIVISION.
       MAIN-P.
           STRING TV DELIMITED BY SIZE INTO OUT
           DISPLAY OUT
           STOP RUN.
