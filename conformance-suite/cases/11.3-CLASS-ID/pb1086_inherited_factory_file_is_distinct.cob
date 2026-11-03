      *>--- conformance-case
      *> id: pb1086_inherited_factory_file_is_distinct
      *> rules: [GR-11.3.4-2]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1086. ISO 1989:2023 11.3.4 general rule 2: the
      *> INHERITS clause specifies the classes inherited "according to
      *> 9.3.9, Class inheritance" (cite.py --check 11.3.4 -> OK 2)).
      *> 9.3.9: "Each factory object has its own copy of inherited
      *> data, distinct from the copy belonging to a factory object of
      *> an inherited class" and "The subclass inherits all the file
      *> definitions in the same way as the data definitions"
      *> (cite.py --check 9.3.9 -> OK, both).
      *>
      *> DERIVATION. The factory of PB1086GB describes the file FF,
      *> assigned to the name in its factory item FNAME, and the
      *> factory methods SETN, OPN, WRT, CLS and RDB. The class
      *> PB1086GS inherits it and defines nothing, so its factory has
      *> its own copy of FNAME and its own file connector FF. The
      *> program names a different file for each factory, opens both
      *> (neither connector is open, so each OPEN OUTPUT succeeds:
      *> status 00 twice), writes one record to each, closes both
      *> (00 twice: each connector was open) and reads each file back,
      *> so each factory finds only the record it wrote. Were the
      *> connector shared, the second OPEN would find it already open.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1086GD.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1086GB
           CLASS PB1086GS.
       PROCEDURE DIVISION.
       MAIN-PARA.
           INVOKE PB1086GB "SETN" USING "pb1086gb.dat"
           INVOKE PB1086GS "SETN" USING "pb1086gs.dat"
           INVOKE PB1086GB "OPN"
           INVOKE PB1086GS "OPN"
           INVOKE PB1086GB "WRT" USING "BASE-REC"
           INVOKE PB1086GS "WRT" USING "SUB-REC"
           INVOKE PB1086GB "CLS"
           INVOKE PB1086GS "CLS"
           INVOKE PB1086GB "RDB"
           INVOKE PB1086GS "RDB"
           STOP RUN.
       END PROGRAM PB1086GD.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1086GB INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       FACTORY.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FF ASSIGN USING FNAME
               ORGANIZATION IS LINE SEQUENTIAL
               FILE STATUS IS FFS.
       DATA DIVISION.
       FILE SECTION.
       FD FF.
       01 FREC PIC X(10).
       WORKING-STORAGE SECTION.
       01 FNAME PIC X(40).
       01 FFS PIC XX.
       PROCEDURE DIVISION.
       METHOD-ID. SETN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 NM PIC X(12).
       PROCEDURE DIVISION USING NM.
           MOVE NM TO FNAME.
       END METHOD SETN.
       METHOD-ID. OPN.
       PROCEDURE DIVISION.
           OPEN OUTPUT FF
           DISPLAY "OPEN " FFS.
       END METHOD OPN.
       METHOD-ID. WRT.
       DATA DIVISION.
       LINKAGE SECTION.
       01 TXT PIC X(10).
       PROCEDURE DIVISION USING TXT.
           MOVE TXT TO FREC
           WRITE FREC
           DISPLAY "WRITE " FFS.
       END METHOD WRT.
       METHOD-ID. CLS.
       PROCEDURE DIVISION.
           CLOSE FF
           DISPLAY "CLOSE " FFS.
       END METHOD CLS.
       METHOD-ID. RDB.
       PROCEDURE DIVISION.
           OPEN INPUT FF
           READ FF
           DISPLAY "READ " FFS " " FREC
           CLOSE FF.
       END METHOD RDB.
       END FACTORY.
       END CLASS PB1086GB.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1086GS INHERITS FROM PB1086GB.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1086GB.
       END CLASS PB1086GS.
