      *>--- conformance-case
      *> id: pb3139_strong_vlg_pointer_call
      *> rules: [SR-14.8.2.2-2, GR-14.9.25.4-9]
      *> accept: [2014, 2023]
      *> source-format: fixed
      *>---
      *> kb/Work PB3139 - a STRONGLY-TYPED VARIABLE-LENGTH group whose
      *> members include POINTER leaves (one in a dynamic-capacity
      *> table's element) crosses a program activation BY REFERENCE, BY
      *> CONTENT and as a RETURNING item, is a user-defined function's
      *> result, and is moved whole.
      *> cite.py --check 14.8.2.2 "If either the formal parameter or the
      *>   corresponding argument is a strongly-typed group item, both
      *>   shall be of the same type" -> OK 14.8.2.2 2)
      *> cite.py --check 13.18.60.3 "subordinate to a type declaration
      *>   that includes the STRONG phrase" -> OK 13.18.60.3 14)
      *> cite.py --check 8.5.1.12.1 "a group item whose data description
      *>   has at least one dynamic-length elementary item or
      *>   dynamic-capacity table as a subordinate item" -> OK
      *> cite.py --check 14.2.3 "operates as if the formal parameter
      *>   occupies the same storage area as the argument" -> OK
      *>   14.2.3 8)
      *> cite.py --check 14.2.3 "allocated by the activating runtime
      *>   element" -> OK 14.2.3 9)
      *> cite.py --check 14.6.5 "the content of the data item referenced
      *>   by that RETURNING phrase" -> OK 14.6.5
      *> cite.py --check 14.9.25.3 "be described as a group item of the
      *>   same type" -> OK 14.9.25.3 2)
      *> cite.py --check 14.6.9.2 "The operation recreates or overwrites
      *>   the receiving table with a copy of the sending table" -> OK
      *> Every pair is of the one type TG, so the source conforms;
      *> before the fix the CALL drew COBOL1688 (and the prototype
      *> against its definition COBOL1513), and the MOVE was a
      *> run-time abort.
      *> DERIVATION (G holds TA "hello", TP -> X "DATA", TN 05, two
      *> occurrences of TT: "ab", and "cd" whose TT-P -> W "WWWW"):
      *>   SUB  the callee sees all of it: SUB [hello] 05 2 cd WWWW.
      *>   COPY MOVE LK-G TO C (same type, GR9 a)): C gets the content,
      *>        both occurrences and the reference: COPY 2 ab WWWW.
      *>   CON  BY CONTENT the callee's record is a copy (GR9): its MOVE
      *>        "world!", ADD 1 and its reference to TT-X (3), which
      *>        creates a third occurrence (8.5.1.9.3), reach only the
      *>        copy: CON [hello] 05 2 abcd.
      *>   RET  14.6.5 places LK-R's content in R: C's content (taken
      *>        before the ADD), TA "ret-val" and TT-P (1) -> Y "YYYY":
      *>        RET [ret-val] 05 2 ab YYYY.
      *>   REF  BY REFERENCE the callee's stores are G's (GR8):
      *>        REF [world!] 06 3 abcdef.
      *>   FUN  MOVE of the function's result (same type): its TA "fun",
      *>        one occurrence "zz" whose TT-P -> the function's own
      *>        "FFFF": FUN [fun] 1 zz FFFF.
      *> Dynamic-length items and dynamic-capacity tables: COBOL 2014.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3139S IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 TG IS TYPEDEF STRONG.
          05 TA PIC X DYNAMIC LENGTH LIMIT IS 10.
          05 TP USAGE POINTER.
          05 TT OCCURS DYNAMIC CAPACITY IN TC FROM 1 TO 5.
             10 TT-X PIC X(2).
             10 TT-P USAGE POINTER.
          05 TN PIC 9(2).
       01 LK-G TYPE TG.
       01 LK-R TYPE TG.
       PROCEDURE DIVISION USING LK-G RETURNING LK-R.
       END PROGRAM P3139S.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. P3139F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TG IS TYPEDEF STRONG.
          05 TA PIC X DYNAMIC LENGTH LIMIT IS 10.
          05 TP USAGE POINTER.
          05 TT OCCURS DYNAMIC CAPACITY IN TC FROM 1 TO 5.
             10 TT-X PIC X(2).
             10 TT-P USAGE POINTER.
          05 TN PIC 9(2).
       01 F-Z PIC X(4) VALUE "FFFF".
       LINKAGE SECTION.
       01 LF TYPE TG.
       PROCEDURE DIVISION RETURNING LF.
           MOVE "fun" TO TA OF LF
           MOVE "zz" TO TT-X OF LF (1)
           SET TT-P OF LF (1) TO ADDRESS OF F-Z
           GOBACK.
       END FUNCTION P3139F.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3139A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM P3139S
           FUNCTION P3139F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TG IS TYPEDEF STRONG.
          05 TA PIC X DYNAMIC LENGTH LIMIT IS 10.
          05 TP USAGE POINTER.
          05 TT OCCURS DYNAMIC CAPACITY IN TC FROM 1 TO 5.
             10 TT-X PIC X(2).
             10 TT-P USAGE POINTER.
          05 TN PIC 9(2).
       01 G TYPE TG.
       01 R TYPE TG.
       01 F2 TYPE TG.
       01 X PIC X(4) VALUE "DATA".
       01 W PIC X(4) VALUE "WWWW".
       01 B PIC X(4) BASED.
       01 K PIC Z.
       01 D PIC X(2).
       PROCEDURE DIVISION.
           MOVE "hello" TO TA OF G
           SET TP OF G TO ADDRESS OF X
           MOVE 5 TO TN OF G
           MOVE "ab" TO TT-X OF G (1)
           MOVE "cd" TO TT-X OF G (2)
           SET TT-P OF G (2) TO ADDRESS OF W
           CALL P3139S USING BY CONTENT G RETURNING R
           MOVE TC OF G TO K
           MOVE TN OF G TO D
           DISPLAY "CON [" TA OF G "] " D " " K " "
               TT-X OF G (1) TT-X OF G (2)
           MOVE TC OF R TO K
           MOVE TN OF R TO D
           SET ADDRESS OF B TO TT-P OF R (1)
           DISPLAY "RET [" TA OF R "] " D " " K " "
               TT-X OF R (1) " " B
           CALL P3139S USING BY REFERENCE G RETURNING R
           MOVE TC OF G TO K
           MOVE TN OF G TO D
           DISPLAY "REF [" TA OF G "] " D " " K " "
               TT-X OF G (1) TT-X OF G (2) TT-X OF G (3)
           MOVE FUNCTION P3139F TO F2
           MOVE TC OF F2 TO K
           SET ADDRESS OF B TO TT-P OF F2 (1)
           DISPLAY "FUN [" TA OF F2 "] " K " " TT-X OF F2 (1) " " B
           STOP RUN.
       END PROGRAM P3139A.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3139S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TG IS TYPEDEF STRONG.
          05 TA PIC X DYNAMIC LENGTH LIMIT IS 10.
          05 TP USAGE POINTER.
          05 TT OCCURS DYNAMIC CAPACITY IN TC FROM 1 TO 5.
             10 TT-X PIC X(2).
             10 TT-P USAGE POINTER.
          05 TN PIC 9(2).
       01 Y PIC X(4) VALUE "YYYY".
       01 C TYPE TG.
       01 K PIC Z.
       01 D PIC X(2).
       LINKAGE SECTION.
       01 LK-G TYPE TG.
       01 LK-R TYPE TG.
       01 B PIC X(4) BASED.
       PROCEDURE DIVISION USING LK-G RETURNING LK-R.
           SET ADDRESS OF B TO TT-P OF LK-G (2)
           MOVE TC OF LK-G TO K
           MOVE TN OF LK-G TO D
           DISPLAY "SUB [" TA OF LK-G "] " D " " K " "
               TT-X OF LK-G (2) " " B
           MOVE LK-G TO C
           MOVE "world!" TO TA OF LK-G
           ADD 1 TO TN OF LK-G
           MOVE "ef" TO TT-X OF LK-G (3)
           SET ADDRESS OF B TO TT-P OF C (2)
           MOVE TC OF C TO K
           DISPLAY "COPY " K " " TT-X OF C (1) " " B
           MOVE C TO LK-R
           MOVE "ret-val" TO TA OF LK-R
           SET TT-P OF LK-R (1) TO ADDRESS OF Y
           GOBACK.
       END PROGRAM P3139S.
