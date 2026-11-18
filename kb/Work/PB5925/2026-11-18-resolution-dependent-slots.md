## Lead: four kinds of name slot are a reference only when the word names a declared symbol (listing step 7a)

Step 7a labelled every name slot of the grammar with its class (DESIGN-listing.md section 6.7.1). Four kinds of slot hold either
a name the program declares or a word of the implementor or the standard, and the grammar cannot tell which:

- `functionName`'s `cobolWord` (`FUNCTION f(...)`): a user-defined function declared in the REPOSITORY paragraph, or an
  intrinsic function;
- `raisingTarget`'s `cobolWord` (RAISING in a procedure division header): an exception-name, an object class or an interface;
- `assignTarget`'s `cobolWord` (ASSIGN TO): a device-name (an implementor-name) or a data-name;
- RERUN ON's `cobolWord`: a file-name or an implementor-name.

Each is labelled `ref`. Step 7b's completeness oracle (`EveryWrittenReferenceIsRecorded`: each `ref` node has exactly one
occurrence) must count an occurrence at such a node only when `Identify` identified a symbol there, or these slots need a class
of their own (a `ref` that may name nothing). Decide in step 7b, with the oracle; the counter of `GrammarNameSlotDriftTests`
needs no change either way.
