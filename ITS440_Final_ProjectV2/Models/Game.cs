using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITS440_Final_ProjectV2.Models
{
    public class Game
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Title { get; set; }
        public string Source { get; set; } // "Steam" or "Custom"
        public bool IsCompleted { get; set; }
        public DateTime DateAdded { get; set; }
        public int Priority { get; set; } = 0;
        public string Notes { get; set; } = "";

    }
}
