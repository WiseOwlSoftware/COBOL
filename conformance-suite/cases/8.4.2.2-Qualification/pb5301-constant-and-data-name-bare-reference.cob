      *>--- conformance-case
      *> id: pb5301-constant-and-data-name-bare-reference
      *> rules: [SR-8.4.2.2.1-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5301 - §2023:8.4.2.2.1 (cite.py --check OK): "Qualification of a
      *> user-defined name is required unless one of the following is true: 1) No
      *> other name has the identical spelling." The constant KK and the data
      *> item KK share a spelling (allowed by §2023:8.3.2.2 exception 3,
      *> §2002:8.3.1.1.1), a constant-name cannot be qualified, and DISPLAY
      *> accepts an identifier or a literal, so the bare KK does not uniquely
      *> identify its item: §2023:8.4.2.2.3 SR1, COBOL1639, at 2002, 2014 and
      *> 2023 (constants exist from 2002). The program used to display the
      *> constant's 5. The declarations alone are legal
      *> (pb5301_constant_and_data_name_one_spelling).
      *> Fails if: the program compiles.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5301BR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  KK CONSTANT GLOBAL AS 5.
       01  KK PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       P1.
           DISPLAY KK.
           STOP RUN.
