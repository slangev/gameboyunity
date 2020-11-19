using UnityEngine;

public class GameBoyInterrupts
{
    // Interrupt flags
    bool IME = false; // Interrupt Master Enabled (flag)
    bool EI = false; // enable interrupt
    bool DI = false; // disable interrupt
    public static readonly ushort IF = 0xFF0F;
    public static readonly ushort IE = 0xFFFF; 
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
