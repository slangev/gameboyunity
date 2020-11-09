using System.Collections.Generic;
using System.IO;

public class GameBoyCartiridge
{
    public List<byte> romMemory;
    public string Title {get;set;}
    public byte IsGameBoyColor {get;set;}
    public byte highNibble {get;set;}
    public byte lowNibble {get;set;}
    public byte CartiridgeType {get;set;}
    public byte Indicator {get;set;}
    public byte RomSize {get;set;}
    public byte RamSize {get;set;}
    public byte IsNonJapanese {get;set;}
    public byte LicenseeCode {get;set;}
    public byte MaskRomVersionNumber {get;set;}

    public GameBoyCartiridge(uint size) {
        romMemory = new List<byte>(new byte[size]);
    }

    public void Write(ushort pc, byte data) {
        romMemory[pc] = data;
    }

    public byte Read(ushort pc) {
        return romMemory[pc];
    }

    public void LoadRom(string rom) {
        byte[] bytes = File.ReadAllBytes(rom);
        romMemory = new List<byte>(bytes);
        for(ushort b = 0x134; b < 0x143; b++) {
            Title = Title + (char)(romMemory[b]);
        }
        CartiridgeType = romMemory[0x147];
        RomSize = romMemory[0x148];
        IsNonJapanese = romMemory[0x014A];
    }

    public string DumpRom() {
        string result = "";
        for(int i = 0; i < romMemory.Count; i++) {
            result = result + "Pos: " + "[" + i + "] " + " 0x" + romMemory[i].ToString("X2") + " ";
        }
        return result.Substring(0,result.Length-1);
    }
}
