*>--- conformance-case
*> id: pb4464_method_options_float_binary
*> rules: [SR-11.9.8.3-2, SR-11.9.8.3-3, GR-11.9.4-1]
*> accept: [2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB4464 - a METHOD's own OPTIONS FLOAT-BINARY clause governs the data items of that method's source element.
      *> cite.py --check 11.9.4 "The clauses in the OPTIONS paragraph apply to the source element in which they are
      *>   specified and to all source elements contained in that source element unless overridden by a clause in an
      *>   OPTIONS paragraph in a contained source element" -> OK  11.9.4 1)
      *> cite.py --check 11.9.8.3 "When the HIGH-ORDER-RIGHT phrase is specified" -> OK  11.9.8.3 3): the phrase is implied
      *>   "for the USAGE clause in the data description entry of any data item described with a standard binary
      *>   floating-point usage in which an endianness-phrase is not specified".
      *> cite.py --check 11.9.8.3 "When the HIGH-ORDER-LEFT phrase is specified" -> the SR2 twin.
      *> cite.py --check 13.18.60.4 "The HIGH-ORDER-RIGHT phrase specifies that the endianness of the data item is
      *>   little-endian" -> OK  13.18.60.4 19) b); a) is the big-endian HIGH-ORDER-LEFT.
      *> A byte order is observable only through the character image, and a round trip cannot see it (both lanes would
      *> reverse), so each leg moves the item's group to an alphanumeric item and reads single bytes with FUNCTION ORD
      *> (the ordinal position, natively byte + 1). Binary32 1.5 is X"3FC00000" (ISO/IEC 60559:2020): big-endian the
      *> bytes are 3F C0 00 00 and the first and fourth read 64 and 1; little-endian they are 00 00 C0 3F and read 1 and 64.
      *> Every clause below is written, so no leg rests on the implementor's choice of 11.9.8.3 SR1.
      *> Class RCLS writes FLOAT-BINARY DEFAULT IS HIGH-ORDER-RIGHT at class level:
      *>   RCLS.R1 has no OPTIONS paragraph: it takes the class's clause (11.9.4 GR1) -> little-endian 1 64.
      *>   RCLS.R2 has its own FLOAT-BINARY DEFAULT IS HIGH-ORDER-LEFT, which overrides the class's -> big-endian 64 1.
      *>   RCLS.R3 has its own OPTIONS paragraph without a FLOAT-BINARY clause: the class's clause is not overridden -> 1 64.
      *>   RCLS.R2 reads the object's data item, declared in the class's element and not the method's: it keeps the
      *>     class's clause whatever method reads it -> 1 64. R2's item B32R wrote HIGH-ORDER-RIGHT and keeps its own
      *>     phrase under the method's HIGH-ORDER-LEFT (the clause is implied only where no phrase is specified) -> 1 64.
      *> Class LCLS has no class-level clause; LCLS.L1 writes HIGH-ORDER-RIGHT in its own OPTIONS paragraph -> 1 64
      *>   (a class half that has none reaches 64 1 by the implementor's default, so the method's clause is the only
      *>   thing that can make this leg read 1 64).
       IDENTIFICATION DIVISION.
       CLASS-ID. RCLS INHERITS FROM BASE.
       OPTIONS.
           FLOAT-BINARY DEFAULT IS HIGH-ORDER-RIGHT.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OG.
          05 OB32 USAGE FLOAT-BINARY-32 VALUE 1.5.
       PROCEDURE DIVISION.
       METHOD-ID. R1.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 GH.
          05 B32 USAGE FLOAT-BINARY-32 VALUE 1.5.
       01 XH PIC X(4).
       PROCEDURE DIVISION.
           MOVE GH TO XH
           DISPLAY "R1=[" FUNCTION ORD(XH(1:1)) " " FUNCTION ORD(XH(4:1)) "]".
       END METHOD R1.
       METHOD-ID. R2.
       OPTIONS.
           FLOAT-BINARY DEFAULT IS HIGH-ORDER-LEFT.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 GH.
          05 B32 USAGE FLOAT-BINARY-32 VALUE 1.5.
          05 B32R USAGE FLOAT-BINARY-32 HIGH-ORDER-RIGHT VALUE 1.5.
       01 XH PIC X(8).
       01 XO PIC X(4).
       PROCEDURE DIVISION.
           MOVE GH TO XH
           DISPLAY "R2=[" FUNCTION ORD(XH(1:1)) " " FUNCTION ORD(XH(4:1)) "]".
           DISPLAY "R2R=[" FUNCTION ORD(XH(5:1)) " " FUNCTION ORD(XH(8:1)) "]".
           MOVE OG TO XO
           DISPLAY "R2O=[" FUNCTION ORD(XO(1:1)) " " FUNCTION ORD(XO(4:1)) "]".
       END METHOD R2.
       METHOD-ID. R3.
       OPTIONS.
           DEFAULT ROUNDED MODE IS TRUNCATION.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 GH.
          05 B32 USAGE FLOAT-BINARY-32 VALUE 1.5.
       01 XH PIC X(4).
       PROCEDURE DIVISION.
           MOVE GH TO XH
           DISPLAY "R3=[" FUNCTION ORD(XH(1:1)) " " FUNCTION ORD(XH(4:1)) "]".
       END METHOD R3.
       END OBJECT.
       END CLASS RCLS.
       IDENTIFICATION DIVISION.
       CLASS-ID. LCLS INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. L1.
       OPTIONS.
           FLOAT-BINARY DEFAULT IS HIGH-ORDER-RIGHT.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 GH.
          05 B32 USAGE FLOAT-BINARY-32 VALUE 1.5.
       01 XH PIC X(4).
       PROCEDURE DIVISION.
           MOVE GH TO XH
           DISPLAY "L1=[" FUNCTION ORD(XH(1:1)) " " FUNCTION ORD(XH(4:1)) "]".
       END METHOD L1.
       END OBJECT.
       END CLASS LCLS.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4464.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS RCLS
           CLASS LCLS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O1 USAGE OBJECT REFERENCE RCLS.
       01 O2 USAGE OBJECT REFERENCE LCLS.
       PROCEDURE DIVISION.
           INVOKE RCLS "NEW" RETURNING O1
           INVOKE O1 "R1"
           INVOKE O1 "R2"
           INVOKE O1 "R3"
           INVOKE LCLS "NEW" RETURNING O2
           INVOKE O2 "L1"
           STOP RUN.
