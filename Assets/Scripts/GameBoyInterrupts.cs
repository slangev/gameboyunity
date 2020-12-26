public class GameBoyInterrupts
{
    // Interrupt flags
    public static bool IMEFlag = false; // Interrupt Master Enabled (flag)
    public static bool IMEHold = false; // Interrupt Master Enabled (flag) Hold
    public static bool EIDIFlag = false; // enable/disable interrupt flag
    public static readonly ushort IF = 0xFF0F;
    public static readonly ushort IE = 0xFFFF; 

    public static readonly byte VBlankBit = 0;
    public static readonly byte LDDCBit = 1;
    public static readonly byte TimerOverflowBit = 2;

    private GameBoyMemory memory;

    public GameBoyInterrupts(GameBoyMemory m) {
        memory = m;
    }

    public void RequestInterrupt(byte bit) {
        byte ifReg = memory.ReadFromMemory(IF);
        memory.WriteToMemory(IF,GameBoyCPU.setBit(bit,ifReg));
    }
}
