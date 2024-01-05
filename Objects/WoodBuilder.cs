using EscapeFromTheWoods.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EscapeFromTheWoods
{
    public static class WoodBuilder
    {
        public static Wood GetWood(int size, Map map, string path, DBwriter db)
        {
            // Changed tree checking by List.contains to dictionary 
            Random r = new Random(100);
            Dictionary<int, Tree> trees = new Dictionary<int, Tree>();
            int n = 0;
            int index;
            while (n < size)
            {
                index = r.Next(size);
                if (!trees.ContainsKey(index))
                {
                    n++;
                    trees.Add(index, new Tree(IDgenerator.GetTreeID(), r.Next(map.xmin, map.xmax), r.Next(map.ymin, map.ymax)));
                }
            }

            Wood w = new Wood(IDgenerator.GetWoodID(), trees.Values.ToList(), map, path, db);
            return w;
        }
    }
}
