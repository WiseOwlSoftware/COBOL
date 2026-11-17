      *>--- conformance-case
      *> id: pb5870_renames_same_word_two_items_85
      *> rules: [1985-SR-VI.5.11.3-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5870, item 2. §1985:VI.5.11.3 SR4: data-name-2 and
      *> data-name-3 "cannot be the same data-name". The determination
      *> D-85-RENAMES-SAME-NAME (docs/conformance/3/): the same
      *> data-name is the same item, not the same word, as GnuCOBOL
      *> compares the two resolved items, so A OF G1 THRU A OF G2 names
      *> two items and compiles. §1985:VI.5.11.4 GR1: R-QQ "includes all
      *> elementary items starting with data-name-2" and concluding with
      *> data-name-3: A of G1 (AB), B (CD) and A of G2 (EF), so the
      *> line is QQ=ABCDEF. A compiler that compared the words
      *> would refuse the entry and print nothing.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5870REN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W.
           05  G1.
               10  A PIC X(2) VALUE "AB".
               10  B PIC X(2) VALUE "CD".
           05  G2.
               10  A PIC X(2) VALUE "EF".
               10  C PIC X(2) VALUE "GH".
       66  R-QQ RENAMES A OF G1 THRU A OF G2.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "QQ=" R-QQ.
           STOP RUN.
