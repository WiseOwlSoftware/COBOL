namespace CobolSharp.Runtime.Terminal;

[System.Flags]
public enum TerminalAttributes
{
    None        = 0,
    Highlight   = 1 << 0,
    Lowlight    = 1 << 1,
    Reverse     = 1 << 2,
    Underline   = 1 << 3,
    Blink       = 1 << 4,
    Secure      = 1 << 5,
    ForegroundMask = 0x0F00,
    BackgroundMask = 0xF000
}
