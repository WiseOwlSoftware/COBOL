## PB2403 step 1: AdapterPair, OoStandardClasses and the crossing-form predicates are model data

PB2403 removes the backend's last reads of the binder's OO answers (`WiseOwl.Cobol.Compiler.Oo`). This branch lands its
behavior-neutral first step:
- `AdapterPair` moved to `Binding/Model`. It names only the OO symbols.
- `OoStandardClasses` moved whole to `Binding/Model`, because its closure is model types only. `BackendContract` now
  classifies its three COBOL words.
- The two crossing-form predicates moved out of `OoClassTable` into the model's `ActivationCrossing`, and every caller in
  the binder and the backend changed. `OoEmitter.OoStringCarried`, a forwarder to one of them, is deleted.

The architecture walk fell from `CodeGen → Compiler.Oo` 13 to 8 and from `Binding.Model → Compiler.Oo` 2 to 1, and the
edges file is ratcheted. The two inventory rows that cited the moved `BuildBase` are re-sited. The oracle compared the tree
with the PB4517 head and found it identical.

Step 2 is still open, and kb/Work PB2403's Progress section records its plan. The activation descriptions cannot move below
the binder, because their item half reads `MoveTable16` and `PrototypeSignatures`. They therefore become bind-time data, as
do the class lists, the imported types and the RAISING class names.
