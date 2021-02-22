using NUnit.Framework;

namespace Tests
{
    public class GameBoyTest
    {
        GameBoyMemory gbMemory;
        GameBoyCPU gbCPU;
        GameBoyTimer gbTimer;
        GameBoyInterrupts gbInterrupts;
        GameBoyCartiridge gbCart;
        GameBoyGraphic gbGraphics;

        /* TODO
            halt
            0x02
            0x0A
            0x3A
            0x06
            0x0E
            0x1E
            0x26
            0x2E
            BIT(N,HL)
            RES(N,HL)
            SET(N,HL)
            DAA
            PPU test https://github.com/mattcurrie/dmg-acid2
        */

        [SetUp] 
        public void Init() {
            gbCart = new GameBoyCartiridge(0x10000,0xFF); //0xFF = no mbc test cart
            gbMemory = new GameBoyMemory(gbCart);
            gbMemory.WriteToMemory(0xFF50,0x01); // Disable bootstrap
            gbInterrupts = new GameBoyInterrupts(gbMemory);
            gbTimer = new GameBoyTimer(gbMemory,gbInterrupts);
            gbCPU = new GameBoyCPU(gbMemory,gbInterrupts);
            gbGraphics = new GameBoyGraphic(160,144,null,gbInterrupts,gbMemory);
            GameBoyCPU.ClockCycle = 0;
            GameBoyInterrupts.IMEHold = false;
            GameBoyInterrupts.IMEFlag = false;
            gbCPU.SP = 0xFFFE;
        }

/*********************************************LOADS TESTs*********************************************/
        [Test]
        public void GameBoyTestLoadIntoSPTest()
        {
            //Load FFFE into SP.
            gbMemory.WriteToMemory(0,0x31);
            gbMemory.WriteToMemory(1,0xFE);
            gbMemory.WriteToMemory(2,0xFF);
            gbCPU.SP = 0x0000;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xFFFE, gbCPU.SP);
            Assert.AreEqual(GameBoyCPU.ClockCycle, cycle);
        }

        [Test]
        public void GameBoyTestLoadSPa16()
        {
            //Load SP(0xFFF8) into (0x0080).
            gbMemory.WriteToMemory(0,0x08);
            gbMemory.WriteToMemory(1,0x80);
            gbMemory.WriteToMemory(2,0x00);
            gbCPU.SP = 0xFFF8;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xFFF8, gbCPU.SP);
            Assert.AreEqual(20, cycle);
            Assert.AreEqual(3, gbCPU.PC);
            Assert.AreEqual(0xF8, gbMemory.ReadFromMemory(0x0080));
            Assert.AreEqual(0xFF, gbMemory.ReadFromMemory(0x0081));
        }

        [Test]
        public void GameBoyTestLoadSPHL()
        {
            //Load HL into SP.
            gbMemory.WriteToMemory(0,0xF9);
            gbCPU.H = 0x69;
            gbCPU.L = 0x01;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x6901, gbCPU.SP);
            Assert.AreEqual(8, cycle);
            Assert.AreEqual(1, gbCPU.PC);
        }

        [Test]
        public void GameBoyTestLoadIntoBCTest()
        {
            //Load d16(0x0400) into BC.
            gbMemory.WriteToMemory(0,0x01);
            gbMemory.WriteToMemory(1,0x00);
            gbMemory.WriteToMemory(2,0x04);
            gbCPU.B = 0x00;
            gbCPU.C = 0x00;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x04, gbCPU.B);
            Assert.AreEqual(0x00, gbCPU.C);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestLoada16A()
        {
            //Load adress16(0x8000) = 0x80 into A.
            gbMemory.WriteToMemory(0,0xFA);
            gbMemory.WriteToMemory(1,0x00);
            gbMemory.WriteToMemory(2,0x80);
            gbMemory.WriteToMemory(0x8000,0x80);
            gbCPU.A = 0x00;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x80, gbCPU.A);
            Assert.AreEqual(16, cycle);
        }

        [Test]
        public void GameBoyTestLoadIntoMemory()
        {
            //Load FFFE into Memory.
            gbCPU.A = 0xFC;
            gbMemory.WriteToMemory(0,0xE0);
            gbMemory.WriteToMemory(1,0x47);
            gbMemory.WriteToMemory(0xFF00+0x47,gbCPU.A);
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(gbCPU.A, gbMemory.ReadFromMemory(0xFF00+0x47));
            Assert.AreEqual(cycle, GameBoyCPU.ClockCycle);
        }

        [Test]
        public void GameBoyTestLoadFromMemoryIntoReg()
        {
            //Load FFFE into Memory.
            gbMemory.WriteToMemory(0,0xF0);
            gbMemory.WriteToMemory(1,0x80);
            gbMemory.WriteToMemory(0xFF00+0x80,0x69);
            gbCPU.A = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(gbCPU.A, gbMemory.ReadFromMemory(0xFF00+0x80));
            Assert.AreEqual(0x69, gbCPU.A);
            Assert.AreEqual(8, cycle);
            Assert.AreEqual(2, gbCPU.PC);
        }

        [Test]
        public void GameBoyTestLoadFromMemoryWordIntoReg()
        {
            //Load FFFE into Memory.
            gbMemory.WriteToMemory(0,0xFA);
            gbMemory.WriteToMemory(1,0x03);
            gbMemory.WriteToMemory(2,0x00);
            gbMemory.WriteToMemory(3,0x40);

            gbCPU.A = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(gbCPU.A, 0x40);
            Assert.AreEqual(16, cycle);
        }

        [Test]
        public void GameBoyTestLDRNC()
        {
            //Load 0x11 into C
            gbMemory.WriteToMemory(0, 0x0E);
            gbMemory.WriteToMemory(1, 0x11);
            gbCPU.C = 0x00;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(gbMemory.ReadFromMemory(1), gbCPU.C);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestLDRND()
        {
            //Load 0x20 into D
            gbMemory.WriteToMemory(0, 0x16);
            gbMemory.WriteToMemory(1, 0x20);
            gbCPU.D = 0x00;
            gbCPU.Tick();
            Assert.AreEqual(gbMemory.ReadFromMemory(1), gbCPU.D);
            Assert.AreEqual(8, GameBoyCPU.ClockCycle);
        }

        [Test]
        public void GameBoyTestLDRRAD()
        {
            //Load D into A
            gbCPU.PC = 0x0;
            gbCPU.D = 0xE0;
            gbCPU.A = 0xFF;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x7A));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(gbCPU.A, 0xE0);
        }

        [Test]
        public void GameBoyTestLDRREB()
        {
            //Load B into E
            gbCPU.PC = 0x0;
            gbCPU.E = 0xE0;
            gbCPU.B = 0xFF;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x58));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(gbCPU.E, 0xFF);
            Assert.AreEqual(1, gbCPU.PC);
        }

        [Test]
        public void GameBoyTestLDRREC()
        {
            //Load C into E
            gbCPU.PC = 0x0;
            gbCPU.E = 0xE0;
            gbCPU.C = 0xFF;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x59));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(gbCPU.E, 0xFF);
            Assert.AreEqual(1, gbCPU.PC);
        }

        [Test]
        public void GameBoyTestLDRRED()
        {
            //Load D into E
            gbCPU.PC = 0x0;
            gbCPU.E = 0xE0;
            gbCPU.D = 0xFF;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x5A));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(gbCPU.E, 0xFF);
            Assert.AreEqual(1, gbCPU.PC);
        }

        [Test]
        public void GameBoyTestLDRREE()
        {
            //Load E into E
            gbCPU.PC = 0x0;
            gbCPU.E = 0xE0;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x5B));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(gbCPU.E, 0xE0);
            Assert.AreEqual(1, gbCPU.PC);
        }

        [Test]
        public void GameBoyTestLDRREH()
        {
            //Load H into E
            gbCPU.PC = 0x0;
            gbCPU.E = 0xE0;
            gbCPU.H = 0xFF;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x5C));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(gbCPU.E, 0xFF);
            Assert.AreEqual(1, gbCPU.PC);
        }

        [Test]
        public void GameBoyTestLDRREL()
        {
            //Load L into E
            gbCPU.PC = 0x0;
            gbCPU.L = 0xE0;
            gbCPU.E = 0xFF;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x5D));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(gbCPU.E, 0xE0);
        }

        [Test]
        public void GameBoyTestLDRNE()
        {
            //Load 0x02 into E
            gbMemory.WriteToMemory(0, 0x1E);
            gbMemory.WriteToMemory(1, 0x02);
            gbCPU.E = 0x00;
            gbCPU.Tick();
            Assert.AreEqual(gbMemory.ReadFromMemory(1), gbCPU.E);
            Assert.AreEqual(8, GameBoyCPU.ClockCycle);
        }

        [Test]
        public void GameBoyTestLDRNLn()
        {
            //Load 0x0F into L
            gbMemory.WriteToMemory(0, 0x2E);
            gbMemory.WriteToMemory(1, 0x0F);
            gbCPU.L = 0x00;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(gbMemory.ReadFromMemory(1), gbCPU.L);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestLDRNHn()
        {
            //Load 0x0F into H
            gbMemory.WriteToMemory(0, 0x26);
            gbMemory.WriteToMemory(1, 0x0F);
            gbCPU.H = 0x00;
            gbCPU.Tick();
            Assert.AreEqual(gbMemory.ReadFromMemory(1), gbCPU.H);
            Assert.AreEqual(8, GameBoyCPU.ClockCycle);
        }

        [Test]
        public void GameBoyTestLDRRAA()
        {
            //Load A into A
            gbCPU.PC = 0x0;
            gbCPU.A = 0xFF;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x7F));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(1,gbCPU.PC);
            Assert.AreEqual(gbCPU.A, 0xFF);
        }

        [Test]
        public void GameBoyTestLDRRAC()
        {
            //Load C into A
            gbCPU.PC = 0x0;
            gbCPU.C = 0xF0;
            gbCPU.A = 0xFF;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x79));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(1, gbCPU.PC);
            Assert.AreEqual(gbCPU.A, 0xF0);
        }

        [Test]
        public void GameBoyTestLDRRLE()
        {
            //Load E into L
            GameBoyCPU.ClockCycle = 0;
            gbCPU.PC = 0x0;
            gbCPU.L = 0x00;
            gbCPU.E = 0x77;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x6B));
            gbCPU.Tick();
            Assert.AreEqual(4, GameBoyCPU.ClockCycle);
            Assert.AreEqual(gbCPU.L, 0x77);
        }

        [Test]
        public void GameBoyTestLDRRLB()
        {
            //Load B into L
            gbCPU.PC = 0x0;
            gbCPU.L = 0x00;
            gbCPU.B = 0x77;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x68));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(1,gbCPU.PC);
            Assert.AreEqual(gbCPU.L, 0x77);
        }

        [Test]
        public void GameBoyTestLDRRLC()
        {
            //Load C into L
            gbCPU.PC = 0x0;
            gbCPU.L = 0x00;
            gbCPU.C = 0x77;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x69));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(1,gbCPU.PC);
            Assert.AreEqual(gbCPU.L, 0x77);
        }

        [Test]
        public void GameBoyTestLDRRLD()
        {
            //Load C into L
            gbCPU.PC = 0x0;
            gbCPU.L = 0x00;
            gbCPU.D = 0x77;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x6A));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(1,gbCPU.PC);
            Assert.AreEqual(gbCPU.L, 0x77);
        }

        [Test]
        public void GameBoyTestLDRRLH()
        {
            //Load H into L
            gbCPU.PC = 0x0;
            gbCPU.L = 0x00;
            gbCPU.H = 0x77;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x6C));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(1,gbCPU.PC);
            Assert.AreEqual(gbCPU.L, 0x77);
        }

        [Test]
        public void GameBoyTestLDRRLL()
        {
            //Load H into L
            gbCPU.PC = 0x0;
            gbCPU.L = 0x77;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x6D));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(1,gbCPU.PC);
            Assert.AreEqual(gbCPU.L, 0x77);
        }

        [Test]
        public void GameBoyTestLDRRCA()
        {
            //Load A into C
            gbCPU.PC = 0x0;
            gbCPU.A = 0xF0;
            gbCPU.C = 0xFF;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x4F));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(gbCPU.C, 0xF0);
        }

        [Test]
        public void GameBoyTestLDRRCB()
        {
            //Load B into C
            gbCPU.PC = 0x0;
            gbCPU.B = 0xF0;
            gbCPU.C = 0xFF;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x48));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(1, gbCPU.PC);
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(gbCPU.C, 0xF0);
        }

        [Test]
        public void GameBoyTestLDRRCC()
        {
            //Load C into C
            gbCPU.PC = 0x0;
            gbCPU.C = 0xFF;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x49));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(1, gbCPU.PC);
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(gbCPU.C, 0xFF);
        }

        [Test]
        public void GameBoyTestLDRRCD()
        {
            //Load D into C
            gbCPU.PC = 0x0;
            gbCPU.C = 0xFF;
            gbCPU.D = 0x69;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x4A));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(1, gbCPU.PC);
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(gbCPU.C, 0x69);
        }

        [Test]
        public void GameBoyTestLDRRCE()
        {
            //Load E into C
            gbCPU.PC = 0x0;
            gbCPU.C = 0xFF;
            gbCPU.E = 0x69;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x4B));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(1, gbCPU.PC);
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(gbCPU.C, 0x69);
        }

        [Test]
        public void GameBoyTestLDRRCH()
        {
            //Load H into C
            gbCPU.PC = 0x0;
            gbCPU.C = 0xFF;
            gbCPU.H = 0x69;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x4C));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(1, gbCPU.PC);
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(gbCPU.C, 0x69);
        }

        [Test]
        public void GameBoyTestLDRRCL()
        {
            //Load L into C
            gbCPU.PC = 0x0;
            gbCPU.C = 0xFF;
            gbCPU.L = 0x69;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x4D));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(1, gbCPU.PC);
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(gbCPU.C, 0x69);
        }

        [Test]
        public void GameBoyTestLDRRHA()
        {
            // Load A into H
            gbCPU.PC = 0x0;
            gbCPU.H = 0xF0;
            gbCPU.A = 0xFF;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x67));
            gbCPU.Tick();
            Assert.AreEqual(4, GameBoyCPU.ClockCycle);
            Assert.AreEqual(gbCPU.H, gbCPU.A);
        }

        [Test]
        public void GameBoyTestLDRRHC()
        {
            // Load C into H
            gbCPU.PC = 0x0;
            gbCPU.H = 0xF0;
            gbCPU.C = 0x69;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x61));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(1, gbCPU.PC);
            Assert.AreEqual(0x69, gbCPU.H);
        }

        [Test]
        public void GameBoyTestLDRRHD()
        {
            // Load D into H
            gbCPU.PC = 0x0;
            gbCPU.H = 0xF0;
            gbCPU.D = 0x69;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x62));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(1, gbCPU.PC);
            Assert.AreEqual(0x69, gbCPU.H);
        }

        [Test]
        public void GameBoyTestLDRRHE()
        {
            // Load E into H
            gbCPU.PC = 0x0;
            gbCPU.H = 0xF0;
            gbCPU.E = 0x69;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x63));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(1, gbCPU.PC);
            Assert.AreEqual(0x69, gbCPU.H);
        }

        [Test]
        public void GameBoyTestLDRRHH()
        {
            // Load H into H
            gbCPU.PC = 0x0;
            gbCPU.H = 0xF0;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x64));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(1, gbCPU.PC);
            Assert.AreEqual(0xF0, gbCPU.H);
        }

        [Test]
        public void GameBoyTestLDRRHL()
        {
            // Load L into H
            gbCPU.PC = 0x0;
            gbCPU.H = 0xF0;
            gbCPU.L = 0x69;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x65));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(1, gbCPU.PC);
            Assert.AreEqual(0x69, gbCPU.H);
        }

        [Test]
        public void GameBoyTestLDRRHHL()
        {
            // Load (HL) into H
            gbCPU.PC = 0x0;
            gbCPU.H = 0x00;
            gbCPU.L = 0x01;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x66));
            Assert.True(gbMemory.WriteToMemory(0x1, 0xAA));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle);
            Assert.AreEqual(1, gbCPU.PC);
            Assert.AreEqual(gbMemory.ReadFromMemory(0x1), gbCPU.H);
        }

        [Test]
        public void GameBoyTestLDRRHB()
        {
            // Load B into H
            gbCPU.PC = 0x0;
            gbCPU.H = 0xF0;
            gbCPU.B = 0x69;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x60));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(1, gbCPU.PC);
            Assert.AreEqual(gbCPU.H, gbCPU.B);
        }

        [Test]
        public void GameBoyTestLDRREHL()
        {
            // Load (HL) into E
            gbCPU.PC = 0x0;
            gbCPU.H = 0x00;
            gbCPU.L = 0x01;
            gbCPU.E = 0xFF;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x5E));
            Assert.True(gbMemory.WriteToMemory(0x1, 0xAA));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle);
            Assert.AreEqual(gbMemory.ReadFromMemory(0x1), gbCPU.E);
        }

        [Test]
        public void GameBoyTestLDRRLHL()
        {
            // Load (HL) into L
            gbCPU.PC = 0x0;
            gbCPU.H = 0x00;
            gbCPU.L = 0x01;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x6E));
            Assert.True(gbMemory.WriteToMemory(0x1, 0xAA));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle);
            Assert.AreEqual(gbMemory.ReadFromMemory(0x1), gbCPU.L);
        }

        [Test]
        public void GameBoyTestLDRRLA()
        {
            // Load A into L
            gbCPU.PC = 0x0;
            gbCPU.L = 0xF0;
            gbCPU.A = 0xFF;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x6F));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(gbCPU.L, gbCPU.A);
        }



        [Test]
        public void GameBoyTestLDRRDHL()
        {
            // Load (HL) into D
            gbCPU.PC = 0x0;
            gbCPU.H = 0x00;
            gbCPU.L = 0x01;
            gbCPU.D = 0xFF;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x56));
            Assert.True(gbMemory.WriteToMemory(0x1, 0xAA));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle);
            Assert.AreEqual(gbMemory.ReadFromMemory(0x1), gbCPU.D);
        }

        [Test]
        public void GameBoyTestLDRRDA()
        {
            // Load A into D
            gbCPU.PC = 0x0;
            gbCPU.D = 0xF0;
            gbCPU.A = 0xFF;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x57));
            gbCPU.Tick();
            Assert.AreEqual(4, GameBoyCPU.ClockCycle);
            Assert.AreEqual(gbCPU.D, gbCPU.A);
            Assert.AreEqual(1, gbCPU.PC);
        }

        [Test]
        public void GameBoyTestLDRRDB()
        {
            // Load B into D
            gbCPU.PC = 0x0;
            gbCPU.D = 0xF0;
            gbCPU.B = 0xFF;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x50));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(gbCPU.D, 0xFF);
            Assert.AreEqual(1, gbCPU.PC);
        }

        [Test]
        public void GameBoyTestLDRRDC()
        {
            // Load C into D
            gbCPU.PC = 0x0;
            gbCPU.D = 0xF0;
            gbCPU.C = 0xFF;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x51));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(gbCPU.D, 0xFF);
            Assert.AreEqual(1, gbCPU.PC);
        }

        [Test]
        public void GameBoyTestLDRRDD()
        {
            // Load D into D
            gbCPU.PC = 0x0;
            gbCPU.D = 0xF0;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x52));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(gbCPU.D, 0xF0);
            Assert.AreEqual(1, gbCPU.PC);
        }

        [Test]
        public void GameBoyTestLDRRDE()
        {
            // Load E into D
            gbCPU.PC = 0x0;
            gbCPU.D = 0xF0;
            gbCPU.E = 0xFF;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x53));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(gbCPU.D, 0xFF);
            Assert.AreEqual(1, gbCPU.PC);
        }

        [Test]
        public void GameBoyTestLDRRDH()
        {
            // Load H into D
            gbCPU.PC = 0x0;
            gbCPU.D = 0x69;
            gbCPU.H = 0xFF;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x54));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(gbCPU.D, 0xFF);
            Assert.AreEqual(1, gbCPU.PC);
        }

        [Test]
        public void GameBoyTestLDRRDL()
        {
            // Load H into D
            gbCPU.PC = 0x0;
            gbCPU.D = 0x69;
            gbCPU.L = 0xFF;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x55));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(gbCPU.D, 0xFF);
            Assert.AreEqual(1, gbCPU.PC);
        }

        [Test]
        public void GameBoyTestLDRRAH()
        {
            // Load H into A
            gbCPU.PC = 0x0;
            gbCPU.A = 0x00;
            gbCPU.H = 0xFF;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x7C));
            gbCPU.Tick();
            Assert.AreEqual(4, GameBoyCPU.ClockCycle);
            Assert.AreEqual(gbCPU.H, gbCPU.A);
        }

        [Test]
        public void GameBoyTestLDRRAL()
        {
            // Load L into A
            gbCPU.PC = 0x0;
            gbCPU.A = 0x00;
            gbCPU.L = 0xFF;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x7D));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(gbCPU.L, gbCPU.A);
        }

        [Test]
        public void GameBoyTestLDRREA()
        {
            // Load A into E
            gbCPU.A = 0xFF;
            gbCPU.E = 0xF0;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x5F));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(gbCPU.E, gbCPU.A);
        }

        [Test]
        public void GameBoyTestLDRRAB()
        {
            // Load B into A
            gbCPU.PC = 0x0;
            gbCPU.A = 0x00;
            gbCPU.B = 0x11;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x78));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(gbCPU.B, gbCPU.A);
        }

        [Test]
        public void GameBoyTestLDRRBA()
        {
            // Load A into B
            gbCPU.PC = 0x0;
            gbCPU.A = 0xFF;
            gbCPU.B = 0x11;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x47));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(gbCPU.B, gbCPU.A);
        }

        [Test]
        public void GameBoyTestLDRRBB()
        {
            // Load B into B
            gbCPU.PC = 0x0;
            gbCPU.B = 0x11;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x40));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(gbCPU.B, gbCPU.B);
            Assert.AreEqual(0x1, gbCPU.PC);
        }

        [Test]
        public void GameBoyTestLDRRBC()
        {
            // Load C into B
            gbCPU.PC = 0x0;
            gbCPU.B = 0x11;
            gbCPU.C = 0xFF;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x41));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(gbCPU.C, gbCPU.B);
            Assert.AreEqual(0x1, gbCPU.PC);
        }

        [Test]
        public void GameBoyTestLDRRBD()
        {
            // Load D into B
            gbCPU.PC = 0x0;
            gbCPU.B = 0x11;
            gbCPU.D = 0xFF;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x42));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(gbCPU.D, gbCPU.B);
            Assert.AreEqual(0x1, gbCPU.PC);
        }

        [Test]
        public void GameBoyTestLDRRBE()
        {
            // Load E into B
            gbCPU.PC = 0x0;
            gbCPU.B = 0x11;
            gbCPU.E = 0xFF;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x43));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(gbCPU.E, gbCPU.B);
            Assert.AreEqual(0x1, gbCPU.PC);
        }

        [Test]
        public void GameBoyTestLDRRBH()
        {
            // Load H into B
            gbCPU.PC = 0x0;
            gbCPU.B = 0x11;
            gbCPU.H = 0xFF;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x44));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(gbCPU.H, gbCPU.B);
            Assert.AreEqual(0x1, gbCPU.PC);
        }

        [Test]
        public void GameBoyTestLDRRBL()
        {
            // Load L into B
            gbCPU.PC = 0x0;
            gbCPU.B = 0x11;
            gbCPU.L = 0xFF;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x45));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(gbCPU.L, gbCPU.B);
            Assert.AreEqual(0x1, gbCPU.PC);
        }

        [Test]
        public void GameBoyTestLDRRDEA()
        {
            // Load A into (DE)
            gbCPU.PC = 0x0;
            gbCPU.A = 0xFF;
            gbCPU.D = 0x00;
            gbCPU.E = 0x01;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x12));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x00));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle);
            Assert.AreEqual(gbMemory.ReadFromMemory(0x1), gbCPU.A);
        }

        [Test]
        public void GameBoyTestLDRRHLB()
        {
            // Load B into (HL)
            gbCPU.PC = 0x0;
            gbCPU.B = 0xFF;
            gbCPU.H = 0x00;
            gbCPU.L = 0x01;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x70));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x00));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle);
            Assert.AreEqual(gbMemory.ReadFromMemory(0x1), gbCPU.B);
        }

        [Test]
        public void GameBoyTestLDRRHLC()
        {
            // Load C into (HL)
            gbCPU.PC = 0x0;
            gbCPU.C = 0xFF;
            gbCPU.H = 0x00;
            gbCPU.L = 0x01;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x71));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x00));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle);
            Assert.AreEqual(gbMemory.ReadFromMemory(0x1), gbCPU.C);
        }

        [Test]
        public void GameBoyTestLDRRHLD()
        {
            // Load D into (HL)
            gbCPU.PC = 0x0;
            gbCPU.D = 0xFF;
            gbCPU.H = 0x00;
            gbCPU.L = 0x01;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x72));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x00));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle);
            Assert.AreEqual(gbMemory.ReadFromMemory(0x1), gbCPU.D);
        }

        [Test]
        public void GameBoyTestLDRRHLE()
        {
            // Load E into (HL)
            gbCPU.PC = 0x0;
            gbCPU.E = 0xFF;
            gbCPU.H = 0x00;
            gbCPU.L = 0x01;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x73));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x00));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle);
            Assert.AreEqual(gbMemory.ReadFromMemory(0x1), gbCPU.E);
        }

        [Test]
        public void GameBoyTestLDRRHLH()
        {
            // Load H into (HL)
            gbCPU.PC = 0x0;
            gbCPU.H = 0x00;
            gbCPU.L = 0x01;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x74));
            Assert.True(gbMemory.WriteToMemory(0x1, 0xFF));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle);
            Assert.AreEqual(1,gbCPU.PC);
            Assert.AreEqual(gbMemory.ReadFromMemory(0x1), gbCPU.H);
        }

        [Test]
        public void GameBoyTestLDRRHLL()
        {
            // Load L into (HL)
            gbCPU.PC = 0x0;
            gbCPU.H = 0x00;
            gbCPU.L = 0x01;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x75));
            Assert.True(gbMemory.WriteToMemory(0x1, 0xFF));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle);
            Assert.AreEqual(1,gbCPU.PC);
            Assert.AreEqual(gbMemory.ReadFromMemory(0x1), gbCPU.L);
        }

        [Test]
        public void GameBoyTestLDRRBHL()
        {
            // Load (HL) into B
            gbCPU.PC = 0x0;
            gbCPU.B = 0x00;
            gbCPU.H = 0x00;
            gbCPU.L = 0x01;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x46));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x69));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle);
            Assert.AreEqual(0x01, gbCPU.PC);
            Assert.AreEqual(gbMemory.ReadFromMemory(0x1), gbCPU.B);
        }

        [Test]
        public void GameBoyTestLDRRCHL()
        {
            // Load (HL) into C
            gbCPU.PC = 0x0;
            gbCPU.C = 0x00;
            gbCPU.H = 0x00;
            gbCPU.L = 0x01;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x4E));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x99));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle);
            Assert.AreEqual(0x01, gbCPU.PC);
            Assert.AreEqual(gbMemory.ReadFromMemory(0x1), gbCPU.C);
        }

/********************************************* AND TEST *********************************************/
        [Test]
        public void GameBoyTestANDA()
        {
            // AND A
            gbMemory.WriteToMemory(0,0xA7);
            gbCPU.A = 0x5A;
            gbCPU.F = 0x00;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x5A, gbCPU.A);
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(4, cycle);
        }

        [Test]
        public void GameBoyTestANDB()
        {
            // AND B
            gbMemory.WriteToMemory(0,0xA0);
            gbCPU.A = 0x5A;
            gbCPU.B = 0x3F;
            gbCPU.F = 0x00;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x1A, gbCPU.A);
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(4, cycle);
        }

        [Test]
        public void GameBoyTestANDC()
        {
            // AND C
            gbMemory.WriteToMemory(0,0xA1);
            gbCPU.A = 0x5A;
            gbCPU.C = 0x3F;
            gbCPU.F = 0x00;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x1A, gbCPU.A);
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(4, cycle);
        }

        [Test]
        public void GameBoyTestANDD()
        {
            // AND D
            gbMemory.WriteToMemory(0,0xA2);
            gbCPU.A = 0x5A;
            gbCPU.D = 0x3F;
            gbCPU.F = 0x00;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x1A, gbCPU.A);
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(4, cycle);
        }

        [Test]
        public void GameBoyTestANDE()
        {
            // AND E
            gbMemory.WriteToMemory(0,0xA3);
            gbCPU.A = 0x5A;
            gbCPU.E = 0x3F;
            gbCPU.F = 0x00;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x1A, gbCPU.A);
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(4, cycle);
        }

        [Test]
        public void GameBoyTestANDH()
        {
            // AND H
            gbMemory.WriteToMemory(0,0xA4);
            gbCPU.A = 0x5A;
            gbCPU.H = 0x3F;
            gbCPU.F = 0x00;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x1A, gbCPU.A);
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(4, cycle);
        }

        [Test]
        public void GameBoyTestANDL()
        {
            // AND L
            gbMemory.WriteToMemory(0,0xA5);
            gbCPU.A = 0x5A;
            gbCPU.L = 0x3F;
            gbCPU.F = 0x00;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x1A, gbCPU.A);
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(4, cycle);
        }

        [Test]
        public void GameBoyTestANDd8()
        {
            // AND d8
            gbMemory.WriteToMemory(0,0xE6);
            gbMemory.WriteToMemory(1,0x38);
            gbCPU.A = 0x5A;
            gbCPU.F = 0x00;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x18, gbCPU.A);
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestANDHL()
        {
            // AND (HL)
            gbMemory.WriteToMemory(0,0xA6);
            gbMemory.WriteToMemory(1,0x00);
            gbCPU.A = 0x5A;
            gbCPU.H = 0x00;
            gbCPU.L = 0x01;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x00, gbCPU.A);
            Assert.AreEqual(0xA0, gbCPU.F);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestCPLA()
        {
            // CPL A
            gbMemory.WriteToMemory(0,0x2F);
            gbCPU.A = 0x35;
            gbCPU.F = 0x00;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xCA, gbCPU.A);
            Assert.AreEqual(0x60, gbCPU.F);
            Assert.AreEqual(4, cycle);
        }
        
/******************************************************* SWAP N *******************************************************/ 
        [Test]
        public void GameBoyTestSWAPA()
        {
            // SWAP A
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x37);
            gbCPU.A = 0x00;
            gbCPU.F = 0xF0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x00, gbCPU.A);
            Assert.AreEqual(0x80, gbCPU.F);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestSWAPB()
        {
            // SWAP B
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x30);
            gbCPU.B = 0xF0;
            gbCPU.F = 0x80;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x0F, gbCPU.B);
            Assert.AreEqual(0x00, gbCPU.F);
            Assert.AreEqual(8, cycle); // CB(4) + SWAP(8)
        }

        [Test]
        public void GameBoyTestSWAPC()
        {
            // SWAP C
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x31);
            gbCPU.C = 0xF0;
            gbCPU.F = 0x80;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x0F, gbCPU.C);
            Assert.AreEqual(0x00, gbCPU.F);
            Assert.AreEqual(8, cycle); // CB(4) + SWAP(8)
        }

        [Test]
        public void GameBoyTestSWAPD()
        {
            // SWAP D
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x32);
            gbCPU.D = 0xF0;
            gbCPU.F = 0x80;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x0F, gbCPU.D);
            Assert.AreEqual(0x00, gbCPU.F);
            Assert.AreEqual(8, cycle); // CB(4) + SWAP(8)
        }

        [Test]
        public void GameBoyTestSWAPE()
        {
            // SWAP E
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x33);
            gbCPU.E = 0xF0;
            gbCPU.F = 0x80;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x0F, gbCPU.E);
            Assert.AreEqual(0x00, gbCPU.F);
            Assert.AreEqual(8, cycle); // CB(4) + SWAP(8)
        }

        [Test]
        public void GameBoyTestSWAPH()
        {
            // SWAP H
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x34);
            gbCPU.H = 0xF0;
            gbCPU.F = 0x80;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x0F, gbCPU.H);
            Assert.AreEqual(0x00, gbCPU.F);
            Assert.AreEqual(8, cycle); // CB(4) + SWAP(8)
        }

        [Test]
        public void GameBoyTestSWAPL()
        {
            // SWAP L
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x35);
            gbCPU.L = 0xF0;
            gbCPU.F = 0x80;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x0F, gbCPU.L);
            Assert.AreEqual(0x00, gbCPU.F);
            Assert.AreEqual(8, cycle); // CB(4) + SWAP(8)
        }

        [Test]
        public void GameBoyTestSWAPHL()
        {
            // SWAP HL
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x36);
            gbMemory.WriteToMemory(2,0xF0);
            gbCPU.H = 0x00;
            gbCPU.L = 0x02;
            gbCPU.F = 0x80;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x0F, gbMemory.ReadFromMemory(0x0002));
            Assert.AreEqual(0x00, gbCPU.F);
            Assert.AreEqual(16, cycle);
        }

        [Test]
        public void GameBoyTestJP()
        {
            //Jump to 0x8000
            gbMemory.WriteToMemory(0,0xC3);
            gbMemory.WriteToMemory(1,0x00);
            gbMemory.WriteToMemory(2,0x80);
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x8000, gbCPU.PC);
            Assert.AreEqual(16, cycle);
        }

        [Test]
        public void GameBoyTestJPHL()
        {
            //Jump to 0x8000 via (HL)
            gbMemory.WriteToMemory(0,0xE9);
            gbCPU.H = 0x80;
            gbCPU.L = 0x00;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x8000, gbCPU.PC);
            Assert.AreEqual(4, cycle);
        }

        [Test]
        public void GameBoyTestJPZZ()
        {
            //Jump to 0x8000 when cc = Z and Z = 1
            gbMemory.WriteToMemory(0,0xCA);
            gbMemory.WriteToMemory(1,0x00);
            gbMemory.WriteToMemory(2,0x80);
            gbCPU.F = 0x80;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x8000, gbCPU.PC);
            Assert.AreEqual(16, cycle);

            //Skip CC = Z and Z != 1
            gbCPU.PC = 0x00;
            gbCPU.F = 0x00;
            cycle = gbCPU.Tick();
            Assert.AreEqual(0x03, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestJPNZ()
        {
            //Jump to 0x8000 when cc = NZ and Z = 0
            gbMemory.WriteToMemory(0,0xC2);
            gbMemory.WriteToMemory(1,0x00);
            gbMemory.WriteToMemory(2,0x80);
            gbCPU.F = 0x00;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x8000, gbCPU.PC);
            Assert.AreEqual(16, cycle);

            //Skip CC = NZ and Z = 1
            gbCPU.PC = 0x00;
            gbCPU.F = 0x80;
            cycle = gbCPU.Tick();
            Assert.AreEqual(0x03, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestJPNC()
        {
            //Jump to 0x8000 when cc = NC and C = 0
            gbMemory.WriteToMemory(0,0xD2);
            gbMemory.WriteToMemory(1,0x00);
            gbMemory.WriteToMemory(2,0x80);
            gbCPU.F = 0x00;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x8000, gbCPU.PC);
            Assert.AreEqual(16, cycle);

            //Skip CC = NC and C = 1
            gbCPU.PC = 0x00;
            gbCPU.F = 0x10;
            cycle = gbCPU.Tick();
            Assert.AreEqual(0x03, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestJPC()
        {
            //Jump to 0x8000 when cc = C and C = !
            gbMemory.WriteToMemory(0,0xDA);
            gbMemory.WriteToMemory(1,0x00);
            gbMemory.WriteToMemory(2,0x80);
            gbCPU.F = 0x10;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x8000, gbCPU.PC);
            Assert.AreEqual(16, cycle);

            //Skip CC = C and C = 0
            gbCPU.PC = 0x00;
            gbCPU.F = 0x00;
            cycle = gbCPU.Tick();
            Assert.AreEqual(0x03, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestLoadTestSeparateRegisters()
        {
            //Load 0x9FFF into HL
            gbMemory.WriteToMemory(0,0x21);
            gbMemory.WriteToMemory(1,0xFF);
            gbMemory.WriteToMemory(2,0x9F);
            gbCPU.H = 0x00;
            gbCPU.L = 0x00;
            gbCPU.Tick();
            Assert.AreEqual(0x9F, gbCPU.H);
            Assert.AreEqual(0xFF, gbCPU.L);

            //Load 0x0104 into DE
            gbCPU = new GameBoyCPU(gbMemory,gbInterrupts);
            gbMemory.WriteToMemory(0,0x11);
            gbMemory.WriteToMemory(1,0x04);
            gbMemory.WriteToMemory(2,0x01);
            gbCPU.D = 0x00;
            gbCPU.E = 0x00;
            gbCPU.Tick();
            Assert.AreEqual(0x01, gbCPU.D);
            Assert.AreEqual(0x04, gbCPU.E);
        }

        [Test]
        public void GameBoyTestXORA()
        {
            //XOR A=0xFF with A
            gbMemory.WriteToMemory(0,0xAF);
            gbCPU.A = 0xFF;
            gbCPU.F = 0x0;
            gbCPU.Tick();
            Assert.AreEqual(0x0, gbCPU.A);
            Assert.AreEqual(0x80, gbCPU.F);
        }

        [Test]
        public void GameBoyTestXORB()
        {
            //XOR A=0xFF with B
            gbMemory.WriteToMemory(0,0xA8);
            gbCPU.A = 0xFF;
            gbCPU.B = 0x0F;
            gbCPU.F = 0x0;
            gbCPU.Tick();
            Assert.AreEqual(0xF0, gbCPU.A);
            Assert.AreEqual(0x00, gbCPU.F);
        }

        [Test]
        public void GameBoyTestXORC()
        {
            //XOR A=0xFF with C
            gbMemory.WriteToMemory(0,0xA9);
            gbCPU.A = 0xFF;
            gbCPU.C = 0x0F;
            gbCPU.F = 0x0;
            gbCPU.Tick();
            Assert.AreEqual(0xF0, gbCPU.A);
            Assert.AreEqual(0x00, gbCPU.F);
        }

        [Test]
        public void GameBoyTestXORD()
        {
            //XOR A=0xFF with D
            gbMemory.WriteToMemory(0,0xAA);
            gbCPU.A = 0xFF;
            gbCPU.D = 0x0F;
            gbCPU.F = 0x0;
            gbCPU.Tick();
            Assert.AreEqual(0xF0, gbCPU.A);
            Assert.AreEqual(0x00, gbCPU.F);
        }

        [Test]
        public void GameBoyTestXORE()
        {
            //XOR A=0xFF with E
            gbMemory.WriteToMemory(0,0xAB);
            gbCPU.A = 0xFF;
            gbCPU.E = 0x0F;
            gbCPU.F = 0x0;
            gbCPU.Tick();
            Assert.AreEqual(0xF0, gbCPU.A);
            Assert.AreEqual(0x00, gbCPU.F);
        }

        [Test]
        public void GameBoyTestXORH()
        {
            //XOR A=0xFF with H
            gbMemory.WriteToMemory(0,0xAC);
            gbCPU.A = 0xFF;
            gbCPU.H = 0x0F;
            gbCPU.F = 0x0;
            gbCPU.Tick();
            Assert.AreEqual(0xF0, gbCPU.A);
            Assert.AreEqual(0x00, gbCPU.F);
        }

        [Test]
        public void GameBoyTestXORL()
        {
            //XOR A=0xFF with L
            gbMemory.WriteToMemory(0,0xAD);
            gbCPU.A = 0xFF;
            gbCPU.L = 0x0F;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xF0, gbCPU.A);
            Assert.AreEqual(0x00, gbCPU.F);
            Assert.AreEqual(4, cycle);
        }

        [Test]
        public void GameBoyTestXORn()
        {
            //XOR A=0xFF with n
            gbMemory.WriteToMemory(0,0xEE);
            gbMemory.WriteToMemory(1,0x0F);
            gbCPU.A = 0xFF;
            gbCPU.F = 0xF0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xF0, gbCPU.A);
            Assert.AreEqual(0x00, gbCPU.F);
            Assert.AreEqual(0x8, cycle);
        }

        [Test]
        public void GameBoyTestXORHL()
        {
            //XOR A=0xFF with (HL) = 0x8A
            gbMemory.WriteToMemory(0,0xAE);
            gbMemory.WriteToMemory(1,0x8A);
            gbCPU.A = 0xFF;
            gbCPU.H = 0x00;
            gbCPU.F = 0x80;
            gbCPU.L = 0x01;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x75, gbCPU.A);
            Assert.AreEqual(0x00, gbCPU.F);
            Assert.AreEqual(0x08, cycle);
        }

        [Test]
        public void GameBoyTestORA()
        {
            //OR A=0x5A with A
            gbMemory.WriteToMemory(0,0xB7);
            gbCPU.A = 0x5A;
            gbCPU.F = 0x80;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x5A, gbCPU.A);
            Assert.AreEqual(0x00, gbCPU.F);
            Assert.AreEqual(4, cycle);
        }

        [Test]
        public void GameBoyTestORD()
        {
            //OR A=0x5A with D
            gbMemory.WriteToMemory(0,0xB2);
            gbCPU.A = 0x5A;
            gbCPU.D = 0x03;
            gbCPU.F = 0xF0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x5B, gbCPU.A);
            Assert.AreEqual(0x00, gbCPU.F);
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(1, gbCPU.PC);
        }

        [Test]
        public void GameBoyTestORE()
        {
            //OR A=0x5A with E
            gbMemory.WriteToMemory(0,0xB3);
            gbCPU.A = 0x5A;
            gbCPU.E = 0x03;
            gbCPU.F = 0xF0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x5B, gbCPU.A);
            Assert.AreEqual(0x00, gbCPU.F);
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(1, gbCPU.PC);
        }

        [Test]
        public void GameBoyTestORH()
        {
            //OR A=0x5A with H
            gbMemory.WriteToMemory(0,0xB4);
            gbCPU.A = 0x5A;
            gbCPU.H = 0x03;
            gbCPU.F = 0xF0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x5B, gbCPU.A);
            Assert.AreEqual(0x00, gbCPU.F);
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(1, gbCPU.PC);
        }

        [Test]
        public void GameBoyTestORL()
        {
            //OR A=0x5A with L
            gbMemory.WriteToMemory(0,0xB5);
            gbCPU.A = 0x5A;
            gbCPU.L = 0x03;
            gbCPU.F = 0xF0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x5B, gbCPU.A);
            Assert.AreEqual(0x00, gbCPU.F);
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(1, gbCPU.PC);
        }

        [Test]
        public void GameBoyTestORd8()
        {
            //OR A=0x5A with 3
            gbMemory.WriteToMemory(0,0xF6);
            gbMemory.WriteToMemory(1,0x03);
            gbCPU.A = 0x5A;
            gbCPU.F = 0xF0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x5B, gbCPU.A);
            Assert.AreEqual(0x00, gbCPU.F);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestORHL()
        {
            //OR A with HL
            gbMemory.WriteToMemory(0,0xB6);
            gbMemory.WriteToMemory(1,0x0F);
            gbCPU.A = 0x5A;
            gbCPU.H = 0x00;
            gbCPU.L = 0x01;
            gbCPU.F = 0xF0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x5F, gbCPU.A);
            Assert.AreEqual(0x00, gbCPU.F);
            Assert.AreEqual(8, cycle);
        }

/*********************************************DEC TESTs*********************************************/
        [Test]
        public void GameBoyTestDECC()
        {
            //DEC C
            gbMemory.WriteToMemory(0, 0x0D);
            gbCPU.C = 0x01;
            gbCPU.F = 0x00;
            gbCPU.Tick();
            Assert.AreEqual(0x00, gbCPU.C);
            Assert.AreEqual(4, GameBoyCPU.ClockCycle);
            Assert.AreEqual(0xC0, gbCPU.F);
        }

        [Test]
        public void GameBoyTestDECL()
        {
            //DEC L
            gbMemory.WriteToMemory(0, 0x2D);
            gbCPU.L = 0x01;
            gbCPU.F = 0x00;
            gbCPU.Tick();
            Assert.AreEqual(0x00, gbCPU.C);
            Assert.AreEqual(4, GameBoyCPU.ClockCycle);
            Assert.AreEqual(0xC0, gbCPU.F);
        }

        [Test]
        public void GameBoyTestDECD()
        {
            //DEC D
            gbMemory.WriteToMemory(0, 0x15);
            gbCPU.D = 0x00;
            gbCPU.F = 0x00;
            gbCPU.Tick();
            Assert.AreEqual(0xFF, gbCPU.D);
            Assert.AreEqual(4, GameBoyCPU.ClockCycle);
            Assert.AreEqual(0x60, gbCPU.F);
        }

        [Test]
        public void GameBoyTestDECE()
        {
            //DEC E
            gbMemory.WriteToMemory(0, 0x1D);
            gbCPU.E = 0x00;
            gbCPU.F = 0x00;
            gbCPU.Tick();
            Assert.AreEqual(0xFF, gbCPU.E);
            Assert.AreEqual(4, GameBoyCPU.ClockCycle);
            Assert.AreEqual(0x60, gbCPU.F);
        }

        [Test]
        public void GameBoyTestDECB()
        {
            //DEC B
            gbMemory.WriteToMemory(0, 0x05);
            gbCPU.B = 0x01;
            gbCPU.F = 0x00;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x00, gbCPU.B);
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(0xC0, gbCPU.F);
        }

        [Test]
        public void GameBoyTestDECH()
        {
            //DEC H
            gbMemory.WriteToMemory(0, 0x25);
            gbCPU.H = 0x01;
            gbCPU.F = 0x00;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x00, gbCPU.B);
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(0xC0, gbCPU.F);
        }

        [Test]
        public void GameBoyTestDECA()
        {
            //DEC A
            GameBoyCPU.ClockCycle = 0;
            gbMemory.WriteToMemory(0, 0x3D);
            gbCPU.A = 0xFF;
            gbCPU.F = 0x00;
            gbCPU.Tick();
            Assert.AreEqual(0xFE, gbCPU.A);
            Assert.AreEqual(4, GameBoyCPU.ClockCycle);
            Assert.AreEqual(0x40, gbCPU.F);
        }

        [Test]
        public void GameBoyTestDECFlag()
        {
            //DEC C
            gbMemory = new GameBoyMemory(null);
            gbTimer = new GameBoyTimer(gbMemory,gbInterrupts);
            gbInterrupts = new GameBoyInterrupts(gbMemory);
            gbCPU = new GameBoyCPU(gbMemory,gbInterrupts);
            GameBoyCPU.ClockCycle = 0;
            gbMemory.WriteToMemory(0, 0x35);
            gbMemory.WriteToMemory(0x0001, 0x00);
            gbCPU.PC = 0x0000;
            gbCPU.H = 0x00;
            gbCPU.L = 0x01;
            gbCPU.F = 0x00;
            gbCPU.Tick();
            Assert.AreEqual(0x0001, gbCPU.PC);
            Assert.AreEqual(0xFF, gbMemory.ReadFromMemory(0x0001)); //HL
            Assert.AreEqual(12, GameBoyCPU.ClockCycle);
            Assert.AreEqual(0x60, gbCPU.F);
        }

        [Test]
        public void GameBoyTestLDDHLADEC()
        {
            gbMemory.WriteToMemory(0,0x32);
            gbCPU.A = 0x00;
            gbCPU.H = 0x9F;
            gbCPU.L = 0xFF;
            gbCPU.Tick();
            Assert.AreEqual(0x9F, gbCPU.H);
            Assert.AreEqual(0xFE, gbCPU.L);
            Assert.AreEqual(0, gbMemory.ReadFromMemory(0x9FFF));
        }

        [Test]
        public void GameBoyTesDECBC()
        {
            gbMemory.WriteToMemory(0,0x0B);
            gbCPU.B = 0x23;
            gbCPU.C = 0x5F;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x23, gbCPU.B);
            Assert.AreEqual(0x5E, gbCPU.C);
            Assert.AreEqual(8,cycle);
        }

        [Test]
        public void GameBoyTesDECDE()
        {
            gbMemory.WriteToMemory(0,0x1B);
            gbCPU.D = 0x23;
            gbCPU.E = 0x5F;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x23, gbCPU.D);
            Assert.AreEqual(0x5E, gbCPU.E);
            Assert.AreEqual(8,cycle);
        }

        [Test]
        public void GameBoyTesDECHL()
        {
            gbMemory.WriteToMemory(0,0x2B);
            gbCPU.H = 0x23;
            gbCPU.L = 0x5F;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x23, gbCPU.H);
            Assert.AreEqual(0x5E, gbCPU.L);
            Assert.AreEqual(8,cycle);
            Assert.AreEqual(1,gbCPU.PC);
        }

        [Test]
        public void GameBoyTesDECSP()
        {
            gbMemory.WriteToMemory(0,0x3B);
            gbCPU.SP = 0x235F;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x235E, gbCPU.SP);
            Assert.AreEqual(8,cycle);
            Assert.AreEqual(1,gbCPU.PC);
        }

        [Test]
        public void GameBoyTestLDDHLDEight()
        {
            gbMemory.WriteToMemory(0,0x36);
            gbMemory.WriteToMemory(1,0x77);
            gbCPU.H = 0x9F;
            gbCPU.L = 0xFF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x9F, gbCPU.H);
            Assert.AreEqual(0xFF, gbCPU.L);
            Assert.AreEqual(0x77, gbMemory.ReadFromMemory(0x9FFF));
            Assert.AreEqual(12,cycle);
        }

        [Test]
        public void GameBoyTestLDDHLAINC()
        {
            gbMemory.WriteToMemory(0,0x22);
            gbCPU.A = 0x56;
            gbCPU.H = 0xFF;
            gbCPU.L = 0xFF;
            gbCPU.Tick();
            Assert.AreEqual(0x00, gbCPU.H);
            Assert.AreEqual(0x00, gbCPU.L);
            Assert.AreEqual(0x56, gbMemory.ReadFromMemory(0xFFFF));
        }

        [Test]
        public void GameBoyTestADDHL()
        {
            //Add (HL) to A
            gbMemory.WriteToMemory(0,0x86);
            gbMemory.WriteToMemory(1,0x12);
            gbCPU.A = 0x3C;
            gbCPU.H = 0x00;
            gbCPU.L = 0x01;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x4E, gbCPU.A);
            Assert.AreEqual(0x00, gbCPU.H);
            Assert.AreEqual(0x01, gbCPU.L);
            Assert.AreEqual(0x00, gbCPU.F);
            Assert.AreEqual(8, cycle);
            Assert.AreEqual(1, gbCPU.PC);
        }

        [Test]
        public void GameBoyTestADDHLSP()
        {
            //Add SP to HL
            gbMemory.WriteToMemory(0,0x39);
            gbCPU.SP = 0xFFEE;
            gbCPU.H = 0x00;
            gbCPU.L = 0x00;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xFF, gbCPU.H);
            Assert.AreEqual(0xEE, gbCPU.L);
            Assert.AreEqual(0x00, gbCPU.F);
            Assert.AreEqual(8, cycle);
            Assert.AreEqual(1, gbCPU.PC);
        }

        [Test]
        public void GameBoyTestADDHLSPN()
        {
            //Add signed byte + SP to HL
            gbMemory.WriteToMemory(0,0xF8);
            gbMemory.WriteToMemory(1,0x02);
            gbCPU.SP = 0xFFF8;
            gbCPU.F = 0xA0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xFFF8, gbCPU.SP);
            Assert.AreEqual(0xFF, gbCPU.H);
            Assert.AreEqual(0xFA, gbCPU.L);
            Assert.AreEqual(0x00, gbCPU.F);
            Assert.AreEqual(12, cycle);
            Assert.AreEqual(2, gbCPU.PC);
        }

        [Test]
        public void GameBoyTestADDHLSPNSigned()
        {
            //Add signed byte 2 + SP to HL
            gbMemory.WriteToMemory(0,0xF8);
            gbMemory.WriteToMemory(1,0xFE); // -2
            gbCPU.SP = 0xFFF8;
            gbCPU.F = 0xA0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xFFF8, gbCPU.SP);
            Assert.AreEqual(0xFF, gbCPU.H);
            Assert.AreEqual(0xF6, gbCPU.L);
            Assert.AreEqual(0x30, gbCPU.F);
            Assert.AreEqual(12, cycle);
            Assert.AreEqual(2, gbCPU.PC);
        }

        [Test]
        public void GameBoyTestADDSPN()
        {
            //Add signed byte to SP
            gbMemory.WriteToMemory(0,0xE8);
            gbMemory.WriteToMemory(1,0x02);
            gbCPU.SP = 0xFFF8;
            gbCPU.F = 0xA0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xFFFA, gbCPU.SP);
            Assert.AreEqual(0x00, gbCPU.F);
            Assert.AreEqual(16, cycle);
            Assert.AreEqual(2, gbCPU.PC);
        }

        [Test]
        public void GameBoyTestADDSPNSigned()
        {
            //Add signed byte to SP
            gbMemory.WriteToMemory(0,0xE8);
            gbMemory.WriteToMemory(1,0xFE); // -2
            gbCPU.SP = 0xFFF8;
            gbCPU.F = 0xA0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xFFF6, gbCPU.SP);
            Assert.AreEqual(0x30, gbCPU.F);
            Assert.AreEqual(16, cycle);
            Assert.AreEqual(2, gbCPU.PC);
        }

        //https://stackoverflow.com/questions/57958631/game-boy-half-carry-flag-and-16-bit-instructions-especially-opcode-0xe8
        [Test]
        public void GameBoyTestADDSPNSignedTwo()
        {
            //Add signed byte to SP
            gbMemory.WriteToMemory(0,0xE8);
            gbMemory.WriteToMemory(1,0x10); //
            gbCPU.SP = 0x0FF0;
            gbCPU.F = 0x00;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x1000, gbCPU.SP);
            Assert.AreEqual(0x10, gbCPU.F);
            Assert.AreEqual(16, cycle);
            Assert.AreEqual(2, gbCPU.PC);
        }

        [Test]
        public void GameBoyTestADDn()
        {
            //Add n to A
            gbMemory.WriteToMemory(0,0xC6);
            gbMemory.WriteToMemory(1,0xFF);
            gbCPU.A = 0x3C;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x3B, gbCPU.A);
            Assert.AreEqual(0x30, gbCPU.F);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestADDA()
        {
            //Add A to A
            gbMemory.WriteToMemory(0,0x87);
            gbCPU.A = 0x3A;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x74, gbCPU.A);
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(1, gbCPU.PC);
        }

        [Test]
        public void GameBoyTestADDB()
        {
            //Add B to A
            gbMemory.WriteToMemory(0,0x80);
            gbCPU.A = 0x3A;
            gbCPU.B = 0xC6;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x00, gbCPU.A);
            Assert.AreEqual(0xB0, gbCPU.F);
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(1, gbCPU.PC);
        }

        [Test]
        public void GameBoyTestADDC()
        {
            //Add C to A
            gbMemory.WriteToMemory(0,0x81);
            gbCPU.A = 0x3A;
            gbCPU.C = 0xC6;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x00, gbCPU.A);
            Assert.AreEqual(0xB0, gbCPU.F);
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(1, gbCPU.PC);
        }

        [Test]
        public void GameBoyTestADDD()
        {
            //Add D to A
            gbMemory.WriteToMemory(0,0x82);
            gbCPU.A = 0x3A;
            gbCPU.D = 0xC6;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x00, gbCPU.A);
            Assert.AreEqual(0xB0, gbCPU.F);
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(1, gbCPU.PC);
        }

        [Test]
        public void GameBoyTestADDE()
        {
            //Add E to A
            gbMemory.WriteToMemory(0,0x83);
            gbCPU.A = 0x3A;
            gbCPU.E = 0xC6;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x00, gbCPU.A);
            Assert.AreEqual(0xB0, gbCPU.F);
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(1, gbCPU.PC);
        }

        [Test]
        public void GameBoyTestADDH()
        {
            //Add H to A
            gbMemory.WriteToMemory(0,0x84);
            gbCPU.A = 0x3A;
            gbCPU.H = 0xC6;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x00, gbCPU.A);
            Assert.AreEqual(0xB0, gbCPU.F);
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(1, gbCPU.PC);
        }

        [Test]
        public void GameBoyTestADDL()
        {
            //Add L to A
            gbMemory.WriteToMemory(0,0x85);
            gbCPU.A = 0x3A;
            gbCPU.L = 0xC6;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x00, gbCPU.A);
            Assert.AreEqual(0xB0, gbCPU.F);
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(1, gbCPU.PC);
        }

        [Test]
        public void GameBoyTestADDHLBC()
        {
            //Add BC to HL
            gbMemory.WriteToMemory(0,0x09);
            gbCPU.B = 0x06;
            gbCPU.C = 0x05;
            gbCPU.H = 0x8A;
            gbCPU.L = 0x23;
            gbCPU.F = 0x00;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x90, gbCPU.H);
            Assert.AreEqual(0x28, gbCPU.L);
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(8, cycle);
            Assert.AreEqual(1, gbCPU.PC);
        }
        
        [Test]
        public void GameBoyTestADDHLDE()
        {
            //Add DE to HL
            gbMemory.WriteToMemory(0,0x19);
            gbCPU.D = 0x06;
            gbCPU.E = 0x05;
            gbCPU.H = 0x8A;
            gbCPU.L = 0x23;
            gbCPU.F = 0x00;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x90, gbCPU.H);
            Assert.AreEqual(0x28, gbCPU.L);
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestADDHLHL()
        {
            //Add HL to HL
            gbMemory.WriteToMemory(0,0x29);
            gbCPU.H = 0x8A;
            gbCPU.L = 0x23;
            gbCPU.F = 0x40;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x14, gbCPU.H);
            Assert.AreEqual(0x46, gbCPU.L);
            Assert.AreEqual(0x30, gbCPU.F);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestLDDHLA()
        {
            gbMemory.WriteToMemory(0,0x77);
            gbCPU.A = 0x22;
            gbCPU.H = 0xFF;
            gbCPU.L = 0x25;
            gbCPU.Tick();
            Assert.AreEqual(0xFF, gbCPU.H);
            Assert.AreEqual(0x25, gbCPU.L);
            Assert.AreEqual(gbCPU.A, gbMemory.ReadFromMemory(0xFF25));
        }


/*********************************************** BIT TEST ***************************************************************/

        [Test]
        public void GameBoyTestBit0B()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x40);
            gbCPU.B = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit0C()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x41);
            gbCPU.C = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit0D()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x42);
            gbCPU.D = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit0E()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x43);
            gbCPU.E = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit0H()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x44);
            gbCPU.H = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit0L()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x45);
            gbCPU.L = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit0A()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x47);
            gbCPU.A = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit1B()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x48);
            gbCPU.B = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit1C()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x49);
            gbCPU.C = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit1D()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x4A);
            gbCPU.D = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit1E()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x4B);
            gbCPU.E = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit1H()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x4C);
            gbCPU.H = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit1L()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x4D);
            gbCPU.L = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit1A()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x4F);
            gbCPU.A = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit2B()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x50);
            gbCPU.B = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit2C()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x51);
            gbCPU.C = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit2D()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x52);
            gbCPU.D = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit2E()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x53);
            gbCPU.E = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit2H()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x54);
            gbCPU.H = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit2L()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x55);
            gbCPU.L = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit2A()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x57);
            gbCPU.A = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit3B()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x58);
            gbCPU.B = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit3C()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x59);
            gbCPU.C = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit3D()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x5A);
            gbCPU.D = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit3E()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x5B);
            gbCPU.E = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit3H()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x5C);
            gbCPU.H = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit3L()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x5D);
            gbCPU.L = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit3A()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x5F);
            gbCPU.A = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit4B()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x60);
            gbCPU.B = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xA0, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit4C()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x61);
            gbCPU.C = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xA0, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit4D()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x62);
            gbCPU.D = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xA0, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit4E()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x63);
            gbCPU.E = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xA0, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit4H()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x64);
            gbCPU.H = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xA0, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit4L()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x65);
            gbCPU.L = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xA0, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit4A()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x67);
            gbCPU.A = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xA0, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit5B()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x68);
            gbCPU.B = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit5C()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x69);
            gbCPU.C = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit5D()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x6A);
            gbCPU.D = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit5E()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x6B);
            gbCPU.E = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit5H()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x6C);
            gbCPU.H = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit5L()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x6D);
            gbCPU.L = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit5A()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x6F);
            gbCPU.A = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit6B()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x70);
            gbCPU.B = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit6C()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x71);
            gbCPU.C = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit6D()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x72);
            gbCPU.D = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit6E()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x73);
            gbCPU.E = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit6H()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x74);
            gbCPU.H = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit6L()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x75);
            gbCPU.L = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit6A()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x77);
            gbCPU.A = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit7B()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x78);
            gbCPU.B = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit7C()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x79);
            gbCPU.C = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit7D()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x7A);
            gbCPU.D = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit7E()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x7B);
            gbCPU.E = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit7H()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x7C);
            gbCPU.H = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit7L()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x7D);
            gbCPU.L = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestBit7A()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x7F);
            gbCPU.A = 0xEF;
            gbCPU.F = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x20, gbCPU.F);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

/*********************************************** RES TEST ***************************************************************/
        [Test]
        public void GameBoyTestRES0B()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x80);
            gbCPU.B = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xEE,gbCPU.B);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES0C()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x81);
            gbCPU.C = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xEE,gbCPU.C);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES0D()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x82);
            gbCPU.D = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xEE,gbCPU.D);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES0E()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x83);
            gbCPU.E = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xEE,gbCPU.E);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES0H()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x84);
            gbCPU.H = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xEE,gbCPU.H);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES0L()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x85);
            gbCPU.L = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xEE,gbCPU.L);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES0A()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x87);
            gbCPU.A = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xEE,gbCPU.A);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES1B()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x88);
            gbCPU.B = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xED, gbCPU.B);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES1C()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x89);
            gbCPU.C = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xED, gbCPU.C);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES1D()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x8A);
            gbCPU.D = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xED, gbCPU.D);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES1E()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x8B);
            gbCPU.E = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xED, gbCPU.E);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES1H()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x8C);
            gbCPU.H = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xED, gbCPU.H);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES1L()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x8D);
            gbCPU.L = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xED, gbCPU.L);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES1A()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x8F);
            gbCPU.A = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xED, gbCPU.A);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES2B()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x90);
            gbCPU.B = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xEB, gbCPU.B);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES2C()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x91);
            gbCPU.C = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xEB, gbCPU.C);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES2D()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x92);
            gbCPU.D = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xEB, gbCPU.D);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES2E()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x93);
            gbCPU.E = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xEB, gbCPU.E);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES2H()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x94);
            gbCPU.H = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xEB, gbCPU.H);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES2L()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x95);
            gbCPU.L = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xEB, gbCPU.L);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES2A()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x97);
            gbCPU.A = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xEB, gbCPU.A);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES3B()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x98);
            gbCPU.B = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xE7, gbCPU.B);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES3C()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x99);
            gbCPU.C = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xE7, gbCPU.C);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES3D()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x9A);
            gbCPU.D = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xE7, gbCPU.D);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES3E()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x9B);
            gbCPU.E = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xE7, gbCPU.E);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES3H()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x9C);
            gbCPU.H = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xE7, gbCPU.H);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES3L()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x9D);
            gbCPU.L = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xE7, gbCPU.L);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES3A()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x9F);
            gbCPU.A = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xE7, gbCPU.A);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES4B()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0xA0);
            gbCPU.B = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xEF, gbCPU.B);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES4C()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0xA1);
            gbCPU.C = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xEF, gbCPU.C);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES4D()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0xA2);
            gbCPU.D = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xEF, gbCPU.D);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES4E()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0xA3);
            gbCPU.E = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xEF, gbCPU.E);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES4H()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0xA4);
            gbCPU.H = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xEF, gbCPU.H);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES4L()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0xA5);
            gbCPU.L = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xEF, gbCPU.L);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES4A()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0xA7);
            gbCPU.A = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xEF, gbCPU.A);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES5B()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0xA8);
            gbCPU.B = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xCF, gbCPU.B);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES5C()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0xA9);
            gbCPU.C = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xCF, gbCPU.C);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES5D()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0xAA);
            gbCPU.D = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xCF, gbCPU.D);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES5E()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0xAB);
            gbCPU.E = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xCF, gbCPU.E);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES5H()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0xAC);
            gbCPU.H = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xCF, gbCPU.H);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES5L()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0xAD);
            gbCPU.L = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xCF, gbCPU.L);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES5A()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0xAF);
            gbCPU.A = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xCF, gbCPU.A);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES6B()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0xB0);
            gbCPU.B = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xAF, gbCPU.B);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES6C()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0xB1);
            gbCPU.C = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xAF, gbCPU.C);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES6D()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0xB2);
            gbCPU.D = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xAF, gbCPU.D);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES6E()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0xB3);
            gbCPU.E = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xAF, gbCPU.E);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES6H()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0xB4);
            gbCPU.H = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xAF, gbCPU.H);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES6L()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0xB5);
            gbCPU.L = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xAF, gbCPU.L);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES6A()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0xB7);
            gbCPU.A = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0xAF, gbCPU.A);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES7B()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0xB8);
            gbCPU.B = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x6F, gbCPU.B);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES7C()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0xB9);
            gbCPU.C = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x6F, gbCPU.C);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES7D()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0xBA);
            gbCPU.D = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x6F, gbCPU.D);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES7E()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0xBB);
            gbCPU.E = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x6F, gbCPU.E);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES7H()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0xBC);
            gbCPU.H = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x6F, gbCPU.H);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES7L()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0xBD);
            gbCPU.L = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x6F, gbCPU.L);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestRES7A()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0xBF);
            gbCPU.A = 0xEF;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x6F, gbCPU.A);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

/*********************************************** SET TEST ***************************************************************/
        [Test]
        public void GameBoyTestSET0B()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0xC0);
            gbCPU.B = 0x10;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x11, gbCPU.B);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestSET0C()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0xC1);
            gbCPU.C = 0x10;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x11, gbCPU.C);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestSET0D()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0xC2);
            gbCPU.D = 0x10;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x11, gbCPU.D);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestSET0E()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0xC3);
            gbCPU.E = 0x10;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x11, gbCPU.E);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestSET0H()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0xC4);
            gbCPU.H = 0x10;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x11, gbCPU.H);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestSET0L()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0xC5);
            gbCPU.L = 0x10;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x11, gbCPU.L);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestSET0A()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0xC7);
            gbCPU.A = 0x10;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x11, gbCPU.A);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestSET1B()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0xC8);
            gbCPU.B = 0x10;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x12, gbCPU.B);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestSET1C()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0xC9);
            gbCPU.C = 0x10;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x12, gbCPU.C);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestSET1D()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0xCA);
            gbCPU.D = 0x10;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x12, gbCPU.D);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestSET1E()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0xCB);
            gbCPU.E = 0x10;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x12, gbCPU.E);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestSET1H()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0xCC);
            gbCPU.H = 0x10;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x12, gbCPU.H);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestSET1L()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0xCD);
            gbCPU.L = 0x10;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x12, gbCPU.L);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestSET1A()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0xCF);
            gbCPU.A = 0x10;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x12, gbCPU.A);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(8, cycle);
        }

/****************************** Jumps ****************************************/

        [Test]
        public void GameBoyTestJRCCNZ()
        {
            gbMemory.WriteToMemory(0,0x20);
            gbMemory.WriteToMemory(1,0xFE); // -2 jump back memory[0]
            gbCPU.F = 0x00;
            gbCPU.Tick();
            Assert.AreEqual(0x0, gbCPU.PC);
            Assert.AreEqual(gbMemory.ReadFromMemory(0), gbMemory.ReadFromMemory(gbCPU.PC));

            gbCPU.F = 0x80;
            gbMemory.WriteToMemory(2,0xAA);
            gbCPU.Tick();
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(gbMemory.ReadFromMemory(2), gbMemory.ReadFromMemory(gbCPU.PC));
        }

        [Test]
        public void GameBoyTestJRCCZ()
        {
            gbMemory.WriteToMemory(0,0x28);
            gbMemory.WriteToMemory(1,0xFE); // -2 jump back memory[0]
            gbCPU.F = 0x80;
            gbCPU.Tick();
            Assert.AreEqual(0x0, gbCPU.PC);
            Assert.AreEqual(gbMemory.ReadFromMemory(0), gbMemory.ReadFromMemory(gbCPU.PC));

            gbCPU.F = 0x00;
            gbMemory.WriteToMemory(2,0xAA);
            gbCPU.Tick();
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(gbMemory.ReadFromMemory(2), gbMemory.ReadFromMemory(gbCPU.PC));
        }

        [Test]
        public void GameBoyTestJRCCNC()
        {
            gbMemory.WriteToMemory(0,0x30);
            gbMemory.WriteToMemory(1,0xFE); // -2 jump back memory[0]
            gbCPU.F = 0x20; // CY = 0 should jump
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x0, gbCPU.PC);
            Assert.AreEqual(gbMemory.ReadFromMemory(0), gbMemory.ReadFromMemory(gbCPU.PC));
            Assert.AreEqual(8, cycle);
            Assert.AreEqual(0x20, gbCPU.F);

            gbCPU.F = 0x10; //set CY = 1 should not jump
            gbMemory.WriteToMemory(2,0x00); // Move to no-op
            cycle = gbCPU.Tick();
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(gbMemory.ReadFromMemory(2), gbMemory.ReadFromMemory(gbCPU.PC));
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestJRCCC()
        {
            gbMemory.WriteToMemory(0,0x38);
            gbMemory.WriteToMemory(1,0xFE); // -2 jump back memory[0]
            gbCPU.F = 0x10; // CY = 1 should jump
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x0, gbCPU.PC);
            Assert.AreEqual(gbMemory.ReadFromMemory(0), gbMemory.ReadFromMemory(gbCPU.PC));
            Assert.AreEqual(8, cycle);
            Assert.AreEqual(0x10, gbCPU.F);

            gbCPU.F = 0x00; //set CY = 0 should not jump
            gbMemory.WriteToMemory(2,0x00); 
            cycle = gbCPU.Tick();  // Move to no-op
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(gbMemory.ReadFromMemory(2), gbMemory.ReadFromMemory(gbCPU.PC));
            Assert.AreEqual(8, cycle);
            Assert.AreEqual(0x00, gbCPU.F);
        }

        [Test]
        public void GameBoyTestInterruptsRST()
        {
            gbMemory.WriteToMemory(0x40,0x00);
            gbMemory.WriteToMemory(GameBoyInterrupts.IE,0x01);
            gbMemory.WriteToMemory(GameBoyInterrupts.IF,0x01);
            Assert.AreEqual(gbMemory.ReadFromMemory(GameBoyInterrupts.IE), 0x01);
            Assert.AreEqual(gbMemory.ReadFromMemory(GameBoyInterrupts.IF), 0x01);
            GameBoyInterrupts.IMEHold = true;
            gbCPU.Tick();
            Assert.AreEqual(0x41, gbCPU.PC);
            Assert.AreEqual(false, GameBoyInterrupts.IMEHold);
            Assert.AreEqual(false, GameBoyInterrupts.IMEFlag);
            Assert.AreEqual(0x01,gbMemory.ReadFromMemory(GameBoyInterrupts.IE));
            Assert.AreEqual(0x00,gbMemory.ReadFromMemory(GameBoyInterrupts.IF));
        }

        [Test]
        public void GameBoyTestRST0x28()
        {
            gbMemory.WriteToMemory(0x7FFF,0xEF);
            gbCPU.PC = 0x7FFF;
            gbCPU.SP = 0xFFFE;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x28, gbCPU.PC);
            Assert.AreEqual(0xFFFC, gbCPU.SP);
            Assert.AreEqual(0x00,gbMemory.ReadFromMemory((ushort)(gbCPU.SP)));
            Assert.AreEqual(0x80,gbMemory.ReadFromMemory((ushort)(gbCPU.SP + 1)));
            Assert.AreEqual(16,cycle);
        }

        [Test]
        public void GameBoyTestRST0x00()
        {
            gbMemory.WriteToMemory(0x7FFF,0xC7);
            gbCPU.PC = 0x7FFF;
            gbCPU.SP = 0xFFFE;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x00, gbCPU.PC);
            Assert.AreEqual(0xFFFC, gbCPU.SP);
            Assert.AreEqual(0x00,gbMemory.ReadFromMemory((ushort)(gbCPU.SP)));
            Assert.AreEqual(0x80,gbMemory.ReadFromMemory((ushort)(gbCPU.SP + 1)));
            Assert.AreEqual(16,cycle);
        }

        [Test]
        public void GameBoyTestRST0x08()
        {
            gbMemory.WriteToMemory(0x7FFF,0xCF);
            gbCPU.PC = 0x7FFF;
            gbCPU.SP = 0xFFFE;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x08, gbCPU.PC);
            Assert.AreEqual(0xFFFC, gbCPU.SP);
            Assert.AreEqual(0x00,gbMemory.ReadFromMemory((ushort)(gbCPU.SP)));
            Assert.AreEqual(0x80,gbMemory.ReadFromMemory((ushort)(gbCPU.SP + 1)));
            Assert.AreEqual(16,cycle);
        }

        [Test]
        public void GameBoyTestRST0x10()
        {
            gbMemory.WriteToMemory(0x7FFF,0xD7);
            gbCPU.PC = 0x7FFF;
            gbCPU.SP = 0xFFFE;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x10, gbCPU.PC);
            Assert.AreEqual(0xFFFC, gbCPU.SP);
            Assert.AreEqual(0x00,gbMemory.ReadFromMemory((ushort)(gbCPU.SP)));
            Assert.AreEqual(0x80,gbMemory.ReadFromMemory((ushort)(gbCPU.SP + 1)));
            Assert.AreEqual(16,cycle);
        }

        [Test]
        public void GameBoyTestRST0x18()
        {
            gbMemory.WriteToMemory(0x7FFF,0xDF);
            gbCPU.PC = 0x7FFF;
            gbCPU.SP = 0xFFFE;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x18, gbCPU.PC);
            Assert.AreEqual(0xFFFC, gbCPU.SP);
            Assert.AreEqual(0x00,gbMemory.ReadFromMemory((ushort)(gbCPU.SP)));
            Assert.AreEqual(0x80,gbMemory.ReadFromMemory((ushort)(gbCPU.SP + 1)));
            Assert.AreEqual(16,cycle);
        }

        [Test]
        public void GameBoyTestRST0x20()
        {
            gbMemory.WriteToMemory(0x7FFF,0xE7);
            gbCPU.PC = 0x7FFF;
            gbCPU.SP = 0xFFFE;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x20, gbCPU.PC);
            Assert.AreEqual(0xFFFC, gbCPU.SP);
            Assert.AreEqual(0x00,gbMemory.ReadFromMemory((ushort)(gbCPU.SP)));
            Assert.AreEqual(0x80,gbMemory.ReadFromMemory((ushort)(gbCPU.SP + 1)));
            Assert.AreEqual(16,cycle);
        }

        [Test]
        public void GameBoyTestRST0x30()
        {
            gbMemory.WriteToMemory(0x7FFF,0xF7);
            gbCPU.PC = 0x7FFF;
            gbCPU.SP = 0xFFFE;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x30, gbCPU.PC);
            Assert.AreEqual(0xFFFC, gbCPU.SP);
            Assert.AreEqual(0x00,gbMemory.ReadFromMemory((ushort)(gbCPU.SP)));
            Assert.AreEqual(0x80,gbMemory.ReadFromMemory((ushort)(gbCPU.SP + 1)));
            Assert.AreEqual(16,cycle);
        }

        [Test]
        public void GameBoyTestRST0x38()
        {
            gbMemory.WriteToMemory(0x7FFF,0xFF);
            gbCPU.PC = 0x7FFF;
            gbCPU.SP = 0xFFFE;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x38, gbCPU.PC);
            Assert.AreEqual(0xFFFC, gbCPU.SP);
            Assert.AreEqual(0x00,gbMemory.ReadFromMemory((ushort)(gbCPU.SP)));
            Assert.AreEqual(0x80,gbMemory.ReadFromMemory((ushort)(gbCPU.SP + 1)));
            Assert.AreEqual(16,cycle);
        }

        [Test]
        public void GameBoyTestJR()
        {
            // JR would always jump if opcode is 0x18 regardless of F value
            gbMemory.WriteToMemory(0,0x18);
            gbMemory.WriteToMemory(1,0xFE); // -2 jump back memory[0]
            gbCPU.F = 0x80;
            gbCPU.Tick();
            Assert.AreEqual(0x0, gbCPU.PC);
            Assert.AreEqual(gbMemory.ReadFromMemory(0), gbMemory.ReadFromMemory(gbCPU.PC));
            Assert.AreEqual(12, GameBoyCPU.ClockCycle);

            gbCPU.F = 0x00;
            gbCPU.Tick();
            Assert.AreEqual(0x0, gbCPU.PC);
            Assert.AreEqual(gbMemory.ReadFromMemory(0), gbMemory.ReadFromMemory(gbCPU.PC));
            Assert.AreEqual(24, GameBoyCPU.ClockCycle);
        }

        [Test]
        public void GameBoyTestLDCR()
        {
            //Load 0x11 into C
            gbMemory.WriteToMemory(0, 0xE2);
            gbCPU.C = 0x11;
            gbCPU.A = 0x80;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(gbMemory.ReadFromMemory((ushort)(0xFF00+gbCPU.C)), gbCPU.A);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestINCD()
        {
            //INC D
            gbMemory.WriteToMemory(0, 0x14);
            gbCPU.D = 0xFF;
            gbCPU.F = 0x00;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x00, gbCPU.D);
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(0xA0, gbCPU.F);
        }

        [Test]
        public void GameBoyTestINCL()
        {
            //INC L
            gbMemory.WriteToMemory(0, 0x2C);
            gbCPU.L = 0xFF;
            gbCPU.F = 0x00;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x00, gbCPU.L);
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(0xA0, gbCPU.F);
        }

        [Test]
        public void GameBoyTestINCA()
        {
            //INC A
            gbMemory.WriteToMemory(0, 0x3C);
            gbCPU.A = 0x0F;
            gbCPU.F = 0x00;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x10, gbCPU.A);
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(0x20, gbCPU.F);
        }

        [Test]
        public void GameBoyTestINCC()
        {
            //INC C
            gbMemory.WriteToMemory(0, 0x0C);
            gbCPU.C = 0xFF;
            gbCPU.F = 0x00;
            gbCPU.Tick();
            Assert.AreEqual(0x00, gbCPU.C);
            Assert.AreEqual(4, GameBoyCPU.ClockCycle);
            Assert.AreEqual(0xA0, gbCPU.F);
        }

        [Test]
        public void GameBoyTestINCB()
        {
            //INC B
            gbMemory.WriteToMemory(0, 0x04);
            gbCPU.B = 0xFE;
            gbCPU.F = 0x00;
            gbCPU.Tick();
            Assert.AreEqual(0xFF, gbCPU.B);
            Assert.AreEqual(4, GameBoyCPU.ClockCycle);
            Assert.AreEqual(0x00, gbCPU.F);
        }

        [Test]
        public void GameBoyTestINCHLMemory()
        {
            //INC (HL)
            gbMemory.WriteToMemory(0, 0x34);
            gbMemory.WriteToMemory(1, 0x50);
            gbCPU.H = 0x00;
            gbCPU.L = 0x01;
            gbCPU.F = 0xE0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x51, gbMemory.ReadFromMemory(1));
            Assert.AreEqual(8, cycle);
            Assert.AreEqual(0x00, gbCPU.F);
        }

        [Test]
        public void GameBoyTestINCH()
        {
            //INC H
            gbMemory.WriteToMemory(0, 0x24);
            gbCPU.H = 0x00;
            gbCPU.F = 0x00;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x01, gbCPU.H);
            Assert.AreEqual(4, GameBoyCPU.ClockCycle); // Total tick cycles
            Assert.AreEqual(4, cycle); // Tick Cycle
            Assert.AreEqual(0x00, gbCPU.F);
        }

        [Test]
        public void GameBoyTestINCE()
        {
            //INC E
            gbMemory.WriteToMemory(0, 0x1C);
            gbCPU.E = 0x50;
            gbCPU.F = 0xFF;
            gbCPU.Tick();
            Assert.AreEqual(0x51, gbCPU.E);
            Assert.AreEqual(4, GameBoyCPU.ClockCycle);
            Assert.AreEqual(0x10, gbCPU.F);
        }

        [Test]
        public void GameBoyTestINCHL()
        {
            //INC HL - page 97
            gbMemory.WriteToMemory(0, 0x23);
            gbCPU.H = 0x23;
            gbCPU.L = 0x5F;
            gbCPU.F = 0x00;
            gbCPU.Tick();
            Assert.AreEqual(0x23, gbCPU.H);
            Assert.AreEqual(0x60, gbCPU.L);
            Assert.AreEqual(8, GameBoyCPU.ClockCycle);
            Assert.AreEqual(0x00, gbCPU.F);
        }

        [Test]
        public void GameBoyTestINCSP()
        {
            //INC SP - page 97
            gbMemory.WriteToMemory(0, 0x33);
            gbCPU.SP = 0x235F;
            gbCPU.F = 0x00;
            gbCPU.Tick();
            Assert.AreEqual(0x2360, gbCPU.SP);
            Assert.AreEqual(8, GameBoyCPU.ClockCycle);
            Assert.AreEqual(0x00, gbCPU.F);
        }
/*************************************** CP TEST ****************************************************/

        [Test]
        public void GameBoyTestCPA()
        {
            //CP Compare - page 95 compare
            Assert.True(gbMemory.WriteToMemory(0, 0xBF));
            gbCPU.A = 0x2F;
            gbCPU.F = 0x00;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x01, gbCPU.PC);
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(0xC0, gbCPU.F);
        }

        [Test]
        public void GameBoyTestCPB()
        {
            //CP Compare - page 95 compare
            Assert.True(gbMemory.WriteToMemory(0, 0xB8));
            gbCPU.A = 0x3C;
            gbCPU.B = 0x2F;
            gbCPU.F = 0x00;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x01, gbCPU.PC);
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(0x60, gbCPU.F);
        }

        [Test]
        public void GameBoyTestCPC()
        {
            //CP Compare - page 95 compare
            Assert.True(gbMemory.WriteToMemory(0, 0xB9));
            gbCPU.A = 0x3C;
            gbCPU.C = 0x2F;
            gbCPU.F = 0x00;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x01, gbCPU.PC);
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(0x60, gbCPU.F);
        }

        [Test]
        public void GameBoyTestCPD()
        {
            //CP Compare - page 95 compare
            Assert.True(gbMemory.WriteToMemory(0, 0xBA));
            gbCPU.A = 0x3C;
            gbCPU.D = 0x2F;
            gbCPU.F = 0x00;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x01, gbCPU.PC);
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(0x60, gbCPU.F);
        }

        [Test]
        public void GameBoyTestCPE()
        {
            //CP Compare - page 95 compare
            Assert.True(gbMemory.WriteToMemory(0, 0xBB));
            gbCPU.A = 0x3C;
            gbCPU.E = 0x2F;
            gbCPU.F = 0x00;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x01, gbCPU.PC);
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(0x60, gbCPU.F);
        }

        [Test]
        public void GameBoyTestCPH()
        {
            //CP Compare - page 95 compare
            Assert.True(gbMemory.WriteToMemory(0, 0xBC));
            gbCPU.A = 0x3C;
            gbCPU.H = 0x2F;
            gbCPU.F = 0x00;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x01, gbCPU.PC);
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(0x60, gbCPU.F);
        }

        [Test]
        public void GameBoyTestCPL()
        {
            //CP Compare - page 95 compare
            Assert.True(gbMemory.WriteToMemory(0, 0xBD));
            gbCPU.A = 0x3C;
            gbCPU.L = 0x2F;
            gbCPU.F = 0x00;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x01, gbCPU.PC);
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(0x60, gbCPU.F);
        }

        [Test]
        public void GameBoyTestCPN()
        {
            //CP Compare - page 95 compare
            Assert.True(gbMemory.WriteToMemory(0, 0xFE));
            Assert.True(gbMemory.WriteToMemory(1, 0x3C));
            gbCPU.A = 0x3C;
            gbCPU.F = 0x00;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(8, cycle);
            Assert.AreEqual(0xC0, gbCPU.F);
        }

        [Test]
        public void GameBoyTestCPHL()
        {
            //CP Compare - page 95 compare
            Assert.True(gbMemory.WriteToMemory(0, 0xBE));
            Assert.True(gbMemory.WriteToMemory(100, 0x40));
            gbCPU.A = 0x3C;
            gbCPU.F = 0x00;
            gbCPU.H = 0x00;
            gbCPU.L = 0x64;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x01, gbCPU.PC);
            Assert.AreEqual(8, cycle);
            Assert.AreEqual(0x50, gbCPU.F);
        }

/*************************************** SUB TEST ****************************************************/


        [Test]
        public void GameBoyTestSUBA()
        {
            //SUB - page 93 Subtract A from A
            Assert.True(gbMemory.WriteToMemory(0, 0x97));
            gbCPU.A = 0x3E;
            gbCPU.F = 0x00;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x01, gbCPU.PC);
            Assert.AreEqual(0x00, gbCPU.A);
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(0xC0, gbCPU.F); //TODO
        }

        [Test]
        public void GameBoyTestSUBB()
        {
            //SUB - page 93 Subtract B from A
            Assert.True(gbMemory.WriteToMemory(0, 0x90));
            gbCPU.A = 0x3E;
            gbCPU.B = 0x3E;
            gbCPU.F = 0x00;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x01, gbCPU.PC);
            Assert.AreEqual(0x00, gbCPU.A);
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(0xC0, gbCPU.F);
        }

        [Test]
        public void GameBoyTestSUBC()
        {
            //SUB - page 93 Subtract C from A
            Assert.True(gbMemory.WriteToMemory(0, 0x91));
            gbCPU.A = 0x3E;
            gbCPU.C = 0x3E;
            gbCPU.F = 0x00;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x01, gbCPU.PC);
            Assert.AreEqual(0x00, gbCPU.A);
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(0xC0, gbCPU.F);
        }

        [Test]
        public void GameBoyTestSUBD()
        {
            //SUB - page 93 Subtract D from A
            Assert.True(gbMemory.WriteToMemory(0, 0x92));
            gbCPU.A = 0x3E;
            gbCPU.D = 0x3E;
            gbCPU.F = 0x00;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x01, gbCPU.PC);
            Assert.AreEqual(0x00, gbCPU.A);
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(0xC0, gbCPU.F);
        }

        [Test]
        public void GameBoyTestSUBE()
        {
            //SUB - page 93 Subtract E from A
            Assert.True(gbMemory.WriteToMemory(0, 0x93));
            gbCPU.A = 0x3E;
            gbCPU.E = 0x3E;
            gbCPU.F = 0x00;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x01, gbCPU.PC);
            Assert.AreEqual(0x00, gbCPU.A);
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(0xC0, gbCPU.F);
        }

        [Test]
        public void GameBoyTestSUBH()
        {
            //SUB - page 93 Subtract H from A
            Assert.True(gbMemory.WriteToMemory(0, 0x94));
            gbCPU.A = 0x3E;
            gbCPU.H = 0x3E;
            gbCPU.F = 0x00;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x01, gbCPU.PC);
            Assert.AreEqual(0x00, gbCPU.A);
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(0xC0, gbCPU.F);
        }

        [Test]
        public void GameBoyTestSUBL()
        {
            //SUB - page 93 Subtract L from A
            Assert.True(gbMemory.WriteToMemory(0, 0x95));
            gbCPU.A = 0x3E;
            gbCPU.L = 0x3E;
            gbCPU.F = 0x00;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x01, gbCPU.PC);
            Assert.AreEqual(0x00, gbCPU.A);
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(0xC0, gbCPU.F);
        }

        [Test]
        public void GameBoyTestSUBHL()
        {
            //SUB - page 93 Subtract (HL) from A
            Assert.True(gbMemory.WriteToMemory(0, 0x96));
            Assert.True(gbMemory.WriteToMemory(1, 0x40));
            gbCPU.A = 0x3E;
            gbCPU.H = 0x00;
            gbCPU.L = 0x01;
            gbCPU.F = 0x00;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x01, gbCPU.PC);
            Assert.AreEqual(0xFE, gbCPU.A);
            Assert.AreEqual(8, cycle);
            Assert.AreEqual(0x50, gbCPU.F);
        }

        [Test]
        public void GameBoyTestSUBN()
        {
            //SUB - page 93 Subtract N from A
            Assert.True(gbMemory.WriteToMemory(0, 0xD6));
            Assert.True(gbMemory.WriteToMemory(1, 0x0F));
            gbCPU.A = 0x3E;
            gbCPU.F = 0x00;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0x2F, gbCPU.A);
            Assert.AreEqual(8, cycle);
            Assert.AreEqual(0x60, gbCPU.F);
        }

/*************************************** SBC TEST ****************************************************/

        [Test]
        public void GameBoyTestSBCA()
        {
            //SBC - page 93 Subtract A and CY from A
            Assert.True(gbMemory.WriteToMemory(0, 0x9F));
            gbCPU.A = 0x3B;
            gbCPU.F = 0x10;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(1, gbCPU.PC);
            Assert.AreEqual(0xFF, gbCPU.A);
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(0x70, gbCPU.F);
        }

        [Test]
        public void GameBoyTestSBCB()
        {
            //SBC - page 93 Subtract B and CY from A
            Assert.True(gbMemory.WriteToMemory(0, 0x98));
            gbCPU.A = 0x3B;
            gbCPU.B = 0x2A;
            gbCPU.F = 0x10;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(1, gbCPU.PC);
            Assert.AreEqual(0x10, gbCPU.A);
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(0x40, gbCPU.F);
        }

        [Test]
        public void GameBoyTestSBCC()
        {
            //SBC - page 93 Subtract C and CY from A
            Assert.True(gbMemory.WriteToMemory(0, 0x99));
            gbCPU.A = 0x3B;
            gbCPU.C = 0x2A;
            gbCPU.F = 0x10;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(1, gbCPU.PC);
            Assert.AreEqual(0x10, gbCPU.A);
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(0x40, gbCPU.F);
        }

        [Test]
        public void GameBoyTestSBCD()
        {
            //SBC - page 93 Subtract D and CY from A
            Assert.True(gbMemory.WriteToMemory(0, 0x9A));
            gbCPU.A = 0x3B;
            gbCPU.D = 0x2A;
            gbCPU.F = 0x10;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(1, gbCPU.PC);
            Assert.AreEqual(0x10, gbCPU.A);
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(0x40, gbCPU.F);
        }

        [Test]
        public void GameBoyTestSBCE()
        {
            //SBC - page 93 Subtract E and CY from A
            Assert.True(gbMemory.WriteToMemory(0, 0x9B));
            gbCPU.A = 0x3B;
            gbCPU.E = 0x2A;
            gbCPU.F = 0x10;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(1, gbCPU.PC);
            Assert.AreEqual(0x10, gbCPU.A);
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(0x40, gbCPU.F);
        }

        [Test]
        public void GameBoyTestSBCH()
        {
            //SBC - page 93 Subtract H and CY from A
            Assert.True(gbMemory.WriteToMemory(0, 0x9C));
            gbCPU.A = 0x3B;
            gbCPU.H = 0x2A;
            gbCPU.F = 0x10;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(1, gbCPU.PC);
            Assert.AreEqual(0x10, gbCPU.A);
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(0x40, gbCPU.F);
        }

        [Test]
        public void GameBoyTestSBCL()
        {
            //SBC - page 93 Subtract L and CY from A
            Assert.True(gbMemory.WriteToMemory(0, 0x9D));
            gbCPU.A = 0x3B;
            gbCPU.L = 0x2A;
            gbCPU.F = 0x10;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(1, gbCPU.PC);
            Assert.AreEqual(0x10, gbCPU.A);
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(0x40, gbCPU.F);
        }

        [Test]
        public void GameBoyTestSBCU8()
        {
            //SBC - page 93 Subtract u8 and CY from A
            Assert.True(gbMemory.WriteToMemory(0, 0xDE));
            Assert.True(gbMemory.WriteToMemory(1, 0x3A));
            gbCPU.A = 0x3B;
            gbCPU.F = 0x10;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(2, gbCPU.PC);
            Assert.AreEqual(0x00, gbCPU.A);
            Assert.AreEqual(8, cycle);
            Assert.AreEqual(0xC0, gbCPU.F);
        }


        [Test]
        public void GameBoyTestINCFlags()
        {
            //INC C
            GameBoyCPU.ClockCycle = 0;
            gbMemory.WriteToMemory(0, 0x0C);
            gbCPU.C = 0xFD;
            gbCPU.F = 0xFF;
            gbCPU.Tick();
            Assert.AreEqual(0xFE, gbCPU.C);
            Assert.AreEqual(4, GameBoyCPU.ClockCycle);
            Assert.AreEqual(0x10, gbCPU.F);
        }

        [Test]
        public void GameBoyTestLDNN()
        {
            gbCart = new GameBoyCartiridge(0xFFFF);
            gbCart.Write(0x0104, 0x5F);
            gbMemory = new GameBoyMemory(gbCart);
            gbTimer = new GameBoyTimer(gbMemory,gbInterrupts);
            gbInterrupts = new GameBoyInterrupts(gbMemory);
            gbCPU = new GameBoyCPU(gbMemory,gbInterrupts);
            GameBoyCPU.ClockCycle = 0;
            gbMemory.WriteToMemory(0x0, 0x1A);
            gbCPU.A = 0x00;
            gbCPU.D = 0x01;
            gbCPU.E = 0x04;
            gbCPU.Tick();
            Assert.AreEqual(0x5F, gbCPU.A);
            Assert.AreEqual(8, GameBoyCPU.ClockCycle);
        }

        [Test]
        public void GameBoyTestLDNNAHLI()
        {
            gbCart = new GameBoyCartiridge(0xFFFF);
            gbCart.Write(0x01FF, 0x56);
            gbMemory = new GameBoyMemory(gbCart);
            gbTimer = new GameBoyTimer(gbMemory,gbInterrupts);
            gbInterrupts = new GameBoyInterrupts(gbMemory);
            gbCPU = new GameBoyCPU(gbMemory,gbInterrupts);
            gbMemory.WriteToMemory(0x0, 0x2A);
            gbMemory.WriteToMemory(0x0, 0x2A);
            gbCPU.A = 0x00;
            gbCPU.H = 0x01;
            gbCPU.L = 0xFF;

            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x56, gbCPU.A);
            Assert.AreEqual(0x02, gbCPU.H);
            Assert.AreEqual(0x00, gbCPU.L);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestCallNN()
        {
            gbMemory = new GameBoyMemory(null);
            gbTimer = new GameBoyTimer(gbMemory,gbInterrupts);
            gbInterrupts = new GameBoyInterrupts(gbMemory);
            gbCPU = new GameBoyCPU(gbMemory,gbInterrupts);
            GameBoyCPU.ClockCycle = 0;
            gbCPU.PC = 0x8000;
            Assert.True(gbMemory.WriteToMemory(0x8000, 0xCD));
            Assert.True(gbMemory.WriteToMemory(0x8001, 0x34));
            Assert.True(gbMemory.WriteToMemory(0x8002, 0x12));
            gbCPU.SP = 0xFFFE;
            gbCPU.Tick();
            Assert.AreEqual(0x1234, gbCPU.PC);
            Assert.AreEqual(0xFFFC, gbCPU.SP);
            Assert.AreEqual(0x80, gbMemory.ReadFromMemory(0xFFFD));
            Assert.AreEqual(0x03, gbMemory.ReadFromMemory(0xFFFC));
            Assert.AreEqual(24, GameBoyCPU.ClockCycle);
        }

        [Test]
        public void GameBoyTestCallNZSkip()
        {
            gbCPU.PC = 0x8000;
            gbCPU.F = 0x80;
            Assert.True(gbMemory.WriteToMemory(0x8000, 0xC4));
            Assert.True(gbMemory.WriteToMemory(0x8001, 0x34));
            Assert.True(gbMemory.WriteToMemory(0x8002, 0x12));
            gbCPU.SP = 0xFFFE;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x8003, gbCPU.PC);
            Assert.AreEqual(0xFFFE, gbCPU.SP);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestCallNZ()
        {
            gbCPU.PC = 0x8000;
            gbCPU.F = 0x00;
            Assert.True(gbMemory.WriteToMemory(0x8000, 0xC4));
            Assert.True(gbMemory.WriteToMemory(0x8001, 0x00));
            Assert.True(gbMemory.WriteToMemory(0x8002, 0x90));
            Assert.True(gbMemory.WriteToMemory(0x9000, 0x00));
            gbCPU.SP = 0xFFFE;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x9000, gbCPU.PC);
            Assert.AreEqual(0xFFFC, gbCPU.SP);
            Assert.AreEqual(24, cycle);
            Assert.AreEqual(0x03,gbMemory.ReadFromMemory((ushort)(gbCPU.SP)));
            Assert.AreEqual(0x80,gbMemory.ReadFromMemory((ushort)(gbCPU.SP + 1)));
        }

        [Test]
        public void GameBoyTestCallNC()
        {
            gbCPU.PC = 0x6000;
            //Call to 0x9000 when cc = NC and C = 0
            gbCPU.F = 0x00;
            Assert.True(gbMemory.WriteToMemory(0x6000, 0xD4));
            Assert.True(gbMemory.WriteToMemory(0x6001, 0x00));
            Assert.True(gbMemory.WriteToMemory(0x6002, 0x70));
            Assert.True(gbMemory.WriteToMemory(0x7000, 0x00));
            gbCPU.SP = 0xFFFE;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x7000, gbCPU.PC);
            Assert.AreEqual(0xFFFC, gbCPU.SP);
            Assert.AreEqual(24, cycle);
            Assert.AreEqual(0x03,gbMemory.ReadFromMemory((ushort)(gbCPU.SP)));
            Assert.AreEqual(0x60,gbMemory.ReadFromMemory((ushort)(gbCPU.SP + 1)));
        }

        [Test]
        public void GameBoyTestCallC()
        {
            gbCPU.PC = 0x6000;
            //Call to 0x7000 when cc = C and C = 1
            gbCPU.F = 0x10;
            Assert.True(gbMemory.WriteToMemory(0x6000, 0xDC));
            Assert.True(gbMemory.WriteToMemory(0x6001, 0x00));
            Assert.True(gbMemory.WriteToMemory(0x6002, 0x70));
            Assert.True(gbMemory.WriteToMemory(0x7000, 0x00));
            gbCPU.SP = 0xFFFE;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x7000, gbCPU.PC);
            Assert.AreEqual(0xFFFC, gbCPU.SP);
            Assert.AreEqual(24, cycle);
            Assert.AreEqual(0x03,gbMemory.ReadFromMemory((ushort)(gbCPU.SP)));
            Assert.AreEqual(0x60,gbMemory.ReadFromMemory((ushort)(gbCPU.SP + 1)));
        }

        [Test]
        public void GameBoyTestCallCSkip()
        {
            gbCPU.PC = 0x8000;
            //Call to 0x9000 when cc = C and C = 0 AKA SKIP
            gbCPU.F = 0x00;
            Assert.True(gbMemory.WriteToMemory(0x8000, 0xDC));
            Assert.True(gbMemory.WriteToMemory(0x8001, 0x00));
            Assert.True(gbMemory.WriteToMemory(0x8002, 0x90));
            Assert.True(gbMemory.WriteToMemory(0x9000, 0x00));
            gbCPU.SP = 0xFFFE;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x8003, gbCPU.PC);
            Assert.AreEqual(0xFFFE, gbCPU.SP);
            Assert.AreEqual(8, cycle);
        }

        [Test]
        public void GameBoyTestCallZ()
        {
            gbCPU.PC = 0x8000;
            gbCPU.F = 0x80;
            Assert.True(gbMemory.WriteToMemory(0x8000, 0xCC));
            Assert.True(gbMemory.WriteToMemory(0x8001, 0x34));
            Assert.True(gbMemory.WriteToMemory(0x8002, 0x12));
            gbCPU.SP = 0xFFFE;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x1234, gbCPU.PC);
            Assert.AreEqual(0xFFFC, gbCPU.SP);
            Assert.AreEqual(0x80, gbMemory.ReadFromMemory(0xFFFD));
            Assert.AreEqual(0x03, gbMemory.ReadFromMemory(0xFFFC));
            Assert.AreEqual(24, cycle);
        }

/*********************************************RET TESTs*********************************************/

        [Test]
        public void GameBoyTestRet()
        {
            gbCPU.PC = 0x8000;
            // CALL 0x8003
            Assert.True(gbMemory.WriteToMemory(0x8000, 0xCD));
            Assert.True(gbMemory.WriteToMemory(0x8001, 0x03));
            Assert.True(gbMemory.WriteToMemory(0x8002, 0x80));
            Assert.True(gbMemory.WriteToMemory(0x8003, 0x00)); // NOOP
            Assert.True(gbMemory.WriteToMemory(0x8004, 0xC9));
            gbCPU.SP = 0xFFFE;
            uint cycle = gbCPU.Tick(); // Handle CALL FIRST
            Assert.AreEqual(0x8003, gbCPU.PC);
            Assert.AreEqual(0xFFFC, gbCPU.SP);
            Assert.AreEqual(0x80, gbMemory.ReadFromMemory(0xFFFD));
            Assert.AreEqual(0x03, gbMemory.ReadFromMemory(0xFFFC));
            Assert.AreEqual(24, cycle); // CALL(24)

            // CALL set us at 0x8003 which is a NOOP
            cycle = gbCPU.Tick(); // Handle NOOP
            Assert.AreEqual(0x8004, gbCPU.PC);
            Assert.AreEqual(0xFFFC, gbCPU.SP);
            Assert.AreEqual(0x80, gbMemory.ReadFromMemory(0xFFFD));
            Assert.AreEqual(0x03, gbMemory.ReadFromMemory(0xFFFC));
            Assert.AreEqual(4, cycle); //CALL(24) + NOOP(4) time

            // NOOP moved us to RET
            cycle = gbCPU.Tick(); // Handle RET 
            Assert.AreEqual(0x8003, gbCPU.PC);
            Assert.AreEqual(0xFFFE, gbCPU.SP);
            Assert.AreEqual(0x80, gbMemory.ReadFromMemory(0xFFFD));
            Assert.AreEqual(0x03, gbMemory.ReadFromMemory(0xFFFC));
            Assert.AreEqual(16, cycle); //CALL(24)+RET(16)+NOOP(4) time
        }

        [Test]
        public void GameBoyTestRetCCZ()
        {
            gbCPU.PC = 0x8000;
            gbCPU.F = 0x80; // Z = 1
            // CALL 0x8003
            Assert.True(gbMemory.WriteToMemory(0x8000, 0xCD));
            Assert.True(gbMemory.WriteToMemory(0x8001, 0x03));
            Assert.True(gbMemory.WriteToMemory(0x8002, 0x80));
            Assert.True(gbMemory.WriteToMemory(0x8003, 0x00)); // NOOP
            Assert.True(gbMemory.WriteToMemory(0x8004, 0xC8));
            gbCPU.SP = 0xFFFE;
            uint cycle = gbCPU.Tick(); // Handle CALL FIRST
            Assert.AreEqual(0x8003, gbCPU.PC);
            Assert.AreEqual(0xFFFC, gbCPU.SP);
            Assert.AreEqual(0x80, gbMemory.ReadFromMemory(0xFFFD));
            Assert.AreEqual(0x03, gbMemory.ReadFromMemory(0xFFFC));
            Assert.AreEqual(24, cycle); // CALL(24)

            // CALL set us at 0x8003 which is a NOOP
            cycle = gbCPU.Tick(); // Handle NOOP
            Assert.AreEqual(0x8004, gbCPU.PC);
            Assert.AreEqual(0xFFFC, gbCPU.SP);
            Assert.AreEqual(0x80, gbMemory.ReadFromMemory(0xFFFD));
            Assert.AreEqual(0x03, gbMemory.ReadFromMemory(0xFFFC));
            Assert.AreEqual(4, cycle); //CALL(24) + NOOP(4) time

            // NOOP moved us to RET Z (0x8004)
            cycle = gbCPU.Tick(); // Handle RET 
            Assert.AreEqual(0x8003, gbCPU.PC);
            Assert.AreEqual(0xFFFE, gbCPU.SP);
            Assert.AreEqual(0x80, gbMemory.ReadFromMemory(0xFFFD));
            Assert.AreEqual(0x03, gbMemory.ReadFromMemory(0xFFFC));
            Assert.AreEqual(20, cycle); //CALL(24)+RET(16)+NOOP(4) time
        }

        [Test]
        public void GameBoyTestRetCCNZ()
        {
            gbCPU.PC = 0x8000;
            gbCPU.F = 0x00; // Z = 0
            // CALL 0x8003
            Assert.True(gbMemory.WriteToMemory(0x8000, 0xCD));
            Assert.True(gbMemory.WriteToMemory(0x8001, 0x03));
            Assert.True(gbMemory.WriteToMemory(0x8002, 0x80));
            Assert.True(gbMemory.WriteToMemory(0x8003, 0x00)); // NOOP
            Assert.True(gbMemory.WriteToMemory(0x8004, 0xC0));
            gbCPU.SP = 0xFFFE;
            uint cycle = gbCPU.Tick(); // Handle CALL FIRST
            Assert.AreEqual(0x8003, gbCPU.PC);
            Assert.AreEqual(0xFFFC, gbCPU.SP);
            Assert.AreEqual(0x80, gbMemory.ReadFromMemory(0xFFFD));
            Assert.AreEqual(0x03, gbMemory.ReadFromMemory(0xFFFC));
            Assert.AreEqual(24, cycle); // CALL(24)

            // CALL set us at 0x8003 which is a NOOP
            cycle = gbCPU.Tick(); // Handle NOOP
            Assert.AreEqual(0x8004, gbCPU.PC);
            Assert.AreEqual(0xFFFC, gbCPU.SP);
            Assert.AreEqual(0x80, gbMemory.ReadFromMemory(0xFFFD));
            Assert.AreEqual(0x03, gbMemory.ReadFromMemory(0xFFFC));
            Assert.AreEqual(4, cycle); //CALL(24) + NOOP(4) time

            // NOOP moved us to RET Z (0x8004)
            cycle = gbCPU.Tick(); // Handle RET 
            Assert.AreEqual(0x8003, gbCPU.PC);
            Assert.AreEqual(0xFFFE, gbCPU.SP);
            Assert.AreEqual(0x80, gbMemory.ReadFromMemory(0xFFFD));
            Assert.AreEqual(0x03, gbMemory.ReadFromMemory(0xFFFC));
            Assert.AreEqual(20, cycle); //CALL(24)+RET(16)+NOOP(4) time
        }

        [Test]
        public void GameBoyTestRetCCNZSkip()
        {
            gbCPU.PC = 0x8000;
            gbCPU.F = 0x80; // Z = 1 (Normally Z = 0 to ret, checking if we go to next instruction)
            // CALL 0x8003
            Assert.True(gbMemory.WriteToMemory(0x8000, 0xCD));
            Assert.True(gbMemory.WriteToMemory(0x8001, 0x03));
            Assert.True(gbMemory.WriteToMemory(0x8002, 0x80));
            Assert.True(gbMemory.WriteToMemory(0x8003, 0x00)); // NOOP
            Assert.True(gbMemory.WriteToMemory(0x8004, 0xC0));
            gbCPU.SP = 0xFFFE;
            uint cycle = gbCPU.Tick(); // Handle CALL FIRST
            Assert.AreEqual(0x8003, gbCPU.PC);
            Assert.AreEqual(0xFFFC, gbCPU.SP);
            Assert.AreEqual(0x80, gbMemory.ReadFromMemory(0xFFFD));
            Assert.AreEqual(0x03, gbMemory.ReadFromMemory(0xFFFC));
            Assert.AreEqual(24, cycle); // CALL(24)

            // CALL set us at 0x8003 which is a NOOP
            cycle = gbCPU.Tick(); // Handle NOOP
            Assert.AreEqual(0x8004, gbCPU.PC);
            Assert.AreEqual(0xFFFC, gbCPU.SP);
            Assert.AreEqual(0x80, gbMemory.ReadFromMemory(0xFFFD));
            Assert.AreEqual(0x03, gbMemory.ReadFromMemory(0xFFFC));
            Assert.AreEqual(4, cycle); //CALL(24) + NOOP(4) time

            // NOOP moved us to RET Z (0x8004) // CC is false, so move to next instruction (0x8005)
            cycle = gbCPU.Tick(); // Handle RET 
            Assert.AreEqual(0x8005, gbCPU.PC);
            Assert.AreEqual(0xFFFC, gbCPU.SP);
            Assert.AreEqual(0x80, gbMemory.ReadFromMemory(0xFFFD));
            Assert.AreEqual(0x03, gbMemory.ReadFromMemory(0xFFFC));
            Assert.AreEqual(8, cycle); //CALL(24)+RET(8)+NOOP(4) time
        }

        [Test]
        public void GameBoyTestRetCCNC()
        {
            gbCPU.PC = 0x8000;
            // CALL 0x8003
            Assert.True(gbMemory.WriteToMemory(0x8000, 0xCD));
            Assert.True(gbMemory.WriteToMemory(0x8001, 0x03));
            Assert.True(gbMemory.WriteToMemory(0x8002, 0x80));
            Assert.True(gbMemory.WriteToMemory(0x8003, 0x00)); // NOOP
            Assert.True(gbMemory.WriteToMemory(0x8004, 0xD0));
            gbCPU.SP = 0xFFFE;
            uint cycle = gbCPU.Tick(); // Handle CALL FIRST
            Assert.AreEqual(0x8003, gbCPU.PC);
            Assert.AreEqual(0xFFFC, gbCPU.SP);
            Assert.AreEqual(0x80, gbMemory.ReadFromMemory(0xFFFD));
            Assert.AreEqual(0x03, gbMemory.ReadFromMemory(0xFFFC));
            Assert.AreEqual(24, cycle); // CALL(24)

            // CALL set us at 0x8003 which is a NOOP
            cycle = gbCPU.Tick(); // Handle NOOP
            Assert.AreEqual(0x8004, gbCPU.PC);
            Assert.AreEqual(0xFFFC, gbCPU.SP);
            Assert.AreEqual(0x80, gbMemory.ReadFromMemory(0xFFFD));
            Assert.AreEqual(0x03, gbMemory.ReadFromMemory(0xFFFC));
            Assert.AreEqual(4, cycle); //CALL(24) + NOOP(4) time

            // NOOP moved us to RET NZ
            gbCPU.F = 0x00;
            cycle = gbCPU.Tick(); // Handle RET NZ
            Assert.AreEqual(0x8003, gbCPU.PC);
            Assert.AreEqual(0xFFFE, gbCPU.SP);
            Assert.AreEqual(0x80, gbMemory.ReadFromMemory(0xFFFD));
            Assert.AreEqual(0x03, gbMemory.ReadFromMemory(0xFFFC));
            Assert.AreEqual(20, cycle); //CALL(24)+RET(16)+NOOP(4) time
        }

        [Test]
        public void GameBoyTestRetCCNCSkip()
        {
            gbCPU.PC = 0x8000;
            // CALL 0x8003
            Assert.True(gbMemory.WriteToMemory(0x8000, 0xCD));
            Assert.True(gbMemory.WriteToMemory(0x8001, 0x03));
            Assert.True(gbMemory.WriteToMemory(0x8002, 0x80));
            Assert.True(gbMemory.WriteToMemory(0x8003, 0x00)); // NOOP
            Assert.True(gbMemory.WriteToMemory(0x8004, 0xD0));
            gbCPU.SP = 0xFFFE;
            uint cycle = gbCPU.Tick(); // Handle CALL FIRST
            Assert.AreEqual(0x8003, gbCPU.PC);
            Assert.AreEqual(0xFFFC, gbCPU.SP);
            Assert.AreEqual(0x80, gbMemory.ReadFromMemory(0xFFFD));
            Assert.AreEqual(0x03, gbMemory.ReadFromMemory(0xFFFC));
            Assert.AreEqual(24, cycle); // CALL(24)

            // CALL set us at 0x8003 which is a NOOP
            cycle = gbCPU.Tick(); // Handle NOOP
            Assert.AreEqual(0x8004, gbCPU.PC);
            Assert.AreEqual(0xFFFC, gbCPU.SP);
            Assert.AreEqual(0x80, gbMemory.ReadFromMemory(0xFFFD));
            Assert.AreEqual(0x03, gbMemory.ReadFromMemory(0xFFFC));
            Assert.AreEqual(4, cycle); //CALL(24) + NOOP(4) time

            // NOOP moved us to RET NZ
            gbCPU.F = 0x10;
            cycle = gbCPU.Tick(); // Handle RET NZ skip
            Assert.AreEqual(0x8005, gbCPU.PC);
            Assert.AreEqual(0xFFFC, gbCPU.SP);
            Assert.AreEqual(0x80, gbMemory.ReadFromMemory(0xFFFD));
            Assert.AreEqual(0x03, gbMemory.ReadFromMemory(0xFFFC));
            Assert.AreEqual(8, cycle); //CALL(24)+RET(8)+NOOP(4) time
        }

        [Test]
        public void GameBoyTestRetCCC()
        {
            gbCPU.PC = 0x8000;
            // CALL 0x8003
            Assert.True(gbMemory.WriteToMemory(0x8000, 0xCD));
            Assert.True(gbMemory.WriteToMemory(0x8001, 0x03));
            Assert.True(gbMemory.WriteToMemory(0x8002, 0x80));
            Assert.True(gbMemory.WriteToMemory(0x8003, 0x00)); // NOOP
            Assert.True(gbMemory.WriteToMemory(0x8004, 0xD8));
            gbCPU.SP = 0xFFFE;
            uint cycle = gbCPU.Tick(); // Handle CALL FIRST
            Assert.AreEqual(0x8003, gbCPU.PC);
            Assert.AreEqual(0xFFFC, gbCPU.SP);
            Assert.AreEqual(0x80, gbMemory.ReadFromMemory(0xFFFD));
            Assert.AreEqual(0x03, gbMemory.ReadFromMemory(0xFFFC));
            Assert.AreEqual(24, cycle); // CALL(24)

            // CALL set us at 0x8003 which is a NOOP
            cycle = gbCPU.Tick(); // Handle NOOP
            Assert.AreEqual(0x8004, gbCPU.PC);
            Assert.AreEqual(0xFFFC, gbCPU.SP);
            Assert.AreEqual(0x80, gbMemory.ReadFromMemory(0xFFFD));
            Assert.AreEqual(0x03, gbMemory.ReadFromMemory(0xFFFC));
            Assert.AreEqual(4, cycle); //CALL(24) + NOOP(4) time

            // NOOP moved us to RET C
            gbCPU.F = 0x10;
            cycle = gbCPU.Tick(); // Handle RET C
            Assert.AreEqual(0x8003, gbCPU.PC);
            Assert.AreEqual(0xFFFE, gbCPU.SP);
            Assert.AreEqual(0x80, gbMemory.ReadFromMemory(0xFFFD));
            Assert.AreEqual(0x03, gbMemory.ReadFromMemory(0xFFFC));
            Assert.AreEqual(20, cycle); //CALL(24)+RET(16)+NOOP(4) time
            Assert.AreEqual(0x10, gbCPU.F); //CALL(24)+RET(16)+NOOP(4) time
        }

        [Test]
        public void GameBoyTestRetCCCSkip()
        {
            gbCPU.PC = 0x8000;
            // CALL 0x8003
            Assert.True(gbMemory.WriteToMemory(0x8000, 0xCD));
            Assert.True(gbMemory.WriteToMemory(0x8001, 0x03));
            Assert.True(gbMemory.WriteToMemory(0x8002, 0x80));
            Assert.True(gbMemory.WriteToMemory(0x8003, 0x00)); // NOOP
            Assert.True(gbMemory.WriteToMemory(0x8004, 0xD8));
            gbCPU.SP = 0xFFFE;
            uint cycle = gbCPU.Tick(); // Handle CALL FIRST
            Assert.AreEqual(0x8003, gbCPU.PC);
            Assert.AreEqual(0xFFFC, gbCPU.SP);
            Assert.AreEqual(0x80, gbMemory.ReadFromMemory(0xFFFD));
            Assert.AreEqual(0x03, gbMemory.ReadFromMemory(0xFFFC));
            Assert.AreEqual(24, cycle); // CALL(24)

            // CALL set us at 0x8003 which is a NOOP
            cycle = gbCPU.Tick(); // Handle NOOP
            Assert.AreEqual(0x8004, gbCPU.PC);
            Assert.AreEqual(0xFFFC, gbCPU.SP);
            Assert.AreEqual(0x80, gbMemory.ReadFromMemory(0xFFFD));
            Assert.AreEqual(0x03, gbMemory.ReadFromMemory(0xFFFC));
            Assert.AreEqual(4, cycle); //CALL(24) + NOOP(4) time

            // NOOP moved us to RET C
            gbCPU.F = 0x00;
            cycle = gbCPU.Tick(); // Handle RET C skip
            Assert.AreEqual(0x8005, gbCPU.PC);
            Assert.AreEqual(0xFFFC, gbCPU.SP);
            Assert.AreEqual(0x80, gbMemory.ReadFromMemory(0xFFFD));
            Assert.AreEqual(0x03, gbMemory.ReadFromMemory(0xFFFC));
            Assert.AreEqual(8, cycle); //CALL(24)+RET(8)+NOOP(4) time
        }

        [Test]
        public void GameBoyTestPush()
        {
            gbMemory = new GameBoyMemory(null);
            gbTimer = new GameBoyTimer(gbMemory,gbInterrupts);
            gbInterrupts = new GameBoyInterrupts(gbMemory);
            gbCPU = new GameBoyCPU(gbMemory,gbInterrupts);
            GameBoyCPU.ClockCycle = 0;
            gbCPU.PC = 0x00;
            Assert.True(gbMemory.WriteToMemory(0x00, 0xC5));
            gbCPU.B = 0x3C;
            gbCPU.C = 0x5F;
            gbCPU.SP = 0xFFFE;
            gbCPU.Tick();
            Assert.AreEqual(0x1, gbCPU.PC);
            Assert.AreEqual(0xFFFC, gbCPU.SP);
            Assert.AreEqual(0x3C, gbMemory.ReadFromMemory(0xFFFD));
            Assert.AreEqual(0x5F, gbMemory.ReadFromMemory(0xFFFC));
            Assert.AreEqual(16, GameBoyCPU.ClockCycle);
        }

        [Test]
        public void GameBoyTestPop()
        {
            gbMemory = new GameBoyMemory(null);
            gbTimer = new GameBoyTimer(gbMemory,gbInterrupts);
            gbInterrupts = new GameBoyInterrupts(gbMemory);
            gbCPU = new GameBoyCPU(gbMemory,gbInterrupts);
            GameBoyCPU.ClockCycle = 0;
            gbCPU.PC = 0x00;
            Assert.True(gbMemory.WriteToMemory(0x00, 0xC1));
            Assert.True(gbMemory.WriteToMemory(0xFFFD, 0x3C));
            Assert.True(gbMemory.WriteToMemory(0xFFFC, 0x5F));
            gbCPU.B = 0x00;
            gbCPU.C = 0x00;
            gbCPU.SP = 0xFFFC;
            gbCPU.Tick();
            Assert.AreEqual(0x1, gbCPU.PC);
            Assert.AreEqual(0xFFFE, gbCPU.SP);
            Assert.AreEqual(0x3C, gbCPU.B);
            Assert.AreEqual(0x5F, gbCPU.C);
            Assert.AreEqual(12, GameBoyCPU.ClockCycle);
        }

/******************************* RLA *****************************/

        [Test]
        public void GameBoyTestRLA()
        {
            gbCPU.PC = 0x0;
            gbCPU.A = 0x95;
            gbCPU.F = 0x90; //Z and C are set
            Assert.True(gbMemory.WriteToMemory(0x0, 0x17));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(0x01, gbCPU.PC);
            Assert.AreEqual(0x2B, gbCPU.A);
            Assert.AreEqual(0x10, gbCPU.F); //Z should be unset
        }

/******************************* RL N ****************************/

        [Test]
        public void GameBoyTestRLB()
        {
            gbCPU.PC = 0x0;
            gbCPU.B = 0x80;
            gbCPU.F = 0x80; //Z is set
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x10));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle); // CB(4) + RL B(8)
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0x00, gbCPU.B);
            Assert.AreEqual(0x90, gbCPU.F); //Z and CY should be set
        }

        [Test]
        public void GameBoyTestRLC()
        {
            gbCPU.PC = 0x0;
            gbCPU.C = 0x80;
            gbCPU.F = 0x80; //Z is set
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x11));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle); // CB(4) + RL B(8)
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0x00, gbCPU.C);
            Assert.AreEqual(0x90, gbCPU.F); //Z and CY should be set
        }

        [Test]
        public void GameBoyTestRLD()
        {
            gbCPU.PC = 0x0;
            gbCPU.D = 0x80;
            gbCPU.F = 0x80; //Z is set
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x12));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle); // CB(4) + RL B(8)
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0x00, gbCPU.D);
            Assert.AreEqual(0x90, gbCPU.F); //Z and CY should be set
        }

        [Test]
        public void GameBoyTestRLE()
        {
            gbCPU.PC = 0x0;
            gbCPU.E = 0x80;
            gbCPU.F = 0x80; //Z is set
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x13));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle); // CB(4) + RL B(8)
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0x00, gbCPU.E);
            Assert.AreEqual(0x90, gbCPU.F); //Z and CY should be set
        }

        [Test]
        public void GameBoyTestRLH()
        {
            gbCPU.PC = 0x0;
            gbCPU.H = 0x80;
            gbCPU.F = 0x80; //Z is set
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x14));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle); // CB(4) + RL B(8)
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0x00, gbCPU.H);
            Assert.AreEqual(0x90, gbCPU.F); //Z and CY should be set
        }

        [Test]
        public void GameBoyTestRLL()
        {
            gbCPU.PC = 0x0;
            gbCPU.L = 0x80;
            gbCPU.F = 0x00;
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x15));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle); // CB(4) + RL B(8)
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0x00, gbCPU.L);
            Assert.AreEqual(0x90, gbCPU.F);
        }

        [Test]
        public void GameBoyTestCBRLA()
        {
            gbCPU.PC = 0x0;
            gbCPU.A = 0x80;
            gbCPU.F = 0x00;
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x17));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle); // CB(4) + RL B(8)
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0x00, gbCPU.L);
            Assert.AreEqual(0x90, gbCPU.F);
        }

        [Test]
        public void GameBoyTestRLHL()
        {
            gbCPU.PC = 0x0;
            gbCPU.H = 0x00;
            gbCPU.L = 0x80;
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x16));
            Assert.True(gbMemory.WriteToMemory(0x80, 0x11)); //HL = 0x80
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(16, cycle);
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0x00, gbCPU.H);
            Assert.AreEqual(0x80, gbCPU.L);
            Assert.AreEqual(0x00, gbCPU.F);
            Assert.AreEqual(0x22, gbMemory.ReadFromMemory(0x80)); //HL = 0x80
        }

/******************************* RLCA *****************************/

        [Test]
        public void GameBoyTestRLCA()
        {
            gbCPU.PC = 0x0;
            gbCPU.A = 0x85;
            gbCPU.F = 0xE0; //Z, H, and N are set
            Assert.True(gbMemory.WriteToMemory(0x0, 0x07));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(0x01, gbCPU.PC);
            Assert.AreEqual(0x0B, gbCPU.A); // Maybe 0x0A? Based on RLCA example from gameboy programming manual
            Assert.AreEqual(0x10, gbCPU.F); //Z, H, and N should be unset
        }

/******************************* RLC n *****************************/

        [Test]
        public void GameBoyTestRLCarryA()
        {
            gbCPU.PC = 0x0;
            gbCPU.A = 0x85;
            gbCPU.F = 0xE0; //Z, H, and N are set
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x07));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle); // CB(4) + RLC(8)
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0x0B, gbCPU.A); // Maybe 0x0A? Based on RLCA example from gameboy programming manual
            Assert.AreEqual(0x10, gbCPU.F); //Z, H, and N should be unset
        }

        [Test]
        public void GameBoyTestRLCarryB()
        {
            gbCPU.PC = 0x0;
            gbCPU.B = 0x85;
            gbCPU.F = 0xE0; //Z, H, and N are set
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x00));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle); // CB(4) + RLC(8)
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0x0B, gbCPU.B); // Maybe 0x0A? Based on RLCA example from gameboy programming manual
            Assert.AreEqual(0x10, gbCPU.F); //Z, H, and N should be unset
        }

        [Test]
        public void GameBoyTestRLCarryC()
        {
            gbCPU.PC = 0x0;
            gbCPU.C = 0x0;
            gbCPU.F = 0xE0; //Z, H, and N are set
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x01));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle); // CB(4) + RLC(8)
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0x00, gbCPU.C); // Maybe 0x0A? Based on RLCA example from gameboy programming manual
            Assert.AreEqual(0x80, gbCPU.F); //Z should be set
        }

        [Test]
        public void GameBoyTestRLCarryD()
        {
            gbCPU.PC = 0x0;
            gbCPU.D = 0x85;
            gbCPU.F = 0xE0; //Z, H, and N are set
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x02));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle); // CB(4) + RLC(8)
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0x0B, gbCPU.D); // Maybe 0x0A? Based on RLCA example from gameboy programming manual
            Assert.AreEqual(0x10, gbCPU.F); //Z, H, and N should be unset
        }

        [Test]
        public void GameBoyTestRLCarryE()
        {
            gbCPU.PC = 0x0;
            gbCPU.E = 0x85;
            gbCPU.F = 0xE0; //Z, H, and N are set
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x03));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle); // CB(4) + RLC(8)
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0x0B, gbCPU.E); // Maybe 0x0A? Based on RLCA example from gameboy programming manual
            Assert.AreEqual(0x10, gbCPU.F); //Z, H, and N should be unset
        }

        [Test]
        public void GameBoyTestRLCarryH()
        {
            gbCPU.PC = 0x0;
            gbCPU.H = 0x85;
            gbCPU.F = 0xE0; //Z, H, and N are set
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x04));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle); // CB(4) + RLC(8)
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0x0B, gbCPU.H); // Maybe 0x0A? Based on RLCA example from gameboy programming manual
            Assert.AreEqual(0x10, gbCPU.F); //Z, H, and N should be unset
        }

        [Test]
        public void GameBoyTestRLCarryL()
        {
            gbCPU.PC = 0x0;
            gbCPU.L = 0x85;
            gbCPU.F = 0xE0; //Z, H, and N are set
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x05));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle); // CB(4) + RLC(8)
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0x0B, gbCPU.L); // Maybe 0x0A? Based on RLCA example from gameboy programming manual
            Assert.AreEqual(0x10, gbCPU.F); //Z, H, and N should be unset
        }

/******************************* RRCA *****************************/

        [Test]
        public void GameBoyTestRRCA()
        {
            gbCPU.PC = 0x0;
            gbCPU.A = 0x3B;
            gbCPU.F = 0xE0; //Z, H, and N are set
            Assert.True(gbMemory.WriteToMemory(0x0, 0x0F));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(0x01, gbCPU.PC);
            Assert.AreEqual(0x9D, gbCPU.A); // Maybe 0x0A? Based on RLCA example from gameboy programming manual
            Assert.AreEqual(0x10, gbCPU.F); //Z, H, and N should be unset
        }

/******************************* RRC n *****************************/

        [Test]
        public void GameBoyTestRRCarryB()
        {
            gbCPU.PC = 0x0;
            gbCPU.B = 0x01;
            gbCPU.F = 0xE0; //Z, H, and N are set
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x08));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle); //CB(4) + RRC(8)
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0x80, gbCPU.B);
            Assert.AreEqual(0x10, gbCPU.F); //Z, H, and N should be unset
        }

        [Test]
        public void GameBoyTestRRCarryC()
        {
            gbCPU.PC = 0x0;
            gbCPU.C = 0x01;
            gbCPU.F = 0xE0; //Z, H, and N are set
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x09));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle); //CB(4) + RRC(8)
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0x80, gbCPU.C);
            Assert.AreEqual(0x10, gbCPU.F); //Z, H, and N should be unset
        }

        [Test]
        public void GameBoyTestRRCarryD()
        {
            gbCPU.PC = 0x0;
            gbCPU.D = 0x01;
            gbCPU.F = 0xE0; //Z, H, and N are set
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x0A));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle); //CB(4) + RRC(8)
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0x80, gbCPU.D);
            Assert.AreEqual(0x10, gbCPU.F); //Z, H, and N should be unset
        }

        [Test]
        public void GameBoyTestRRCarryE()
        {
            gbCPU.PC = 0x0;
            gbCPU.E = 0x01;
            gbCPU.F = 0xE0; //Z, H, and N are set
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x0B));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle); //CB(4) + RRC(8)
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0x80, gbCPU.E);
            Assert.AreEqual(0x10, gbCPU.F); //Z, H, and N should be unset
        }

        [Test]
        public void GameBoyTestRRCarryH()
        {
            gbCPU.PC = 0x0;
            gbCPU.H = 0x01;
            gbCPU.F = 0xE0; //Z, H, and N are set
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x0C));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle); //CB(4) + RRC(8)
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0x80, gbCPU.H);
            Assert.AreEqual(0x10, gbCPU.F); //Z, H, and N should be unset
        }

        [Test]
        public void GameBoyTestRRCarryL()
        {
            gbCPU.PC = 0x0;
            gbCPU.L = 0x01;
            gbCPU.F = 0xE0; //Z, H, and N are set
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x0D));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle); //CB(4) + RRC(8)
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0x80, gbCPU.L);
            Assert.AreEqual(0x10, gbCPU.F); //Z, H, and N should be unset
        }

        [Test]
        public void GameBoyTestRRCarryA()
        {
            gbCPU.PC = 0x0;
            gbCPU.A = 0x01;
            gbCPU.F = 0xE0; //Z, H, and N are set
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x0F));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle); //CB(4) + RRC(8)
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0x80, gbCPU.A);
            Assert.AreEqual(0x10, gbCPU.F); //Z, H, and N should be unset
        }

        [Test]
        public void GameBoyTestLDNNA()
        {
            gbCPU.PC = 0x0;
            gbCPU.A = 0x19;
            Assert.True(gbMemory.WriteToMemory(0x0, 0xEA));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x10));
            Assert.True(gbMemory.WriteToMemory(0x2, 0x99));

            uint cycle = gbCPU.Tick();
            Assert.AreEqual(16, cycle);
            Assert.AreEqual(0x03, gbCPU.PC);
            Assert.AreEqual(0x19, gbCPU.A);
            Assert.AreEqual(0x19, gbMemory.ReadFromMemory(0x9910));
        }
/*********************************  SRL *********************************************/
        [Test]
        public void GameBoyTestSRLA()
        {
            gbCPU.PC = 0x0;
            gbCPU.A = 0x01;
            gbCPU.F = 0x00;
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x3F));

            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle);
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0x00, gbCPU.A);
            Assert.AreEqual(0x90, gbCPU.F);
        }

        [Test]
        public void GameBoyTestSRLB()
        {
            gbCPU.PC = 0x0;
            gbCPU.B = 0x01;
            gbCPU.F = 0x00;
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x38));

            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle);
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0x00, gbCPU.B);
            Assert.AreEqual(0x90, gbCPU.F);
        }

        [Test]
        public void GameBoyTestSRLC()
        {
            gbCPU.PC = 0x0;
            gbCPU.C = 0x01;
            gbCPU.F = 0x00;
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x39));

            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle);
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0x00, gbCPU.C);
            Assert.AreEqual(0x90, gbCPU.F);
        }

        [Test]
        public void GameBoyTestSRLD()
        {
            gbCPU.PC = 0x0;
            gbCPU.D = 0x01;
            gbCPU.F = 0x00;
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x3A));

            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle);
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0x00, gbCPU.D);
            Assert.AreEqual(0x90, gbCPU.F);
        }

        [Test]
        public void GameBoyTestSRLE()
        {
            gbCPU.PC = 0x0;
            gbCPU.E = 0x01;
            gbCPU.F = 0x00;
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x3B));

            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle);
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0x00, gbCPU.E);
            Assert.AreEqual(0x90, gbCPU.F);
        }

        [Test]
        public void GameBoyTestSRLH()
        {
            gbCPU.PC = 0x0;
            gbCPU.H = 0x01;
            gbCPU.F = 0x00;
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x3C));

            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle);
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0x00, gbCPU.H);
            Assert.AreEqual(0x90, gbCPU.F);
        }

        [Test]
        public void GameBoyTestSRLL()
        {
            gbCPU.PC = 0x0;
            gbCPU.L = 0x01;
            gbCPU.F = 0x00;
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x3D));

            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle);
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0x00, gbCPU.L);
            Assert.AreEqual(0x90, gbCPU.F);
        }
/********************************* RR N *********************************************/
        [Test]
        public void GameBoyTestRRA()
        {
            gbCPU.PC = 0x0;
            gbCPU.A = 0x01;
            gbCPU.F = 0x00;
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x1F));

            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle);
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0x00, gbCPU.A);
            Assert.AreEqual(0x90, gbCPU.F);
        }

        [Test]
        public void GameBoyTestRRB()
        {
            gbCPU.PC = 0x0;
            gbCPU.B = 0x01;
            gbCPU.F = 0x10;
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x18));

            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle);
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0x80, gbCPU.B);
            Assert.AreEqual(0x10, gbCPU.F);
        }

        [Test]
        public void GameBoyTestRRC()
        {
            gbCPU.PC = 0x0;
            gbCPU.C = 0x8A;
            gbCPU.F = 0x00;
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x19));

            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle);
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0x45, gbCPU.C);
            Assert.AreEqual(0x00, gbCPU.F);
        }

        [Test]
        public void GameBoyTestRRD()
        {
            gbCPU.PC = 0x0;
            gbCPU.D = 0x8A;
            gbCPU.F = 0x00;
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x1A));

            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle);
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0x45, gbCPU.D);
            Assert.AreEqual(0x00, gbCPU.F);
        }

        [Test]
        public void GameBoyTestRRE()
        {
            gbCPU.PC = 0x0;
            gbCPU.E = 0x8A;
            gbCPU.F = 0x60;
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x1B));

            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle);
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0x45, gbCPU.E);
            Assert.AreEqual(0x00, gbCPU.F); // H and N should be unset
        }

        [Test]
        public void GameBoyTestRRH()
        {
            gbCPU.PC = 0x0;
            gbCPU.H = 0x8A;
            gbCPU.F = 0x60;
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x1C));

            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle);
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0x45, gbCPU.H);
            Assert.AreEqual(0x00, gbCPU.F); // H and N should be unset
        }

        [Test]
        public void GameBoyTestRRL()
        {
            gbCPU.PC = 0x0;
            gbCPU.L = 0x8A;
            gbCPU.F = 0x60;
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x1D));

            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle);
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0x45, gbCPU.L);
            Assert.AreEqual(0x00, gbCPU.F); // H and N should be unset
        }

/********************************* SLA m *********************************************/

        [Test]
        public void GameBoyTestSLAB()
        {
            gbCPU.PC = 0x0;
            gbCPU.B = 0x80;
            gbCPU.F = 0x70;
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x20));

            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle); // CB(4) + SLA(8) 
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0x00, gbCPU.B);
            Assert.AreEqual(0x90, gbCPU.F); // Z and C should be set
        }

        [Test]
        public void GameBoyTestSLAC()
        {
            gbCPU.PC = 0x0;
            gbCPU.C = 0x80;
            gbCPU.F = 0x70;
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x21));

            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle); // CB(4) + SLA(8) 
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0x00, gbCPU.C);
            Assert.AreEqual(0x90, gbCPU.F); // Z and C should be set
        }

        [Test]
        public void GameBoyTestSLAD()
        {
            gbCPU.PC = 0x0;
            gbCPU.D = 0x80;
            gbCPU.F = 0x70;
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x22));

            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle); // CB(4) + SLA(8) 
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0x00, gbCPU.D);
            Assert.AreEqual(0x90, gbCPU.F); // Z and C should be set
        }

        [Test]
        public void GameBoyTestSLAE()
        {
            gbCPU.PC = 0x0;
            gbCPU.E = 0x80;
            gbCPU.F = 0x70;
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x23));

            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle); // CB(4) + SLA(8) 
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0x00, gbCPU.E);
            Assert.AreEqual(0x90, gbCPU.F); // Z and C should be set
        }

        [Test]
        public void GameBoyTestSLAH()
        {
            gbCPU.PC = 0x0;
            gbCPU.H = 0x80;
            gbCPU.F = 0x70;
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x24));

            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle); // CB(4) + SLA(8) 
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0x00, gbCPU.H);
            Assert.AreEqual(0x90, gbCPU.F); // Z and C should be set
        }

        [Test]
        public void GameBoyTestSLAL()
        {
            gbCPU.PC = 0x0;
            gbCPU.L = 0x80;
            gbCPU.F = 0x70;
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x25));

            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle); // CB(4) + SLA(8) 
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0x00, gbCPU.L);
            Assert.AreEqual(0x90, gbCPU.F); // Z and C should be set
        }

        [Test]
        public void GameBoyTestSLAA()
        {
            gbCPU.PC = 0x0;
            gbCPU.A = 0x80;
            gbCPU.F = 0x70;
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x27));

            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle); // CB(4) + SLA(8) 
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0x00, gbCPU.A);
            Assert.AreEqual(0x90, gbCPU.F); // Z and C should be set
        }

/********************************* SLA m *********************************************/

        [Test]
        public void GameBoyTestSRAB()
        {
            gbCPU.PC = 0x0;
            gbCPU.B = 0x8A;
            gbCPU.F = 0xE0; //All is set but C.
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x28));

            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle); // CB(4) + SRA(8) 
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0xC5, gbCPU.B);
            Assert.AreEqual(0x00, gbCPU.F); // Everything unset
        }

        [Test]
        public void GameBoyTestSRAC()
        {
            gbCPU.PC = 0x0;
            gbCPU.C = 0x01;
            gbCPU.F = 0x00; //All is set but C.
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x29));

            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle); // CB(4) + SRA(8) 
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0x00, gbCPU.C);
            Assert.AreEqual(0x90, gbCPU.F); // Everything unset
        }

        [Test]
        public void GameBoyTestSRAD()
        {
            gbCPU.PC = 0x0;
            gbCPU.D = 0x8A;
            gbCPU.F = 0xE0; //All is set but C.
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x2A));

            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle); // CB(4) + SRA(8) 
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0xC5, gbCPU.D);
            Assert.AreEqual(0x00, gbCPU.F); // Everything unset
        }

        [Test]
        public void GameBoyTestSRAE()
        {
            gbCPU.PC = 0x0;
            gbCPU.E = 0x8A;
            gbCPU.F = 0xE0; //All is set but C.
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x2B));

            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle); // CB(4) + SRA(8) 
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0xC5, gbCPU.E);
            Assert.AreEqual(0x00, gbCPU.F); // Everything unset
        }

        [Test]
        public void GameBoyTestSRAH()
        {
            gbCPU.PC = 0x0;
            gbCPU.H = 0x8A;
            gbCPU.F = 0xE0; //All is set but C.
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x2C));

            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle); // CB(4) + SRA(8) 
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0xC5, gbCPU.H);
            Assert.AreEqual(0x00, gbCPU.F); // Everything unset
        }

        [Test]
        public void GameBoyTestSRAL()
        {
            gbCPU.PC = 0x0;
            gbCPU.L = 0x8A;
            gbCPU.F = 0xE0; //All is set but C.
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x2D));

            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle); // CB(4) + SRA(8) 
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0xC5, gbCPU.L);
            Assert.AreEqual(0x00, gbCPU.F); // Everything unset
        }

        [Test]
        public void GameBoyTestSRAA()
        {
            gbCPU.PC = 0x0;
            gbCPU.A = 0x8A;
            gbCPU.F = 0xE0; //All is set but C.
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x2F));

            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle); // CB(4) + SRA(8) 
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0xC5, gbCPU.A);
            Assert.AreEqual(0x00, gbCPU.F); // Everything unset
        }

/********************************* RRA *********************************************/
        [Test]
        public void GameBoyTestRRA2()
        {
            gbCPU.PC = 0x0;
            gbCPU.A = 0x81;
            gbCPU.F = 0x00;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x1F));

            uint cycle = gbCPU.Tick();
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(0x01, gbCPU.PC);
            Assert.AreEqual(0x40, gbCPU.A);
            Assert.AreEqual(0x10, gbCPU.F);
        }
        /*********************************  ADC  *********************************************/
        [Test]
        public void GameBoyTestADCAA()
        {
            gbCPU.PC = 0x0;
            gbCPU.A = 0x00;
            gbCPU.F = 0x00;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x8F));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(0x01, gbCPU.PC);
            Assert.AreEqual(0x00, gbCPU.A);
            Assert.AreEqual(0x80, gbCPU.F);
        }

        [Test]
        public void GameBoyTestADCAB()
        {
            gbCPU.PC = 0x0;
            gbCPU.A = 0xE1;
            gbCPU.B = 0x0F;
            gbCPU.F = 0x10;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x88));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(0x01, gbCPU.PC);
            Assert.AreEqual(0xF1, gbCPU.A);
            Assert.AreEqual(0x20, gbCPU.F);
        }

        [Test]
        public void GameBoyTestADCAC()
        {
            gbCPU.PC = 0x0;
            gbCPU.A = 0xE1;
            gbCPU.C = 0x0F;
            gbCPU.F = 0x10;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x89));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(0x01, gbCPU.PC);
            Assert.AreEqual(0xF1, gbCPU.A);
            Assert.AreEqual(0x20, gbCPU.F);
        }

        [Test]
        public void GameBoyTestADCAD()
        {
            gbCPU.PC = 0x0;
            gbCPU.A = 0xE1;
            gbCPU.D = 0x0F;
            gbCPU.F = 0x10;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x8A));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(0x01, gbCPU.PC);
            Assert.AreEqual(0xF1, gbCPU.A);
            Assert.AreEqual(0x20, gbCPU.F);
        }

        [Test]
        public void GameBoyTestADCAE()
        {
            gbCPU.PC = 0x0;
            gbCPU.A = 0xE1;
            gbCPU.E = 0x0F;
            gbCPU.F = 0x10;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x8B));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(0x01, gbCPU.PC);
            Assert.AreEqual(0xF1, gbCPU.A);
            Assert.AreEqual(0x20, gbCPU.F);
        }

        [Test]
        public void GameBoyTestADCAH()
        {
            gbCPU.PC = 0x0;
            gbCPU.A = 0xE1;
            gbCPU.H = 0x0F;
            gbCPU.F = 0x10;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x8C));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(0x01, gbCPU.PC);
            Assert.AreEqual(0xF1, gbCPU.A);
            Assert.AreEqual(0x20, gbCPU.F);
        }

        [Test]
        public void GameBoyTestADCAL()
        {
            gbCPU.PC = 0x0;
            gbCPU.A = 0xE1;
            gbCPU.L = 0x0F;
            gbCPU.F = 0x10;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x8D));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(0x01, gbCPU.PC);
            Assert.AreEqual(0xF1, gbCPU.A);
            Assert.AreEqual(0x20, gbCPU.F);
        }

        [Test]
        public void GameBoyTestADCAd8()
        {
            gbCPU.PC = 0x0;
            gbCPU.A = 0xE1;
            gbCPU.F = 0x10;
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCE));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x3B));

            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle);
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0x1D, gbCPU.A);
            Assert.AreEqual(0x10, gbCPU.F);
        }

        [Test]
        public void GameBoyTestADCAHL()
        {
            gbCPU.PC = 0x0;
            gbCPU.A = 0xE1;
            gbCPU.F = 0x10;
            gbCPU.H = 0x00;
            gbCPU.L = 0x01;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x8E));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x1E));

            uint cycle = gbCPU.Tick();
            Assert.AreEqual(8, cycle);
            Assert.AreEqual(0x01, gbCPU.PC);
            Assert.AreEqual(0x00, gbCPU.A);
            Assert.AreEqual(0xB0, gbCPU.F);
        }

        [Test]
        public void GameBoyTestSCF()
        {
            //SCF
            gbMemory.WriteToMemory(0,0x37);
            gbCPU.F = 0x60; // H and N set
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x10, gbCPU.F); // H and N unset. C is set.
            Assert.AreEqual(4, cycle);
        }

        [Test]
        public void GameBoyTestCCF()
        {
            //CCF
            gbMemory.WriteToMemory(0,0x3F);
            gbCPU.F = 0x70; // H,N, and C are set
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x00, gbCPU.F); // H,N, and C are unset
            Assert.AreEqual(4, cycle);
        }

        [Test]
        public void GameBoyTestDAA()
        {
            //DAA
            gbCPU.A = 0x45;
            gbCPU.B = 0x38;
            gbCPU.F = 0x50; //N is Set
            gbMemory.WriteToMemory(0,0x80);
            gbMemory.WriteToMemory(1,0x27);
            gbMemory.WriteToMemory(2,0x90);
            gbMemory.WriteToMemory(3,0x27);

            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x00, gbCPU.F); // N is unset
            Assert.AreEqual(0x7D, gbCPU.A);
            Assert.AreEqual(4, cycle);

            cycle = gbCPU.Tick();
            Assert.AreEqual(0x00, gbCPU.F); // C is unset
            Assert.AreEqual(0x83, gbCPU.A);
            Assert.AreEqual(4, cycle);
        } 
    }
}
