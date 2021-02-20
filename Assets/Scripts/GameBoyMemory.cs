using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class GameBoyMemory
{
    internal class WorkRam {
        byte WRAMBank = 1;
	    byte[] RAM = new byte[0x8000];
        public WorkRam() {

        }
        public void Write(ushort address, byte data) {
            //Debug.Log("WORK RAM WRITE: " + data.ToString("X2"));
            if (address >= 0xC000 && address <= 0xFDFF) {
                ushort wramAddress = (ushort)((address & 0x1FFF));
                if (wramAddress >= 0x1000) {
                    wramAddress &= 0xFFF;
                    wramAddress |= (ushort)(WRAMBank << 12);
                }
                RAM[wramAddress] = data;	// Mask covers echo area too
            }
            else if (address == 0xFF70) {
                WRAMBank = (byte)(data & 0x7);
                if (WRAMBank == 0) {
                   WRAMBank = 1;
                }
            }
        }

        public byte Read(ushort address) {
            //Debug.Log("READ TO WRAM: " + address.ToString("X2"));
            byte returnVal = 0xFF;
            if (address >= 0xC000 && address <= 0xFDFF) {
                ushort wramAddress = (ushort)((address & 0x1FFF));
                if (wramAddress >= 0x1000) {
                    wramAddress &= 0xFFF;
                    wramAddress |= (ushort)(WRAMBank << 12);
                }
                returnVal = RAM[wramAddress];	// Mask covers echo area too
            }
            else if (address == 0xFF70) {
                returnVal = WRAMBank;
            }
            return returnVal;
        }
    }
    private uint memorySize = 0x10000;
    public readonly ushort DMA = 0xFF46;
    private ushort KEY1 = 0xFF4D;
    private static byte joypadState = 0xFF; // All buttons are up
    private ushort biosSize;
    private List<byte> memory {get; set;}
    private GameBoyCartiridge gbCart;
    private GameBoyTimer gbTimer;
    private GameBoyGraphic gbGraphic;
    private GameBoyAudio gbAudio;
    private WorkRam workRam;
    private uint speed = 1; //1 == normal, 2 == double
    public GameBoyMemory(GameBoyCartiridge gbCart) {
        this.gbCart = gbCart;
        this.workRam = new WorkRam();
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
            if(address >= 0x104 && address <= 0x150) {
                return gbCart.Read(address);
            }
		    if (memory[0xFF50] == 0 && address < 0x100) {
			    return memory[address];
		    } else if(memory[0xFF50] == 0 && GameBoyCartiridge.IsGameBoyColor) {
                return memory[address];
            }
			return gbCart.Read(address);
		} else if (address >= 0x8000 && address <= 0x9FFF) {
		    return gbGraphic.Read(address);
	    } else if(address >= 0xA000 && address <= 0xBFFF) {
            return gbCart.Read(address);
        } else if (address >= 0xC000 && address <= 0xFDFF) {
            return workRam.Read(address);
	    } else if(address == 0xFF00) {
            return getJoyPadState();
        } else if(address >= 0xFF10 && address <= 0xFF3F){
            return gbAudio.Read(address);
        } else if((address == 0xFF4F || (address >= 0xFF51 && address <= 0xFF55) || 
		    (address >= 0xFF68 && address <= 0xFF6B)) & GameBoyCartiridge.IsGameBoyColor) {
		    return gbGraphic.Read(address);
	    } else if(address == 0xFF70) {
		    return workRam.Read(address);
	    } else if(address == KEY1) {
            return memory[address];
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
        } else if (address >= 0xC000 && address <= 0xFDFF) {
            workRam.Write(address,data);
	    } else if(address == DMA){
            DMATransfer(data);
        } else if ((address == 0xFF4F || (address >= 0xFF51 && address <= 0xFF55) || 
		    (address >= 0xFF68 && address <= 0xFF6B)) & GameBoyCartiridge.IsGameBoyColor) {
		    gbGraphic.Write(address,data);
	    } else if(address >= 0xFF01 && address <= 0xFF02) {
            Debug.Log("Link Port");
        } else if(address >= 0xFF10 && address <= 0xFF3F) {
            gbAudio.Write(address,data);
        } else if(address == GameBoyGraphic.STATAddr) {
            memory[address] = (byte)((memory[address] & 0x7) | (data & 0xF8));
        } else if (address == 0xFF70 && GameBoyCartiridge.IsGameBoyColor) {
            workRam.Write(address,data);
	    } else if (address == KEY1) {
            data = (byte)(data & 0x7f);
            memory[address] = data;
        } else {
            memory[address] = data;
        }
        return true;
    }

    public bool isPrepared() {
        return (byte)(memory[KEY1] & 0x01) == 1 ? true : false;
    }

    public void unSetPrepared() {
       memory[KEY1] = GameBoyCPU.resetBit(0,memory[KEY1]);
    }

    private void setSpeedBit() {
        memory[KEY1] = GameBoyCPU.setBit(7,memory[KEY1]);
    }

    private void resetSpeedBit() {
        memory[KEY1] = GameBoyCPU.resetBit(7,memory[KEY1]);
    }

    private byte getSpeedBit() {
        return GameBoyCPU.getBit(7,memory[KEY1]);
    }

    public void setSpeed() {
        byte speedBit = getSpeedBit();
        if(speedBit == 0) {
            setSpeedBit();
        } else {
            resetSpeedBit();
        }
        speed = (uint)((GameBoyCPU.getBit(7,memory[KEY1])) == 1 ? 2 : 1); // if 1 we switch to normal speed (1) and if 0 we switch to double speed (2)
    }

    public uint GetSpeed() {
        return speed;
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
