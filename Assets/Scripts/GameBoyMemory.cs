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
            result = result + "Pos: " + "[" +  i + "] " + " 0x" + memory[i].ToString("X2") + " ";
        }
        return result.Substring(0, result.Length-1);
    }

    public string PrintMemory() {
        string result = "";
        for(int i = 0; i < memory.Count; i++) {
            result = result + "Pos: " + "[" +  i + "] " + " 0x" + memory[i].ToString("X2") + " ";
        }
        return result.Substring(0, result.Length-1);
    }

    public byte ReadFromMemory(ushort pos) {
        if (pos >= 0x0000 && pos <= 0x7FFF) {
            // 0xFF50 (bios/bootstrap) is disabled if 0xA0
		    if (memory[0xFF50] == 0 && pos < 0x100) {
			    return memory[pos];
		    }
			return gbCart.Read(pos);
		} 
        else if(pos >= 0xA000 && pos <= 0xBFFF) {
            return gbCart.Read(pos);
        }
        else if(pos >= 0xE000 && pos <= 0xFDFF ) {
            //Debug.Log("Read from internal ram/ echo ram");
            return memory[pos];
        // Joypad register
        } else if(pos == 0xFF00) {
            return getJoyPadState();
        } else if(pos >= 0xFF10 && pos <= 0xFF3F){
            return gbAudio.Read(pos);
        }
        return memory[pos];
    }
    
    public bool WriteToMemory(ushort PC, byte data) {
        if(PC >= 0x0000 && PC <= 0x7FFF) {
            gbCart.Write(PC,data);
        } else if(PC == GameBoyTimer.DIV) {
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
        } else if(PC == GameBoyTimer.TIMA) {
            //if(!gbTimer.isReloadingTIMA()) {
                memory[GameBoyTimer.TIMA] = data;
            //}
        } else if(PC >= 0xA000 && PC <= 0xBFFF) {
            gbCart.Write(PC,data);
        } else if(PC == GameBoyGraphic.LYAddr) {
            memory[PC] = 0;
            gbGraphic.resetWindowLine();
        } else if(PC == DMA){
            DMATransfer(data);
        } else if(PC >= 0xE000 && PC <= 0xFDFF) {
            //Debug.Log("Writing to internal ram/ echo ram");
            memory[PC] = data;
        } else if(PC >= 0xFF01 && PC <= 0xFF02) {
            Debug.Log("Link Port");
        } else if(PC >= 0xFF10 && PC <= 0xFF3F) {
            gbAudio.Write(PC,data);
        } else if(PC == GameBoyGraphic.STATAddr) {
            memory[PC] = (byte)((memory[PC] & 0x7) | (data & 0xF8));
        } else {
            memory[PC] = data;
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

    public bool IncrementReg(ushort pos) {
        memory[pos]++;
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
