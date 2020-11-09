using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameBoy : MonoBehaviour
{
    // Start is called before the first frame update
    GameBoyScreen gbScreen;
    GameBoyMemory gbMemory;
    GameBoyCartiridge gbCart;
    GameBoyCPU gbCPU;
    Texture2D texture;
    GameObject GoGbScreen;
    ushort width = 160;
    ushort height = 144;
    const uint MAXCYCLES = 69905;
    public string pathToRom;
    public string pathToBios;

    void InitalizeComponent() {
        Application.targetFrameRate = 60;
        //Time.fixedDeltaTime = 0.01666667f;
        //Create display
        texture = new Texture2D(width,height);
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, Screen.width, Screen.height), Vector2.zero,1);
        sprite.name = "Screen";
        GoGbScreen = GameObject.Find("GameBoyScreen");
        GoGbScreen.GetComponent<SpriteRenderer>().sprite = sprite;
        Vector2 pivotPoints = new Vector2(0.0f,0.0f);
        GoGbScreen.GetComponent<RectTransform>().pivot = pivotPoints;
        gbScreen = new GameBoyScreen(width,height,texture);
        gbScreen.DrawScreen();

        //Load Cartiridge
        gbCart = new GameBoyCartiridge(0);
        gbCart.LoadRom(pathToRom);
        Debug.Log(gbCart.Title);
        Debug.Log("CartiridgeType: " + gbCart.CartiridgeType);

        //Create memory
        gbMemory = new GameBoyMemory(gbCart);
        gbMemory.LoadBios(pathToBios);

        //Create CPU
        gbCPU = new GameBoyCPU(gbMemory);
    }
        
    
    void Start() {
        InitalizeComponent();
    }

    void Update() {
        while (GameBoyCPU.ClockCycle < MAXCYCLES) {
            gbCPU.Tick();
            //UpdateTimers(cycles) ;
            //UpdateGraphics(cycles) ;
            //DoInterupts( ) ;
        }
       GameBoyCPU.ClockCycle = 0;
        //Debug.Log("Done : " + cyclesThisUpdate);
    }
}
