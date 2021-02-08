using System.Collections;
using System.Collections.Generic;
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
        uint duty = 0;
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
            return lengthCounter > 0;
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
            enabled = true;
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
                    //lengthCounter = 64 - (lengthLoad & 0x3F);
                    duty = (byte)((data >> 6) & 0x3);
                    break;
                // Envelope
                case 0x2:
                    // See if dac is enabled, if all high 5 bits are not 0
                    dacEnabled = (data & 0xF8) != 0;
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

        public byte readRegister(ushort address) {
            byte returnData = 0;
	        byte squareRegister = (byte)((address & 0xF) % 0x5);
            switch (squareRegister) {
                // Sweep. Only on Square 1
            case 0x0:
                //returnData = (sweepShift) | ((sweepNegate) << 3) | (sweepPeriodLoad << 4);
                break;
                // Duty, Length Load
            case 0x1:
                //returnData = (lengthLoad & 0x3F) | ((duty & 0x3) << 6);
                break;
                // Envelope
            case 0x2:
                //returnData = (envelopePeriodLoad & 0x7) | (envelopeAddMode << 3) | ((volumeLoad & 0xF) << 4);
                break;
            case 0x3:
                //returnData = timerLoad & 0xFF;
                break;
                // Trigger, length enable, frequency MSB
            case 0x4:
                //returnData = ((timerLoad >> 8) & 0x7) | (lengthEnable << 6) | (triggerBit << 7);	// Trigger bit probably?
                // Trigger is on 0x80, bit 7.
                break;
            }

            return returnData;
        }

    internal class WaveChannel {
        public WaveChannel () {
            
        }
    }

    internal class NoiseChannel {
        public NoiseChannel () {
            
        }
    }

    private int channels = 2;
    private int freq = 44100;
    private static readonly int sample = 4096;
    private AudioSource audio;
    private int frameSequenceCountDown = 8192;
    private int frameSequencer = 8;

    // Values OR'd into register reads.
	readonly byte[] readOrValues = new byte[] { 0x80,0x3f,0x00,0xff,0xbf,
										0xff,0x3f,0x00,0xff,0xbf,
										0x7f,0xff,0x9f,0xff,0xbf,
										0xff,0xff,0x00,0x00,0xbf,
										0x00,0x00,0x70 };
	// APU Universal registers
	bool vinLeftEnable = false;
	byte leftVol = 0;
	bool vinRightEnable = false;
	byte rightVol = 0;
	bool[] leftEnables = new bool[]{ false,false,false,false };
	bool[] rightEnables = new bool[]{ false,false,false,false };
	bool powerControl = false;
	int downSampleCount = 95;
	int bufferFillAmount = 0;
	float[] mainBuffer = new float[sample];
    private SquareChannel squareOne;
    private SquareChannel squareTwo;
    private WaveChannel waveChannel;
    private NoiseChannel noiseChannel;
    
    public GameBoyAudio() {
        initializeAPU();
    }

    private void dummyCode() {
        /*
            float[] samples = new float[samplerate];
            for(int i = 0; i < samples.Length; i++)
            {
                samples[i] = Mathf.Sin(Mathf.PI*2*i*freq/samplerate);
            }
            myClip.SetData(samples,0);
            audio.loop = true;
            audio.Play();
        */
    }

    private void initializeAPU() {
        GameObject gb = GameObject.Find("GameBoyCamera");
        gb.AddComponent(typeof(AudioListener));
        gb.AddComponent(typeof(AudioSource));
        AudioClip myClip = AudioClip.Create("GameBoyAudio", sample, channels, freq, false);
        audio = gb.GetComponent<AudioSource>();
        audio.clip = myClip;
        squareOne = new SquareChannel();
        squareTwo = new SquareChannel();
        waveChannel = new WaveChannel();
        noiseChannel = new NoiseChannel();
        
    }

    public byte Read(ushort address) {
        Debug.Log("READ Address: " + address.ToString("X2"));
        return 0;
    }

    public void Write(ushort address, byte data) {
        byte apuRegister = (byte)(address & 0xFF);
        // Pulse 1 redirect
        Debug.Log("WRITE Address: " + apuRegister.ToString("X2") + " " + data.ToString("X2"));
        if (apuRegister >= 0x10 && apuRegister <= 0x14) {
            squareOne.writeRegister(apuRegister, data);
        }
        // Pulse 2 redirect
        // ignore 0x15 since sweep doesn't exist
        else if (apuRegister >= 0x16 && apuRegister <= 0x19) {
            squareTwo.writeRegister(apuRegister, data);
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
                        break;
                    case 1:
                        break;
                    case 2:
                        squareOne.sweepClck();
                        break;
                    case 3:
                        break;
                    case 4:
                        squareOne.lengthClck();
                        break;
                    case 5:
                        break;
                    case 6:
                        squareOne.sweepClck();
                        break;
                    case 7:
                        squareOne.envClck();
                        break;
                }
                frameSequencer++;
                if(frameSequencer >= 8) {
                    frameSequencer = 0;
                }
            }
            squareOne.step();
            if(--downSampleCount <= 0) {
                downSampleCount = 95;

                // Left
                float bufferin0 = 0;
                float bufferin1 = 0;
                int volume = (128*leftVol)/7;
                if (leftEnables[0]) {
				    bufferin1 = ((float)squareOne.getOutputVol()) / 100;
				    //SDL_MixAudioFormat((Uint8*)&bufferin0, (Uint8*)&bufferin1, AUDIO_F32SYS, sizeof(float), volume);
			    }
                /*if (leftEnables[1]) {
                    bufferin1 = ((float)squareTwo.getOutputVol()) / 100;
                    //SDL_MixAudioFormat((Uint8*)&bufferin0, (Uint8*)&bufferin1, AUDIO_F32SYS, sizeof(float), volume);
                }
                if (leftEnables[2]) {
                    bufferin1 = ((float)waveChannel.getOutputVol()) / 100;
                    //SDL_MixAudioFormat((Uint8*)&bufferin0, (Uint8*)&bufferin1, AUDIO_F32SYS, sizeof(float), volume);
                }
                if (leftEnables[3]) {
                    bufferin1 = ((float)noiseChannel.getOutputVol()) / 100;
                    //SDL_MixAudioFormat((Uint8*)&bufferin0, (Uint8*)&bufferin1, AUDIO_F32SYS, sizeof(float), volume);
                }*/
			    mainBuffer[bufferFillAmount] = bufferin0;
            }
        }
    }
}
