using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfMoviesProject.Models
{
    public class Favourite
    {
        public int Id { get; set; }
        public int KinopoiskId { get; set; }
        public string Title { get; set; }
        public string PosterUrl { get; set; }
        public int Year { get; set; }
        public double Rating { get; set; }
        public string Genres { get; set; }
    }
}
