using LibraryOfMoviesProject.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryOfMoviesProject.Data
{
    public class ApplicationDataContext : DbContext
    {
        public DbSet<Favourite> Favorites { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=favourite.db");
        }
    }
}
