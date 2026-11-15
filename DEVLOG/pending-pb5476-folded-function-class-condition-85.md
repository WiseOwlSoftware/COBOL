## PB5476: a bind-time-folded function (FUNCTION LENGTH (X5)) in a class condition is a function-identifier at COBOL-85

**Mechanism.** `FUNCTION LENGTH (X5) IS NUMERIC` compiled and printed T at `--std 85`, while `FUNCTION NUMVAL ("1") IS NUMERIC` was refused (COBOL6918). §1985:VI.6.3.1.2 GR5: "If identifier-1 is a function-identifier, it must reference an alphanumeric function". The binder folds LENGTH of a fixed item to `BoundNumericLiteral { FunctionValue: true }`, and `ConditionBinder.CheckClassConditionOperand` computed its `function` flag from `BoundComputedOperand { Expr: BoundIntrinsicCall }` only, so `UsageDisplay1985` failed open (function false, usage null).

**Re-probe.** On the branch's build before the fix the note's repro printed `T` at 85; after, `COBOL6918` at 85 for NUMERIC and `COBOL6916` for `FUNCTION LENGTH (X5) IS ALPHABETIC`; 2002 and 2023 still compile the NUMERIC form (category numeric, §2023:8.8.4.4.3 SR8) and print `T`.

**Fix.** The `function` flag now also reads the folded operand, the binder's own statement of "a function-identifier whose value was folded" (kb/Work PB1398). No other file changed: the folded literal's category was already numeric through `IntrinsicResultType.OperandCategory`.

**Goldens.** `pb5476-function-length-numeric-at-85` (refused at 85, COBOL6918) and `pb5476_function_length_numeric_2002` (accepted from 2002, prints T). Inventory: `1985-GR-VI.6.3.1.2-5` PARTIAL to CONFORMS (GAP 157 to 156).

**Sweep.** The other readers of `{ Expr: BoundIntrinsicCall }` (MoveBinder `senderIsFunction`, OoBinder, RelationComparability) are not class-condition screens and are unchanged.
