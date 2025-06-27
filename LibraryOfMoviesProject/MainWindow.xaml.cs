using LibraryOfMoviesProject.Data;
using LibraryOfMoviesProject.Models;
using LibraryOfMoviesProject.Services;
using LibraryOfMoviesProject.ViewModel;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LibraryOfMoviesProject
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private KinopoiskService _kinopoiskService;
        private int _currentPage = 1;
        private bool _isNavCollapsed = false;
        private bool _isLoading = false;
        private bool _isEndOfResults = false;
        private string _currentKeyword = "";
        private string _currentOrder = "RATING";
        private int? _currentCountryId = null;
        private int? _currentYear = null;
        private string _currentContentType = "TOP_POPULAR";

        private int? _currentlyViewiedFilmId = null;
        private JObject _currentFilmDetails = null;

        private readonly ObservableCollection<MovieViewModel> _movieCollection = new ObservableCollection<MovieViewModel>();

        public MainWindow()
        {
            InitializeComponent();

            string apiKey = App.Configuration["KinopoiskUnofficial:ApiKey"];

            if (string.IsNullOrEmpty(apiKey))
            {
                MessageBox.Show("API ключ не найден!");
                Application.Current.Shutdown();
                return;
            }

            _kinopoiskService = new KinopoiskService(apiKey);

            using(var context = new ApplicationDataContext())
            {
                context.Database.EnsureCreatedAsync();
            }

            MoviesGrid.ItemsSource = _movieCollection;

            LoadCountryFilterAsync();
            //LoadMoviesAsync(clearList: true);
        }

        private void ToggleNavButton_Click(object sender, RoutedEventArgs e)
        {
            _isNavCollapsed = !_isNavCollapsed;

            if (_isNavCollapsed)
            {
                NavColumn.Width = new GridLength(80);

                NameText.Visibility = Visibility.Collapsed;
                HomeText.Visibility = Visibility.Collapsed;
                MovieText.Visibility = Visibility.Collapsed;
                SeriesText.Visibility = Visibility.Collapsed;
                GenreText.Visibility = Visibility.Collapsed;
                FavouriteText.Visibility = Visibility.Collapsed;
            }
            else
            {
                NavColumn.Width = new GridLength(180);

                NameText.Visibility = Visibility.Visible;
                HomeText.Visibility = Visibility.Visible;
                MovieText.Visibility = Visibility.Visible;
                SeriesText.Visibility = Visibility.Visible;
                GenreText.Visibility = Visibility.Visible;
                FavouriteText.Visibility = Visibility.Visible;
            }
        }

        /*private void LoadMoreButton_Click(object sender, RoutedEventArgs e)
        {
            LoadMoviesAsync();
        }*/

        private async Task LoadMoviesAsync(bool clearList = false)
        {
            if (clearList)
            {
                _currentPage = 1;
                _movieCollection.Clear();
                _isEndOfResults = false;
            }

            if (_isEndOfResults)
                return;

            try
            {
                JObject movieResponse;
                JArray moviesJson;

                if (_currentContentType == "TOP_POPULAR")
                {
                    movieResponse = await _kinopoiskService.GetPopularFilmsAsync(_currentPage);

                    moviesJson = (JArray)movieResponse["films"];
                }
                else
                {
                    movieResponse = await _kinopoiskService.GetFilmsAsync(_currentContentType, _currentPage, _currentKeyword, _currentOrder, _currentCountryId, _currentYear);

                    moviesJson = (JArray)movieResponse["items"];
                }
                if (!moviesJson.Any())
                {
                    _isEndOfResults = true;
                    return;
                }

                foreach (var movieData in moviesJson)
                {
                    int id = (int)(movieData["filmId"] ?? movieData["kinopoiskId"]);
                    var genres = movieData["genres"].Select(g => g["genre"].ToString());

                    var countries = movieData["countries"].Select(c => c["country"]).ToString();

                    double.TryParse((movieData["rating"] ?? movieData["ratingKinopoisk"])?.ToString(), out double rating);

                    if (_currentContentType != "TOP_POPULAR" && rating == 0.0)
                        continue;

                    var viewModel = new MovieViewModel
                    {
                        Id = id,
                        Title = movieData["nameRu"]?.ToString() ?? "Без названия",
                        PosterUrl = movieData["posterUrlPreview"]?.ToString(),
                        Countries = countries,
                        Year = (int)movieData["year"],
                        Genres = string.Join(",", genres),
                        Rating = rating
                    };

                    _movieCollection.Add(viewModel);
                }

                _currentPage++;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private async Task LoadCountryFilterAsync()
        {
            try
            {
                var filters = await _kinopoiskService.GetFiltersAsync();

                var countries = filters["countries"]
                    .Select(c => new { Id = (int)c["id"], Name = c["country"].ToString() })
                    .OrderBy(c => c.Name).ToList();

                CountryComboBox.Items.Clear();
                
                CountryComboBox.Items.Add(new ComboBoxItem { Content = "Все страны", Tag = (int?)null });

                
                foreach (var country in countries)
                {
                    CountryComboBox.Items.Add(new ComboBoxItem { Content = country.Name, Tag = country.Id });
                }
                
                CountryComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось загрузить список стран: {ex.Message}");
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            _currentContentType = "ALL";
            _currentKeyword = SearchTextBox.Text;
            LoadMoviesAsync(clearList: true);
        }

        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(SortComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
            {
                _currentOrder = selectedItem.Tag.ToString();

                if (_kinopoiskService != null)
                {
                    LoadMoviesAsync(clearList: true); 
                }
            }
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            _currentContentType = "TOP_POPULAR";
            _currentKeyword = "";
            SearchTextBox.Text = "";
            LoadMoviesAsync(clearList: true);
        }

        private void MovieButton_Click(object sender, RoutedEventArgs e)
        {
            _currentContentType = "FILM";
            LoadMoviesAsync(clearList: true);
        }

        private void SeriesButton_Click(object sender, RoutedEventArgs e)
        {
            _currentContentType = "TV_SERIES";
            LoadMoviesAsync(clearList: true);
        }

        private void GenreButton_Click(object sender, RoutedEventArgs e)
        {
            LoadGenresAsync();
        }

        private async void FavouriteButton_Click(object sender, RoutedEventArgs e)
        {
            MovieDetailsView.Visibility = Visibility.Collapsed;
            MovieListView.Visibility = Visibility.Visible;

            _movieCollection.Clear();

            using(var context = new ApplicationDataContext())
            {
                var favouriteMovies = await context.Favorites.ToListAsync();

                if (!favouriteMovies.Any())
                {
                    MessageBox.Show("Ваш список избранных фильмов пуст. Выберите фильм из предложенных и добавльте в избранное!");
                    return;
                }

                foreach(var fav in favouriteMovies)
                {
                    _movieCollection.Add(new MovieViewModel
                    {
                        Id = fav.KinopoiskId,
                        PosterUrl = fav.PosterUrl,
                        Title = fav.Title,
                        Genres = fav.Genres,
                        Rating = fav.Rating
                    });
                }
            }
        }

        private async Task LoadGenresAsync()
        {
            try
            {
                var filters = await _kinopoiskService.GetFiltersAsync();
                var genres = filters["genres"].Select(g => g["genre"].ToString());

                MessageBox.Show("Доступные жанры:\n\n" + string.Join("\n", genres), "Жанры");

                _movieCollection.Clear();
            }
            catch (Exception ex)
            {
                _movieCollection.Clear();
                MessageBox.Show($"Произошла ошибка: {ex.Message}");
            }
        }

        private async void MovieItem_Click(object sender, RoutedEventArgs e)
        {
            if(sender is Button clickedButton && clickedButton.DataContext is MovieViewModel selectedMovie)
            {
                await ShowMovieDetailsAsync(selectedMovie.Id);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            MovieDetailsView.Visibility = Visibility.Collapsed;
            MovieListView.Visibility = Visibility.Visible;
            _currentlyViewiedFilmId = null;
            _currentFilmDetails = null;
        }

        private async Task ShowMovieDetailsAsync(int id)
        {
            try
            {
                var details = await _kinopoiskService.GetFilmByIdAsync(id);

                _currentlyViewiedFilmId = id;
                _currentFilmDetails = details;

                DetailPosterImage.Source = new BitmapImage(new Uri(details["posterUrl"].ToString()));
                DetailTitleTextBlock.Text = details["nameRu"]?.ToString() ?? details["nameOriginal"].ToString();
                DetailSloganTextBlock.Text = details["slogan"]?.ToString() ?? "";

                var year = details["year"];
                var countries = string.Join(", ", details["countries"].Select(c => c["country"]));
                var genres = string.Join(", ", details["genres"].Select(g => g["genre"]));

                DetailInfoTextBlock.Text = $"{year} / {countries} / {genres}";

                DetailDescriptionTextBlock.Text = details["description"]?.ToString() ?? "Описание фильма";
                
                using(var context=  new ApplicationDataContext())
                {
                    bool isFavourite = await context.Favorites.AnyAsync(f => f.KinopoiskId == id);

                    AddToFavoritesButton.Visibility = isFavourite ? Visibility.Collapsed : Visibility.Visible;
                    DeleteFavouritesButton.Visibility = isFavourite? Visibility.Visible : Visibility.Collapsed;
                }

                MovieListView.Visibility = Visibility.Collapsed;
                MovieDetailsView.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось загрузить информацию по фильму. Ошибка {ex.Message}");
            }
        }

        private async void MovieListView_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var scrollViewer = sender as ScrollViewer;
            if (scrollViewer == null)
                return;

            if(scrollViewer.ScrollableHeight > 0 && e.VerticalOffset >= scrollViewer.ScrollableHeight - 100 && !_isLoading && !_isEndOfResults)
            {
                _isLoading = true;
                await LoadMoviesAsync();
                _isLoading = false;
            }
        }

        private async void AddToFavoritesButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentlyViewiedFilmId == null || _currentFilmDetails == null)
            {
                MessageBox.Show("Информация о фильме не загружена!");
                return;
            }

            using (var context = new ApplicationDataContext())
            {
                bool dataExists = await context.Favorites.AnyAsync(f => f.KinopoiskId == _currentlyViewiedFilmId);

                if (dataExists)
                {
                    MessageBox.Show("Фильм уже добавлен в избранное");
                    return;
                }

                double.TryParse(_currentFilmDetails["ratingKinopoisk"]?.ToString(), out double rating);

                var favouriteModel = new Favourite
                {
                    KinopoiskId = _currentlyViewiedFilmId.Value,
                    Title = _currentFilmDetails["nameRu"].ToString() ?? _currentFilmDetails["nameOriginal"].ToString(),
                    PosterUrl = _currentFilmDetails["posterUrlPreview"].ToString(),
                    Year = _currentFilmDetails["year"].ToObject<int>(),
                    Rating = rating,
                    Genres = string.Join(", ", _currentFilmDetails["genres"].Select(g => g["genre"]))
                };

                context.Favorites.Add(favouriteModel);
                await context.SaveChangesAsync();

                MessageBox.Show($"Фильм {favouriteModel.Title} добавлен в избранное!");
            }
        }

        private void CountryCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CountryComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                // Получаем ID страны из Tag. Tag может быть null, поэтому используем 'as int?'
                _currentCountryId = selectedItem.Tag as int?;

                // Перезагружаем список с новым фильтром, если сервис уже готов
                if (_kinopoiskService != null)
                {
                    LoadMoviesAsync(clearList: true);
                }
            }
        }

        private void YearTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (int.TryParse(YearTextBox.Text, out int year))
                {
                    _currentYear = year;
                    YearTextBox.Text = "";
                }
                else 
                { 
                    _currentYear = null;
                }

                LoadMoviesAsync(clearList: true);
            }
        }

        private async void DeleteFavouritesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using(var context = new ApplicationDataContext())
                {
                    var favourite = await context.Favorites.FirstOrDefaultAsync(f => f.KinopoiskId == _currentlyViewiedFilmId);

                    if(favourite != null)
                    {
                        context.Favorites.Remove(favourite);
                        await context.SaveChangesAsync();

                        MessageBox.Show("Фильм был удален из избранного!");

                        MovieDetailsView.Visibility = Visibility.Collapsed;
                        MovieListView.Visibility = Visibility.Visible;
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show($"Ошибка удаления: {ex.Message}");
            }
        }
    }
}