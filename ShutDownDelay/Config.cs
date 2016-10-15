using System.IO;
using Newtonsoft.Json;

namespace ShutDownDelay
{
    public class Config
    {
        public int delay = 30;
        public string reason = "";
        public int[] notifyIntervals = { 5, 10, 15, 20, 25, 30, 60 };

        public void Write(string path)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        public Config Read(string path)
        {
            return !File.Exists(path)
                ? new Config()
                : JsonConvert.DeserializeObject<Config>(File.ReadAllText(path));
        }
    }
}
