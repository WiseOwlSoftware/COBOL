// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// The run-unit module call-name stack backing FUNCTION MODULE-NAME (ISO §15.65). <see cref="ProgramRegistry"/>
/// pushes a frame at every activation boundary (<see cref="ProgramRegistry.RunMain"/> for the main program;
/// <see cref="ProgramRegistry.CallProgram"/> for every CALL / cross-assembly CALL / user-function reference /
/// nested-program CALL) and pops it on return, so the top of stack is always the currently running runtime
/// element. Each frame carries the element's own name, the outermost program-id of its compilation unit
/// (§15.65.4 r7 CURRENT), and whether it is a nested (contained) program (r8 NESTED). Thread-static: a run unit
/// executes on one thread; a new activation on a new thread starts with an empty stack.
///
/// OO method (INVOKE) activation is the one runtime element that does not funnel through CallProgram (methods are
/// not registry nodes). That is conformant, not residue: §15.65.4 r3/r4 make the returned form for method-id
/// elements implementor-defined, and the corpus MODULE-NAME hierarchy is the CALL/program tree (§15.65.1). The
/// extension point is the same single hook — when __CobolInvoke gains its activation entry it calls
/// <see cref="Push"/>(methodName, declaringClassName, isNested:false) / <see cref="Pop"/> around the body.
/// </summary>
public static class CobolModule
{
    private readonly record struct Frame(string Name, string Outermost, bool IsNested, bool IsMain);

    [ThreadStatic] private static List<Frame>? _stack;
    private static List<Frame> Stack => _stack ??= [];

    /// <summary>Push the run-unit main program (TOP-LEVEL, §15.65.4 r10; ACTIVATING → single space, r5).</summary>
    public static void PushMain(string name) => Stack.Add(new(name, name, IsNested: false, IsMain: true));

    /// <summary>Push a CALLed / referenced element (its name, its compilation unit's outermost program-id, and
    /// whether it is a contained/nested program).</summary>
    public static void Push(string name, string outermost, bool isNested)
        => Stack.Add(new(name, outermost, isNested, IsMain: false));

    /// <summary>Pop the current element on activation return (paired with every push in a finally).</summary>
    public static void Pop() { var s = Stack; if (s.Count > 0) s.RemoveAt(s.Count - 1); }

    /// <summary>Clear the stack at run-unit start (called from <see cref="ProgramRegistry.Reset"/>).</summary>
    public static void Reset() => _stack?.Clear();

    /// <summary>Resolve MODULE-NAME for the keyword selector (0 CURRENT · 1 ACTIVATING · 2 NESTED · 3 STACK ·
    /// 4 TOP-LEVEL). Returns the §15.65.4-defined value, trimmed (a dynamic-length result — r1, no trailing
    /// spaces).</summary>
    public static string Name(int kind)
    {
        var s = _stack;
        if (s is null || s.Count == 0) return " ";           // no running element — a non-COBOL context (r3)
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
