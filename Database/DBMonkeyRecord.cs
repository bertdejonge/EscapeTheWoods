using System;
using System.Collections.Generic;
using System.Text;

namespace EscapeFromTheWoods
{
    public class DBMonkeyRecord
    {
        public DBMonkeyRecord(int monkeyID, string monkeyName, List<Tree> route)
        {
            this.monkeyID = monkeyID;
            this.monkeyName = monkeyName;
            this.route = route;
        }

        public int monkeyID { get; set; }
        public string monkeyName { get; set; }
        public List<Tree> route { get; set; }
    }
}
