      *>--- conformance-case
      *> id: 1985-GR-IV_4_3_1_2-p5
      *> rules: [1985-GR-IV.4.3.1.2-p5, 1985-GR-IV.4.3.2-p1, 1985-GR-IV.4.3.2-p2, 1985-GR-IV.4.3.2-p3, 1985-SR-IV.4.3.1.3-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.1.2 p5; §1985:IV.4.3.2 p1; §1985:IV.4.3.2 p2;
      *> §1985:IV.4.3.2 p3; §1985:IV.4.3.1.3 p1 (Logical Record
      *> Concept). An output test: the program must translate and run at
      *> 85 and DISPLAY exactly the lines its .out holds, the outcome
      *> the rule requires. Placed from written and run by the PB5453
      *> reader of lineage-reads-1985/s-04.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * 1985-GR-IV.4.3.1.2-p5, 1985-SR-IV.4.3.1.3-p1,
      * 1985-GR-IV.4.3.2-p1/p2/p3: working storage is grouped into
      * logical records of data description entries (level-number,
      * data-name if required, independent clauses); subdivisions
      * are further subdivided; elementary items belong to several
      * groups; a record may itself be elementary.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. WSREC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  REC-ELEM          PIC X(3) VALUE "ELM".
       01  REC.
           05  PART-A.
               10  A1        VALUE "1" PIC X.
               10            PIC X VALUE "-".
               10  A2.
                   15  A21   PIC X VALUE "2".
                   15  A22   VALUE 3 PIC 9.
           05  PART-B        PIC XX VALUE "BB".
           05  FILLER        PIC X VALUE "!".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "REC-ELEM=" REC-ELEM.
           DISPLAY "REC=" REC.
           DISPLAY "PART-A=" PART-A.
           DISPLAY "A2=" A2.
           MOVE "XY" TO A2.
           DISPLAY "AFTER A2 MOVE REC=" REC " PART-A=" PART-A.
           IF A21 = "X" DISPLAY "A21 IN A2 PART-A REC OK".
           STOP RUN.
