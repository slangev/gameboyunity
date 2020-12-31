using UnityEngine;
using System.Collections.Generic;

public class GameBoyGraphic
{
    Texture2D screen;
    uint width;
    uint height;
    private GameBoyInterrupts interrupts;
    private GameBoyMemory memory;
    private uint GPUCycleCount;

    // Frame Buffer
    public List<List<Color>> videoMemory;

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
            byte windowX = (byte)(memory.ReadFromMemory(WXAddr) - 7);
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
                    tileNum = (byte)memory.ReadFromMemory(tileAddress);
                else
                    tileNum = (sbyte)memory.ReadFromMemory(tileAddress) ;
                ushort tileLocation = tileData;
                if(!signed)
				    tileLocation += (ushort) (tileNum * 16);
			    else
				    tileLocation += (ushort) ((tileNum+128) * 16);

                byte line = (byte)(yPos % 8);
                line *= 2;
                // Read two bytes of data. These bytes determine the color of the pixel
                byte data1 = (byte)memory.ReadFromMemory((ushort)(tileLocation + line));
                byte data2 = (byte)memory.ReadFromMemory((ushort)(tileLocation + line + 1));

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
                videoMemory[LY][pixel]=c;
            }
        }
    }

    private void renderBackground() {
        bool backgroundEnabled = ((memory.ReadFromMemory(LCDCAddr) & 0x1) != 0) ? true : false;
        bool backgroundTileSelect = ((memory.ReadFromMemory(LCDCAddr) & 0x8) != 0) ? true : false;
        if(backgroundEnabled) {
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
            short tileRow = (short)((yPos/8) * 32); // There are 32 rows and each row has 32 tiles. Each tile is 8 pixel tall. 

            for(int pixel = 0; pixel < 160; pixel++) {
                byte xPos = (byte)(pixel+scrollX); // xPos equals the current pixel we are working on. It's part of the calculation for tileCol. Same idea as tileRow.
                short tileCol = (short)(xPos/8);
                short tileNum;
                ushort tileAddress = (ushort)(backgroundMemory + tileRow + tileCol);
                if(!signed)
                    tileNum = (byte)memory.ReadFromMemory(tileAddress);
                else
                    tileNum = (sbyte)memory.ReadFromMemory(tileAddress) ;
                ushort tileLocation = tileData;
                if(!signed)
				    tileLocation += (ushort) (tileNum * 16);
			    else
				    tileLocation += (ushort) ((tileNum+128) * 16);

                byte line = (byte)(yPos % 8);
                line *= 2;
                // Read two bytes of data. These bytes determine the color of the pixel
                byte data1 = (byte)memory.ReadFromMemory((ushort)(tileLocation + line));
                byte data2 = (byte)memory.ReadFromMemory((ushort)(tileLocation + line + 1));

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
                videoMemory[LY][pixel]=c;
            }
        }
    }

    private void renderSprites() {
        bool spritesEnabled = ((memory.ReadFromMemory(LCDCAddr) & 0x2) != 0) ? true : false;
        bool use8x16 = ((memory.ReadFromMemory(LCDCAddr) & 0x4) != 0) ? true : false;
        int ysize = (use8x16) ? 16 : 8;
        if(spritesEnabled) {
            List<int> set = new List<int>(new int[160]);
            for(int i = 0; i < 160; i++) {
                set[i] = -1;
            }
            byte LY = memory.ReadFromMemory(LYAddr);
            int spritecount = 0;
            for(int i = 0; i < 40 && spritecount < 10; i++) {
                byte PosY = (byte)(memory.ReadFromMemory((ushort)(OAMStartAdress + (i * 4))) - 16);
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
                        line -= ysize - 1;
                        line *= -1;
                    }
                    line *=2;
                    if(use8x16) {
                        tileID = GameBoyCPU.resetBit(0,tileID);
                    }
                    ushort tileLocation = (ushort)(0x8000 + tileID * 16);
                    // Read two bytes of data. These bytes determine the color of the pixel
                    byte data1 = (byte)memory.ReadFromMemory((ushort)(tileLocation + line));
                    byte data2 = (byte)memory.ReadFromMemory((ushort)(tileLocation + line + 1));
                    
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
					    int pixel = PosX+xPix;
                        if ((LY<0)||(LY>143)||(pixel<0)||(pixel>159)) {
                            continue ;
                        }

                        if(spritePriorityBit == 1) {
                            if(colorResult == 0) {
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
            memory.WriteToMemory(STATAddr, (byte)(memory.ReadFromMemory(STATAddr) | 0x04));
        } else {
            memory.WriteToMemory(STATAddr, (byte)(memory.ReadFromMemory(STATAddr) & 0xFB));
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
}
