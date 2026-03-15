CobolSharp Complete Developer Guide — Best Practices, Patterns, Anti‑Patterns & Performance Recipes (CIL‑Only)
==============================================================================================================

Purpose
-------
Provide the authoritative developer guide for CobolSharp:
- Best practices for writing maintainable, deterministic COBOL
- Recommended patterns for data, control flow, OO, and interop
- Anti‑patterns to avoid (performance, determinism, readability)
- Performance recipes for StorageBlocks, numeric ops, string ops, file I/O, JSON/XML, SORT, and REPORT
- Debugging techniques
- Testing strategies
- AOT/WASM‑friendly coding guidelines
- Migration guidelines for legacy COBOL developers

This document is the practical, developer‑facing companion to the formal architecture.

------------------------------------------------------------
SECTION 1 — CORE PRINCIPLES
------------------------------------------------------------

1.1 Determinism first
---------------------
Always write code that:
- Produces identical output for identical input  
- Avoids nondeterministic constructs  
- Avoids reliance on system locale or environment  

1.2 Predictable data layout
---------------------------
Use:
- Explicit PIC clauses  
- Explicit USAGE  
- Explicit OCCURS bounds  

Avoid:
- Implicit conversions  
- Ambiguous REDEFINES  

1.3 Minimal side effects
------------------------
Prefer:
- Pure computations  
- Localized state  
- Clear data flow  

Avoid:
- Hidden state in COMMON programs  
- Overuse of REDEFINES  

1.4 Structured control flow
---------------------------
Prefer:
- PERFORM  
- IF/EVALUATE  
- Paragraphs with clear entry/exit  

Avoid:
- Deeply nested IFs  
- GO TO except for structured patterns  

------------------------------------------------------------
SECTION 2 — DATA DESIGN BEST PRACTICES
------------------------------------------------------------

2.1 Use groups for structure
----------------------------
Group items should represent:
- Records  
- Composite values  
- Logical entities  

2.2 Use elementary items for computation
----------------------------------------
Elementary items should represent:
- Numeric fields  
- Flags  
- Keys  

2.3 REDEFINES usage
-------------------
Use REDEFINES for:
- Variant records  
- Overlaying binary formats  

Avoid REDEFINES for:
- General‑purpose type conversion  
- Unrelated fields  

2.4 OCCURS usage
----------------
Use OCCURS for:
- Fixed tables  
- Repeating structures  

Use ODO for:
- Variable‑length records  
- JSON/XML arrays  

------------------------------------------------------------
SECTION 3 — CONTROL FLOW BEST PRACTICES
------------------------------------------------------------

3.1 Paragraph design
--------------------
Each paragraph should:
- Perform one logical task  
- Have a clear name  
- Avoid side effects  

3.2 PERFORM patterns
--------------------
Use:
- PERFORM paragraph  
- PERFORM A THRU B for structured blocks  
- PERFORM UNTIL for loops  

Avoid:
- PERFORM THRU with large ranges  
- PERFORM inside deeply nested IFs  

3.3 EVALUATE patterns
---------------------
Use EVALUATE for:
- Multi‑branch logic  
- Pattern matching  
- Status code handling  

------------------------------------------------------------
SECTION 4 — OO BEST PRACTICES
------------------------------------------------------------

4.1 Class design
----------------
Classes should:
- Encapsulate behavior  
- Avoid global state  
- Use FACTORY for stateless helpers  

4.2 INVOKE usage
----------------
Use INVOKE for:
- .NET interop  
- Utility methods  
- JSON/XML helpers  

Avoid:
- INVOKE inside tight loops  
- INVOKE for trivial operations  

------------------------------------------------------------
SECTION 5 — INTEROP BEST PRACTICES
------------------------------------------------------------

5.1 Safe interop
----------------
Only call:
- Pure .NET methods  
- Deterministic APIs  

Avoid:
- Reflection  
- File I/O outside FileManager  
- Threading APIs  

5.2 Data marshaling
-------------------
Use:
- DISPLAY for strings  
- COMP‑5 for integers  
- COMP‑3 for decimals  

------------------------------------------------------------
SECTION 6 — PERFORMANCE RECIPES
------------------------------------------------------------

6.1 Numeric performance
-----------------------
- Use COMP‑5 for counters  
- Use COMP‑3 for financial values  
- Avoid repeated conversions  
- Pre‑scale values when possible  

6.2 String performance
----------------------
- Use reference modification instead of substring functions  
- Avoid repeated STRING operations in loops  
- Use NATIONAL only when required  

6.3 StorageBlock performance
----------------------------
- Prefer fixed‑size fields  
- Avoid deeply nested groups  
- Avoid large REDEFINES overlays  

6.4 File I/O performance
------------------------
- Use sequential access when possible  
- Use START before random READ  
- Keep keys compact  

6.5 JSON/XML performance
------------------------
- Use SAX parsing (default)  
- Avoid deeply nested structures  
- Use OCCURS for arrays  

6.6 SORT performance
--------------------
- Use fixed‑size keys  
- Avoid expensive key extraction  
- Use GIVING for large outputs  

6.7 REPORT performance
----------------------
- Precompute line widths  
- Avoid dynamic formatting in loops  

------------------------------------------------------------
SECTION 7 — ANTI‑PATTERNS
------------------------------------------------------------

7.1 Data anti‑patterns
----------------------
Avoid:
- Giant WORKING‑STORAGE blocks  
- Overuse of REDEFINES  
- Numeric DISPLAY for computation  

7.2 Control‑flow anti‑patterns
------------------------------
Avoid:
- GO TO spaghetti  
- Deep nesting  
- Paragraphs with side effects  

7.3 OO anti‑patterns
--------------------
Avoid:
- Classes with global state  
- INVOKE for trivial logic  

7.4 Interop anti‑patterns
-------------------------
Avoid:
- Calling nondeterministic APIs  
- Passing large strings repeatedly  

------------------------------------------------------------
SECTION 8 — DEBUGGING TECHNIQUES
------------------------------------------------------------

8.1 Breakpoint strategy
-----------------------
- Break on paragraph entry  
- Break on declaratives  
- Break on key statements  

8.2 StorageBlock inspection
---------------------------
Inspect:
- Raw bytes  
- Decoded fields  
- OCCURS tables  
- REDEFINES overlays  

8.3 ExecutionContext inspection
-------------------------------
Check:
- PERFORM stack  
- CALL stack  
- ExceptionState  

------------------------------------------------------------
SECTION 9 — TESTING BEST PRACTICES
------------------------------------------------------------

9.1 Unit tests
--------------
Test:
- Paragraphs  
- Numeric logic  
- String logic  
- File I/O  

9.2 Snapshot tests
------------------
Snapshot:
- StorageBlocks  
- Execution traces  
- File sequences  

9.3 Cross‑target tests
----------------------
Verify:
- CoreCLR  
- AOT  
- WASM  

------------------------------------------------------------
SECTION 10 — AOT/WASM GUIDELINES
------------------------------------------------------------

10.1 AOT‑friendly code
----------------------
Avoid:
- Reflection  
- Dynamic invocation  
- Large static constructors  

10.2 WASM‑friendly code
-----------------------
Avoid:
- Large recursion  
- Large memory allocations  
- Deep JSON/XML nesting  

------------------------------------------------------------
SECTION 11 — MIGRATION GUIDELINES
------------------------------------------------------------

11.1 From IBM/Micro Focus
-------------------------
Replace:
- COMP → COMP‑5  
- COMP‑1/COMP‑2 → Decimal  
- POINTER → INDEX  

11.2 From legacy COBOL
-----------------------
Replace:
- ALTER  
- STOP literal  
- Non‑standard intrinsics  

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Developer Guide:
- Provides best practices for