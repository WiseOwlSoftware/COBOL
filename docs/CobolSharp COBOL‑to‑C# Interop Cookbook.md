CobolSharp COBOL‑to‑C# Interop Cookbook (CIL‑Only)
=================================================

Purpose
-------
Provide a practical, example‑driven guide for developers integrating COBOL code compiled by CobolSharp with C# and other .NET languages.  
This cookbook shows how to:

- Call .NET from COBOL  
- Call COBOL from .NET  
- Marshal data safely  
- Use OO and generics across the boundary  
- Handle exceptions  
- Work with arrays, records, packed decimals, and REDEFINES  
- Build hybrid COBOL/C# systems  

All examples compile to **pure .NET CIL** — no VM, no alternate backend.

------------------------------------------------------------
SECTION 1 — CALLING .NET FROM COBOL
------------------------------------------------------------

1.1 Calling a static C# method
-----------------------------
C#:
public static class MathLib {
    public static int Add(int a, int b) => a + b;
}

COBOL:
CALL "MathLib::Add" USING A B GIVING RESULT.

Notes:
- "::" indicates a .NET method.
- Types are marshaled automatically.

1.2 Calling an instance method
------------------------------
C#:
public class Greeter {
    public string Greet(string name) => $"Hello, {name}";
}

COBOL:
INVOKE "Greeter::new" RETURNING G.
INVOKE G::Greet USING NAME GIVING MESSAGE.

1.3 Calling a .NET property
---------------------------
C#:
public class Customer {
    public string Name { get; set; }
}

COBOL:
INVOKE "Customer::new" RETURNING CUST.
SET PROPERTY Name OF CUST TO "Alice".
GET PROPERTY Name OF CUST GIVING CUSTOMER-NAME.

1.4 Calling a generic .NET method
---------------------------------
C#:
public static class Utils {
    public static T Echo<T>(T value) => value;
}

COBOL:
CALL "Utils::Echo<System.String>" USING MY-NAME GIVING RESULT.

1.5 Calling async .NET methods
------------------------------
C#:
public async Task<int> FetchValueAsync() => 42;

COBOL:
CALL "MyService::FetchValueAsync" RETURNING TASK.
CALL "CobolSharp.Runtime.Async::Await" USING TASK GIVING RESULT.

------------------------------------------------------------
SECTION 2 — CALLING COBOL FROM C#
------------------------------------------------------------

2.1 Calling a COBOL program
---------------------------
COBOL:
IDENTIFICATION DIVISION.
PROGRAM-ID. HELLO.
PROCEDURE DIVISION.
    DISPLAY "HELLO WORLD".
    GOBACK.

C#:
CobolSharp.HELLO.Main(new string[0]);

2.2 Calling a COBOL method
--------------------------
COBOL:
CLASS-ID. Customer.
METHOD-ID. SetName.
PROCEDURE DIVISION USING BY VALUE NAME.
    MOVE NAME TO CUSTOMER-NAME.
END METHOD.

C#:
var cust = new CobolSharp.Customer();
cust.SetName("Alice");

2.3 Calling COBOL with complex data
-----------------------------------
COBOL:
01 CUSTOMER-RECORD.
   05 ID        PIC 9(5).
   05 NAME      PIC X(30).
   05 BALANCE   PIC S9(7)V99 COMP-3.

C#:
var cust = new CobolSharp.CUSTOMER_RECORD {
    ID = 12345,
    NAME = "Alice",
    BALANCE = 102.50m
};

------------------------------------------------------------
SECTION 3 — DATA MARSHALING RULES
------------------------------------------------------------

3.1 Strings
-----------
COBOL PIC X(n) ↔ C# string  
- Truncated or padded as needed  
- NATIONAL maps to UTF‑16  

3.2 Numeric types
-----------------
COBOL PIC 9(n) → C# int/long/decimal  
COBOL COMP‑3 → C# decimal  
COBOL COMP → C# int/long  
COBOL COMP‑5 → C# IntPtr or long  

3.3 Group items
---------------
Mapped to explicit‑layout C# classes with FieldOffset attributes.

3.4 OCCURS
----------
COBOL OCCURS n → C# T[]  
COBOL OCCURS DEPENDING ON → C# List<T>  

3.5 REDEFINES
-------------
Mapped to overlapping fields in explicit‑layout classes.

3.6 88‑levels
-------------
Mapped to enum‑like constants or boolean helpers.

------------------------------------------------------------
SECTION 4 — EXCEPTION HANDLING
------------------------------------------------------------

4.1 .NET exceptions in COBOL
----------------------------
.NET → COBOL mapping:

ArgumentException → ON EXCEPTION  
InvalidOperationException → ON EXCEPTION  
FileNotFoundException → INVALID KEY  
JsonException → ON EXCEPTION  
XmlException → ON EXCEPTION  

Example:
CALL "MyLib::Dangerous" ON EXCEPTION
    MOVE 1 TO ERROR-FLAG
END-CALL.

4.2 COBOL exceptions in .NET
----------------------------
COBOL runtime exceptions become .NET exceptions when crossing boundaries.

------------------------------------------------------------
SECTION 5 — ARRAYS, RECORDS, AND COMPLEX TYPES
------------------------------------------------------------

5.1 Passing OCCURS arrays to C#
-------------------------------
COBOL:
01 VALUES OCCURS 10 TIMES PIC 9(4).

C#:
int[] arr = cobolObject.VALUES;

5.2 Passing COBOL records to C#
-------------------------------
COBOL group items become C# classes with explicit layout.

5.3 Passing nested structures
-----------------------------
Nested COBOL groups become nested C# classes.

------------------------------------------------------------
SECTION 6 — OO INTEROP
------------------------------------------------------------

6.1 COBOL implementing a C# interface
-------------------------------------
C#:
public interface IGreeter {
    string Greet(string name);
}

COBOL:
CLASS-ID. CobolGreeter IMPLEMENTS IGreeter.
METHOD-ID. Greet.
PROCEDURE DIVISION USING NAME RETURNING RESULT.
    MOVE "Hello " & NAME TO RESULT.
END METHOD.

C#:
IGreeter g = new CobolGreeter();

6.2 COBOL inheriting from a C# base class
-----------------------------------------
C#:
public abstract class Animal {
    public abstract string Speak();
}

COBOL:
CLASS-ID. Dog INHERITS Animal.
METHOD-ID. Speak.
PROCEDURE DIVISION RETURNING RESULT.
    MOVE "Woof" TO RESULT.
END METHOD.

------------------------------------------------------------
SECTION 7 — BEST PRACTICES
------------------------------------------------------------

7.1 Keep interop boundaries clean
---------------------------------
- Use simple types at boundaries  
- Avoid REDEFINES in public APIs  
- Avoid OCCURS DEPENDING ON in public APIs  

7.2 Prefer COBOL classes for interop
------------------------------------
Programs are callable, but classes are cleaner.

7.3 Use marshaling helpers
--------------------------
CobolSharp.Runtime.Marshaling.* provides safe conversions.

7.4 Avoid passing raw StorageBlocks
-----------------------------------
Use typed records instead.

------------------------------------------------------------
SECTION 8 — COMMON PITFALLS
------------------------------------------------------------

- Passing PIC X(n) strings longer than n  
- Passing packed decimals without proper scale  
- Using REDEFINES across interop boundaries  
- Using GO TO inside methods intended for interop  
- Forgetting to initialize COBOL objects before calling methods  

------------------------------------------------------------
SECTION 9 — FULL HYBRID EXAMPLE
------------------------------------------------------------

COBOL business logic + C# UI:

COBOL:
CLASS-ID. OrderProcessor.
METHOD-ID. CalculateTotal.
PROCEDURE DIVISION USING PRICE QTY RETURNING TOTAL.
    COMPUTE TOTAL = PRICE * QTY.
END METHOD.

C#:
var op = new CobolSharp.OrderProcessor();
decimal total = op.CalculateTotal(19.99m, 3);

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp COBOL‑to‑C# Interop Cookbook:
- Provides practical recipes for hybrid COBOL/.NET systems
- Covers calling .NET from COBOL and COBOL from .NET
- Defines marshaling rules for all COBOL types
- Shows OO, generics, arrays, records, and exceptions across boundaries
- Ensures all interop compiles to pure .NET CIL
- Enables modern, maintainable, enterprise‑grade COBOL/C# integration
