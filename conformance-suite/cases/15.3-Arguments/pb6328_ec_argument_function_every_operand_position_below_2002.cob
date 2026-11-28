*>--- conformance-case
*> id: pb6328_ec_argument_function_every_operand_position_below_2002
*> rules: [AR-15.3-14]
*> source-format: free
*> portable: unverified
*>---
      *> The edition gate under conformance:cases/15.3-Arguments/pb6328_ec_argument_function_every_operand_position.
      *> §2023:15.3 AR14's EC-ARGUMENT-FUNCTION is only OBSERVABLE where checking can be enabled, and the §2023:7.3
      *> compiler-directive facility - >>TURN (§2023:7.3.25) - is a COBOL-2002 introduction, so the same program (here
      *> its CANCEL position alone) is refused at 1985. The raise itself is edition-independent from 2002 on; this
      *> fixture exists so the 2002 floor of the checking half cannot drift unnoticed.
       >>TURN EC-ARGUMENT-FUNCTION CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6328OPSGATE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-Z PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           CANCEL FUNCTION CHAR(W-Z).
           STOP RUN.
