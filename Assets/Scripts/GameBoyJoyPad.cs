using UnityEngine;
public class GameBoyJoyPad {
    private GameBoyInterrupts interrupts;
    private GameBoyMemory memory;

    public GameBoyJoyPad(GameBoyInterrupts interrupts, GameBoyMemory memory) {
        this.interrupts = interrupts;
        this.memory = memory;
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
            interrupts.RequestInterrupt(4); // joypad interrupts
        }
        if(Input.GetKeyDown(KeyCode.DownArrow)) {
            highToLow = memory.ResetJoyPadBit(3);
            interrupts.RequestInterrupt(4); // joypad interrupts
        }
        if(Input.GetKeyDown(KeyCode.LeftArrow)) {
            highToLow = memory.ResetJoyPadBit(1);
            interrupts.RequestInterrupt(4); // joypad interrupts
        }
        if(Input.GetKeyDown(KeyCode.RightArrow)) {
            highToLow = memory.ResetJoyPadBit(0);
            interrupts.RequestInterrupt(4); // joypad interrupts
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
