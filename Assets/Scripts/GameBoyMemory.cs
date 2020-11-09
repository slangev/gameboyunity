﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class GameBoyMemory
{
    private uint memorySize = 0xFFFF;
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

    public byte ReadFromMemory(ushort pc) {
        if (pc >= 0x0000 && pc <= 0x7FFF) {
            // 0xFF50 (bios/bootstrap) is enabled if 0xA0
		    if (memory[0xFF50] == 0 && pc < 0x100) {
			    return memory[pc];
		    }
			return gbCart.Read(pc);
		}
        return memory[pc];
    }
    
    public bool WriteToMemory(ushort pos, byte data) {
        memory[pos] = data;
        return true;
    }
}
