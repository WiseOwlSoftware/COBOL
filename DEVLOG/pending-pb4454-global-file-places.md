## kb/Work PB4454: a GLOBAL file's clause operands in a contained program have no place

Filed PB4454 from the w20261827a branch review's lead R2-1.
The compiler now throws InvalidOperationException, and before PB2375 the emitted C# failed with CS0120.
This happens when a contained program uses a container's GLOBAL file whose RELATIVE KEY, ASSIGN USING, VARYING DEPENDING or SORT key is a non-GLOBAL container item.
The fix gives every file-clause operand the FILE STATUS anchor mechanism.
