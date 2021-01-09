﻿using System.Collections.Generic;
using System.IO;

public class GameBoyCartiridge
{
    enum MBCType : byte
    {
        NoMBC,
        MBC1,
        MBC1RAM,
        MBC1RAMBATT
    }

    private List<byte> romMemory;
    private List<byte> ramMemory;
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
        CartiridgeType = romMemory[0x147];
        RomSize = romMemory[0x148];
        RamSize = romMemory[0x149];
        switch(CartiridgeType) {
            case (byte)(MBCType.NoMBC):
                mbc = new GameBoyNoMBC(romMemory);
                break;
            case (byte)(MBCType.MBC1):
                mbc = new GameBoyMBC1(romMemory,ramMemory,RomSize,RamSize,false);
                break;
            case (byte)(MBCType.MBC1RAM):
                mbc = new GameBoyMBC1(romMemory,ramMemory,RomSize,RamSize,false);
                break;
            case (byte)(MBCType.MBC1RAMBATT):
                mbc = new GameBoyMBC1(romMemory,ramMemory,RomSize,RamSize,false);
                break;
        }
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
