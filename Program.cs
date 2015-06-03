using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Net;
using System.Web.Script.Serialization;

using dict = System.Collections.Generic.Dictionary<string, object>;

namespace WowHeadQuestRipper
{
    class Program
    {

        static void Main(string[] args)
        {
            string m_readString;
            uint m_type;
            uint m_id;
            uint m_totalCount = 1;
            string m_url;
            string[] m_id_name = new string[] { "Creature", "Creature", "Gameobject", "Gameobject", "Item", "Item" };
            string[] m_id_type = new string[] { "Creature", "Creature", "Gameobject", "Gameobject", "Item", "Item" };
            string[] m_id_raw_name = new string[] { "npc", "npc", "object", "object", "item", "item" };
            string[] m_id_DB_name = new string[] { "creature_queststarter", "creature_questender", "gameobject_queststarter", "gameobject_questender", "item_queststarter", "item_questender" };
            Console.WriteLine("ProjectCoreDevs Wowhead Quest parser");
            StreamWriter file = File.CreateText("Quest.sql");
        menu:
            Console.WriteLine("Select parser type:");
            Console.WriteLine("0 - Creature quest start");
            Console.WriteLine("1 - Creature quest end");
            Console.WriteLine("2 - Gameobject quest start");
            Console.WriteLine("3 - Gameobject quest end");
            Console.WriteLine("4 - Item quest start");
            Console.WriteLine("5 - Item quest end");
            Console.WriteLine("6 - Exit");

            m_readString = Console.ReadLine();

            if (!uint.TryParse(m_readString, out m_type))
            {
                Console.WriteLine("Incorrect Value!");
                goto menu;
            }

            if (m_type < 0 || m_type > 6)
            {
                Console.WriteLine("Incorrect Value!");
                goto menu;
            }

            if (m_type == 6)
            {
                file.Close();
                Environment.Exit(0);
            }

            Console.WriteLine("Enter {0} Id:", m_id_name[m_type]);
            m_readString = Console.ReadLine();

            if (!uint.TryParse(m_readString, out m_id))
            {
                Console.WriteLine("Incorrect Value!");
                goto menu;
            }

            m_url = "http://www.wowhead.com/" + m_id_raw_name[m_type] + "=" + m_id;

            List<string> content;
            try
            {
                content = ReadPage(m_url);
            }
            catch (Exception e)
            {
                Console.WriteLine("{0} Id {1} Doesn't exist ({2})", m_id_name[m_type], m_id, e.Message);
                goto menu;
            }
            
            Regex r = new Regex(@"new Listview\(\{template: 'quest', id: 'starts'.*data: (\[.+\])\}\);");
            Regex r2 = new Regex(@"new Listview\(\{template: 'quest', id: 'starts'.*_totalCount:");
            if (m_type == 1)
            {
                r = new Regex(@"new Listview\(\{template: 'quest', id: 'ends'.*data: (\[.+\])\}\);");
                r2 = new Regex(@"new Listview\(\{template: 'quest', id: 'ends'.*computeDataFunc:");
            }
            if (m_type == 2)
            {
                r = new Regex(@"new Listview\(\{template: 'quest', id: 'starts'.*data: (\[.+\])\}\);");
                r2 = new Regex(@"new Listview\(\{template: 'quest', id: 'starts'.*_totalCount:");
            }
            if (m_type == 3)
            {
                r = new Regex(@"new Listview\(\{template: 'quest', id: 'ends'.*data: (\[.+\])\}\);");
                r2 = new Regex(@"new Listview\(\{template: 'quest', id: 'ends'.*_totalCount:");
            }
            if (m_type == 4)
            {
                r = new Regex(@"new Listview\(\{template: 'quest', id: 'starts'.*data: (\[.+\])\}\);");
                r2 = new Regex(@"new Listview\(\{template: 'quest', id: 'starts'.*_totalCount:");
            }
            if (m_type == 5)
            {
                r = new Regex(@"new Listview\(\{template: 'quest', id: 'ends'.*data: (\[.+\])\}\);");
                r2 = new Regex(@"new Listview\(\{template: 'quest', id: 'ends'.*_totalCount:");
            }
            foreach (string line in content)
            {
                Match m2 = r2.Match(line);
                Match m = r.Match(line);
                if (m2.Success)
                {
                    string str = m2.Groups[0].Captures[0].Value;
                    string[] numbers = Regex.Split(str, @"\D+");
                    m_totalCount = uint.Parse(numbers[1]);
                }
                if (!m.Success)
                {
                    continue;
                }
                file.WriteLine("-- Parsed {0} quest for {1} id : {2} ", m_id_type[m_type], m_id_name[m_type], m_id);
                file.WriteLine("DELETE FROM `{0}` WHERE `Entry` = {1} ;", m_id_DB_name[m_type], m_id);
                file.WriteLine();
                var json = new JavaScriptSerializer() { MaxJsonLength = int.MaxValue };
                string data = m.Groups[1].Captures[0].Value;
                data = data.Replace("[,", "[0,");   // otherwise deserializer complains
                object[] m_object = (object[])json.DeserializeObject(data);
                foreach (dict objectInto in m_object)
                {
                    try
                    {
                        int id = (int)objectInto["id"];
                        string name = "";

                        if (objectInto.ContainsKey("name"))
                            name = (string)objectInto["name"];
                        file.WriteLine("INSERT INTO `{0}` VALUES ( '{1}', '{2}'); -- {3}",
                        m_id_DB_name[m_type], m_id, id, name);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
                // should have only one data line
                break;
            }
            Console.WriteLine();
            Console.WriteLine("Sucessfully parsed {0}: {1}", m_id_name[m_type], m_id);
            Console.WriteLine();
            file.WriteLine();
            file.Flush();
            goto menu;
        }

        static List<string> ReadPage(string url)
        {
            WebRequest wrGETURL = WebRequest.Create(url);
            Stream objStream = wrGETURL.GetResponse().GetResponseStream();
            StreamReader objReader = new StreamReader(objStream);

            string sLine = "";
            int i = 0;
            List<string> content = new List<string>();
            while (sLine != null)
            {
                i++;
                sLine = objReader.ReadLine();
                if (sLine != null)
                    content.Add(sLine);
            }
            return content;

        }
    }
}
