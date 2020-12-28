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
    public GameBoyMemory(GameBoyCartiridge gbCart) {
        this.gbCart = gbCart;
        memory = new List<byte>(new byte[memorySize]);
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
		} else if(pos >= 0xE000 && pos <= 0xFDFF ){
            Debug.Log("Read from internal ram/ echo ram");
            return memory[pos];
        // Joypad register
        } else if(pos == 0xFF00) {
            return getJoyPadState();
        }
        return memory[pos];
    }
    
    public bool WriteToMemory(ushort pos, byte data) {
        if(pos == GameBoyTimer.DIV) {
            memory[pos] = 0;
            //TODO: Reset TAC to 0 to prevent intrerrupts?
        } else if(pos == GameBoyGraphic.LYAddr) {
            memory[pos] = 0;
        } else if(pos == DMA){
            DMATransfer(data);
        } else if(pos >= 0xE000 && pos <= 0xFDFF) {
            Debug.Log("Writing to internal ram/ echo ram");
            memory[pos] = data;
        } else {
            memory[pos] = data;
        }
        return true;
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
