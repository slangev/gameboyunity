public class GameBoyTimer
{
    GameBoyMemory gbMemory;
    GameBoyInterrupts gbInterrupts;
    private ushort DIVCycleCount = 0;
    private ushort TIMAReloadCounter = 0;
    private bool isReloading = false;
    public ushort TIMACycleCount {get;set;} = 0;

    // Divide/Timer Register address
    public static readonly ushort DIV = 0xFF04;
    public static readonly ushort TIMA = 0xFF05;
    public static readonly ushort TMA  = 0xFF06;
    public static readonly ushort TAC = 0xFF07;

    public GameBoyTimer(GameBoyMemory m, GameBoyInterrupts i) {
        gbMemory = m;
        gbInterrupts = i;
    }

    private ushort getClockRateFromTac() {
        ushort rate = (byte)(gbMemory.ReadFromMemory(TAC) & 0x3);
        return (ushort)(rate == (0) ? 1024 : rate == (1) ? 16 : rate == (2) ? 64 : rate == (3) ? 256 : 0);
    }

    public void resetTimer() {
        TIMACycleCount = 0;
    }

    public bool isReloadingTIMA() {
        return isReloading;
    }

    public void IncrementTIMACheck() {
        gbMemory.IncrementReg(TIMA);
        byte result = (byte)(gbMemory.ReadFromMemory(TIMA));

        if (result == 0) {
            gbMemory.WriteToMemory(TIMA, 0);
            TIMAReloadCounter = 4;
            isReloading = true;
        }
    }

    public void UpdateTimers(uint cycles) {
        DIVCycleCount += (ushort)(cycles);
	    while (DIVCycleCount >= 256) {
		    DIVCycleCount -= 256;
		    gbMemory.IncrementReg(DIV);
	    }
        bool tacEnabled = (byte)(gbMemory.ReadFromMemory(TAC) & 0x4) != 0;
        if(tacEnabled) {
            TIMACycleCount += (ushort)(cycles);
            TIMAReloadCounter -= (ushort)(cycles);
		    ushort clockRateNum = getClockRateFromTac();
            while (TIMACycleCount >= clockRateNum) {
			    TIMACycleCount -= clockRateNum;
                IncrementTIMACheck();
		    }
            if(TIMAReloadCounter <= 0 && isReloading) {
                isReloading = false;
                gbMemory.WriteToMemory(TIMA, (byte)(gbMemory.ReadFromMemory(TMA)));
                gbInterrupts.RequestInterrupt(GameBoyInterrupts.TimerOverflowBit);
            }
        }
    }
}
