using System.Collections.Generic;

public class GameBoyNoMBC : GameBoyMBC {
    private List<byte> romMemory;
    
    public GameBoyNoMBC(List<byte> romMemory) {
        this.romMemory = romMemory;
    }
    
    public void Write(ushort PC, byte data) {
        romMemory[PC] = data;
    }

    public byte Read(ushort PC) {
        return romMemory[PC];
    }
}
