using UnityEngine;
using System.Collections.Generic;

public class GameBoyGraphic
{
    Texture2D screen;
    readonly private uint width;
    readonly private uint height;
    private GameBoyInterrupts interrupts;
    private GameBoyMemory memory;
    private uint GPUCycleCount;

    // Frame Buffer
    public List<List<Color>> videoMemory;
    private List<int> bgWinPriority; 
    private List<int> bgWinColorResult;

    public static readonly ushort LCDCAddr = 0xFF40;
    public static readonly ushort OAMStartAdress = 0xFE00;
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
    private byte windowLine {get; set;} = 0;

    bool CGBmode = false;
    bool HDMAActive = false;
    ushort HDMAsrc = 0;
	ushort HDMAdst = 0;
	ushort HDMAlen = 0xFF;	
    byte cgbBGPaletteIndex = 0;
	byte[] cgbBGPalette = new byte[0x40];
	byte cgbSPRPaletteIndex = 0;
	byte[] cgbSPRPalette = new byte[0x40];
	byte VRAMBank = 0;
    byte[] VRAM = new byte[0x4000];

    public GameBoyGraphic(uint width, uint height, Texture2D screen, GameBoyInterrupts interrupts, GameBoyMemory memory) {
        this.width = width;
        this.height = height;
        this.screen = screen;
        this.interrupts = interrupts;
        this.memory = memory;
        videoMemory = new List<List<Color>>();
        for(int i = 0; i < this.height; i++) {
            videoMemory.Add(new List<Color>(new Color[this.width]));
        }
        bgWinPriority = new List<int>();
        bgWinColorResult = new List<int>(); 
        for(int i = 0; i < width; i++) {
            bgWinPriority.Add(0);
            bgWinColorResult.Add(0);
        }
        this.CGBmode = GameBoyCartiridge.IsGameBoyColor;
    }

    //https://gbdev.io/pandocs/ Look at Pixel FIFO section
    public void UpdateGraphics(uint cycles) {
        uint mode = (uint)(memory.ReadFromMemory(STATAddr) & 0x03);
        GPUCycleCount += cycles;
        bool lcdcEnabled = ((memory.ReadFromMemory(LCDCAddr) & 0x80) != 0) ? true : false;
        if(lcdcEnabled) {
            // This mode is h-blank mode
            if(mode == 0) {
                // End of h-blank
                if (GPUCycleCount >= 204) {
					GPUCycleCount -= 204;
                    memory.IncrementReg(LYAddr);
                    LYCInterrupt();
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
                    LYCInterrupt();
                    byte LY = memory.ReadFromMemory(LYAddr);

                    if(LY == 153) {
                        memory.WriteToMemory(LYAddr, 0);
                        LYCInterrupt();
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
					mode = 0;
                    if ((memory.ReadFromMemory(STATAddr) & 0x20) != 0) {
                        interrupts.RequestInterrupt(GameBoyInterrupts.LDDCBit);
                    }
					// Render scanline before going to hblank
					renderScanLine(lcdcEnabled);
                    if (HDMAActive) {
						if ((HDMAlen & 0x7F) == 0) {
							HDMAActive = false;
						}
						HDMA();
						HDMAlen--;
					}
				}
            }
        } else {
            GPUCycleCount = 0;
			memory.WriteToMemory(LYAddr, 0);
		    mode = 0; // Probably needs to start at two or one
            memory.WriteDirectly(STATAddr, (byte)(memory.ReadFromMemory(STATAddr) & 0xFC));
        }
        
        memory.WriteDirectly(STATAddr, (byte)(memory.ReadFromMemory(STATAddr) & 0xFC));
        memory.WriteDirectly(STATAddr, (byte)(memory.ReadFromMemory(STATAddr) | mode));
    }

    private void renderScanLine(bool lcdcEnabled) {
        if (lcdcEnabled) {
            renderBackground();
            renderWindow();
            renderSprites();
        }
    }

    private void renderWindow() {
        bool windowEnabled = ((memory.ReadFromMemory(LCDCAddr) & 0x20) != 0) ? true : false;
        bool windowTileSelect = ((memory.ReadFromMemory(LCDCAddr) & 0x40) != 0) ? true : false;
        if(windowEnabled) {
            if(CGBmode) {
                byte windowX = (byte)(memory.ReadFromMemory(WXAddr));
                if(windowX < 7) {
                    windowX = 7;
                }
                windowX -= 7;
                byte windowY = memory.ReadFromMemory(WYAddr);
                byte scrollX = memory.ReadFromMemory(SCXAddr);
                byte LY = memory.ReadFromMemory(LYAddr);
                if (windowX >= 160 || windowY > LY) {
                    return;
                }
                ushort tileData = (ushort)(((memory.ReadFromMemory(LCDCAddr) & 0x10) != 0) ? 0x8000 : 0x8800); // Bit 4 - BG & Window Tile Data Select (0=8800-97FF, 1=8000-8FFF)
                bool signed = (tileData == 0x8800) ? true : false; // 0x8800 uses signed data
                ushort backgroundMemory = 0;
                if(windowTileSelect) {
                    backgroundMemory = 0x9C00;
                } else {
                    backgroundMemory = 0x9800;
                }
                byte yPos = windowLine++;
                short tileRow = (short)((yPos/8) * 32); // There are 32 rows and each row has 32 tiles. Each tile is 8 pixel tall. 

                for(int pixel = windowX; pixel < 160; pixel++) {
                    byte xPos = (byte)(pixel+scrollX); // xPos equals the current pixel we are working on. It's part of the calculation for tileCol. Same idea as tileRow.
                    if(pixel >= windowX) {
                        xPos = (byte)(pixel - windowX);
                    }
                    short tileCol = (short)(xPos/8);
                    short tileNum;
                    ushort tileAddress = (ushort)(backgroundMemory + tileRow + tileCol);
                    if(!signed)
                        tileNum = (byte)(VRAM[(tileAddress - 0x8000)]);
                    else
                        tileNum = (sbyte)(VRAM[(tileAddress - 0x8000)]);
                    ushort tileLocation = (ushort)(tileData);
                    if(!signed)
                        tileLocation += (ushort) (tileNum * 16);
                    else
                        tileLocation += (ushort) ((tileNum+128) * 16);

                    ushort line = (ushort)((yPos % 8));
                    // Read two bytes of data. These bytes determine the color of the pixel
                    ushort bankOffSet = 0x8000;
                    byte tileAttr = (byte)(VRAM[tileAddress-0x6000]);
		            if(GameBoyCartiridge.IsGameBoyColor && GameBoyCPU.getBit(3,tileAttr) == 1) {
                        bankOffSet = 0x6000;
                    }

                    //v-flip
                    if(GameBoyCPU.getBit(6,tileAttr) == 1) {
                        line = (ushort)(7 - line) ;
                        line = (ushort)(line * 2);
                    } else {
                        line = (ushort)((yPos % 8) * 2);
                    }

                    byte data1 = (byte)(VRAM[tileLocation + line - bankOffSet]);
                    byte data2 = (byte)(VRAM[tileLocation + line + 1 - bankOffSet]);

                    //x-flip
                    if(GameBoyCPU.getBit(5,tileAttr) == 1) {
                        xPos = (byte)(7 - xPos);
                    }

                    byte colorBit = (byte)(((xPos % 8) -7) * -1);

                    byte bitFromData1 = GameBoyCPU.getBit(colorBit,data1);
                    byte bitFromData2 = GameBoyCPU.getBit(colorBit,data2);
                    byte colorNum = 0;
                    if(bitFromData1 == 1) {
                        colorNum = GameBoyCPU.setBit(0,colorNum);
                    }
                    if(bitFromData2 == 1) {
                        colorNum = GameBoyCPU.setBit(1,colorNum);
                    }

                    // Go through color template
                    byte cgbPalette = (byte)(tileAttr & 0x7);
                    int index =  (cgbPalette << 2) | 0x20;
                    index = (int)(index | colorNum);
                    index = index & 0x1F;
                    index = index << 1;
		            int colorData = cgbBGPalette[index]|((cgbBGPalette[index|1]) << 8); // Combine two bytes into one int
                    colorData &= 0x7FFF; // Mask out the unneeded bits after bit 14
                    Color c = getRGBColors(colorData);
                    bgWinPriority[pixel] = GameBoyCPU.getBit(7, tileAttr); // either 0 or 1 hopefully.
                    bgWinColorResult[pixel] = colorNum;
                    videoMemory[LY][pixel]=c;
                }

            } else {
                byte windowX = (byte)(memory.ReadFromMemory(WXAddr));
                if(windowX < 7) {
                    windowX = 7;
                }
                windowX -= 7;
                byte windowY = memory.ReadFromMemory(WYAddr);
                byte scrollX = memory.ReadFromMemory(SCXAddr);
                byte LY = memory.ReadFromMemory(LYAddr);
                if (windowX >= 160 || windowY > LY) {
                    return;
                }
                ushort tileData = (ushort)(((memory.ReadFromMemory(LCDCAddr) & 0x10) != 0) ? 0x8000 : 0x8800); // Bit 4 - BG & Window Tile Data Select (0=8800-97FF, 1=8000-8FFF)
                bool signed = (tileData == 0x8800) ? true : false; // 0x8800 uses signed data
                ushort backgroundMemory = 0;
                if(windowTileSelect) {
                    backgroundMemory = 0x9C00;
                } else {
                    backgroundMemory = 0x9800;
                }
                byte yPos = windowLine++;
                short tileRow = (short)((yPos/8) * 32); // There are 32 rows and each row has 32 tiles. Each tile is 8 pixel tall. 

                for(int pixel = windowX; pixel < 160; pixel++) {
                    byte xPos = (byte)(pixel+scrollX); // xPos equals the current pixel we are working on. It's part of the calculation for tileCol. Same idea as tileRow.
                    if(pixel >= windowX) {
                        xPos = (byte)(pixel - windowX);
                    }
                    short tileCol = (short)(xPos/8);
                    short tileNum;
                    ushort tileAddress = (ushort)(backgroundMemory + tileRow + tileCol);
                    if(!signed)
                        tileNum = (byte)(VRAM[(tileAddress - 0x8000)]);
                    else
                        tileNum = (sbyte)(VRAM[(tileAddress - 0x8000)]);
                    ushort tileLocation = (ushort)(tileData);
                    if(!signed)
                        tileLocation += (ushort) (tileNum * 16);
                    else
                        tileLocation += (ushort) ((tileNum+128) * 16);

                    byte line = (byte)(yPos % 8);
                    line *= 2;
                    // Read two bytes of data. These bytes determine the color of the pixel
                    ushort bankOffSet = 0x8000;

                    byte data1 = (byte)(VRAM[tileLocation + line - bankOffSet]);
                    byte data2 = (byte)(VRAM[tileLocation + line + 1 - bankOffSet]);

                    byte colorBit = (byte)(((xPos % 8) -7) * -1);

                    byte bitFromData1 = GameBoyCPU.getBit(colorBit,data1);
                    byte bitFromData2 = GameBoyCPU.getBit(colorBit,data2);
                    byte colorNum = 0;
                    if(bitFromData1 == 1) {
                        colorNum = GameBoyCPU.setBit(0,colorNum);
                    }
                    if(bitFromData2 == 1) {
                        colorNum = GameBoyCPU.setBit(1,colorNum);
                    }

                    // Go through color template
                    byte colorTemplate = (byte)memory.ReadFromMemory((ushort)(BGPAddr));
                    var getColorResult = getColor(colorNum,colorTemplate);
                    Color c = getColorResult.Item1;
                    bgWinPriority[pixel] = colorNum;
                    videoMemory[LY][pixel]=c;
                }
            }
        }
    }

    private void renderBackground() {
        bool backgroundEnabled = ((memory.ReadFromMemory(LCDCAddr) & 0x1) != 0) ? true : false;
        bool backgroundTileSelect = ((memory.ReadFromMemory(LCDCAddr) & 0x8) != 0) ? true : false;
        byte scrollY = memory.ReadFromMemory(SCYAddr);
        byte scrollX = memory.ReadFromMemory(SCXAddr);
        byte LY = memory.ReadFromMemory(LYAddr);
        ushort tileData = (ushort)(((memory.ReadFromMemory(LCDCAddr) & 0x10) != 0) ? 0x8000 : 0x8800); // Bit 4 - BG & Window Tile Data Select (0=8800-97FF, 1=8000-8FFF)
        bool signed = (tileData == 0x8800) ? true : false; // 0x8800 uses signed data
        ushort backgroundMemory = 0;

        if(backgroundTileSelect) {
            backgroundMemory = 0x9C00;
        } else {
            backgroundMemory = 0x9800;
        }
        byte yPos = (byte)(scrollY + LY); // yPos equals the current tile row/pixel
        if(backgroundEnabled) {
            if(CGBmode) {
                for(int pixel = 0; pixel < 160; pixel++) {
                    short tileRow = (short)((yPos/8) * 32); // There are 32 rows and each row has 32 tiles. Each tile is 8 pixel tall. 
                    byte xPos = (byte)(pixel+scrollX); // xPos equals the current pixel we are working on. It's part of the calculation for tileCol. Same idea as tileRow.
                    short tileCol = (short)(xPos/8);
                    ushort tileAddress = (ushort)(backgroundMemory + tileRow + tileCol); //index into VRAM
                    short tileNum;
                    if(!signed)
                        tileNum = (byte)(VRAM[(tileAddress - 0x8000)]);
                    else
                        tileNum = (sbyte)(VRAM[(tileAddress - 0x8000)]);
                    
                    ushort tileLocation = (ushort)(tileData);
                    if(!signed)
                        tileLocation += (ushort) (tileNum * 16);
                    else
                        tileLocation += (ushort) ((tileNum+128) * 16);

                    ushort line = (ushort)((yPos % 8));
                    // Read two bytes of data. These bytes determine the color of the pixel

                    ushort bankOffSet = 0x8000;
                    byte tileAttr = (byte)(VRAM[tileAddress-0x6000]);
		            if(GameBoyCartiridge.IsGameBoyColor && GameBoyCPU.getBit(3,tileAttr) == 1) {
                        bankOffSet = 0x6000;
                    }
                    //v-flip
                    if(GameBoyCPU.getBit(6,tileAttr) == 1) {
                        line = (ushort)(7 - line) ;
                        line = (ushort)(line * 2);
                    } else {
                        line = (ushort)((yPos % 8) * 2);
                    }
                    
                    byte data1 = (byte)(VRAM[tileLocation + line - bankOffSet]);
                    byte data2 = (byte)(VRAM[tileLocation + line + 1 - bankOffSet]);

                    //x-flip
                    if(GameBoyCPU.getBit(5,tileAttr) == 1) {
                        xPos = (byte)(7 - xPos);
                    }
                    byte colorBit = (byte)(((xPos % 8) -7) * -1);
                    
                    byte bitFromData1 = GameBoyCPU.getBit(colorBit,data1);
                    byte bitFromData2 = GameBoyCPU.getBit(colorBit,data2);
                    byte colorNum = 0;
                    if(bitFromData1 == 1) {
                        colorNum = GameBoyCPU.setBit(0,colorNum);
                    }
                    if(bitFromData2 == 1) {
                        colorNum = GameBoyCPU.setBit(1,colorNum);
                    }

                    // Go through color template
                    byte cgbPalette = (byte)(tileAttr & 0x7);
                    int index =  (cgbPalette << 2) | 0x20;
                    index = (int)(index | colorNum);
                    index = index & 0x1F;
                    index = index << 1;
		            int colorData = cgbBGPalette[index]|((cgbBGPalette[index|1]) << 8); // Combine two bytes into one int
                    colorData &= 0x7FFF; // Mask out the unneeded bits after bit 14
                    Color c = getRGBColors(colorData);
                    bgWinPriority[pixel] = GameBoyCPU.getBit(7, tileAttr); // either 0 or 1 hopefully.
                    bgWinColorResult[pixel] = colorNum;
                    videoMemory[LY][pixel]=c;
                }
            } else {
                for(int pixel = 0; pixel < 160; pixel++) {
                    short tileRow = (short)((yPos/8) * 32);
                    byte xPos = (byte)(pixel+scrollX); // xPos equals the current pixel we are working on. It's part of the calculation for tileCol. Same idea as tileRow.
                    short tileCol = (short)(xPos/8);
                    ushort tileAddress = (ushort)(backgroundMemory + tileRow + tileCol); //index into VRAM
                    short tileNum;
                    if(!signed)
                        tileNum = (byte)(VRAM[(tileAddress - 0x8000)]);
                    else
                        tileNum = (sbyte)(VRAM[(tileAddress - 0x8000)]);
                    
                    ushort tileLocation = (ushort)(tileData);
                    if(!signed)
                        tileLocation += (ushort) (tileNum * 16);
                    else
                        tileLocation += (ushort) ((tileNum+128) * 16);

                    byte line = (byte)(yPos % 8);
                    line *= 2;
                    // Read two bytes of data. These bytes determine the color of the pixel

                    ushort bankOffSet = 0x8000;
                    
                    byte data1 = (byte)(VRAM[tileLocation + line - bankOffSet]);
                    byte data2 = (byte)(VRAM[tileLocation + line + 1 - bankOffSet]);

                    byte colorBit = (byte)(((xPos % 8) -7) * -1);

                    byte bitFromData1 = GameBoyCPU.getBit(colorBit,data1);
                    byte bitFromData2 = GameBoyCPU.getBit(colorBit,data2);
                    byte colorNum = 0;
                    if(bitFromData1 == 1) {
                        colorNum = GameBoyCPU.setBit(0,colorNum);
                    }
                    if(bitFromData2 == 1) {
                        colorNum = GameBoyCPU.setBit(1,colorNum);
                    }

                    // Go through color template
                    byte colorTemplate = (byte)memory.ReadFromMemory((ushort)(BGPAddr));
                    var getColorResult = getColor(colorNum,colorTemplate);
                    Color c = getColorResult.Item1;
                    bgWinPriority[pixel] = colorNum;
                    videoMemory[LY][pixel]=c;
                }
            }
        }
    }

    private void renderSprites() {
        bool spritesEnabled = ((memory.ReadFromMemory(LCDCAddr) & 0x2) != 0) ? true : false;
        if(spritesEnabled) {
            bool use8x16 = ((memory.ReadFromMemory(LCDCAddr) & 0x4) != 0) ? true : false;
            int ysize = (use8x16) ? 16 : 8;
            byte LY = memory.ReadFromMemory(LYAddr);
            int spritecount = 0;
            List<int> set = new List<int>(new int[160]);
            for(int i = 0; i < 160; i++) {
                set[i] = -1;
            }
            if(CGBmode) {
                for(int i = 0; i < 40 && spritecount < 10; i++) {
                    int PosY = (int)(memory.ReadFromMemory((ushort)(OAMStartAdress + (i * 4))) - 16);
                    byte PosX = (byte)(memory.ReadFromMemory((ushort)(OAMStartAdress + (i * 4) + 1)) - 8);
                    byte tileID = memory.ReadFromMemory((ushort)(OAMStartAdress + (i * 4) + 2));
                    byte attributes = memory.ReadFromMemory((ushort)(OAMStartAdress + (i * 4) + 3));
                    if((LY >= PosY) && (LY < (PosY+ysize))) {
                        spritecount++;
                        byte spritePriorityBit = GameBoyCPU.getBit(7,attributes);
                        byte yFlipBit = GameBoyCPU.getBit(6,attributes);
                        byte xFlipBit = GameBoyCPU.getBit(5,attributes);
                        int line = LY - PosY;
                        if(yFlipBit == 1) {
                            line = ysize - line - 1;
                        }
                        if(use8x16) {
                            tileID = GameBoyCPU.resetBit(0,tileID);
                        }
                        ushort bankOffSet = 0;
                        if(GameBoyCartiridge.IsGameBoyColor && GameBoyCPU.getBit(3,attributes) == 1) {
                            bankOffSet = 1;
                        }
                        ushort tileLocation = (ushort)((ushort)(tileID * 16) + (ushort)(line*2) + (bankOffSet * 0x2000));
                        // Read two bytes of data. These bytes determine the color of the pixel
                        byte data1 = (byte)(VRAM[tileLocation]);
                        byte data2 = (byte)(VRAM[tileLocation + 1]);
                        for (int tilePixel = 7; tilePixel >= 0; tilePixel--) {
                            int colorBit = tilePixel;
                            if (xFlipBit == 1) {
                                colorBit -= 7;
                                colorBit *= -1;
                            }
                            byte bitFromData1 = GameBoyCPU.getBit((byte)(colorBit),data1);
                            byte bitFromData2 = GameBoyCPU.getBit((byte)(colorBit),data2);
                            byte colorNum = 0;
                            if(bitFromData1 == 1) {
                                colorNum = GameBoyCPU.setBit(0,colorNum);
                            }
                            if(bitFromData2 == 1) {
                                colorNum = GameBoyCPU.setBit(1,colorNum);
                            }
                            // If the pixel is 0 before template is applied, ignore it. White pixels(0) are transparent.
                            if(colorNum == 0) {
                                continue;
                            }
                            byte cgbPalette = (byte)(attributes & 0x7);
                            int index =  (cgbPalette << 2);
                            index = (int)(index | colorNum);
                            index = index & 0x1F;
                            index = index << 1;
                            int colorData = cgbSPRPalette[index]|((cgbSPRPalette[index|1]) << 8); // Combine two bytes into one int
                            colorData &= 0x7FFF; // Mask out the unneeded bits after bit 14
                            Color c = getRGBColors(colorData);
                            int xPix = 0 - tilePixel;
                            xPix += 7 ;
                            byte pixel = (byte)(PosX+xPix);
                            if ((LY<0)||(LY>143)||(pixel<0)||(pixel>159)) {
                                continue;
                            }

                            byte LCDCBit = GameBoyCPU.getBit(0,memory.ReadFromMemory(LCDCAddr));
                            if(bgWinPriority[pixel] == 1 && LCDCBit == 1) {
                                if(bgWinColorResult[pixel] != 0) {
                                    continue;
                                }
                            }

                            if(spritePriorityBit == 1) {
                                if(bgWinColorResult[pixel] != 0) {
                                    continue;
                                }
                            }
                            
                            videoMemory[LY][pixel]=c;
                        }
                    }
                }      
            } else {
                for(int i = 0; i < 40 && spritecount < 10; i++) {
                    int PosY = (int)(memory.ReadFromMemory((ushort)(OAMStartAdress + (i * 4))) - 16);
                    byte PosX = (byte)(memory.ReadFromMemory((ushort)(OAMStartAdress + (i * 4) + 1)) - 8);
                    byte tileID = memory.ReadFromMemory((ushort)(OAMStartAdress + (i * 4) + 2));
                    byte attributes = memory.ReadFromMemory((ushort)(OAMStartAdress + (i * 4) + 3));
                    if((LY >= PosY) && (LY < (PosY+ysize))) {
                        spritecount++;
                        byte spritePriorityBit = GameBoyCPU.getBit(7,attributes);
                        byte yFlipBit = GameBoyCPU.getBit(6,attributes);
                        byte xFlipBit = GameBoyCPU.getBit(5,attributes);
                        byte paletteNumberBit = GameBoyCPU.getBit(4,attributes);
                        int line = LY - PosY;
                        if(yFlipBit == 1) {
                            line = ysize - line - 1;
                        }
                        if(use8x16) {
                            tileID = GameBoyCPU.resetBit(0,tileID);
                        }
                        ushort bank = 0;
                        ushort tileLocation = (ushort)((ushort)(tileID * 16) + (ushort)(line*2) + (bank * 0x2000));
                        // Read two bytes of data. These bytes determine the color of the pixel
                        byte data1 = (byte)(VRAM[tileLocation]);
                        byte data2 = (byte)(VRAM[tileLocation + 1]);
                        
                        for (int tilePixel = 7; tilePixel >= 0; tilePixel--) {
                            int colorBit = tilePixel;
                            if (xFlipBit == 1) {
                                colorBit -= 7;
                                colorBit *= -1;
                            }
                            byte bitFromData1 = GameBoyCPU.getBit((byte)(colorBit),data1);
                            byte bitFromData2 = GameBoyCPU.getBit((byte)(colorBit),data2);
                            byte colorNum = 0;
                            if(bitFromData1 == 1) {
                                colorNum = GameBoyCPU.setBit(0,colorNum);
                            }
                            if(bitFromData2 == 1) {
                                colorNum = GameBoyCPU.setBit(1,colorNum);
                            }
                            // If the pixel is 0 before template is applied, ignore it. White pixels(0) are transparent.
                            if(colorNum == 0) {
                                continue;
                            }
                            int pall = (paletteNumberBit == 1) ? 0xFF49 : 0xFF48;
                            // Go through color template
                            byte colorTemplate = (byte)memory.ReadFromMemory((ushort)(pall));
                            var getColorResult = getColor(colorNum,colorTemplate);
                            Color c = getColorResult.Item1;
                            byte colorResult = getColorResult.Item2;
                            int xPix = 0 - tilePixel;
                            xPix += 7 ;
                            byte pixel = (byte)(PosX+xPix);
                            if ((LY<0)||(LY>143)||(pixel<0)||(pixel>159)) {
                                continue;
                            }

                            if(spritePriorityBit == 1) {
                                if(bgWinPriority[pixel] != 0) {
                                    continue;
                                }
                            }
                            
                            if(set[pixel] == -1 || set[pixel] > PosX) {
                                set[pixel] = PosX;
                                videoMemory[LY][pixel]=c;
                            }
                        }  
                    }
                }
            }
        }
    }

    private Color getRGBColors(int colorData) {
        byte red = (byte)(colorData & 0x1F);
        red = (byte)((red * 255) / 31);
	    byte green = (byte)((colorData >> 5) & 0x1F);
        green = (byte)((green * 255) / 31);
	    byte blue = (byte)((colorData >> 10) & 0x1F);
        blue = (byte)((blue * 255) / 31);
        Color resultColor = new Color(red/255.0f, green/255.0f, blue/255.0f,1);
        return resultColor;
    }

    private (Color,byte) getColor(byte colorNum, byte colorTemplate) {
        Color resultColor = new Color();
        byte result = 0;
        switch(colorNum) {
            case 0:
                result = (byte)((colorTemplate & 0x3));
                break;
            case 1:
                result = (byte)((colorTemplate & 0xC) >> 2);
                break;
            case 2:
                result = (byte)((colorTemplate & 0x30) >> 4);
                break;
            case 3:
                result = (byte)((colorTemplate & 0xC0) >> 6);
                break;
        }

        //https://www.designpieces.com/palette/game-boy-original-color-palette-hex-and-rgb/
        switch(result) {
            case 0:
                resultColor = new Color(155/255.0f, 188/255.0f, 15/255.0f,1);
                break;
            case 1:
                resultColor = new Color(139/255.0f, 172/255.0f, 15/255.0f,1);
                break;
            case 2:
                resultColor = new Color(48/255.0f, 98/255.0f, 48/255.0f,1);
                break;
            case 3:
                resultColor = new Color(15/255.0f, 56/255.0f, 15/255.0f,1);
                break;
        }

        return (resultColor,result);
    }

    private void LYCInterrupt() {
        byte LYValue = memory.ReadFromMemory(LYAddr);
        byte LYCValue = memory.ReadFromMemory(LYCAddr);
        if(LYValue == LYCValue) {
            memory.WriteDirectly(STATAddr, (byte)(memory.ReadFromMemory(STATAddr) | 0x04));
        } else {
            memory.WriteDirectly(STATAddr, (byte)(memory.ReadFromMemory(STATAddr) & 0xFB));
	    }
        //Check if LYC interrupt is enable and requesting.
        if ((byte)(memory.ReadFromMemory(STATAddr) & 0x60) != 0 && ((byte)(memory.ReadFromMemory(STATAddr) & 0x04) != 0)) {
			interrupts.RequestInterrupt(GameBoyInterrupts.LDDCBit);
        }
    }

    private void showStaticScreen(int x, int y) {
        Color pixelColour;
        int n = Random.Range(0,2); //50/50 chance it will be 0 or 1
        if (n == 0) {
            pixelColour = new Color(0, 0, 0, 1); //Black
        }
        else {
            pixelColour = new Color(1, 1, 1, 1); //White
        }
        screen.SetPixel(y, x, pixelColour);
    }

    public void DrawScreen() {
        int row = (int)(height-1);
        for (int x = 0; x < screen.height; x++) {
            int col = 0;
            for (int y = 0; y < screen.width; y++) {
                screen.SetPixel(y,x,videoMemory[row][col++]);
            }
            row--;
        }
        screen.Apply();
    }
    
    public string PrintScreenMemory() {
        string result = "";
        for(int i = 0; i < videoMemory.Count; i++) {
            for(int j = 0; j < videoMemory[i].Count; j++) {
                result = result + videoMemory[i][j] + " ";
            }
            result = result + "\n";
        }
        return result.Substring(0,result.Length-1);
    }

    public void resetWindowLine() {
        windowLine = 0;
    }

    public byte Read(ushort address) {
        byte returnData = 0;
        if (address >= 0x8000 && address <= 0x9FFF) {
		    return VRAM[(address & 0x1FFF) | (VRAMBank << 13)];
	    } // VRAM Bank
        else if (address == 0xFF4F) {
            return VRAMBank;
        }
        // CGB HDMA
        else if (address >= 0xFF51 && address <= 0xFF55) {
            switch (address) {
                case 0xFF51:
                    returnData = (byte)(HDMAsrc >> 8);
                    break;
                case 0xFF52:
                    returnData = (byte)(HDMAsrc & 0xFF);
                    break;
                case 0xFF53:
                    returnData = (byte)(HDMAdst >> 8);
                    break;
                case 0xFF54:
                    returnData = (byte)(HDMAdst & 0xFF);
                    break;
                case 0xFF55:
                    returnData = (byte)((HDMAlen & 0x7F) | (HDMAActive ? 0 : 1) << 7);
                    break;
            }
        }
        // CGB Palette Data
        else if (address >= 0xFF68 && address <= 0xFF6B) {
            switch (address) {
                case 0xFF68:
                    returnData = cgbBGPaletteIndex;
                    break;
                case 0xFF69:
                    returnData = cgbBGPalette[cgbBGPaletteIndex & 0x3F];
                    break;
                case 0xFF6A:
                    returnData = cgbSPRPaletteIndex;
                    break;
                case 0xFF6B:
                    returnData = cgbSPRPalette[cgbSPRPaletteIndex & 0x3F];
                    break;
            }
        }
        return returnData;
    }

    public void Write(ushort address, byte data) {
        //Debug.Log("WRITE VRAM " + address.ToString("X2") + " " + data.ToString("X2"));
        if (address >= 0x8000 && address <= 0x9FFF) {
		    VRAM[(address & 0x1FFF)|(VRAMBank << 13)] = data;
	    } // VRAM Bank
        else if (address == 0xFF4F) {
            VRAMBank = (byte)(data & 0x1);
        }
        // CGB HDMA
        else if (address >= 0xFF51 && address <= 0xFF55) {
            switch (address) {
            case 0xFF51:
                HDMAsrc = (ushort)((HDMAsrc & 0xFF) | (data << 8));
                break;
            case 0xFF52:
                HDMAsrc = (ushort)((HDMAsrc & 0xFF00) | (data & 0xF0));
                break;
            case 0xFF53:
                HDMAdst = (ushort)((HDMAdst & 0xFF) | (((data & 0x1F)| 0x80) << 8));
                break;
            case 0xFF54:
                HDMAdst = (ushort)((HDMAdst & 0xFF00) | (data & 0xF0));
                break;
            case 0xFF55:
                // START DMA
                HDMAlen = (byte)(data & 0x7F);
                if ((data & 0x80) != 0x80 && !HDMAActive) {
                    // Instant DMA
                    for (int i = 0; i <= (HDMAlen & 0x7F); i++) {
                        HDMA();
                    }
                    HDMAlen = 0xFF;
                    HDMAActive = false;
                }
                else if ((data & 0x80) != 0x80 && HDMAActive) {
                    HDMAActive = false;
                }
                else {
                    HDMAActive = true;
                }
                break;
            }
        }
        // CGB Palette Data
        else if (address >= 0xFF68 && address <= 0xFF6B) {
            switch (address) {
                case 0xFF68:
                    cgbBGPaletteIndex = data;
                    break;
                case 0xFF69:
                    cgbBGPalette[cgbBGPaletteIndex & 0x3F] = data;
                    if ((cgbBGPaletteIndex & 0x80) == 0x80) {
                        cgbBGPaletteIndex = (byte)((cgbBGPaletteIndex + 1) & 0xBF);
                    }
                    break;
                case 0xFF6A:
                    cgbSPRPaletteIndex = data;
                    break;
                case 0xFF6B:
                    cgbSPRPalette[cgbSPRPaletteIndex & 0x3F] = data;
                    if ((cgbSPRPaletteIndex & 0x80) == 0x80) {
                        cgbSPRPaletteIndex = (byte)((cgbSPRPaletteIndex + 1) & 0xBF);
                    }
                    break;
            }
        }
    }

    public void HDMA() {
	    // CGB HDMA transfer
        for (int j = 0; j < 0x10; j++) {
            if (HDMAsrc < 0xC000) {
                byte data = memory.ReadFromMemory(HDMAsrc);
                Write(HDMAdst, data);
            }
            else {
                byte data = memory.ReadFromMemory(HDMAsrc);
                Write(HDMAdst, data);
            }
            HDMAdst++;
            HDMAsrc++;
        }
    }
}
