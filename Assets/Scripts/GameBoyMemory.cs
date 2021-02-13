using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class GameBoyMemory
{
    private uint memorySize = 0x10000;
    private ushort DMA = 0xFF46;
    private static byte joypadState = 0xFF; // All buttons are up
    private ushort biosSize;
    private List<byte> memory {get; set;}
    private GameBoyCartiridge gbCart;
    private GameBoyTimer gbTimer;
    private GameBoyGraphic gbGraphic;
    private GameBoyAudio gbAudio;
    public GameBoyMemory(GameBoyCartiridge gbCart) {
        this.gbCart = gbCart;
        memory = new List<byte>(new byte[memorySize]);
    }

    public void AddTimer(GameBoyTimer gbTimer) {
        this.gbTimer = gbTimer;
    }

    public void AddGraphics(GameBoyGraphic gbGraphic) {
        this.gbGraphic = gbGraphic;
    }

    public void AddAudio(GameBoyAudio gbAudio) {
        this.gbAudio = gbAudio;
    }

    public void LoadBios(string bios) {
        byte[] bytes = File.ReadAllBytes(bios);
        biosSize = (ushort)(bytes.Length);
        for(ushort b = 0x0; b < bytes.Length; b++) {
            memory[b] = bytes[b];
        }
    }

    public string PrintBios() {
        string result = "";
        for(ushort i = 0; i < biosSize; i++) {
            result = result + "address: " + "[" +  i + "] " + " 0x" + memory[i].ToString("X2") + " ";
        }
        return result.Substring(0, result.Length-1);
    }

    public string PrintMemory() {
        string result = "";
        for(int i = 0; i < memory.Count; i++) {
            result = result + "address: " + "[" +  i + "] " + " 0x" + memory[i].ToString("X2") + " ";
        }
        return result.Substring(0, result.Length-1);
    }

    public byte ReadFromMemory(ushort address) {
        if (address >= 0x0000 && address <= 0x7FFF) {
            // 0xFF50 (bios/bootstrap) is disabled if 0xA0
		    if (memory[0xFF50] == 0 && address < 0x100) {
			    return memory[address];
		    } else if(memory[0xFF50] == 0 && GameBoyCartiridge.IsGameBoyColor) {
                return memory[address];
            }
			return gbCart.Read(address);
		} else if (address >= 0x8000 && address <= 0x9FFF) {
		    return gbGraphic.Read(address);
	    }
        else if(address >= 0xA000 && address <= 0xBFFF) {
            return gbCart.Read(address);
        }
        else if(address >= 0xE000 && address <= 0xFDFF ) {
            //Debug.Log("Read from internal ram/ echo ram");
            return memory[address];
        // Joypad register
        } else if(address == 0xFF00) {
            return getJoyPadState();
        } else if(address >= 0xFF10 && address <= 0xFF3F){
            return gbAudio.Read(address);
        }
        return memory[address];
    }
    
    public bool WriteToMemory(ushort address, byte data) {
        if(address >= 0x0000 && address <= 0x7FFF) {
            gbCart.Write(address,data);
        } else if (address >= 0x8000 && address <= 0x9FFF) {
		    gbGraphic.Write(address,data);
	    } else if(address == GameBoyTimer.DIV) {
            uint rate = (byte)(memory[GameBoyTimer.TAC] & 0x3);
            byte divValue = memory[GameBoyTimer.DIV];
            //Failing Edge detector... don't know if this is correct
            if(rate == 0 && GameBoyCPU.getBitFromWord(9, gbTimer.TIMACycleCount) == 1) {
                gbTimer.IncrementTIMACheck();
            } else if(rate == 1 && GameBoyCPU.getBitFromWord(3, gbTimer.TIMACycleCount) == 1) {
                gbTimer.IncrementTIMACheck();
            } else if(rate == 2 && GameBoyCPU.getBitFromWord(5, gbTimer.TIMACycleCount) == 1) {
                gbTimer.IncrementTIMACheck();
            } else if(rate == 3 && GameBoyCPU.getBitFromWord(7, gbTimer.TIMACycleCount) == 1) {
                gbTimer.IncrementTIMACheck();
            }
            memory[GameBoyTimer.DIV] = 0;
            gbTimer.resetTimer();
        } else if(address == GameBoyTimer.TIMA) {
            //if(!gbTimer.isReloadingTIMA()) {
                memory[GameBoyTimer.TIMA] = data;
            //}
        } else if(address >= 0xA000 && address <= 0xBFFF) {
            gbCart.Write(address,data);
        } else if(address == GameBoyGraphic.LYAddr) {
            memory[address] = 0;
            gbGraphic.resetWindowLine();
        } else if(address == DMA){
            DMATransfer(data);
        } else if(address >= 0xE000 && address <= 0xFDFF) {
            //Debug.Log("Writing to internal ram/ echo ram");
            memory[address] = data;
        } else if(address >= 0xFF01 && address <= 0xFF02) {
            Debug.Log("Link Port");
        } else if(address >= 0xFF10 && address <= 0xFF3F) {
            gbAudio.Write(address,data);
        } else if(address == GameBoyGraphic.STATAddr) {
            memory[address] = (byte)((memory[address] & 0x7) | (data & 0xF8));
        } else {
            memory[address] = data;
        }
        return true;
    }

    public void WriteDirectly(ushort addr, byte data) {
        memory[addr] = data;
    }

    private void DMATransfer(byte data) {
        ushort address = (ushort)(data * 0x100);
        for(int i = 0; i < 0xA0; i++) {
            WriteToMemory((ushort)(0xFE00+i), ReadFromMemory((ushort)(address+i)));
        }
    }

    public bool IncrementReg(ushort address) {
        memory[address]++;
        return true;
    }

    private byte getJoyPadState() {
        byte res = memory[0xFF00] ;
        // flip all the bits
        res ^= 0xFF ;

        // are we interested in the standard buttons?
        if (GameBoyCPU.getBit(4, res) == 0)
        {
            byte topJoypad = (byte)(joypadState >> 4);
            topJoypad |= 0xF0 ; // turn the top 4 bits on
            res &= topJoypad ; // show what buttons are pressed
        }
        else if (GameBoyCPU.getBit(5, res) == 0)//directional buttons
        {
            byte bottomJoypad = (byte)(joypadState & 0xF);
            bottomJoypad |= 0xF0 ;
            res &= bottomJoypad;
        }
        return res;
    }

    public void SetJoyPadBit(byte key) {
        joypadState = GameBoyCPU.setBit(key, GameBoyMemory.joypadState);
    }

    public bool ResetJoyPadBit(byte key) {
        bool isHighToLow = (GameBoyCPU.getBit(5, memory[0xFF00]) == 1) ? true : false; 
        joypadState = GameBoyCPU.resetBit(key, GameBoyMemory.joypadState);
        return isHighToLow;
    }
}
