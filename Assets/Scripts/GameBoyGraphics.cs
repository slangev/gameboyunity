using UnityEngine;

public class GameBoyGraphic
{
    Texture2D screen;
    uint width;
    uint height;
    private GameBoyInterrupts interrupts;
    private GameBoyMemory memory;
    private uint GPUCycleCount;

    public static readonly ushort LCDCAddr = 0xFF40;
    public static readonly ushort STATAddr = 0xFF41;
    public static readonly ushort SCYAddr = 0xFF42;
    public static readonly ushort SCXAddr = 0xFF43;
    public static readonly ushort LYAddr = 0xFF44;
    public static readonly ushort LYCAddr = 0xFF45;
    public static readonly ushort DMAAddr = 0xFF46;
    public static readonly ushort BGPAddr = 0xFF47;
    public static readonly ushort OBP0Addr = 0xFF48;
    public static readonly ushort OBP1Addr = 0xFF49;
    public static readonly ushort WYAddr = 0xFF4A;
    public static readonly ushort WXAddr = 0xFF4B;

    public GameBoyGraphic(uint width, uint height, Texture2D screen, GameBoyInterrupts interrupts, GameBoyMemory memory) {
        this.width = width;
        this.height = height;
        this.screen = screen;
        this.interrupts = interrupts;
        this.memory = memory;
    }

    //https://gbdev.io/pandocs/ Look at Pixel FIFO section
    public void UpdateGraphics(uint cycles) {
        uint mode = (uint)(memory.ReadFromMemory(STATAddr) & 0x03);
        GPUCycleCount += cycles;
        bool ldcEnabled = ((memory.ReadFromMemory(LCDCAddr) & 0x80) != 0) ? true : false;
        if(ldcEnabled) {
            // This mode is h-blank mode
            if(mode == 0) {
                // End of h-blank
                if (GPUCycleCount >= 204) {
					GPUCycleCount -= 204;
                    memory.IncrementReg(LYAddr);
                    byte LYvalue = memory.ReadFromMemory(LYAddr);
					
                    //Start of V-blank
					if (LYvalue == 144) {
						mode = 1;	// Switch to v-blank
						if ((memory.ReadFromMemory(STATAddr) & 0x10) != 0) {
							interrupts.RequestInterrupt(GameBoyInterrupts.LDDCBit);
						}
						// Request vblank interrupt
						interrupts.RequestInterrupt(GameBoyInterrupts.VBlankBit);
					}
                    //Move to next Line which start at mode 2.
					else {
						mode = 2;
						if ((memory.ReadFromMemory(STATAddr) & 0x20) != 0) {
							interrupts.RequestInterrupt(GameBoyInterrupts.LDDCBit);
						}
					}
				}
            //This mode is v-blank mode 
            } else if(mode == 1) { 
                //End of v-blank cycle
                if(GPUCycleCount >= 456) {
                    GPUCycleCount -= 456;
                    memory.IncrementReg(LYAddr);
                    byte LY = memory.ReadFromMemory(LYAddr);

                    if(LY == 153) {
                        memory.WriteToMemory(LYAddr, 0);
                    } else if(LY == 1) {
                        mode = 2;
                        if ((memory.ReadFromMemory(STATAddr) & 0x20) != 0) {
							interrupts.RequestInterrupt(GameBoyInterrupts.LDDCBit);
						}
                        memory.WriteToMemory(LYAddr, 0);
                    }
                }
               
            //This mode is searching objects mode
            } else if(mode == 2) {
                if (GPUCycleCount >= 80) {
					GPUCycleCount -= 80;
					mode = 3;
				}
            //This mode is drawing mode 
            } else if(mode == 3) {
                if (GPUCycleCount >= 172) {
					GPUCycleCount -= 172;
					//GPUCycleCount = 0;
					mode = 0;
                    if ((memory.ReadFromMemory(STATAddr) & 0x20) != 0) {
                        interrupts.RequestInterrupt(GameBoyInterrupts.LDDCBit);
                    }
					// Render scanline before going to hblank
					// renderScanline();
				}
            }
        } else {
            GPUCycleCount = 0;
			memory.WriteToMemory(LYAddr, 0);
		    mode = 0; // Probably needs to start at two or one
            memory.WriteToMemory(STATAddr, (byte)(memory.ReadFromMemory(STATAddr) & 0xFC));
        }
        memory.WriteToMemory(STATAddr, (byte)(memory.ReadFromMemory(STATAddr) & 0xFC));
        memory.WriteToMemory(STATAddr, (byte)(memory.ReadFromMemory(STATAddr) | mode));
        LYCInterrupt();
    }

    private void LYCInterrupt() {
        byte LYValue = memory.ReadFromMemory(LYAddr);
        byte LYCValue = memory.ReadFromMemory(LYCAddr);
        if(LYValue == LYCAddr) {
            memory.WriteToMemory(STATAddr, (byte)(memory.ReadFromMemory(STATAddr) | 0x04));
        } else {
            memory.WriteToMemory(STATAddr, (byte)(memory.ReadFromMemory(STATAddr) & 0xFB));
	    }
        //Check if LYC interrupt is enable and requesting.
        if ((byte)(memory.ReadFromMemory(STATAddr) & 0x60) != 0 && ((byte)(memory.ReadFromMemory(STATAddr) & 0x04) != 0)) {
			interrupts.RequestInterrupt(GameBoyInterrupts.LDDCBit);
        }
    }

    public void DrawScreen() {
        uint row = height-1;
        for (int x = 0; x < screen.height; x++)
        {
            //int col = 0;
            for (int y = 0; y < screen.width; y++)
            {
                Color pixelColour;
                int n = Random.Range(0,2); //50/50 chance it will be 0 or 1
                if (n == 0) 
                {
                    pixelColour = new Color(0, 0, 0, 1); //Black
                }
                else
                {
                    pixelColour = new Color(1, 1, 1, 1); //White
                }
                screen.SetPixel(y, x, pixelColour);
            }
            row--;
        }
        screen.Apply();
    }
}
