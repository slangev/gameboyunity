using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class GameBoyMemory
{
    private uint memorySize = 0x10000;

    private uint DMA = 0xFF46;
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
            // 0xFF50 (bios/bootstrap) is disabled if 0xA0
		    if (memory[0xFF50] == 0 && pc < 0x100) {
			    return memory[pc];
		    }
			return gbCart.Read(pc);
		}
        return memory[pc];
    }
    
    public bool WriteToMemory(ushort pos, byte data) {
        if(pos == GameBoyTimer.DIV) {
            memory[pos] = 0;
        } else if(pos == GameBoyGraphic.LYAddr) {
            memory[pos] = 0;
        } else if(pos == DMA){
            Debug.Log("Starting DMA transfer...");
            DMATransfer(data);
        }
        else {
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
}
