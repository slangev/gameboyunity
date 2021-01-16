using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using System.IO;
using System;
using UnityEngine;

public class GameBoyMBC3 : GameBoyMBC {
    private List<byte> romMemory;
    private List<byte> ramMemory;
    private byte romBankSize;
    private byte ramBankSize;
    private uint romSize;
    private uint ramSize;
    private bool ramEnable = false;
    private byte bank1 = 1;
    private byte bank2 = 0;
    private uint ROM_BANK_SIZE = 0x4000;
    private uint RAM_BANK_SIZE = 0x2000;
    private bool battery = false;
    private string fileName;
    
    public GameBoyMBC3(List<byte> romMemory, List<byte> ramMemory, uint romSize, uint ramSize, bool multicart, bool battery) {
        this.romMemory = romMemory;
        this.ramMemory = ramMemory;
        this.romSize = romSize;
        this.ramSize = ramSize;
        this.battery = battery;
        this.fileName = Application.persistentDataPath + getFileName();
        load();
        ramEnable = false;
        romBankSize = setRomBankSize(this.romSize);
        ramBankSize = setRamBankSize(this.ramSize);
    }

    ~GameBoyMBC3() {
       save();
    }

    private string getFileName() {
        string t = "";
        try {
            t = Regex.Replace(GameBoyCartiridge.Title, @"[^\w\.@-]", "", RegexOptions.None, TimeSpan.FromSeconds(1.5));
        }
        // If we timeout when replacing invalid characters,
        // we should return Empty.
        catch (RegexMatchTimeoutException) {
            t = "";
        }
        return t;
    }

    private byte setRamBankSize(uint ramSize) {
        byte result = 0;
        switch(ramSize) {
            case 1:
                result = 1;
                break;
            case 2:
                result = 1;
                break;
            case 3:
                result = 4;
                break;
            case 4:
                result = 16;
                break;
            default:
                break;
        }
        return result;
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

    private (uint,uint) rom_offsets(bool multicart) {
        uint lower_bank = 0b00;
        uint upper_bank = (uint)(bank1);
        return ((ROM_BANK_SIZE * lower_bank),(ROM_BANK_SIZE * upper_bank));
    }

    private uint ram_offset() {
        return RAM_BANK_SIZE * bank2;
    }

    private uint ram_addr(ushort addr) {
        return (uint)((ram_offset() | (uint)((addr & 0x1FFF))) & (ramMemory.Count - 1));
    }

    private void write_ram(ushort PC, byte data) {
        if(ramSize != 0) {
            uint addr = ram_addr(PC);
            ramMemory[(int)(addr)] = data;
        }
    }

    private byte read_ram(ushort PC) {
        if(ramSize != 0) {
            uint addr = ram_addr(PC);
            return ramMemory[(int)(addr)];
        }
        return 0xFF; //Default value
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
            bank1 = ((byte)(data & 0x7F) == 0) ? (byte)(1) : (byte)(data & 0x7F);
        }
        // RAM BANK NUMBER(32kB Ram carts only) or Upper Bits of ROM Bank Number
        else if(PC >= 0x4000 && PC <= 0x5FFF) { 
            bank2 = (byte)(data & 0b11);
        }

        //Latch Clock Data
        else if(PC >= 0x6000 && PC <= 0x7FFF) {
        } 
        else if(PC >= 0xA000 && PC <= 0xBFFF) {
            if(ramEnable) {
                write_ram(PC,data);
            }
        }
    }

    public byte Read(ushort PC) {
        //ROM BANK 00
        if(PC >= 0x0000 && PC <= 0x3FFF) {
            var romBanks = rom_offsets(false);
            uint rom_lower = romBanks.Item1;
            //Maybe pain point
            int index = (int)(((rom_lower | (uint)((PC & 0x3fff))) & (romMemory.Count - 1)));
            return romMemory[index];
        }
        // ROM BANK 01-7F 
        else if(PC >= 0x4000 && PC <= 0x7FFF) {
            var romBanks = rom_offsets(false);
            uint rom_upper = romBanks.Item2;
            //Maybe pain point
            int index = (int)(((rom_upper | (uint)((PC & 0x3fff))) & (romMemory.Count - 1)));
            return romMemory[index];
        }
        // RAM BANK 00-03 
        else if(PC >= 0xA000 && PC <= 0xBFFF) {
            if(ramEnable) {
                return read_ram(PC);
            }
        }
        //Default value from read. 
        return 0xFF;
    }

    private void save() {
        if(battery) {
            string t = "";
            try {
                t = Regex.Replace(GameBoyCartiridge.Title, @"[^\w\.@-]", "",
                                RegexOptions.None, TimeSpan.FromSeconds(1.5));
            }
            // If we timeout when replacing invalid characters,
            // we should return Empty.
            catch (RegexMatchTimeoutException) {
                t = "";
            }
            
            try {
                FileStream fs = new FileStream(fileName, FileMode.Create);
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(fs, ramMemory);
                Debug.Log("Done saving");
                fs.Close();
            } catch (Exception e) {
                Debug.Log("Error: " + e.ToString());
            } 
        }
    }

    private void load() {
        if(battery) {
            if (File.Exists(fileName)) {
                Debug.Log("Found file");
                FileStream fs = new FileStream(fileName, FileMode.Open);
                BinaryFormatter bf = new BinaryFormatter();
                try {
                    ramMemory = (List<byte>)bf.Deserialize(fs);
                }
                catch {
                    Debug.Log("Failed to deserialize game files");
                }
                finally {
                    fs.Close();
                }
            }
        }
    }
}