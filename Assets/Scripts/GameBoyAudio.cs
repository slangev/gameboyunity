﻿using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;


//https://stackoverflow.com/questions/376036/algorithm-to-mix-sound
//https://answers.unity.com/questions/1417541/is-it-possible-to-create-sound-with-scripting.html
public class GameBoyAudio {
    internal class SquareChannel {

        // Duty Cycle table
        public readonly bool[][] dutyTable = new bool[][] {
            new bool[] {false, false, false, false, false, false, false, true},
            new bool[] {true, false, false, false, false, false, false, true},
            new bool[] {true, false, false, false, false, true, true, true},
            new bool[] {false, true, true, true, true, true, true, false}
        };

        uint outputVol = 0;
        uint sequencePointer = 0;
        byte duty = 0;
        int timer = 0;	// AKA frequency
        ushort timerLoad = 0;	// Reloads the timer
        bool lengthEnable = false;
        byte volumeLoad = 0;
        byte volume = 0;
        byte lengthLoad = 0;
        byte lengthCounter;
        bool envelopeAddMode = false;
        byte envelopePeriodLoad = 0;
        int envelopePeriod = 0;
        // Sweep
        byte sweepPeriodLoad = 0;
        int sweepPeriod = 0;
        bool sweepNegate = false;
        byte sweepShift = 0;
        ushort sweepShadow = 0;
        bool sweepEnable = false;
        // Keeps track of last bit written to trigger
        bool triggerBit = false;
        bool enabled = false;
        bool dacEnabled = true;	// Seperate from the other enabled, controlled by high 5 bits of nr2
        bool envelopeRunning = true;	// 

        public SquareChannel () {

        }

        public bool getRunning() {
            return lengthCounter > 0 && enabled;
        }

        public void step() {
            if (--timer <= 0) {
                //timer = timerLoad;
                timer = (2048 - timerLoad) * 4;	// ???
                sequencePointer = (sequencePointer + 1) & 0x7;
            }

            if (enabled && dacEnabled) {
                outputVol = volume;
            }
            else {
                outputVol = 0;
            }

            if (!dutyTable[duty][sequencePointer]) {
                outputVol = 0;
            }
        }

        public void lengthClck() {
            if (lengthCounter > 0 && lengthEnable) {
                lengthCounter--;
                if (lengthCounter == 0) {
                    enabled = false;	// Disable channel
                }
            }
        }

        public void envClck() {
            // Envelope tick when it's zero
            if(--envelopePeriod <= 0){
                // Reload period
                // does this loop or?
                envelopePeriod = envelopePeriodLoad;
                if (envelopePeriod == 0) {
                    envelopePeriod = 8;		// Some obscure behavior, But I don't understand this behavior at all..
                }
                // Should envelopePeriod > 0 be here?
                if (envelopeRunning && envelopePeriodLoad > 0) {
                    if (envelopeAddMode && volume < 15) {
                        volume++;
                    }
                    else if (!envelopeAddMode && volume > 0) {
                        volume--;
                    }
                }
                if (volume == 0 || volume == 15) {
                    envelopeRunning = false;
                }
            }
        }

        public void sweepClck() {
            if (--sweepPeriod <= 0) {
                sweepPeriod = sweepPeriodLoad;
                if (sweepPeriod == 0) {
                    sweepPeriod = 8;
                }
                if (sweepEnable && sweepPeriodLoad > 0) {
                    ushort newFreq = sweepCalculation();
                    if (newFreq <= 2047 && sweepShift > 0) {
                        sweepShadow = newFreq;
                        timerLoad = newFreq;
                        sweepCalculation();
                    }
                    sweepCalculation();
                }
            }
        }

        private ushort sweepCalculation() {
            ushort newFreq = 0;
            newFreq = (ushort)(sweepShadow >> sweepShift);
            if (sweepNegate) {
                newFreq = (ushort)(sweepShadow - newFreq);
            }
            else {
                newFreq = (ushort)(sweepShadow + newFreq);
            }
            // Should I assume that there's some sort of underflow with this frequency calculation/overflow check? 
            // It'd disable the channel in the event of an underflow at least. That might be intended
            if (newFreq > 2047) {
                enabled = false;
            }

            return newFreq;
        }

        private void trigger() {
            if(dacEnabled){
                enabled = true;
            }
            if (lengthCounter == 0) {
                lengthCounter = 64;	// It's a little large
            }
            timer = (2048 - timerLoad) * 4;
            envelopeRunning = true;
            envelopePeriod = envelopePeriodLoad;
            volume = volumeLoad;
            // Sweep trigger stuff
            sweepShadow = timerLoad;
            sweepPeriod = sweepPeriodLoad;
            if (sweepPeriod == 0) {
                sweepPeriod = 8;
            }
            sweepEnable = sweepPeriod > 0 || sweepShift > 0;
            // If the sweep shift is non-zero, frequency calculation and the overflow check are performed immediately.
            if (sweepShift > 0) {
                // Overflow check?
                sweepCalculation();
            }
        }

        public byte getOutputVol() {
            return (byte)(outputVol); 
        }

        public byte readRegister(ushort address) {
            byte returnData = 0;
	        byte squareRegister = (byte)((address & 0xF) % 0x5);
            switch (squareRegister) {
                // Sweep. Only on Square 1
            case 0x0:
                byte sweepNegateValue = sweepNegate ? (byte) 1 : (byte) 0;
                returnData = (byte)((sweepShift) | (byte)((sweepNegateValue) << 3) | (sweepPeriodLoad << 4));
                break;
                // Duty, Length Load
            case 0x1:
                returnData = (byte)((lengthLoad & 0x3F) | ((duty & 0x3) << 6));
                break;
                // Envelope
            case 0x2:
                byte envelopeAddModeValue = envelopeAddMode ? (byte) 1 : (byte) 0;
                returnData = (byte)((envelopePeriodLoad & 0x7) | (envelopeAddModeValue << 3) | ((volumeLoad & 0xF) << 4));
                break;
            case 0x3:
                returnData = (byte)(timerLoad & 0xFF);
                break;
                // Trigger, length enable, frequency MSB
            case 0x4:
                byte lengthEnableValue = lengthEnable ? (byte) 1 : (byte) 0;
                byte triggerBitValue = triggerBit ? (byte) 1 : (byte) 0;

                returnData = (byte)(((timerLoad >> 8) & 0x7) | (lengthEnableValue << 6) | (triggerBitValue << 7));	// Trigger bit probably?
                // Trigger is on 0x80, bit 7.
                break;
            }

            return returnData;
        }

        public void writeRegister(ushort address, byte data) {
            byte squareRegister = (byte)((address & 0xF) % 0x5);
            //Debug.Log("HERE: " + squareRegister.ToString("X2") + " DATA: " + data.ToString("X2"));
	        switch (squareRegister) {
		        // Sweep. Only on Square 1
		        case 0x0:
                    sweepShift = (byte)(data & 0x7);
                    sweepNegate = (data & 0x8) == 0x8;
                    sweepPeriodLoad = (byte)((data >> 4) & 0x7);
                    //sweepPeriod = sweepPeriodLoad;
			        break;
                // Duty, Length Load
                case 0x1:
                    lengthLoad = (byte)(data & 0x3F);
                    lengthCounter = (byte)(64 - (lengthLoad & 0x3F));
                    duty = (byte)((data >> 6) & 0x3);
                    break;
                // Envelope
                case 0x2:
                    // See if dac is enabled, if all high 5 bits are not 0
                    dacEnabled = (data & 0xF8) != 0;
                    if(!dacEnabled) {
                        enabled = false;
                    }
                    // Starting Volume
                    volumeLoad = (byte)((data >> 4) & 0xF);
                    // Add mode
                    envelopeAddMode = (data & 0x8) == 0x8;
                    // Period
                    envelopePeriodLoad = (byte)((data & 0x7));
                    envelopePeriod = envelopePeriodLoad;
                    // TEMP?
                    volume = volumeLoad;
                    break;
                // Frequency LSB
                case 0x3:
                    timerLoad = (ushort)((timerLoad & 0x700) | data);
                    break;
                // Trigger, length enable, frequency MSB
                case 0x4:
                    timerLoad = (ushort)((timerLoad & 0xFF) | ((data & 0x7) << 8));
                    lengthEnable = (data & 0x40) == 0x40;
                    // This should happen LAST, it'll cause issues if it isn't last
                    triggerBit = (data & 0x80) == 0x80;
                    if ((data & 0x80) == 0x80) {
                        trigger();
                    }
                    break;
                }
            }
        }

    internal class WaveChannel {
        // Wave Table ram, 16 entries, 32 samples in total (4 bits per sample).
        byte[] waveTable = new byte[16];
        // Registers
        byte lengthLoad = 0;
        byte volumeCode = 0;
        // Timer aka Frequency
        ushort timerLoad = 0;
        bool lengthEnable = false;
        bool triggerBit = false;
        // Internal
        byte positionCounter = 0;
        ushort lengthCounter = 0;
        int timer = 0;
        byte outputVol = 0;
        bool enabled = false;
        bool dacEnabled = false;
        public WaveChannel () {
            
        }

        public byte readRegister(ushort address) {
            // Eh
            byte returnData = 0;

            byte registerVal = (byte)(address & 0xF);
            if (address >= 0xFF1A && address <= 0xFF1E) {
                switch (registerVal) {
                case 0xA:
                    byte dacEnabledValue = dacEnabled ? (byte) 1 : (byte) 0;
                    returnData = (byte)((dacEnabledValue) << 7);
                    break;
                case 0xB:
                    returnData = lengthLoad;
                    break;
                case 0xC:
                    returnData = (byte)(volumeCode << 5);
                    break;
                case 0xD:
                    returnData = (byte)(timerLoad & 0xFF);
                    break;
                case 0xE:
                    byte lengthEnableValue = lengthEnable ? (byte) 1 : (byte) 0;
                    byte triggerBitValue = triggerBit ? (byte) 1 : (byte) 0;
                    returnData = (byte)(((timerLoad >> 8) & 0x7) | (lengthEnableValue << 6) | (triggerBitValue << 7));	// Trigger bit probably?
                    break;
                }
            }
            // wave ram
            else if (address >= 0xFF30 && address <= 0xFF3F) {
                returnData = waveTable[registerVal];
            }

            return returnData;
        }

        public void writeRegister(ushort address, byte data) {
            byte registerVal = (byte)(address & 0xF);
            if(address >= 0xFF1A && address <= 0xFF1E){
                switch (registerVal) {
                    case 0xA:
                        dacEnabled = (data & 0x80) == 0x80;
                        if(!dacEnabled) {
                            enabled = false;
                        }
                        break;
                    case 0xB:
                        lengthLoad = data;
                        lengthCounter = (ushort)(256 - lengthLoad);
                        break;
                    case 0xC:
                        volumeCode = (byte)((data >> 5) & 0x3);
                        break;
                    case 0xD:
                        timerLoad = (ushort)((timerLoad & 0x700) | data);
                        break;
                    case 0xE:
                        timerLoad = (ushort)((timerLoad & 0xFF) | ((data & 0x7) << 8));
                        lengthEnable = (data & 0x40) == 0x40;
                        triggerBit = (data & 0x80) == 0x80;
                        if (triggerBit) {
                            trigger();	// Trigger event
                        }
                        break;
                }
            }
            // wave ram
            else if (address >= 0xFF30 && address <= 0xFF3F) {
                waveTable[registerVal] = data;
            }
        }

        public void lengthClck() {
            if (lengthCounter > 0 && lengthEnable) {
                lengthCounter--;
                if (lengthCounter == 0) {
                    enabled = false;	// Disable channel
                }
            }
        }

        public byte getOutputVol() {
            return outputVol;
        }

        public bool getRunning() {
            return lengthCounter > 0 && enabled;
        }

        public void trigger() {
            if(dacEnabled) {
                enabled = true;
            }
            if (lengthCounter == 0) {
                lengthCounter = 256;
            }
            timer = (2048 - timerLoad) * 2;
            positionCounter = 0;
        }

        public void step() {
            if (--timer <= 0) {
                timer = (2048 - timerLoad) * 2;
                // Should increment happen before or after?
                positionCounter = (byte)((positionCounter + 1) & 0x1F);
                // Decide output volume
                if (enabled && dacEnabled) {
                    // Decide what byte it should be first
                    int position = positionCounter / 2;
                    byte outputByte = waveTable[position];
                    bool highBit = (positionCounter & 0x1) == 0;
                    if (highBit) {
                        outputByte >>= 4;
                    }
                    outputByte &= 0xF;
                    // Handle volume code. 0 shouldn't occur.
                    if (volumeCode > 0) {
                        outputByte >>= volumeCode - 1;
                    }
                    else {
                        outputByte = 0;
                    }
                    outputVol = outputByte;
                }
                else {
                    outputVol = 0;
                }
            }
        }
    }

    internal class NoiseChannel {
        
        byte lengthLoad = 0;
        byte volumeLoad = 0;
        bool envelopeAddMode = false;
        byte envelopePeriodLoad = 0;
        // Noise specific stuff
        byte clockShift = 0;
        bool widthMode = false;
        byte divisorCode = 0;
        bool triggerBit = false;
        bool lengthEnable = false;

        // Internal
        // Divisor table, for divisor codes
        readonly int[] divisors = new int[] { 8, 16, 32, 48, 64, 80, 96, 112 };
        int timer = 0;
        bool enabled = false;
        bool dacEnabled = false;
        byte lengthCounter = 0;
        byte volume = 0;
        int envelopePeriod = 0;
        ushort lfsr = 0;	// Linear feedback shift register
        bool envelopeRunning = false;
        byte outputVol = 0;
        public NoiseChannel () {
            
        }

        public byte readRegister(ushort address) {
            byte returnData = 0;

            switch (address) {
                case 0xFF1F:
                    // Not used
                    break;
                case 0xFF20:
                    returnData = (byte)(lengthLoad & 0x3F);
                    break;
                case 0xFF21:
                    byte envelopeAddModeValue = envelopeAddMode ? (byte) 1 : (byte) 0;
                    returnData = (byte)((envelopePeriodLoad & 0x7) | (envelopeAddModeValue << 3) | ((volumeLoad & 0xF) << 4));
                    break;
                case 0xFF22:
                    byte widthModeValue = widthMode ? (byte) 1 : (byte) 0;
                    returnData = (byte)((divisorCode) | (widthModeValue << 3) | (clockShift << 4));
                    break;
                case 0xFF23:
                    byte lengthEnableValue = lengthEnable ? (byte) 1 : (byte) 0;
                    byte triggerBitValue = triggerBit ? (byte) 1 : (byte) 0;

                    returnData = (byte)((lengthEnableValue << 6) | (triggerBitValue << 7));	// Trigger bit probably?
                    break;
            }

            return returnData;
        }
        public void writeRegister(ushort address, byte data) {
            switch (address) {
                case 0xFF1F:
                    // Not used
                    break;
                case 0xFF20:
                    lengthLoad = (byte)(data & 0x3F);
                    lengthCounter = (byte)(64 - lengthLoad);
                    break;
                case 0xFF21:
                    // See if dac is enabled, if all high 5 bits are not 0
                    dacEnabled = (data & 0xF8) != 0;
                    if(!dacEnabled) {
                        enabled = false;
                    }
                    // Starting Volume
                    volumeLoad = (byte)((data >> 4) & 0xF);
                    // Add mode
                    envelopeAddMode = (data & 0x8) == 0x8;
                    // Period
                    envelopePeriodLoad = (byte)((data & 0x7));
                    envelopePeriod = envelopePeriodLoad;
                    volume = volumeLoad;
                    break;
                case 0xFF22:
                    divisorCode = (byte)(data & 0x7);
                    widthMode = (data & 0x8) == 0x8;
                    clockShift = (byte)((data >> 4) & 0xF);
                    break;
                case 0xFF23:
                    lengthEnable = (data & 0x40) == 0x040;
                    triggerBit = (data & 0x80) == 0x80;
                    if (triggerBit) {
                        trigger();
                    }
                    break;
	        }
        }

        public void envClck() {
            // Envelope tick when it's zero
            if (--envelopePeriod <= 0) {
                // Reload period
                // does this loop or?
                envelopePeriod = envelopePeriodLoad;
                if (envelopePeriod == 0) {
                    envelopePeriod = 8;		// Some obscure behavior, But I don't understand this behavior at all..
                }
                // Should envelopePeriod > 0 be here?
                if (envelopeRunning && envelopePeriodLoad > 0) {
                    if (envelopeAddMode && volume < 15) {
                        volume++;
                    }
                    else if (!envelopeAddMode && volume > 0) {
                        volume--;
                    }
                }
                if (volume == 0 || volume == 15) {
                    envelopeRunning = false;
                }
            }
        }

        public void lengthClck() {
            if (lengthCounter > 0 && lengthEnable) {
                lengthCounter--;
                if (lengthCounter == 0) {
                    enabled = false;	// Disable channel
                }
            }
        }

        public byte getOutputVol() {
            return outputVol;
        }

        public bool getRunning() {
            return lengthCounter > 0 && enabled;
        }

        public void trigger() {
            if(dacEnabled) {
                enabled = true;
            }
            if (lengthCounter == 0) {
                lengthCounter = 64;
            }
            timer = divisors[divisorCode] << clockShift;
            envelopePeriod = envelopePeriodLoad;
            envelopeRunning = true;
            volume = volumeLoad;
            lfsr = 0x7FFF;
        }

        public void step() {
            if (--timer <= 0) {
                timer = divisors[divisorCode] << clockShift;	// odd

                //It has a 15 - bit shift register with feedback.When clocked by the frequency timer, the low two bits(0 and 1) are XORed, 
                //all bits are shifted right by one, and the result of the XOR is put into the now - empty high bit.If width mode is 1 (NR43), 
                //the XOR result is ALSO put into bit 6 AFTER the shift, resulting in a 7 - bit LFSR.
                //The waveform output is bit 0 of the LFSR, INVERTED.
                byte result = (byte)((lfsr & 0x1) ^ ((lfsr >> 1) & 0x1));
                lfsr >>= 1;
                lfsr |= (ushort)(result << 14);
                if (widthMode) {
                    unchecked{
			            lfsr &= (ushort)(~0x40);
		            }
                    lfsr |= (ushort)(result << 6);
                }
                if (enabled && dacEnabled && (lfsr & 0x1) == 0) {
                    outputVol = volume;
                }
                else {
                    outputVol = 0;
                }
            }
        }
    }

    private static int channels = 2;
    private static int sample = 2048;
    private AudioSource audio;

    // Values OR'd into register reads.
	readonly byte[] readOrValues = new byte[] { 0x80,0x3f,0x00,0xff,0xbf,
										0xff,0x3f,0x00,0xff,0xbf,
										0x7f,0xff,0x9f,0xff,0xbf,
										0xff,0xff,0x00,0x00,0xbf,
										0x00,0x00,0x70 };
	// APU Universal registers
	bool vinLeftEnable = false;
    bool vinRightEnable = false;
	byte leftVol = 0;
	byte rightVol = 0;
	bool[] leftEnables = new bool[]{ false,false,false,false };
	bool[] rightEnables = new bool[]{ false,false,false,false };
	bool powerControl = false;
    int APUBufferCount = 0;
    public float[] mainBuffer = new float[sample*channels];
    public float[] mainVolBuffer = new float[sample*channels];
    //Queue<float> mainBuffer = new();
    private int frameSequenceCountDown = 8192;
    private int frameSequencer = 8;
    private static int AudioSampleRate = 44100;
    static float CPUSpeed = 4194304.0f; //4.19MHz
	int downSampleCount = (int)(CPUSpeed / AudioSampleRate);
    public bool WriteSamples = false;
    int Speed = (int)(CPUSpeed / AudioSampleRate);
    private SquareChannel squareOne;
    private SquareChannel squareTwo;
    private WaveChannel waveChannel;
    private NoiseChannel noiseChannel;
    public int position = 0;
    
    public GameBoyAudio() {
        initializeAPU();
    }
    private void initializeAPU() {
        // GameObject gb = GameObject.Find("GameBoyCamera");
        // gb.AddComponent(typeof(AudioListener));
        // gb.AddComponent(typeof(AudioSource));
        // AudioClip myClip = AudioClip.Create(name: "GameBoyAudio", sample*channels, channels, AudioSampleRate, true, OnAudioRead, OnAudioSetPosition);
        // // AudioClip myClip = AudioClip.Create("GameBoyAudio", sample*channels, channels, AudioSampleRate, false);
        // audio = gb.GetComponent<AudioSource>();
        // audio.clip = myClip;
        // audio.volume = 0.5f;
        // audio.Play();

        squareOne = new SquareChannel();
        squareTwo = new SquareChannel();
        waveChannel = new WaveChannel();
        noiseChannel = new NoiseChannel();
    }

    void OnAudioRead(float[] data)
    {
        // Debug.Log("data: " + data.Length);
        // Debug.Log("mainBuffer: " + mainBuffer.Length);
        if(WriteSamples) {
            for(int i = 0; i < data.Length; i++)
            {
                data[i] = mainBuffer[i];
            }
            WriteSamples = false;
        }

    }

    void OnAudioSetPosition(int newPosition)
    {
        position = newPosition;
    }

    public byte Read(ushort address) {
        byte returnData = 0xFF;
	    ushort apuRegister = (ushort)(address & 0xFF);

	    if (apuRegister >= 0x10 && apuRegister <= 0x14) {
		    returnData = squareOne.readRegister(apuRegister);
	    } else if (apuRegister >= 0x16 && apuRegister <= 0x19) {
		    returnData = squareTwo.readRegister(apuRegister);
	    }
	    else if (apuRegister >= 0x1A && apuRegister <= 0x1E) {
		    returnData = waveChannel.readRegister(address);
	    }
	    else if (apuRegister >= 0x1F && apuRegister <= 0x23) {
		    returnData = noiseChannel.readRegister(address);
	    }
        else if (apuRegister >= 0x24 && apuRegister <= 0x26) {
		switch (apuRegister) {
			case 0x24:
                byte vinRightValue = vinRightEnable ? (byte) 1 : (byte) 0;
                byte vinLeftValue = vinLeftEnable ? (byte) 1 : (byte) 0;
				returnData = (byte)((rightVol) | (vinRightValue << 3) | (leftVol << 4) | (vinLeftValue << 7));
				break;
			case 0x25:
                returnData = 0;
				// Adjusts the enables on left and right
				for (int i = 0; i < 4; i++) {
                    byte rightEnablesValue = rightEnables[i] ? (byte) 1 : (byte) 0;
					returnData |= (byte)((rightEnablesValue << i));
				}
				for (int i = 0; i < 4; i++) {
                    byte leftEnablesValue = leftEnables[i] ? (byte) 1 : (byte) 0;
					returnData |= (byte)((leftEnablesValue << (i+4)));
				}
				break;
			case 0x26:
				// Power Control
                returnData = 0x00;
                byte powerControlValue = powerControl ? (byte) 1 : (byte) 0;
                byte squareOneValue = squareOne.getRunning() ? (byte) 1 : (byte) 0;
                byte squareTwoValue = squareTwo.getRunning() ? (byte) 1 : (byte) 0;
                byte waveChannelValue = waveChannel.getRunning() ? (byte) 1 : (byte) 0;
                byte noiseChannelValue = noiseChannel.getRunning() ? (byte) 1 : (byte) 0;
				returnData |= (byte)(powerControlValue << 7);
				returnData |= (byte)(squareOneValue << 0);
				returnData |= (byte)(squareTwoValue << 1);
				returnData |= (byte)(waveChannelValue << 2);
				returnData |= (byte)(noiseChannelValue << 3);
				break;
		}
	}
	else if (apuRegister >= 0x30 && apuRegister <= 0x3F) {
		returnData = waveChannel.readRegister(address);
	}
	if (apuRegister <= 0x26) {
		returnData |= readOrValues[apuRegister - 0x10];
	}
        // Debug.Log("Read " + address.ToString("X4") + " data: " + returnData.ToString("X2"));
        return returnData;
    }

    public void Write(ushort address, byte data) {
        if(!powerControl && address != 0xFF26) {
            return;
        }

        byte apuRegister = (byte)(address & 0xFF);
        // Pulse 1 redirect

        // Debug.Log(message: "Write " + address.ToString("X4") + " " + data.ToString("X2"));
        if (apuRegister >= 0x10 && apuRegister <= 0x14) {
            squareOne.writeRegister(apuRegister, data);
        }

        // Pulse 2 redirect
        // ignore 0x15 since sweep doesn't exist
        else if (apuRegister >= 0x16 && apuRegister <= 0x19) {
            squareTwo.writeRegister(apuRegister, data);
        } else if (apuRegister >= 0x1A && apuRegister <= 0x1E) {
		    waveChannel.writeRegister(address, data);
	    } else if (apuRegister >= 0x30 && apuRegister <= 0x3F) {
		    waveChannel.writeRegister(address, data);
	    } else if (apuRegister >= 0x1F && apuRegister <= 0x23) {
		    noiseChannel.writeRegister(address, data);
	    } else if (apuRegister >= 0x24 && apuRegister <= 0x26) {
            switch (apuRegister) {
                case 0x24:
                    // Vin bits don't do anything right now. It has something to do with cartridge mixing.
                    // Right
                    rightVol = (byte)(data & 0x7);
                    vinRightEnable = (data & 0x8) == 0x8;
                    // left
                    leftVol = (byte)((data >> 4) & 0x7);
                    vinLeftEnable = (data & 0x80) == 0x80;
                    break;
                case 0x25:
                    // Adjusts the enables on left and right
                    for (int i = 0; i < 4; i++) {
                        rightEnables[i] = ((data >> i) & 0x1) == 0x1;
                    }
                    for (int i = 0; i < 4; i++) {
                        leftEnables[i] = ((data >> (i+4)) & 0x1) == 0x1;
                    }                    
                    break;
                case 0x26:
                    // I don't think writing to length statues does anything
                    // Power control
                    
                    // Shut off event loop
                    // Writes 0 to every register besides this one
                    if ((data & 0x80) != 0x80) {
                        for (ushort i = 0xFF10; i <= 0xFF25; i++) {
                            Write(i, 0);
                        }
                        powerControl = false;
                    }
                    // Only turn on if powerControl was previously off
                    else if (!powerControl) {
                        // Turn on event resets channels, probably do that later.
                        frameSequencer = 0;
                        // Reset wave table
                        for (int i = 0; i < 16; i++) {
                            waveChannel.writeRegister((ushort)(0xFF30 | i), 0);
                        }
                        powerControl = true;
                    }
                    break;
            } 
        }
    }

    public void UpdateAudioTimer(uint cycles) {
        while (cycles-- != 0) {
        // Frame Sequencer
        // https://gist.github.com/drhelius/3652407 
            if (--frameSequenceCountDown <= 0) {
                frameSequenceCountDown = 8192;
                switch (frameSequencer) {
                    case 0:
                        squareOne.lengthClck();
					    squareTwo.lengthClck();
                        waveChannel.lengthClck();
                        noiseChannel.lengthClck();
                        break;
                    case 1:
                        break;
                    case 2:
                        squareOne.sweepClck();
					    squareOne.lengthClck();
					    squareTwo.lengthClck();
                        waveChannel.lengthClck();
                        noiseChannel.lengthClck();
                        break;
                    case 3:
                        break;
                    case 4:
                        squareOne.lengthClck();
					    squareTwo.lengthClck();
                        waveChannel.lengthClck();
                        noiseChannel.lengthClck();
                        break;
                    case 5:
                        break;
                    case 6:
                        squareOne.sweepClck();
					    squareOne.lengthClck();
					    squareTwo.lengthClck();
                        waveChannel.lengthClck();
                        noiseChannel.lengthClck();
                        break;
                    case 7:
                        squareOne.envClck();
					    squareTwo.envClck();
                        noiseChannel.envClck();
                        break;
                }
                frameSequencer++;
                if(frameSequencer >= 8) {
                    frameSequencer = 0;
                }
            }

            squareOne.step();
            squareTwo.step();
            waveChannel.step();
            noiseChannel.step();
    
            if(APUBufferCount % Speed == 0) {
                downSampleCount = (int)(CPUSpeed / AudioSampleRate);

                // Left
                float bufferin0 = 0;
                byte buffer = 0;
                float bufferin1 = 0;
                float volume = leftVol/10.0f;
                float divider = 50.0f;
                if (leftEnables[0]) {
				    bufferin1 = squareOne.getOutputVol() / divider;
                    bufferin0 = (bufferin0 + bufferin1) * volume;
                    buffer = (byte)(buffer + squareOne.getOutputVol());
			    }
                if (leftEnables[1]) {
                    bufferin1 = squareTwo.getOutputVol() / divider;
                    bufferin0 = (bufferin0 + bufferin1) * volume;
                    buffer = (byte)(buffer + squareTwo.getOutputVol());
                }
                if (leftEnables[2]) {
                    bufferin1 = waveChannel.getOutputVol() / divider;
                    bufferin0 = (bufferin0 + bufferin1) * volume;
                    buffer = (byte)(buffer + waveChannel.getOutputVol());
                }
                if (leftEnables[3]) {
                    bufferin1 = noiseChannel.getOutputVol() / divider;
                    bufferin0 = (bufferin0 + bufferin1) * volume;
                    buffer = (byte)(buffer + noiseChannel.getOutputVol());
                }
			    mainBuffer[APUBufferCount / Speed] = bufferin0;
                buffer = 0;
                bufferin0 = 0;
                volume = rightVol/10.0f;
                if (rightEnables[0]) {
                    bufferin1 = squareOne.getOutputVol() / divider;
                    bufferin0 = (bufferin0 + bufferin1) * volume;
                    buffer = (byte)(buffer + squareOne.getOutputVol());
                }
                if (rightEnables[1]) {
                    bufferin1 = squareTwo.getOutputVol() / divider;
                    bufferin0 = (bufferin0 + bufferin1) * volume;
                    buffer = (byte)(buffer + squareTwo.getOutputVol());
                }
                if (rightEnables[2]) {
                    bufferin1 = waveChannel.getOutputVol() / divider;
                    bufferin0 = (bufferin0 + bufferin1) * volume;
                    buffer = (byte)(buffer + waveChannel.getOutputVol());
                }
                if (rightEnables[3]) {
                    bufferin1 = noiseChannel.getOutputVol() / divider;
                    bufferin0 = (bufferin0 + bufferin1) * volume;
                    buffer = (byte)(buffer + noiseChannel.getOutputVol());
                }
                mainBuffer[(APUBufferCount+1) / Speed] = bufferin0;
            } 
            APUBufferCount += 2;
            if(APUBufferCount/Speed >= mainBuffer.Length) {
                APUBufferCount = 0;
                WriteSamples = true;
            }
        }
    }
}
