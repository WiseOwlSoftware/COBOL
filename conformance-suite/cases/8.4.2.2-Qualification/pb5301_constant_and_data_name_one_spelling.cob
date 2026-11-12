      *>--- conformance-case
      *> id: pb5301_constant_and_data_name_one_spelling
      *> rules: [SR-8.4.2.2.1-1]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5301 - the legal side. §2023:8.3.2.2 exception 3 (cite.py --check
      *> OK; §2002:8.3.1.1.1 is the same text): "the same name may be used as any of
      *> the following types of user-defined words: constant-name, data-name,
      *> property-name, record-key-name, record-name", so the constant KK and the
      *> data item KK of G may share a spelling. A constant-name takes no
      *> qualifier, so KK OF G names the data item and only it: §2023:8.4.2.2.1
      *> establishes uniqueness "through qualification", and a program that
      *> never writes the bare KK is legal.
      *> DERIVATION: KK OF G is the data item, whose value is 1, not the
      *> constant 5 (the bare KK would be ambiguous, see
      *> pb5301-constant-and-data-name-bare-reference).
      *> Fails if: the declarations are refused, or KK OF G reads the constant.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5301OK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  KK CONSTANT AS 5.
       01  G.
           05  KK PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       P1.
           DISPLAY KK OF G.
           STOP RUN.
