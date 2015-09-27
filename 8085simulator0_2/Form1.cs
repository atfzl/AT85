using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace _8085simulator0_2
{
    public partial class Form1 : Form
    {
        public Form1()  // Consrtuctor for form
        {
            a = new Assembler85();  // create an object for assembler
            InitializeComponent();
            showMemoryPanel(0x2000);    // draw memory panel from location 0x2000
            showPortPanel(0x00);        // draw port panel from location 0x00
            programBackup = richTextBox1.Text;
        }

        Assembler85 a;  // declare an object for class Assembler85

                        //
                        //  we will access assembler and it's function from the object "a"
                        //  created from class Assembler85  
                        //
                    
        Label[] memoryAddressLabels = new Label[0x10];      // Rows of memory panel   
        Label[] memoryAddressIndexLabels = new Label[0x8];  // columns of memory panel
        Label[,] memoryTableLabels = new Label[0x10, 0x8];  // contents of memory panel table

        Label[] portAddressLabels = new Label[0x10];      // Rows of memory panel   
        Label[] portAddressIndexLabels = new Label[0x8];  // columns of memory panel
        Label[,] portTableLabels = new Label[0x10, 0x8];  // contents of memory panel table

        string chosen_file = "";    // file selected for saving 
                                    // this file is also the current opened file
        string programBackup;

        int usedRamSize = 0;        // the RAM occupied by assembly program
        int nextInstrAddress = 0;   // next instruction address, will be used for debugging

        //Label[] programLineLabel = new Label[1000];

        //
        //
        // Updating and Drawing the labels of Registers, Flags, Memory, PORT
        //
        //
        private void updateRegisters()  // this updates the registers in the form 
        {
            // example- change 10 to "A" then change "A" to "0A", note "A" is in HEX
            labelBRegister.Text = a.register[0].ToString("X").PadLeft(2, '0');  
            labelCRegister.Text = a.register[1].ToString("X").PadLeft(2, '0');
            labelDRegister.Text = a.register[2].ToString("X").PadLeft(2, '0');
            labelERegister.Text = a.register[3].ToString("X").PadLeft(2, '0');
            labelHRegister.Text = a.register[4].ToString("X").PadLeft(2, '0');
            labelLRegister.Text = a.register[5].ToString("X").PadLeft(2, '0');
            //
            labelARegister.Text = a.register[7].ToString("X").PadLeft(2, '0');

            labelPCRegister.Text = a.registerPC.ToString("X").PadLeft(4, '0');
            labelSPRegister.Text = a.registerSP.ToString("X").PadLeft(4, '0');
        }

        private void updateFlags()
        {
            labelCFlag.Text = a.flag[0].ToString().Replace("True", "1").Replace("False", "0");
            //
            labelPFlag.Text = a.flag[2].ToString().Replace("True", "1").Replace("False", "0");
            //
            labelACFlag.Text = a.flag[4].ToString().Replace("True", "1").Replace("False", "0");
            //
            labelZFlag.Text = a.flag[6].ToString().Replace("True", "1").Replace("False", "0");
            labelSFlag.Text = a.flag[7].ToString().Replace("True", "1").Replace("False", "0");
        }

        private void showMemoryPanel(int startAddress)  // draw memory panel starting from 
        {                                               // address startAddress
            int i = 0;
            int j;

            panelMemory.Controls.Clear();   // clear the memory panel

            //memory panel update
            // we can view 128 Byte of memory at a time
            // it will be in form of 16 X 8


            //memoryAddressLabels
            // Display initial memory address labels from 0x2000 to 0x20ff 
            // 
            // example
            //          
            //  2000
            //  2008
            //  2010
            //  ..
            //  ..
            //  2078
            //
            //  

            for (i = 0; i <= 0xF; i++)
            {
                memoryAddressLabels[i] = new Label();
            }
            i = 0;
            j = startAddress;
            foreach (Label lbl in memoryAddressLabels)
            {
                lbl.Name = "memoryAddressLabel" + i.ToString("X");
                lbl.Text = j.ToString("X").PadLeft(4, '0');
                lbl.Visible = true;
                lbl.Size = new System.Drawing.Size(40, 15);
                lbl.Location= new Point(10, 20 + 20 * i);
                panelMemory.Controls.Add(lbl);
                i++;
                j+=0x8;
            }

            //memoryAddressIndexLabels
            // Display the top row required for the memory table
            //
            // example
            //
            //  0 1 2 3 4 5 6 7 
            //
            //

            for (i = 0; i <= 0x7; i++)
            {
                memoryAddressIndexLabels[i] = new Label();
            }
            i = 0;
            j = 0x0;
            foreach (Label lbl in memoryAddressIndexLabels)
            {
                lbl.Name = "memoryAddressIndexLabel" + i.ToString("X");
                lbl.Text = j.ToString("X");
                lbl.Visible = true;
                lbl.Size = new System.Drawing.Size(20,15);
                lbl.Location = new Point(60 + 30 * i , 0);
                panelMemory.Controls.Add(lbl);
                i++;
                j+=1;
            }

            //memoryTableLabels
            // Display the memory contents
            //
            // example
            //
            //       0   1   2   3   4   5   6   7
            // 2000  00  00  00  00  00  00  00  00
            // 2008  01  03  FA  3F  BC  DD  23  48
            //
            
            for (i = 0; i <= 0xf; i++)
            {
                for (j = 0; j <= 0x7; j++ )
                {
                    memoryTableLabels[i,j] = new Label();
                }
            }
            i = 0;
            j = 0x0;
            foreach (Label lbl in memoryTableLabels)
            {
                lbl.Name = "memoryTableLabel" + i.ToString("X");
                lbl.Text = a.RAM[startAddress + (8 * i) + j].ToString("X").PadLeft(2, '0') ;
                lbl.Visible = true;
                lbl.Size = new System.Drawing.Size(30, 15);
                lbl.Location = new Point(60 + 30 * j, 20 + 20 * i);
                panelMemory.Controls.Add(lbl);
                j++;
                if (j == 8)
                {
                    j = 0;
                    i++;
                }
            }
        }

        private void showPortPanel(int startAddress)
        {
            int i = 0;
            int j;

            panelPort.Controls.Clear();

            //port panel update
            // we can view 128 Byte of memory at a time
            // it will be in form of 16 X 8


            //portAddressLabels
            // Display initial memory address labels from 0x2000 to 0x20ff 
            // 
            // example
            //          
            //  00
            //  08
            //  10
            //  ..
            //  ..
            //  78
            //
            //  

            for (i = 0; i <= 0xF; i++)
            {
                portAddressLabels[i] = new Label();
            }
            i = 0;
            j = startAddress;
            foreach (Label lbl in portAddressLabels)
            {
                lbl.Name = "portAddressLabel" + i.ToString("X");
                lbl.Text = j.ToString("X").PadLeft(2, '0');
                lbl.Visible = true;
                lbl.Size = new System.Drawing.Size(40, 15);
                lbl.Location = new Point(10, 20 + 20 * i);
                panelPort.Controls.Add(lbl);
                i++;
                j += 0x8;
            }

            //portAddressIndexLabels
            // Display the top row required for the port table
            //
            // example
            //
            //  0 1 2 3 4 5 6 7 
            //
            //

            for (i = 0; i <= 0x7; i++)
            {
                portAddressIndexLabels[i] = new Label();
            }
            i = 0;
            j = 0x0;
            foreach (Label lbl in portAddressIndexLabels)
            {
                lbl.Name = "portAddressIndexLabel" + i.ToString("X");
                lbl.Text = j.ToString("X");
                lbl.Visible = true;
                lbl.Size = new System.Drawing.Size(20, 15);
                lbl.Location = new Point(60 + 30 * i, 0);
                panelPort.Controls.Add(lbl);
                i++;
                j += 1;
            }

            //portTableLabels
            // Display the port contents
            //
            // example
            //
            //       0   1   2   3   4   5   6   7
            // 2000  00  00  00  00  00  00  00  00
            // 2008  01  03  FA  3F  BC  DD  23  48
            //

            for (i = 0; i <= 0xf; i++)
            {
                for (j = 0; j <= 0x7; j++)
                {
                    portTableLabels[i, j] = new Label();
                }
            }
            i = 0;
            j = 0x0;
            foreach (Label lbl in portTableLabels)
            {
                lbl.Name = "portTableLabel" + i.ToString("X");
                lbl.Text = a.PORT[startAddress + (8 * i) + j].ToString("X").PadLeft(2, '0');
                lbl.Visible = true;
                lbl.Size = new System.Drawing.Size(30, 15);
                lbl.Location = new Point(60 + 30 * j, 20 + 20 * i);
                panelPort.Controls.Add(lbl);
                j++;
                if (j == 8)
                {
                    j = 0;
                    i++;
                }
            }
        }

        //
        //  END of Updating Labels
        //


        private int getTextBoxMemoryStartAddress(string str)
        {                       // to get the memory start address from text box
            string txtval = textBoxMemoryStartAddress.Text;
            int n = 0;

            n = Convert.ToInt32(txtval, 16);    // convert HEX to INT

            if (n >= 0xFF80)
            {
                return (0xFF80);
            }
            else
            {
                return n;
            }
        }

        private void quitToolStripMenuItem_Click(object sender, EventArgs e)    // exit the program
        {
            Application.Exit();
        }

        private void textBoxMemoryStartAddress_KeyPress(object sender, KeyPressEventArgs e)
        {                                                       // enter only HEX values in textbox
            e.Handled = !("\b0123456789ABCDEFabcdef".Contains(e.KeyChar));
        }

        private void buttonMemoryStartAddress_Click(object sender, EventArgs e)
        {                                            // update memory panel with values from textbox
            showMemoryPanel(getTextBoxMemoryStartAddress(textBoxMemoryStartAddress.Text));
        }

        private void textBoxMemoryStartAddress_Leave(object sender, EventArgs e)
        {                                           // even if we leave the textbox, the memory panel                                                                         
                                                    // is updated
            showMemoryPanel(getTextBoxMemoryStartAddress(textBoxMemoryStartAddress.Text));
        }

        private void textBoxMemoryStartAddress_KeyDown(object sender, KeyEventArgs e)
        {                                           // when we press ENTER, memory panel is updated
            if (e.KeyData == Keys.Enter)
            {
                showMemoryPanel(getTextBoxMemoryStartAddress(textBoxMemoryStartAddress.Text));
            }
        }

        private void textBoxMemoryUpdateByte_KeyPress(object sender, KeyPressEventArgs e)
        {                                           // Enter only HEX values when updating byte
            e.Handled = !("\b0123456789ABCDEFabcdef".Contains(e.KeyChar));
        }

        private void buttonMemoryUpdate_Click(object sender, EventArgs e)
        {                                           // update memory which was input
            a.RAM[(int)numericUpDown1.Value] = Convert.ToByte(textBoxMemoryUpdateByte.Text, 16);
                                     // convert numericUpDown1(HEX) to int, convert string from textbox  
                                     // to BYTE
            if ((((int)numericUpDown1.Value) 
                >= getTextBoxMemoryStartAddress(textBoxMemoryStartAddress.Text)) 
                && (((int)numericUpDown1.Value) 
                <= getTextBoxMemoryStartAddress(textBoxMemoryStartAddress.Text) + 0x7F))
            {
                showMemoryPanel(getTextBoxMemoryStartAddress(textBoxMemoryStartAddress.Text));
            }
        }

        private void button1_Click(object sender, EventArgs e)  // goto next PAGE of RAM
        {
            int n = getTextBoxMemoryStartAddress(textBoxMemoryStartAddress.Text);

            if (n + 0x80 > 0xFFFF)
                return;
            textBoxMemoryStartAddress.Text = (n + 0x80).ToString("X");
            showMemoryPanel(getTextBoxMemoryStartAddress(textBoxMemoryStartAddress.Text));
        }

        private void button2_Click(object sender, EventArgs e)  // goto previous PAGE of RAM
        {
            int n = getTextBoxMemoryStartAddress(textBoxMemoryStartAddress.Text);

            if (n - 0x80 < 0x0000)
                return;
            textBoxMemoryStartAddress.Text = (n - 0x80).ToString("X");
            showMemoryPanel(getTextBoxMemoryStartAddress(textBoxMemoryStartAddress.Text));
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)    // open from menu
        {
            openFD.Title = "Select Assembly File";
            openFD.InitialDirectory = 
                System.Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            openFD.FileName = "";
            openFD.Filter = "8085 assembly|*.asm|All Files|*.*";

            if (openFD.ShowDialog() != DialogResult.Cancel)
            {
                chosen_file = openFD.FileName;
                //MessageBox.Show(chosen_file);
                System.IO.StreamReader asmprogramReader;
                asmprogramReader = new System.IO.StreamReader(chosen_file);
                richTextBox1.Text = asmprogramReader.ReadToEnd();
                asmprogramReader.Close();
            }
        }

        private void toolStripButtonOpen_Click(object sender, EventArgs e)  // open from icon
        {
            openFD.Title = "Select Assembly File";
            openFD.InitialDirectory = 
                System.Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            openFD.FileName = "";
            openFD.Filter = "8085 assembly|*.asm|All Files|*.*";

            if (openFD.ShowDialog() != DialogResult.Cancel)
            {
                chosen_file = openFD.FileName;
                //MessageBox.Show(chosen_file);
                System.IO.StreamReader asmprogramReader;
                asmprogramReader = new System.IO.StreamReader(chosen_file);
                richTextBox1.Text = asmprogramReader.ReadToEnd();
                asmprogramReader.Close();
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)    // save file from menu
        {
            if (chosen_file == "")
            {
                saveAsFD.Title = "Save File As";
                saveAsFD.InitialDirectory =
                    System.Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                saveAsFD.FileName = "";
                saveAsFD.Filter = "8085 assembly|*.asm|All Files|*.*";

                if (saveAsFD.ShowDialog() != DialogResult.Cancel)
                {
                    chosen_file = saveAsFD.FileName;
                    System.IO.StreamWriter asmprogramWriter;
                    asmprogramWriter = new System.IO.StreamWriter(chosen_file);
                    asmprogramWriter.Write(richTextBox1.Text);
                    asmprogramWriter.Close();
                }
            }

            else
            {
                System.IO.StreamWriter asmprogramWriter;
                asmprogramWriter = new System.IO.StreamWriter(chosen_file);
                asmprogramWriter.Write(richTextBox1.Text);
                asmprogramWriter.Close();
            }
        }

        private void toolStripButtonSave_Click(object sender, EventArgs e)  // save file from icon
        {
            if (chosen_file == "")
            {
                saveAsFD.Title = "Save File As";
                saveAsFD.InitialDirectory = 
                    System.Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                saveAsFD.FileName = "";
                saveAsFD.Filter = "8085 assembly|*.asm|All Files|*.*";

                if (saveAsFD.ShowDialog() != DialogResult.Cancel)
                {
                    chosen_file = saveAsFD.FileName;
                    System.IO.StreamWriter asmprogramWriter;
                    asmprogramWriter = new System.IO.StreamWriter(chosen_file);
                    asmprogramWriter.Write(richTextBox1.Text);
                    asmprogramWriter.Close();
                }
            }

            else
            {
                System.IO.StreamWriter asmprogramWriter;
                asmprogramWriter = new System.IO.StreamWriter(chosen_file);
                asmprogramWriter.Write(richTextBox1.Text);
                asmprogramWriter.Close();
            }
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e) // save as from menu
        {
            saveAsFD.Title = "Save File As";
            saveAsFD.InitialDirectory =
                System.Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            saveAsFD.FileName = "";
            saveAsFD.Filter = "8085 assembly|*.asm|All Files|*.*";

            if (saveAsFD.ShowDialog() != DialogResult.Cancel)
            {
                chosen_file = saveAsFD.FileName;
                System.IO.StreamWriter asmprogramWriter;
                asmprogramWriter = new System.IO.StreamWriter(chosen_file);
                asmprogramWriter.Write(richTextBox1.Text);
                asmprogramWriter.Close();
            }
        }

        private void toolStripButton4_Click(object sender, EventArgs e) // save as from icon
        {
            saveAsFD.Title = "Save File As";
            saveAsFD.InitialDirectory =
                System.Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            saveAsFD.FileName = "";
            saveAsFD.Filter = "8085 assembly|*.asm|All Files|*.*";

            if (saveAsFD.ShowDialog() != DialogResult.Cancel)
            {
                chosen_file = saveAsFD.FileName;
                System.IO.StreamWriter asmprogramWriter;
                asmprogramWriter = new System.IO.StreamWriter(chosen_file);
                asmprogramWriter.Write(richTextBox1.Text);
                asmprogramWriter.Close();
            }
        }

        //
        // Display Binary value of registers on mouse hover
        //
        private void registerHoverBinary(Label l)
        {
            string binaryval;
            binaryval = Convert.ToString(Convert.ToInt32(l.Text, 16), 2);   
                                        // change the HEX string to BINARY string
            binaryval = binaryval.PadLeft(8, '0');
            toolTipRegisterBinary.SetToolTip(l, binaryval); 
                          // show tooltip with string binaryval when we hover mouse over label l
        }

        private void labelARegister_MouseHover(object sender, EventArgs e)
        {
            registerHoverBinary(labelARegister);
        }

        private void labelBRegister_MouseHover(object sender, EventArgs e)
        {
            registerHoverBinary(labelBRegister);
        }

        private void labelCRegister_MouseHover(object sender, EventArgs e)
        {
            registerHoverBinary(labelCRegister);
        }

        private void labelDRegister_MouseHover(object sender, EventArgs e)
        {
            registerHoverBinary(labelDRegister);
        }

        private void labelERegister_MouseHover(object sender, EventArgs e)
        {
            registerHoverBinary(labelERegister);
        }

        private void labelHRegister_MouseHover(object sender, EventArgs e)
        {
            registerHoverBinary(labelHRegister);
        }

        private void labelLRegister_MouseHover(object sender, EventArgs e)
        {
            registerHoverBinary(labelLRegister);
        }

        private void labelPCRegister_MouseHover(object sender, EventArgs e)
        {
            string binaryval;
            binaryval = Convert.ToString(Convert.ToInt32(labelPCRegister.Text, 16), 2);   
                                                        // change the HEX string to BINARY string
            binaryval = binaryval.PadLeft(16, '0');
            toolTipRegisterBinary.SetToolTip(labelPCRegister, binaryval); 
                               // show tooltip with string binaryval when we hover mouse over label l
        }

        private void labelSPRegister_MouseHover(object sender, EventArgs e)
        {
            string binaryval;
            binaryval = Convert.ToString(Convert.ToInt32(labelPCRegister.Text, 16), 2);     
                                                        // change the HEX string to BINARY string
            binaryval = binaryval.PadLeft(16, '0');
            toolTipRegisterBinary.SetToolTip(labelPCRegister, binaryval); 
                               // show tooltip with string binaryval when we hover mouse over label l
        }

        //
        //  END of mouse hover
        //

        private void richTextBox1_SelectionChanged(object sender, EventArgs e)
        {                                           // the main assembly program region
            int index = richTextBox1.SelectionStart;        // get index of cursor in current program
            int line = richTextBox1.GetLineFromCharIndex(index);// get line number
            labelLineNumber.Text = (line+1).ToString();
                                                // line number is displayed  line [0] is 1ine 1                     

            int column = richTextBox1.SelectionStart - richTextBox1.GetFirstCharIndexFromLine(line);   
            labelColumnNumber.Text = (column+1).ToString();      
        }

        private void buttonNextPortPage_Click(object sender, EventArgs e)
        {
            showPortPanel(0x80);
        }

        private void buttonPrevPortPage_Click(object sender, EventArgs e)
        {
            showPortPanel(0x00);
        }

        private void textBoxPortUpdateByte_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = !("\b0123456789ABCDEFabcdef".Contains(e.KeyChar));
        }

        private void buttonPortUpdate_Click(object sender, EventArgs e)
        {
            a.PORT[(int)numericUpDown2.Value] = Convert.ToByte(textBoxPortUpdateByte.Text, 16);

            if (numericUpDown2.Value <= 0x7F)
                showPortPanel(0x00);
            else
                showPortPanel(0x80);
        }

        private void buttonCreateList_Click(object sender, EventArgs e)
        {
            MessageBox.Show(a.createListProgram(richTextBox1.Lines));
        }

        //
        // Clear RAM and PORTS
        //
        private void buttonClearRAM_Click(object sender, EventArgs e)
        {
            a.clearRAM();
            showMemoryPanel(getTextBoxMemoryStartAddress(textBoxMemoryStartAddress.Text));
            buttonRun.Visible = false;
            buttonCreateList.Visible = false;
        }

        private void buttonClearPORT_Click(object sender, EventArgs e)
        {
            a.clearPORT();
            showPortPanel(0x00);
        }

        //
        // END
        //

        private void changeColorRTBLine(int line)
        {
            richTextBox1.SelectionStart = 0;
            richTextBox1.SelectionLength = richTextBox1.Text.Length;
            richTextBox1.SelectionBackColor = System.Drawing.Color.White;
            int firstcharindex = richTextBox1.GetFirstCharIndexFromLine(line);
            int currentline = richTextBox1.GetLineFromCharIndex(firstcharindex);
            string currentlinetext = richTextBox1.Lines[currentline];
            richTextBox1.SelectionStart = firstcharindex;
            richTextBox1.SelectionLength = currentlinetext.Length;
            richTextBox1.SelectionBackColor = System.Drawing.Color.Red;
            //richTextBox1.Select(firstcharindex, currentlinetext.Length);
        }

        private void buttonAssemble_Click(object sender, EventArgs e)   // button for assembling
        {
            nextInstrAddress = Convert.ToInt32(textBoxProgramStart.Text, 16);
            // set the start address for debugger
            toolStripButtonStartDebug.Visible = true;   // make the Start Debug button Visible
            buttonRun.Visible = true;           // button to run is Visible
            buttonCreateList.Visible = true;    // button to list program is Visible
            try
            {
                a.changeProgramLength = richTextBox1.Lines.Length;// get the number of lines of program
                a.changeStartLocation = Convert.ToInt32(textBoxProgramStart.Text, 16);
                                                    // change start location of the program in RAM
                a.FirstPass(richTextBox1.Lines);    // run the first Pass of assembler
                usedRamSize = a.SecondPass();       // run second pass and store the used RAM
                showMemoryPanel(getTextBoxMemoryStartAddress(textBoxMemoryStartAddress.Text));  
                                                                    // show updated memory
            }
            catch (AssemblerErrorException) { }
        }

        private void buttonRun_Click(object sender, EventArgs e)
        {
            a.runProgram((Convert.ToInt32(textBoxProgramStart.Text, 16)), usedRamSize);    
                                                                // run the program 
            // program will run correctly because
            // usedRamSize is always greater than instructions in RAM
            showMemoryPanel(Convert.ToInt32(textBoxProgramStart.Text, 16));
            showPortPanel(0x00);
            updateRegisters();  // update register labels
            updateFlags();      // update falg labels
            textBoxMessage.Text += "Program Halted            ";
        }

        private void toolStripButtonStepIn_Click(object sender, EventArgs e)
        {
            if (nextInstrAddress != 0xffff)
            {
                nextInstrAddress = a.runProgram(nextInstrAddress, 1);
                updateRegisters();  // update register labels
                updateFlags();      // update flag labels
                if (nextInstrAddress != 0xffff && nextInstrAddress <= 0xffff)
                    changeColorRTBLine(a.RAMprogramLine[nextInstrAddress]);
            }
            else
            {
                labelLineNumber.Visible = true;
                labelColumnNumber.Visible = true;
                MessageBox.Show("Debugging Over");
                textBoxMessage.Text += "\nDebugging Over                 ";
                richTextBox1.Clear();
                richTextBox1.Text = programBackup;
                toolStripButtonStepIn.Visible = false;
                buttonRun.Visible = true;
                buttonCreateList.Visible = true;
                toolStripButtonStartDebug.Visible = true;
            }
        }

        private void toolStripButtonStartDebug_Click(object sender, EventArgs e)
        {
            nextInstrAddress = Convert.ToInt32(textBoxProgramStart.Text, 16); 
            textBoxMessage.Text += "\nDebugger Running                 ";
            programBackup = richTextBox1.Text;
            labelLineNumber.Visible = false;
            labelColumnNumber.Visible = false;
            toolStripButtonStepIn.Visible = true;
            buttonRun.Visible = false;
            buttonCreateList.Visible = false;
            toolStripButtonStartDebug.Visible = false;
            changeColorRTBLine(a.RAMprogramLine[nextInstrAddress]);
        }

        private void resetSimulatorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            programBackup = richTextBox1.Text;
            a = new Assembler85();
            showMemoryPanel(0x2000);
            showPortPanel(0x00);
            updateRegisters();
            updateFlags();
            textBoxMessage.Text = "";
            buttonRun.Visible = false;
            buttonCreateList.Visible = false;
            toolStripButtonStartDebug.Visible = false;
            toolStripButtonStepIn.Visible = false;
            richTextBox1.Text = programBackup;
        }

        private void toolStripButtonRestartSimulator_Click(object sender, EventArgs e)
        {
            programBackup = richTextBox1.Text;
            a = new Assembler85();
            showMemoryPanel(0x2000);
            showPortPanel(0x00);
            updateRegisters();
            updateFlags();
            textBoxMessage.Text = "";
            buttonRun.Visible = false;
            buttonCreateList.Visible = false;
            toolStripButtonStartDebug.Visible = false;
            toolStripButtonStepIn.Visible = false;
            richTextBox1.Clear();
            richTextBox1.Text = programBackup;
        }

        private void resetRAMToolStripMenuItem_Click(object sender, EventArgs e)
        {
            a.clearRAM();
            showMemoryPanel(getTextBoxMemoryStartAddress(textBoxMemoryStartAddress.Text));
            buttonRun.Visible = false;
            buttonCreateList.Visible = false;
        }

        private void resetPortsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            a.clearPORT();
            showPortPanel(0x00);
        }

        private void tabControl1_Enter(object sender, EventArgs e)
        {
            showPortPanel(0x00);
        }

    }
}
