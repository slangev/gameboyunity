using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//http://index-of.es/Varios-2/Game%20Boy%20Programming%20Manual.pdf
public class GameBoyCPU {
    public byte A {get;set;}
    public byte F {get;set;}
    public byte B {get;set;}
    public byte C {get;set;}
    public byte D {get;set;}
    public byte E {get;set;}
    public byte H {get;set;}
    public byte L {get;set;}
    public ushort SP {get;set;}
    public ushort PC {get;set;}
    public static uint ClockCycle {get;set;}
    private GameBoyMemory m;

    const int ZFlag = 7;
    const int NFlag = 6;
    const int HFlag = 5;
    const int CFlag = 4;

    public GameBoyCPU(GameBoyMemory m) {
        this.m = m;
    }

    public void Tick() {
        handleInterrupts();
        //TODO: protect the fetch.
        byte instruction = m.ReadFromMemory(PC++);
        HandleInstructions(instruction);
    }

    public void HandleInstructions(byte opcode) {
        if(opcode == 0x31) {
            SP = LDnNN(12);
        } else if(opcode == 0x0E) {
            C = LDRN(8);
        } else if(opcode == 0xE0) {
            m.WriteToMemory((ushort)(0xFF00 + LDRN(12)),A);
        } else if(opcode == 0xAF) {
            A = XORn(A,A,4);
        } else if(opcode == 0x3E) {
            A = LDRN(8);
        } else if(opcode == 0x06) {
            B = LDRN(8);
        } else if(opcode == 0xE2) {
            LDCR(C,A,8);
        } else if(opcode == 0x0C) {
            C = INC(4,C);
        } else if(opcode == 0x05) {
            B = DEC(4,B);
        } else if(opcode == 0x35) {
            ushort HL = combineBytesToWord(H,L);
            m.WriteToMemory(HL,DEC(12,m.ReadFromMemory(HL)));
        } else if(opcode == 0x20) {
            JRCC(opcode,12);
        } else if(opcode == 0x4F) {
            C = LDRR(4,A);
        } else if(opcode == 0x11) {
            ushort word = LDnNN(12);
            var separatedBytes = separateWordToBytes(word);
            D = separatedBytes.Item1;
            E = separatedBytes.Item2;
        } else if(opcode == 0x17) {
            A = RL(4,A);
            resetBit(ZFlag,F);
        } else if (opcode == 0x1A) {
            ushort pair = combineBytesToWord(D,E);
            A = LDNN(pair,8);
        } else if(opcode == 0x21) {
            ushort word = LDnNN(12);
            var separatedBytes = separateWordToBytes(word);
            H = separatedBytes.Item1;
            L = separatedBytes.Item2;
        } else if(opcode == 0x77) {
            LDDHLA(8,0);
        } else if(opcode == 0x32) {
            LDDHLA(8,-1);
        } else if(opcode == 0xCD) {
            Call(24);
        } else if(opcode == 0xC5) {
            ushort pair = combineBytesToWord(B,C);
            Push(16, pair);
        } else if(opcode == 0xC1) {
            ushort BC = Pop(12);
            var separatedBytes = separateWordToBytes(BC);
            B = separatedBytes.Item1;
            C = separatedBytes.Item2;
        }
        
        //Extended opcodes
        else if(opcode == 0xCB) {
            byte opcodetwo = m.ReadFromMemory(PC++);
            if(opcodetwo == 0x7C) {
                TestBit(7,H,8);
            } else if(opcodetwo == 0x11) {
                C = RL(8,C);
            } else if(opcodetwo == 0x15) {
                L = RL(8,L);
            } else if(opcodetwo == 0x16) {
                ushort HL = combineBytesToWord(H,L);
                m.WriteToMemory(HL,RL(16,m.ReadFromMemory(HL)));
            } else {
                Debug.Log("Unknown opcode while using extended opcodes: " + opcodetwo.ToString("X2") + " PC: " + (PC-1).ToString("X2"));
            }
        }
        else {
            Debug.Log("Unknown opcode: " + opcode.ToString("X2") + " PC: " + (PC-1).ToString("X2"));
        }
    } 

    private void handleInterrupts() {

    }

    private (byte,byte) separateWordToBytes(ushort word) {
        return ((byte)((word & 0xFF00) >> 8),(byte)(word & 0x00FF));
    }

    private ushort combineBytesToWord(byte highByte, byte lowByte) {
        ushort word = 0;
        word = (ushort)((word | highByte) << 8);
        word = (ushort)((word | lowByte));
        return word;
    }

    private byte setBit(byte pos, byte r) {
        byte result = (byte)(r | (1 << pos));
        return result;
    }

    private byte resetBit(byte pos, byte r) {
        byte result = (byte)((r  & ~(1 << pos)));
        return result;
    }

    private byte getBit(byte pos, byte reg) {
        byte result = (byte)((reg & (1 << pos)));
        result = (byte)(result >> pos); 
        return result;
    }

    private void clearLowerBitOfF() {
        //bit 0-3 ALWAYS ZERO
        F = resetBit(3,F);
        F = resetBit(2,F);
        F = resetBit(1,F);
        F = resetBit(0,F);
    }

    // page 103
    private byte TestBit(byte b, ushort r, ushort time) {
        byte result = (byte)((r & (1 << b)));
		result = (byte)(result >> b);
        result = (byte)(~result & 0x1);
        if(result == 0x1) {
            F = setBit(ZFlag,F); // set z to 1
        } else { 
            F = resetBit(ZFlag,F); // set z to 0
        }
        F = resetBit(NFlag,F);
        F = setBit(HFlag,F);
        clearLowerBitOfF();
        ClockCycle += time;
        return result;
    }

    // 8-bit load
    // Logical exclusive OR n with register r, result in r
    // page 94
    private byte XORn(byte r, byte n, ushort time) {
        byte result = (byte)(A ^ n);
        if(result == 0) {
            F = setBit(ZFlag,F);
        } else {
            F = resetBit(ZFlag,F);
        }
        F = resetBit(NFlag,F);
        F = resetBit(HFlag,F);
        F = resetBit(CFlag,F);
        clearLowerBitOfF();
        ClockCycle += time;
        return result;
    }

    //page 95
    private byte INC(uint time, byte r) {
        byte result = (byte)(r+1);
        F = (result == 0) ? setBit(ZFlag,F) : resetBit(ZFlag,F);
        F = resetBit(NFlag,F);
        F = ((((r & 0xf) + (1 & 0xf)) & 0x10) == 0x10) ? setBit(HFlag,F) : resetBit(HFlag,F);
        clearLowerBitOfF();
        ClockCycle += time;
        return result;
    }

    //page 95
    private byte DEC(uint time, byte r) {
        byte result = (byte)(r-1);
        F = (result == 0) ? setBit(ZFlag,F) : resetBit(ZFlag,F);
        F = setBit(NFlag,F);
        F = ((r & 0xf) < (1 & 0xf)) ? setBit(HFlag,F) : resetBit(HFlag,F);
        clearLowerBitOfF();
        ClockCycle += time;
        return result;
    }

    //page 85
    private byte LDRN(uint time) {
        byte result = m.ReadFromMemory(PC++);
        ClockCycle += time;
        return result;
    }

    //page 86
    private byte LDRNN(uint time, ushort pos) {
        byte result = m.ReadFromMemory(pos);
        ClockCycle += time;
        return result;
    }

    //page 89
    private void LDDHLA(uint time, int op) {
        ushort HL = combineBytesToWord(H,L);
        m.WriteToMemory(HL,A);
        HL += (ushort)(op);
        var separatedBytes = separateWordToBytes(HL);
        H = separatedBytes.Item1;
        L = separatedBytes.Item2;
        ClockCycle += time;
    }

    private byte LDRR(uint time, byte reg) {
        ClockCycle += time;
        return reg;
    }

    //16-bit Loads
    //Put value nn into n
    private ushort LDnNN(uint time) {
        byte lowByte = m.ReadFromMemory(PC++);
        byte highByte = m.ReadFromMemory(PC++);
        ushort word = combineBytesToWord(highByte,lowByte);
        ClockCycle += time;
        return word;
    }

    // page 105
    private void JRCC(byte cc, uint time) {
        byte b = m.ReadFromMemory(PC++);
		sbyte sb = unchecked((sbyte)(b));
        if(cc == 0x20 && getBit(7,F) == 0) {
            PC = (ushort)(PC + sb);
            ClockCycle += time;
        } else {
            //Not jump increase cycle by 8.
            ClockCycle += 8;
        }
    }

    private void LDCR(byte c, byte reg, uint time) {
        m.WriteToMemory((ushort)(0xFF00+C), reg);
        ClockCycle += time;
    }

    // page 86
    private byte LDNN(ushort index, uint time) {
        byte b = m.ReadFromMemory(index);
        ClockCycle += time;
        return b;
    }

    // page 107
    private void Call(uint time) {
        byte lowByte = m.ReadFromMemory(PC++);
        byte highByte = m.ReadFromMemory(PC++);
        ushort word = combineBytesToWord(highByte,lowByte);
        var separatedBytes = separateWordToBytes(PC);
        highByte = separatedBytes.Item1;
        lowByte = separatedBytes.Item2;
        m.WriteToMemory((ushort)(SP - 1),highByte);
        m.WriteToMemory((ushort)(SP - 2),lowByte);
        PC = word;
        SP = (ushort)(SP - 2);
        ClockCycle += time;
    }

    // page 90
    private void Push(uint time, ushort pair) {
        var separatedBytes = separateWordToBytes(pair);
        byte highByte = separatedBytes.Item1;
        byte lowByte = separatedBytes.Item2;
        m.WriteToMemory((ushort)(SP - 1),highByte);
        m.WriteToMemory((ushort)(SP - 2),lowByte);
        SP = (ushort)(SP - 2);
        ClockCycle += time;
    }

    // page 91
    private ushort Pop(uint time) {
        byte lowByte = m.ReadFromMemory((ushort)(SP));
        byte highByte = m.ReadFromMemory((ushort)(SP+1));
        SP = (ushort)(SP+2);
        ushort word = combineBytesToWord(highByte,lowByte);
        ClockCycle += time;
        return word;
    }

    // page 98-99
    private byte RL(uint time, byte reg) {
        byte bit = getBit(7,reg);
        byte result = (byte)((reg << 1) | (byte)(getBit(CFlag,F)));
        F = (result == 0x00) ? setBit(ZFlag,F) :  F = resetBit(ZFlag,F);
        F = resetBit(NFlag,F);
        F = resetBit(HFlag,F);
        F = (bit == 0x00) ? resetBit(CFlag,F) : setBit(CFlag,F);
        clearLowerBitOfF();
        ClockCycle += time;
        return result;
    }
}
