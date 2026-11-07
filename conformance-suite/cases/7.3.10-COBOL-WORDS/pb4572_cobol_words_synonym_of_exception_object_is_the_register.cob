*>--- conformance-case
*> id: pb4572_cobol_words_synonym_of_exception_object_is_the_register
*> rules: [GR-7.3.10.4-2, SR-8.4.3.6.3-2, GR-8.4.3.6.4-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 7.3.10.4 GR2 (kb/Work PB4572): "When the EQUATE option is specified, the COBOL word that is the
*> content of literal-2 shall be treated as a synonym for the COBOL word that is the content of literal-1, and may
*> be used in any syntax requiring the use of the reserved word, context-sensitive word, or intrinsic function name
*> that is the content of literal-1."
*>   cite.py: OK  §2023:7.3.10.4 2)  (General rules)
*> 8.4.3.6.3 SR2: "EXCEPTION-OBJECT is implicitly described as class object and category object reference, as an
*> external data item, and as a universal object reference."
*>   cite.py: OK  §2023:8.4.3.6.3 2)  (Syntax rules)
*> 8.4.3.6.4 GR1: "EXCEPTION-OBJECT references the current exception object. If an exception object is not
*> associated with the current exception, EXCEPTION-OBJECT is set to null."
*>   cite.py: OK  §2023:8.4.3.6.4 1)  (General rules)
*> EXCEPTION-OBJECT is a reserved word the lexer has no token for (the grammar reads it as an ordinary word), so the
*> directive reaches a synonym of it only by name. The compiler asked the written spelling and read EXO as an
*> undefined name (COBOL1639) AND as a reserved word used as a user-defined word (COBOL0901), though GR2 makes EXO the
*> register itself.
*> Why each leg can fail: no exception condition has occurred, so no exception object is associated with the current
*> exception and the register is null (GR1) - through the synonym and through the word itself, which stays usable
*> (EQUATE adds a synonym, it removes nothing). EXO = NULL and EXO = EXCEPTION-OBJECT (one instance per run unit,
*> 8.4.3.6.4 GR2: the same object reference) are both true. A compiler that read EXO as a user-defined word refuses
*> the program; one that read it as an undeclared name would too.
*> EXPECTED OUTPUT: EXO NULL / EO NULL / SAME
       >>COBOL-WORDS EQUATE "EXCEPTION-OBJECT" WITH "EXO"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4572EO.
       PROCEDURE DIVISION.
           IF EXO = NULL
              DISPLAY "EXO NULL"
           END-IF
           IF EXCEPTION-OBJECT = NULL
              DISPLAY "EO NULL"
           END-IF
           IF EXO = EXCEPTION-OBJECT
              DISPLAY "SAME"
           END-IF
           STOP RUN.
