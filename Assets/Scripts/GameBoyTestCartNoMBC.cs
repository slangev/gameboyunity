using System.Collections.Generic;
// This file is used for testing only. 

public class GameBoyTestCartNoMBC : GameBoyMBC {
    private List<byte> romMemory;
    
    public GameBoyTestCartNoMBC(List<byte> romMemory) {
        this.romMemory = romMemory;
    }
    
    public void Write(ushort PC, byte data) {
        romMemory[PC] = data;
    }

    public byte Read(ushort PC) {
        return romMemory[PC & 0x7FFF];
    }
}
