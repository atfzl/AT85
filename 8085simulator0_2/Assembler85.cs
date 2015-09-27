using System;
using System.Collections.Generic;   // for List
using System.Windows.Forms; // for MessageBox in exception

namespace _8085simulator0_2
{
    public class AssemblerErrorException : System.Exception
    {
        public AssemblerErrorException(string message):base(message)
        {
            MessageBox.Show(message);
        }
    }

    class Assembler85
    {
        public byte[] RAM = new byte[0x10000];  // total RAM of 65536 bytes( 0x0000 - 0xffff )
        public byte[] PORT = new byte[0x100];   // total 256 PORTS ( 0x00 - 0xff )
        private int usedRam=0;                  
        byte prevRegisterA;                     // used for finding flags
        public int[] RAMprogramLine = new int[0x10000];// it gives the linenumber for a
                                                       // given byte of program


        //
        // Address Symbol Table
        List<string> addressSymbolTableLabel = new List<string>();
        List<int> addressSymbolTableValue = new List<int>();
        //
        

        string[] program;   // edited program for running second pass
        string[] listProgram;   // program listing
        string binaryInstruction = "00000000";
        byte byteInstruction = 00;
        int startLocation;  // strat location of the program

        public byte[] register = new byte[8]; /*
                                               * register[0] = registerB 
                                               * register[1] = registerC
                                               * register[2] = registerD
                                               * register[3] = registerE 
                                               * register[4] = registerH
                                               * register[5] = registerL
                                               * register[6] = registerM
                                               * register[7] = registerA
                                               */

        public int registerPC = 0x0000;
        public int registerSP = 0xFFFF;
       
        public byte[] flag = new byte[8];     /*
                                               * flag[0] = flagC
                                               * flag[1] = flagX
                                               * flag[2] = flagP
                                               * flag[3] = flagX
                                               * flag[4] = flagAC
                                               * flag[5] = flagX
                                               * flag[6] = flagZ
                                               * flag[7] = flagS
                                               */

        private int flagIndex(string s) // get flag index from string
        {
            switch(s)
            {
                case "C":
                case "c": return 0;

                case "P":
                case "p": return 2;

                case "A":
                case "a":
                case "AC":
                case "ac": return 4;

                case "Z":
                case "z": return 6;

                case "S":
                case "s": return 7;

                default: return -1;
            }

        }

        private int registerIndex(string s) // get register index from string
        {
            switch (s)
            {
                case "0":
                case "B":
                case "b": return 0;

                case "1":
                case "C":
                case "c": return 1;

                case "2":
                case "D":
                case "d": return 2;

                case "3":
                case "E":
                case "e": return 3;

                case "4":
                case "H":
                case "h": return 4;

                case "5":
                case "L":
                case "l": return 5;

                case "6":
                case "M":
                case "m":
                    {
                        updateMRegister();
                        return 6;
                    }

                case "7":
                case "A":
                case "a": return 7;

                default: return -1;
            }

        }

        private void updateMRegister()
        {
            register[6] = RAM[ register[registerIndex("H")]*0x100 + register[registerIndex("L")] ];
        }

        private void initFlags()
        {
            prevRegisterA = register[7];    // save value of registerA
        }

        private void changeFlags()
        {
            int i,count;
            byte b1=00, b2=00;
            string prevBin = Convert.ToString
                (Convert.ToInt32(prevRegisterA.ToString("X"), 16), 2).PadLeft(8, '0');

            string newBin = Convert.ToString
                (Convert.ToInt32(register[7].ToString("X"), 16), 2).PadLeft(8, '0');

            //
            // sign flag
            if (newBin[7] == '1')   
                flag[7] = 1;
            else
                flag[7] = 0;
            //

            //
            // zero flag
            if (newBin == "00000000")   
                flag[6] = 1;
            else
                flag[6] = 0;
            //

            //
            // parity flag
            count = 0;
            for (i = 0; i < 8; i++)
            {
                if (newBin[i] == '1')
                    count++;
            }

            if (count % 2 == 0)     
                flag[2] = 1;
            else
                flag[2] = 0;
            //

            //
            // carry flag
            if (register[7] < prevRegisterA)    // if new value of Accumulator is less that means carry
                flag[0] = 1;                    // carry flag
            else if (register[7] > prevRegisterA)
                flag[0] = 0;
            //

            //
            // auxiliary carry flag
            b1 = (byte)(0x0F & prevRegisterA);  // masking upper 4 bits
            b2 = (byte)(0x0F & register[7]);    // masking upper 4 bits

            if (b2 < b1)
                flag[4] = 1;       // auxiliary carry flag
            else if(b2 > b1)
                flag[4] = 0;
            //
        }

        public byte MEMORY(int index)   // get memory byte at address=index
        {
            if ((index <= 0xFFFF) && (index >= 0x0000))
                return RAM[index];
            else
                return 0x00;    // if index is out of bound
        }

        public Assembler85()    // Constructor for initialisation
        {
            int i;
            startLocation = 0;
            registerSP = 0xFFFF;

            for (i = 0; i < RAM.Length; i++ )
            {
                RAM[i] = Convert.ToByte("00", 16);
            }
        }

        public int changeStartLocation
        {
            get { return startLocation; }
            set { startLocation = value; }
        }

        public int changeProgramLength
        {
            set { 
                    program = new String[value];
                    listProgram = new String[value];
                }
        }

        public void DisplayAddressSymbolTable()
        {
            int i;
            string str = "";
            for (i = 0; i < addressSymbolTableLabel.Count; i++)
            {
                str = str + addressSymbolTableLabel[i] 
                    + " " + addressSymbolTableValue[i].ToString("X") + "\n";
            }
            MessageBox.Show(str);
        }

        private bool isByte(string s)   // check if string is a byte   eg 2F
        {
            int n = 0;

            if (addressSymbolTableLabel.Contains(s + ":"))
            {
                int index = addressSymbolTableLabel.IndexOf(s + ":");
                s = addressSymbolTableValue[index].ToString().TrimEnd(':');
            }

            if (int.TryParse(s, out n))
            {
                if( (n<=255) && (n>=0) )
                return true;
            }

            else
            {
                s = s.TrimEnd('h', 'H');
            }

            try
            {
                n = Convert.ToInt32(s, 16);
            }
            catch
            {
                return false;
            }

            if( (n>=0x00) && (n<=0xFF) )
            {
                return true;
            }

            return false;
        }

        private bool is2Byte(string s)  // check if string is 2byte   eg 3F34
        {
            int n = 0;

            if (addressSymbolTableLabel.Contains(s + ":"))
            {
                int index = addressSymbolTableLabel.IndexOf(s + ":");
                s = addressSymbolTableValue[index].ToString().TrimEnd(':');
            }

            if (int.TryParse(s, out n))
            {
                if ( (n <= 65535) && (n >= 0) )
                return true;
            }

            else
            {
                s = s.TrimEnd('h', 'H');
            }

            try
            {
                n = Convert.ToInt32(s, 16);
            }
            catch
            {
                return false;
            }

            if ((n >= 0x0000) && (n <= 0xFFFF))
            {
                return true;
            }

            return false;
        }

        private int getIntByte(string s)
        {
            int n = 0;

            if (addressSymbolTableLabel.Contains(s + ":"))
            {
                int index = addressSymbolTableLabel.IndexOf(s + ":");
                s = addressSymbolTableValue[index].ToString().TrimEnd(':');
            }

            if (int.TryParse(s, out n))
            {
                if ((n <= 255) && (n >= 0))
                    return n;
            }

            else
            {
                s = s.TrimEnd('h', 'H');
            }

            try
            {
                n = Convert.ToInt32(s, 16);
            }
            catch
            {
                return -1;
            }

            if ((n >= 0x00) && (n <= 0xFF))
            {
                return n;
            }

            return -1;
        }

        private int getInt2Byte(string s)
        {
            int n = 0;

            if (addressSymbolTableLabel.Contains(s + ":"))
            {
                int index = addressSymbolTableLabel.IndexOf(s + ":");
                s = addressSymbolTableValue[index].ToString().TrimEnd(':');
            }

            if (int.TryParse(s, out n))
            {
                if ((n <= 65535) && (n >= 0))
                    return n;
            }

            else
            {
                s = s.TrimEnd('h', 'H');
            }

            try
            {
                n = Convert.ToInt32(s, 16);
            }
            catch
            {
                return -1;
            }

            if ((n >= 0x0000) && (n <= 0xFFFF))
            {
                return n;
            }

            return -1;
        }

        private void get2ByteFromInt(int n, out string s1, out string s2)
        {
            s1 = n.ToString("X");
            s1 = s1.PadLeft(4, '0');
            s2 = s1.Substring(0, 2);
            s1 = s1.Substring(2, 2);
        }

        public void FirstPass(string[] line)
        {
            int i, j, k;    // temporary variables

            int LocationCounter = startLocation;    // StartLocation denotes the first RAM location to
                                                    // which we are 
                                                    // assembling the program
                                                    // LocationCounter is a temporary variable
                                                    // to traverse 
                                                    // program for first pass

            string[] instructionSplit;              // This array of strings store the instruction
                                                    // split in parts
                                                    // Example:
                                                    // LOOP: MOV A,B
                                                    // instructionSplit[0] = LOOP:
                                                    // instructionSplit[1] = MOV
                                                    // instructionSplit[2] = A
                                                    // instructionSplit[3] = B

            char[] delimiters = new[] { ',', ' ' }; // delimiters is used for splitting 
                                                    // the line of instructions
                                                    // chars in this array separate the strings 
            
            for (i = 0; i < line.Length; i++)       // i is used for line number
            {
                if ( line[i].IndexOf(';') != -1 )                     // if a comment is found
                    line[i] = line[i].Remove(line[i].IndexOf(';'));   // remove comments

                instructionSplit = line[i].Split(delimiters , StringSplitOptions.RemoveEmptyEntries);
                                                    // split the i'th  line and 
                                                    // store the strings formed in array

                if (instructionSplit.Length == 0)   // empty line, no need to check for anything
                    goto END;

                if (line[i].Contains("ORG") || line[i].Contains("org"))// find if this line have an ORG
                {
                    if (instructionSplit.Length == 1)   // line must have 2 strings, if not ERROR
                    {
                        throw new AssemblerErrorException("Wrong way to use ORG");
                    }
                    if (instructionSplit[0].Equals("ORG", StringComparison.CurrentCultureIgnoreCase))  
                    {                                          // check if ORG is at correct position
                                                               // i.e. [0] index
                        if (is2Byte(instructionSplit[1]))
                        {                                // check if the operand at [1] is a valid byte
                            LocationCounter = getInt2Byte(instructionSplit[1]);
                            // if valid address then store in LocationCounter
                        }
                        else
                        {
                            throw new AssemblerErrorException("incorrect operand for ORG");
                        }
                    }
                }

                if (line[i].Contains("EQU") || line[i].Contains("equ"))
                {
                    if ( instructionSplit.Length < 3 ) // length should be atleast 3 
                                                       // [0]  [1] [2]
                                                       // DATA EQU 1234H
                    {
                        throw new AssemblerErrorException("Wrong way to use EQU");
                    }
                    if (instructionSplit[0].IndexOf(':') == instructionSplit[0].Length - 1)// can't use :
                    {
                        throw new AssemblerErrorException("Wrong way to use EQU");
                    }

                    if (instructionSplit[1].Equals("EQU", StringComparison.CurrentCultureIgnoreCase))
                    {
                        addressSymbolTableLabel.Add(instructionSplit[0]+':');   // Add the label
                        if (is2Byte(instructionSplit[2]))
                        {
                            addressSymbolTableValue.Add(getInt2Byte(instructionSplit[2]));
                                                         // add the numbers in instructionSplit[2]
                        }
                    }
                }

                j = 0;  // no label found
                //1               
                if (instructionSplit[0].IndexOf(':') == instructionSplit[0].Length-1)
                {                                                       // : is at correct position
                    addressSymbolTableLabel.Add(instructionSplit[0]);   // store label
                    addressSymbolTableValue.Add(LocationCounter);       // store current address
                    j = 1;  // flag set to denote label found
                }   // j is the position of operand

                if (instructionSplit.Length == 1 && j == 1) // only label in the line
                {
                    line[i] = "";   // clear the line
                    goto END;       // jump to end
                }

                switch (instructionSplit[j])    // check the operand in switch
                {
                //2
                    case "DB":
                    case "db":
                    {
                        if (instructionSplit.Length <= j + 1)
                            throw new AssemblerErrorException
                                ("DB directive has less operands" + " at line " + (i + 1));

                        if (instructionSplit[0].Equals("DB",
                                                    StringComparison.CurrentCultureIgnoreCase))
                            j = 0;  // DB at [0], no label was used
                        else if (instructionSplit[1].Equals("DB", 
                                                    StringComparison.CurrentCultureIgnoreCase))
                            j = 1;  // DB at [1], label was used at [0]
                        else
                            throw new AssemblerErrorException
                                ("DB directive not found at correct position" + " at line " + (i + 1));
                                                             // DB is not at correct position

                        for (k = j + 1; k < instructionSplit.Length; k++)// loop for traversing after DB
                        {
                            if (!isByte(instructionSplit[k]))
                                throw new AssemblerErrorException
                                    ("DB directive has incorrect operands" + " at line " + (i + 1));
                            else
                                LocationCounter++;  
                                    // get to next location by skipping location for byte
                        }   // note : we are not saving the value defined by DB 
                            // in the RAM, we'll do this in second pass

                        break;
                    }

                //3
                    case "DW":
                    case "dw":
                    {
                        if (instructionSplit.Length <= j + 1)
                            throw new AssemblerErrorException
                                ("DW directive has less operands" + " at line " + (i + 1));

                        if (instructionSplit[0].Equals
                            ("DW", StringComparison.CurrentCultureIgnoreCase))
                            j = 0;  // DW at [0], no label was used
                        else if (instructionSplit[1].Equals
                            ("DW", StringComparison.CurrentCultureIgnoreCase))
                            j = 1;  // DW at [1], label was used at [0]
                        else
                            throw new AssemblerErrorException
                                ("DW directive not found at correct position" + " at line " + (i + 1));
                                                                // DW is not at correct position
                            
                        for (k = j + 1; k < instructionSplit.Length; k++)
                        {
                            if (!is2Byte(instructionSplit[k]))
                                throw new AssemblerErrorException
                                    ("DW directive has incorrect operands" + " at line " + (i + 1));
                            else
                                LocationCounter += 2;   
                            // get to next location by skipping location for 2 bytes
                        }   // note : we are not saving the value defined by DB
                            //in the RAM, we'll do this in second pass

                        break;
                    }

                //4
                    case "DS":
                    case "ds":
                    {
                        if (instructionSplit.Length <= j + 1)
                            throw new AssemblerErrorException
                                ("DS directive has less operands" + " at line " + (i + 1));

                        if (instructionSplit[0].Equals
                            ("DS", StringComparison.CurrentCultureIgnoreCase))
                            j = 0;  // DS at [0], no label was used
                        else if (instructionSplit[1].Equals
                            ("DS", StringComparison.CurrentCultureIgnoreCase))
                            j = 1;  // DS at [1], label was used at [0]
                        else
                            throw new AssemblerErrorException
                                ("DS directive not found at correct position" + " at line " + (i + 1));
                                                                // DS is not at correct position

                        if (!is2Byte(instructionSplit[j + 1]))
                            throw new AssemblerErrorException
                                ("DS directive has incorrect operands" + " at line " + (i + 1));
                        else
                            LocationCounter += getInt2Byte(instructionSplit[j + 1]);
                                // get to next location by skipping location                                                      
                                // for count given by operand of DS

                        break;
                    }
                //5
                    case "ADC":
                    case "adc":
                    case "ADD":
                    case "add":
                    case "ANA":
                    case "ana":
                    case "CMA":
                    case "cma":
                    case "CMC":
                    case "cmc":
                    case "CMP":
                    case "cmp":
                    case "DAA":
                    case "daa":
                    case "DAD":
                    case "dad":
                    case "DCR":
                    case "dcr":
                    case "DCX":
                    case "dcx":
                    case "DI":
                    case "di":
                    case "EI":
                    case "ei":
                    case "HLT":
                    case "hlt":
                    case "INR":
                    case "inr":
                    case "INX":
                    case "inx":
                    case "LDAX":
                    case "ldax":
                    case "MOV":
                    case "mov":
                    case "NOP":
                    case "nop":
                    case "ORA":
                    case "ora":
                    case "PCHL":
                    case "pchl":
                    case "POP":
                    case "pop":
                    case "PUSH":
                    case "push":
                    case "RAL":
                    case "ral":
                    case "RAR":
                    case "rar":
                    case "RLC":
                    case "rlc":
                    case "RRC":
                    case "rrc":
                    case "RET":
                    case "ret":
                    case "RC":
                    case "rc":
                    case "RNC":
                    case "rnc":
                    case "RP":
                    case "rp":
                    case "RM":
                    case "rm":
                    case "RPE":
                    case "rpe":
                    case "RPO":
                    case "rpo":
                    case "RZ":
                    case "rz":
                    case "RNZ":
                    case "rnz":
                    case "RIM":
                    case "rim":
                    case "RST":
                    case "rst":
                    case "SBB":
                    case "sbb":
                    case "SIM":
                    case "sim":
                    case "SPHL":
                    case "sphl":
                    case "STAX":
                    case "stax":
                    case "STC":
                    case "stc":
                    case "SUB":
                    case "sub":
                    case "XCHG":
                    case "xchg":
                    case "XRA":
                    case "xra":
                    case "XTHL":
                    case "xthl":
                    {
                        LocationCounter += 1;   // single byte instructions
                        break;
                    }
                //6
                    case "ACI":
                    case "aci":
                    case "ADI":
                    case "adi":
                    case "ANI":
                    case "ani":
                    case "CPI":
                    case "cpi":
                    case "IN":
                    case "in":
                    case "MVI":
                    case "mvi":
                    case "ORI":
                    case "ori":
                    case "OUT":
                    case "out":
                    case "SBI":
                    case "sbi":
                    case "SUI":
                    case "sui":
                    case "XRI":
                    case "xri":
                    {
                        LocationCounter += 2;   // 2 byte instructions
                        break;
                    }
                //7
                    case "CALL":
                    case "call":
                    case "CC":
                    case "cc":
                    case "CNC":
                    case "cnc":
                    case "CP":
                    case "cp":
                    case "CM":
                    case "cm":
                    case "CPE":
                    case "cpe":
                    case "CPO":
                    case "cpo":
                    case "CZ":
                    case "cz":
                    case "CNZ":
                    case "cnz":
                    case "JMP":
                    case "jmp":
                    case "JC":
                    case "jc":
                    case "JNC":
                    case "jnc":
                    case "JP":
                    case "jp":
                    case "JM":
                    case "jm":
                    case "JPE":
                    case "jpe":
                    case "JPO":
                    case "jpo":
                    case "JZ":
                    case "jz":
                    case "JNZ":
                    case "jnz":
                    case "LDA":
                    case "lda":
                    case "LHLD":
                    case "lhld":
                    case "LXI":
                    case "lxi":
                    case "SHLD":
                    case "shld":
                    case "STA":
                    case "sta":
                    {
                        LocationCounter += 3;   // 3 byte instructions
                        break;
                    }

                }
            
                // we remove all the labels from the program
                // now the program is labels free
                //
                //  we are removing labels beacuse they will not be used in second pass
                //  example
                //  LOOP:   MOV A,B
                //  
                //  MOV A,B
                //
                if (instructionSplit[0].IndexOf(':') == instructionSplit[0].Length - 1)// remove label
                {
                    line[i] = line[i].Substring
                        (line[i].IndexOf(instructionSplit[0]) + instructionSplit[0].Length + 1);
                }

                // we remove all the Lines with EQU directive
                // now program does not contain EQU
                //
                //  we are removing EQU because they will not be used in second pass
                //
                if (line[i].Contains("EQU") || line[i].Contains("equ"))     // remove line with EQU
                {
                    line[i] = "";
                }

                //
                //  copy the whole edited program(without labels and EQU) to new array of strings
                //  the new program array of strings will be used in second pass
                //
            END:
                program[i] = line[i];   // store the edited program in array program[]

            }
        }   

        public int SecondPass()
        {
                                               //
                                               //  startLocation gives the location from which we have 
                                               //  to start assembling
                                               //
            int LocationCounter = startLocation; // usign LocationCounter to traverse the location of 
                                                 // RAM during second pass

            string[] instructionSplit;
            string commonError;
            

            //
            // Temporary variables
            //
            int i,k;
            int temp;
            string s1, s2;
            //
            //

            char[] delimiters = new[] { ',', ' ' }; // for instruction split

            for (i = 0; i < program.Length; i++)
            {
                instructionSplit = program[i].Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

                if (instructionSplit.Length == 0)   // empty line
                    continue;                       // if line is empty, there is no need to check

                commonError = "Invalid operand for " + instructionSplit[0] + " in line " + (i + 1);
                                                    // common error message 

                //
                // find out which instruction it is
                // note : the operand is always at first position
                // because we have removed all labels and EQU
                //

                switch (instructionSplit[0])        
                {                               
                    case "ORG":
                    case "org":
                        {
                            if (instructionSplit.Length == 1)   // cannot have just 1 string in line
                            {
                                throw new AssemblerErrorException("Wrong way to use ORG");
                            }
                            else if (is2Byte(instructionSplit[1]))
                            {
                                LocationCounter = getInt2Byte(instructionSplit[1]);
                            }
                            else // not a valid operand
                            {
                                throw new AssemblerErrorException(commonError);
                            }

                            break;
                        }
                    case "DB":
                    case "db":
                        {
                            for (k = 1; k < instructionSplit.Length; k++) // extract all DB operands
                            {
                                temp = getIntByte(instructionSplit[k]);   // get int value of operand
                                s1 = temp.ToString("X");     
                                                            // change to HEX string
                                RAMprogramLine[LocationCounter] = i;       
                                                            // it gives the linenumber
                                                            // for a given byte of program
                                RAM[LocationCounter++] = Convert.ToByte(s1, 16);
                                                            // store the operand of DB in RAM
                            }
                            break;
                        }
                    case "DW":
                    case "dw":
                        {
                            for (k = 1; k < instructionSplit.Length; k++)
                            {
                                temp = getInt2Byte(instructionSplit[k]);
                                get2ByteFromInt(temp ,out s1, out s2);
                                RAMprogramLine[LocationCounter] = i;
                                RAM[LocationCounter++] = Convert.ToByte(s1, 16);
                                RAMprogramLine[LocationCounter] = i;
                                RAM[LocationCounter++] = Convert.ToByte(s2, 16);
                            }
                            break;
                        }
                    case "DS":
                    case "ds":
                        {
                            k = getInt2Byte(instructionSplit[1]);
                            while ((k--)!=0)
                            {
                                RAMprogramLine[LocationCounter] = i;
                                LocationCounter++;  // we don't have to initialize operands for DS 
                                                    // just reserve space for them
                            }
                            break;
                        }
                    case "ACI":     // add with carry immediate
                    case "aci":     // ACI 52H  : R[A]= R[A] + 52H + F[C]  //instruction not executed
                        {
                            RAMprogramLine[LocationCounter] = i;
                            RAM[LocationCounter++] = Convert.ToByte("CE", 16); 
                                                                // store the instruction byte in RAM
                            if (isByte(instructionSplit[1]))    // check for valid operand
                            {
                                temp = getIntByte(instructionSplit[1]);
                                s1 = temp.ToString("X");
                                RAMprogramLine[LocationCounter] = i;
                                RAM[LocationCounter++] = Convert.ToByte(s1, 16);
                            }
                            else    // operand is invalid
                            {
                                throw new AssemblerErrorException(commonError);
                            }
                            break;
                        }
                    case "ADC":     // add with carry
                    case "adc":     // ADC B    : R[A] = R[A] + R[B] + F[C]
                        {
                            k = 0x88;
                            s1 = instructionSplit[1];

                            if (registerIndex(instructionSplit[1]) == -1) // find if valid operand 
                                throw new AssemblerErrorException(commonError); // example - ADC X 

                            k += registerIndex(instructionSplit[1]);    // find the instruction byte

                            s1 = k.ToString("X");
                            RAMprogramLine[LocationCounter] = i;
                            RAM[LocationCounter++] = Convert.ToByte(s1, 16); // store instruction byte

                            break;
                        }
                    case "ADD":     // add 
                    case "add":     // ADD C    : R[A] = R[A] + R[C]
                        {
                            k = 0x80;

                            if (registerIndex(instructionSplit[1]) == -1) // find if valid operand
                                throw new AssemblerErrorException(commonError);
                            s1 = instructionSplit[1];

                            k += registerIndex(instructionSplit[1]); // find the instruction byte

                            s1 = k.ToString("X");
                            RAMprogramLine[LocationCounter] = i;
                            RAM[LocationCounter++] = Convert.ToByte(s1, 16); // store instruction byte

                            break;
                        }
                    case "ADI":     // add with immediate
                    case "adi":     // ADI 52H  : R[A] = R[A] + 52H
                        {
                            RAMprogramLine[LocationCounter] = i;
                            RAM[LocationCounter++] = Convert.ToByte("C6", 16);

                            if (isByte(instructionSplit[1]))    // check valid operand
                            {
                                temp = getIntByte(instructionSplit[1]);
                                s1 = temp.ToString("X");
                                RAMprogramLine[LocationCounter] = i;
                                RAM[LocationCounter++] = Convert.ToByte(s1, 16);
                            }
                            else    // invalid operand
                            {
                                throw new AssemblerErrorException(commonError);
                            }
                            break;
                        }
                    case "ANA":
                    case "ana":
                        {
                            k = 0xA0;
                            s1 = instructionSplit[1];

                            if (registerIndex(instructionSplit[1]) == -1)
                                throw new AssemblerErrorException(commonError);

                            k += registerIndex(instructionSplit[1]);

                            s1 = k.ToString("X");
                            RAMprogramLine[LocationCounter] = i;
                            RAM[LocationCounter++] = Convert.ToByte(s1, 16);

                            break;
                        }
                    case "ANI":
                    case "ani":
                        {
                            RAMprogramLine[LocationCounter] = i;
                            RAM[LocationCounter++] = Convert.ToByte("E6", 16);
                            if (isByte(instructionSplit[1]))
                            {
                                temp = getIntByte(instructionSplit[1]);
                                s1 = temp.ToString("X");
                                RAMprogramLine[LocationCounter] = i;
                                RAM[LocationCounter++] = Convert.ToByte(s1, 16);
                            }
                            else
                            {
                                throw new AssemblerErrorException(commonError);
                            }
                            break;
                        }
                    case "CALL":    // call a procedure
                    case "call":
                        {
                            RAMprogramLine[LocationCounter] = i;
                            RAM[LocationCounter++] = Convert.ToByte("CD", 16);
                            if (is2Byte(instructionSplit[1]))
                            {
                                temp = getInt2Byte(instructionSplit[1]);
                                get2ByteFromInt(temp, out s1, out s2);
                                RAMprogramLine[LocationCounter] = i;
                                RAM[LocationCounter++] = Convert.ToByte(s1, 16);
                                RAMprogramLine[LocationCounter] = i;
                                RAM[LocationCounter++] = Convert.ToByte(s2, 16);
                            }
                            else
                            {
                                throw new AssemblerErrorException(commonError);
                            }
                            break;
                        }
                    case "CC":
                    case "cc":
                        {
                            RAMprogramLine[LocationCounter] = i;
                            RAM[LocationCounter++] = Convert.ToByte("DC", 16);
                            if (is2Byte(instructionSplit[1]))
                            {
                                temp = getInt2Byte(instructionSplit[1]);
                                get2ByteFromInt(temp, out s1, out s2);
                                RAMprogramLine[LocationCounter] = i;
                                RAM[LocationCounter++] = Convert.ToByte(s1, 16);
                                RAMprogramLine[LocationCounter] = i;
                                RAM[LocationCounter++] = Convert.ToByte(s2, 16);
                            }
                            else
                            {
                                throw new AssemblerErrorException(commonError);
                            }
                            break;
                        }
                    case "CNC":
                    case "cnc":
                        {
                            RAMprogramLine[LocationCounter] = i;
                            RAM[LocationCounter++] = Convert.ToByte("D4", 16);
                            if (is2Byte(instructionSplit[1]))
                            {
                                temp = getInt2Byte(instructionSplit[1]);
                                get2ByteFromInt(temp, out s1, out s2);
                                RAMprogramLine[LocationCounter] = i;
                                RAM[LocationCounter++] = Convert.ToByte(s1, 16);
                                RAMprogramLine[LocationCounter] = i;
                                RAM[LocationCounter++] = Convert.ToByte(s2, 16);
                            }
                            else
                            {
                                throw new AssemblerErrorException(commonError);
                            }
                            break;
                        }
                    case "CP":
                    case "cp":
                        {
                            RAMprogramLine[LocationCounter] = i;
                            RAM[LocationCounter++] = Convert.ToByte("F4", 16);
                            if (is2Byte(instructionSplit[1]))
                            {
                                temp = getInt2Byte(instructionSplit[1]);
                                get2ByteFromInt(temp, out s1, out s2);
                                RAMprogramLine[LocationCounter] = i;
                                RAM[LocationCounter++] = Convert.ToByte(s1, 16);
                                RAMprogramLine[LocationCounter] = i;
                                RAM[LocationCounter++] = Convert.ToByte(s2, 16);
                            }
                            else
                            {
                                throw new AssemblerErrorException(commonError);
                            }
                            break;
                        }
                    case "CM":
                    case "cm":
                        {
                            RAMprogramLine[LocationCounter] = i;
                            RAM[LocationCounter++] = Convert.ToByte("FC", 16);
                            if (is2Byte(instructionSplit[1]))
                            {
                                temp = getInt2Byte(instructionSplit[1]);
                                get2ByteFromInt(temp, out s1, out s2);
                                RAMprogramLine[LocationCounter] = i;
                                RAM[LocationCounter++] = Convert.ToByte(s1, 16);
                                RAMprogramLine[LocationCounter] = i;
                                RAM[LocationCounter++] = Convert.ToByte(s2, 16);
                            }
                            else
                            {
                                throw new AssemblerErrorException(commonError);
                            }
                            break;
                        }
                    case "CPE":
                    case "cpe":
                        {
                            RAMprogramLine[LocationCounter] = i;
                            RAM[LocationCounter++] = Convert.ToByte("EC", 16);
                            if (is2Byte(instructionSplit[1]))
                            {
                                temp = getInt2Byte(instructionSplit[1]);
                                get2ByteFromInt(temp, out s1, out s2);
                                RAMprogramLine[LocationCounter] = i;
                                RAM[LocationCounter++] = Convert.ToByte(s1, 16);
                                RAMprogramLine[LocationCounter] = i;
                                RAM[LocationCounter++] = Convert.ToByte(s2, 16);
                            }
                            else
                            {
                                throw new AssemblerErrorException(commonError);
                            }
                            break;
                        }
                    case "CPO":
                    case "cpo":
                        {
                            RAMprogramLine[LocationCounter] = i;
                            RAM[LocationCounter++] = Convert.ToByte("E4", 16);
                            if (is2Byte(instructionSplit[1]))
                            {
                                temp = getInt2Byte(instructionSplit[1]);
                                get2ByteFromInt(temp, out s1, out s2);
                                RAMprogramLine[LocationCounter] = i;
                                RAM[LocationCounter++] = Convert.ToByte(s1, 16);
                                RAMprogramLine[LocationCounter] = i;
                                RAM[LocationCounter++] = Convert.ToByte(s2, 16);
                            }
                            else
                            {
                                throw new AssemblerErrorException(commonError);
                            }
                            break;
                        }
                    case "CZ":
                    case "cz":
                        {
                            RAMprogramLine[LocationCounter] = i;
                            RAM[LocationCounter++] = Convert.ToByte("CC", 16);
                            if (is2Byte(instructionSplit[1]))
                            {
                                temp = getInt2Byte(instructionSplit[1]);
                                get2ByteFromInt(temp, out s1, out s2);
                                RAMprogramLine[LocationCounter] = i;
                                RAM[LocationCounter++] = Convert.ToByte(s1, 16);
                                RAMprogramLine[LocationCounter] = i;
                                RAM[LocationCounter++] = Convert.ToByte(s2, 16);
                            }
                            else
                            {
                                throw new AssemblerErrorException(commonError);
                            }
                            break;
                        }
                    case "CNZ":
                    case "cnz":
                        {
                            RAMprogramLine[LocationCounter] = i;
                            RAM[LocationCounter++] = Convert.ToByte("C4", 16);
                            if (is2Byte(instructionSplit[1]))
                            {
                                temp = getInt2Byte(instructionSplit[1]);
                                get2ByteFromInt(temp, out s1, out s2);
                                RAMprogramLine[LocationCounter] = i;
                                RAM[LocationCounter++] = Convert.ToByte(s1, 16);
                                RAMprogramLine[LocationCounter] = i; 
                                RAM[LocationCounter++] = Convert.ToByte(s2, 16);
                            }
                            else
                            {
                                throw new AssemblerErrorException(commonError);
                            }
                            break;
                        }
                    case "CMA":
                    case "cma":
                        {
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte("2F", 16);
                            break;
                        }
                    case "CMC":
                    case "cmc":
                        {
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte("3F", 16);
                            break;
                        }
                    case "CMP":
                    case "cmp":
                        {
                            k = 0xB8;
                            s1 = instructionSplit[1];

                            if (registerIndex(instructionSplit[1]) == -1)
                                throw new AssemblerErrorException(commonError);

                            k += registerIndex(instructionSplit[1]);

                            s1 = k.ToString("X");
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte(s1, 16);

                            break;
                        }
                    case "CPI":
                    case "cpi":
                        {
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte("FE", 16);
                            if (isByte(instructionSplit[1]))
                            {
                                temp = getIntByte(instructionSplit[1]);
                                s1 = temp.ToString("X");
                                RAMprogramLine[LocationCounter] = i; 
                                RAM[LocationCounter++] = Convert.ToByte(s1, 16);
                            }
                            else
                            {
                                throw new AssemblerErrorException(commonError);
                            }
                            break;
                        }
                    case "DAA":
                    case "daa":
                        {
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte("27", 16);
                            break;
                        }
                    case "DAD":
                    case "dad":
                        {
                            k = 0x09;
                            s1 = instructionSplit[1];
                            switch (s1)
                            {
                                case "B": k += 0x00;
                                    break;
                                case "D": k += 0x10;
                                    break;
                                case "H": k += 0x20;
                                    break;
                                case "SP": k += 0x30;
                                    break;
                                default: throw new AssemblerErrorException(commonError);
                            }

                            s1 = k.ToString("X");
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte(s1, 16);

                            break;
                        }
                    case "DCR":
                    case "dcr":
                        {
                            k = 0x05;
                            s1 = instructionSplit[1];
                            
                            if (registerIndex(instructionSplit[1]) == -1)
                                throw new AssemblerErrorException(commonError);

                            k += (registerIndex(instructionSplit[1]))*0x08 ;

                            s1 = k.ToString("X");
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte(s1, 16);

                            break;
                        }
                    case "DCX":
                    case "dcx":
                        {
                            k = 0x0B;
                            s1 = instructionSplit[1];
                            switch (s1)
                            {
                                case "B": k += 0x00;
                                    break;
                                case "D": k += 0x10;
                                    break;
                                case "H": k += 0x20;
                                    break;
                                case "SP": k += 0x30;
                                    break;
                                default: throw new AssemblerErrorException(commonError);
                            }

                            s1 = k.ToString("X");
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte(s1, 16);

                            break;
                        }
                    case "DI":
                    case "di":
                        {
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte("F3", 16);
                            break;
                        }
                    case "EI":
                    case "ei":
                        {
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte("FB", 16);
                            break;
                        }
                    case "HLT":
                    case "hlt":
                        {
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte("76", 16);
                            break;
                        }
                    case "IN":
                    case "in":
                        {
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte("DB", 16);
                            if (isByte(instructionSplit[1]))
                            {
                                temp = getIntByte(instructionSplit[1]);
                                s1 = temp.ToString("X");
                                RAMprogramLine[LocationCounter] = i; 
                                RAM[LocationCounter++] = Convert.ToByte(s1, 16);
                            }
                            else
                            {
                                throw new AssemblerErrorException(commonError);
                            }
                            break;
                        }
                    case "INR":
                    case "inr":
                        {
                            k = 0x04;
                            s1 = instructionSplit[1];

                            if (registerIndex(instructionSplit[1]) == -1)
                                throw new AssemblerErrorException(commonError);

                            k += registerIndex(instructionSplit[1])*8;

                            s1 = k.ToString("X");
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte(s1, 16);

                            break;
                        }
                    case "INX":
                    case "inx":
                        {
                            k = 0x03;
                            s1 = instructionSplit[1];
                            switch (s1)
                            {
                                case "B": k += 0x00;
                                    break;
                                case "D": k += 0x10;
                                    break;
                                case "H": k += 0x20;
                                    break;
                                case "SP": k += 0x30;
                                    break;
                                default: throw new AssemblerErrorException(commonError);
                            }

                            s1 = k.ToString("X");
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte(s1, 16);

                            break;
                        }
                    case "JMP": // jump to a location
                    case "jmp":
                        {
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte("C3", 16);
                            if (is2Byte(instructionSplit[1]))
                            {
                                temp = getInt2Byte(instructionSplit[1]);
                                get2ByteFromInt(temp, out s1, out s2);
                                RAMprogramLine[LocationCounter] = i;
                                RAM[LocationCounter++] = Convert.ToByte(s1, 16);
                                RAMprogramLine[LocationCounter] = i; 
                                RAM[LocationCounter++] = Convert.ToByte(s2, 16);
                            }
                            else
                            {
                                throw new AssemblerErrorException(commonError);
                            }
                            break;
                        }
                    case "JC":
                    case "jc":
                        {
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte("DA", 16);
                            if (is2Byte(instructionSplit[1]))
                            {
                                temp = getInt2Byte(instructionSplit[1]);
                                get2ByteFromInt(temp, out s1, out s2);
                                RAMprogramLine[LocationCounter] = i;
                                RAM[LocationCounter++] = Convert.ToByte(s1, 16);
                                RAMprogramLine[LocationCounter] = i; 
                                RAM[LocationCounter++] = Convert.ToByte(s2, 16);
                            }
                            else
                            {
                                throw new AssemblerErrorException(commonError);
                            }
                            break;
                        }
                    case "JNC":
                    case "jnc":
                        {
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte("D2", 16);
                            if (is2Byte(instructionSplit[1]))
                            {
                                temp = getInt2Byte(instructionSplit[1]);
                                get2ByteFromInt(temp, out s1, out s2);
                                RAMprogramLine[LocationCounter] = i;
                                RAM[LocationCounter++] = Convert.ToByte(s1, 16);
                                RAMprogramLine[LocationCounter] = i; 
                                RAM[LocationCounter++] = Convert.ToByte(s2, 16);
                            }
                            else
                            {
                                throw new AssemblerErrorException(commonError);
                            }
                            break;
                        }
                    case "JP":
                    case "jp":
                        {
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte("F2", 16);
                            if (is2Byte(instructionSplit[1]))
                            {
                                temp = getInt2Byte(instructionSplit[1]);
                                get2ByteFromInt(temp, out s1, out s2);
                                RAMprogramLine[LocationCounter] = i;
                                RAM[LocationCounter++] = Convert.ToByte(s1, 16);
                                RAMprogramLine[LocationCounter] = i; 
                                RAM[LocationCounter++] = Convert.ToByte(s2, 16);
                            }
                            else
                            {
                                throw new AssemblerErrorException(commonError);
                            }
                            break;
                        }
                    case "JM":
                    case "jm":
                        {
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte("FA", 16);
                            if (is2Byte(instructionSplit[1]))
                            {
                                temp = getInt2Byte(instructionSplit[1]);
                                get2ByteFromInt(temp, out s1, out s2);
                                RAMprogramLine[LocationCounter] = i;
                                RAM[LocationCounter++] = Convert.ToByte(s1, 16);
                                RAMprogramLine[LocationCounter] = i; 
                                RAM[LocationCounter++] = Convert.ToByte(s2, 16);
                            }
                            else
                            {
                                throw new AssemblerErrorException(commonError);
                            }
                            break;
                        }
                    case "JPE":
                    case "jpe":
                        {
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte("EA", 16);
                            if (is2Byte(instructionSplit[1]))
                            {
                                temp = getInt2Byte(instructionSplit[1]);
                                get2ByteFromInt(temp, out s1, out s2);
                                RAMprogramLine[LocationCounter] = i;
                                RAM[LocationCounter++] = Convert.ToByte(s1, 16);
                                RAMprogramLine[LocationCounter] = i; 
                                RAM[LocationCounter++] = Convert.ToByte(s2, 16);
                            }
                            else
                            {
                                throw new AssemblerErrorException(commonError);
                            }
                            break;
                        }
                    case "JPO":
                    case "jpo":
                        {
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte("E2", 16);
                            if (is2Byte(instructionSplit[1]))
                            {
                                temp = getInt2Byte(instructionSplit[1]);
                                get2ByteFromInt(temp, out s1, out s2);
                                RAMprogramLine[LocationCounter] = i;
                                RAM[LocationCounter++] = Convert.ToByte(s1, 16);
                                RAMprogramLine[LocationCounter] = i; 
                                RAM[LocationCounter++] = Convert.ToByte(s2, 16);
                            }
                            else
                            {
                                throw new AssemblerErrorException(commonError);
                            }
                            break;
                        }
                    case "JZ":
                    case "jz":
                        {
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte("CA", 16);
                            if (is2Byte(instructionSplit[1]))
                            {
                                temp = getInt2Byte(instructionSplit[1]);
                                get2ByteFromInt(temp, out s1, out s2);
                                RAMprogramLine[LocationCounter] = i;
                                RAM[LocationCounter++] = Convert.ToByte(s1, 16);
                                RAMprogramLine[LocationCounter] = i; 
                                RAM[LocationCounter++] = Convert.ToByte(s2, 16);
                            }
                            else
                            {
                                throw new AssemblerErrorException(commonError);
                            }
                            break;
                        }
                    case "JNZ":
                    case "jnz":
                        {
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte("C2", 16);
                            if (is2Byte(instructionSplit[1]))
                            {
                                temp = getInt2Byte(instructionSplit[1]);
                                get2ByteFromInt(temp, out s1, out s2);
                                RAMprogramLine[LocationCounter] = i;
                                RAM[LocationCounter++] = Convert.ToByte(s1, 16);
                                RAMprogramLine[LocationCounter] = i; 
                                RAM[LocationCounter++] = Convert.ToByte(s2, 16);
                            }
                            else
                            {
                                throw new AssemblerErrorException(commonError);
                            }
                            break;
                        }
                    case "LDA":
                    case "lda":
                        {
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte("3A", 16);
                            if (is2Byte(instructionSplit[1]))
                            {
                                temp = getInt2Byte(instructionSplit[1]);
                                get2ByteFromInt(temp, out s1, out s2);
                                RAMprogramLine[LocationCounter] = i;
                                RAM[LocationCounter++] = Convert.ToByte(s1, 16);
                                RAMprogramLine[LocationCounter] = i; 
                                RAM[LocationCounter++] = Convert.ToByte(s2, 16);
                            }
                            else
                            {
                                throw new AssemblerErrorException(commonError);
                            }
                            break;
                        }
                    case "LDAX":
                    case "ldax":
                        {
                            k = 0x0A;
                            s1 = instructionSplit[1];
                            switch (s1)
                            {
                                case "B": k += 0x00;
                                    break;
                                case "D": k += 0x10;
                                    break;
                                default: throw new AssemblerErrorException(commonError);
                            }

                            s1 = k.ToString("X");
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte(s1, 16);

                            break;
                        }
                    case "LHLD":
                    case "lhld":
                        {
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte("2A", 16);
                            if (is2Byte(instructionSplit[1]))
                            {
                                temp = getInt2Byte(instructionSplit[1]);
                                get2ByteFromInt(temp, out s1, out s2);
                                RAMprogramLine[LocationCounter] = i;
                                RAM[LocationCounter++] = Convert.ToByte(s1, 16);
                                RAMprogramLine[LocationCounter] = i; 
                                RAM[LocationCounter++] = Convert.ToByte(s2, 16);
                            }
                            else
                            {
                                throw new AssemblerErrorException(commonError);
                            }
                            break;
                        }
                    case "LXI":
                    case "lxi":
                        {
                            k = 0x01;
                            s1 = instructionSplit[1];
                            switch (s1)
                            {
                                case "B": k += 0x00;
                                    break;
                                case "D": k += 0x10;
                                    break;
                                case "H": k += 0x20;
                                    break;
                                case "SP": k += 0x30;
                                    break;
                                default: throw new AssemblerErrorException(commonError);
                            }

                            s1 = k.ToString("X");
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte(s1, 16);

                            if (is2Byte(instructionSplit[2]))
                            {
                                temp = getInt2Byte(instructionSplit[2]);
                                get2ByteFromInt(temp, out s1, out s2);
                                RAMprogramLine[LocationCounter] = i;
                                RAM[LocationCounter++] = Convert.ToByte(s1, 16);
                                RAMprogramLine[LocationCounter] = i; 
                                RAM[LocationCounter++] = Convert.ToByte(s2, 16);
                            }
                            else
                            {
                                throw new AssemblerErrorException(commonError);
                            }

                            break;
                        }
                    case "MOV":
                    case "mov":
                        {
                            k = 0x40;
                            k = k + 0x08 * registerIndex(instructionSplit[1]) 
                                + registerIndex(instructionSplit[2]);
                            if ( (k == 0x76) || (registerIndex(instructionSplit[1]) == -1)
                                || ( registerIndex(instructionSplit[2]) == -1 ) )
                            {
                                throw new AssemblerErrorException(commonError);
                            }
                            else
                            {
                                s1 = k.ToString("X");
                                RAMprogramLine[LocationCounter] = i; 
                                RAM[LocationCounter++] = Convert.ToByte(s1, 16);
                            }
                            break;
                        }
                    case "MVI":
                    case "mvi":
                        {
                            k = 0x06;
                            s1 = instructionSplit[1];

                            if (registerIndex(instructionSplit[1]) == -1)
                                throw new AssemblerErrorException(commonError);

                            k += registerIndex(instructionSplit[1])*0x08;

                            s1 = k.ToString("X");
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte(s1, 16);

                            if (isByte(instructionSplit[2]))
                            {
                                temp = getIntByte(instructionSplit[2]);
                                s1 = temp.ToString("X");
                                RAMprogramLine[LocationCounter] = i; 
                                RAM[LocationCounter++] = Convert.ToByte(s1, 16);
                            }
                            else
                            {
                                throw new AssemblerErrorException(commonError);
                            }

                            break;
                        }
                    case "NOP":
                    case "nop":
                        {
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte("00", 16);
                            break;
                        }
                    case "ORA":
                    case "ora":
                        {
                            k = 0xB0;
                            s1 = instructionSplit[1];

                            if (registerIndex(instructionSplit[1]) == -1)
                                throw new AssemblerErrorException(commonError);

                            k += registerIndex(instructionSplit[1]);

                            s1 = k.ToString("X");
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte(s1, 16);

                            break;
                        }
                    case "ORI":
                    case "ori":
                        {
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte("F6", 16);
                            if (isByte(instructionSplit[1]))
                            {
                                temp = getIntByte(instructionSplit[1]);
                                s1 = temp.ToString("X");
                                RAMprogramLine[LocationCounter] = i; 
                                RAM[LocationCounter++] = Convert.ToByte(s1, 16);
                            }
                            else
                            {
                                throw new AssemblerErrorException(commonError);
                            }
                            break;
                        }
                    case "OUT":
                    case "out":
                        {
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte("D3", 16);
                            if (isByte(instructionSplit[1]))
                            {
                                temp = getIntByte(instructionSplit[1]);
                                s1 = temp.ToString("X");
                                RAMprogramLine[LocationCounter] = i; 
                                RAM[LocationCounter++] = Convert.ToByte(s1, 16);
                            }
                            else
                            {
                                throw new AssemblerErrorException(commonError);
                            }
                            break;
                        }
                    case "PCHL":
                    case "pchl":
                        {
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte("E9", 16);
                            break;
                        }
                    case "POP":
                    case "pop":
                        {
                            k = 0xC1;
                            s1 = instructionSplit[1];
                            switch (s1)
                            {
                                case "B": k += 0x00;
                                    break;
                                case "D": k += 0x10;
                                    break;
                                case "H": k += 0x20;
                                    break;
                                case "PSW": k += 0x30;
                                    break;
                                default: throw new AssemblerErrorException(commonError);
                            }

                            s1 = k.ToString("X");
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte(s1, 16);

                            break;
                        }
                    case "PUSH":
                    case "push":
                        {
                            k = 0xC5;
                            s1 = instructionSplit[1];
                            switch (s1)
                            {
                                case "B": k += 0x00;
                                    break;
                                case "D": k += 0x10;
                                    break;
                                case "H": k += 0x20;
                                    break;
                                case "PSW": k += 0x30;
                                    break;
                                default: throw new AssemblerErrorException(commonError);
                            }

                            s1 = k.ToString("X");
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte(s1, 16);

                            break;
                        }
                    case "RAL":
                    case "ral":
                        {
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte("17", 16);
                            break;
                        }
                    case "RAR":
                    case "rar":
                        {
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte("1F", 16);
                            break;
                        }
                    case "RLC":
                    case "rlc":
                        {
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte("07", 16);
                            break;
                        }
                    case "RRC":
                    case "rrc":
                        {
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte("0F", 16);
                            break;
                        }
                    case "RET":
                    case "ret":
                        {
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte("C9", 16);
                            break;
                        }
                    case "RC":
                    case "rc":
                        {
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte("D8", 16);
                            break;
                        }
                    case "RNC":
                    case "rnc":
                        {
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte("D0", 16);
                            break;
                        }
                    case "RP":
                    case "rp":
                        {
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte("F0", 16);
                            break;
                        }
                    case "RM":
                    case "rm":
                        {
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte("F8", 16);
                            break;
                        }
                    case "RPE":
                    case "rpe":
                        {
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte("E8", 16);
                            break;
                        }
                    case "RPO":
                    case "rpo":
                        {
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte("E0", 16);
                            break;
                        }
                    case "RZ":
                    case "rz":
                        {
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte("C8", 16);
                            break;
                        }
                    case "RNZ":
                    case "rnz":
                        {
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte("C0", 16);
                            break;
                        }
                    case "RIM":
                    case "rim":
                        {
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte("20", 16);
                            break;
                        }
                    case "RST":
                    case "rst":
                        {
                            k = 0xC7;
                            if (int.TryParse(instructionSplit[1], out temp))
                            {
                                k = k + temp * 0x08;
                                s1 = k.ToString("X");
                                RAMprogramLine[LocationCounter] = i; 
                                RAM[LocationCounter++] = Convert.ToByte(s1, 16);
                            }
                            else
                            {
                                throw new AssemblerErrorException(commonError);
                            }
                            break;
                        }
                    case "SBB":
                    case "sbb":
                        {
                            k = 0x98;
                            s1 = instructionSplit[1];

                            if (registerIndex(instructionSplit[1]) == -1)
                                throw new AssemblerErrorException(commonError);

                            k += registerIndex(instructionSplit[1]);

                            s1 = k.ToString("X");
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte(s1, 16);

                            break;
                        }
                    case "SBI":
                    case "sbi":
                        {
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte("DE", 16);
                            if (isByte(instructionSplit[1]))
                            {
                                temp = getIntByte(instructionSplit[1]);
                                s1 = temp.ToString("X");
                                RAMprogramLine[LocationCounter] = i; 
                                RAM[LocationCounter++] = Convert.ToByte(s1, 16);
                            }
                            else
                            {
                                throw new AssemblerErrorException(commonError);
                            }
                            break;
                        }
                    case "SHLD":
                    case "shld":
                        {
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte("22", 16);
                            if (is2Byte(instructionSplit[1]))
                            {
                                temp = getInt2Byte(instructionSplit[1]);
                                get2ByteFromInt(temp, out s1, out s2);
                                RAMprogramLine[LocationCounter] = i;
                                RAM[LocationCounter++] = Convert.ToByte(s1, 16);
                                RAMprogramLine[LocationCounter] = i; 
                                RAM[LocationCounter++] = Convert.ToByte(s2, 16);
                            }
                            else
                            {
                                throw new AssemblerErrorException(commonError);
                            }
                            break;
                        }
                    case "SIM":
                    case "sim":
                        {
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte("30", 16);
                            break;
                        }
                    case "SPHL":
                    case "sphl":
                        {
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte("F9", 16);
                            break;
                        }
                    case "STA":
                    case "sta":
                        {
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte("32", 16);
                            if (is2Byte(instructionSplit[1]))
                            {
                                temp = getInt2Byte(instructionSplit[1]);
                                get2ByteFromInt(temp, out s1, out s2);
                                RAMprogramLine[LocationCounter] = i;
                                RAM[LocationCounter++] = Convert.ToByte(s1, 16);
                                RAMprogramLine[LocationCounter] = i; 
                                RAM[LocationCounter++] = Convert.ToByte(s2, 16);
                            }
                            else
                            {
                                throw new AssemblerErrorException(commonError);
                            }
                            break;
                        }
                    case "STAX":
                    case "stax":
                        {
                            k = 0x02;
                            s1 = instructionSplit[1];
                            switch (s1)
                            {
                                case "B": k += 0x00;
                                    break;
                                case "D": k += 0x10;
                                    break;
                                default: throw new AssemblerErrorException(commonError);
                            }

                            s1 = k.ToString("X");
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte(s1, 16);

                            break;
                        }
                    case "STC":
                    case "stc":
                        {
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte("37", 16);
                            break;
                        }
                    case "SUB":
                    case "sub":
                        {
                            k = 0x90;
                            s1 = instructionSplit[1];

                            if (registerIndex(instructionSplit[1]) == -1)
                                throw new AssemblerErrorException(commonError);

                            k += registerIndex(instructionSplit[1]);

                            s1 = k.ToString("X");
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte(s1, 16);

                            break;
                        }
                    case "SUI":
                    case "sui":
                        {
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte("D6", 16);
                            if (isByte(instructionSplit[1]))
                            {
                                temp = getIntByte(instructionSplit[1]);
                                s1 = temp.ToString("X");
                                RAMprogramLine[LocationCounter] = i; 
                                RAM[LocationCounter++] = Convert.ToByte(s1, 16);
                            }
                            else
                            {
                                throw new AssemblerErrorException(commonError);
                            }
                            break;
                        }
                    case "XCHG":
                    case "xchg":
                        {
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte("EB", 16);
                            break;
                        }
                    case "XRA":
                    case "xra":
                        {
                            k = 0xA8;
                            s1 = instructionSplit[1];

                            if (registerIndex(instructionSplit[1]) == -1)
                                throw new AssemblerErrorException(commonError);

                            k += registerIndex(instructionSplit[1]);

                            s1 = k.ToString("X");
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte(s1, 16);

                            break;
                        }
                    case "XRI":
                    case "xri":
                        {
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte("EE", 16);
                            if (isByte(instructionSplit[1]))
                            {
                                temp = getIntByte(instructionSplit[1]);
                                s1 = temp.ToString("X");
                                RAMprogramLine[LocationCounter] = i; 
                                RAM[LocationCounter++] = Convert.ToByte(s1, 16);
                            }
                            else
                            {
                                throw new AssemblerErrorException(commonError);
                            }
                            break;
                        }
                    case "XTHL":
                    case "xthl":
                        {
                            RAMprogramLine[LocationCounter] = i; 
                            RAM[LocationCounter++] = Convert.ToByte("E3", 16);
                            break;
                        }
                    case "END":
                    case "end":
                        {
                            usedRam = LocationCounter - startLocation + 1;
                            return LocationCounter-startLocation+1 ;
                        }
                    default:
                        {
                            throw new AssemblerErrorException("Unknown instruction found at "+ (i+1));
                        } 
                }
            }
            usedRam = LocationCounter - startLocation + 1;  // this gives us how much RAM is used
            return LocationCounter-startLocation+1;
        }

        public int runProgram(int a, int instrCount) // run program from memory address a
        {                                            // , run program for linelength lines
            int num;

            registerPC = a;

            while ((instrCount--) > 0)
            {

                binaryInstruction =
                    Convert.ToString(
                    Convert.ToInt32(RAM[registerPC].ToString("X"), 16), 2).PadLeft(8, '0');
                byteInstruction = RAM[registerPC];

                //
                // note: we are not using SWITCH because we 
                // have to compare ranges
                //

                if (byteInstruction == 0x00)    // NOP
                {
                    registerPC++;
                }
                else if (byteInstruction == 0x76)    // HLT
                {
                    return 0xffff;
                }
                else if ((byteInstruction >= 0x40) && (byteInstruction <= 0x7F))    // MOV
                {                                  // note : no flags are affected by mov
                    int destinationIndex;
                    int sourceIndex;
                    num = byteInstruction - 0x40;

                    sourceIndex = num % 0x8;
                    destinationIndex = num / 0x08;

                    register[registerIndex(destinationIndex.ToString())] = register[registerIndex(sourceIndex.ToString())];

                    registerPC++;
                }
                else if ((byteInstruction >= 0x80) && (byteInstruction <= 0x87))    // ADD
                {
                    num = byteInstruction - 0x80;

                    initFlags();
                    register[registerIndex("A")] += register[registerIndex(num.ToString())];
                    changeFlags();

                    registerPC++;
                }
                else if ((byteInstruction >= 0x88) && (byteInstruction <= 0x8F))    // ADC
                {
                    num = byteInstruction - 0x88;

                    initFlags();
                    register[registerIndex("A")] += (byte)(register[registerIndex(num.ToString())] + flag[flagIndex("C")]);
                    changeFlags();

                    registerPC++;
                }
                else if ((byteInstruction >= 0x90) && (byteInstruction <= 0x97))    // SUB
                {
                    num = byteInstruction - 0x90;

                    initFlags();
                    register[registerIndex("A")] -= register[registerIndex(num.ToString())];
                    changeFlags();

                    registerPC++;
                }
                else if ((byteInstruction >= 0x98) && (byteInstruction <= 0x9F))    // SBB
                {
                    num = byteInstruction - 0x98;

                    initFlags();
                    register[registerIndex("A")] -= (byte)(register[registerIndex(num.ToString())] + flag[flagIndex("C")]);
                    changeFlags();

                    registerPC++;
                }
                else if ((byteInstruction >= 0xA0) && (byteInstruction <= 0xA7))    // ANA
                {
                    num = byteInstruction - 0xA0;

                    initFlags();
                    register[registerIndex("A")] &= register[registerIndex(num.ToString())];
                    changeFlags();

                    registerPC++;
                }
                else if ((byteInstruction >= 0xA8) && (byteInstruction <= 0xAF))    // XRA
                {
                    num = byteInstruction - 0xA8;

                    initFlags();
                    register[registerIndex("A")] ^= register[registerIndex(num.ToString())];
                    changeFlags();

                    registerPC++;
                }
                else if ((byteInstruction >= 0xB0) && (byteInstruction <= 0xB7))    // ORA
                {
                    num = byteInstruction - 0xB0;

                    initFlags();
                    register[registerIndex("A")] |= register[registerIndex(num.ToString())];
                    changeFlags();

                    registerPC++;
                }
                else if ((byteInstruction >= 0xB8) && (byteInstruction <= 0xBF))    // CMP  
                {
                    num = byteInstruction - 0xB8;          //Update all the flags except "Z" and "C"
                    byte temp1 = register[7];
                    byte temp2 = register[registerIndex(num.ToString())];
                    initFlags();
                    register[registerIndex("A")] -= register[registerIndex(num.ToString())];
                    changeFlags();
                    register[7] = temp1;
                    register[registerIndex(num.ToString())] = temp2;


                    if (register[7] < register[registerIndex(num.ToString())])                 //Update "Z" and "C" flags
                    {
                        flag[flagIndex("C")] = 1;
                        flag[flagIndex("Z")] = 0;
                    }
                    else if (register[7] == register[registerIndex(num.ToString())])
                    {
                        flag[flagIndex("C")] = 0;
                        flag[flagIndex("Z")] = 1;
                    }
                    else if (register[7] > register[registerIndex(num.ToString())])
                    {
                        flag[flagIndex("C")] = 0;
                        flag[flagIndex("Z")] = 0;
                    }
                    registerPC++;
                }
                else if ((binaryInstruction.Substring(0, 2) == "00")
                    && (binaryInstruction.Substring(5, 3) == "110")) // MVI
                {
                    string str = binaryInstruction.Substring(2, 3);
                    num = Convert.ToInt32(str, 2);
                    registerPC++;

                    register[registerIndex(num.ToString())] = RAM[registerPC];    // MVI X, ABH
                    registerPC++;
                }
                else if ((byteInstruction == 0x01) 
                    || (byteInstruction == 0x11) 
                    || (byteInstruction == 0x21) 
                    || (byteInstruction == 0x31))   // LXI
                {
                    if (byteInstruction == 0x01)
                    {
                        registerPC++;
                        register[registerIndex("C")] = RAM[registerPC];
                        registerPC++;
                        register[registerIndex("B")] = RAM[registerPC];
                        registerPC++;
                    }
                    else if (byteInstruction == 0x11)
                    {
                        registerPC++;
                        register[registerIndex("E")] = RAM[registerPC];
                        registerPC++;
                        register[registerIndex("D")] = RAM[registerPC];
                        registerPC++;
                    }
                    else if (byteInstruction == 0x21)
                    {
                        registerPC++;
                        register[registerIndex("L")] = RAM[registerPC];
                        registerPC++;
                        register[registerIndex("H")] = RAM[registerPC];
                        registerPC++;
                    }
                    else if (byteInstruction == 0x31)
                    {
                        byte b1, b2;
                        registerPC++;
                        b1 = RAM[registerPC];
                        registerPC++;
                        b2 = RAM[registerPC];
                        registerPC++;

                        registerSP = b1 + (0x100 * b2);
                    }
                }
                else if (byteInstruction == 0xCE)   //ACI
                {
                    registerPC++;

                    initFlags();
                    register[registerIndex("A")]
                        += (byte)(RAM[registerPC] + flag[flagIndex("C")]);
                    changeFlags();

                    registerPC++;
                }
                else if (byteInstruction == 0xC6)   //ADI
                {
                    registerPC++;

                    initFlags();
                    register[registerIndex("A")] += RAM[registerPC];
                    changeFlags();

                    registerPC++;
                }
                else if (byteInstruction == 0xE6)   //ANI
                {
                    registerPC++;

                    initFlags();
                    register[registerIndex("A")] &= RAM[registerPC];
                    changeFlags();

                    registerPC++;
                }
                else if (byteInstruction == 0x2F)   // CMA
                {
                    register[7] = (byte)(0xFF - register[7]); // complement accumulator
                }
                else if (byteInstruction == 0x3F)   // CMC
                {
                    flag[0] = (byte)((flag[0] == 0) ? 1 : 0);
                }
                else if (byteInstruction == 0xFE)    // CPI  
                {
                    registerPC++;

                    byte temp1 = register[7];  //Update all the flags except "Z" and "C"
                    initFlags();
                    register[registerIndex("A")] -= RAM[registerPC];
                    changeFlags();
                    register[7] = temp1;


                    if (register[7] < RAM[registerPC])  //Update "Z" and "C" flags
                    {
                        flag[flagIndex("C")] = 1;
                        flag[flagIndex("Z")] = 0;
                    }
                    else if (register[7] == RAM[registerPC])
                    {
                        flag[flagIndex("C")] = 0;
                        flag[flagIndex("Z")] = 1;
                    }
                    else if (register[7] > RAM[registerPC])
                    {
                        flag[flagIndex("C")] = 0;
                        flag[flagIndex("Z")] = 0;
                    }
                    registerPC++;
                }
                else if (byteInstruction == 0x3A)    // LDA
                {
                    int address = 0;
                    registerPC++;
                    address += RAM[registerPC];
                    registerPC++;
                    address += (0x100 * RAM[registerPC]);
                    registerPC++;
                    register[7] = RAM[address];
                }
                else if ((byteInstruction == 0x0A) || (byteInstruction == 0x1A))    // LDAX
                {
                    int address;

                    if (byteInstruction == 0x0A)
                    {
                        address = (register[0] * 0x100 + register[1]);  // get address from BC pair
                        register[7] = RAM[address];
                    }
                    else if (byteInstruction == 0x1A)
                    {
                        address = (register[2] * 0x100 + register[3]);  // get address from DE pair
                        register[7] = RAM[address];
                    }
                }
                else if (byteInstruction == 0x02)       //STAX B
                {
                    int address;
                    address = register[registerIndex("C")];
                    address = address + (0x100 * register[registerIndex("B")]);
                    RAM[address] = register[registerIndex("A")];
                    registerPC++;
                }

                else if (byteInstruction == 0x03)       //INX B
                {
                    int value 
                        = (0x100 * register[registerIndex("B")] + register[registerIndex("C")]);
                    value += 0x1;

                    string g1, g2;
                    get2ByteFromInt(value, out g1, out g2);
                    register[registerIndex("C")] = (byte)Convert.ToInt32(g1, 16);
                    register[registerIndex("B")] = (byte)Convert.ToInt32(g2, 16);

                    registerPC++;

                }

                else if (byteInstruction == 0x04)       //INR B
                {
                    byte temp;
                    byte save_flag;
                    save_flag = flag[0];
                    temp = register[registerIndex("A")];
                    register[registerIndex("A")] = register[registerIndex("B")];
                    initFlags();
                    register[registerIndex("A")] += 0x1;
                    changeFlags();
                    register[registerIndex("B")] = register[registerIndex("A")];
                    register[registerIndex("A")] = temp;
                    flag[0] = save_flag;
                    registerPC++;
                }

                else if (byteInstruction == 0x05)       //DCR B
                {
                    byte temp;
                    byte save_flag;
                    save_flag = flag[0];
                    temp = register[registerIndex("A")];
                    register[registerIndex("A")] = register[registerIndex("B")];
                    initFlags();
                    register[registerIndex("A")] -= 0x1;
                    changeFlags();
                    register[registerIndex("B")] = register[registerIndex("A")];
                    register[registerIndex("A")] = temp;
                    flag[0] = save_flag;
                    registerPC++;

                }

                else if (byteInstruction == 0x07)       //RLC
                {
                    initFlags();
                    register[registerIndex("A")] = (byte)(register[registerIndex("A")] * 2);
                    changeFlags();
                    registerPC++;
                }

                else if (byteInstruction == 0x09)       //DAD B
                {
                    int value1
                        = (0x100 * register[registerIndex("B")] + register[registerIndex("C")]);
                    int value2
                        = (0x100 * register[registerIndex("H")] + register[registerIndex("L")]);
                    int value = value1 + value2;


                    string g1, g2;
                    get2ByteFromInt(value, out g1, out g2);
                    register[registerIndex("L")] = (byte)Convert.ToInt32(g1, 16);
                    register[registerIndex("H")] = (byte)Convert.ToInt32(g2, 16);

                    registerPC++;


                }

                else if (byteInstruction == 0x0B)       //DCX B
                {
                    int value 
                        = (0x100 * register[registerIndex("B")] + register[registerIndex("C")]);
                    value -= 0x1;

                    string g1, g2;
                    get2ByteFromInt(value, out g1, out g2);
                    register[registerIndex("C")] = (byte)Convert.ToInt32(g1, 16);
                    register[registerIndex("B")] = (byte)Convert.ToInt32(g2, 16);

                    registerPC++;
                }

                else if (byteInstruction == 0x0C)       //INR C
                {
                    byte temp;
                    byte save_flag;
                    save_flag = flag[0];
                    temp = register[registerIndex("A")];
                    register[registerIndex("A")] = register[registerIndex("C")];
                    initFlags();
                    register[registerIndex("A")] += 0x1;
                    changeFlags();
                    register[registerIndex("C")] = register[registerIndex("A")];
                    register[registerIndex("A")] = temp;
                    flag[0] = save_flag;
                    registerPC++;

                }

                else if (byteInstruction == 0x0D)       //DCR C
                {
                    byte temp;
                    byte save_flag;
                    save_flag = flag[0];
                    temp = register[registerIndex("A")];
                    register[registerIndex("A")] = register[registerIndex("C")];
                    initFlags();
                    register[registerIndex("A")] -= 0x1;
                    changeFlags();
                    register[registerIndex("C")] = register[registerIndex("A")];
                    register[registerIndex("A")] = temp;
                    flag[0] = save_flag;
                    registerPC++;

                }

                else if (byteInstruction == 0x0F)       //RRC
                {
                    initFlags();
                    register[registerIndex("A")] = (byte)(register[registerIndex("A")] / 2);
                    changeFlags();
                    registerPC++;
                }

                else if (byteInstruction == 0x12)       //STAX D
                {
                    int address;
                    address = register[registerIndex("E")];
                    address = address + (0x100 * register[registerIndex("D")]);
                    RAM[address] = register[registerIndex("A")];
                    registerPC++;
                }

                else if (byteInstruction == 0x13)       //INX D
                {
                    int value
                        = (0x100 * register[registerIndex("D")] + register[registerIndex("E")]);
                    value += 0x1;

                    string g1, g2;
                    get2ByteFromInt(value, out g1, out g2);
                    register[registerIndex("E")] = (byte)Convert.ToInt32(g1, 16);
                    register[registerIndex("D")] = (byte)Convert.ToInt32(g2, 16);

                    registerPC++;
                }

                else if (byteInstruction == 0x14)       //INR D
                {
                    byte temp;
                    byte save_flag;
                    save_flag = flag[0];
                    temp = register[registerIndex("A")];
                    register[registerIndex("A")] = register[registerIndex("D")];
                    initFlags();
                    register[registerIndex("A")] += 0x1;
                    changeFlags();
                    register[registerIndex("D")] = register[registerIndex("A")];
                    register[registerIndex("A")] = temp;
                    flag[0] = save_flag;
                    registerPC++;

                }

                else if (byteInstruction == 0x15)       //DCR D
                {
                    byte temp;
                    byte save_flag;
                    save_flag = flag[0];
                    temp = register[registerIndex("A")];
                    register[registerIndex("A")] = register[registerIndex("D")];
                    initFlags();
                    register[registerIndex("A")] -= 0x1;
                    changeFlags();
                    register[registerIndex("D")] = register[registerIndex("A")];
                    register[registerIndex("A")] = temp;
                    flag[0] = save_flag;
                    registerPC++;

                }

                else if (byteInstruction == 0x17)       //RAL 
                {
                    byte prevA;
                    byte ac;
                    byte saveC;
                    byte c = 0;

                    ac = register[7];    // accumulator
                    if (flag[flagIndex("C")] == 1)
                    {
                        saveC = 1;
                    }
                    else
                    {
                        saveC = 0;
                    }

                    prevA = ac;
                    ac *= 2;
                    if (ac < prevA)
                    {
                        c = 1;
                    }
                    else
                    {
                        c = 0;
                    }

                    ac += saveC;

                    flag[flagIndex("C")] = c;
                    register[registerIndex("A")] = ac;

                    registerPC++;
                }

                else if (byteInstruction == 0x19)       //DAD D
                {
                    int value1
                        = (0x100 * register[registerIndex("D")] + register[registerIndex("E")]);
                    int value2 
                        = (0x100 * register[registerIndex("H")] + register[registerIndex("L")]);
                    int value = value1 + value2;


                    string g1, g2;
                    get2ByteFromInt(value, out g1, out g2);
                    register[registerIndex("L")] = (byte)Convert.ToInt32(g1, 16);
                    register[registerIndex("H")] = (byte)Convert.ToInt32(g2, 16);

                    registerPC++;
                }


                else if (byteInstruction == 0x1B)       //DCX D
                {
                    int value
                        = (0x100 * register[registerIndex("D")] + register[registerIndex("E")]);
                    value -= 0x1;

                    string g1, g2;
                    get2ByteFromInt(value, out g1, out g2);
                    register[registerIndex("E")] = (byte)Convert.ToInt32(g1, 16);
                    register[registerIndex("D")] = (byte)Convert.ToInt32(g2, 16);

                    registerPC++;
                }

                else if (byteInstruction == 0x1C)       //INR E
                {
                    byte temp;
                    byte save_flag;
                    save_flag = flag[0];
                    temp = register[registerIndex("A")];
                    register[registerIndex("A")] = register[registerIndex("E")];
                    initFlags();
                    register[registerIndex("A")] += 0x1;
                    changeFlags();
                    register[registerIndex("E")] = register[registerIndex("A")];
                    register[registerIndex("A")] = temp;
                    flag[0] = save_flag;
                    registerPC++;

                }

                else if (byteInstruction == 0x1D)       //DCR E
                {
                    byte temp;
                    byte save_flag;
                    save_flag = flag[0];
                    temp = register[registerIndex("A")];
                    register[registerIndex("A")] = register[registerIndex("E")];
                    initFlags();
                    register[registerIndex("A")] -= 0x1;
                    changeFlags();
                    register[registerIndex("E")] = register[registerIndex("A")];
                    register[registerIndex("A")] = temp;
                    flag[0] = save_flag;
                    registerPC++;

                }

                else if (byteInstruction == 0x1F)       //RAR   
                {
                    byte ac;
                    byte saveC;
                    byte c = 0;

                    ac = register[7];    // accumulator
                    if (flag[flagIndex("C")] == 1)
                    {
                        saveC = 1;
                    }
                    else
                    {
                        saveC = 0;
                    }

                    c = (byte)(0x01&ac);

                    ac /= 2;

                    ac += (byte)(saveC*0x80);

                    flag[flagIndex("C")] = c;
                    register[registerIndex("A")] = ac;

                    registerPC++;
                }

                else if (byteInstruction == 0x20)       //RIM
                {
                    registerPC++;
                }

                else if (byteInstruction == 0x22)       //SHLD
                {
                    int address = 0;
                    registerPC++;
                    registerPC++;
                    address = RAM[registerPC];
                    registerPC--;
                    address = (0x100 * address) + RAM[registerPC];
                    registerPC++;
                    RAM[address] = register[registerIndex("L")];
                    address++;
                    RAM[address] = register[registerIndex("H")];
                    registerPC++;
                }

                else if (byteInstruction == 0x23)       //INX H
                {
                    int value
                        = (0x100 * register[registerIndex("H")] + register[registerIndex("L")]);
                    value += 0x1;

                    string g1, g2;
                    get2ByteFromInt(value, out g1, out g2);
                    register[registerIndex("L")] = (byte)Convert.ToInt32(g1, 16);
                    register[registerIndex("H")] = (byte)Convert.ToInt32(g2, 16);

                    registerPC++;
                }

                else if (byteInstruction == 0x24)       //INR H
                {
                    byte temp;
                    byte save_flag;
                    save_flag = flag[0];
                    temp = register[registerIndex("A")];
                    register[registerIndex("A")] = register[registerIndex("H")];
                    initFlags();
                    register[registerIndex("A")] += 0x1;
                    changeFlags();
                    register[registerIndex("H")] = register[registerIndex("A")];
                    register[registerIndex("A")] = temp;
                    flag[0] = save_flag;
                    registerPC++;

                }

                else if (byteInstruction == 0x25)       //DCR H
                {
                    byte temp;
                    byte save_flag;
                    save_flag = flag[0];
                    temp = register[registerIndex("A")];
                    register[registerIndex("A")] = register[registerIndex("H")];
                    initFlags();
                    register[registerIndex("A")] -= 0x1;
                    changeFlags();
                    register[registerIndex("H")] = register[registerIndex("A")];
                    register[registerIndex("A")] = temp;
                    flag[0] = save_flag;
                    registerPC++;

                }

                else if (byteInstruction == 0x27)       //DAA // incomplete
                {
                    // if low order bits in r[A] is greater than 9 or flag AC is set
                    // 6 is added to low order bits

                    byte low=00;   // low order bits of Accumulator
                    byte high=00;  // high order bits of Accumulator

                    bool l = false;
                    bool h = false;

                    byte ad = 00;

                    low = (byte)(register[7] & 0x0F);
                    high = (byte)(register[7] & 0xF0);
                    high /= 2;
                    high /= 2;
                    high /= 2;
                    high /= 2;

                    if ((low > 9) || (flag[flagIndex("AC")] == 1))
                    {
                        l = true ;
                    }
                    

                    if (l == true)
                    {
                        ad = 0x06;
                    }
                    else if(l == false)
                    {
                        ad = 0x00;
                    }

                    initFlags();
                    register[7] += ad;
                    changeFlags();

                    l = h = false;
                    low = high = 00;
                    ad = 0;

                    low = (byte)(register[7] & 0x0F);
                    high = (byte)(register[7] & 0xF0);
                    high /= 2;
                    high /= 2;
                    high /= 2;
                    high /= 2;

                    if ((high > 9) || (flag[flagIndex("C")] == 1))
                    {
                        h = true;
                    }


                    if (h == true)
                    {
                        ad = 0x60;
                    }
                    else if (h == false)
                    {
                        ad = 0x00;
                    }

                    initFlags();
                    register[7] += ad;
                    changeFlags();
                    
                    registerPC++;
                }

                else if (byteInstruction == 0x29)       //DAD H
                {
                    int value1
                        = (0x100 * register[registerIndex("H")] + register[registerIndex("L")]);
                    int value2
                        = (0x100 * register[registerIndex("H")] + register[registerIndex("L")]);
                    int value = value1 + value2;


                    string g1, g2;
                    get2ByteFromInt(value, out g1, out g2);
                    register[registerIndex("L")] = (byte)Convert.ToInt32(g1, 16);
                    register[registerIndex("H")] = (byte)Convert.ToInt32(g2, 16);

                    registerPC++;
                }

                else if (byteInstruction == 0x2A)       //LHLD
                {
                    int address = 0;
                    registerPC++;
                    registerPC++;
                    address = RAM[registerPC];
                    registerPC--;
                    address = (0x100 * address) + RAM[registerPC];
                    registerPC++;
                    register[registerIndex("L")] = RAM[address];
                    address++;
                    register[registerIndex("H")] = RAM[address];
                    registerPC++;
                }

                else if (byteInstruction == 0x2B)       //DCX H
                {
                    int value 
                        = (0x100 * register[registerIndex("H")] + register[registerIndex("L")]);
                    value -= 0x1;

                    string g1, g2;
                    get2ByteFromInt(value, out g1, out g2);
                    register[registerIndex("L")] = (byte)Convert.ToInt32(g1, 16);
                    register[registerIndex("H")] = (byte)Convert.ToInt32(g2, 16);

                    registerPC++;
                }

                else if (byteInstruction == 0x2C)       //INR L
                {
                    byte temp;
                    byte save_flag;
                    save_flag = flag[0];
                    temp = register[registerIndex("A")];
                    register[registerIndex("A")] = register[registerIndex("L")];
                    initFlags();
                    register[registerIndex("A")] += 0x1;
                    changeFlags();
                    register[registerIndex("L")] = register[registerIndex("A")];
                    register[registerIndex("A")] = temp;
                    flag[0] = save_flag;
                    registerPC++;

                }

                else if (byteInstruction == 0x2D)       //DCR L
                {
                    byte temp;
                    byte save_flag;
                    save_flag = flag[0];
                    temp = register[registerIndex("A")];
                    register[registerIndex("A")] = register[registerIndex("L")];
                    initFlags();
                    register[registerIndex("A")] -= 0x1;
                    changeFlags();
                    register[registerIndex("L")] = register[registerIndex("A")];
                    register[registerIndex("A")] = temp;
                    flag[0] = save_flag;
                    registerPC++;

                }

                else if (byteInstruction == 0x30)       //SIM
                {
                    registerPC++;
                }

                else if (byteInstruction == 0x32)       //STA
                {
                    int address = 0;
                    registerPC++;
                    address = RAM[registerPC];
                    registerPC++;
                    address += (0x100 * RAM[registerPC]);
                    RAM[address] = register[7];
                    registerPC++;
                }

                else if (byteInstruction == 0x33)       //INX SP
                {

                    registerSP += 0x1;
                    registerPC++;
                }

                else if (byteInstruction == 0x34)       //INR M
                {
                    byte temp;
                    byte save_flag;
                    save_flag = flag[0];

                    int address = 0;
                    temp = register[registerIndex("A")];
                    address 
                        = (0x100 * register[registerIndex("H")] + register[registerIndex("L")]);
                    register[registerIndex("A")] = RAM[address];
                    initFlags();
                    register[registerIndex("A")] += 0x1;
                    changeFlags();
                    RAM[address] = register[registerIndex("A")];
                    register[registerIndex("A")] = temp;
                    flag[0] = save_flag;
                    registerPC++;
                }

                else if (byteInstruction == 0x35)       //DCR M
                {
                    byte temp;
                    byte save_flag;
                    save_flag = flag[0];

                    int address = 0;
                    temp = register[registerIndex("A")];
                    address 
                        = (0x100 * register[registerIndex("H")] + register[registerIndex("L")]);
                    register[registerIndex("A")] = RAM[address];
                    initFlags();
                    register[registerIndex("A")] -= 0x1;
                    changeFlags();
                    RAM[address] = register[registerIndex("A")];
                    register[registerIndex("A")] = temp;
                    flag[0] = save_flag;
                    registerPC++;
                }

                else if (byteInstruction == 0x37)       //STC
                {
                    flag[flagIndex("C")] = 1;
                    registerPC++;
                }

                else if (byteInstruction == 0x39)       //DAD SP
                {
                    int value1 = registerSP;
                    int value2 
                        = (0x100 * register[registerIndex("H")] + register[registerIndex("L")]);
                    int value = value1 + value2;


                    string g1, g2;
                    get2ByteFromInt(value, out g1, out g2);
                    register[registerIndex("L")] = (byte)Convert.ToInt32(g1, 16);
                    register[registerIndex("H")] = (byte)Convert.ToInt32(g2, 16);

                    registerPC++;
                }

                else if (byteInstruction == 0x3B)       //DCX SP
                {
                    registerSP -= 0x1;
                    registerPC++;
                }

                else if (byteInstruction == 0x3C)       //INR A
                {
                    byte save_flag;
                    save_flag = flag[0];
                    initFlags();
                    register[registerIndex("A")] += 0x1;
                    changeFlags();
                    flag[0] = save_flag;
                    registerPC++;
                }

                else if (byteInstruction == 0x3D)       //DCR A
                {
                    byte save_flag;
                    save_flag = flag[0];
                    initFlags();
                    register[registerIndex("A")] -= 0x1;
                    changeFlags();
                    flag[0] = save_flag;
                    registerPC++;
                }

                else if (byteInstruction == 0x3F)       //CMC
                {
                    if (flag[flagIndex("C")] == 1)
                        flag[flagIndex("C")] = 0;
                    else
                        flag[flagIndex("C")] = 1;
                    registerPC++;
                }

                else if (byteInstruction == 0xC0)       //RNZ
                {
                    if (flag[6] == 1)
                        registerPC++;
                    else
                    {
                        registerPC = registerSP;
                        registerSP++;
                        registerPC = (registerSP * 0x100) + registerPC;
                        registerSP++;

                        registerPC++;
                    }
                }

                else if (byteInstruction == 0xC1)       //POP B
                {
                    //No flags are modified
                    register[1] = RAM[registerSP];  //Lower-order registerC
                    registerSP++;
                    register[0] = RAM[registerSP];  //High-Order registerB
                    registerSP++;
                    registerPC++;
                }

                else if (byteInstruction == 0xC2)       //JNZ
                {
                    if (flag[6] == 1)
                        registerPC = registerPC + 3;
                    else
                    {
                        registerPC++;
                        registerPC = RAM[registerPC];
                    }
                }

                else if (byteInstruction == 0xC3)       //JMP
                {
                    int address = 0;
                    registerPC++;
                    address += RAM[registerPC];
                    registerPC++;
                    address += (0x100 * RAM[registerPC]);
                    registerPC++;

                    registerPC = address;
                }

                else if (byteInstruction == 0xC4)       //CNZ
                {
                    if (flag[6] == 1)
                    {
                        registerPC++;
                        registerPC++;
                        registerPC++;
                    }
                    else
                    {
                        int address = 0;
                        registerPC++;
                        address += RAM[registerPC];
                        registerPC++;
                        address += (0x100 * RAM[registerPC++]);
                        registerPC++;
                        register[7] = RAM[address];
                    }
                }

                else if (byteInstruction == 0xC5)       //PUSH B
                {
                    registerSP--;
                    RAM[registerSP] = register[0];
                    registerSP--;
                    RAM[registerSP] = register[1];

                    registerPC++;
                }

                else if (byteInstruction == 0xC7)       //RST 0
                {
                    registerPC++;
                }

                else if (byteInstruction == 0xC8)       //RZ
                {
                    if (flag[6] == 0)
                        registerPC++;
                    else
                    {
                        registerPC = registerSP;
                        registerSP++;
                        registerPC = (registerSP * 0x100) + registerPC;
                        registerSP++;

                        registerPC++;
                    }
                }

                else if (byteInstruction == 0xC9)       //RET
                {
                    int address;

                    address = RAM[registerSP];
                    registerSP++;
                    address += RAM[registerSP] * 0x100;
                    registerSP++;

                    registerPC++;

                    registerPC = address;
                }

                else if (byteInstruction == 0xCA)       //JZ
                {
                    if (flag[6] == 1)
                        registerPC = registerPC + 3;
                    else
                    {
                        int address = 0;
                        registerPC++;
                        address += RAM[registerPC];
                        registerPC++;
                        address += (0x100 * RAM[registerPC]);
                        registerPC++;
                        MessageBox.Show(address.ToString());
                        registerPC = address;
                    }
                }

                else if (byteInstruction == 0xCC)       //CZ
                {
                    if (flag[6] == 0)
                    {
                        registerPC++;
                        registerPC++;
                        registerPC++;
                    }
                    else
                    {
                        int address = 0;
                        registerPC++;
                        address += RAM[registerPC];
                        registerPC++;
                        address += (0x100 * RAM[registerPC]);
                        registerPC++;
                        register[7] = RAM[address];
                    }
                }

                else if (byteInstruction == 0xCD)       //CALL
                {
                    int address = 0;
                    registerPC++;
                    address += RAM[registerPC];
                    registerPC++;
                    address += (0x100 * RAM[registerPC]);
                    registerPC++;

                    string a1, a2;
                    get2ByteFromInt(registerPC, out a1, out a2);

                    registerSP--;
                    RAM[registerSP] = Convert.ToByte(a2, 16);
                    registerSP--;
                    RAM[registerSP] = Convert.ToByte(a1, 16);
                    

                    registerPC = address;
                }



                else if (byteInstruction == 0xCF)   //RST 1
                {
                    registerPC++;
                }

                else if (byteInstruction == 0xD0)   //RNC
                {
                    if (flag[0] == 1)
                        registerPC++;
                    else
                    {
                        registerPC = registerSP;
                        registerSP++;
                        registerPC += (registerSP * 0x100) + registerPC;
                        registerSP++;

                        registerPC++;
                    }
                }

                else if (byteInstruction == 0xD1)   //POP D
                {
                    //No flags are modified
                    register[3] = RAM[registerSP];  //Lower-order registerE
                    registerSP++;
                    register[2] = RAM[registerSP];  //High-Order registerD
                    registerSP++;
                    registerPC++;
                }
                else if (byteInstruction == 0xD2)   //JNC
                {
                    if (flag[0] == 0)
                        registerPC = registerPC + 3;
                    else
                    {
                        int address = 0;
                        registerPC++;
                        address += RAM[registerPC];
                        registerPC++;
                        address += (0x100 * RAM[registerPC]);
                        registerPC++;
                        MessageBox.Show(address.ToString());
                        registerPC = address;
                    }
                }

                else if (byteInstruction == 0xD3)   //OUT
                {
                    registerPC++;
                    PORT[RAM[registerPC]] = register[7];//OUTPUT PORT
                    registerPC++;
                }

                else if (byteInstruction == 0xD4)   //CNC   INCOMPLETE
                {
                    if (flag[0] == 0)
                    {
                        registerPC++;
                        registerPC++;
                        registerPC++;
                    }
                    else
                    {
                        int address = 0;
                        registerPC++;
                        address += RAM[registerPC];
                        registerPC++;
                        address += (0x100 * RAM[registerPC]);
                        registerPC++;

                        string a1, a2;
                        get2ByteFromInt(registerPC, out a1, out a2);

                        registerSP--;
                        RAM[registerSP] = Convert.ToByte(a2, 16);
                        registerSP--;
                        RAM[registerSP] = Convert.ToByte(a1, 16);


                        registerPC = address;
                    }
                }

                else if (byteInstruction == 0xD5)   //PUSH D
                {
                    registerSP--;
                    RAM[registerSP] = register[2];
                    registerSP--;
                    RAM[registerSP] = register[3];

                    registerPC++;
                }

                else if (byteInstruction == 0xD6)       //SUI
                {
                    registerPC++;
                    initFlags();
                    register[7] -= RAM[registerPC];
                    changeFlags();
                    registerPC++;
                }

                else if (byteInstruction == 0xD7)       //RST 2
                {
                    registerPC++;
                }

                else if (byteInstruction == 0xD8)       //RC
                {
                    if (flag[0] == 0)
                        registerPC++;
                    else
                    {
                        registerPC = registerSP;
                        registerSP++;
                        registerPC += (registerSP * 0x100) + registerPC;
                        registerSP++;

                        registerPC++;
                    }
                }

                else if (byteInstruction == 0xDA)       //JC
                {
                    if (flag[0] == 1)
                        registerPC = registerPC + 3;
                    else
                    {
                        int address = 0;
                        registerPC++;
                        address += RAM[registerPC];
                        registerPC++;
                        address += (0x100 * RAM[registerPC]);
                        registerPC++;

                        string a1, a2;
                        get2ByteFromInt(registerPC, out a1, out a2);

                        registerSP--;
                        RAM[registerSP] = Convert.ToByte(a1, 16);
                        registerSP--;
                        RAM[registerSP] = Convert.ToByte(a2, 16);


                        registerPC = address;
                    }
                }

                else if (byteInstruction == 0xDB)       //IN
                {
                    registerPC++;
                    register[7] = PORT[RAM[registerPC]];
                    registerPC++;
                }

                else if (byteInstruction == 0xDC)       //CC    
                {
                    if (flag[0] == 1)
                    {
                        registerPC++;
                        registerPC++;
                        registerPC++;
                    }
                    else
                    {
                        int address = 0;
                        registerPC++;
                        address += RAM[registerPC];
                        registerPC++;
                        address += (0x100 * RAM[registerPC]);
                        registerPC++;

                        string a1, a2;
                        get2ByteFromInt(registerPC, out a1, out a2);

                        registerSP--;
                        RAM[registerSP] = Convert.ToByte(a2, 16);
                        registerSP--;
                        RAM[registerSP] = Convert.ToByte(a1, 16);


                        registerPC = address;
                    }
                }

                else if (byteInstruction == 0xDF)       //RST 3
                {
                    registerPC++;
                }

                else if (byteInstruction == 0xE0)       //RPO
                {
                    if (flag[2] == 1)
                        registerPC++;
                    else
                    {
                        registerPC = registerSP;
                        registerSP++;
                        registerPC += (registerSP * 0x100) + registerPC;
                        registerSP++;

                        registerPC++;
                    }
                }

                else if (byteInstruction == 0xE1)       //POP H
                {
                    register[5] = RAM[registerSP];  //Lower-order registerE
                    registerSP++;
                    register[4] = RAM[registerSP];  //High-Order registerD
                    registerSP++;
                    registerPC++;
                }

                else if (byteInstruction == 0xE2)       //JPO
                {
                    if (flag[2] == 0)
                        registerPC = registerPC + 3;
                    else
                    {
                        int address = 0;
                        registerPC++;
                        address += RAM[registerPC];
                        registerPC++;
                        address += (0x100 * RAM[registerPC]);
                        registerPC++;

                        string a1, a2;
                        get2ByteFromInt(registerPC, out a1, out a2);

                        registerSP--;
                        RAM[registerSP] = Convert.ToByte(a1, 16);
                        registerSP--;
                        RAM[registerSP] = Convert.ToByte(a2, 16);


                        registerPC = address;
                    }
                }

                else if (byteInstruction == 0xE3)       //XTHL
                {
                    byte t1, t2;
                    t1 = register[registerIndex("L")];
                    t2 = register[registerIndex("H")];

                    register[registerIndex("L")] = RAM[registerSP];
                    register[registerIndex("H")] = RAM[registerSP + 1];

                    RAM[registerSP] = t1;
                    RAM[registerSP + 1] = t2;

                    registerPC++;
                }

                else if (byteInstruction == 0xE4)       //CPO
                {
                    if (flag[0] == 1)
                    {
                        registerPC++;
                        registerPC++;
                        registerPC++;
                    }
                    else
                    {
                        int address = 0;
                        registerPC++;
                        address += RAM[registerPC];
                        registerPC++;
                        address += (0x100 * RAM[registerPC]);
                        registerPC++;

                        string a1, a2;
                        get2ByteFromInt(registerPC, out a1, out a2);

                        registerSP--;
                        RAM[registerSP] = Convert.ToByte(a2, 16);
                        registerSP--;
                        RAM[registerSP] = Convert.ToByte(a1, 16);


                        registerPC = address;
                    }
                }

                else if (byteInstruction == 0xE5)       //PUSH H
                {
                    registerSP--;
                    RAM[registerSP] = register[4];
                    registerSP--;
                    RAM[registerSP] = register[5];

                    registerPC++;
                }

                else if (byteInstruction == 0xE7)       //RST 4
                {
                    registerPC++;
                }

                else if (byteInstruction == 0xE8)       //RPE
                {
                    if (flag[2] == 0)
                        registerPC++;
                    else
                    {
                        registerPC = registerSP;
                        registerSP++;
                        registerPC += (registerSP * 0x100) + registerPC;
                        registerSP++;

                        registerPC++;
                    }
                }

                else if (byteInstruction == 0xE9)       //PCHL
                {
                    registerPC = register[5];
                    registerPC = registerPC + (register[4] * 0x100);
                }

                else if (byteInstruction == 0xEA)       //JPE
                {
                    if (flag[2] == 1)
                        registerPC = registerPC + 3;
                    else
                    {
                        int address = 0;
                        registerPC++;
                        address += RAM[registerPC];
                        registerPC++;
                        address += (0x100 * RAM[registerPC]);
                        registerPC++;

                        string a1, a2;
                        get2ByteFromInt(registerPC, out a1, out a2);

                        registerSP--;
                        RAM[registerSP] = Convert.ToByte(a1, 16);
                        registerSP--;
                        RAM[registerSP] = Convert.ToByte(a2, 16);


                        registerPC = address;
                    }
                }

                else if (byteInstruction == 0xEB)       //XCHG
                {
                    byte temp;
                    temp = register[registerIndex("D")];
                    register[registerIndex("D")] = register[registerIndex("H")];
                    register[registerIndex("H")] = temp;

                    temp = register[registerIndex("L")];
                    register[registerIndex("L")] = register[registerIndex("E")];
                    register[registerIndex("E")] = temp;

                    registerPC++;

                }

                else if (byteInstruction == 0xEC)       //CPE
                {
                    if (flag[2] == 0)
                    {
                        registerPC++;
                        registerPC++;
                        registerPC++;
                    }
                    else
                    {
                        int address = 0;
                        registerPC++;
                        address += RAM[registerPC];
                        registerPC++;
                        address += (0x100 * RAM[registerPC]);
                        registerPC++;

                        string a1, a2;
                        get2ByteFromInt(registerPC, out a1, out a2);

                        registerSP--;
                        RAM[registerSP] = Convert.ToByte(a2, 16);
                        registerSP--;
                        RAM[registerSP] = Convert.ToByte(a1, 16);


                        registerPC = address;
                    }
                }

                else if (byteInstruction == 0xEE)       //XRI
                {
                    byte b;
                    registerPC++;
                    b = RAM[registerPC];

                    initFlags();
                    register[registerIndex("A")] ^= b;
                    changeFlags();

                    registerPC++;
                }

                else if (byteInstruction == 0xEF)       //RST 5
                {
                    registerPC++;
                }

                else if (byteInstruction == 0xF0)       //RP
                {
                    if (flag[7] == 1)
                        registerPC++;
                    else
                    {
                        registerPC = registerSP;
                        registerSP++;
                        registerPC += (registerSP * 0x100) + registerPC;
                        registerSP++;

                        registerPC++;
                    }
                }

                else if (byteInstruction == 0xF1)       //POP PSW 
                {
                    byte aflag,b;
                    aflag = RAM[registerSP];  //Lower-order registerA
                    registerSP++;
                    register[7] = RAM[registerSP];  //High-Order flags
                    registerSP++;

                    // Decoding flags

                    b = (byte)(aflag & 0x01);
                    flag[0] = b;

                    b = (byte)(aflag & 0x04);
                    b /= 0x4;
                    flag[2] = b;

                    b = (byte)(aflag & 0x10);
                    b /= 0x10;
                    flag[4] = b;

                    b = (byte)(aflag & 0x40);
                    b /= 0x40;
                    flag[6] = b;

                    b = (byte)(aflag & 0x80);
                    b /= 0x80;
                    flag[7] = b;

                    //
                    registerPC++;
                }

                else if (byteInstruction == 0xF2)       //JP
                {
                    if (flag[7] == 0)
                        registerPC = registerPC + 3;
                    else
                    {
                        int address = 0;
                        registerPC++;
                        address += RAM[registerPC];
                        registerPC++;
                        address += (0x100 * RAM[registerPC]);
                        registerPC++;

                        string a1, a2;
                        get2ByteFromInt(registerPC, out a1, out a2);

                        registerSP--;
                        RAM[registerSP] = Convert.ToByte(a1, 16);
                        registerSP--;
                        RAM[registerSP] = Convert.ToByte(a2, 16);


                        registerPC = address;
                    }
                }

                else if (byteInstruction == 0xF3)       //DI
                {
                    registerPC++;
                }

                else if (byteInstruction == 0xF4)       //CP
                {
                    if (flag[7] == 1)
                    {
                        registerPC++;
                        registerPC++;
                        registerPC++;
                    }
                    else
                    {
                        int address = 0;
                        registerPC++;
                        address += RAM[registerPC];
                        registerPC++;
                        address += (0x100 * RAM[registerPC]);
                        registerPC++;

                        string a1, a2;
                        get2ByteFromInt(registerPC, out a1, out a2);

                        registerSP--;
                        RAM[registerSP] = Convert.ToByte(a2, 16);
                        registerSP--;
                        RAM[registerSP] = Convert.ToByte(a1, 16);


                        registerPC = address;
                    }
                }

                else if (byteInstruction == 0xF5)       //PUSH PSW 
                {
                    byte aflag = 00;
                    aflag = (byte)(flag[7] * 0x80 + flag[6] * 0x40 + flag[4] * 0x10 + flag[2] * 0x4 + flag[0] * 0x1) ;

                    registerSP--;
                    RAM[registerSP] = register[7];
                    registerSP--;
                    RAM[registerSP] = aflag;

                    registerPC++;
                }

                else if (byteInstruction == 0xF7)       //RST 6
                {
                    registerPC++;
                }

                else if (byteInstruction == 0xF8)       //RM
                {
                    if (flag[flagIndex("S")] == 0)
                        registerPC++;
                    else
                    {
                        registerPC = registerSP;
                        registerSP++;
                        registerPC += (registerSP * 0x100) + registerPC;
                        registerSP++;

                        registerPC++;
                    }
                }

                else if (byteInstruction == 0xF9)       //SPHL
                {
                    registerSP = register[registerIndex("L")];
                    registerSP = registerSP + (0x100 * register[registerIndex("H")]);

                    registerPC++;
                }

                else if (byteInstruction == 0xFA)       //JM
                {
                    if (flag[7] == 1)
                        registerPC = registerPC + 3;
                    else
                    {
                        int address = 0;
                        registerPC++;
                        address += RAM[registerPC];
                        registerPC++;
                        address += (0x100 * RAM[registerPC]);
                        registerPC++;

                        string a1, a2;
                        get2ByteFromInt(registerPC, out a1, out a2);

                        registerSP--;
                        RAM[registerSP] = Convert.ToByte(a1, 16);
                        registerSP--;
                        RAM[registerSP] = Convert.ToByte(a2, 16);


                        registerPC = address;
                    }
                }

                else if (byteInstruction == 0xFB)       //EI
                {
                    registerPC++;
                }

                else if (byteInstruction == 0xFC)       //CM
                {
                    if (flag[7] == 0)
                    {
                        registerPC++;
                        registerPC++;
                        registerPC++;
                    }
                    else
                    {
                        int address = 0;
                        registerPC++;
                        address += RAM[registerPC];
                        registerPC++;
                        address += (0x100 * RAM[registerPC]);
                        registerPC++;

                        string a1, a2;
                        get2ByteFromInt(registerPC, out a1, out a2);

                        registerSP--;
                        RAM[registerSP] = Convert.ToByte(a2, 16);
                        registerSP--;
                        RAM[registerSP] = Convert.ToByte(a1, 16);


                        registerPC = address;
                    }
                }

                else if (byteInstruction == 0xFF)       //RST   7
                {
                    registerPC++;
                }
                else
                {
                    return 0xffff;
                }
            }
            return registerPC;
        }
       
        public string createListProgram(string[] line)    // create program listing
        {
            int k;
            for (k = 0; k < listProgram.Length; k++)
                listProgram[k] = "";

            string[] instructionSplit;
            string str="";
            char[] delimiters = new[] { ',', ' ' };
            int i=startLocation;
            int j;

            for (j = 0; j < listProgram.Length; j++)
            {
                listProgram[j] = (j+1).ToString().PadRight(3,' ') + ". " + line[j].PadRight(16,' '); 
            }


            j = RAMprogramLine[i];
            for (i = startLocation; i <= startLocation + usedRam && j<=program.Length; i++)
            {
                if (line[j].IndexOf(';') != -1)
                    str = line[j].Remove(line[j].IndexOf(';'));
                else
                    str = line[j];
                instructionSplit = str.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

                if (line[j].Contains("ORG") || line[j].Contains("org"))
                    i = getInt2Byte(instructionSplit[1]);

                while( (RAMprogramLine[i] != j)&&(j <= program.Length) )
                    {
                        j++;
                    }

                if (RAMprogramLine[i] == j)
                    listProgram[j] += " " + RAM[i].ToString("X").PadLeft(2, '0');
                   
            }

            string s="";
            for (j = 0; j < listProgram.Length; j++)
                s += listProgram[j] + "\n";

            return s;
        }

        public void clearRAM()  // clear the RAM
        {
            int i;
            for (i = 0; i < RAM.Length; i++)
                RAM[i] = 0x00;
        }

        public void clearPORT() // clear the PORTS
        {
            int i;
            for (i = 0; i < PORT.Length; i++)
                PORT[i] = 0x00;
        }
    }
}
