using SQLite;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ITS440_Final_ProjectV2.Models
{


    public class Game
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Title { get; set; }
        public string Source { get; set; }
        public bool IsCompleted { get; set; }
        public string Notes { get; set; }
        public int Priority { get; set; }

        public int Rating { get; set; }
        public DateTime DateAdded { get; set; } = DateTime.Now;
        public string ImagePath { get; set; } // path to the image file

    }

}




