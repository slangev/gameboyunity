using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class GameBoyMemory
{
    private uint memorySize = 0x10000;
    private ushort DMA = 0xFF46;
    public byte joypadState = 0xFF; // All buttons are up
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
        //Debug.Log(joypadState.ToString("X2"));
        return joypadState;
    }

    public void handleKeyEvents() {

        // Buttons down (b,a,select,start)
        if(Input.GetKey(KeyCode.S)) {
            joypadState = GameBoyCPU.resetBit(5, joypadState);
        }
        if(Input.GetKey(KeyCode.A)) {
            joypadState = GameBoyCPU.resetBit(4, joypadState);
        }
        if(Input.GetKey(KeyCode.Space)) {
            joypadState = GameBoyCPU.resetBit(6, joypadState);
        }
        if(Input.GetKey(KeyCode.Return)) {
            joypadState = GameBoyCPU.resetBit(7, joypadState);
        }

        // Buttons up (b,a,select,start)
        if(Input.GetKeyUp(KeyCode.S)) {
            joypadState = GameBoyCPU.setBit(5, joypadState);
        }
        if(Input.GetKeyUp(KeyCode.A)) {
            joypadState = GameBoyCPU.setBit(4, joypadState);
        }
        if(Input.GetKeyUp(KeyCode.Space)) {
            joypadState = GameBoyCPU.setBit(6, joypadState);
        }
        if(Input.GetKeyUp(KeyCode.Return)) {
            joypadState = GameBoyCPU.setBit(7, joypadState);
        }

        // Directional down (b,a,select,start)
        if(Input.GetKey(KeyCode.UpArrow)) {
            //Debug.Log("Key UpArrow is down");
            joypadState = GameBoyCPU.resetBit(2, joypadState);
        }
        if(Input.GetKey(KeyCode.DownArrow)) {
            //Debug.Log("Key DownArrow is down");
            joypadState = GameBoyCPU.resetBit(3, joypadState);
        }
        if(Input.GetKey(KeyCode.LeftArrow)) {
            //Debug.Log("Key LeftArrow is down");
            joypadState = GameBoyCPU.resetBit(1, joypadState);
        }
        if(Input.GetKey(KeyCode.RightArrow)) {
            //Debug.Log("Key RightArrow is down");
            joypadState = GameBoyCPU.resetBit(0, joypadState);
        }

        // Directional up (b,a,select,start)
        if(Input.GetKeyUp(KeyCode.UpArrow)) {
            //Debug.Log("Key UpArrow is down");
            joypadState = GameBoyCPU.setBit(2, joypadState);

        }
        if(Input.GetKeyUp(KeyCode.DownArrow)) {
            //Debug.Log("Key DownArrow is down");
            joypadState = GameBoyCPU.setBit(3, joypadState);

        }
        if(Input.GetKeyUp(KeyCode.LeftArrow)) {
            //Debug.Log("Key LeftArrow is down");
            joypadState = GameBoyCPU.setBit(1, joypadState);

        }
        if(Input.GetKeyUp(KeyCode.RightArrow)) {
            //Debug.Log("Key RightArrow is down");
            joypadState = GameBoyCPU.setBit(0, joypadState);
        }
    }
}
