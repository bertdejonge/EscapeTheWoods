using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Drawing.Imaging;
using System.Drawing;
using System.Threading.Tasks;
using System.Drawing.Printing;
using EscapeFromTheWoods.Objects;
using System.Reflection;
using System.Collections.Concurrent;

namespace EscapeFromTheWoods
{
    public class Wood
    {
        private const int drawingFactor = 8;
        private string path;
        private DBwriter db;
        private Random r = new Random(1);
        public int woodID { get; set; }
        public List<Tree> trees { get; set; }
        public List<Monkey> monkeys { get; private set; }
        private Map map;
        public Wood(int woodID, List<Tree> trees, Map map, string path, DBwriter db)
        {
            this.woodID = woodID;
            this.trees = trees;
            this.monkeys = new List<Monkey>();
            this.map = map;
            this.path = path;
            this.db = db;
        }

        // No changes
        public void PlaceMonkey(string monkeyName, int monkeyID)
        {
            int treeNr;
            do
            {
                treeNr = r.Next(0, trees.Count - 1);
            }
            while (trees[treeNr].hasMonkey);
            Monkey m = new Monkey(monkeyID, monkeyName, trees[treeNr]);
            monkeys.Add(m);
            trees[treeNr].hasMonkey = true;
        }

        public async Task EscapeAsync()
        {
            List<List<Tree>> monkeyRoutes = new();

            // Parallel escaping monkeys 
            // We await until all monkeys escaped to write the result
            var tasks = monkeys.Select(async m =>
            {
                List<Tree> treeRoute = await EscapeMonkeyAsync(m, this.map);
                monkeyRoutes.Add(treeRoute);
            });
            await Task.WhenAll(tasks.ToArray());

            WriteEscaperoutesToBitmap(monkeyRoutes);
        }

        // NO CHANGES
        // Monkeys need to jump synchronously
        private async Task writeRouteToDB(Monkey monkey,List<Tree> trees)
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine($"{woodID}:write db routes {woodID},{monkey.name} start");
            
            DBMonkeyRecord record = new(monkey.monkeyID, monkey.name, trees);

            await db.WriteMonkeyRecord(record);

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine($"{woodID}:write db routes {woodID},{monkey.name} end");
        }       

        // NO CHANGES
        // Drawing might experience issues when running parallel
        public void WriteEscaperoutesToBitmap(List<List<Tree>> routes)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"{woodID}:write bitmap routes {woodID} start");
            Color[] cvalues = new Color[] { Color.Red, Color.Yellow, Color.Blue, Color.Cyan, Color.GreenYellow };
            Bitmap bm = new Bitmap((map.xmax - map.xmin) * drawingFactor, (map.ymax - map.ymin) * drawingFactor);
            Graphics g = Graphics.FromImage(bm);
            int delta = drawingFactor / 2;
            Pen p = new Pen(Color.Green, 1);

            foreach(Tree t in trees) { 
                g.DrawEllipse(p, t.x * drawingFactor, t.y * drawingFactor, drawingFactor, drawingFactor);
            }

            int colorN = 0;
            foreach (List<Tree> route in routes)
            {
                int p1x = route[0].x * drawingFactor + delta;
                int p1y = route[0].y * drawingFactor + delta;
                Color color = cvalues[colorN % cvalues.Length];
                Pen pen = new Pen(color, 1);
                g.DrawEllipse(pen, p1x - delta, p1y - delta, drawingFactor, drawingFactor);
                g.FillEllipse(new SolidBrush(color), p1x - delta, p1y - delta, drawingFactor, drawingFactor);
                for (int i = 1; i < route.Count; i++)
                {
                    g.DrawLine(pen, p1x, p1y, route[i].x * drawingFactor + delta, route[i].y * drawingFactor + delta);
                    p1x = route[i].x * drawingFactor + delta;
                    p1y = route[i].y * drawingFactor + delta;
                }
                colorN++;
            }
            bm.Save(Path.Combine(path, woodID.ToString() + "_escapeRoutes.jpg"), ImageFormat.Jpeg);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"{woodID}:write bitmap routes {woodID} end");
        }        

        // Write records parallel, as they are independent
        public async Task WriteWoodToDB()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"{woodID}:write db wood {woodID} start");

            DBWoodRecord record = new(woodID, trees);


            await db.WriteWoodRecordAsync(record);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"{woodID}:write db wood {woodID} end");
        }


        public async Task<List<Tree>> EscapeMonkeyAsync(Monkey monkey, Map map)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"{woodID}:start {woodID},{monkey.name}");

            // Use hashset (fastest lookup) for storage of visited strees
            HashSet<int> visitedTreesID = new();

            // Route of the monkey
            List<Tree> route = new List<Tree>() { monkey.tree }; 

            // Make a grid to optimize searching
            WoodGrid grid = new(10, map);

            // Add trees to grid in parallel
            Parallel.ForEach(trees, t => grid.AddTree(t));

            // Set ammount of nearest trees to find
            int ammNearest = 10;

            // Do while distance to the border > distance to tree 
            do
            {
                visitedTreesID.Add(monkey.tree.treeID);

                SortedList<double, List<Tree>> distanceToMonkey = new SortedList<double, List<Tree>>();               

                //zoek dichtste bomen die nog niet zijn bezocht            
                distanceToMonkey = FindNearestNeighbors(monkey, grid, distanceToMonkey, ammNearest, visitedTreesID);

                //distance to border            
                //noord oost zuid west
                double distanceToBorder = (new List<double>()
                { map.ymax - monkey.tree.y,
                  map.xmax - monkey.tree.x,
                  monkey.tree.y-map.ymin,
                  monkey.tree.x-map.xmin 
                }).Min();

                // Safety check
                if (distanceToMonkey.Count == 0)
                {
                    writeRouteToDB(monkey, route);
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine($"{woodID}:end {woodID},{monkey.name}");
                    return route;
                }

                if (distanceToBorder < distanceToMonkey.First().Key)
                {
                    writeRouteToDB(monkey, route);
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine($"{woodID}:end {woodID},{monkey.name}");
                    return route;
                }

                route.Add(distanceToMonkey.First().Value.First());
                route[route.Count - 1].hasMonkey = true;
                route[route.Count - 2].hasMonkey = false;
                
                monkey.tree = distanceToMonkey.First().Value.First();
            }
            while (true);
        }

        private SortedList<double, List<Tree>> FindNearestNeighbors(Monkey monkey, WoodGrid grid, SortedList<double, List<Tree>> distanceToMonkey, int ammNearest, HashSet<int> visitedTreesID)
        {
            // Find cell where tree is located
            (int i, int j) = FindCell(monkey.tree.x, monkey.tree.y, grid);
            ProcessCell(distanceToMonkey, grid, i, j, monkey, ammNearest, visitedTreesID);
            int ring = 0;

            while(distanceToMonkey.Count < ammNearest)
            {
                ring++;
                ProcessRing(i, j, ring, distanceToMonkey, monkey, visitedTreesID, grid, ammNearest);
            }

            // Calculate no. of correction rings
            int n_rings = (int)Math.Ceiling(Math.Sqrt(2) * (ring + 1)) - ring;
            for(int extraRings = 1; extraRings <= n_rings; extraRings++)
            {
                ProcessRing(i, j, ring, distanceToMonkey, monkey, visitedTreesID, grid, ammNearest);
            }
            return distanceToMonkey;
        }

        private (int i, int j) FindCell(int x, int y, WoodGrid grid)
        {
            // Out of bounds check
            if(x < grid.Map.xmin || x > grid.Map.xmax || y < grid.Map.ymin || y > grid.Map.ymax)
            {
                throw new ArgumentOutOfRangeException("out of bounds");
            }

            // Map position to grid indeces 
            int i = (int)((x - grid.Map.xmin) / grid.Delta);
            int j = (int)((y - grid.Map.ymin) / grid.Delta);

            // If ON bounds, adjust for zero-based indexing (index out of bounds)
            if (i == grid.NX) { i--; }
            if (j == grid.NY) { j--; }
            return (i, j);
        }

        private void ProcessRing(int i, int j, int ring, SortedList<double, List<Tree>> distanceToMonkey, Monkey monkey, HashSet<int> visitedTreesID, WoodGrid grid, int ammNearest)
        {
            // Iterate over all cells in the ring 
            // Call process cell for every valid cell option
            for(int gridX = i - ring; gridX <= i + ring; gridX++)
            {
                // PRocess the cell under the target cell
                int gridY = j - ring;
                if(IsValidCell(gridX, gridY, grid)) ProcessCell(distanceToMonkey, grid, gridX, gridY, monkey, ammNearest, visitedTreesID);

                // and the cell above the target
                gridY = j + ring;
                if (IsValidCell(gridX, gridY, grid)) ProcessCell(distanceToMonkey, grid, gridX, gridY, monkey, ammNearest, visitedTreesID);
            }

            for(int gridY = j - ring + 1; gridY <= j + ring - 1; gridY++)
            {
                // Left
                int gridX = i - ring;
                if (IsValidCell(gridX, gridY, grid)) ProcessCell(distanceToMonkey, grid, gridX, gridY, monkey, ammNearest, visitedTreesID);

                // Right
                gridX = i + ring;
                if (IsValidCell(gridX, gridY, grid)) ProcessCell(distanceToMonkey, grid, gridX, gridY, monkey, ammNearest, visitedTreesID);

            }
        }

        private void ProcessCell(SortedList<double, List<Tree>> distanceToMonkey, WoodGrid grid, int i, int j, Monkey monkey, int ammNearest, HashSet<int> visitedTreesID)
        {
            // Loop over every tree in the gridcell
            foreach(Tree t in grid.Trees[i][j])
            {
                // Skip tree if it already has a monkey in it, or has already been visited
                if (!visitedTreesID.Contains(t.treeID) && !t.hasMonkey) {

                    // Calculate distance between monkey and tree
                    double dsquare = Math.Sqrt(Math.Pow(t.x - monkey.tree.x, 2) + Math.Pow(t.y - monkey.tree.y,2));

                    // Add to the list of nearest if we still need trees, or if we found a closer tree
                    if(distanceToMonkey.Count < ammNearest || dsquare < distanceToMonkey.Keys[distanceToMonkey.Count - 1])
                    {

                        // Check if we already have a key for trees at this distance
                        // If not, than add the key and new list of trees to the dict
                        if (distanceToMonkey.ContainsKey(dsquare))
                        {
                            distanceToMonkey[dsquare].Add(t);
                        } else
                        {
                            distanceToMonkey.Add(dsquare, new List<Tree>() { t });
                        }
                    }
                }
            }
        }

        private bool IsValidCell(int gridX, int gridY, WoodGrid grid)
        {
            if ((gridX < 0) || (gridX >= grid.NX)) return false;
            if ((gridY < 0) || (gridY >= grid.NY)) return false;
            return true;
        }


    }
}
