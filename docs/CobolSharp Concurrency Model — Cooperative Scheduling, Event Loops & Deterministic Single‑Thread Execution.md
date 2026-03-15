CobolSharp Concurrency Model — Cooperative Scheduling, Event Loops & Deterministic Single‑Thread Execution (CIL‑Only)
====================================================================================================================

Purpose
-------
Define the authoritative architecture for:
- CobolSharp’s deterministic single‑thread execution model
- Cooperative scheduling (non‑preemptive)
- Event loop integration (I/O, timers, async interop)
- Prohibition of true parallelism
- Deterministic ordering of asynchronous events
- Safe interop with .NET async/await
- AOT/WASM‑safe scheduling
- CIL‑friendly lowering

This document governs how CobolSharp ensures deterministic execution while supporting modern asynchronous patterns.

------------------------------------------------------------
SECTION 1 — CONCURRENCY MODEL OVERVIEW
------------------------------------------------------------

CobolSharp enforces:
- **Single‑threaded execution**
- **No preemption**
- **No parallelism**
- **Deterministic event ordering**
- **Cooperative scheduling only**

This ensures:
- Reproducible behavior
- Identical results across platforms
- No data races
- No nondeterministic interleavings

------------------------------------------------------------
SECTION 2 — WHY COBOLSHARP IS SINGLE‑THREADED
------------------------------------------------------------

2.1 COBOL language semantics
----------------------------
COBOL assumes:
- Sequential execution
- No shared‑memory concurrency
- No atomic operations
- No thread safety requirements

2.2 Determinism requirements
----------------------------
Parallelism introduces:
- Race conditions
- Nondeterministic ordering
- Platform‑dependent scheduling

CobolSharp forbids all of these.

2.3 AOT/WASM constraints
------------------------
WASM:
- No threads (unless WASM threads extension enabled)
- No shared memory by default

AOT:
- No dynamic thread creation

------------------------------------------------------------
SECTION 3 — COOPERATIVE SCHEDULING MODEL
------------------------------------------------------------

3.1 Cooperative only
--------------------
Tasks yield control explicitly:
- During I/O
- During async interop
- During timers
- During WAIT operations (future extension)

3.2 No preemption
-----------------
Runtime never interrupts COBOL code.

3.3 Scheduling points
---------------------
Scheduling occurs only at:
- File I/O operations
- Console I/O operations
- JSON/XML parsing
- SORT operations
- INVOKE of async .NET methods
- Timer events

------------------------------------------------------------
SECTION 4 — EVENT LOOP ARCHITECTURE
------------------------------------------------------------

4.1 Event loop responsibilities
-------------------------------
- Manage pending tasks
- Deliver I/O completions
- Deliver timer events
- Deliver async interop completions
- Maintain deterministic ordering

4.2 Event queue
---------------
Queue is:
- FIFO
- Deterministic
- Single‑threaded

4.3 Event ordering rules
------------------------
1. I/O completions  
2. Timer events  
3. Async interop completions  
4. User‑scheduled events  

Within each category:
- FIFO order
- No reordering

------------------------------------------------------------
SECTION 5 — ASYNC INTEROP WITH .NET
------------------------------------------------------------

5.1 INVOKE of async .NET method
-------------------------------
INVOKE obj FooAsync RETURNING r.

Lowering:
- Call FooAsync()
- Register continuation with event loop
- Yield execution
- Resume when task completes

5.2 Awaiting tasks
------------------
CobolSharp does not expose `await`, but:
- Async .NET methods return Task
- Task completion triggers event loop callback
- Callback resumes COBOL execution

5.3 Exception handling
----------------------
If Task faults:
- ExceptionState populated
- ON EXCEPTION triggered

------------------------------------------------------------
SECTION 6 — TIMERS
------------------------------------------------------------

6.1 Timer scheduling
--------------------
CobolSharp supports:
- EVENT AFTER n SECONDS (future extension)
- Timer callbacks delivered via event loop

6.2 Timer determinism
---------------------
Timers use:
- Monotonic clock
- Deterministic ordering
- No drift across platforms

------------------------------------------------------------
SECTION 7 — FILE I/O ASYNC MODEL
------------------------------------------------------------

7.1 Non‑blocking I/O
--------------------
FileManager uses:
- Async file operations
- Event loop integration

7.2 READ/WRITE behavior
-----------------------
READ:
- If data available → immediate
- Else → yield until completion event

WRITE:
- Buffered
- Completion event delivered deterministically

------------------------------------------------------------
SECTION 8 — JSON/XML ASYNC MODEL
------------------------------------------------------------

8.1 Streaming parsers
---------------------
JSON/XML parsing:
- Non‑blocking
- Yields during large inputs
- Deterministic token delivery

8.2 Error handling
------------------
Errors delivered as:
- Event loop exception events
- Routed to ExceptionState

------------------------------------------------------------
SECTION 9 — SORT ENGINE ASYNC MODEL
------------------------------------------------------------

9.1 External merge sort
-----------------------
Large sorts:
- Chunking
- Async file I/O
- Deterministic merge ordering

9.2 Event loop integration
--------------------------
Each merge step:
- Yields
- Resumes deterministically

------------------------------------------------------------
SECTION 10 — PROHIBITED CONCURRENCY FEATURES
------------------------------------------------------------

CobolSharp forbids:
- Threads
- ThreadPool
- Tasks created manually
- Parallel LINQ
- Locks, mutexes, semaphores
- async void
- Background workers
- Timers outside event loop
- Shared mutable state across tasks

------------------------------------------------------------
SECTION 11 — CIL LOWERING RULES
------------------------------------------------------------

11.1 Async interop lowering
---------------------------
INVOKE FooAsync:
- call FooAsync()
- call EventLoop.Register(task)
- br yieldPoint

11.2 Yield lowering
-------------------
Compiler inserts:
- call EventLoop.Yield()

11.3 Resume lowering
--------------------
Event loop calls:
- ctx.Resume(state)

------------------------------------------------------------
SECTION 12 — DEBUGGER INTEGRATION
------------------------------------------------------------

Debugger shows:
- Event queue
- Pending tasks
- Timer events
- Current scheduling point
- Async continuation state

------------------------------------------------------------
SECTION 13 — AOT/WASM‑SAFE CONCURRENCY
------------------------------------------------------------

13.1 No threads
---------------
All concurrency simulated via event loop.

13.2 No blocking syscalls
-------------------------
All I/O async.

13.3 Deterministic scheduling
-----------------------------
Event loop identical across platforms.

------------------------------------------------------------
SECTION 14 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

14.1 Async method returns null Task
-----------------------------------
Runtime error.

14.2 Timer scheduled in past
----------------------------
Executes immediately.

14.3 Multiple completions at same timestamp
-------------------------------------------
FIFO ordering.

14.4 Task completes synchronously
---------------------------------
Handled immediately, no yield.

14.5 File I/O error during async
--------------------------------
ExceptionState populated.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp Concurrency Model:
- Enforces deterministic single‑thread execution
- Provides cooperative scheduling via event loop
- Supports async .NET interop safely and predictably
- Ensures reproducible behavior across CoreCLR, AOT, and WASM
- Eliminates all sources of nondeterminism from parallelism
