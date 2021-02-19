using UnityEngine;
using System;

public class GameBoy : MonoBehaviour
{
    GameBoyGraphic gbGraphic;
    GameBoyAudio gbAudio;
    GameBoyMemory gbMemory;
    GameBoyTimer gbTimer;
    GameBoyInterrupts gbInterrupts;
    GameBoyCartiridge gbCart;
    GameBoyCPU gbCPU;
    GameBoyJoyPad gbJoyPad;
    Texture2D texture;
    GameObject GogbGraphic;
    ushort width = 160;
    ushort height = 144;
    const uint MAXCYCLES = 69905;
    public string pathToRom;
    public string pathToBios;
 
    void InitalizeComponent() {
        Application.targetFrameRate = 60;

        //Create display
        texture = new Texture2D(width,height);
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, Screen.width, Screen.height), Vector2.zero,1);
        sprite.name = "Screen";
        GogbGraphic = GameObject.Find("GameBoyScreen");
        GogbGraphic.GetComponent<SpriteRenderer>().sprite = sprite;
        Vector2 pivotPoints = new Vector2(0.0f,0.0f);
        GogbGraphic.GetComponent<RectTransform>().pivot = pivotPoints;
        

        //Load Cartiridge
        gbCart = new GameBoyCartiridge(0);
        try {
            gbCart.LoadRom(pathToRom);
        } catch {
            Debug.Log("NO ROM LOADED");
        }
        

        //Create memory
        gbMemory = new GameBoyMemory(gbCart);
        bool reset = false;
        try {
            gbMemory.LoadBios(pathToBios);
        } catch {
            reset = true;
        }

        //Create Interrupts
        gbInterrupts = new GameBoyInterrupts(gbMemory);

        //Create Timer
        gbTimer = new GameBoyTimer(gbMemory, gbInterrupts);
        gbMemory.AddTimer(gbTimer);

        //Create CPU
        gbCPU = new GameBoyCPU(gbMemory,gbInterrupts);
        if(reset && !GameBoyCartiridge.IsGameBoyColor) {
            gbCPU.ResetGBNoBios();
        } else if(reset && GameBoyCartiridge.IsGameBoyColor) {
            gbCPU.ResetCGBNoBios();
        }

        //Create GPU
        gbGraphic = new GameBoyGraphic(width, height, texture, gbInterrupts, gbMemory);
        //Create Audio
        gbAudio = new GameBoyAudio();
        gbMemory.AddGraphics(gbGraphic);
        gbMemory.AddAudio(gbAudio);

        //Create Keyboard
        gbJoyPad = new GameBoyJoyPad(gbInterrupts,gbMemory);
        Debug.Log(GameBoyCartiridge.Title);
        Debug.Log(GameBoyCartiridge.IsGameBoyColor);
        Debug.Log(gbCart.CartiridgeType.ToString("X2"));
    }

    void Start() {
        InitalizeComponent();
    }

    void Update() {
        uint cyclesThisUpdate = 0 ; 
        while (cyclesThisUpdate < MAXCYCLES * gbMemory.GetSpeed()) {
            gbJoyPad.HandleKeyEvents();
            uint cycles = gbCPU.Tick();
            cyclesThisUpdate+=cycles ;
            gbTimer.UpdateTimers(cycles);
            gbGraphic.UpdateGraphics(cycles);
            gbAudio.UpdateAudioTimer(cycles);
        }
        gbGraphic.DrawScreen();
    }
}
