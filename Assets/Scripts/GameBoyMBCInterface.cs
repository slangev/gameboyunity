public interface GameBoyMBC
{
    void Write(ushort PC, byte data);
    byte Read(ushort PC);
}
