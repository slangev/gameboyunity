using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;

public class GameBoy : MonoBehaviour
{
	private int _samplesAvailable;
	private PipeStream _pipeStream;
	private float[] _buffer;
    private static float[] _volData;
    private class PipeStream
	{
		private readonly Queue<float> _buffer = new();
		private long _maxBufferLength = 8192;
		
		public long MaxBufferLength
		{
			get { return _maxBufferLength; }
			set { _maxBufferLength = value; }
		}
		
		public void Dispose()
		{
			_buffer.Clear();
		}
	
		public int Read(float[] buffer, int offset, int count)
		{
			if (offset != 0)
				throw new NotImplementedException("Offsets with value of non-zero are not supported");
			if (buffer == null)
				throw new ArgumentException("Buffer is null");
			if (offset + count > buffer.Length)
				throw new ArgumentException("The sum of offset and count is greater than the buffer length. ");
			if (offset < 0 || count < 0)
				throw new ArgumentOutOfRangeException("offset", "offset or count is negative.");
			
			if (count == 0)
				return 0;
			
			int readLength = 0;
			
			lock (_buffer)
			{
				// fill the read buffer
				for (; readLength < count && Length > 0; readLength++)
				{
					buffer [readLength] = _buffer.Dequeue();
				}
			}
			
			return readLength;
		}

		private bool ReadAvailable(int count)
		{
			return (Length >= count);
		}

		public void Write(float[] buffer, int offset, int count)
		{
			lock (_buffer)
			{
				while (Length >= _maxBufferLength)
					return;
				
				// queue up the buffer data
				foreach (float b in buffer)
				{
					_buffer.Enqueue(b);
				}
			}
		}

		public bool CanRead
		{
			get { return true; }
		}

		public bool CanSeek
		{
			get { return false; }
		}

		public bool CanWrite
		{
			get { return true; }
		}

		public long Length
		{
			get { return _buffer.Count; }
		}
	
		public long Position
		{
			get { return 0; }
			set { throw new NotImplementedException(); }
		}
	}    
    
    void OnAudioFilterRead(float[] data, int channels) {
        if(gbAudio == null) {
            return;
        }
        if (_buffer.Length != data.Length)
		{
			Debug.Log("Does DSPBufferSize or speakerMode changed? Audio disabled.");
			return;
		}
       
        int r = _pipeStream.Read(_buffer, 0, data.Length);
        for (int i=0; i<r; ++i)
        {
            data [i] = _buffer [i];
        }
        gbAudio.WriteSamples = false;
    }

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
    private readonly ushort width = 160;
    private readonly ushort height = 144;
    const uint MAXCYCLES = 69905;
    public string pathToRom = "";
    public string pathToBios = "";
    IEnumerator emu; 
    IEnumerator handleInputCoroutine;

    void InitalizeComponent() {
        //Create display
        texture = new Texture2D(width,height);
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, width, height), Vector2.zero,1);
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
        emu = Run();
        handleInputCoroutine = HandleInput();
        StartCoroutine(emu);
        StartCoroutine(handleInputCoroutine);
    }

    IEnumerator HandleInput() {
        while(true) {
            gbJoyPad.HandleKeyEvents();
            yield return null;
        }
    }

    IEnumerator Run() {
        while(true) {
            uint cyclesThisUpdate = 0 ; 
            while (cyclesThisUpdate < MAXCYCLES * gbMemory.GetSpeed()) {
                uint cycles = gbCPU.Tick();
                cyclesThisUpdate+=cycles ;
                gbTimer.UpdateTimers(cycles);
                gbGraphic.UpdateGraphics(cycles);
                gbAudio.UpdateAudioTimer(cycles);
                if(gbAudio.WriteSamples){
                    _volData = gbAudio.mainVolBuffer;
                    _pipeStream.Write(gbAudio.mainBuffer,0,gbAudio.mainBuffer.Length);
                    yield return new WaitUntil(() => !gbAudio.WriteSamples);
                }
            }

            gbGraphic.DrawScreen();
            yield return null;
        }
    }

    public void Submit() {
        gbJoyPad.Submit();
    }

    void Awake() {
        // Get Unity Buffer size
        AudioSettings.GetDSPBufferSize(out int bufferLength, out int numBuffers);
        _samplesAvailable = bufferLength;

        // Prepare our buffer
        _pipeStream = new PipeStream
        {
            MaxBufferLength = _samplesAvailable * 2 * 2
        };
        _buffer = new float[_samplesAvailable * 2];
        
        InitalizeComponent();
    }

    void OnDestroy() {
        StopCoroutine(emu);
        StopCoroutine(handleInputCoroutine);
    }
}
