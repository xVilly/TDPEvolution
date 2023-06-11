using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace TSPEvolution
{
    internal enum CrossoverOptions { 
        PMX,
        OX,
        CX
    }
    internal enum MutationOptions {
        Inversion,
        Transposition
    }

    internal class TSPSettings
    {
        public int PopulationCount;
        public double MutationChance;
        public double EliteRatio;
        public CrossoverOptions CrossoverType;
        public MutationOptions MutationType;
        public bool RandomizeMutation;
    }

    internal class TSPWorker
    {
        public delegate (int[] Child1, int[] Child2) CrossoverStructure(ref int[] Parent1, ref int[] Parent2);
        public delegate void MutationStructure(ref int[] Child);

        public TSPSettings Settings;

        public Random Random;
        public double[][] DistanceMtx;

        public int[] BestSolution;
        public double BestDistance;

        public List<(int[] Tour, double Distance)> Population { get; set; }

        public int CityCount;
        public int LastGeneration = 0;

        public DateTime StartTime;
        public DateTime EndTime;

        public List<(int Generation, double Progress)> ProgressData;

        /// <summary>
        /// Constructor for TSP Worker. Initializes all class members such as Population, Randomizer..
        /// </summary>
        /// <param name="distanceMtx">Distance matrix of all cities</param>
        /// <param name="settings">TSPSettings structure</param>
        public TSPWorker(double[][] distanceMtx, TSPSettings settings)
        {
            // Initialize class members
            Settings = settings;
            Random = new Random();
            DistanceMtx = distanceMtx;

            BestDistance = 0;

            CityCount = DistanceMtx.Length;

            ProgressData = new List<(int, double)>();

            // Fill population map and their total distances (list of solutions)
            // each solution has a tour and total distance
            // Initial population map is fully randomized
            Population = new List<(int[], double)>();
            for (int i=0; i < Settings.PopulationCount; i++)
            {
                int[] randomTour = new int[CityCount];
                for (int j=0; j < CityCount; j++)
                    randomTour[j] = j;
                for (int k = 0; k < CityCount; ++k)
                {
                    int l = Random.Next(k, CityCount);
                    int _temp = randomTour[l];
                    randomTour[l] = randomTour[k];
                    randomTour[k] = _temp;
                }
                Population.Add((randomTour, CalculateTotalDistance(randomTour)));
            }

            // Find initial BestSolution and BestDistance by sorting the population map ascending
            Population.Sort((x, y) => x.Distance.CompareTo(y.Distance));
            BestSolution = new int[CityCount];
            for (int i = 0; i < CityCount; i++)
                BestSolution[i] = Population[0].Tour[i];
            BestDistance = Population[0].Distance;
            Console.WriteLine($"Best initial distance: {BestDistance}");
        }

        /// <summary>
        /// Uses distance matrix to calculate a total distance between all points in specified order
        /// </summary>
        /// <param name="solution">Solution (city order)</param>
        /// <returns>Total travel distance of the solution</returns>
        public double CalculateTotalDistance(int[] solution)
        {
            double totalDistance = 0;
            for (int i = 0; i < CityCount - 1; i++)
                totalDistance += DistanceMtx[solution[i]][solution[i + 1]];
            totalDistance += DistanceMtx[solution[CityCount - 1]][0];
            return totalDistance;
        }


        /// <summary>
        /// Runs evolution for specified amount of generations. Results are saved to BestSolution and BestDistance members.
        /// </summary>
        /// <param name="Generations">Amount of generations to iterate through</param>
        /// <param name="NoImprovement">If true, the iteration counter resets each time there's a progress (lower total distance)</param>
        public void RunEvolution(int Generations, bool NoImprovement = true)
        {
            StartTime = DateTime.Now;
            int CurrentGen = 0;
            int NoImprovementGens = 0;
            int TotalGen = LastGeneration;

            CrossoverStructure crossoverMethod;
            MutationStructure mutationMethod;
            // Choose crossover
            switch (Settings.CrossoverType)
            {
                case CrossoverOptions.OX:
                    crossoverMethod = OXCross;
                    break;
                case CrossoverOptions.CX:
                    crossoverMethod = CXCross;
                    break;
                case CrossoverOptions.PMX:
                default:
                    crossoverMethod = PMXCross;
                    break;
            }

            if (Settings.RandomizeMutation)
                Settings.MutationType = (MutationOptions)Random.Next(0, (int)MutationOptions.Transposition);

            switch (Settings.MutationType)
            {
                case MutationOptions.Transposition:
                    mutationMethod = MutateTransposition;
                    break;
                case MutationOptions.Inversion:
                default:
                    mutationMethod = MutateInversion;
                    break;
            }

            while (true)
            {
                TotalGen = CurrentGen + LastGeneration;
                if (NoImprovement)
                {
                    if (NoImprovementGens > Generations)
                    {
                        LastGeneration = TotalGen;
                        Console.WriteLine($"{CurrentGen}");
                        Console.WriteLine($"No improvement for {Generations} generations. Stopping evolution.");
                        break;
                    }
                }
                else
                {
                    if (CurrentGen > Generations)
                    {
                        LastGeneration = TotalGen;
                        Console.WriteLine($"Stopping evolution after {Generations} generations.");
                        break;
                    }
                }
                bool ImprovementOccured = false;

                // Select the strongest inviduals as the new population
                int OldPopCount = Population.Count;
                Population = Population.Take(Settings.PopulationCount).ToList();
                var elite = Population.Take((int)(Population.Count * Settings.EliteRatio)).ToList();

                // Breed elites
                List<(int[] Tour, double Distance)> children = new List<(int[] Tour, double Distance)>();
                while(elite.Count > 1)
                {
                    int count = elite.Count;
                    int elite1Idx = Random.Next(0, count);
                    int[] elite1 = elite[elite1Idx].Tour;
                    elite.RemoveAt(elite1Idx);
                    int elite2Idx = Random.Next(0, count - 1);
                    int[] elite2 = elite[elite2Idx].Tour;
                    elite.RemoveAt(elite2Idx);
                    var breed = crossoverMethod(ref elite1, ref elite2);
                    children.Add((breed.Child1, CalculateTotalDistance(breed.Child1)));
                    children.Add((breed.Child2, CalculateTotalDistance(breed.Child2)));
                }

                int mutationCount = 0;
                var childrenArray = children.ToArray();
                for (int i = 0; i < children.Count; i++)
                {
                    if (Random.NextDouble() < Settings.MutationChance){
                        MutateInversion(ref childrenArray[i].Tour);
                        childrenArray[i].Distance = CalculateTotalDistance(childrenArray[i].Tour);
                        mutationCount++;
                    }
                }
                Population.AddRange(childrenArray.ToList());

                Population.Sort((x, y) => x.Distance.CompareTo(y.Distance));

                if (Population[0].Distance < BestDistance){
                    double oldDist = BestDistance;
                    BestDistance = Population[0].Distance;
                    BestSolution = Population[0].Tour;
                    Console.WriteLine($"Found new best solution of distance {BestDistance} at gen {TotalGen}.");
                    ImprovementOccured = true;
                    ProgressData.Add((TotalGen, BestDistance - oldDist));
                }

                CurrentGen++;
                if (ImprovementOccured)
                    NoImprovementGens = 0;
                else
                    NoImprovementGens++;
            }
            LastGeneration = TotalGen;
            Console.WriteLine($"Evolution has stopped. Generations: {LastGeneration}, Best Distance: {BestDistance}");
            EndTime = DateTime.Now;
            TimeSpan ts = EndTime - StartTime;
            Console.WriteLine($"Total evolution time: {Math.Round(ts.TotalSeconds, 3)} seconds.");
            Console.WriteLine($"Average time per generation: {Math.Round(ts.TotalMilliseconds / LastGeneration, 5)} ms");

        }

        public (int[] Child1, int[] Child2) PMXCross(ref int[] Parent1, ref int[] Parent2)
        {
            int Cut1 = Random.Next(1, (int)Math.Floor(CityCount * 0.6666));
            int Cut2 = Random.Next(Cut1+1, CityCount);
            int[] Child1 = new int[CityCount];
            int[] Child2 = new int[CityCount];
            List<(int, int)> Pointers = new List<(int, int)>();
            // Switch the cut segment
            for (int i=Cut1; i < Cut2; i++)
            {
                Child2[i] = Parent1[i];
                Child1[i] = Parent2[i];
                Pointers.Add((Parent2[i], Parent1[i]));
            }
            // Fill the remaining positions with replacement or copy
            for (int i=0; i < CityCount; i++)
            {
                if (i >= Cut1 && i < Cut2)
                    continue;
                int parentElement = Parent1[i];
                while (true){
                    if (Pointers.Exists(x => x.Item1 == parentElement))
                        parentElement = Pointers.Find(x => x.Item1 == parentElement).Item2;
                    else
                    {
                        Child1[i] = parentElement;
                        break;
                    }
                }

                parentElement = Parent2[i];
                while (true){
                    if (Pointers.Exists(x => x.Item2 == parentElement))
                        parentElement = Pointers.Find(x => x.Item2 == parentElement).Item1;
                    else {
                        Child2[i] = parentElement;
                        break;
                    }
                }
            }
            return (Child1, Child2);
        }

        public (int[] Child1, int[] Child2) OXCross(ref int[] Parent1, ref int[] Parent2)
        {
            int Cut1 = Random.Next(1, (int)Math.Floor(CityCount * 0.6666));
            int Cut2 = Random.Next(Cut1 + 1, CityCount);
            int[] Child1 = new int[CityCount];
            int[] Child2 = new int[CityCount];
            for (int i = Cut1; i < Cut2; i++)
            {
                Child1[i] = Parent1[i];
                Child2[i] = Parent2[i];
            }
            // Create list starting at Cut2 for both parents
            List<int> Parent1List = new List<int>();
            List<int> Parent2List = new List<int>();
            for (int i = Cut2; i<CityCount; i++)
            {
                Parent1List.Add(Parent1[i]);
                Parent2List.Add(Parent2[i]);
                if (i == CityCount - 1)
                    i = -1;
                else if (i == Cut2 - 1)
                    break;
            }
            // Append non-duplicates to empty child fields starting at Cut2
            int k = -1;
            for (int i = Cut2; i < CityCount; i++)
            {
                k++;
                if (!Child2.Contains(Parent1List[k]))
                    Child2[i] = Parent1List[k];
                else
                    i--;
                if (k >= Parent1List.Count - 1)
                    break;
                if (i == CityCount - 1)
                    i = -1;
            }
            k = -1;
            for (int i = Cut2; i < CityCount; i++)
            {
                k++;
                if (!Child1.Contains(Parent2List[k]))
                    Child1[i] = Parent2List[k];
                else
                    i--;
                if (k >= Parent2List.Count - 1)
                    break;
                if (i == CityCount - 1)
                    i = -1;
            }
            return (Child1, Child2);
        }

        public (int[] Child1, int[] Child2) CXCross(ref int[] Parent1, ref int[] Parent2)
        {
            int[] Child1 = new int[CityCount];
            int[] Child2 = new int[CityCount];
            List<int> Cycle1 = new List<int>();
            int startIndex = Random.Next(0, CityCount);
            int next = Parent2[startIndex];
            while (true)
            {
                int nextIndex = Array.IndexOf(Parent1, next);
                Cycle1.Add(nextIndex);
                next = Parent2[nextIndex];
                
                if (next == Parent2[startIndex] || Cycle1.Count >= CityCount)
                    break;
            }
            foreach (int pos in Cycle1)
            {
                Child1[pos] = Parent1[pos];
            }
            for (int i=0; i < CityCount; i++)
            {
                if (!Cycle1.Contains(i))
                    Child1[i] = Parent2[i];
            }

            List<int> Cycle2 = new List<int>();
            next = Parent1[startIndex];
            while (true)
            {
                int nextIndex = Array.IndexOf(Parent2, next);
                Cycle2.Add(nextIndex);
                next = Parent1[nextIndex];

                if (next == Parent1[startIndex] || Cycle2.Count >= CityCount)
                    break;
            }
            foreach (int pos in Cycle2)
            {
                Child2[pos] = Parent2[pos];
            }
            for (int i = 0; i < CityCount; i++)
            {
                if (!Cycle2.Contains(i))
                    Child2[i] = Parent1[i];
            }
            return (Child1, Child2);
        }

        public void MutateInversion(ref int[] Child)
        {
            int Cut1 = Random.Next(1, (int)Math.Floor(CityCount * 0.6666));
            int Cut2 = Random.Next(Cut1 + 1, CityCount);
            int inversionEnd = Cut2;
            for (int i = Cut1; i < Cut2; i++)
            {
                inversionEnd = Cut2 - i + Cut1 - 1;
                if (i >= inversionEnd || Child[i] == Child[inversionEnd])
                    break;
                int _tmp = Child[i];
                Child[i] = Child[inversionEnd];
                Child[inversionEnd] = _tmp;
            }
        }

        public void MutateTransposition(ref int[] Child)
        {
            int rndIdx = Random.Next(0, CityCount);
            int rndIdx2 = Random.Next(0, CityCount);
            int breakPoint = 100;
            int i = 0;
            while (rndIdx2 == rndIdx)
            {
                i++;
                rndIdx2 = Random.Next(0, CityCount);
                if (i >= breakPoint)
                    return;
            }
            int _tmp = Child[rndIdx];
            Child[rndIdx] = Child[rndIdx2];
            Child[rndIdx2] = _tmp;
        }

        public void ShowSolution(int[] sol)
        {
            List<int> dup = new List<int>();
            foreach(var city in sol)
            {
                if (sol.Count(x => x == city) > 1 && !dup.Contains(city))
                    dup.Add(city);
                Console.Write(city + ", ");
            }
            Console.WriteLine();
        }
    }
}
