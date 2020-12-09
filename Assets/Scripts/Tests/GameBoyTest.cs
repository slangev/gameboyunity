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
            AND (HL)
            LD A, C
            RETI
            PUSH *
            POPS *
            JP (HL)
            LD L,E
            LD L,B
            LD A,(HL)
            DEC DE
            CALL NZ,a16
            INC L
        */

        [SetUp] 
        public void Init() {
            gbMemory = new GameBoyMemory(null);
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
            Assert.AreEqual(12, cycle);
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
            gbMemory.WriteToMemory(1,0x44);
            gbCPU.A = 0x0;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(gbCPU.A, gbMemory.ReadFromMemory(0xFF00+0x44));
            Assert.AreEqual(cycle, GameBoyCPU.ClockCycle);
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
            gbCPU.Tick();
            Assert.AreEqual(gbMemory.ReadFromMemory(1), gbCPU.C);
            Assert.AreEqual(8, GameBoyCPU.ClockCycle);
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
        public void GameBoyTestLDRNL()
        {
            //Load 0x0F into L
            gbMemory.WriteToMemory(0, 0x2E);
            gbMemory.WriteToMemory(1, 0x0F);
            gbCPU.L = 0x00;
            gbCPU.Tick();
            Assert.AreEqual(gbMemory.ReadFromMemory(1), gbCPU.L);
            Assert.AreEqual(8, GameBoyCPU.ClockCycle);
        }

        [Test]
        public void GameBoyTestLDRRCA()
        {
            //Load A into C
            GameBoyCPU.ClockCycle = 0;
            gbCPU.PC = 0x0;
            gbCPU.C = 0xF0;
            gbCPU.A = 0xFF;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x4F));
            gbCPU.Tick();
            Assert.AreEqual(4, GameBoyCPU.ClockCycle);
            Assert.AreEqual(gbCPU.C, gbCPU.A);
        }

        [Test]
        public void GameBoyTestLDRRAC()
        {
            //Load C into A
            gbCPU.PC = 0x0;
            gbCPU.A = 0xF0;
            gbCPU.C = 0xFF;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x4F));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(gbCPU.C, gbCPU.A);
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

/*********************************************CONTROL TESTs*********************************************/
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

        [Test]
        public void GameBoyTestRES0A()
        {
            // RES 0, A
            gbCPU.A = 0x0F;
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x87));
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(12, cycle);
            Assert.AreEqual(0x0E, gbCPU.A);
        }
        
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
            Assert.AreEqual(12, cycle);
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
            Assert.AreEqual(20, cycle);
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
            Assert.AreEqual(12, cycle);
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
        public void GameBoyTestXORa()
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
        public void GameBoyTestXORc()
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
        public void GameBoyTestORa()
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
        public void GameBoyTestORd8()
        {
            //XOR A=0x5A with 3
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

        [Test]
        public void GameBoyTestBitTest()
        {
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x7C);
            gbCPU.H = 0x80;
            gbCPU.F = 0x0;
            gbCPU.Tick();
            Assert.AreEqual(0x20, gbCPU.F);

            gbMemory = new GameBoyMemory(null);
            gbInterrupts = new GameBoyInterrupts(gbMemory);
            gbTimer = new GameBoyTimer(gbMemory,gbInterrupts);
            gbCPU = new GameBoyCPU(gbMemory,gbInterrupts);
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x7C);
            gbCPU.H = 0x40;
            gbCPU.F = 0x0;
            gbCPU.Tick();
            Assert.AreEqual(0xA0, gbCPU.F);
        }

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
            gbMemory.WriteToMemory(0x8000,0xEF);
            gbCPU.PC = 0x8000;
            gbCPU.SP = 0xFFFE;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x28, gbCPU.PC);
            Assert.AreEqual(0xFFFC, gbCPU.SP);
            Assert.AreEqual(0x01,gbMemory.ReadFromMemory((ushort)(gbCPU.SP)));
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
        public void GameBoyTestJRCCNZManyOpsFlow()
        {
            gbMemory = new GameBoyMemory(null);
            gbInterrupts = new GameBoyInterrupts(gbMemory);
            gbTimer = new GameBoyTimer(gbMemory,gbInterrupts);
            gbCPU = new GameBoyCPU(gbMemory,gbInterrupts);
            gbMemory.WriteToMemory(0,0x32);
            gbMemory.WriteToMemory(1,0xCB);
            gbMemory.WriteToMemory(2,0x7C);
            gbMemory.WriteToMemory(3,0x20);
            gbMemory.WriteToMemory(4,0xFB); // -5 jump back memory[0]
            gbCPU.A = 0x00;
            gbCPU.H = 0x9F;
            gbCPU.L = 0xFF;
            gbCPU.PC = 0x00;
            //8191
            for(int i = 0; i < 0x9fff-0x8000; i++) {
                // for HL-,A
                gbCPU.Tick();
                // for test bit 7
                gbCPU.Tick();
                // for jumping
                gbCPU.Tick();
            }
            Assert.AreEqual(0x80, gbCPU.H);
            Assert.AreEqual(0x00, gbCPU.L);
        }

        [Test]
        public void GameBoyTestJRCCNZManyOpsFlowTimerInterrupts()
        {
            gbMemory = new GameBoyMemory(null);
            gbInterrupts = new GameBoyInterrupts(gbMemory);
            gbTimer = new GameBoyTimer(gbMemory,gbInterrupts);
            gbCPU = new GameBoyCPU(gbMemory,gbInterrupts);
            gbMemory.WriteToMemory(0,0x32);
            gbMemory.WriteToMemory(1,0xCB);
            gbMemory.WriteToMemory(2,0x7C);
            gbMemory.WriteToMemory(3,0x20);
            gbMemory.WriteToMemory(4,0xFB); // -5 jump back memory[0]
            gbMemory.WriteToMemory(GameBoyTimer.TAC, 0x07); // Turn on Tac and set clock rate to 256
            gbMemory.WriteToMemory(GameBoyTimer.TMA, 0x20);
            gbCPU.A = 0x00;
            gbCPU.H = 0x9F;
            gbCPU.L = 0xFF;
            gbCPU.PC = 0x00;
            uint cycles = 0;
            //8191
            for(int i = 0; i < 0x9fff-0x8000; i++) {
                cycles += gbCPU.Tick();
                gbTimer.UpdateTimers(cycles);
                cycles = 0;
                cycles += gbCPU.Tick();
                gbTimer.UpdateTimers(cycles);
                cycles = 0;
                cycles += gbCPU.Tick();
                gbTimer.UpdateTimers(cycles);
                cycles = 0;
            }
            Assert.AreEqual(0x80, gbCPU.H);
            Assert.AreEqual(0x00, gbCPU.L);
            Assert.AreEqual(229348, GameBoyCPU.ClockCycle);

            Assert.AreEqual(0x04, gbMemory.ReadFromMemory(GameBoyInterrupts.IF)); //timer interrupts are requesting (If this test case was testing graphics, this would be 0x7)
            Assert.AreEqual(0xDF, gbMemory.ReadFromMemory(GameBoyTimer.TIMA)); // Was set to TMA and some increments later
            Assert.AreEqual(0x7F, gbMemory.ReadFromMemory(GameBoyTimer.DIV));
            Assert.AreEqual(gbMemory.ReadFromMemory(GameBoyTimer.TMA), 0x20);
        }

        [Test]
        public void GameBoyTestJRCCNZManyOpsGraphicsInterrupts()
        {
            gbMemory.WriteToMemory(0,0x32);
            gbMemory.WriteToMemory(1,0xCB);
            gbMemory.WriteToMemory(2,0x7C);
            gbMemory.WriteToMemory(3,0x20);
            gbMemory.WriteToMemory(4,0xFB); // -5 jump back memory[0]
            gbMemory.WriteToMemory(GameBoyTimer.TAC, 0x07); // Turn on Tac and set clock rate to 256
            gbMemory.WriteToMemory(GameBoyTimer.TMA, 0x20);
            gbMemory.WriteToMemory(GameBoyGraphic.LCDCAddr, 0x91); // Turn on the LCDC
            gbMemory.WriteToMemory(GameBoyGraphic.STATAddr, 0xFC); // Turn on the STAT Interrupts
            gbCPU.A = 0x00;
            gbCPU.H = 0x9F;
            gbCPU.L = 0xFF;
            gbCPU.PC = 0x00;
            uint cycles = 0;
            //8191
            for(int i = 0; i < 0x9fff-0x8000; i++) {
                cycles += gbCPU.Tick();
                gbTimer.UpdateTimers(cycles);
                gbGraphics.UpdateGraphics(cycles);
                cycles = 0;
                cycles += gbCPU.Tick();
                gbTimer.UpdateTimers(cycles);
                gbGraphics.UpdateGraphics(cycles);
                cycles = 0;
                cycles += gbCPU.Tick();
                gbTimer.UpdateTimers(cycles);
                gbGraphics.UpdateGraphics(cycles);
                cycles = 0;
            }
            Assert.AreEqual(0x80, gbCPU.H);
            Assert.AreEqual(0x00, gbCPU.L);
            Assert.AreEqual(229348, GameBoyCPU.ClockCycle);

            Assert.AreEqual(0x07, gbMemory.ReadFromMemory(GameBoyInterrupts.IF)); //v-blank, LCD stat, and timer interrupts are requesting
            Assert.AreEqual(0xDF, gbMemory.ReadFromMemory(GameBoyTimer.TIMA)); // Was set to TMA and some increments later
            Assert.AreEqual(0x7F, gbMemory.ReadFromMemory(GameBoyTimer.DIV));
            Assert.AreEqual(gbMemory.ReadFromMemory(GameBoyTimer.TMA), 0x20);
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
            Assert.AreEqual(12, cycle);
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
        public void GameBoyTestCPN()
        {
            //CP Compare - page 95 compare
            gbMemory = new GameBoyMemory(null);
            gbTimer = new GameBoyTimer(gbMemory,gbInterrupts);
            gbInterrupts = new GameBoyInterrupts(gbMemory);
            gbCPU = new GameBoyCPU(gbMemory,gbInterrupts);
            GameBoyCPU.ClockCycle = 0;
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

        [Test]
        public void GameBoyTestSUBs()
        {
            //SUB - page 93 Subtract from A
            Assert.True(gbMemory.WriteToMemory(0, 0x90));
            gbCPU.A = 0x3E;
            gbCPU.B = 0x3E;
            gbCPU.F = 0x00;
            uint cycle = gbCPU.Tick();
            Assert.AreEqual(0x00, gbCPU.A);
            Assert.AreEqual(4, GameBoyCPU.ClockCycle);
            Assert.AreEqual(4, cycle);
            Assert.AreEqual(0xC0, gbCPU.F);
        }

        [Test]
        public void GameBoyTestINCFlags()
        {
            //INC C
            gbMemory = new GameBoyMemory(null);
            gbTimer = new GameBoyTimer(gbMemory,gbInterrupts);
            gbInterrupts = new GameBoyInterrupts(gbMemory);
            gbCPU = new GameBoyCPU(gbMemory,gbInterrupts);
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
            gbCPU.Tick();
            Assert.AreEqual(0x00, gbCPU.B);
            Assert.AreEqual(4, GameBoyCPU.ClockCycle);
            Assert.AreEqual(0xC0, gbCPU.F);
        }

        [Test]
        public void GameBoyTestDECA()
        {
            //DEC A
            gbMemory = new GameBoyMemory(null);
            gbTimer = new GameBoyTimer(gbMemory,gbInterrupts);
            gbInterrupts = new GameBoyInterrupts(gbMemory);
            gbCPU = new GameBoyCPU(gbMemory,gbInterrupts);
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
        public void GameBoyTestCallNZ()
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
            Assert.AreEqual(12, cycle);
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

        [Test]
        public void GameBoyTestRL()
        {
            gbMemory = new GameBoyMemory(null);
            gbTimer = new GameBoyTimer(gbMemory,gbInterrupts);
            gbInterrupts = new GameBoyInterrupts(gbMemory);
            gbCPU = new GameBoyCPU(gbMemory,gbInterrupts);
            GameBoyCPU.ClockCycle = 0;
            gbCPU.PC = 0x0;
            gbCPU.L = 0x80;
            gbCPU.F = 0x00;
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x15));
            gbCPU.Tick();
            Assert.AreEqual(12, GameBoyCPU.ClockCycle);
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0x00, gbCPU.L);
            Assert.AreEqual(0x90, gbCPU.F);

            gbMemory = new GameBoyMemory(null);
            gbTimer = new GameBoyTimer(gbMemory,gbInterrupts);
            gbInterrupts = new GameBoyInterrupts(gbMemory);
            gbCPU = new GameBoyCPU(gbMemory,gbInterrupts);
            GameBoyCPU.ClockCycle = 0;
            gbCPU.PC = 0x0;
            gbCPU.H = 0x00;
            gbCPU.L = 0x80;
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x16));
            Assert.True(gbMemory.WriteToMemory(0x80, 0x11)); //HL = 0x80
            gbCPU.Tick();
            Assert.AreEqual(20, GameBoyCPU.ClockCycle);
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0x00, gbCPU.H);
            Assert.AreEqual(0x80, gbCPU.L);
            Assert.AreEqual(0x00, gbCPU.F);
            Assert.AreEqual(0x22, gbMemory.ReadFromMemory(0x80)); //HL = 0x80
        }

        [Test]
        public void GameBoyTestRLA()
        {
            gbMemory = new GameBoyMemory(null);
            gbTimer = new GameBoyTimer(gbMemory,gbInterrupts);
            gbInterrupts = new GameBoyInterrupts(gbMemory);
            gbCPU = new GameBoyCPU(gbMemory,gbInterrupts);
            GameBoyCPU.ClockCycle = 0;
            gbCPU.PC = 0x0;
            gbCPU.A = 0x95;
            gbCPU.F = 0x90; //Z and C are set
            Assert.True(gbMemory.WriteToMemory(0x0, 0x17));
            gbCPU.Tick();
            Assert.AreEqual(4, GameBoyCPU.ClockCycle);
            Assert.AreEqual(0x01, gbCPU.PC);
            Assert.AreEqual(0x2B, gbCPU.A);
            Assert.AreEqual(0x10, gbCPU.F); //Z should be unset
        }

        [Test]
        public void GameBoyTestLDNNA()
        {
            gbMemory = new GameBoyMemory(null);
            gbTimer = new GameBoyTimer(gbMemory,gbInterrupts);
            gbInterrupts = new GameBoyInterrupts(gbMemory);
            gbCPU = new GameBoyCPU(gbMemory,gbInterrupts);
            GameBoyCPU.ClockCycle = 0;
            gbCPU.PC = 0x0;
            gbCPU.A = 0x19;
            Assert.True(gbMemory.WriteToMemory(0x0, 0xEA));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x10));
            Assert.True(gbMemory.WriteToMemory(0x2, 0x99));

            gbCPU.Tick();
            Assert.AreEqual(16, GameBoyCPU.ClockCycle);
            Assert.AreEqual(0x03, gbCPU.PC);
            Assert.AreEqual(0x19, gbCPU.A);
            Assert.AreEqual(0x19, gbMemory.ReadFromMemory(0x9910));
        }
    }
}
