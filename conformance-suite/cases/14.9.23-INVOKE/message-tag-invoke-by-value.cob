*>--- conformance-case
*> id: message-tag-invoke-by-value
*> rules: [SR-14.9.23.3-15, SR-14.2.2-2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4740 review R1-5 - 14.9.23.3 SR15: "If identifier-5 or its corresponding formal parameter is specified
      *> with the BY VALUE phrase, identifier-5 shall be of class message-tag, numeric, object or pointer." (cite.py --check
      *> 14.9.23.3 OK, SR 15.) 14.2.2 SR2 admits the same class as a BY VALUE formal (cite.py --check 14.2.2 OK, SR 2). The
      *> method TAKE declares a message-tag formal BY VALUE and the INVOKE passes a message tag BY VALUE: the formal is a
      *> detached copy (14.2.3 GR10), here the NULL content, so the callee sees NULL and its SET of the copy changes nothing
      *> for the caller.
      *> Derived: CALLEE NULL, then BACK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MTINVMAIN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS MTINVC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A USAGE OBJECT REFERENCE MTINVC.
       01 W-T USAGE MESSAGE-TAG.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE MTINVC "NEW" RETURNING A.
           INVOKE A "TAKE" USING BY VALUE W-T.
           DISPLAY "BACK".
           STOP RUN.
       END PROGRAM MTINVMAIN.

       IDENTIFICATION DIVISION.
       CLASS-ID. MTINVC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. TAKE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-T USAGE MESSAGE-TAG.
       PROCEDURE DIVISION USING BY VALUE L-T.
       MAIN.
           IF L-T = NULL
              DISPLAY "CALLEE NULL"
           END-IF
           SET L-T TO NULL
           GOBACK.
       END METHOD TAKE.
       END OBJECT.
       END CLASS MTINVC.
