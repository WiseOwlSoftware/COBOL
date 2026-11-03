      *>--- conformance-case
      *> id: pb1086_inherited_factory_data_is_distinct
      *> rules: [GR-11.3.4-2]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1086. ISO 1989:2023 11.3.4 general rule 2: "The
      *> INHERITS clause specifies the names of classes that are
      *> inherited by object-class-name-1 according to 9.3.9, Class
      *> inheritance" (cite.py --check 11.3.4 -> OK 2)). 9.3.9: "The
      *> inherited data definitions define data for every instance
      *> object of the subclass and for its factory object. ... Each
      *> factory object has its own copy of inherited data, distinct
      *> from the copy belonging to a factory object of an inherited
      *> class ... The inherited factory data is allocated
      *> independently from the factory data of the inherited class or
      *> classes and is initialized when the factory of the subclass
      *> is created" (cite.py --check 9.3.9 -> OK).
      *>
      *> DERIVATION. The factory of PB1086FB describes CNT PIC 9(3)
      *> VALUE 100 and the factory method BUMP that adds 1 to it. The
      *> class PB1086FS inherits it and defines nothing of its own, so
      *> its factory has its own CNT, initialized to 100 and changed
      *> only by the BUMPs invoked on PB1086FS. Two BUMPs on PB1086FB
      *> leave its CNT 102; one BUMP on PB1086FS leaves the subclass's
      *> CNT 101, and PB1086FB's stays 102.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1086FD.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1086FB
           CLASS PB1086FS.
       PROCEDURE DIVISION.
       MAIN-PARA.
           INVOKE PB1086FB "BUMP"
           INVOKE PB1086FB "BUMP"
           INVOKE PB1086FS "BUMP"
           INVOKE PB1086FB "SHOW"
           INVOKE PB1086FS "SHOW"
           STOP RUN.
       END PROGRAM PB1086FD.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1086FB INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       FACTORY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CNT PIC 9(3) VALUE 100.
       PROCEDURE DIVISION.
       METHOD-ID. BUMP.
       PROCEDURE DIVISION.
           ADD 1 TO CNT.
       END METHOD BUMP.
       METHOD-ID. SHOW.
       PROCEDURE DIVISION.
           DISPLAY "CNT=" CNT.
       END METHOD SHOW.
       END FACTORY.
       END CLASS PB1086FB.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1086FS INHERITS FROM PB1086FB.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1086FB.
       END CLASS PB1086FS.
