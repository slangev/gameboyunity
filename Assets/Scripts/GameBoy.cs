using UnityEngine;

public class GameBoy : MonoBehaviour
{
    GameBoyScreen gbScreen;
    GameBoyMemory gbMemory;
    GameBoyTimer gbTimer;
    GameBoyInterrupts gbInterrupts;
    GameBoyCartiridge gbCart;
    GameBoyCPU gbCPU;
    Texture2D texture;
    GameObject GoGbScreen;
    ushort width = 160;
    ushort height = 144;
    const uint MAXCYCLES = 69905;
    public string pathToRom;
    public string pathToBios;


    bool halt = false; 
    uint instructCount = 0;
 
    void InitalizeComponent() {
        Application.targetFrameRate = 60;
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

        //Create Interrupts
        gbInterrupts = new GameBoyInterrupts(gbMemory);

        //Create Timer
        gbTimer = new GameBoyTimer(gbMemory, gbInterrupts);

        //Create CPU
        gbCPU = new GameBoyCPU(gbMemory,gbInterrupts);
    }
        


    private void UpdateGraphics(uint cycle) {
        
    }

    void Start() {
        InitalizeComponent();
    }

    void Update() {
        uint cyclesThisUpdate = 0 ; 
        while (cyclesThisUpdate < MAXCYCLES) {
            uint cycles = gbCPU.Tick();
            cyclesThisUpdate+=cycles ;
            gbTimer.UpdateTimers(cycles);
            //UpdateGraphics(cycles) ;
        }
        GameBoyCPU.ClockCycle = 0;
    }
}
