using CobolSharp.Compiler.Semantics.Bound;
using CobolSharp.Runtime.Terminal;

namespace CobolSharp.Runtime.Screen;

public static class ScreenAttributeMapper
{
    public static TerminalAttributes GetAttributes(BoundScreenItem item)
    {
        return TerminalAttributes.None;
    }
}
