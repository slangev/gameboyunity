﻿using System.Collections.Generic;
using System;
using UnityEngine;

public class GameBoyMBC1 : GameBoyMBC {
    private List<byte> romMemory;
    private List<byte> ramMemory;
    private bool ramEnable;
    private byte romBankNumber;
    private byte romBankSize;
    private byte ramBankNumber;
    private byte mode;
    private uint romSize;
    private uint ramSize;
    private byte higherRomBankBits = 0;
    
    public GameBoyMBC1(List<byte> romMemory, List<byte> ramMemory, uint romSize, uint ramSize) {
        this.romMemory = romMemory;
        this.ramMemory = ramMemory;
        this.romSize = romSize;
        this.ramSize = ramSize;
        ramEnable = false;
        romBankNumber = 1;
        romBankSize = setRomBankSize(this.romSize);
        ramBankNumber = 0;
        mode = 0;
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
        //RAM Enabled
        if(PC >= 0x0000 && PC <= 0x1FFF) {
            // Practically any value with 0Ah in the lower 4 bits enables RAM and any other value disables RAM.
            byte mask = (byte)(data & 0xF);
            ramEnable = (mask == 0xA) ? true : false;
        } 
        //ROM BANK Number Write
        else if(PC >= 0x2000 && PC <= 0x3FFF) {
            // When 00h is written, the MBC translates that to bank 01h. 
            // The same happens for banks 20h, 40h, and 60h, as this register would need to be 00h for those addresses. 
            // Any attempt to address these ROM Banks will select Bank 21h, 41h and 61h instead.
            romBankNumber = (byte)(data & 0x1F);
            if(romBankNumber == 0x00 || romBankNumber == 0x20 || romBankNumber == 0x40 || romBankNumber == 0x60) {
                romBankNumber++;
            }
            romBankNumber &= (byte)(romBankSize-1);
        }
        // RAM BANK NUMBER(32kB Ram carts only) or Upper Bits of ROM Bank Number
        else if(PC >= 0x4000 && PC <= 0x5FFF) {
            if(romSize == 0) {
                ramBankNumber = 0;
            }
            else if(ramSize == 3) {
                //32kB Ram carts
                ramBankNumber = (byte)(data & 0x03);
            } else {
                // Upper Bits off Rom Bank Number
            }
        }
        // Banking Mode Select This 1bit Register selects between the two MBC1 banking modes, 
        // controlling the behaviour of the secondary 2 bit banking register (above)
        else if(PC >= 0x6000 && PC <= 0x7FFF) {
            byte mask = (byte)(data & 0x1);
            mode = mask;
        } 
        else if(PC >= 0xA000 && PC <= 0xBFFF) {
            if(ramEnable) {
                if(mode == 0) {
                    ushort newAddress = (ushort)(PC - 0xA000);
                    ramMemory[newAddress + (ramBankNumber*0x2000)] = data;
                } else if(mode == 1) {
                    Debug.Log("Writing to RAM mode 1");
                }
            }
        }
    }

    public byte Read(ushort PC) {
        //ROM BANK 00/20/40/60
        if(PC >= 0x0000 && PC <= 0x3FFF) {
            if(mode == 0) {
                //Should be zero for smaller carts 20/40/60 and only be accessed by mode 1
                return romMemory[(int)(PC)];
            } else if(mode == 1) {
                //Debug.LogWarning("Reading from Bank 0x0000-0x3FFF mode1");
                //FIXME
                return romMemory[(int)(PC)];
            } else {
                Debug.LogWarning("Bad Mode");
                return 0xFF;
            }
        }
        // ROM BANK 01-7F 
        else if(PC >= 0x4000 && PC <= 0x7FFF) {
            ushort newAddress = (ushort)(PC - 0x4000);
            return romMemory[newAddress + (romBankNumber*0x4000)] ;
        }
        // RAM BANK 00-03 
        else if(PC >= 0xA000 && PC <= 0xBFFF) {
            if(ramEnable) {
                if(mode == 0) {
                    ushort newAddress = (ushort)(PC - 0xA000);
                    return ramMemory[newAddress + (ramBankNumber*0xA000)];
                } else if(mode == 1) {
                    Debug.Log("Reading from RAM mode 1");
                }
            }
        }
        //Default value from read. 
        return 0xFF;
    }
}