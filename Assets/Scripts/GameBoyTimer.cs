public class GameBoyTimer
{
    GameBoyMemory gbMemory;
    GameBoyInterrupts gbInterrupts;
    private uint DIVCycleCount = 0;
    private uint TIMACycleCount = 0;

    // Divide/Timer Register address
    public static readonly ushort DIV = 0xFF04;
    public static readonly ushort TIMA = 0xFF05;
    public static readonly ushort TMA  = 0xFF06;
    public static readonly ushort TAC = 0xFF07;

    public GameBoyTimer(GameBoyMemory m, GameBoyInterrupts i) {
        gbMemory = m;
        gbInterrupts = i;
    }

    private uint getClockRateFromTac() {
        uint rate = (byte)(gbMemory.ReadFromMemory(TAC) & 0x3);
        return (uint)(rate == (0) ? 1024 : rate == (1) ? 16 : rate == (2) ? 64 : rate == (3) ? 256 : 0);
    }

    public void resetTimer() {
        TIMACycleCount = 0;
    }

    public void UpdateTimers(uint cycles) {
        DIVCycleCount += cycles;
	    if (DIVCycleCount >= 256) {
		    DIVCycleCount -= 256;
		    gbMemory.IncrementReg(DIV);
	    }
        bool tacEnabled = (byte)(gbMemory.ReadFromMemory(TAC) & 0x4) != 0;
        if(tacEnabled) {
            TIMACycleCount += cycles;
		    uint clockRateNum = getClockRateFromTac();
            while (TIMACycleCount >= clockRateNum) {
			    TIMACycleCount -= clockRateNum;
                gbMemory.IncrementReg(TIMA);
			    byte result = (byte)(gbMemory.ReadFromMemory(TIMA));

			    if (result == 0) {
				    gbMemory.WriteToMemory(TIMA, (byte)(gbMemory.ReadFromMemory(TMA)));
                    gbInterrupts.RequestInterrupt(GameBoyInterrupts.TimerOverflowBit);
			    }
		    }
        }
    }
}
