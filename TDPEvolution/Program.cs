using System.Drawing;
using System.Xml;

namespace TSPEvolution
{
    internal class Program
    {

        static string getCross(CrossoverOptions c)
        {
            switch (c)
            {
                case CrossoverOptions.CX:
                    return "CX";
                case CrossoverOptions.OX:
                    return "OX";
                case CrossoverOptions.PMX:
                    return "PMX";
            }
            return "UNDEF";
        }
        static string getMut(MutationOptions c)
        {
            switch (c)
            {
                case MutationOptions.Inversion:
                    return "Inversion";
                case MutationOptions.Transposition:
                    return "Transposition";
            }
            return "UNDEF";
        }

        static List<City> GenerateRandomCities(int count)
        {
            Random r = new Random();
            var output = new List<City>();
            for (int i=0; i < count; i++)
            {
                output.Add(new City(r.Next(0, 1000), r.Next(0, 1000)));
            }
            return output;
        }
        static void Main(string[] args)
        {
            int page = 0;
            // Console interface
            Console.Clear();

            List<City> cities = new List<City>();
            CrossoverOptions crossoverType = CrossoverOptions.CX;
            MutationOptions mutationType = MutationOptions.Inversion;
            int popCount = 100;
            double eliteRatio = 0.7;
            double mutationChance = 0.2;
            int genLimit = 1000;
            bool noProgress = true;
            while (true)
            {
                switch (page)
                {
                    case 0:
                    default:
                        Console.WriteLine("TSPEvolution - Michal Szczerba - Projekt MP");
                        Console.WriteLine("Options - Data Input:");
                        Console.WriteLine("[1] Load city list from file");
                        Console.WriteLine("[2] Randomize city list");
                        Console.Write("> ");
                        string opt = Console.ReadLine() ?? "";
                        if (opt == "1")
                        {
                            Console.WriteLine("City file path: ");
                            string path = Console.ReadLine() ?? "";
                            var res = FileManager.ReadCityFile(path);
                            if (res == null)
                            {
                                Console.WriteLine("Press any key to continue.");
                                Console.ReadKey();
                                Console.Clear();
                                break;
                            }
                            cities = res;
                            page = 1;
                            Console.Clear();
                            break;
                        } else if (opt == "2")
                        {
                            int cityCount = 30;
                            Console.WriteLine("- City count (default 30): ");
                            Console.Write("> ");
                            opt = Console.ReadLine() ?? "";
                            if (opt != "" && (!int.TryParse(opt, out cityCount) || cityCount < 3))
                            {
                                Console.WriteLine("Wrong input.");
                                Console.ReadKey();
                                Console.Clear();
                                break;
                            }
                            cities = GenerateRandomCities(cityCount);
                            page = 1;
                        } else
                        {
                            Console.WriteLine("Option not found. Press any key to continue.");
                            Console.ReadKey();
                            Console.Clear();
                        }
                        break;
                    case 1:
                        Console.WriteLine("Options - Crossover Type:");
                        Console.WriteLine("[1] PMX");
                        Console.WriteLine("[2] OX");
                        Console.WriteLine("[3] CX");
                        Console.Write("> ");
                        opt = Console.ReadLine() ?? "";
                        if (opt == "1")
                        {
                            crossoverType = CrossoverOptions.PMX;
                            page = 2;
                            break;
                        }
                        else if (opt == "2")
                        {
                            crossoverType = CrossoverOptions.OX;
                            page = 2;
                            break;
                        }
                        else if (opt == "3")
                        {
                            crossoverType = CrossoverOptions.CX;
                            page = 2;
                            break;
                        }
                        else
                        {
                            Console.WriteLine("Option not found. Press any key to continue.");
                            Console.ReadKey();
                            Console.Clear();
                        }
                        break;
                    case 2:
                        Console.WriteLine("Options - Mutation Type:");
                        Console.WriteLine("[1] Inversion");
                        Console.WriteLine("[2] Transposition");
                        Console.Write("> ");
                        opt = Console.ReadLine() ?? "";
                        if (opt == "1")
                        {
                            mutationType = MutationOptions.Inversion;
                            page = 3;
                            break;
                        }
                        else if (opt == "2")
                        {
                            mutationType = MutationOptions.Transposition;
                            page = 3;
                            break;
                        }
                        else
                        {
                            Console.WriteLine("Option not found. Press any key to continue.");
                            Console.ReadKey();
                        }
                        Console.Clear();
                        break;
                    case 3:
                        Console.WriteLine("Options - Evolution Parameters:");
                        Console.WriteLine("- Population count (default 100): ");
                        Console.Write("> ");
                        opt = Console.ReadLine() ?? "";
                        if (opt != "" && (!int.TryParse(opt, out popCount) || popCount < 3))
                        {
                            Console.WriteLine("Wrong input.");
                            Console.ReadKey();
                            Console.Clear();
                            break;
                        }
                        Console.WriteLine("- Elite population ratio (default 0,7): ");
                        Console.Write("> ");
                        opt = Console.ReadLine() ?? "";
                        if (opt != "" && (!double.TryParse(opt, out eliteRatio) || eliteRatio < 0 || eliteRatio > 1))
                        {
                            Console.WriteLine("Wrong input.");
                            Console.ReadKey();
                            Console.Clear();
                            break;
                        }
                        Console.WriteLine("- Mutation chance (default 0,2): ");
                        Console.Write("> ");
                        opt = Console.ReadLine() ?? "";
                        if (opt != "" && (!double.TryParse(opt, out mutationChance) || mutationChance < 0 || mutationChance > 1))
                        {
                            Console.WriteLine("Wrong input.");
                            Console.ReadKey();
                            Console.Clear();
                            break;
                        }
                        page = 4;
                        break;
                    case 4:
                        Console.WriteLine("Options - Generation limit:");
                        Console.Write("> ");
                        opt = Console.ReadLine() ?? "";
                        if (opt != "" && (!int.TryParse(opt, out genLimit) || genLimit < 0))
                        {
                            Console.WriteLine("Wrong input.");
                            Console.ReadKey();
                            Console.Clear();
                            break;
                        }
                        Console.WriteLine("- Reset limiter when progress is made? (Y/N)");
                        Console.Write("> ");
                        noProgress = Console.ReadLine() == "Y";
                        page = 5;
                        Console.Clear();
                        break;
                    case 5:
                        var dMtx = GetDistanceMatrix(ref cities);
                        TSPWorker worker = new TSPWorker(dMtx, new TSPSettings
                        {
                            PopulationCount = popCount,
                            EliteRatio = eliteRatio,
                            MutationChance = mutationChance,
                            CrossoverType = crossoverType,
                            MutationType = mutationType,
                            RandomizeMutation = false
                        });
                        worker.RunEvolution(genLimit, noProgress);
                        Console.WriteLine("======");
                        Console.WriteLine("SUMMARY:");
                        Console.WriteLine($"Path Representation, {getCross(crossoverType)}, {getMut(mutationType)}");
                        Console.WriteLine($"Population {popCount}, Elite Ratio {eliteRatio}, Mut Chance {mutationChance}");
                        Console.WriteLine($"Best Distance: {worker.BestDistance} achieved after {worker.LastGeneration} generations");
                        worker.ShowSolution(worker.BestSolution);
                        DisplayProgressDistribution(worker);
                        Console.WriteLine("Press 'ENTER' to rerun the same settings.");
                        Console.WriteLine("Press 'ESCAPE' to restart the application.");
                        Console.WriteLine("Press 'SPACE' to save current city list to file.");
                        var k = Console.ReadKey();
                        if (k.Key == ConsoleKey.Enter) {
                            break;
                        }
                        else if (k.Key == ConsoleKey.Escape)
                            page = 0;
                        else if (k.Key == ConsoleKey.Spacebar)
                        {
                            Console.WriteLine("- Save file name or path:");
                            Console.Write("> ");
                            string path = Console.ReadLine() ?? "";
                            FileManager.SaveCityFile(path, cities);
                            Console.WriteLine("File saved.");
                            Console.ReadKey();
                        }
                        break;

                }
            }
            
        }

        public static void DisplayProgressDistribution(TSPWorker worker)
        {
            Console.WriteLine("Progress data (y axis - 1/10th of all generations, x axis - total progress made in distance):");
            var GroupedGenerations = new (int Bottom, int Top, double Total)[10];
            for (int i = 0; i < 10; i++)
            {
                int groupBottom = (int)Math.Floor((double)worker.LastGeneration / 10) * i;
                int groupTop = (int)Math.Floor((double)worker.LastGeneration / 10) * (i+1);
                GroupedGenerations[i] = (groupBottom, groupTop, 0);
            }
            double Total = 0;
            foreach (var prog in worker.ProgressData)
            {
                for (int i=0; i < 10; i++)
                {
                    var gr = GroupedGenerations[i];
                    if (prog.Generation <= gr.Top && prog.Generation >= gr.Bottom)
                    {
                        GroupedGenerations[i].Total += prog.Progress;
                        Total += prog.Progress;
                    }
                }
            }
            for (int i = 0; i < 10; i++)
            {
                var gr = GroupedGenerations[i];
                double percentage = Math.Round(gr.Total / Total * 100, 2);
                int barCount = (int)Math.Ceiling(percentage / 2);
                Console.Write((i+1)+"/10th\t");
                for (int k = 0; k < barCount; k++)
                    Console.Write("█");
                Console.Write($" {percentage}% ({Math.Round(gr.Total, 0)})\n");
                Console.WriteLine();
            }
        }

        public static double[][] GetDistanceMatrix(ref List<City> cityList)
        {
            double[][] distanceMatrix = new double[cityList.Count][];
            for (int i=0; i < distanceMatrix.Length; i++)
                distanceMatrix[i] = new double[cityList.Count];
            for (int i=0; i < distanceMatrix.Length; i++)
            {
                for (int j=0; j < distanceMatrix[i].Length; j++)
                {
                    int x1 = cityList[i].x, y1 = cityList[i].y;
                    int x2 = cityList[j].x, y2 = cityList[j].y;
                    distanceMatrix[i][j] = Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
                }
            }
            return distanceMatrix;
        }
    }
}