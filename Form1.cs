using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace ProxyScrapper
{
    public partial class Form1 : Form
    {
        private static List<string> urls = new List<string>();
        private static string urltocheck = "http://bloginstructions.blogspot.com/";
        private static string filename = GetFullPath("result.txt");

        public Form1()
        {
            InitializeComponent();
            this.textBox1.Text = urltocheck;
            this.listBox1.Items.Add("https://searchenginereports.net/free-proxy-list");
        }
       
        
        private void loadefaults()
        {
            this.button1.Enabled = false;
            var lines = this.listBox1.Items;
            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i].ToString();
                if (!urls.Contains(line))
                    urls.Add(line);
            }
            var proxies = GetProxies();
            if(proxies.Count > 0)
            {
                if (System.IO.File.Exists(filename))
                    System.IO.File.Delete(filename);
                if (!System.IO.File.Exists(filename))
                {
                    using (FileStream fs = File.Create(filename))
                    {
                        string proxi = string.Empty;
                        foreach (var proxy in proxies)
                        {
                            proxi += proxy.serverAddress + ":" + proxy.port + Environment.NewLine;
                        }
                        Byte[] info = new UTF8Encoding(true).GetBytes(proxi);
                        fs.Write(info, 0, info.Length);
                    }
                }
                this.label4.Text = "Done! File copied to " + filename;
            }
            else
            {
                this.label4.Text = "Done! ";
            }
            this.button1.Enabled = true;
        }

       
       
       private  List<ProxyServer> GetProxies()
        {
            this.label4.Text = "Started Looking for Proxy";
            List<ProxyServer> serversToReturn = new List<ProxyServer>();
            List<ProxyServer> serversToCheck = new List<ProxyServer>();
            foreach (var url in urls)
            {
                bool success;
                var results = GetUrl(url, out success);
                if (success == true && !string.IsNullOrEmpty(results))
                {
                    var proxies = ExtractProxies(results);
                    serversToCheck.AddRange(proxies);
                    this.label4.Text = "Total Proxies Found: " + serversToCheck.Count;
                }
            }
            this.label4.Text = "Checking Connectivity: " + serversToCheck.Count;
            if (serversToCheck.Count > 0)
            {
                serversToReturn.AddRange(CheckProxyServers(serversToCheck));
            }
            this.label4.Text = "Valid Proxies: " + serversToCheck.Count;
            WriteLog("total servers found:" + serversToCheck.Count + " Working:" + serversToReturn);
            return serversToReturn;
        }


        private static List<ProxyServer> CheckProxyServers(List<ProxyServer> servers)
        {

            List<ProxyServer> proxies = new List<ProxyServer>();
            Thread[] array = new Thread[servers.Count];
            ProxyServer[] serversList = servers.ToArray();
            for (int i = 0; i < array.Length; i++)
            {
                var test = i;
                var server = serversList[test];
                array[i] = new Thread(() => checkConnectivity(server));
                array[i].Start();
            }

            for (int i = 0; i < array.Length; i++)
            {
                array[i].Join();
            }
            return serversList.Where(x => x.active).ToList();
        } 
        private static List<ProxyServer> ExtractProxies(string result)
        {
            List<ProxyServer> found = new List<ProxyServer>();

            try
            {
                foreach (Match match in Regex.Matches(result, @"[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+:[0-9]+"))
                {

                    //_Global.log("Found new proxy server " + match.Value);
                    string[] server = match.Value.Split(':');

                    if (server.Length == 2)
                    {
                        int port;
                        if (Int32.TryParse(server[1], out port) == true)
                        {
                            found.Add(new ProxyServer() { port = port, serverAddress = server[0] });
                        }

                    }

                }
            }
            catch (Exception e)
            {
                WriteLog("", e);
            }
            return found;
        }

        private static void checkConnectivity(ProxyServer server)
        {
            bool success = false;
            try
            {
                WebRequest web = WebRequest.Create(urltocheck);
                web.Timeout = 15000;
                web.Proxy = new WebProxy(server.serverAddress, server.port);
                
                HttpWebResponse response = (HttpWebResponse)web.GetResponse();
                Stream dataStream = response.GetResponseStream();
                if (dataStream != null)
                {
                    StreamReader reader = new StreamReader(dataStream);
                    reader.ReadToEnd();
                    success = true;
                }
            }
            catch (Exception e)
            {
                success = false;
            }
            server.active = success;
        }

        private static string GetUrl(string url, out bool success, ProxyServer server = null)
        {
            var result = string.Empty;
            success = false;
            try
            {
                WebRequest web = WebRequest.Create(url);
                web.Timeout = 15000;
                if (server != null)
                {
                    web.Proxy = new WebProxy(server.serverAddress, server.port);
                }
                HttpWebResponse response = (HttpWebResponse)web.GetResponse();
                Stream dataStream = response.GetResponseStream();
                if (dataStream != null)
                {
                    StreamReader reader = new StreamReader(dataStream);
                    result = reader.ReadToEnd();
                    success = true;
                }
            }
            catch (Exception e)
            {
                success = false;
                if (server == null)
                    WriteLog("List Url dead at: " + url, e);

            }
            return result;
        }

        public static List<string> readTextFileLineByLine(string filePath)
        {
            int counter = 0;
            string line;
            List<string> resultStrings = new List<string>();
            try
            {
                if (File.Exists(filePath))
                {
                    // Read the file and display it line by line.  
                    System.IO.StreamReader file = new System.IO.StreamReader(filePath);
                    while ((line = file.ReadLine()) != null)
                    {
                        resultStrings.Add(line);
                        counter++;
                    }

                    file.Close();
                }
               
            }
            catch (Exception e)
            {
                WriteLog("", e);
            }

            return resultStrings;
        }
        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }
        public static string GetFullPath(string fileName)
        {
            string outputPath = Path.Combine(AssemblyDirectory, fileName);
            return outputPath;
        }
        public static void WriteLog(string text = "", Exception ex = null)
        {
           
                string filePath = GetFullPath("logs.txt");
                if (!System.IO.File.Exists(filePath))
                {
                    using (FileStream fs = File.Create(filePath))
                    {
                        Byte[] info = new UTF8Encoding(true).GetBytes("--------" + Environment.NewLine);
                        fs.Write(info, 0, info.Length);
                    }
                }
                if (File.Exists(filePath))
                {
                    if (ex != null)
                    {
                        using (StreamWriter writer = new StreamWriter(filePath, true))
                        {
                            writer.WriteLine("Service Error :" + text + "<br/>" + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "StackTrace :" +
                                             ex.StackTrace +
                                             "" + Environment.NewLine + "Date :" + DateTime.Now.ToString());
                            writer.WriteLine(Environment.NewLine +
                                             "-----------------------------------------------------------------------------" +
                                             Environment.NewLine);
                        }
                    }
                    if (!string.IsNullOrEmpty(text))
                    {
                        using (StreamWriter writer = new StreamWriter(filePath, true))
                        {
                            writer.WriteLine("System Message :" + text + Environment.NewLine + "Date: " + DateTime.Now.ToString());
                            if (ex != null)
                            {
                               writer.WriteLine("Service Error :" + text + "<br/>" + Environment.NewLine + "Message: " + ex.Message + Environment.NewLine + "StackTrace :" +
                                             ex.StackTrace +
                                             "" + Environment.NewLine + "Date :" + DateTime.Now.ToString());
                            }
                         
                            writer.WriteLine(Environment.NewLine +
                                             "-----------------------------------------------------------------------------" +
                                             Environment.NewLine);
                        }

                }
             }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            urltocheck = this.textBox1.Text;
        }

        private void removefromlist(object sender, EventArgs e)
        {
            this.listBox1.Items.Remove(this.listBox1.SelectedItem);
        }
        private void button1_Click(object sender, EventArgs e)
        {
            if(this.listBox1.Items.Count > 0 && isValidUrl(this.textBox1.Text) )
            {
                this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
                saveFileDialog1.InitialDirectory = @"C:\";
                saveFileDialog1.DefaultExt = "txt";
                saveFileDialog1.Filter = "txt files (*.txt)|*.txt";
                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    filename = saveFileDialog1.FileName;
                }
                if (!string.IsNullOrEmpty(filename))
                {
                    loadefaults();
                }
            }
            else
            {
                MessageBox.Show("Add Urls First");
            }
        }

       
        private void button2_Click(object sender, EventArgs e)
        {
            var site = this.textBox2.Text;
            if(isValidUrl(site) == false)
            {
                MessageBox.Show("invalid url");
                return;
            }
            if (this.listBox1.Items.Contains(site))
            {
                MessageBox.Show("Already Exist");
            }
            else 
            {
                this.listBox1.Items.Add(site);
                this.textBox2.Text = "";
            }
        }
        private static bool isValidUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return false;
            try
            {
                new Uri(url);
                return true;
            }
            catch (Exception)
            {

                return false;
            }
        }
    }

}

