using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfMoviesProject.ViewModel
{
    public class MovieViewModel
    {
        public int Id { get; set; }
        public int KinopoiskId { get; set; }
        public string Title { get; set; }
        public string PosterUrl { get; set; }
        public string Genres { get; set; }
        public string Countries { get; set; }
        public int Year { get; set; }
        public double Rating { get; set; }
    }
}
