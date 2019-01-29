using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SpreadMoney
{
    class Spreader
    {
        public static readonly string Neo = @"c56f33fc6ecfcd0c225c4ab356fee59390af8560be0e930faebe74a6daff7c9b";
        public static readonly string spreadMoneyCommand = @"spreadMoney";
        public static readonly string spreadMoneyCommand2 = @"spreadMoney2";
        public static readonly string accountNumCommand = @"accountNum";
        public static readonly string configFileURL = @"config.ini";
        public static readonly int maxSmashCount = 10000;
        public static string testeeURL = null;
        public static int spreadAmountPerAccount = 1;
        public static string targetAccount = null;
        public static int migrateCount2 = 0;

        public static bool readConfig()
        {
            if (!File.Exists(configFileURL)) return false;
            StreamReader reader = new StreamReader(configFileURL);
            testeeURL = reader.ReadLine();
            if (testeeURL == null || testeeURL == "" || reader.EndOfStream) return false;
            spreadAmountPerAccount = int.Parse(reader.ReadLine());
            if (spreadAmountPerAccount <= 0 || reader.EndOfStream) return false;
            migrateCount2 = int.Parse(reader.ReadLine());
            if (migrateCount2 <= 0 || reader.EndOfStream) return false;
            targetAccount = reader.ReadLine(); ;
            if (targetAccount == null || targetAccount == "") return false;
            return true;
        }

        public static string CreateCommand(string command, List<string> parameters)
        {
            string result = testeeURL + @"/?jsonrpc=2.0&method=" + command + @"&params=[";
            if (parameters.Count != 0)
            {
                foreach (string parameter in parameters)
                {
                    result += "\"" + parameter + "\",";
                }
                result = result.Substring(0, result.Length - 1);
            }
            result += @"]&id=1";
            return result;
        }

        public static int AccountNum()
        {
            HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(new Uri(CreateCommand(accountNumCommand, new List<string> { })));
            webReq.Method = "GET";
            webReq.ContentType = "application/x-www-form-urlencoded";
            webReq.Timeout = 600000;
            webReq.ContentLength = 0;
            HttpWebResponse response = (HttpWebResponse)webReq.GetResponse();
            StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
            string ret = sr.ReadToEnd();
            sr.Close();
            response.Close();
            ReturnedResult deserializedResult = JsonConvert.DeserializeObject<ReturnedResult>(ret);
            return int.Parse(deserializedResult.result);
        }

        public static bool SpreadMoneyStep(int startPos, int migrateCount)
        {
            if (startPos < 0 || migrateCount <= 0) return false;
            HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(new Uri(CreateCommand(spreadMoneyCommand, new List<string> { Neo, startPos.ToString(), migrateCount.ToString(), spreadAmountPerAccount .ToString()})));
            webReq.Method = "GET";
            webReq.ContentType = "application/x-www-form-urlencoded";
            webReq.Timeout = 600000;
            webReq.ContentLength = 0;
            HttpWebResponse response = (HttpWebResponse)webReq.GetResponse();
            StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
            string ret = sr.ReadToEnd();
            sr.Close();
            response.Close();
            ReturnedResult deserializedResult = JsonConvert.DeserializeObject<ReturnedResult>(ret);
            if (deserializedResult == null || deserializedResult.result == null)
            {
                Console.WriteLine(ret);
                return false;
            }
            if (deserializedResult.result.Equals("Success")) return true;
            else return false;
        }

        public static bool SpreadMoneyStep2(int migrateCount)
        {
            if (migrateCount <= 0) return false;
            HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(new Uri(CreateCommand(spreadMoneyCommand2, new List<string> { Neo, targetAccount, migrateCount.ToString(), spreadAmountPerAccount.ToString() })));
            webReq.Method = "GET";
            webReq.ContentType = "application/x-www-form-urlencoded";
            webReq.Timeout = 600000;
            webReq.ContentLength = 0;
            HttpWebResponse response = (HttpWebResponse)webReq.GetResponse();
            StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
            string ret = sr.ReadToEnd();
            sr.Close();
            response.Close();
            ReturnedResult deserializedResult = JsonConvert.DeserializeObject<ReturnedResult>(ret);
            if (deserializedResult == null || deserializedResult.result == null)
            {
                Console.WriteLine(ret);
                return false;
            }
            if (deserializedResult.result.Equals("Success")) return true;
            else return false;
        }

        public static bool SpreadMoney()
        {
            Console.WriteLine("Spreading money...");
            int currentAccountNum = AccountNum();
            if (currentAccountNum <= 0) return false;
            int currentStart = 0;
            int currentCount = Math.Min(maxSmashCount, currentAccountNum);
            while (true)
            {
                if (SpreadMoneyStep(currentStart, currentCount))
                {
                    Console.WriteLine("Have spreaded from account " + currentStart + " to account " + currentCount);
                    currentStart = currentCount;
                    currentCount = Math.Min(currentCount + maxSmashCount, currentAccountNum);
                    if (currentCount == currentStart) break;
                }
                else
                {
                    Console.WriteLine("Error in spreading money.");
                }
                Thread.Sleep(20000);
            }
            Console.WriteLine("Wallet smashed successfully.");
            return true;
        }

        public static bool SpreadMoney2()
        {
            Console.WriteLine("Spreading money...");
            int currentCount = Math.Min(maxSmashCount, migrateCount2);
            int sumCounts = 0;
            while (true)
            {
                if (SpreadMoneyStep2(currentCount))
                {
                    sumCounts += currentCount;
                    Console.WriteLine("Have spreaded " + sumCounts + " UTXOs");
                    if (sumCounts >= migrateCount2) break;
                    currentCount = Math.Min(maxSmashCount, migrateCount2 - sumCounts);
                }
                else
                {
                    Console.WriteLine("Error in spreading money.");
                }
                Thread.Sleep(20000);
            }
            Console.WriteLine("Wallet smashed successfully.");
            return true;
        }

        static void Main(string[] args)
        {
            if (!readConfig())
            {
                Console.WriteLine("Config.ini invalid");
                return;
            }
            //SpreadMoney();
            SpreadMoney2();

            Console.WriteLine("Input \"exit\" to finish test.");
            while (!Console.ReadLine().ToLower().Equals("exit"))
            {
            }

            Console.ReadLine();
        }
    }

    class ReturnedResult
    {
        public string jsonrpc = null;
        public string id = null;
        public string result = null;
    }
}
