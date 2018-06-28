using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Xml.Serialization;
using System.IO;

namespace BackupRotator
{
    public class Config
    {
        private const string CONFIG_FILE_NAME = "config.xml";
        private static bool s_isInitialized = false;

        private static Config s_instance;
        public static Config Instance
        {
            get
            {
                if(!s_isInitialized)
                {
                    s_instance = new Config();
                    s_isInitialized = true;
                }

                return s_instance;
            }

            set
            {
                s_instance = value;
            }
        }

        public static bool Load()
        {
            bool result = false;
            try
            {
                XmlSerializer xs = new XmlSerializer(typeof(Config));
                using (FileStream fs = new FileStream(CONFIG_FILE_NAME, FileMode.Open))
                {
                    Config theConfig = (Config)xs.Deserialize(fs);
                    s_instance = theConfig;
                    s_isInitialized = true;
                }

                result = true;
            }
            catch(Exception e)
            {
                // Nothing to do
            }

            return result;
        }

        public static bool Save()
        {
            bool result = false;
            try
            {
                XmlSerializer xs = new XmlSerializer(typeof(Config));
                using (FileStream fs = new FileStream(CONFIG_FILE_NAME, FileMode.OpenOrCreate))
                {
                    xs.Serialize(fs, s_instance);
                }

                result = true;
            }
            catch(Exception e)
            {
                // Nothing to do
            }

            return result;
        }

        public int NumberOfBackups { get; set; }
        public int BackupInterval { get; set; }
    }
}
