using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSPEvolution
{
    internal class City
    {
        public int x, y;
        public City(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }
    internal class FileManager
    {
        public static List<City>? ReadCityFile(string path)
        {
            if (!File.Exists(path))
            {
                Console.WriteLine("FileManager.ReadCityFile: File not found.");
                return null;
            }
            List<City> result = new List<City>();
            string[] lines = File.ReadAllLines(path);
            foreach (string line in lines)
            {
                string[] data = line.Split("  ");
                if (data.Length < 2)
                    continue;
                int x = 0, y = 0;
                if (!int.TryParse(data[0], out x) || !int.TryParse(data[1], out y))
                    continue;
                result.Add(new City(x, y));
            }
            if (result.Count < 3)
            {
                Console.WriteLine("FileManager.ReadCityFile: File doesn't have enough cities.");
                return null;
            }
            return result;
        }

        public static void SaveCityFile(string path, List<City> cities)
        {
            string[] contents = new string[cities.Count];
            for (int i=0; i < cities.Count; i++)
                contents[i] = $"{cities[i].x}  {cities[i].y}";
            File.WriteAllLines(path, contents);
        }
    }
}
