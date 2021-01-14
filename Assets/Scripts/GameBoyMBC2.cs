using System.Collections.Generic;
using UnityEngine;

public class GameBoyMBC2 : GameBoyMBC
{
    private List<byte> romMemory;
    private List<byte> ramMemory;
    private byte romBankSize;
    private byte ramBankSize;
    private uint romSize;
    private bool ramEnable = false;
    private byte bank1 = 1;
    private uint ROM_BANK_SIZE = 0x4000;
    private bool battery;
    public GameBoyMBC2(List<byte> romMemory, List<byte> ramMemory, uint romSize, bool battery) {
        this.romMemory = romMemory;
        this.ramMemory = ramMemory;
        this.romSize = romSize;
        this.battery = battery;
        ramEnable = false;
        romBankSize = setRomBankSize(this.romSize);
    }
    
    private byte setRomBankSize(uint romSize) {
        byte result = 0;
        switch(romSize) {
            case 0:
                result = 2;
                break;
            case 1:
                result = 4;
                break;
            case 2:
                result = 8;
                break;
            case 3:
                result = 16;
                break;
            case 4:
                result = 32;
                break;
            case 5:
                result = 64;
                break;
            default:
                break;
        }
        return result;
    }

    public void Write(ushort PC, byte data) {
        //RAM Enabled and ROM Bank Number
        if(PC >= 0x0000 && PC <= 0x3FFF) {
            byte bit8 = (byte)(GameBoyCPU.getBitFromWord(8,PC));
            if(bit8 != 1) {
                byte mask = (byte)(data & 0x0F);
                bool prevRamState = ramEnable;
                ramEnable = (mask == 0x0A) ? true : false;
                //Went from true to false
                if(prevRamState && !ramEnable && battery) {
                    save();
                }
            } else {
                bank1 = (byte)(data & 0x0F);
                if(bank1 == 0) {
                    bank1 = 1;
                }
            }
        } else if(PC >= 0xA000 && PC <= 0xBFFF) {
            if(ramEnable) {
                byte newData = (byte)(data & 0x0F);
                int offset = PC - 0xA000;
                ramMemory[offset % 512] = data;
            }
        }
    }

    public byte Read(ushort PC) {
        if(PC >= 0x0000 && PC <= 0x3FFF) {
            int index = (int)(((0x0000 | (uint)((PC & 0x3fff))) & (romMemory.Count - 1)));
            return romMemory[index];
        } else if(PC >= 0x4000 && PC <= 0x7FFF) {
            uint bankNumber = (ROM_BANK_SIZE * bank1);
            int index = (int)(((bankNumber | (uint)((PC & 0x3fff))) & (romMemory.Count - 1)));
            return romMemory[index];
        } else if(PC >= 0xA000 && PC <= 0xBFFF) {
            if(ramEnable) {
                int offset = PC - 0xA000;
                return (byte)((0xFF & 0xF0) | (byte)(ramMemory[offset % 512] & 0x0F)); // F is undefined for Upper 4 bits. So Always return 0xFX where X is the lower 4 bits
            }
        } 
        return 0xFF;
    }

    private void save() {
        Debug.Log("HERE");
    }

    private void restoreRam() {
        Debug.Log("restoreRam");
    }
}
