using UnityEngine;
using UnityEngine.UI;
public class GameBoyJoyPad {
    private GameBoyInterrupts interrupts;
    private GameBoyMemory memory;
    private GameObject UIPanel;
    public GameBoyJoyPad(GameBoyInterrupts interrupts, GameBoyMemory memory) {
        this.interrupts = interrupts;
        this.memory = memory;
        GameObject[] onlyInactive = GameObject.FindObjectsOfType<GameObject>(true);
        foreach(GameObject g in onlyInactive){
            if(g.name == "UI"){
                 UIPanel = g;
            }
        }
    }

    //https://datacrystal.romhacking.net/wiki/Pok%C3%A9mon_Red/Blue:RAM_map
    //https://bulbapedia.bulbagarden.net/wiki/List_of_Pok%C3%A9mon_by_index_number_(Generation_I)
    //https://bulbapedia.bulbagarden.net/wiki/List_of_items_by_index_number_(Generation_I)
    public void Submit() {
        var Texts = UIPanel.GetComponentsInChildren<Text>();
        string addrStr = Texts[0].text;
        string valueStr = Texts[1].text;
        ushort addr = ushort.Parse(addrStr,System.Globalization.NumberStyles.HexNumber);
        byte value = byte.Parse(valueStr,System.Globalization.NumberStyles.HexNumber);
        Debug.Log(addr);
        Debug.Log(value);
        memory.WriteToMemory(addr,value);
    }

    public void HandleKeyEvents() {
        bool highToLow = false; // High to low mean we went from 1(released) to 0(pressed)

        // Buttons down (b,a,select,start) 0 = pressed 1 = released
        if(Input.GetKeyDown(KeyCode.S)) {
            highToLow = memory.ResetJoyPadBit(5);
        }
        if(Input.GetKeyDown(KeyCode.A)) {
            highToLow = memory.ResetJoyPadBit(4);
        }
        if(Input.GetKeyDown(KeyCode.Space)) {
            highToLow = memory.ResetJoyPadBit(6);
        }
        if(Input.GetKeyDown(KeyCode.Return)) {
            highToLow = memory.ResetJoyPadBit(7);
        }

        if(Input.GetKeyDown(KeyCode.Tab)) {
            if(UIPanel.activeSelf) {
                UIPanel.SetActive(false);
            } else {
                UIPanel.SetActive(true);
            }
        }

        // Buttons up (b,a,select,start)
        if(Input.GetKeyUp(KeyCode.S)) {
            memory.SetJoyPadBit(5);
        }
        if(Input.GetKeyUp(KeyCode.A)) {
            memory.SetJoyPadBit(4);
        }
        if(Input.GetKeyUp(KeyCode.Space)) {
            memory.SetJoyPadBit(6);
        }
        if(Input.GetKeyUp(KeyCode.Return)) {
            memory.SetJoyPadBit(7);
        }

        // Directional down (b,a,select,start)
        if(Input.GetKeyDown(KeyCode.UpArrow)) {
            highToLow = memory.ResetJoyPadBit(2);
        }
        if(Input.GetKeyDown(KeyCode.DownArrow)) {
            highToLow = memory.ResetJoyPadBit(3);
        }
        if(Input.GetKeyDown(KeyCode.LeftArrow)) {
            highToLow = memory.ResetJoyPadBit(1);
        }
        if(Input.GetKeyDown(KeyCode.RightArrow)) {
            highToLow = memory.ResetJoyPadBit(0);
        }

        // Directional up (b,a,select,start)
        if(Input.GetKeyUp(KeyCode.UpArrow)) {
            memory.SetJoyPadBit(2);
        }
        if(Input.GetKeyUp(KeyCode.DownArrow)) {
            memory.SetJoyPadBit(3);
        }
        if(Input.GetKeyUp(KeyCode.LeftArrow)) {
            memory.SetJoyPadBit(1);
        }
        if(Input.GetKeyUp(KeyCode.RightArrow)) {
            memory.SetJoyPadBit(0);
        }

        if(highToLow) {
            interrupts.RequestInterrupt(4); // joypad interrupts
        }
    }
}
