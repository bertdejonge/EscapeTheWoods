using System;
using System.Collections.Generic;
using System.Text;

namespace EscapeFromTheWoods
{
    public class DBWoodRecord
    {
        public DBWoodRecord(int woodID, List<Tree> trees)
        {
            this.woodID = woodID;
            this.trees = trees;
        }

        public int woodID { get; set; }
        public int treeID { get; set; }
        public List<Tree> trees { get; set; }
    }
}
