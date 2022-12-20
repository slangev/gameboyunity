using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;

public class GameBoy : MonoBehaviour
{

    public float Gain = 0.05f;
	private int _samplesAvailable;
	private PipeStream _pipeStream;
	private byte[] _buffer;
    private class PipeStream : Stream
	{
		private readonly Queue<byte> _buffer = new Queue<byte>();
		private long _maxBufferLength = 8192;
		
		public long MaxBufferLength
		{
			get { return _maxBufferLength; }
			set { _maxBufferLength = value; }
		}
		
		public new void Dispose()
		{
			_buffer.Clear();
		}

		public override void Flush()
		{
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotImplementedException();
		}

		public override void SetLength(long value)
		{
			throw new NotImplementedException();
		}
	
		public override int Read(byte[] buffer, int offset, int count)
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

		public override void Write(byte[] buffer, int offset, int count)
		{
			lock (_buffer)
			{
				while (Length >= _maxBufferLength)
					return;
				
				// queue up the buffer data
				foreach (byte b in buffer)
				{
					_buffer.Enqueue(b);
				}
			}
		}

		public override bool CanRead
		{
			get { return true; }
		}

		public override bool CanSeek
		{
			get { return false; }
		}

		public override bool CanWrite
		{
			get { return true; }
		}

		public override long Length
		{
			get { return _buffer.Count; }
		}
	
		public override long Position
		{
			get { return 0; }
			set { throw new NotImplementedException(); }
		}
	}
    
    void Awake() {
        // Get Unity Buffer size
		int bufferLength = 0, numBuffers = 0;
		AudioSettings.GetDSPBufferSize(out bufferLength, out numBuffers);
		_samplesAvailable = bufferLength;

		// Prepare our buffer
		_pipeStream = new PipeStream();
		_pipeStream.MaxBufferLength = _samplesAvailable * 2 * 2;
		_buffer = new byte[_samplesAvailable * 2];
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
			data [i] = _buffer [i] / 50.0f;
		}
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
    public string pathToRom = "/Users/slangev/Documents/Unreal_Projects/GBUnreal/Content/Data/Pokemon - Yellow Version - Special Pikachu Edition (USA, Europe) (GBC,SGB Enhanced).gb";
    public string pathToBios = "/Users/slangev/Unity3D/gameboyunity/Assets/Roms/gbc_bios.bin";
    IEnumerator emu; 
    void InitalizeComponent() {
        Application.targetFrameRate = 63;
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
        StartCoroutine(emu);
    }

    void Start() {
        InitalizeComponent();
    }

    IEnumerator Run() {
        while(true) {
            uint cyclesThisUpdate = 0 ; 

            while (cyclesThisUpdate < MAXCYCLES * gbMemory.GetSpeed()) {
                gbJoyPad.HandleKeyEvents();
                uint cycles = gbCPU.Tick();
                cyclesThisUpdate+=cycles ;
                gbTimer.UpdateTimers(cycles);
                gbGraphic.UpdateGraphics(cycles);
                gbAudio.UpdateAudioTimer(cycles);
                if(gbAudio.WriteSamples){
                    _pipeStream.Write(gbAudio.mainBuffer,0,gbAudio.mainBuffer.Length);
                    gbAudio.WriteSamples = false;
                }
            }

            gbGraphic.DrawScreen();
            yield return null;
        }
    }

    public void Submit() {
        gbJoyPad.Submit();
    }

    void OnDestroy() {
        StopCoroutine(emu);
    }
}
