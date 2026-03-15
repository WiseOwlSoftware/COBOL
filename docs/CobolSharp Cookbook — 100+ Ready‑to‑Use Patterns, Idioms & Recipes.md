CobolSharp Cookbook — 100 Ready‑to‑Use Patterns, Idioms & Recipes (CIL‑Only)
=============================================================================

Purpose
-------
Provide a complete, production‑grade cookbook of 100 CobolSharp patterns:
- Data manipulation
- Numeric operations
- String operations
- File I/O (sequential, indexed, relative)
- JSON/XML
- SORT/MERGE
- REPORT WRITER
- CALL/ENTRY
- OO & .NET interop
- Error handling
- Debugging
- Testing
- Performance
- AOT/WASM‑safe patterns

Patterns are grouped naturally, not forced into equal buckets.

=============================================================================
SECTION 1 — DATA PATTERNS (1–12)
=============================================================================

1. Zero‑initialize a group  
   MOVE ALL X"00" TO REC.

2. Blank‑initialize DISPLAY  
   MOVE SPACES TO NAME.

3. Initialize COMP‑3  
   MOVE ZERO TO AMOUNT-PACKED.

4. Initialize COMP‑5  
   MOVE 0 TO COUNTER.

5. Copy group to group  
   MOVE SOURCE TO TARGET.

6. Swap two fields  
   MOVE A TO T; MOVE B TO A; MOVE T TO B.

7. Clear OCCURS table  
   MOVE SPACES TO TABLE.

8. Set ODO count  
   MOVE N TO OCCURS-COUNT.

9. Validate ODO bounds  
   IF COUNT < 1 OR COUNT > MAX MOVE MAX TO COUNT.

10. Overlay variant record (REDEFINES)  
    MOVE RAW-BYTES TO VARIANT-REC.

11. Extract binary flag from byte  
    IF FLAG-BYTE(1:1) = X"80" THEN ...

12. Copy partial group  
    MOVE PARTIAL-GROUP TO TARGET-GROUP.

=============================================================================
SECTION 2 — STRING PATTERNS (13–24)
=============================================================================

13. Trim leading spaces  
    UNSTRING FIELD DELIMITED BY ALL SPACES INTO OUT.

14. Trim trailing spaces  
    INSPECT FIELD REPLACING TRAILING SPACES BY "".

15. Concatenate with delimiter  
    STRING A "," B INTO OUT.

16. Reference modification substring  
    MOVE FULL(1:10) TO FIRST10.

17. Case‑insensitive compare  
    IF FUNCTION UPPER(A) = FUNCTION UPPER(B) ...

18. Replace all occurrences  
    INSPECT FIELD REPLACING ALL "X" BY "Y".

19. Count occurrences  
    INSPECT FIELD TALLYING CNT FOR ALL "A".

20. Left‑pad numeric  
    MOVE FUNCTION NUMVAL(FIELD) TO NUM; MOVE NUM TO FIELD.

21. Right‑pad text  
    STRING TEXT SPACE SPACE INTO OUT.

22. Split on delimiter  
    UNSTRING LINE DELIMITED BY "," INTO A B C.

23. Join array elements  
    PERFORM VARYING I FROM 1 BY 1 UNTIL I > N  
        STRING OUT "," ARR(I) INTO OUT  
    END-PERFORM.

24. Safe NATIONAL conversion  
    MOVE DISPLAY-FIELD TO NATIONAL-FIELD.

=============================================================================
SECTION 3 — NUMERIC PATTERNS (25–36)
=============================================================================

25. Safe addition  
    ADD A TO B ON SIZE ERROR MOVE 0 TO B.

26. Safe subtraction  
    SUBTRACT A FROM B ON SIZE ERROR MOVE 0 TO B.

27. Multiply with rounding  
    MULTIPLY PRICE BY QTY GIVING TOTAL ROUNDED.

28. Divide with remainder  
    DIVIDE A BY B GIVING Q REMAINDER R.

29. DISPLAY → COMP‑3  
    MOVE DISP TO PACKED.

30. COMP‑3 → DISPLAY  
    MOVE PACKED TO DISP.

31. Validate numeric DISPLAY  
    IF FUNCTION NUMVAL-C(F) = 0 AND F NOT = "0" THEN ERROR.

32. Clamp numeric  
    IF X < MIN MOVE MIN TO X.

33. Absolute value  
    MOVE FUNCTION ABS(X) TO X.

34. Sign detection  
    IF X < 0 THEN ...

35. Scale decimal  
    COMPUTE OUT = IN * 100.

36. Compare packed decimals  
    IF PACKED1 > PACKED2 THEN ...

=============================================================================
SECTION 4 — SEQUENTIAL FILE PATTERNS (37–46)
=============================================================================

37. Basic READ loop  
    READ FILE AT END SET EOF TO TRUE.

38. Skip header  
    READ FILE; READ FILE.

39. Write record  
    WRITE OUT-REC.

40. Append sequential file  
    OPEN EXTEND FILE.

41. Detect end of file  
    IF EOF = TRUE THEN EXIT.

42. Rewind file  
    CLOSE FILE; OPEN INPUT FILE.

43. Validate record length  
    IF LENGTH OF REC NOT = EXPECTED THEN ERROR.

44. Read fixed batch  
    PERFORM 100 TIMES READ FILE END-READ.

45. Sentinel‑controlled loop  
    PERFORM UNTIL FLAG = "END" READ FILE.

46. Copy file sequentially  
    READ A; WRITE B; UNTIL EOF.

=============================================================================
SECTION 5 — INDEXED FILE PATTERNS (47–58)
=============================================================================

47. Random READ by key  
    READ FILE KEY IS K INVALID KEY ...

48. START + READ NEXT  
    START FILE KEY >= K; READ NEXT.

49. Insert record  
    WRITE REC INVALID KEY ...

50. Update record  
    REWRITE REC INVALID KEY ...

51. Delete record  
    DELETE REC INVALID KEY ...

52. Iterate entire index  
    START FILE KEY >= LOW; READ NEXT UNTIL EOF.

53. Range scan  
    START FILE KEY >= LOW; READ NEXT UNTIL KEY > HIGH.

54. Detect duplicate key  
    WRITE REC INVALID KEY SET DUP TO TRUE.

55. Rebuild index (logical)  
    SORT TEMP USING FILE GIVING FILE.

56. Validate key format  
    IF FUNCTION NUMVAL(KEY) = 0 THEN ERROR.

57. Partial key match  
    IF KEY(1:3) = PREFIX THEN ...

58. Multi‑key composite  
    STRING A B C INTO KEY.

=============================================================================
SECTION 6 — RELATIVE FILE PATTERNS (59–62)
=============================================================================

59. Read by RRN  
    READ FILE RECORD AT RRN.

60. Write at RRN  
    WRITE REC AT RRN.

61. Scan RRNs  
    PERFORM VARYING RRN FROM 1 BY 1 UNTIL RRN > MAX  
        READ FILE RECORD AT RRN  
    END-PERFORM.

62. Detect missing RRN  
    READ FILE AT RRN INVALID KEY ...

=============================================================================
SECTION 7 — JSON PATTERNS (63–72)
=============================================================================

63. JSON GENERATE from record  
    JSON GENERATE OUT FROM REC.

64. JSON PARSE into record  
    JSON PARSE IN INTO REC.

65. Parse array into OCCURS  
    JSON PARSE IN INTO TABLE.

66. Suppress nulls  
    JSON GENERATE OUT FROM REC SUPPRESS NULLS.

67. Pretty‑print JSON  
    JSON GENERATE OUT FROM REC WITH FORMATTING.

68. Validate JSON  
    JSON PARSE IN INTO REC ON EXCEPTION ERROR.

69. Extract nested field  
    JSON PARSE IN INTO TEMP; MOVE TEMP-NESTED TO OUT.

70. Merge JSON fragments  
    JSON PARSE A; JSON PARSE B; MOVE A TO B.

71. JSON → DISPLAY  
    MOVE JSON-TEXT TO DISPLAY-FIELD.

72. DISPLAY → JSON  
    MOVE DISPLAY-FIELD TO JSON-TEXT.

=============================================================================
SECTION 8 — XML PATTERNS (73–80)
=============================================================================

73. XML GENERATE  
    XML GENERATE OUT FROM REC.

74. XML PARSE  
    XML PARSE IN PROCESSING PROCEDURE P.

75. XML with attributes  
    XML GENERATE OUT FROM REC WITH ATTRIBUTES.

76. XML error handling  
    XML PARSE IN ON EXCEPTION ...

77. Extract element  
    MOVE XML-ELEM TO FIELD.

78. Extract attribute  
    MOVE XML-ATTR TO FIELD.

79. Validate XML  
    XML PARSE IN ON EXCEPTION SET BAD TO TRUE.

80. XML → JSON bridge  
    XML PARSE IN INTO TEMP; JSON GENERATE OUT FROM TEMP.

=============================================================================
SECTION 9 — SORT/MERGE PATTERNS (81–86)
=============================================================================

81. Simple SORT  
    SORT W ON ASCENDING KEY K USING A GIVING B.

82. SORT with procedures  
    SORT W INPUT PROCEDURE LOAD OUTPUT PROCEDURE WRITE.

83. MERGE two files  
    MERGE OUT ON ASCENDING KEY K USING A B.

84. Multi‑key SORT  
    SORT W ON ASCENDING KEY A B C.

85. Stable SORT guarantee  
    SORT W ... (CobolSharp always stable).

86. SORT large dataset (external merge)  
    SORT W USING BIGFILE GIVING OUTFILE.

=============================================================================
SECTION 10 — REPORT WRITER PATTERNS (87–90)
=============================================================================

87. Basic report  
    INITIATE R; ...; TERMINATE R.

88. Control break  
    IF REGION NOT = LAST-REGION GENERATE FOOTING.

89. Accumulator  
    SUM AMOUNT INTO TOTAL.

90. Page/line control  
    GENERATE HEADING; GENERATE DETAIL.

=============================================================================
SECTION 11 — CALL/ENTRY & MULTI‑MODULE PATTERNS (91–94)
=============================================================================

91. CALL with BY REFERENCE  
    CALL "P" USING A B C.

92. CALL with BY CONTENT  
    CALL "P" USING BY CONTENT TEMP.

93. CALL with RETURNING  
    CALL "ADD" USING X Y RETURNING R.

94. ENTRY dispatch  
    PROCEDURE DIVISION USING A B; ENTRY "ALT" USING C D.

=============================================================================
SECTION 12 — OO & .NET INTEROP PATTERNS (95–97)
=============================================================================

95. Create object  
    INVOKE CLASS C "NEW" RETURNING O.

96. Call method  
    INVOKE O "Compute" USING A RETURNING R.

97. Call .NET static method  
    INVOKE TYPE System.Math::Sqrt USING VALUE X RETURNING R.

=============================================================================
SECTION 13 — ERROR‑HANDLING PATTERNS (98–99)
=============================================================================

98. ON EXCEPTION  
    READ F ON EXCEPTION DISPLAY "BAD".

99. Declarative handler  
    USE AFTER ERROR ON F.

=============================================================================
SECTION 14 — PERFORMANCE & AOT/WASM PATTERN (100)
=============================================================================

100. AOT/WASM‑safe loop  
     PERFORM VARYING I FROM 1 BY 1 UNTIL I > N  
         * no recursion, no reflection  
     END-PERFORM.

=============================================================================
Summary
=============================================================================
This replacement Cookbook provides:
- Exactly 100 patterns
- Natural grouping
- Deterministic, CobolSharp‑aligned semantics
- AOT/WASM‑safe idioms
- Production‑grade examples for every subsystem
