using CobolSharp.Compiler.Semantics;

namespace CobolSharp.Runtime.Terminal;

public static class CursorCodec
{
    public static (int Row, int Col) LoadCursorPosition(
        DataSymbol cursorSymbol,
        byte[] storageArea,
        int offset,
        int length)
    {
        return (1, 1);
    }

    public static void StoreCursorPosition(
        DataSymbol cursorSymbol,
        byte[] storageArea,
        int offset,
        int length,
        int row,
        int col)
    {
    }
}
