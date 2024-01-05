using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace EscapeFromTheWoods.Objects
{
    public class WoodGrid
    {
        public int Delta { get; set; }
        public int NX { get; set; }
        public int NY { get; set; }
        public List<Tree>[][] Trees { get; set; }
        public Map Map { get; set; }

        public WoodGrid(int delta, Map map) {
            Delta = delta;                                          // size of square
            Map = map;                                              // Bounds of wood
            NX = (int)((map.xmax - map.xmin) / Delta) + 1;
            NY = (int)((map.ymax - map.ymin) / Delta) + 1;

            // Make a 2D array to represent the grid.
            // Every cell (variation of [i][j] gets a list of trees
            Trees = new List<Tree>[NX][];
            for(int i = 0; i < NX; i++)
            {
                Trees[i] = new List<Tree>[NY];
                for(int j = 0; j < NY; j++) { 
                    Trees[i][j] = new List<Tree>();
                }
            }
        }

        public async Task AddTree(Tree tree)
        {
            // Out of bounds check
            if((tree.x < Map.xmin) || (tree.x > Map.xmax) || (tree.y < Map.ymin) || (tree.y > Map.ymax))
            {
                throw new Exception("WoodGrid: out of bounds");
            }

            // If ON bounds, adjust 
            int i = (int)((tree.x - Map.xmin) / Delta);
            int j = (int)((tree.y - Map.ymin) / Delta);

            if(i == NX) { i--; }

            if(j == NY) { j--; }

            // Add tree to correct cell
            Trees[i][j].Add(tree);
        }
    }
}
