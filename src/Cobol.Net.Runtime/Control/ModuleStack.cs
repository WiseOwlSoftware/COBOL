// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// The run-unit module call-name stack backing FUNCTION MODULE-NAME (ISO §15.65). <see cref="ProgramTable"/>
/// pushes a frame at every activation boundary (RunMain for the main program; CallProgram for every CALL /
/// cross-assembly CALL / user-function reference / nested-program CALL) and pops it on return, so the top of
/// stack is always the currently running runtime element. Each frame carries the element's own name, the
/// outermost program-id of its compilation unit (§15.65.4 r7 CURRENT), and whether it is a nested (contained)
/// program (r8 NESTED). One INSTANCE per run unit on <see cref="RunUnit"/> (was the runtime's ONE
/// <c>[ThreadStatic]</c> — the ambient run unit's AsyncLocal now carries the activation scope uniformly,
/// DESIGN-runtime-library §2.1).
///
/// OO method (INVOKE) activation does not funnel through CallProgram (methods are not registry nodes), so the
/// push is emitted INSIDE the method body — <c>OoEmitter.EmitMethod</c>, in a try/finally — rather than at any
/// call site: a method is reached by a typed direct call, by the universal <c>__CobolInvoke</c> switch, and by
/// an inline invocation, and a per-site push would be three arms of one dispatch (fix-queue PB36).
///
/// ⛔ THIS COMMENT USED TO SAY THE OMISSION WAS "conformant, not residue: §15.65.4 r3/r4 make the returned form
/// for method-id elements implementor-defined". BOTH CITED RULES ARE REAL AND NEITHER GOVERNS. r3 is about a
/// runtime element that is NOT a COBOL runtime element; a COBOL method is one. r4 is about the FORM of the name
/// and explicitly lists "method-id" among the forms an implementor may return — which presumes the element is on
/// the stack. Meanwhile r5 names "an INVOKE statement" as an activation mechanism outright. The cost of the
/// misreading was three wrong answers inside every method: CURRENT returned the CALLER, STACK omitted the method,
/// and ACTIVATING returned the single space r5 reserves for a MAIN PROGRAM.
/// </summary>
public sealed class ModuleStack
{
    private readonly record struct Frame(string Name, string Outermost, bool IsNested, bool IsMain);

    private readonly List<Frame> _stack = [];

    /// <summary>Push the run-unit main program (TOP-LEVEL, §15.65.4 r10; ACTIVATING → single space, r5).</summary>
    public void PushMain(string name) => _stack.Add(new(name, name, IsNested: false, IsMain: true));

    /// <summary>Push a CALLed / referenced element (its name, its compilation unit's outermost program-id, and
    /// whether it is a contained/nested program).</summary>
    public void Push(string name, string outermost, bool isNested)
        => _stack.Add(new(name, outermost, isNested, IsMain: false));

    /// <summary>Pop the current element on activation return (paired with every push in a finally).</summary>
    public void Pop() { if (_stack.Count > 0) _stack.RemoveAt(_stack.Count - 1); }

    /// <summary>Clear the stack at run-unit start (called from the run-unit reset).</summary>
    public void Reset() => _stack.Clear();

    /// <summary>Resolve MODULE-NAME for the keyword selector (0 CURRENT · 1 ACTIVATING · 2 NESTED · 3 STACK ·
    /// 4 TOP-LEVEL). Returns the §15.65.4-defined value, trimmed (a dynamic-length result — r1, no trailing
    /// spaces).</summary>
    public string Name(int kind)
    {
        var s = _stack;
        if (s.Count == 0) return " ";                        // no running element — a non-COBOL context (r3)
        Frame top = s[^1];
        switch (kind)
        {
            case 0:   // CURRENT — the outermost program of the currently running compilation unit (r7)
                return top.Outermost;
            case 1:   // ACTIVATING (r5/r6)
                if (top.IsMain) return " ";                  // a main program → a single space (r5)
                return s.Count >= 2 ? s[^2].Name : " ";      // the element that activated the current one
            case 2:   // NESTED — the most recently nested currently-running program (r8)
                for (int i = s.Count - 1; i >= 0; i--) if (s[i].IsNested) return s[i].Name;
                return top.Name;                             // arg-rule-1-violating build — implementor-defined (r3/r4)
            case 3:   // STACK (r9): CURRENT ; <activator chain, incl. TOP-LEVEL penultimate> ; <single space>.
            {         // Entries are RUNTIME ELEMENTS (compilation units), emitted by their outermost program-id
                      // (r7 CURRENT granularity) — a nested/contained program shares its unit's outermost id, so
                      // consecutive same-unit frames collapse to ONE entry (no MAIN;MAIN duplicate).
                var sb = new System.Text.StringBuilder();
                string? prev = null;
                for (int i = s.Count - 1; i >= 0; i--)
                {
                    if (s[i].Outermost == prev) continue;    // same compilation unit as the frame above
                    if (prev is not null) sb.Append(';');
                    sb.Append(s[i].Outermost);
                    prev = s[i].Outermost;
                }
                sb.Append("; ");                             // final entry = a single space (the operating environment)
                return sb.ToString();
            }
            case 4:   // TOP-LEVEL — the run-unit main (r10)
                return s[0].Name;
            default:
                return " ";
        }
    }
}
