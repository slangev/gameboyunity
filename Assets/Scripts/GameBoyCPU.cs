using UnityEngine;
using System.IO;

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
    bool doubleSpeed = false;

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

    public void ResetGBNoBios() {
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
        m.WriteToMemory(0xFF00,0xCF); // no buttons pressed
        m.WriteToMemory(0xFF40,0x91);
        m.WriteToMemory(0xFF47,0xFC);
        m.WriteToMemory(0xFF48,0xFF);
        m.WriteToMemory(0xFF49,0xFF);
        m.WriteToMemory(0xFF50,0x01); // Disable bootstrap
    }

    public void ResetCGBNoBios() {
        A = 0x11;
        F = 0x80;
        B = 0x00;
        C = 0x00;
        D = 0xFF;
        E = 0x56;
        H = 0x00;
        L = 0x0D;
        PC = 0x100;
        SP = 0xFFFE;
        m.WriteToMemory(0xFF40,0x91);
        m.WriteToMemory(0xFF41,0x81);
        m.WriteToMemory(0xFF50,0x11); // Disable bootstrap
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
            case 0x02:
                BC = combineBytesToWord(B,C);
                m.WriteToMemory(BC,A);
                break;
            case 0x07:
                A = RLC(A);
                F = resetBit(ZFlag,F);
                break;
            case 0x08:
                word = LDnNN();
                separatedBytes = separateWordToBytes(SP);
                m.WriteToMemory((ushort)(word),separatedBytes.Item2); //Low byte
                m.WriteToMemory((ushort)(word+1),separatedBytes.Item1); //High byte
                break;
            case 0x10:
                //halt=true;
                //m.WriteToMemory(0xFF4D,0x01); //Low byte
                //PC++;
                Debug.Log("CALLED STOPPED");
                break;
            case 0x0F:
                A = RRC(A);
                F = resetBit(ZFlag,F);
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
            case 0xA8:
                A = XORn(A,B);
                break;
            case 0xA9:
                A = XORn(A,C);
                break;
            case 0xAA:
                A = XORn(A,D);
                break;
            case 0xAB:
                A = XORn(A,E);
                break;
            case 0xAC:
                A = XORn(A,H);
                break;
            case 0xAD:
                A = XORn(A,L);
                break;
            case 0xAE:
                ushort HL = combineBytesToWord(H,L);
                A = XORn(A,m.ReadFromMemory(HL));
                break;
            case 0xAF:
                A = XORn(A,A);
                break;
            case 0xEE:
                b = LDRN();
                A = XORn(A,b);
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
            case 0xB2:
                A = OR(A,D);
                break;
            case 0xB3:
                A = OR(A,E);
                break;
            case 0xB4:
                A = OR(A,H);
                break;
            case 0xB5:
                A = OR(A,L);
                break;
            case 0xF6:
                byte r = m.ReadFromMemory(PC++);
                A = OR(A,r);
                break;
            case 0xB6:
                HL = combineBytesToWord(H,L);
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
            case 0xF2:
                A = m.ReadFromMemory((ushort)(0xFF00+C));
                break;
            case 0x26:
                H = LDRN();
                break;
            case 0x27:
                A = DAA(A);
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
            case 0x81:
                A = ADD(C);
                break;
            case 0x82:
                A = ADD(D);
                break;
            case 0x83:
                A = ADD(E);
                break;
            case 0x84:
                A = ADD(H);
                break;
            case 0x85:
                A = ADD(L);
                break;
            case 0x86:
                HL = combineBytesToWord(H,L);
                A = ADD(m.ReadFromMemory(HL));
                break;
            case 0x87:
                A = ADD(A);
                break;
            case 0x88:
                A = ADC(A,B);
                break;
            case 0x89:
                A = ADC(A,C);
                break;
            case 0x8A:
                A = ADC(A,D);
                break;
            case 0x8B:
                A = ADC(A,E);
                break;
            case 0x8C:
                A = ADC(A,H);
                break;
            case 0x8D:
                A = ADC(A,L);
                break;
            case 0x8E:
                HL = combineBytesToWord(H,L);
                A = ADC(A,m.ReadFromMemory(HL));
                break;
            case 0x8F:
                A = ADC(A,A);
                break;
            case 0x90:
                A = SUB(B);
                break;
            case 0x91:
                A = SUB(C);
                break;
            case 0x92:
                A = SUB(D);
                break;
            case 0x93:
                A = SUB(E);
                break;
            case 0x94:
                A = SUB(H);
                break;
            case 0x95:
                A = SUB(L);
                break;
            case 0x96:
                HL = combineBytesToWord(H,L);
                A = SUB(m.ReadFromMemory(HL));
                break;
            case 0x97:
                A = SUB(A);
                break;
            case 0x98:
                A = SBC(A,B);
                break;
            case 0x99:
                A = SBC(A,C);
                break;
            case 0x9A:
                A = SBC(A,D);
                break;
            case 0x9B:
                A = SBC(A,E);
                break;
            case 0x9C:
                A = SBC(A,H);
                break;
            case 0x9D:
                A = SBC(A,L);
                break;
            case 0x9E:
                HL = combineBytesToWord(H,L);
                A = SBC(A,m.ReadFromMemory(HL));
                break;
            case 0x9F:
                A = SBC(A,A);
                break;
            case 0xDE:
                byte n = m.ReadFromMemory(PC++);
                A = SBC(A,n);
                break;
            case 0xC6:
                n = m.ReadFromMemory(PC++);
                A = ADD(n);
                break;
            case 0xCE:
                n = m.ReadFromMemory(PC++);
                A = ADC(A,n);
                break;
            case 0x09:
                HL = combineBytesToWord(H,L);
                BC = combineBytesToWord(B,C);
                HL = ADDWord(HL,BC);
                separatedBytes = separateWordToBytes(HL);
                H = separatedBytes.Item1;
                L = separatedBytes.Item2;
                break;
            case 0x19:
                HL = combineBytesToWord(H,L);
                ushort DE = combineBytesToWord(D,E);
                HL = ADDWord(HL,DE);
                separatedBytes = separateWordToBytes(HL);
                H = separatedBytes.Item1;
                L = separatedBytes.Item2;
                break;
            case 0x29:
                HL = combineBytesToWord(H,L);
                HL = ADDWord(HL,HL);
                separatedBytes = separateWordToBytes(HL);
                H = separatedBytes.Item1;
                L = separatedBytes.Item2;
                break;
            case 0x39:
                HL = combineBytesToWord(H,L);
                HL = ADDWord(HL,SP);
                separatedBytes = separateWordToBytes(HL);
                H = separatedBytes.Item1;
                L = separatedBytes.Item2;
                break;
            case 0x2B:
                HL = combineBytesToWord(H,L);
                HL = DECWord(HL);
                separatedBytes = separateWordToBytes(HL);
                H = separatedBytes.Item1;
                L = separatedBytes.Item2;
                break;
            case 0x37:
                SCF();
                break;
            case 0x3F:
                CCF();
                break;
            case 0xA6:
                HL = combineBytesToWord(H,L);
                A = AND(A,m.ReadFromMemory(HL));
                break;
            case 0xA7:
                A = AND(A,A);
                break;
            case 0xA0:
                A = AND(A,B);
                break;
            case 0xA1:
                A = AND(A,C);
                break;
            case 0xA2:
                A = AND(A,D);
                break;
            case 0xA3:
                A = AND(A,E);
                break;
            case 0xA4:
                A = AND(A,H);
                break;
            case 0xA5:
                A = AND(A,L);
                break;
            case 0xE6:
                n = m.ReadFromMemory(PC++);
                A = AND(A,n);
                break;
            case 0x78:
                A = LDRR(B);
                break;
            case 0x79:
                A = LDRR(C);
                break;
            case 0x7A:
                A = LDRR(D);
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
            case 0x7E:
                HL = combineBytesToWord(H,L);
                A = LDRR(m.ReadFromMemory(HL));
                break;
            case 0x7F:
                A = LDRR(A);
                break;
            case 0x4F:
                C = LDRR(A);
                break;
            case 0x48:
                C = LDRR(B);
                break;
            case 0x49:
                C = LDRR(C);
                break;
            case 0x4A:
                C = LDRR(D);
                break;
            case 0x4B:
                C = LDRR(E);
                break;
            case 0x4C:
                C = LDRR(H);
                break;
            case 0x4D:
                C = LDRR(L);
                break;
            case 0x4E:
                HL = combineBytesToWord(H,L);
                C = LDRR(m.ReadFromMemory(HL));
                break;
            case 0x50:
                D = LDRR(B);
                break;
            case 0x51:
                D = LDRR(C);
                break;
            case 0x52:
                D = LDRR(D);
                break;
            case 0x53:
                D = LDRR(E);
                break;
            case 0x54:
                D = LDRR(H);
                break;
            case 0x55:
                D = LDRR(L);
                break;
            case 0x56:
                HL = combineBytesToWord(H,L);
                D = LDRR(m.ReadFromMemory(HL));
                break;
            case 0x40:
                B = LDRR(B);
                break;
            case 0x41:
                B = LDRR(C);
                break;
            case 0x42:
                B = LDRR(D);
                break;
            case 0x43:
                B = LDRR(E);
                break;
            case 0x44:
                B = LDRR(H);
                break;
            case 0x45:
                B = LDRR(L);
                break;
            case 0x46:
                HL = combineBytesToWord(H,L);
                B = LDRR(m.ReadFromMemory(HL));
                break;
            case 0x47:
                B = LDRR(A);
                break;
            case 0x57:
                D = LDRR(A);
                break;
            case 0x58:
                E = LDRR(B);
                break;
            case 0x59:
                E = LDRR(C);
                break;
            case 0x5A:
                E = LDRR(D);
                break;
            case 0x5B:
                E = LDRR(E);
                break;
            case 0x5C:
                E = LDRR(H);
                break;
            case 0x5D:
                E = LDRR(L);
                break;
            case 0x5E:
                HL = combineBytesToWord(H,L);
                E = LDRR(m.ReadFromMemory(HL));
                break;
            case 0x5F:
                E = LDRR(A);
                break;
            case 0x6E:
                HL = combineBytesToWord(H,L);
                L = LDRR(m.ReadFromMemory(HL));
                break;
            case 0x6F:
                L = LDRR(A);
                break;
            case 0x60:
                H = LDRR(B);
                break;
            case 0x61:
                H = LDRR(C);
                break;
            case 0x62:
                H = LDRR(D);
                break;
            case 0x63:
                H = LDRR(E);
                break;
            case 0x64:
                H = LDRR(H);
                break;
            case 0x65:
                H = LDRR(L);
                break;
            case 0x66:
                HL = combineBytesToWord(H,L);
                H = LDRR(m.ReadFromMemory(HL));
                break;
            case 0x67:
                H = LDRR(A);
                break;
            case 0x68:
                L = LDRR(B);
                break;
            case 0x69:
                L = LDRR(C);
                break;
            case 0x6A:
                L = LDRR(D);
                break;
            case 0x6B:
                L = LDRR(E);
                break;
            case 0x6C:
                L = LDRR(H);
                break;
            case 0x6D:
                L = LDRR(L);
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
            case 0x33:
                SP = INC(SP);
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
            case 0x25:
                H = DEC(H);
                break;
            case 0x2D:
                L = DEC(L);
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
            case 0x3B:
                SP = DECWord(SP);
                break;
            case 0x35:
                HL = combineBytesToWord(H,L);
                m.WriteToMemory(HL,DEC(m.ReadFromMemory(HL)));
                break;
            case 0x20:
            case 0x28:
            case 0x18:
            case 0x30:
            case 0x38:
                lastCycleCount = JRCC(opcode);
                break;
            case 0x11:
                word = LDnNN();
                separatedBytes = separateWordToBytes(word);
                D = separatedBytes.Item1;
                E = separatedBytes.Item2;
                break;
            case 0x17:
                A = RL(A);
                F = resetBit(ZFlag,F);
                break;
            case 0x0A:
                ushort pair = combineBytesToWord(B,C);
                A = LDNN(pair);
                break;
            case 0x1A:
                pair = combineBytesToWord(D,E);
                A = LDNN(pair);
                break;
            case 0x1F:
                A = RRA(A);
                break;
            case 0x2A:
                word = combineBytesToWord(H,L);
                A = LDNN(word++);
                separatedBytes = separateWordToBytes(word);
                H = separatedBytes.Item1;
                L = separatedBytes.Item2;
                break;
            case 0x3A:
                word = combineBytesToWord(H,L);
                A = LDNN(word--);
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
            case 0x70:
                HL = combineBytesToWord(H,L);
                m.WriteToMemory(HL,B);
                break;
            case 0x71:
                HL = combineBytesToWord(H,L);
                m.WriteToMemory(HL,C);
                break;
            case 0x72:
                HL = combineBytesToWord(H,L);
                m.WriteToMemory(HL,D);
                break;
            case 0x73:
                HL = combineBytesToWord(H,L);
                m.WriteToMemory(HL,E);
                break;
            case 0x74:
                HL = combineBytesToWord(H,L);
                m.WriteToMemory(HL,H);
                break;
            case 0x75:
                HL = combineBytesToWord(H,L);
                m.WriteToMemory(HL,L);
                break;
            case 0x76:
                halt = true;
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
            case 0xD0:
                cc = getBit(CFlag,F) == 0;
                if(cc) {
                    lastCycleCount = 20;
                }
                Ret(cc);
                break;
            case 0xD4:
                cc = getBit(CFlag,F) == 0;
                if(cc) {
                    lastCycleCount = 24;
                }
                Call(cc);
                break;
            case 0xD8:
                cc = getBit(CFlag,F) == 1;
                if(cc) {
                    lastCycleCount = 20;
                }
                Ret(cc);
                break;
            case 0xDC:
                cc = getBit(CFlag,F) == 1;
                if(cc) {
                    lastCycleCount = 24;
                }
                Call(cc);
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
                clearLowerBitOfF();
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
                clearLowerBitOfF();
                break;
            case 0xC2:
                cc = getBit(ZFlag,F) == 0;
                if(cc) {
                    lastCycleCount = 16;
                }
                JP(cc);
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
            case 0xD2:
                cc = getBit(CFlag,F) == 0;
                if(cc) {
                    lastCycleCount = 16;
                }
                JP(cc);
                break;
            case 0xDA:
                cc = getBit(CFlag,F) == 1;
                if(cc) {
                    lastCycleCount = 16;
                }
                JP(cc);
                break;
            case 0xE9:
                HL = combineBytesToWord(H,L);
                PC = HL;
                break;
            case 0xB8:
                CP(B);
                break;
            case 0xB9:
                CP(C);
                break;
            case 0xBA:
                CP(D);
                break;
            case 0xBB:
                CP(E);
                break;
            case 0xBC:
                CP(H);
                break;
            case 0xBD:
                CP(L);
                break;
            case 0xBE:
                HL = combineBytesToWord(H,L);
                n = m.ReadFromMemory(HL);
                CP(n);
                break;
            case 0xBF:
                CP(A);
                break;
            case 0xFE:
                n = m.ReadFromMemory(PC++);
                CP(n);
                break;
            case 0xD6:
                n = m.ReadFromMemory(PC++);
                A = SUB(n);
                break;
            case 0xE8:
                b = m.ReadFromMemory(PC++);
		        sbyte sb = unchecked((sbyte)(b));
                byte lowerByteOfSP = (byte)(SP & 0x00FF);
                F = resetBit(ZFlag,F);
                F = resetBit(NFlag,F);
                F = (isHalfCarryAdd(lowerByteOfSP, b)) ? setBit(HFlag,F) : resetBit(HFlag,F);
                F = (isCarryAdd(lowerByteOfSP, b)) ? setBit(CFlag,F) : resetBit(CFlag,F);
                clearLowerBitOfF();
                SP = (ushort)(SP + sb);
                break;
            case 0xEA:
                word = LDNNA();
                m.WriteToMemory(word,A);
                break;
            case 0x2F:
                A = CPL(A);
                break;
            case 0xC7:
                RST(0x00);
                break;
            case 0xCF:
                RST(0x08);
                break;
            case 0xD7:
                RST(0x10);
                break;
            case 0xDF:
                RST(0x18);
                break;
            case 0xE7:
                RST(0x20);
                break;
            case 0xEF:
                RST(0x28);
                break;
            case 0xF7:
                RST(0x30);
                break;
            case 0xFF:
                RST(0x38);
                break;
            case 0xF3:
                DI();
                break;
            case 0xF8:
                b = m.ReadFromMemory(PC++);
		        sb = unchecked((sbyte)(b));
                F = resetBit(ZFlag,F);
                F = resetBit(NFlag,F);
                lowerByteOfSP = (byte)(SP & 0x00FF);
                F = (isHalfCarryAdd(lowerByteOfSP, b)) ? setBit(HFlag,F) : resetBit(HFlag,F);
                F = (isCarryAdd(lowerByteOfSP, b)) ? setBit(CFlag,F) : resetBit(CFlag,F);
                clearLowerBitOfF();
                HL = combineBytesToWord(H,L);
                HL = (ushort)(SP + sb);
                separatedBytes = separateWordToBytes(HL);
                H = separatedBytes.Item1;
                L = separatedBytes.Item2;
                break;
            case 0xF9:
                HL = combineBytesToWord(H,L);
                SP = HL;
                break;
            case 0xFB:
                EI();
                break;
            //Extended Opcodes
            case 0xCB:
                byte opcodetwo = m.ReadFromMemory(PC++);
                lastCycleCount = cycleCount_CB[opcodetwo];
                switch(opcodetwo) {
                    case 0x00:
                        B = RLC(B);
                        break;
                    case 0x01:
                        C = RLC(C);
                        break;
                    case 0x02:
                        D = RLC(D);
                        break;
                    case 0x03:
                        E = RLC(E);
                        break;
                    case 0x04:
                        H = RLC(H);
                        break;
                    case 0x05:
                        L = RLC(L);
                        break;
                    case 0x06:
                        HL = combineBytesToWord(H,L);
                        m.WriteToMemory(HL,RLC(m.ReadFromMemory(HL)));
                        break;
                    case 0x07:
                        A = RLC(A);
                        break;
                    case 0x08:
                        B = RRC(B);
                        break;
                    case 0x09:
                        C = RRC(C);
                        break;
                    case 0x0A:
                        D = RRC(D);
                        break;
                    case 0x0B:
                        E = RRC(E);
                        break;
                    case 0x0C:
                        H = RRC(H);
                        break;
                    case 0x0D:
                        L = RRC(L);
                        break;
                    case 0x0E:
                        HL = combineBytesToWord(H,L);
                        m.WriteToMemory(HL,RRC(m.ReadFromMemory(HL)));
                        break;
                    case 0x0F:
                        A = RRC(A);
                        break;
                    case 0x10:
                        B = RL(B);
                        break;
                    case 0x11:  
                        C = RL(C);
                        break;
                    case 0x12:  
                        D = RL(D);
                        break;
                    case 0x13:  
                        E = RL(E);
                        break;
                    case 0x14:  
                        H = RL(H);
                        break;
                    case 0x15:
                        L = RL(L);
                        break;
                    case 0x16:
                        HL = combineBytesToWord(H,L);
                        m.WriteToMemory(HL,RL(m.ReadFromMemory(HL)));
                        break;
                    case 0x17:
                        A = RL(A);
                        break;
                    case 0x1E:
                        HL = combineBytesToWord(H,L);
                        m.WriteToMemory(HL,RR(m.ReadFromMemory(HL)));
                        break;
                    case 0x20:
                        B = SLA(B);
                        break;
                    case 0x21:
                        C = SLA(C);
                        break;
                    case 0x22:
                        D = SLA(D);
                        break;
                    case 0x23:
                        E = SLA(E);
                        break;
                    case 0x24:
                        H = SLA(H);
                        break;
                    case 0x25:
                        L = SLA(L);
                        break;
                    case 0x26:
                        HL = combineBytesToWord(H,L);
                        m.WriteToMemory(HL,SLA(m.ReadFromMemory(HL)));
                        break;
                    case 0x27:
                        A = SLA(A);
                        break;
                    case 0x28:
                        B = SRA(B);
                        break;
                    case 0x29:
                        C = SRA(C);
                        break;
                    case 0x2A:
                        D = SRA(D);
                        break;
                    case 0x2B:
                        E = SRA(E);
                        break;
                    case 0x2C:
                        H = SRA(H);
                        break;
                    case 0x2D:
                        L = SRA(L);
                        break;
                    case 0x2E:
                        HL = combineBytesToWord(H,L);
                        m.WriteToMemory(HL,SRA(m.ReadFromMemory(HL)));
                        break;
                    case 0x2F:
                        A = SRA(A);
                        break;
                    case 0x30:
                        B = SWAP(B);
                        break;
                    case 0x31:
                        C = SWAP(C);
                        break;
                    case 0x32:
                        D = SWAP(D);
                        break;
                    case 0x33:
                        E = SWAP(E);
                        break;
                    case 0x34:
                        H = SWAP(H);
                        break;
                    case 0x35:
                        L = SWAP(L);
                        break;
                    case 0x38:
                        B = SRL(B);
                        break;
                    case 0x39:
                        C = SRL(C);
                        break;
                    case 0x3A:
                        D = SRL(D);
                        break;
                    case 0x3B:
                        E = SRL(E);
                        break;
                    case 0x3C:
                        H = SRL(H);
                        break;
                    case 0x3D:
                        L = SRL(L);
                        break;
                    case 0x3E:
                        HL = combineBytesToWord(H,L);
                        m.WriteToMemory(HL,SRL(m.ReadFromMemory(HL)));
                        break;
                    case 0x3F:
                        A = SRL(A);
                        break;
                    case 0x40:
                        BIT(0,B);
                        break;
                    case 0x41:
                        BIT(0,C);
                        break;
                    case 0x42:
                        BIT(0,D);
                        break;
                    case 0x43:
                        BIT(0,E);
                        break;
                    case 0x44:
                        BIT(0,H);
                        break;
                    case 0x45:
                        BIT(0,L);
                        break;
                    case 0x46:
                        HL = combineBytesToWord(H,L);
                        BIT(0,m.ReadFromMemory(HL));
                        break;
                    case 0x47:
                        BIT(0,A);
                        break;
                    case 0x48:
                        BIT(1,B);
                        break;
                    case 0x49:
                        BIT(1,C);
                        break;
                    case 0x4A:
                        BIT(1,D);
                        break;
                    case 0x4B:
                        BIT(1,E);
                        break;
                    case 0x4C:
                        BIT(1,H);
                        break;
                    case 0x4D:
                        BIT(1,L);
                        break;
                    case 0x4E:
                        HL = combineBytesToWord(H,L);
                        BIT(1,m.ReadFromMemory(HL));
                        break;
                    case 0x4F:
                        BIT(1,A);
                        break;
                    case 0x50:
                        BIT(2,B);
                        break;
                    case 0x51:
                        BIT(2,C);
                        break;
                    case 0x52:
                        BIT(2,D);
                        break;
                    case 0x53:
                        BIT(2,E);
                        break;
                    case 0x54:
                        BIT(2,H);
                        break;
                    case 0x55:
                        BIT(2,L);
                        break;
                    case 0x56:
                        HL = combineBytesToWord(H,L);
                        BIT(2,m.ReadFromMemory(HL));
                        break;
                    case 0x57:
                        BIT(2,A);
                        break;
                    case 0x58:
                        BIT(3,B);
                        break;
                    case 0x59:
                        BIT(3,C);
                        break;
                    case 0x5A:
                        BIT(3,D);
                        break;
                    case 0x5B:
                        BIT(3,E);
                        break;
                    case 0x5C:
                        BIT(3,H);
                        break;
                    case 0x5D:
                        BIT(3,L);
                        break;
                    case 0x5E:
                        HL = combineBytesToWord(H,L);
                        BIT(3,m.ReadFromMemory(HL));
                        break;
                    case 0x5F:
                        BIT(3,A);
                        break;
                    case 0x60:
                        BIT(4,B);
                        break;
                    case 0x61:
                        BIT(4,C);
                        break;
                    case 0x62:
                        BIT(4,D);
                        break;
                    case 0x63:
                        BIT(4,E);
                        break;
                    case 0x64:
                        BIT(4,H);
                        break;
                    case 0x65:
                        BIT(4,L);
                        break;
                    case 0x66:
                        HL = combineBytesToWord(H,L);
                        BIT(4,m.ReadFromMemory(HL));
                        break;
                    case 0x67:
                        BIT(4,A);
                        break;
                    case 0x68:
                        BIT(5,B);
                        break;
                    case 0x69:
                        BIT(5,C);
                        break;
                    case 0x6A:
                        BIT(5,D);
                        break;
                    case 0x6B:
                        BIT(5,E);
                        break;
                    case 0x6C:
                        BIT(5,H);
                        break;
                    case 0x6D:
                        BIT(5,L);
                        break;
                    case 0x6E:
                        HL = combineBytesToWord(H,L);
                        BIT(5,m.ReadFromMemory(HL));
                        break;
                    case 0x6F:
                        BIT(5,A);
                        break;
                    case 0x70:
                        BIT(6,B);
                        break;
                    case 0x71:
                        BIT(6,C);
                        break;
                    case 0x72:
                        BIT(6,D);
                        break;
                    case 0x73:
                        BIT(6,E);
                        break;
                    case 0x74:
                        BIT(6,H);
                        break;
                    case 0x75:
                        BIT(6,L);
                        break;
                    case 0x76:
                        HL = combineBytesToWord(H,L);
                        BIT(6,m.ReadFromMemory(HL));
                        break;
                    case 0x77:
                        BIT(6,A);
                        break;
                    case 0x78:
                        BIT(7,B);
                        break;
                    case 0x79:
                        BIT(7,C);
                        break;
                    case 0x7A:
                        BIT(7,D);
                        break;
                    case 0x7B:
                        BIT(7,E);
                        break;
                    case 0x7C:
                        BIT(7,H);
                        break;
                    case 0x7D:
                        BIT(7,L);
                        break;
                    case 0x7E:
                        HL = combineBytesToWord(H,L);
                        BIT(7,m.ReadFromMemory(HL));
                        break;
                    case 0x7F:
                        BIT(7,A);
                        break;
                    case 0x80:
                        B = resetBit(0,B);
                        break;
                    case 0x81:
                        C = resetBit(0,C);
                        break;
                    case 0x82:
                        D = resetBit(0,D);
                        break;
                    case 0x83:
                        E = resetBit(0,E);
                        break;
                    case 0x84:
                        H = resetBit(0,H);
                        break;
                    case 0x85:
                        L = resetBit(0,L);
                        break;
                    case 0x86:
                        HL = combineBytesToWord(H,L);
                        m.WriteToMemory(HL,resetBit(0,m.ReadFromMemory(HL)));
                        break;
                    case 0x87:
                        A = resetBit(0,A);
                        break;
                    case 0x88:
                        B = resetBit(1,B);
                        break;
                    case 0x89:
                        C = resetBit(1,C);
                        break;
                    case 0x8A:
                        D = resetBit(1,D);
                        break;
                    case 0x8B:
                        E = resetBit(1,E);
                        break;
                    case 0x8C:
                        H = resetBit(1,H);
                        break;
                    case 0x8D:
                        L = resetBit(1,L);
                        break;
                    case 0x8E:
                        HL = combineBytesToWord(H,L);
                        m.WriteToMemory(HL,resetBit(1,m.ReadFromMemory(HL)));
                        break;
                    case 0x8F:
                        A = resetBit(1,A);
                        break;
                    case 0x90:
                        B = resetBit(2,B);
                        break;
                    case 0x91:
                        C = resetBit(2,C);
                        break;
                    case 0x92:
                        D = resetBit(2,D);
                        break;
                    case 0x93:
                        E = resetBit(2,E);
                        break;
                    case 0x94:
                        H = resetBit(2,H);
                        break;
                    case 0x95:
                        L = resetBit(2,L);
                        break;
                    case 0x96:
                        HL = combineBytesToWord(H,L);
                        m.WriteToMemory(HL,resetBit(2,m.ReadFromMemory(HL)));
                        break;
                    case 0x97:
                        A = resetBit(2,A);
                        break;
                    case 0x98:
                        B = resetBit(3,B);
                        break;
                    case 0x99:
                        C = resetBit(3,C);
                        break;
                    case 0x9A:
                        D = resetBit(3,D);
                        break;
                    case 0x9B:
                        E = resetBit(3,E);
                        break;
                    case 0x9C:
                        H = resetBit(3,H);
                        break;
                    case 0x9D:
                        L = resetBit(3,L);
                        break;
                    case 0x9E:
                        HL = combineBytesToWord(H,L);
                        m.WriteToMemory(HL,resetBit(3,m.ReadFromMemory(HL)));
                        break;
                    case 0x9F:
                        A = resetBit(3,A);
                        break;
                    case 0xA0:
                        B = resetBit(4,B);
                        break;
                    case 0xA1:
                        C = resetBit(4,C);
                        break;
                    case 0xA2:
                        D = resetBit(4,D);
                        break;
                    case 0xA3:
                        E = resetBit(4,E);
                        break;
                    case 0xA4:
                        H = resetBit(4,H);
                        break;
                    case 0xA5:
                        L = resetBit(4,L);
                        break;
                    case 0xA6:
                        HL = combineBytesToWord(H,L);
                        m.WriteToMemory(HL,resetBit(4,m.ReadFromMemory(HL)));
                        break;
                    case 0xA7:
                        A = resetBit(4,A);
                        break;        
                    case 0xA8:
                        B = resetBit(5,B);
                        break;
                    case 0xA9:
                        C = resetBit(5,C);
                        break;
                    case 0xAA:
                        D = resetBit(5,D);
                        break;
                    case 0xAB:
                        E = resetBit(5,E);
                        break;
                    case 0xAC:
                        H = resetBit(5,H);
                        break;
                    case 0xAD:
                        L = resetBit(5,L);
                        break;
                    case 0xAE:
                        HL = combineBytesToWord(H,L);
                        m.WriteToMemory(HL,resetBit(5,m.ReadFromMemory(HL)));
                        break;
                    case 0xAF:
                        A = resetBit(5,A);
                        break;  
                    case 0xB0:
                        B = resetBit(6,B);
                        break;
                    case 0xB1:
                        C = resetBit(6,C);
                        break;
                    case 0xB2:
                        D = resetBit(6,D);
                        break;
                    case 0xB3:
                        E = resetBit(6,E);
                        break;
                    case 0xB4:
                        H = resetBit(6,H);
                        break;
                    case 0xB5:
                        L = resetBit(6,L);
                        break;
                    case 0xB6:
                        HL = combineBytesToWord(H,L);
                        m.WriteToMemory(HL,resetBit(6,m.ReadFromMemory(HL)));
                        break;
                    case 0xB7:
                        A = resetBit(6,A);
                        break;
                    case 0xB8:
                        B = resetBit(7,B);
                        break;
                    case 0xB9:
                        C = resetBit(7,C);
                        break;
                    case 0xBA:
                        D = resetBit(7,D);
                        break;
                    case 0xBB:
                        E = resetBit(7,E);
                        break;
                    case 0xBC:
                        H = resetBit(7,H);
                        break;
                    case 0xBD:
                        L = resetBit(7,L);
                        break;
                    case 0xBE:
                        HL = combineBytesToWord(H,L);
                        m.WriteToMemory(HL,resetBit(7,m.ReadFromMemory(HL)));
                        break;
                    case 0xBF:
                        A = resetBit(7,A);
                        break;  
                    case 0xC0:
                        B = setBit(0,B);
                        break;
                    case 0xC1:
                        C = setBit(0,C);
                        break;
                    case 0xC2:
                        D = setBit(0,D);
                        break;
                    case 0xC3:
                        E = setBit(0,E);
                        break;
                    case 0xC4:
                        H = setBit(0,H);
                        break;
                    case 0xC5:
                        L = setBit(0,L);
                        break;
                    case 0xC6:
                        HL = combineBytesToWord(H,L);
                        m.WriteToMemory(HL,setBit(0,m.ReadFromMemory(HL)));
                        break;
                    case 0xC7:
                        A = setBit(0,A);
                        break; 
                    case 0xC8:
                        B = setBit(1,B);
                        break;
                    case 0xC9:
                        C = setBit(1,C);
                        break;
                    case 0xCA:
                        D = setBit(1,D);
                        break;
                    case 0xCB:
                        E = setBit(1,E);
                        break;
                    case 0xCC:
                        H = setBit(1,H);
                        break;
                    case 0xCD:
                        L = setBit(1,L);
                        break;
                    case 0xCE:
                        HL = combineBytesToWord(H,L);
                        m.WriteToMemory(HL,setBit(1,m.ReadFromMemory(HL)));
                        break;
                    case 0xCF:
                        A = setBit(1,A);
                        break;  
                     case 0xD0:
                        B = setBit(2,B);
                        break;
                    case 0xD1:
                        C = setBit(2,C);
                        break;
                    case 0xD2:
                        D = setBit(2,D);
                        break;
                    case 0xD3:
                        E = setBit(2,E);
                        break;
                    case 0xD4:
                        H = setBit(2,H);
                        break;
                    case 0xD5:
                        L = setBit(2,L);
                        break;
                    case 0xD6:
                        HL = combineBytesToWord(H,L);
                        m.WriteToMemory(HL,setBit(2,m.ReadFromMemory(HL)));
                        break;
                    case 0xD7:
                        A = setBit(2,A);
                        break;
                    case 0xD8:
                        B = setBit(3,B);
                        break;
                    case 0xD9:
                        C = setBit(3,C);
                        break;
                    case 0xDA:
                        D = setBit(3,D);
                        break;
                    case 0xDB:
                        E = setBit(3,E);
                        break;
                    case 0xDC:
                        H = setBit(3,H);
                        break;
                    case 0xDD:
                        L = setBit(3,L);
                        break;
                    case 0xDE:
                        HL = combineBytesToWord(H,L);
                        m.WriteToMemory(HL,setBit(3,m.ReadFromMemory(HL)));
                        break;
                    case 0xDF:
                        A = setBit(3,A);
                        break;
                    case 0xE0:
                        B = setBit(4,B);
                        break;
                    case 0xE1:
                        C = setBit(4,C);
                        break;
                    case 0xE2:
                        D = setBit(4,D);
                        break;
                    case 0xE3:
                        E = setBit(4,E);
                        break;
                    case 0xE4:
                        H = setBit(4,H);
                        break;
                    case 0xE5:
                        L = setBit(4,L);
                        break;
                    case 0xE6:
                        HL = combineBytesToWord(H,L);
                        m.WriteToMemory(HL,setBit(4,m.ReadFromMemory(HL)));
                        break;
                    case 0xE7:
                        A = setBit(4,A);
                        break;
                    case 0xE8:
                        B = setBit(5,B);
                        break;
                    case 0xE9:
                        C = setBit(5,C);
                        break;
                    case 0xEA:
                        D = setBit(5,D);
                        break;
                    case 0xEB:
                        E = setBit(5,E);
                        break;
                    case 0xEC:
                        H = setBit(5,H);
                        break;
                    case 0xED:
                        L = setBit(5,L);
                        break;
                    case 0xEF:
                        A = setBit(5,A);
                        break;
                    case 0xEE:
                        HL = combineBytesToWord(H,L);
                        m.WriteToMemory(HL,setBit(5,m.ReadFromMemory(HL)));
                        break;
                    case 0xF0:
                        B = setBit(6,B);
                        break;
                    case 0xF1:
                        C = setBit(6,C);
                        break;
                    case 0xF2:
                        D = setBit(6,D);
                        break;
                    case 0xF3:
                        E = setBit(6,E);
                        break;
                    case 0xF4:
                        H = setBit(6,H);
                        break;
                    case 0xF5:
                        L = setBit(6,L);
                        break;
                    case 0xF6:
                        HL = combineBytesToWord(H,L);
                        m.WriteToMemory(HL,setBit(6,m.ReadFromMemory(HL)));
                        break;
                    case 0xF7:
                        A = setBit(6,A);
                        break;
                    case 0xF8:
                        B = setBit(7,B);
                        break;
                    case 0xF9:
                        C = setBit(7,C);
                        break;
                    case 0xFA:
                        D = setBit(7,D);
                        break;
                    case 0xFB:
                        E = setBit(7,E);
                        break;
                    case 0xFC:
                        H = setBit(7,H);
                        break;
                    case 0xFD:
                        L = setBit(7,L);
                        break;
                    case 0xFE:
                        HL = combineBytesToWord(H,L);
                        m.WriteToMemory(HL,setBit(7,m.ReadFromMemory(HL)));
                        break;
                    case 0xFF:
                        A = setBit(7,A);
                        break;           
                    case 0x36:
                        HL = combineBytesToWord(H,L);
                        m.WriteToMemory(HL,SWAP(m.ReadFromMemory(HL)));
                        break;
                    case 0x37:
                        A = SWAP(A);
                        break;
                    case 0x1F:
                        A = RR(A);
                        break;
                    case 0x18:
                        B = RR(B);
                        break;
                    case 0x19:
                        C = RR(C);
                        break;
                    case 0x1A:
                        D = RR(D);
                        break;
                    case 0x1B:
                        E = RR(E);
                        break;
                    case 0x1C:
                        H = RR(H);
                        break;
                    case 0x1D:
                        L = RR(L);
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

    private void printDebugLine() {
        string line = "";
        line = "A: " +A.ToString("X2")+ " F: " +F.ToString("X2")+ " B: "+B.ToString("X2")+" C: "+C.ToString("X2")+" D: "+D.ToString("X2")+" E: "+E.ToString("X2")+" H: "+H.ToString("X2")+" L: "+L.ToString("X2")+" SP: "+SP.ToString("X4")+" PC: 00:" +PC.ToString("X4")+" ("+m.ReadFromMemory(PC).ToString("X2") + " " + m.ReadFromMemory((ushort)(PC+1)).ToString("X2") + " " + m.ReadFromMemory((ushort)(PC+2)).ToString("X2") + " " + m.ReadFromMemory((ushort)(PC+3)).ToString("X2") + ")";
        using(StreamWriter writetext = File.AppendText(("Assets/write.txt")))
        {
            writetext.WriteLine(line);
        }
        //Debug.Log(line);
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

    public static ushort getBitFromWord(byte pos, ushort reg) {
        ushort result = (ushort)((reg & (1 << pos)));
        result = (ushort)(result >> pos); 
        return result;
    }

    private bool isHalfCarryAdd(byte r, byte n) {
        uint result = (uint)((r & 0xf) + (n & 0xf));
        return result > 0xF;
    }

    private bool isHalfCarryAddWithCarry(byte r, byte n, byte carry) {
        uint result = (uint)((r & 0xf) + (n & 0xf) + carry);
        return result > 0xF;
    }

    private bool isHalfCarrySubWithCarry(byte r, byte n, byte carry) {
        bool result = ((r & 0xf) < ((n & 0xf) + carry));
        return result;
    }

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

    private bool isCarryAddWithCarry(byte r, byte n, byte carry) {
        ushort result = (ushort)(r + n + carry);
        return result > 0xFF;
    }

     private bool isCarrySubWithCarry(byte r, byte n, byte carry) {
        return r < (n + carry);
    }

    private bool isCarryAddWord(ushort r, ushort n) {
        uint result = (uint)(r + n);
        return result > 0xFFFF;
    }

    private void clearLowerBitOfF() {
        //bit 0-3 ALWAYS ZERO
        F = (byte)(F & 0xF0);
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
    private uint JRCC(byte cc)  {
        byte b = m.ReadFromMemory(PC++);
		sbyte sb = unchecked((sbyte)(b));
        if(cc == 0x20 && getBit(ZFlag,F) == 0) {
            PC = (ushort)(PC + sb);
            return 12;
        } else if(cc == 0x28 && getBit(ZFlag,F) == 1) {
            PC = (ushort)(PC + sb);
            return 12;
        } else if(cc == 0x30 && getBit(CFlag,F) == 0) {
            PC = (ushort)(PC + sb);
            return 12;
        } else if(cc == 0x38 && getBit(CFlag,F) == 1) {
            PC = (ushort)(PC + sb);
            return 12;
        } else if(cc == 0x18) {
            //No conditional JR
            PC = (ushort)(PC + sb);
            return 12;
        } else {
            //Not jump increase cycle by 8.
            return 8;
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

    // page 102
    private byte SRL(byte n) {
        byte bitZero = getBit(0,n);
        byte result = (byte)(n >> 1);
        F = (result == 0) ? setBit(ZFlag,F) : resetBit(ZFlag,F);
        F = resetBit(NFlag,F);
        F = resetBit(HFlag,F);
        F = (bitZero == 1) ? setBit(CFlag,F) : resetBit(CFlag,F);
        clearLowerBitOfF();
        return result;
    }

    // page 101
    private byte SLA(byte n) {
        byte bitZero = getBit(7,n);
        byte result = (byte)(n << 1);
        F = (result == 0) ? setBit(ZFlag,F) : resetBit(ZFlag,F);
        F = resetBit(NFlag,F);
        F = resetBit(HFlag,F);
        F = (bitZero == 1) ? setBit(CFlag,F) : resetBit(CFlag,F);
        clearLowerBitOfF();
        return result;
    }

    // page 101
    private byte SRA(byte n) {
        byte bitZero = getBit(0,n);
        byte bitSeven = getBit(7,n);
        byte result = (byte)(n >> 1);
        result = (bitSeven == 1) ? setBit(7,result) : resetBit(7,result);
        F = (result == 0) ? setBit(ZFlag,F) : resetBit(ZFlag,F);
        F = resetBit(NFlag,F);
        F = resetBit(HFlag,F);
        F = (bitZero == 1) ? setBit(CFlag,F) : resetBit(CFlag,F);
        clearLowerBitOfF();
        return result;
    }

    //page 100
    private byte RR(byte n) {
        byte bit = getBit(0,n);
        byte result = (byte)((n >> 1) | (byte)(getBit(CFlag,F)) << 7);
        F = (result == 0x00) ? setBit(ZFlag,F) : resetBit(ZFlag,F);
        F = resetBit(NFlag,F);
        F = resetBit(HFlag,F);
        F = (bit == 0x00) ? resetBit(CFlag,F) : setBit(CFlag,F);
        clearLowerBitOfF();
        return result;
    }

    //page 98
    private byte RRA(byte n) {
        byte result = RR(n);
        F = resetBit(ZFlag,F);
        clearLowerBitOfF();
        return result;
    }

    //page 92
    private byte ADC(byte r, byte n) {
        byte cy = getBit(CFlag,F);
        byte result = (byte)(r+n+cy);
        F = (result == 0) ? setBit(ZFlag,F) : resetBit(ZFlag,F);
        F = resetBit(NFlag,F);
        F = (isHalfCarryAddWithCarry(r,n,cy)) ? setBit(HFlag,F) : resetBit(HFlag,F);
        F = (isCarryAddWithCarry(r,n,cy)) ? setBit(CFlag,F) : resetBit(CFlag,F);
        clearLowerBitOfF();
        return result;
    }

    //page 92
    private byte SBC(byte r, byte n) {
        byte cy = getBit(CFlag,F);
        byte result = (byte)(r-n-cy);
        F = (result == 0) ? setBit(ZFlag,F) : resetBit(ZFlag,F);
        F = setBit(NFlag,F);
        F = (isHalfCarrySubWithCarry(r,n,cy)) ? setBit(HFlag,F) : resetBit(HFlag,F);
        F = (isCarrySubWithCarry(r,n,cy)) ? setBit(CFlag,F) : resetBit(CFlag,F);
        clearLowerBitOfF();
        return result;
    }

    // http://www.devrs.com/gb/files/GBCPU_Instr.html#SCF
    private void SCF() {
        F = resetBit(NFlag,F);
        F = resetBit(HFlag,F);
        F = setBit(CFlag,F);
        clearLowerBitOfF();
    }

    // http://www.devrs.com/gb/files/GBCPU_Instr.html#CCF
    private void CCF() {
        byte getC = getBit(CFlag,F);
        F = resetBit(NFlag,F);
        F = resetBit(HFlag,F);
        F = (getC == 1) ? resetBit(CFlag,F) : setBit(CFlag,F);
        clearLowerBitOfF();
    }

    // page 99 
    private byte RLC(byte r) {
        byte getBitFromR = getBit(7,r);
        r = (byte)(r << 1);
        F = resetBit(NFlag,F);
        F = resetBit(HFlag,F);
        F = (getBitFromR == 1) ? setBit(CFlag,F) : resetBit(CFlag,F);
        byte result = (getBitFromR == 1 ) ? setBit(0,r) : resetBit(0,r);
        F = (result == 0) ? setBit(ZFlag,F) : resetBit(ZFlag,F);
        clearLowerBitOfF();
        return result;
    }

    // page 99 
    private byte RRC(byte r) {
        byte getBitFromR = getBit(0,r);
        r = (byte)(r >> 1);
        F = resetBit(NFlag,F);
        F = resetBit(HFlag,F);
        F = (getBitFromR == 1) ? setBit(CFlag,F) : resetBit(CFlag,F);
        byte result = (getBitFromR == 1 ) ? setBit(7,r) : resetBit(7,r);
        F = (result == 0) ? setBit(ZFlag,F) : resetBit(ZFlag,F);
        clearLowerBitOfF();
        return result;
    }

    // page 103
    private void BIT(byte bit, byte r) {
        byte bitFromR = getBit(bit,r);
        F = (bitFromR == 0) ? setBit(ZFlag,F) : resetBit(ZFlag,F);
        F = resetBit(NFlag,F);
        F = setBit(HFlag,F);
        clearLowerBitOfF();
    }

    // page 110
    private byte DAA(byte r) {
        byte reg_A = r;

        if (getBit(NFlag,F) == 1) {
				if (getBit(HFlag,F) == 1) {
					reg_A += 0xFA;
				}
				if (getBit(CFlag,F) == 1) {
					reg_A += 0xA0;
				}
		} else {
            ushort tempReg_A = reg_A;
            if ((reg_A & 0xf) > 0x9 || getBit(HFlag,F) == 1) {
                tempReg_A += 0x6;
            }
            if ((tempReg_A & 0x1f0) > 0x90 || getBit(CFlag,F) == 1) {
                tempReg_A += 0x60;
                //setC(true);
                F = setBit(CFlag,F);
            }
            else {
                //setC(false);
                F = resetBit(CFlag,F);
            }
            reg_A = (byte)(tempReg_A);
        }
        //setH(false);
        F = resetBit(HFlag,F);
		//setZ(reg_A == 0);
        F = (reg_A == 0) ? setBit(ZFlag,F) : resetBit(ZFlag,F);
        return reg_A;
    }
}
