using System.Collections.Generic;
using System.IO;

public class GameBoyCartiridge
{
    enum MBCType : byte
    {
        NoMBC = 0,
        MBC1 = 1,
        MBC1RAM = 2,
        MBC1RAMBATT = 3,
        MBC2 = 5,
        MBC2BATT = 6,
        RAM = 8,
        RAMBATT = 9,
        MBC3RAMTIMERBATT = 0x10,
        MBC3TIMERBATT = 0xF,
        MBC3RAMBATT = 19,
        MBC5 = 0x19,
        MBC5RAMBATT = 0x1B
    }

    private List<byte> romMemory;
    private List<byte> ramMemory;
    public static string Title {get;set;}
    public static bool IsGameBoyColor {get;set;}
    public byte highNibble {get;set;}
    public byte lowNibble {get;set;}
    public byte CartiridgeType {get;set;}
    public byte Indicator {get;set;}
    public byte RomSize {get;set;}
    public byte RamSize {get;set;}
    public byte IsNonJapanese {get;set;}
    public byte LicenseeCode {get;set;}
    public byte MaskRomVersionNumber {get;set;}
    private GameBoyMBC mbc;
    private static readonly uint maxRamSize = 0x8000;

    public GameBoyCartiridge(uint size) {
        romMemory = new List<byte>(new byte[size]);
        ramMemory = new List<byte>(new byte[maxRamSize]);
    }

    public void Write(ushort PC, byte data) {
        mbc.Write(PC, data);
    }

    public byte Read(ushort PC) {
        return mbc.Read(PC);
    }

    public void LoadRom(string rom) {
        byte[] bytes = File.ReadAllBytes(rom);
        romMemory = new List<byte>(bytes);
        for(ushort b = 0x134; b < 0x143; b++) {
            Title = Title + (char)(romMemory[b]);
        }
        IsGameBoyColor = romMemory[0x143] == 0x80 || romMemory[0x143] == 0xC0;
        CartiridgeType = romMemory[0x147];
        RomSize = romMemory[0x148];
        RamSize = romMemory[0x149];
        IsNonJapanese = romMemory[0x014A];
        switch(CartiridgeType) {
            case (byte)(MBCType.NoMBC):
                mbc = new GameBoyNoMBC(romMemory);
                break;
            case (byte)(MBCType.MBC1):
                mbc = new GameBoyMBC1(romMemory,ramMemory,RomSize,RamSize,false,false);
                break;
            case (byte)(MBCType.MBC1RAM):
                mbc = new GameBoyMBC1(romMemory,ramMemory,RomSize,RamSize,false,false);
                break;
            case (byte)(MBCType.MBC1RAMBATT):
                mbc = new GameBoyMBC1(romMemory,ramMemory,RomSize,RamSize,false,true);
                break;
            case (byte)(MBCType.MBC2):
                mbc = new GameBoyMBC2(romMemory,ramMemory,RomSize,false);
                break;
            case (byte)(MBCType.MBC2BATT):
                mbc = new GameBoyMBC2(romMemory,ramMemory,RomSize,true);
                break;
            case (byte)(MBCType.MBC3RAMBATT):
                mbc = new GameBoyMBC3(romMemory,ramMemory,RomSize,RamSize,false,true,false);
                break;
            case (byte)(MBCType.MBC3TIMERBATT):
                mbc = new GameBoyMBC3(romMemory,ramMemory,RomSize,RamSize,false,true,true);
                break;
            case (byte)(MBCType.MBC3RAMTIMERBATT):
                mbc = new GameBoyMBC3(romMemory,ramMemory,RomSize,RamSize,false,true,true);
                break;
            case (byte)(MBCType.MBC5):
                mbc = new GameBoyMBC5(romMemory,ramMemory,RomSize,RamSize,false,false);
                break;
            case (byte)(MBCType.MBC5RAMBATT):
                mbc = new GameBoyMBC5(romMemory,ramMemory,RomSize,RamSize,false,true);
                break;
        }
    }

    public string DumpRom() {
        string result = "";
        for(int i = 0; i < romMemory.Count; i++) {
            result = result + "Pos: " + "[" + i + "] " + " 0x" + romMemory[i].ToString("X2") + " ";
        }
        return result.Substring(0,result.Length-1);
    }
}
