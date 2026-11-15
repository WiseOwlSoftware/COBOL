*>--- conformance-case
*> id: pb5384-perform-times-refmod
*> rules: [SR-5.5-2, SR-14.9.28.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §2023:14.9.28.3 SR2: "Identifier-1 shall be an integer." N2 (1:1) is no integer data item: the
      *> unique data item a reference modification of a numeric item creates is considered "class and category
      *> alphanumeric" (§2023:8.4.3.3.4 GR6), and an integer is "a fixed-point numeric data item" (§2023:5.5 SR2b.2).
      *> kb/Work PB5384: the one integer classifier read the base item's PICTURE through the reference
      *> modification, so this was ACCEPTED and iterated the body twice (the first digit of 23).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5384H.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N2 PIC 99 VALUE 23.
       01 CNT PIC 99 VALUE 0.
       PROCEDURE DIVISION.
           PERFORM COUNT-IT N2 (1:1) TIMES.
           STOP RUN.
       COUNT-IT.
           ADD 1 TO CNT.
