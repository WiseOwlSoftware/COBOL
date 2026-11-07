*>--- conformance-case
*> id: pb4512_initialize_to_value_edited_receivers
*> rules: [GR-14.9.20.4-6]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4512EV.
      *> kb/Work PB4512 -- ISO 14.9.20.4 GR6 a) 3., at the introduction edition of the TO VALUE phrase (COBOL-2002).
      *>   cite.py: OK  §2023:14.9.20.4 6) a) 3.  (General rules)
      *>   "the sending-operand is determined by the literal in the VALUE clause specified in the data description
      *>   entry of the data item" and, as GR6 a) 3. words the result, it "produces the same result as the initial
      *>   value of the data item as produced by the application of the VALUE clause".
      *> The initial value of a VALUE clause is NOT edited, justified or blanked:
      *>   cite.py: OK  §2023:13.18.63.4 7)  (General rules)
      *>   "initialization is not affected by a JUSTIFIED clause and no editing takes place"
      *>   cite.py: OK  §2023:13.18.63.4 8)  (General rules)
      *>   "the BLANK WHEN ZERO clause has no effect on initialization" (an alphanumeric or national literal)
      *>   cite.py: OK  §2023:13.18.63.3 11)  (Syntax rules)
      *>   "Editing characters in a picture character-string for a numeric-edited data item are used in editing of the
      *>   initial value when ... the literal is numeric."
      *> So INITIALIZE ... ALL TO VALUE must leave each item holding exactly its initial value, and an edited
      *> receiver must not have its VALUE literal edited a second time: E1 PIC XXBXX VALUE "AB CD" is [AB CD]
      *> (a second edit would give [AB  C]), E2 PIC ZZ9.99 VALUE " 12.50" is [ 12.50] (a second edit [250.00]),
      *> E3 PIC X/XX VALUE "Q/RS" is [Q/RS] (a second edit [Q//R]).
      *> J1 JUSTIFIED RIGHT is not right-aligned by initialization: [AB   ].
      *> B1 BLANK WHEN ZERO with the alphanumeric "0000" keeps [0000]. E5 is a national-edited item. T(1..3) are
      *> the table-format arm (GR6 a) 3. "the literal ... that corresponds to the occurrence being initialized").
      *> Every item is overwritten with ALL "Z" first, so a store that never happens shows [Z...] and a
      *> store that edits shows the doubled edit. Each leg can fail: the START line proves the declaration value,
      *> the INIT line proves INITIALIZE restores the same one, and the SUBSET line proves the single-occurrence
      *> receiver (subscripted by a data item) takes its own occurrence's literal.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-GRP.
          05 E1 PIC XXBXX VALUE "AB CD".
          05 E2 PIC ZZ9.99 VALUE " 12.50".
          05 E3 PIC X/XX VALUE "Q/RS".
          05 J1 PIC X(5) JUSTIFIED RIGHT VALUE "AB".
          05 B1 PIC ZZZ9 BLANK WHEN ZERO VALUE "0000".
          05 E5 PIC NNBNN VALUE N"AB CD".
          05 T PIC XXBXX OCCURS 3
                VALUES ARE "AB CD" "EF GH" "IJ KL" FROM (1) TO (3).
       01 WS-I PIC 9 VALUE 2.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "START [" E1 "][" E2 "][" E3 "][" J1 "][" B1 "][" E5 "]"
           DISPLAY "START [" T(1) "][" T(2) "][" T(3) "]"
           MOVE ALL "Z" TO WS-GRP
           INITIALIZE WS-GRP ALL TO VALUE
           DISPLAY "INIT  [" E1 "][" E2 "][" E3 "][" J1 "][" B1 "][" E5 "]"
           DISPLAY "INIT  [" T(1) "][" T(2) "][" T(3) "]"
           MOVE ALL "Z" TO WS-GRP
           INITIALIZE T(WS-I) ALL TO VALUE
           DISPLAY "SUBSET [" T(1) "][" T(2) "][" T(3) "]"
           STOP RUN.
