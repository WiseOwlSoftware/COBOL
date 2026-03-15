CobolSharp COBOL ACCEPT, DISPLAY, Console I/O & Environment‑Variable Architecture (CIL‑Only)
===========================================================================================

Purpose
-------
Define the authoritative architecture for:
- ACCEPT (console, date/time, environment variables)
- DISPLAY (console, file, screen)
- Line advancing and column positioning
- WITH NO ADVANCING semantics
- ACCEPT FROM DATE / TIME / COMMAND‑LINE
- ACCEPT FROM ENVIRONMENT
- ACCEPT FROM ARGUMENT‑VALUE
- UTF‑8/UTF‑16 console bridging
- Integration with ExecutionContext.ConsoleEngine
- AOT/WASM‑safe I/O
- CIL‑friendly lowering

This document governs how CobolSharp implements COBOL’s ACCEPT/DISPLAY and environment‑variable semantics.

------------------------------------------------------------
SECTION 1 — CONSOLEENGINE OVERVIEW
------------------------------------------------------------

ExecutionContext.ConsoleEngine provides:
- Console input (UTF‑16)
- Console output (UTF‑16)
- Environment variable lookup
- Command‑line argument access
- Date/time formatting
- Line/column positioning
- ExceptionState population

Console I/O is:
- Pure managed
- Deterministic
- AOT/WASM‑safe

------------------------------------------------------------
SECTION 2 — DISPLAY STATEMENT
------------------------------------------------------------

2.1 Basic form
--------------
DISPLAY identifier.
DISPLAY literal.
DISPLAY identifier UPON CONSOLE.

2.2 Output encoding
-------------------
DISPLAY → UTF‑16  
NATIONAL → UTF‑16  
Numeric → converted to DISPLAY then UTF‑16

2.3 Line advancing
------------------
Default:
- Append newline after output

2.4 WITH NO ADVANCING
----------------------
DISPLAY x WITH NO ADVANCING.
- No newline appended
- Next DISPLAY continues on same line

2.5 Multiple operands
---------------------
DISPLAY A B C.
- Concatenated with no separator

2.6 Column positioning
----------------------
DISPLAY x AT column.
- Moves cursor to column
- Writes text
- No newline unless advancing

------------------------------------------------------------
SECTION 3 — ACCEPT STATEMENT
------------------------------------------------------------

3.1 Basic form
--------------
ACCEPT identifier.

Reads:
- One line from console
- UTF‑16 input
- Converted to target category

3.2 ACCEPT with size limit
--------------------------
If input longer than target:
- Truncated
- No overflow exception

3.3 ACCEPT FROM DATE / TIME
---------------------------
ACCEPT ws FROM DATE.
ACCEPT ws FROM TIME.

DATE format:
- YYYYMMDD

TIME format:
- HHMMSShh (hundredths)

3.4 ACCEPT FROM DAY / DAY‑OF‑WEEK
---------------------------------
DAY:
- Day of year (001–366)

DAY‑OF‑WEEK:
- 1 = Monday … 7 = Sunday

3.5 ACCEPT FROM COMMAND‑LINE
----------------------------
ACCEPT ws FROM COMMAND‑LINE.

Returns:
- Entire command line as DISPLAY string

3.6 ACCEPT FROM ARGUMENT‑NUMBER
-------------------------------
ACCEPT n FROM ARGUMENT‑NUMBER.
- Returns number of command‑line arguments

3.7 ACCEPT FROM ARGUMENT‑VALUE
------------------------------
ACCEPT ws FROM ARGUMENT‑VALUE i.
- Returns ith argument (1‑based)

3.8 ACCEPT FROM ENVIRONMENT
---------------------------
ACCEPT ws FROM ENVIRONMENT "VAR".

- Looks up environment variable
- Missing variable → spaces

------------------------------------------------------------
SECTION 4 — CATEGORY CONVERSION RULES
------------------------------------------------------------

4.1 DISPLAY target
------------------
- UTF‑16 input converted to ASCII
- Non‑ASCII → runtime error

4.2 NATIONAL target
-------------------
- UTF‑16 input copied directly

4.3 Numeric target
------------------
- Input parsed as Decimal
- Invalid characters → runtime error

4.4 Group target
----------------
- ACCEPT into group:
  - Fills group as DISPLAY
  - No numeric conversion

------------------------------------------------------------
SECTION 5 — ENVIRONMENT VARIABLE ARCHITECTURE
------------------------------------------------------------

5.1 Lookup rules
----------------
Environment variables:
- Case‑insensitive
- Retrieved via .NET environment API

5.2 Missing variable
--------------------
- Target filled with spaces

5.3 NATIONAL targets
--------------------
- UTF‑16 conversion applied

5.4 Numeric targets
-------------------
- Parsed as Decimal
- Invalid → runtime error

------------------------------------------------------------
SECTION 6 — COMMAND‑LINE ARGUMENT ARCHITECTURE
------------------------------------------------------------

6.1 Storage
-----------
ExecutionContext stores:
- Raw command line
- Argument array

6.2 ARGUMENT‑NUMBER
-------------------
Returns:
- Count of arguments (not including program name)

6.3 ARGUMENT‑VALUE
------------------
Returns:
- UTF‑16 string
- Converted to target category

------------------------------------------------------------
SECTION 7 — DATE/TIME ARCHITECTURE
------------------------------------------------------------

7.1 DATE
--------
YYYYMMDD:
- Year: 4 digits
- Month: 2 digits
- Day: 2 digits

7.2 TIME
--------
HHMMSShh:
- Hours: 00–23
- Minutes: 00–59
- Seconds: 00–59
- Hundredths: 00–99

7.3 DAY
-------
Day of year:
- 001–366

7.4 DAY‑OF‑WEEK
---------------
1 = Monday  
7 = Sunday

------------------------------------------------------------
SECTION 8 — CIL LOWERING RULES
------------------------------------------------------------

8.1 DISPLAY lowering
--------------------
Generated IL:
- Evaluate operands
- Convert to UTF‑16
- Call ConsoleEngine.Write
- Append newline unless NO ADVANCING

8.2 ACCEPT lowering
-------------------
Generated IL:
- Call ConsoleEngine.ReadLine
- Convert to target category
- Write into StorageBlock

8.3 ACCEPT FROM DATE/TIME lowering
----------------------------------
Generated IL:
- Call ConsoleEngine.GetDate / GetTime
- Move into target

8.4 ACCEPT FROM ENVIRONMENT lowering
------------------------------------
Generated IL:
- Call ConsoleEngine.GetEnvironmentVariable

8.5 ACCEPT FROM ARGUMENT lowering
---------------------------------
Generated IL:
- Call ConsoleEngine.GetArgumentValue

------------------------------------------------------------
SECTION 9 — DEBUGGER INTEGRATION
------------------------------------------------------------

Debugger shows:
- Input buffer
- Output buffer
- Date/time values
- Environment variable name/value
- Argument index/value
- ExceptionState

------------------------------------------------------------
SECTION 10 — AOT/WASM‑SAFE I/O
------------------------------------------------------------

10.1 No dynamic codegen
-----------------------
All I/O static.

10.2 No unsafe code
-------------------
No pointers or stackalloc.

10.3 Deterministic behavior
---------------------------
Same results across platforms.

10.4 WASM console bridging
--------------------------
ConsoleEngine uses:
- JS interop for input/output
- UTF‑16 marshaling

------------------------------------------------------------
SECTION 11 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

11.1 ACCEPT into smaller field
------------------------------
Truncation.

11.2 ACCEPT into numeric with spaces
------------------------------------
Spaces ignored.

11.3 DISPLAY of NATIONAL with surrogate pairs
---------------------------------------------
UTF‑16 preserved.

11.4 ACCEPT FROM ENVIRONMENT with non‑ASCII
-------------------------------------------
Allowed for NATIONAL; error for DISPLAY.

11.5 ACCEPT FROM ARGUMENT‑VALUE out of range
--------------------------------------------
Spaces.

11.6 DISPLAY WITH NO ADVANCING followed by ACCEPT
-------------------------------------------------
ACCEPT reads from same line.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp ACCEPT/DISPLAY Architecture:
- Implements full COBOL console I/O semantics
- Supports DATE/TIME, ENVIRONMENT, COMMAND‑LINE, and ARGUMENT‑VALUE
- Provides deterministic UTF‑16 console behavior
- Integrates tightly with ExecutionContext.ConsoleEngine
- Generates clean, verifiable, debugger‑friendly CIL
- Ensures correctness across CoreCLR, AOT, and WASM
