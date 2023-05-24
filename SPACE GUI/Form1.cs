using Org.BouncyCastle.Cms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using MailKit.Net.Smtp;
using MailKit;
using MimeKit;
using System.IO;

namespace SPACE_GUI
{
    public partial class Form1 : Form
    {
        public List<string> limits = new List<string>();
        public int countLines; 
        List<string[]> initialValuesList = new List<string[]>();
        List<string> newLinesList = new List<string>();
        List<string> newLinesHeaderList = new List<string>();
        List<string> rowHeaderList = new List<string>();
        string filePath; 
        string newFilePath = "../../../WeatherReport.csv";
        string[] columnsHeader = new string[] { "Statistics", "Average", "Max", "Min", "Median" };  
        int columnCount = 0; 
        public int rowsCount = 0;

        public Form1()
        {
            InitializeComponent();
        }

        private void label1_Click(object sender, EventArgs e)
        {
            
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult;
            if (button1.Enabled)
            {
                dialogResult = MessageBox.Show("Sind Sie sicher, dass Sie die E-Mail senden möchten??", "Bestätigen",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            }
            else
            {
                dialogResult = MessageBox.Show("Are you sure you want to send the email?", "Confirm",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            }

            if (dialogResult == DialogResult.Yes && File.Exists(GetFilePath()) && CheckAuthentification())
            {
                if (button1.Enabled) MessageBox.Show("E-mail Gesendet!");
                else MessageBox.Show("Email Sent!");
                SendEmail();
                this.Close();
            }
            else if (dialogResult == DialogResult.Yes && !File.Exists(GetFilePath()) && CheckAuthentification())
            {
                if (button1.Enabled) MessageBox.Show("Die Datei existiert nicht!");
                else MessageBox.Show("The file doesn't exist!");
                this.Close();
            }
            else  
            {
                if (button1.Enabled) MessageBox.Show("Falsche Email-Daten!");
                else MessageBox.Show("Wrong Email Data!");
                this.Close();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged_1(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(textBox1.Text) && !string.IsNullOrEmpty(textBox2.Text) &&
                !string.IsNullOrEmpty(textBox3.Text) && !string.IsNullOrEmpty(textBox4.Text) &&
                button3.Enabled == false)
            {
                StartButton.Enabled = true;
            }
        }

        private void StartButton_EnabledChanged(object sender, EventArgs e)
        {
        }

        
        private void SendEmail()
        {
            filePath = GetFilePath();
             
            if (File.Exists(filePath))
            {
                string file;
                using (StreamReader reader = new StreamReader(filePath))
                {
                    file = reader.ReadToEnd();
                }
                string[] splittedFile = file.Split('\n');
                rowsCount = splittedFile.Length - 2;
                countLines = rowsCount;
                
                for (int h = 1; h < rowsCount + 1; h++)
                {
                    string singleLine = splittedFile[h];
                    string[] singleLineElements  = singleLine.Split(',');
                    columnCount = singleLineElements.Length - 1; 

                    string[] initialValuesArr = new string[columnCount];
                    for (int i = 1; i < columnCount + 1; i++) initialValuesArr[i - 1] = singleLineElements[i]; 

                    initialValuesList.Add(initialValuesArr);
                    rowHeaderList.Add(singleLineElements[0]); 

                    int[] intArr = new int[rowsCount];
                    intArr = StringArrayToIntArray(initialValuesArr);
                    if (intArr != null)
                    { 
                        string line = CreateLine(rowsCount, rowHeaderList.Last(), intArr);
                        newLinesHeaderList.Add(rowHeaderList.Last());
                        newLinesList.Add(line);
                    }
                    else
                    { 
                        newLinesHeaderList.Add(rowHeaderList.Last());
                        newLinesList.Add(rowHeaderList.Last());
                    }                
                }

                using (StreamWriter fileWriter = new StreamWriter(newFilePath))
                {
                    fileWriter.AutoFlush = true;
                    fileWriter.WriteLine(string.Join(",", columnsHeader));
                    List<List<int>> days = new List<List<int>>();
                    for (int i = 0; i < newLinesList.Count(); i++) fileWriter.WriteLine(newLinesList[i]);

                    for (int i = 0; i < rowsCount; i++)
                    {
                        List<int> listWithPerfectValues = FindPerfectValues(limits, i, columnCount, initialValuesList[i]);                        
                        days.Add(listWithPerfectValues);
                    } 
                    string perfectDay = FindPerfectDay(days);
                    fileWriter.WriteLine("Perfect day: " + perfectDay);
                }

                // email sending with MailKit using smtp
                var senderEmailAddress = GetSenderEmail();
                var receiverEmailAddress = GetReceiverEmail();
                var password = GetPassword();

                var email = new MimeMessage();
                email.From.Add(new MailboxAddress(senderEmailAddress, senderEmailAddress));
                email.To.Add(new MailboxAddress(receiverEmailAddress, receiverEmailAddress));
                email.Subject = "Wheather Report File";  
                var builder = new BodyBuilder();
                builder.Attachments.Add(newFilePath);
                email.Body = builder.ToMessageBody();

                using (var smtp = new SmtpClient())
                { 
                    smtp.Connect("smtp.abv.bg", 465, MailKit.Security.SecureSocketOptions.SslOnConnect);
                    try
                    { 
                        smtp.Authenticate(senderEmailAddress, password);
                        smtp.Send(email); 
                    }
                    catch(Exception ex)
                    { 
                        Console.WriteLine(ex); 
                    }
                    finally
                    {
                        smtp.Disconnect(true);
                    }
                }
            }
            else
            {
                Console.WriteLine("File doesn't exist");
            }
        }
        private int GetNumberRows()
        {
            var path = textBox1.Text;
            if (File.Exists(path))
            {
                string r;
                using (StreamReader reader = new StreamReader(path))
                {
                    r = reader.ReadToEnd();
                }
                string[] splittedFile = r.Split('\n');
                countLines = splittedFile.Length - 1;
            }
            return countLines;
        }
        private string GetFilePath()
        {
            return textBox1.Text;
        }
        private string GetSenderEmail()
        {
            return textBox2.Text;
        }
        private string GetPassword()
        {
            return textBox3.Text;
        }
        private string GetReceiverEmail()
        {
            return textBox4.Text;
        }
        private static string CreateLine(int rowsCount, string header, int[] lineElements)
        {
            string[] elementsForSingleLine = new string[rowsCount];
            elementsForSingleLine[0] = header;
            elementsForSingleLine[1] = CalculateAverageValue(lineElements);
            elementsForSingleLine[2] = CalculateMaxValue(lineElements);
            elementsForSingleLine[3] = CalculateMinValue(lineElements);
            elementsForSingleLine[4] = CalculateMedianValue(lineElements);
            return string.Join(",", elementsForSingleLine);
        }
        private static int[] StringArrayToIntArray(string[] arrString)
        { 
            int[] intValuesSingleLine = new int[arrString.Length];
            int parsedString = 0;
            bool canParse = int.TryParse(arrString[0], out parsedString);

            if (canParse) intValuesSingleLine = Array.ConvertAll(arrString, s => int.Parse(s)); 
            return intValuesSingleLine;
        }
        private static string CalculateAverageValue(int[] elements)
        {
            return Math.Round(Queryable.Average(elements.AsQueryable()), 2).ToString();
        }
        private static string CalculateMaxValue(int[] elements)
        {
            return elements.Max().ToString();
        }
        private static string CalculateMinValue(int[] elements)
        {
            return elements.Min().ToString();
        }
        private static string CalculateMedianValue(int[] elements)
        {
            Array.Sort(elements);
            return elements[elements.Length / 2 + 1].ToString();
        }
        private static List<int> FindPerfectValues(List<string> limitValues, int lineNumber, int columnCount, string[] stringLines)
        {
            var intVal = new int[stringLines.Length]; 
            intVal = StringArrayToIntArray(stringLines); 

            List<int> temp = new List<int>();
            if(intVal != null)
            {
                temp = new List<int>(IntValuesInInterval(columnCount, limitValues[lineNumber], intVal));
            }
            else
            {
                temp = new List<int>(StringValuesFiltration(limitValues[lineNumber], stringLines));
            } 
            return temp;
        }
        private static string FindPerfectDay(List<List<int>> perfectValues)
        { 
            List<int> days = new List<int>();
            days = perfectValues[0].Intersect(perfectValues[1]).ToList();
            for (int i = 3; i < perfectValues.Count(); i++) days = days.Intersect(perfectValues[i]).ToList(); 
            if(days.Count() != 0)
            {               
                return days[0].ToString(); 
            }
            else
            {
                return "None";
            }
        }

        private static List<int> IntValuesInInterval(int columnCount, string filter, int[] intValuesArr)
        {
            List<int> arrForStoring = new List<int>();
            int lowerLimit = int.MinValue;
            int upperLimit = int.MaxValue ;
            
            if (filter != "")
            {
                var filterValues = filter.Split('-');

                if (filterValues.Length == 2)
                { 
                    var res1 = 0;
                    var res2 = 0;
                    bool canParse1 = int.TryParse(filterValues[0], out res1);
                    bool canParse2 = int.TryParse(filterValues[1], out res2);
                    if (canParse1 && canParse2)
                    {
                        lowerLimit = res1;
                        upperLimit = res2;
                        if (lowerLimit > upperLimit)
                        {
                            var temp = lowerLimit;
                            lowerLimit = upperLimit;
                            upperLimit = temp;
                        }
                    }
                }
            }
            for (int i = 0; i < columnCount; i++)
            {
                if (intValuesArr[i] >= lowerLimit && intValuesArr[i] <= upperLimit) arrForStoring.Add(i + 1);
            }
            return arrForStoring;
        }

        private static List<int> StringValuesFiltration(string filter, string[] intValuesArr)
        {
            List<int> arrForStoring = new List<int>();

            if (filter != "")
            {
                var filterArr = filter.Split('-');
                for (int i = 0; i < intValuesArr.Length; i++)
                {
                    for (int j = 0; j < filterArr.Length; j++)
                    {
                        if (intValuesArr[i].ToLower() == filterArr[j].ToLower()) arrForStoring.Add(i + 1);
                    }
                }
            }
            else
            {
                for (int i = 0; i < intValuesArr.Length; i++)
                {
                    arrForStoring.Add(i + 1);                    
                }
            }
            return arrForStoring;
        }
        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void label5_Click_1(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            label1.Text = "Lokaler Dateipfad";
            label2.Text = "E-Mail-Adresse des Absenders";
            label3.Text = "Passwort";
            label4.Text = "E-Mail-Adresse des Empfängers";
            button2.Enabled = false;
            button1.Enabled = true;
            label5.Text = "Senden von SPACE-E-Mails mit ABV";
            StartButton.Text = "SENDE EMAIL";
            button3.Text = "WERTEFILTER";
        }
        private bool CheckAuthentification()
        {
            var success = false;
            using (var smtp = new SmtpClient())
            {
                smtp.Connect("smtp.abv.bg", 465, MailKit.Security.SecureSocketOptions.SslOnConnect);                
                try
                {
                    smtp.Authenticate(GetSenderEmail(), GetPassword());
                    success = true;
                }
                catch (Exception ex)
                {
                    success = false;
                }
            }
            return success;
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            label1.Text = "Local Data Path";
            label2.Text = "Sender Email Address";
            label3.Text = "Password";
            label4.Text = "Receiver Email Address";
            button1.Enabled = false;
            button2.Enabled = true;
            label5.Text = "Sending SPACE File With ABV";
            StartButton.Text = "SEND EMAIL";
            button3.Text = "VALUE FILTER";
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var path = textBox1.Text;
            if (File.Exists(path))
            {
                for (int i = 0; i < GetNumberRows()-1; i++)
                {
                    string value = "";
                    if (button1.Enabled)
                    {
                        if (InputBox("Wert", $"Legen Sie Werte für den Parameter {i + 1} im Format X-Y für Intervalle oder mehrere Zeichenfolgen und X-X für Einzelwerte fest:", ref value) == DialogResult.OK)
                        {
                            limits.Add(value);
                        }
                    }
                    else
                    {
                        if (InputBox("Values", $"Set values for parameter {i + 1} in format X-Y for interval or multiple stringsand X-X for single value:", ref value) == DialogResult.OK)
                        {
                            limits.Add(value);
                        }
                    }
                        
                }
                StartButton.Enabled = true;
                button3.Enabled = false;
            }
            else
            {
                if (button1.Enabled) MessageBox.Show("Die Datei existiert nicht!");
                else MessageBox.Show("The file doesn't exist!");
                this.Close();
            }
        }
        public static DialogResult InputBox(string title, string promptText, ref string value)
        {
            Form form = new Form();
            Label label = new Label();
            TextBox textBox = new TextBox();
            Button buttonOk = new Button();
            Button buttonCancel = new Button();
            form.Text = title;
            label.Text = promptText;
            buttonOk.Text = "OK";
            buttonCancel.Text = "Cancel";
            buttonOk.DialogResult = DialogResult.OK;
            buttonCancel.DialogResult = DialogResult.Cancel;
            label.SetBounds(36, 36, 372, 13);
            textBox.SetBounds(36, 86, 700, 20);
            buttonOk.SetBounds(228, 160, 160, 60);
            buttonCancel.SetBounds(400, 160, 160, 60);
            label.AutoSize = true;
            form.ClientSize = new Size(796, 307);
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.Controls.AddRange(new Control[] { label, textBox, buttonOk, buttonCancel });
            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;
            DialogResult dialogResult = form.ShowDialog();
            value = textBox.Text;
            return dialogResult;
        }
    }
}
