﻿using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using System.IO;
using System;
using UnityEngine;

public class GameBoyMBC3 : GameBoyMBC {
    private List<byte> romMemory;
    private List<byte> ramMemory;
    private List<byte> RTCRegisters;
    private List<byte> LatchRegisters;
    DateTime start;
    private byte romBankSize;
    private byte ramBankSize;
    private uint romSize;
    private uint ramSize;
    private bool ramEnable = false;
    private bool RTCEnable = false;
    private bool ramTimerEnable = false;
    private byte bank1 = 1;
    private byte bank2 = 0;
    private byte RTCSelect = 0;
    private uint ROM_BANK_SIZE = 0x4000;
    private uint RAM_BANK_SIZE = 0x2000;
    private bool battery = false;
    private bool timer = false;
    private bool latched = false;
    private string fileName;
    
    public GameBoyMBC3(List<byte> romMemory, List<byte> ramMemory, uint romSize, uint ramSize, bool multicart, bool battery, bool timer) {
        this.romMemory = romMemory;
        this.ramMemory = ramMemory;
        this.romSize = romSize;
        this.ramSize = ramSize;
        this.battery = battery;
        this.timer = timer;
        this.fileName = Application.persistentDataPath + getFileName();
        RTCRegisters = initializeRTC();
        LatchRegisters = initializeRTC();
        load();
        ramEnable = false;
        romBankSize = setRomBankSize(this.romSize);
        ramBankSize = setRamBankSize(this.ramSize);
        //TODO INITIALIZE RTC REGISTER 0C default value is 0x3E
        /*
            08h  RTC S   Seconds   0-59 (0-3Bh)
            09h  RTC M   Minutes   0-59 (0-3Bh)
            0Ah  RTC H   Hours     0-23 (0-17h)
            0Bh  RTC DL  Lower 8 bits of Day Counter (0-FFh)
            0Ch  RTC DH  Upper 1 bit of Day Counter, Carry Bit, Halt Flag
                Bit 0  Most significant bit of Day Counter (Bit 8)
                Bit 6  Halt (0=Active, 1=Stop Timer)
                Bit 7  Day Counter Carry Bit (1=Counter Overflow)
        */
    }

    ~GameBoyMBC3() {
       save();
    }

    private List<byte> initializeRTC() {
        List<byte> result = new List<byte>();
        byte numOfRTCRegs = 5;
        for(int i = 0; i < numOfRTCRegs; i++) {
            result.Add(0);
        }
        return result;
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
        uint lower_bank = 0x0;
        uint upper_bank = bank1;
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
            ramTimerEnable = (mask == 0xA) ? true : false;
        } 
        //ROM BANK Number Write
        else if(PC >= 0x2000 && PC <= 0x3FFF) {
            bank1 = ((byte)(data & 0x7F) == 0) ? (byte)(1) : (byte)(data & 0x7F);
        }
        // RAM BANK NUMBER(32kB Ram carts only) or Upper Bits of ROM Bank Number
        else if(PC >= 0x4000 && PC <= 0x5FFF) {
            // Ram bank number 
            if(data >= 0x00 && data <= 0x03) {
                bank2 = (byte)(data & 0b11);
                ramEnable = true;
                RTCEnable = false;
            // RTC Register Select
            } else if(data >= 0x08 && data <= 0x0C) {
                RTCSelect = data;
                ramEnable = false;
                RTCEnable = true;
            }
        }

        //Latch Clock Data
        else if(PC >= 0x6000 && PC <= 0x7FFF) {
            updateTimer();
            if(timer && (byte)(data & 0x01) == 0x01 && !latched) {
                latchTimer();
            }
            latched = (byte)(data & 0x01) == 0x01;
        } 

        else if(PC >= 0xA000 && PC <= 0xBFFF) {
            if(ramTimerEnable) {
                if(RTCEnable && timer) {
                    // write to latched AND real register with value
                    updateTimer();
                    if(RTCSelect == 0x08) {
                        updateSeconds(data);
                        LatchRegisters[0] = data;
                    } else if(RTCSelect == 0x09) {
                        updateMinutes(data);
                        LatchRegisters[1] = data;
                    } else if(RTCSelect == 0x0A) {
                        updateHours(data);
                        LatchRegisters[2] = data;
                    } else if(RTCSelect == 0x0B) {
                        updateDays(data);
                        LatchRegisters[3] = data;
                    } else if(RTCSelect == 0x0C) {
                        updateControl(data);
                    }
                } else if(ramEnable) {
                    write_ram(PC,data);
                } 
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
            if(ramTimerEnable) {
                updateTimer();
                if(RTCEnable && !latched) {
                    return RTCRegisters[RTCSelect-8];
                } else if(RTCEnable && latched) {
                    return LatchRegisters[RTCSelect-8];
                }
                if(ramEnable) {
                    return read_ram(PC);
                } 
            }
        }
        //Default value from read. 
        return 0xFF;
    }

    private void updateTimer() {
        if(GameBoyCPU.getBit(6,RTCRegisters[4]) == 0) {
            DateTime curr = System.DateTime.Now;
            TimeSpan currSpan = curr - start;
            RTCRegisters[0] = (byte)(currSpan.Seconds);
            RTCRegisters[1] = (byte)(currSpan.Minutes);
            RTCRegisters[2] = (byte)(currSpan.Hours);
            RTCRegisters[3] = (byte)(currSpan.Days);
            if(currSpan.Days > 255) {
                //Rollover
                RTCRegisters[4] = GameBoyCPU.setBit(0,RTCRegisters[4]);
            }
            if(currSpan.Days > 511) {
                //Set overflow bit.
                RTCRegisters[4] = GameBoyCPU.setBit(7,RTCRegisters[4]);
                //Unset rollover
                RTCRegisters[4] = GameBoyCPU.resetBit(0,RTCRegisters[4]);
                start = start.AddDays(512);
            }
        }
    }

    private void updateSeconds(byte secondReset) {
        DateTime e = System.DateTime.Now;
        TimeSpan diff = e - start;
        if(secondReset < diff.Seconds) {
			byte diffSeconds = (byte)(diff.Seconds - secondReset);
			start = start.AddSeconds(diffSeconds);
		} else if(secondReset > diff.Seconds) {
			byte diffSeconds = (byte)(secondReset - diff.Seconds);
			start = start.AddSeconds(-diffSeconds);
		}
    }

    private void updateMinutes(byte minutesReset) {
        DateTime e = System.DateTime.Now;
        TimeSpan diff = e - start;
        if(minutesReset < diff.Minutes) {
			byte diffMinutes = (byte)(diff.Minutes - minutesReset);
			start = start.AddMinutes(diffMinutes);
		} else if(minutesReset > diff.Minutes) {
			byte diffMinutes = (byte)(minutesReset - diff.Minutes);
			start = start.AddMinutes(-diffMinutes);
		}
    }

    private void updateHours(byte hoursReset) {
        DateTime e = System.DateTime.Now;
        TimeSpan diff = e - start;
        if(hoursReset < diff.Hours) {
			byte diffHours = (byte)(diff.Hours - hoursReset);
			start = start.AddHours(diffHours);
		} else if(hoursReset > diff.Hours) {
			byte diffHours = (byte)(hoursReset - diff.Hours);
			start = start.AddHours(-diffHours);
		}
    }

    private void updateDays(ushort dayReset) {
        DateTime e = System.DateTime.Now;
        TimeSpan diff = e - start;
        if(GameBoyCPU.getBit(0,RTCRegisters[4]) == 1) {
            dayReset += 256;
        }
        if(dayReset < diff.Days) {
			ushort diffDays = (ushort)(diff.Days - dayReset);
			start = start.AddDays(diffDays);
		} else if(dayReset > diff.Days) {
			ushort diffDays = (ushort)(dayReset - diff.Days);
			start = start.AddDays(-diffDays);
		}
    }

    private void updateControl(byte data) {
        DateTime e = System.DateTime.Now;
        TimeSpan diff = e - start;
        byte dayBit = GameBoyCPU.getBit(0,RTCRegisters[4]);
        if(dayBit == 1 && (data & 0x1) == 0) {
			start = start.AddDays(256);
        } else if(dayBit == 0 && (data & 0x1) == 1) {
			start = start.AddDays(-256);
        }
        LatchRegisters[4] = RTCRegisters[4] = data;
    }

    private void latchTimer() {
        LatchRegisters[0] = RTCRegisters[0];
        LatchRegisters[1] = RTCRegisters[1];
        LatchRegisters[2] = RTCRegisters[2];
        LatchRegisters[3] = RTCRegisters[3];
        LatchRegisters[4] = RTCRegisters[4];
    }

    private List<byte> saveTimeData() {
        List<byte> result = new List<byte>();
        int year = start.Year;
        byte lowByte = (byte)(year & 0x00FF);
        byte lowerByte = (byte)((year & 0xFF00) >> 8);
        byte highByte = (byte)((year & 0xFF0000) >> 16);
        byte higherByte = (byte)((year & 0xFF000000) >> 24);
        result.Add(lowByte);
        result.Add(lowerByte);
        result.Add(highByte);
        result.Add(higherByte);
        byte month = (byte)(start.Month);
        byte day = (byte)(start.Day);
        byte hour = (byte)(start.Hour);
        byte minutes = (byte)(start.Minute);
        byte seconds = (byte)(start.Second);
        result.Add(month);
        result.Add(day);
        result.Add(hour);
        result.Add(minutes);
        result.Add(seconds);
        return result;
    }

    private void loadTimeData(List<byte> dataForStart) {
        int year = 0;
        year = (year | dataForStart[3]) << 24;
        year = (year | dataForStart[2]) << 16;
        year = (year | dataForStart[1]) << 8;
        year = (year | dataForStart[0]);
        int month = dataForStart[4];
        int day = dataForStart[5];
        int hour = dataForStart[6];
        int minutes = dataForStart[7];
        int seconds = dataForStart[8];
        //Debug.Log("YEAR: " + year + " month: " + month + " day: " + day + " hour: " + hour + " minutes: " + minutes + " seconds: " + seconds);
        start = new DateTime(year,month,day,hour,minutes,seconds); //Need MS
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
                List<List<byte>> savedData = new List<List<byte>>();
                savedData.Add(ramMemory);
                if(timer) {
                    savedData.Add(RTCRegisters);
                    List<byte> startTimerData = saveTimeData();
                    savedData.Add(startTimerData);
                }
                bf.Serialize(fs, savedData);
                fs.Close();
            } catch (Exception e) {
                Debug.Log("Error: " + e.ToString() + " filename " + fileName);
            } 
        }
    }

    private void load() {
        if(battery) {
            if (File.Exists(fileName)) {
                FileStream fs = new FileStream(fileName, FileMode.Open);
                BinaryFormatter bf = new BinaryFormatter();
                try {
                    List<List<byte>> savedData = (List<List<byte>>)bf.Deserialize(fs);
                    ramMemory = savedData[0];
                    if(timer) {
                        RTCRegisters = savedData[1];
                        start = System.DateTime.Now;
                        loadTimeData(savedData[2]);
                    }
                }
                catch (Exception e){
                    Debug.Log("Failed to deserialize game files: " + e);
                }
                finally {
                    fs.Close();
                }
            } else {
                if(this.timer)
                start = System.DateTime.Now;
            }
        }
    }
}