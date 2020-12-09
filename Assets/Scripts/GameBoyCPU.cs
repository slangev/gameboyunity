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
    public ushort SP {get;set;} = 0xFFFE;
    public ushort PC {get;set;}
    public static uint ClockCycle {get;set;}
    private GameBoyMemory m;
    private GameBoyInterrupts interrupts;
    bool halt = false; 

private static readonly uint[] cycleCount = new uint[] {
	4,12,8,8,4,4,8,4,20,8,8,8,4,4,8,4,
	4,12,8,8,4,4,8,4,12,8,8,8,4,4,8,4,
	8,12,8,8,4,4,8,4,8,8,8,8,4,4,8,4,
	8,12,8,8,12,12,12,4,8,8,8,8,4,4,8,4,
	4,4,4,4,4,4,8,4,4,4,4,4,4,4,8,4,
	4,4,4,4,4,4,8,4,4,4,4,4,4,4,8,4,
	4,4,4,4,4,4,8,4,4,4,4,4,4,4,8,4,
	8,8,8,8,8,8,4,8,4,4,4,4,4,4,8,4,
	4,4,4,4,4,4,8,4,4,4,4,4,4,4,8,4,
	4,4,4,4,4,4,8,4,4,4,4,4,4,4,8,4,
	4,4,4,4,4,4,8,4,4,4,4,4,4,4,8,4,
	4,4,4,4,4,4,8,4,4,4,4,4,4,4,8,4,
	8,12,12,16,12,16,8,16,8,16,12,4,12,24,8,16,
	8,12,12,0,12,16,8,16,8,16,12,0,12,0,8,16,
	12,12,8,0,0,16,8,16,16,4,16,0,0,0,8,16,
	12,12,8,4,0,16,8,16,12,8,16,4,0,0,8,16,
};

private static readonly uint[] cycleCount_CB = new uint[] {
	8,8,8,8,8,8,16,8,8,8,8,8,8,8,16,8,
	8,8,8,8,8,8,16,8,8,8,8,8,8,8,16,8,
	8,8,8,8,8,8,16,8,8,8,8,8,8,8,16,8,
	8,8,8,8,8,8,16,8,8,8,8,8,8,8,16,8,
	8,8,8,8,8,8,12,8,8,8,8,8,8,8,12,8,
	8,8,8,8,8,8,12,8,8,8,8,8,8,8,12,8,
	8,8,8,8,8,8,12,8,8,8,8,8,8,8,12,8,
	8,8,8,8,8,8,12,8,8,8,8,8,8,8,12,8,
	8,8,8,8,8,8,16,8,8,8,8,8,8,8,16,8,
	8,8,8,8,8,8,16,8,8,8,8,8,8,8,16,8,
	8,8,8,8,8,8,16,8,8,8,8,8,8,8,16,8,
	8,8,8,8,8,8,16,8,8,8,8,8,8,8,16,8,
	8,8,8,8,8,8,16,8,8,8,8,8,8,8,16,8,
	8,8,8,8,8,8,16,8,8,8,8,8,8,8,16,8,
	8,8,8,8,8,8,16,8,8,8,8,8,8,8,16,8,
	8,8,8,8,8,8,16,8,8,8,8,8,8,8,16,8,
};

    const byte ZFlag = 7;
    const byte NFlag = 6;
    const byte HFlag = 5;
    const byte CFlag = 4;

    public GameBoyCPU(GameBoyMemory m, GameBoyInterrupts i) {
        interrupts = i;
        this.m = m;
    }

    private void handleInterrupts() {
        if (GameBoyInterrupts.EIDIFlag) {
            GameBoyInterrupts.EIDIFlag = false;
        } else {
            GameBoyInterrupts.IMEFlag = GameBoyInterrupts.IMEHold;
        }
        
        byte req = m.ReadFromMemory(GameBoyInterrupts.IF);
        byte enabled = m.ReadFromMemory(GameBoyInterrupts.IE); 
        if(req > 0) {
            //Only care about bit 0-4
            for(byte i = 0; i < 5; i++)  {
                if(getBit(i,req) != 0 && getBit(i,enabled) != 0) {
                    halt = false;
                    if(GameBoyInterrupts.IMEFlag) {
                        serviceInterrupts(i);
                    }
                }
            }
        }
    }

    private void serviceInterrupts(byte id) {
        GameBoyInterrupts.IMEFlag = false;
        GameBoyInterrupts.IMEHold = false;
        m.WriteToMemory(GameBoyInterrupts.IF, resetBit(id, m.ReadFromMemory(GameBoyInterrupts.IF)));
        byte n = getNFromID(id);
        RST(n);
    }

    private byte getNFromID(byte id) {
        if(id == 0) {
            return 0x40;
        } else if(id == 1) {
            return 0x48;
        } else if(id == 2) {
            return 0x50;
        } else if(id == 3) {
            return 0x58;
        } else if(id == 4) {
            return 0x60;
        } 
        return 0;
    }

    public void ResetNoBios() {
        A = 0x01;
        F = 0xB0;
        B = 0x00;
        C = 0x13;
        D = 0x00;
        E = 0xD8;
        H = 0x01;
        L = 0x4D;
        PC = 0x100;
        SP = 0xFFFE;
        m.WriteToMemory(0xFF40,0x91);
        m.WriteToMemory(0xFF47,0xFC);
        m.WriteToMemory(0xFF48,0xFF);
        m.WriteToMemory(0xFF49,0xFF);
        m.WriteToMemory(0xFF50,0x01); // Disable bootstrap
    }


    public uint Tick() {
        handleInterrupts();
        if(!halt) {
            byte instruction = m.ReadFromMemory(PC++);
            return handleInstructions(instruction);
        } 
        ClockCycle += 4;
        return 4;
    }

    private uint handleInstructions(byte opcode) {
        uint lastCycleCount = cycleCount[opcode];
        switch(opcode) {
            case 0x00:
                NOP();
                break;
            case 0x01:
                ushort word = LDnNN();
                ushort BC = combineBytesToWord(B,C);
                var separatedBytes = separateWordToBytes(word);
                B = separatedBytes.Item1;
                C = separatedBytes.Item2;
                break;
            case 0xFA:
                word = LDnNN();
                A = m.ReadFromMemory(word);
                break;
            case 0x31:
                SP = LDnNN();
                break;
            case 0xE0:
                m.WriteToMemory((ushort)(0xFF00 + LDRN()),A);
                break;
            case 0xF0:
                byte b = LDRN();
                A = m.ReadFromMemory((ushort)(0xFF00 + b));
                break;
            case 0xAF:
                A = XORn(A,A);
                break;
            case 0xA9:
                A = XORn(A,C);
                break;
            case 0xB7:
                A = OR(A,A);
                break;
            case 0xB0:
                A = OR(A,B);
                break;
            case 0xB1:
                A = OR(A,C);
                break;
            case 0xF6:
                byte r = m.ReadFromMemory(PC++);
                A = OR(A,r);
                break;
            case 0xB6:
                ushort HL = combineBytesToWord(H,L);
                A = OR(A,m.ReadFromMemory(HL));
                break;
            case 0x3E:
                A = LDRN();
                break;
            case 0x06:
                B = LDRN();
                break;
            case 0x0E:
                C = LDRN();
                break;
            case 0x16:
                D = LDRN();
                break;
            case 0x1E:
                E = LDRN();
                break;
            case 0xE2:
                LDCR(C,A);
                break;
            case 0x2E:
                L = LDRN();
                break;
            case 0x3C:
                A = INC(A);
                break;
            case 0x04:
                B = INC(B);
                break;
            case 0x0C:
                C = INC(C);
                break;
            case 0x14:
                D = INC(D);
                break;
            case 0x1C:
                E = INC(E);
                break;
            case 0x24:
                H = INC(H);
                break;
            case 0x2C:
                L = INC(L);
                break;
            case 0x34:
                HL = combineBytesToWord(H,L);
                m.WriteToMemory(HL,INC(m.ReadFromMemory(HL)));
                break;
            case 0x80:
                A = ADD(B);
                break;
            case 0x86:
                HL = combineBytesToWord(H,L);
                A = ADD(m.ReadFromMemory(HL));
                break;
            case 0x87:
                A = ADD(A);
                break;
            case 0x19:
                HL = combineBytesToWord(H,L);
                ushort DE = combineBytesToWord(D,E);
                HL = ADDWord(HL,DE);
                separatedBytes = separateWordToBytes(HL);
                H = separatedBytes.Item1;
                L = separatedBytes.Item2;
                break;
            case 0xA7:
                A = AND(A,A);
                break;
            case 0xA1:
                A = AND(A,C);
                break;
            case 0xE6:
                byte n = m.ReadFromMemory(PC++);
                A = AND(A,n);
                break;
            case 0x78:
                A = LDRR(B);
                break;
            case 0x79:
                A = LDRR(C);
                break;
            case 0x7B:
                A = LDRR(E);
                break;
            case 0x7C:
                A = LDRR(H);
                break;
            case 0x7D:
                A = LDRR(L);
                break;
            case 0x47:
                B = LDRR(A);
                break;
            case 0x4F:
                C = LDRR(A);
                break;
            case 0x56:
                HL = combineBytesToWord(H,L);
                D = LDRR(m.ReadFromMemory(HL));
                break;
            case 0x57:
                D = LDRR(A);
                break;
            case 0x5F:
                E = LDRR(A);
                break;
            case 0x5E:
                HL = combineBytesToWord(H,L);
                E = LDRR(m.ReadFromMemory(HL));
                break;
            case 0x67:
                H = LDRR(A);
                break;
            case 0x68:
                L = LDRR(B);
                break;
            case 0x6B:
                L = LDRR(E);
                break;
            case 0x7E:
                HL = combineBytesToWord(H,L);
                A = LDRR(m.ReadFromMemory(HL));
                break;
            case 0x03:
                BC = combineBytesToWord(B,C);
                word = INC(BC);
                separatedBytes = separateWordToBytes(word);
                B = separatedBytes.Item1;
                C = separatedBytes.Item2;
                break;
            case 0x13:
                DE = combineBytesToWord(D,E);
                word = INC(DE);
                separatedBytes = separateWordToBytes(word);
                D = separatedBytes.Item1;
                E = separatedBytes.Item2;
                break;
            case 0x23:
                HL = combineBytesToWord(H,L);
                word = INC(HL);
                separatedBytes = separateWordToBytes(word);
                H = separatedBytes.Item1;
                L = separatedBytes.Item2;
                break;
            case 0x3D:
                A = DEC(A);
                break;
            case 0x05:
                B = DEC(B);
                break;
            case 0x0D:
                C = DEC(C);
                break;
            case 0x15:
                D = DEC(D);
                break;
            case 0x1D:
                E = DEC(E);
                break;
            case 0x0B:
                BC = combineBytesToWord(B,C);
                word = DECWord(BC);
                separatedBytes = separateWordToBytes(word);
                B = separatedBytes.Item1;
                C = separatedBytes.Item2;
                break;
            case 0x1B:
                DE = combineBytesToWord(D,E);
                word = DECWord(DE);
                separatedBytes = separateWordToBytes(word);
                D = separatedBytes.Item1;
                E = separatedBytes.Item2;
                break;
            case 0x35:
                HL = combineBytesToWord(H,L);
                m.WriteToMemory(HL,DEC(m.ReadFromMemory(HL)));
                break;
            case 0x20:
            case 0x28:
            case 0x18:
                JRCC(opcode);
                break;
            case 0x11:
                word = LDnNN();
                separatedBytes = separateWordToBytes(word);
                D = separatedBytes.Item1;
                E = separatedBytes.Item2;
                break;
            case 0x17:
                A = RL(A);
                resetBit(ZFlag,F);
                break;
            case 0x1A:
                ushort pair = combineBytesToWord(D,E);
                A = LDNN(pair);
                break;
            case 0x2A:
                word = combineBytesToWord(H,L);
                A = LDNN(word++);
                separatedBytes = separateWordToBytes(word);
                H = separatedBytes.Item1;
                L = separatedBytes.Item2;
                break;
            case 0x21:
                word = LDnNN();
                separatedBytes = separateWordToBytes(word);
                H = separatedBytes.Item1;
                L = separatedBytes.Item2;
                break;
            case 0x12:
                DE = combineBytesToWord(D,E);
                m.WriteToMemory(DE,A);
                break;
            case 0x22:
                LDDHL(1,A);
                break;
            case 0x77:
                LDDHL(0,A);
                break;
            case 0x32:
                LDDHL(-1,A);
                break;
            case 0x36:
                n = m.ReadFromMemory(PC++);
                LDDHL(0,n);
                break;
            case 0xC4:
                bool cc = getBit(ZFlag,F) == 0;
                if(cc) {
                    lastCycleCount = 24;
                }
                Call(cc);
                break;
            case 0xCC:
                cc = getBit(ZFlag,F) == 1;
                if(cc) {
                    lastCycleCount = 24;
                }
                Call(cc);
                break;
            case 0xCD:
                Call(true);
                break;
            case 0xC0:
                cc = getBit(ZFlag,F) == 0;
                if(cc) {
                    lastCycleCount = 20;
                }
                Ret(cc);
                break;
            case 0xC8:
                cc = getBit(ZFlag,F) == 1;
                if(cc) {
                    lastCycleCount = 20;
                }
                Ret(cc);
                break;
            case 0xC9:
                Ret(true);
                break;
            case 0xD9:
                Ret(true);
			    GameBoyInterrupts.IMEFlag = true;
			    GameBoyInterrupts.IMEHold = true;
                break;
            case 0xC5:
                pair = combineBytesToWord(B,C);
                Push(pair);
                break;
            case 0xD5:
                pair = combineBytesToWord(D,E);
                Push(pair);
                break;
            case 0xE5:
                pair = combineBytesToWord(H,L);
                Push(pair);
                break;
            case 0xF5:
                pair = combineBytesToWord(A,F);
                Push(pair);
                break;
            case 0xC1:
                BC = Pop();
                separatedBytes = separateWordToBytes(BC);
                B = separatedBytes.Item1;
                C = separatedBytes.Item2;
                break;
            case 0xD1:
                DE = Pop();
                separatedBytes = separateWordToBytes(DE);
                D = separatedBytes.Item1;
                E = separatedBytes.Item2;
                break;
            case 0xE1:
                HL = Pop();
                separatedBytes = separateWordToBytes(HL);
                H = separatedBytes.Item1;
                L = separatedBytes.Item2;
                break;
            case 0xF1:
                ushort AF = Pop();
                separatedBytes = separateWordToBytes(AF);
                A = separatedBytes.Item1;
                F = separatedBytes.Item2;
                break;
            case 0xC3:
                JP(true);
                break;
            case 0xCA:
                cc = getBit(ZFlag,F) == 1;
                if(cc) {
                    lastCycleCount = 16;
                }
                JP(cc);
                break;
            case 0xE9:
                HL = combineBytesToWord(H,L);
                PC = HL;
                break;
            case 0xBE:
                HL = combineBytesToWord(H,L);
                n = m.ReadFromMemory(HL);
                CP(n);
                break;
            case 0xFE:
                n = m.ReadFromMemory(PC++);
                CP(n);
                break;
            case 0x90:
                A = SUB(B);
                break;
            case 0xEA:
                word = LDNNA();
                m.WriteToMemory(word,A);
                break;
            case 0x2F:
                A = CPL(A);
                break;
            case 0xEF:
                RST(0x28);
                break;
            case 0xF3:
                DI();
                break;
            case 0xFB:
                EI();
                break;
            //Extended Opcodes
            case 0xCB:
                byte opcodetwo = m.ReadFromMemory(PC++);
                lastCycleCount += cycleCount_CB[opcodetwo];
                switch(opcodetwo) {
                    case 0x7C:
                        TestBit(7,H);
                        break;
                    case 0x11:  
                        C = RL(C);
                        break;
                    case 0x15:
                        L = RL(L);
                        break;
                    case 0x16:
                        HL = combineBytesToWord(H,L);
                        m.WriteToMemory(HL,RL(m.ReadFromMemory(HL)));
                        break;
                    case 0x36:
                        HL = combineBytesToWord(H,L);
                        m.WriteToMemory(HL,SWAP(m.ReadFromMemory(HL)));
                        break;
                    case 0x37:
                        A = SWAP(A);
                        break;
                    case 0x87:
                        A = resetBit(0,A);
                        break;
                    default:
                        Debug.Log("Unknown opcode while using extended opcodes: " + opcodetwo.ToString("X2") + " PC: " + (PC-1).ToString("X2"));
                        break;
                }
                break;
            default:
                Debug.Log("Unknown opcode: " + opcode.ToString("X2") + " PC: " + (PC-1).ToString("X2"));
                break;
        } 
        ClockCycle+=lastCycleCount;
        return lastCycleCount;
    } 

    private void readFromSerialPort() {
        byte r = m.ReadFromMemory(0xFF02);
        if(r == 0x81) {
            char c = (char)(m.ReadFromMemory(0xFF01));
            Debug.Log(c);
            m.WriteToMemory(0xFF02,0x0);
        }
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

    public static byte setBit(byte pos, byte r) {
        byte result = (byte)(r | (1 << pos));
        return result;
    }

    public static byte resetBit(byte pos, byte r) {
        byte result = (byte)((r  & ~(1 << pos)));
        return result;
    }

    public static byte getBit(byte pos, byte reg) {
        byte result = (byte)((reg & (1 << pos)));
        result = (byte)(result >> pos); 
        return result;
    }

    private bool isHalfCarryAdd(byte r, byte n) {
        return (((r & 0xf) + (n & 0xf)) & 0x10) == 0x10;
    }

    //Maybe??? TODO
    private bool isHalfCarryAddWord(ushort r, ushort n) {
        uint result = (uint)(((r & 0x0fff) + (n & 0x0fff)));
        return result > 0xfff;
    }

    private bool isHalfCarrySub(byte r, byte n) {
        return (r & 0xf) < (n & 0xf);
    }

    private bool isCarrySub(byte r, byte n) {
        return r < n;
    }

    private bool isCarryAdd(byte r, byte n) {
        ushort result = (ushort)(r + n);
        return result > 0xFF;
    }

    private bool isCarryAddWord(ushort r, ushort n) {
        uint result = (uint)(r + n);
        return result > 0xFFFF;
    }

    private void clearLowerBitOfF() {
        //bit 0-3 ALWAYS ZERO
        F = (byte)(F & 0xF0);
    }

    // page 103
    private byte TestBit(byte b, ushort r) {
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
        return result;
    }

    // 8-bit load
    // Logical exclusive xOR n with register r, result in r
    // page 94
    private byte XORn(byte r, byte n) {
        byte result = (byte)(r ^ n);
        F = (result == 0) ? setBit(ZFlag,F) : resetBit(ZFlag,F);
        F = resetBit(NFlag,F);
        F = resetBit(HFlag,F);
        F = resetBit(CFlag,F);
        clearLowerBitOfF();
        return result;
    }

    // page 94
    private byte OR(byte r, byte n) {
        byte result = (byte)(r | n);
        F = (result == 0) ? setBit(ZFlag,F) : resetBit(ZFlag,F);
        F = resetBit(NFlag,F);
        F = resetBit(HFlag,F);
        F = resetBit(CFlag,F);
        clearLowerBitOfF();
        return result; 
    }

    //page 95
    private byte INC(byte r) {
        byte result = (byte)(r+1);
        F = (result == 0) ? setBit(ZFlag,F) : resetBit(ZFlag,F);
        F = resetBit(NFlag,F);
        F = (isHalfCarryAdd(r,1)) ? setBit(HFlag,F) : resetBit(HFlag,F);
        clearLowerBitOfF();
        return result;
    }

    //page 97
    private ushort INC( ushort r) {
        ushort result = (ushort)(r+1);
        return result;
    }

    //page 92
    private byte ADD(byte n) {
        byte result = (byte)(A + n);
        F = (result == 0) ? setBit(ZFlag,F) : resetBit(ZFlag,F);
        F = resetBit(NFlag,F);
        F = (isHalfCarryAdd(A,n)) ? setBit(HFlag,F) : resetBit(HFlag,F);
        F = (isCarryAdd(A,n)) ? setBit(CFlag,F) : resetBit(CFlag,F);
        clearLowerBitOfF();
        return result;
    }

    // page 97
    private ushort ADDWord(ushort r, ushort n) {
        ushort result = (ushort)(r + n);
        F = resetBit(NFlag,F);
        F = (isHalfCarryAddWord(r,n)) ? setBit(HFlag,F) : resetBit(HFlag,F);
        F = (isCarryAddWord(r,n)) ? setBit(CFlag,F) : resetBit(CFlag,F);
        clearLowerBitOfF();
        return result;
    }

    //page 95
    private byte DEC(byte r) {
        byte result = (byte)(r-1);
        F = (result == 0) ? setBit(ZFlag,F) : resetBit(ZFlag,F);
        F = setBit(NFlag,F);
        F = (isHalfCarrySub(r,1)) ? setBit(HFlag,F) : resetBit(HFlag,F);
        clearLowerBitOfF();
        return result;
    }

    private ushort DECWord( ushort r) {
        r = (ushort)(r - 1);
        return r;
    }

    // page 93
    private byte SUB(byte n) {
        byte result = (byte)(A-n);
        F = (A == n) ? setBit(ZFlag,F) : resetBit(ZFlag,F);
        F = setBit(NFlag,F);
        F = (isHalfCarrySub(A,n)) ? setBit(HFlag,F) : resetBit(HFlag,F);
        F = (isCarrySub(A,n)) ? setBit(CFlag,F) : resetBit(CFlag,F);
        clearLowerBitOfF();
        return result;
    }

    //page 85
    private byte LDRN() {
        byte result = m.ReadFromMemory(PC++);
        return result;
    }

    //page 86
    private byte LDRNN(ushort pos) {
        byte result = m.ReadFromMemory(pos);
        return result;
    }

    //page 89
    private void LDDHL(int op, byte r) {
        ushort HL = combineBytesToWord(H,L);
        m.WriteToMemory(HL,r);
        HL += (ushort)(op);
        var separatedBytes = separateWordToBytes(HL);
        H = separatedBytes.Item1;
        L = separatedBytes.Item2;
    }

    private byte LDRR(byte reg) {
        return reg;
    }

    private void DI() {
         GameBoyInterrupts.IMEFlag = true;
         GameBoyInterrupts.IMEHold = false;
    }

    private void EI() {
         GameBoyInterrupts.IMEFlag = true;
         GameBoyInterrupts.IMEHold = true;
    }

    //16-bit Loads
    //Put value nn into n
    private ushort LDnNN() {
        byte lowByte = m.ReadFromMemory(PC++);
        byte highByte = m.ReadFromMemory(PC++);
        ushort word = combineBytesToWord(highByte,lowByte);
        return word;
    }

    // page 105
    private void JRCC(byte cc)  {
        byte b = m.ReadFromMemory(PC++);
		sbyte sb = unchecked((sbyte)(b));
        if(cc == 0x20 && getBit(ZFlag,F) == 0) {
            PC = (ushort)(PC + sb);
        } else if(cc == 0x28 && getBit(ZFlag,F) == 1) {
            PC = (ushort)(PC + sb);
        } else if(cc == 0x18) {
            //No conditional JR
            PC = (ushort)(PC + sb);
        } else {
            //Not jump increase cycle by 8.
            ClockCycle += 8;
        }
    }

    // page 105
    private void JP(bool cc) {
        byte lowByte = m.ReadFromMemory((ushort)(PC++));
        byte highByte = m.ReadFromMemory((ushort)(PC++));
        if(cc) {
            ushort word = combineBytesToWord(highByte,lowByte);
            PC = word;
        }
    } 

    private void LDCR(byte c, byte reg)  {
        m.WriteToMemory((ushort)(0xFF00+C), reg);
    }

    // page 86
    private byte LDNN(ushort index)  {
        byte b = m.ReadFromMemory(index);
        return b;
    }

    // page 107
    private void Call(bool cc) {
        byte lowByte = m.ReadFromMemory(PC++);
        byte highByte = m.ReadFromMemory(PC++);
        if(cc) {
            ushort word = combineBytesToWord(highByte,lowByte);
            var separatedBytes = separateWordToBytes(PC);
            highByte = separatedBytes.Item1;
            lowByte = separatedBytes.Item2;
            m.WriteToMemory((ushort)(SP - 1),highByte);
            m.WriteToMemory((ushort)(SP - 2),lowByte);
            PC = word;
            SP = (ushort)(SP - 2);
        }
    }

    // page 90
    private void Push(ushort pair) {
        var separatedBytes = separateWordToBytes(pair);
        byte highByte = separatedBytes.Item1;
        byte lowByte = separatedBytes.Item2;
        m.WriteToMemory((ushort)(SP - 1),highByte);
        m.WriteToMemory((ushort)(SP - 2),lowByte);
        SP = (ushort)(SP - 2);
    }

    // page 91
    private ushort Pop() {
        byte lowByte = m.ReadFromMemory((ushort)(SP));
        byte highByte = m.ReadFromMemory((ushort)(SP+1));
        SP = (ushort)(SP+2);
        ushort word = combineBytesToWord(highByte,lowByte);
        return word;
    }

    // page 108
    private void Ret(bool cc) {
        if(cc) {
            byte lowByte = m.ReadFromMemory((ushort)(SP));
            byte highByte = m.ReadFromMemory((ushort)(SP+1));
            SP = (ushort)(SP+2);
            ushort word = combineBytesToWord(highByte,lowByte);
            PC = word;
        }
    }

    // page 98-99
    private byte RL(byte reg) {
        byte bit = getBit(7,reg);
        byte result = (byte)((reg << 1) | (byte)(getBit(CFlag,F)));
        F = (result == 0x00) ? setBit(ZFlag,F) : resetBit(ZFlag,F);
        F = resetBit(NFlag,F);
        F = resetBit(HFlag,F);
        F = (bit == 0x00) ? resetBit(CFlag,F) : setBit(CFlag,F);
        clearLowerBitOfF();
        return result;
    }

    // page 93-94
    private byte AND(byte r, byte n) {
        byte result = (byte)(r & n);
        F = (result == 0) ? setBit(ZFlag,F) : resetBit(ZFlag,F);
        F = resetBit(NFlag,F);
        F = setBit(HFlag,F);
        F = resetBit(CFlag,F);
        clearLowerBitOfF();
        return result;
    }

    // page 110
    private byte CPL(byte r) {
        byte result = (byte)(~r);
        F = setBit(NFlag,F);
        F = setBit(HFlag,F);
        clearLowerBitOfF();
        return result;
    }

    // page 102
    private byte SWAP(byte r) {
        byte result = 0;
        byte highByte = (byte)((r & 0xF0) >> 4);
        byte lowByte = (byte)((r & 0x0F));
        result = (byte)((result | lowByte) << 4);
        result = (byte)((result | highByte));

        F = (result == 0) ? setBit(ZFlag,F) : resetBit(ZFlag,F);
        F = resetBit(NFlag,F);
        F = resetBit(HFlag,F);
        F = resetBit(CFlag,F);
        clearLowerBitOfF();

        return result;
    }

    //NO OP
    private void NOP() {

    }

    // page 95
    private void CP(byte n) {
        F = (A == n) ? setBit(ZFlag,F) : resetBit(ZFlag,F);
        F = setBit(NFlag,F);
        F = (isHalfCarrySub(A,n)) ? setBit(HFlag,F) : resetBit(HFlag,F);
        F = (isCarrySub(A,n)) ? setBit(CFlag,F) : resetBit(CFlag,F);
        clearLowerBitOfF();
    }

    // page 88
    private ushort LDNNA() {
        byte lowByte = m.ReadFromMemory((ushort)(PC++));
        byte highByte = m.ReadFromMemory((ushort)(PC++));
        ushort word = combineBytesToWord(highByte,lowByte);
        return word;
    }

    // page 109
    private void RST(byte n) {
        Push(PC);
        PC = n;
    }
}
