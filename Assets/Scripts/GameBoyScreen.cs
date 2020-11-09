using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameBoyScreen
{
    Texture2D screen;
    uint width;
    uint height;
    public GameBoyScreen(uint width, uint height, Texture2D screen) {
        this.width = width;
        this.height = height;
        this.screen = screen;
    }

    public void DrawScreen() {
        for(int i = 0; i < screen.height; i++) {
            for(int j = 0; j < screen.width; j++) {
                Color c = new Color(1, 1, 1, 1); //White
                screen.SetPixel(j,i,c);
            }
        }
        screen.Apply();
    }
}
