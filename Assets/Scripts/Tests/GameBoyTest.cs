using NUnit.Framework;

namespace Tests
{
    public class GameBoyTest
    {
        // A Test behaves as an ordinary method
        GameBoyMemory gbMemory;
        GameBoyCPU gbCPU;
        GameBoyCartiridge gbCart;

        [Test]
        public void GameBoyTestLoadIntoSPTest()
        {
            //Load FFFE into SP.
            gbMemory = new GameBoyMemory(null);
            gbCPU = new GameBoyCPU(gbMemory);
            gbMemory.WriteToMemory(0,0x31);
            gbMemory.WriteToMemory(1,0xFE);
            gbMemory.WriteToMemory(2,0xFF);
            byte opcode = gbMemory.ReadFromMemory(gbCPU.PC++);
            gbCPU.SP = 0x0000;
            gbCPU.HandleInstructions(opcode);
            Assert.AreEqual(0xFFFE, gbCPU.SP);
        }

        [Test]
        public void GameBoyTestLoadIntoMemoryTest()
        {
            //Load FFFE into SP.
            gbMemory = new GameBoyMemory(null);
            gbCPU = new GameBoyCPU(gbMemory);
            gbMemory.WriteToMemory(0,0xE0);
            gbMemory.WriteToMemory(1,0x47);
            byte opcode = gbMemory.ReadFromMemory(gbCPU.PC++);
            gbCPU.A = 0xFC;
            gbMemory.WriteToMemory(0xFF00+0x47,gbCPU.A);
            gbCPU.HandleInstructions(opcode);
            Assert.AreEqual(gbCPU.A, gbMemory.ReadFromMemory(0xFF00+0x47));
        }

        [Test]
        public void GameBoyTestLoadTestSeparateRegisters()
        {
            //Load 0x9FFF into HL
            gbMemory = new GameBoyMemory(null);
            gbCPU = new GameBoyCPU(gbMemory);
            gbMemory.WriteToMemory(0,0x21);
            gbMemory.WriteToMemory(1,0xFF);
            gbMemory.WriteToMemory(2,0x9F);
            byte opcode = gbMemory.ReadFromMemory(gbCPU.PC++);
            gbCPU.H = 0x00;
            gbCPU.L = 0x00;
            gbCPU.HandleInstructions(opcode);
            Assert.AreEqual(0x9F, gbCPU.H);
            Assert.AreEqual(0xFF, gbCPU.L);

            //Load 0x0104 into DE
            gbMemory = new GameBoyMemory(null);
            gbCPU = new GameBoyCPU(gbMemory);
            gbMemory.WriteToMemory(0,0x11);
            gbMemory.WriteToMemory(1,0x04);
            gbMemory.WriteToMemory(2,0x01);
            opcode = gbMemory.ReadFromMemory(gbCPU.PC++);
            gbCPU.D = 0x00;
            gbCPU.E = 0x00;
            gbCPU.HandleInstructions(opcode);
            Assert.AreEqual(0x01, gbCPU.D);
            Assert.AreEqual(0x04, gbCPU.E);
        }

        [Test]
        public void GameBoyTestXORa()
        {
            //XOR A=0x00 with A
            gbMemory = new GameBoyMemory(null);
            gbCPU = new GameBoyCPU(gbMemory);
            gbMemory.WriteToMemory(0,0xAF);
            byte opcode = gbMemory.ReadFromMemory(gbCPU.PC++);
            gbCPU.A = 0x0;
            gbCPU.F = 0x0;
            gbCPU.HandleInstructions(opcode);
            Assert.AreEqual(0x0, gbCPU.A);
            Assert.AreEqual(0x80, gbCPU.F);

            //XOR A=0xF0 with A
            //TODO: XOR with different register opcode needs to be implemented.
            /*gbMemory = new GameBoyMemory(null);
            gbCPU = new GameBoyCPU(gbMemory);
            gbMemory.WriteToMemory(0,0xAF);
            byte opcode = gbMemory.ReadFromMemory(gbCPU.PC++);
            gbCPU.A = 0xFF;
            gbCPU.F = 0x80;
            gbCPU.HandleInstructions(opcode);
            Assert.AreEqual(0xF0, gbCPU.A);
            Assert.AreEqual(0x0, gbCPU.F);*/
        }

        [Test]
        public void GameBoyTestLDDHLADEC()
        {
            gbMemory = new GameBoyMemory(null);
            gbCPU = new GameBoyCPU(gbMemory);
            gbMemory.WriteToMemory(0,0x32);
            byte opcode = gbMemory.ReadFromMemory(gbCPU.PC++);
            gbCPU.A = 0x00;
            gbCPU.H = 0x9F;
            gbCPU.L = 0xFF;
            gbCPU.HandleInstructions(opcode);
            Assert.AreEqual(0x9F, gbCPU.H);
            Assert.AreEqual(0xFE, gbCPU.L);
            Assert.AreEqual(0, gbMemory.ReadFromMemory(0x9FFF));
        }

        [Test]
        public void GameBoyTestLDDHLAINC()
        {
            //TODO
            gbMemory = new GameBoyMemory(null);
            gbCPU = new GameBoyCPU(gbMemory);
            gbMemory.WriteToMemory(0,0x22);
            byte opcode = gbMemory.ReadFromMemory(gbCPU.PC++);
            gbCPU.A = 0x56;
            gbCPU.H = 0xFF;
            gbCPU.L = 0xFF;
            gbCPU.HandleInstructions(opcode);
            Assert.AreEqual(0x00, gbCPU.H);
            Assert.AreEqual(0x00, gbCPU.L);
            Assert.AreEqual(0x56, gbMemory.ReadFromMemory(0xFFFF));
        }

        [Test]
        public void GameBoyTestLDDHLA()
        {
            gbMemory = new GameBoyMemory(null);
            gbCPU = new GameBoyCPU(gbMemory);
            gbMemory.WriteToMemory(0,0x77);
            byte opcode = gbMemory.ReadFromMemory(gbCPU.PC++);
            gbCPU.A = 0x22;
            gbCPU.H = 0xFF;
            gbCPU.L = 0x25;
            gbCPU.HandleInstructions(opcode);
            Assert.AreEqual(0xFF, gbCPU.H);
            Assert.AreEqual(0x25, gbCPU.L);
            Assert.AreEqual(gbCPU.A, gbMemory.ReadFromMemory(0xFF25));
        }

        [Test]
        public void GameBoyTestBitTest()
        {
            gbMemory = new GameBoyMemory(null);
            gbCPU = new GameBoyCPU(gbMemory);
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x7C);
            byte opcode = gbMemory.ReadFromMemory(gbCPU.PC++);
            gbCPU.H = 0x80;
            gbCPU.F = 0x0;
            gbCPU.HandleInstructions(opcode);
            Assert.AreEqual(0x20, gbCPU.F);

            gbMemory = new GameBoyMemory(null);
            gbCPU = new GameBoyCPU(gbMemory);
            gbMemory.WriteToMemory(0,0xCB);
            gbMemory.WriteToMemory(1,0x7C);
            opcode = gbMemory.ReadFromMemory(gbCPU.PC++);
            gbCPU.H = 0x40;
            gbCPU.F = 0x0;
            gbCPU.HandleInstructions(opcode);
            Assert.AreEqual(0xA0, gbCPU.F);
        }

        [Test]
        public void GameBoyTestJRCCNZ()
        {
            gbMemory = new GameBoyMemory(null);
            gbCPU = new GameBoyCPU(gbMemory);
            gbMemory.WriteToMemory(0,0x20);
            gbMemory.WriteToMemory(1,0xFE); // -2 jump back memory[0]
            byte opcode = gbMemory.ReadFromMemory(gbCPU.PC++);
            gbCPU.F = 0x00;
            gbCPU.HandleInstructions(opcode);
            Assert.AreEqual(0x0, gbCPU.PC);
            Assert.AreEqual(gbMemory.ReadFromMemory(0), gbMemory.ReadFromMemory(gbCPU.PC));

            gbCPU.PC++;
            gbCPU.F = 0x80;
            gbMemory.WriteToMemory(2,0xAA);
            gbCPU.HandleInstructions(opcode);
            Assert.AreEqual(0x2, gbCPU.PC);
            Assert.AreEqual(gbMemory.ReadFromMemory(2), gbMemory.ReadFromMemory(gbCPU.PC));
        }

        [Test]
        public void GameBoyTestLDNRC()
        {
            //Load 0x11 into C
            gbMemory = new GameBoyMemory(null);
            gbCPU = new GameBoyCPU(gbMemory);
            GameBoyCPU.ClockCycle = 0;
            gbMemory.WriteToMemory(0, 0x0E);
            gbMemory.WriteToMemory(1, 0x11);
            byte opcode = gbMemory.ReadFromMemory(gbCPU.PC++);
            gbCPU.C = 0x00;
            gbCPU.HandleInstructions(opcode);
            Assert.AreEqual(gbMemory.ReadFromMemory(1), gbCPU.C);
            Assert.AreEqual(8, GameBoyCPU.ClockCycle);
        }

        [Test]
        public void GameBoyTestJRCCNZManyOpsFlow()
        {
            gbMemory = new GameBoyMemory(null);
            gbCPU = new GameBoyCPU(gbMemory);
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
                gbCPU.HandleInstructions(gbMemory.ReadFromMemory(gbCPU.PC++));
                // for test bit 7
                gbCPU.HandleInstructions(gbMemory.ReadFromMemory(gbCPU.PC++));
                // for jumping
                gbCPU.HandleInstructions(gbMemory.ReadFromMemory(gbCPU.PC++));
            }
            Assert.AreEqual(0x80, gbCPU.H);
            Assert.AreEqual(0x00, gbCPU.L);
        }

        [Test]
        public void GameBoyTestLDCR()
        {
            //Load 0x11 into C
            gbMemory = new GameBoyMemory(null);
            gbCPU = new GameBoyCPU(gbMemory);
            GameBoyCPU.ClockCycle = 0;
            gbMemory.WriteToMemory(0, 0xE2);
            byte opcode = gbMemory.ReadFromMemory(gbCPU.PC++);
            gbCPU.C = 0x11;
            gbCPU.A = 0x80;
            gbCPU.HandleInstructions(opcode);
            Assert.AreEqual(gbMemory.ReadFromMemory((ushort)(0xFF00+gbCPU.C)), gbCPU.A);
            Assert.AreEqual(8, GameBoyCPU.ClockCycle);
        }

        [Test]
        public void GameBoyTestINC()
        {
            //INC C
            gbMemory = new GameBoyMemory(null);
            gbCPU = new GameBoyCPU(gbMemory);
            GameBoyCPU.ClockCycle = 0;
            gbMemory.WriteToMemory(0, 0x0C);
            byte opcode = gbMemory.ReadFromMemory(gbCPU.PC++);
            gbCPU.C = 0xFF;
            gbCPU.F = 0x00;
            gbCPU.HandleInstructions(opcode);
            Assert.AreEqual(0x00, gbCPU.C);
            Assert.AreEqual(4, GameBoyCPU.ClockCycle);
            Assert.AreEqual(0xA0, gbCPU.F);
        }

        [Test]
        public void GameBoyTestINCHL()
        {
            //INC HL - page 97
            gbMemory = new GameBoyMemory(null);
            gbCPU = new GameBoyCPU(gbMemory);
            GameBoyCPU.ClockCycle = 0;
            gbMemory.WriteToMemory(0, 0x23);
            byte opcode = gbMemory.ReadFromMemory(gbCPU.PC++);
            gbCPU.H = 0x23;
            gbCPU.L = 0x5F;
            gbCPU.F = 0x00;
            gbCPU.HandleInstructions(opcode);
            Assert.AreEqual(0x23, gbCPU.H);
            Assert.AreEqual(0x60, gbCPU.L);
            Assert.AreEqual(8, GameBoyCPU.ClockCycle);
            Assert.AreEqual(0x00, gbCPU.F);
        }

        [Test]
        public void GameBoyTestCPN()
        {
            //CP - page 95
            gbMemory = new GameBoyMemory(null);
            gbCPU = new GameBoyCPU(gbMemory);
            GameBoyCPU.ClockCycle = 0;
            Assert.True(gbMemory.WriteToMemory(0, 0xFE));
            Assert.True(gbMemory.WriteToMemory(1, 0x3C));
            byte opcode = gbMemory.ReadFromMemory(gbCPU.PC++);
            gbCPU.A = 0x3C;
            gbCPU.F = 0x00;
            gbCPU.HandleInstructions(opcode);
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(8, GameBoyCPU.ClockCycle);
            Assert.AreEqual(0xC0, gbCPU.F);
        }

        [Test]
        public void GameBoyTestINCFlags()
        {
            //INC C
            gbMemory = new GameBoyMemory(null);
            gbCPU = new GameBoyCPU(gbMemory);
            GameBoyCPU.ClockCycle = 0;
            gbMemory.WriteToMemory(0, 0x0C);
            byte opcode = gbMemory.ReadFromMemory(gbCPU.PC++);
            gbCPU.C = 0xFD;
            gbCPU.F = 0xFF;
            gbCPU.HandleInstructions(opcode);
            Assert.AreEqual(0xFE, gbCPU.C);
            Assert.AreEqual(4, GameBoyCPU.ClockCycle);
            Assert.AreEqual(0x10, gbCPU.F);
        }

        [Test]
        public void GameBoyTestDEC()
        {
            //DEC C
            gbMemory = new GameBoyMemory(null);
            gbCPU = new GameBoyCPU(gbMemory);
            GameBoyCPU.ClockCycle = 0;
            gbMemory.WriteToMemory(0, 0x05);
            byte opcode = gbMemory.ReadFromMemory(gbCPU.PC++);
            gbCPU.B = 0x01;
            gbCPU.F = 0x00;
            gbCPU.HandleInstructions(opcode);
            Assert.AreEqual(0x00, gbCPU.B);
            Assert.AreEqual(4, GameBoyCPU.ClockCycle);
            Assert.AreEqual(0xC0, gbCPU.F);
        }

        [Test]
        public void GameBoyTestDECFlag()
        {
            //DEC C
            gbMemory = new GameBoyMemory(null);
            gbCPU = new GameBoyCPU(gbMemory);
            GameBoyCPU.ClockCycle = 0;
            gbMemory.WriteToMemory(0, 0x35);
            gbMemory.WriteToMemory(0x0001, 0x00);
            gbCPU.PC = 0x0000;
            byte opcode = gbMemory.ReadFromMemory(gbCPU.PC++);
            gbCPU.H = 0x00;
            gbCPU.L = 0x01;
            gbCPU.F = 0x00;
            gbCPU.HandleInstructions(opcode);
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
            gbCPU = new GameBoyCPU(gbMemory);
            GameBoyCPU.ClockCycle = 0;
            gbMemory.WriteToMemory(0x0, 0x1A);
            byte opcode = gbMemory.ReadFromMemory(gbCPU.PC++);
            gbCPU.A = 0x00;
            gbCPU.D = 0x01;
            gbCPU.E = 0x04;
            gbCPU.HandleInstructions(opcode);
            Assert.AreEqual(0x5F, gbCPU.A);
            Assert.AreEqual(8, GameBoyCPU.ClockCycle);
        }

        [Test]
        public void GameBoyTestCallNN()
        {
            gbMemory = new GameBoyMemory(null);
            gbCPU = new GameBoyCPU(gbMemory);
            GameBoyCPU.ClockCycle = 0;
            gbCPU.PC = 0x8000;
            Assert.True(gbMemory.WriteToMemory(0x8000, 0xCD));
            Assert.True(gbMemory.WriteToMemory(0x8001, 0x34));
            Assert.True(gbMemory.WriteToMemory(0x8002, 0x12));
            byte opcode = gbMemory.ReadFromMemory(gbCPU.PC++);
            gbCPU.SP = 0xFFFE;
            gbCPU.HandleInstructions(opcode);
            Assert.AreEqual(0x1234, gbCPU.PC);
            Assert.AreEqual(0xFFFC, gbCPU.SP);
            Assert.AreEqual(0x80, gbMemory.ReadFromMemory(0xFFFD));
            Assert.AreEqual(0x03, gbMemory.ReadFromMemory(0xFFFC));
            Assert.AreEqual(24, GameBoyCPU.ClockCycle);
        }

        [Test]
        public void GameBoyTestRet()
        {
            gbMemory = new GameBoyMemory(null);
            gbCPU = new GameBoyCPU(gbMemory);
            GameBoyCPU.ClockCycle = 0;
            gbCPU.PC = 0x8000;
            Assert.True(gbMemory.WriteToMemory(0x8000, 0xCD));
            Assert.True(gbMemory.WriteToMemory(0x8001, 0x03));
            Assert.True(gbMemory.WriteToMemory(0x8002, 0x80));
            Assert.True(gbMemory.WriteToMemory(0x8003, 0x00)); // NOOP
            Assert.True(gbMemory.WriteToMemory(0x8004, 0xC9));
            byte opcode = gbMemory.ReadFromMemory(gbCPU.PC++);
            gbCPU.SP = 0xFFFE;
            gbCPU.HandleInstructions(opcode); // Handle CALL FIRST
            Assert.AreEqual(0x8003, gbCPU.PC);
            Assert.AreEqual(0xFFFC, gbCPU.SP);
            Assert.AreEqual(0x80, gbMemory.ReadFromMemory(0xFFFD));
            Assert.AreEqual(0x03, gbMemory.ReadFromMemory(0xFFFC));
            Assert.AreEqual(24, GameBoyCPU.ClockCycle);

            opcode = gbMemory.ReadFromMemory(gbCPU.PC++);
            gbCPU.HandleInstructions(opcode); // Handle NOOP
            Assert.AreEqual(0x8004, gbCPU.PC);
            Assert.AreEqual(0xFFFC, gbCPU.SP);
            Assert.AreEqual(0x80, gbMemory.ReadFromMemory(0xFFFD));
            Assert.AreEqual(0x03, gbMemory.ReadFromMemory(0xFFFC));
            Assert.AreEqual(28, GameBoyCPU.ClockCycle);

            opcode = gbMemory.ReadFromMemory(gbCPU.PC++);
            gbCPU.HandleInstructions(opcode); // Handle RET 
            Assert.AreEqual(0x8003, gbCPU.PC);
            Assert.AreEqual(0xFFFE, gbCPU.SP);
            Assert.AreEqual(0x80, gbMemory.ReadFromMemory(0xFFFD));
            Assert.AreEqual(0x03, gbMemory.ReadFromMemory(0xFFFC));
            Assert.AreEqual(44, GameBoyCPU.ClockCycle); //CALL(24)+RET(16)+NOOP(4) time
        }

        [Test]
        public void GameBoyTestPush()
        {
            gbMemory = new GameBoyMemory(null);
            gbCPU = new GameBoyCPU(gbMemory);
            GameBoyCPU.ClockCycle = 0;
            gbCPU.PC = 0x00;
            Assert.True(gbMemory.WriteToMemory(0x00, 0xC5));
            byte opcode = gbMemory.ReadFromMemory(gbCPU.PC++);
            gbCPU.B = 0x3C;
            gbCPU.C = 0x5F;
            gbCPU.SP = 0xFFFE;
            gbCPU.HandleInstructions(opcode);
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
            gbCPU = new GameBoyCPU(gbMemory);
            GameBoyCPU.ClockCycle = 0;
            gbCPU.PC = 0x00;
            Assert.True(gbMemory.WriteToMemory(0x00, 0xC1));
            Assert.True(gbMemory.WriteToMemory(0xFFFD, 0x3C));
            Assert.True(gbMemory.WriteToMemory(0xFFFC, 0x5F));
            byte opcode = gbMemory.ReadFromMemory(gbCPU.PC++);
            gbCPU.B = 0x00;
            gbCPU.C = 0x00;
            gbCPU.SP = 0xFFFC;
            gbCPU.HandleInstructions(opcode);
            Assert.AreEqual(0x1, gbCPU.PC);
            Assert.AreEqual(0xFFFE, gbCPU.SP);
            Assert.AreEqual(0x3C, gbCPU.B);
            Assert.AreEqual(0x5F, gbCPU.C);
            Assert.AreEqual(12, GameBoyCPU.ClockCycle);
        }

        [Test]
        public void GameBoyTestLDRR()
        {
            gbMemory = new GameBoyMemory(null);
            gbCPU = new GameBoyCPU(gbMemory);
            GameBoyCPU.ClockCycle = 0;
            gbCPU.PC = 0x0;
            gbCPU.C = 0xF0;
            gbCPU.A = 0xFF;
            Assert.True(gbMemory.WriteToMemory(0x0, 0x4F));
            byte opcode = gbMemory.ReadFromMemory(gbCPU.PC++);
            gbCPU.HandleInstructions(opcode);
            Assert.AreEqual(4, GameBoyCPU.ClockCycle);
            Assert.AreEqual(gbCPU.C, gbCPU.A);
        }

        [Test]
        public void GameBoyTestRL()
        {
            gbMemory = new GameBoyMemory(null);
            gbCPU = new GameBoyCPU(gbMemory);
            GameBoyCPU.ClockCycle = 0;
            gbCPU.PC = 0x0;
            gbCPU.L = 0x80;
            gbCPU.F = 0x00;
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x15));
            byte opcode = gbMemory.ReadFromMemory(gbCPU.PC++);
            gbCPU.HandleInstructions(opcode);
            Assert.AreEqual(8, GameBoyCPU.ClockCycle);
            Assert.AreEqual(0x02, gbCPU.PC);
            Assert.AreEqual(0x00, gbCPU.L);
            Assert.AreEqual(0x90, gbCPU.F);

            gbMemory = new GameBoyMemory(null);
            gbCPU = new GameBoyCPU(gbMemory);
            GameBoyCPU.ClockCycle = 0;
            gbCPU.PC = 0x0;
            gbCPU.H = 0x00;
            gbCPU.L = 0x80;
            Assert.True(gbMemory.WriteToMemory(0x0, 0xCB));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x16));
            Assert.True(gbMemory.WriteToMemory(0x80, 0x11)); //HL = 0x80
            opcode = gbMemory.ReadFromMemory(gbCPU.PC++);
            gbCPU.HandleInstructions(opcode);
            Assert.AreEqual(16, GameBoyCPU.ClockCycle);
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
            gbCPU = new GameBoyCPU(gbMemory);
            GameBoyCPU.ClockCycle = 0;
            gbCPU.PC = 0x0;
            gbCPU.A = 0x95;
            gbCPU.F = 0x90; //Z and C are set
            Assert.True(gbMemory.WriteToMemory(0x0, 0x17));
            byte opcode = gbMemory.ReadFromMemory(gbCPU.PC++);
            gbCPU.HandleInstructions(opcode);
            Assert.AreEqual(4, GameBoyCPU.ClockCycle);
            Assert.AreEqual(0x01, gbCPU.PC);
            Assert.AreEqual(0x2B, gbCPU.A);
            Assert.AreEqual(0x10, gbCPU.F); //Z should be unset
        }

        [Test]
        public void GameBoyTestLDNNA()
        {
            gbMemory = new GameBoyMemory(null);
            gbCPU = new GameBoyCPU(gbMemory);
            GameBoyCPU.ClockCycle = 0;
            gbCPU.PC = 0x0;
            gbCPU.A = 0x19;
            Assert.True(gbMemory.WriteToMemory(0x0, 0xEA));
            Assert.True(gbMemory.WriteToMemory(0x1, 0x10));
            Assert.True(gbMemory.WriteToMemory(0x2, 0x99));

            byte opcode = gbMemory.ReadFromMemory(gbCPU.PC++);
            gbCPU.HandleInstructions(opcode);
            Assert.AreEqual(16, GameBoyCPU.ClockCycle);
            Assert.AreEqual(0x03, gbCPU.PC);
            Assert.AreEqual(0x19, gbCPU.A);
            Assert.AreEqual(0x19, gbMemory.ReadFromMemory(0x9910));
        }
    }
}
