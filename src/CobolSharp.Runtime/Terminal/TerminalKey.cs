namespace CobolSharp.Runtime.Terminal;

public enum TerminalKey
{
    None,
    Character,
    Enter,
    Escape,
    Tab,
    Backspace,
    Up,
    Down,
    Left,
    Right,
    Function1,
    Function2,
    Function3,
    Function4,
    Function5,
    Function6,
    Function7,
    Function8,
    Function9,
    Function10,
    Function11,
    Function12
}

public readonly struct TerminalKeyEvent
{
    public TerminalKey Key { get; }
    public char? Payload { get; }

    public TerminalKeyEvent(TerminalKey key, char? payload = null)
    {
        Key = key;
        Payload = payload;
    }
}
