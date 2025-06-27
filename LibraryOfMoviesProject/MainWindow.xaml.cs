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
using System.Windows.Threading;

namespace LibraryOfMoviesProject
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly HashSet<int> _allowedGenreIds = new HashSet<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 };

        private DispatcherTimer _notificationTimer;

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
        private const string _favoritesContentType = "FAVORITES";

        private int? _currentlyViewiedFilmId = null;
        private JObject _currentFilmDetails = null;
        private HashSet<int> _favoriteIds = new HashSet<int>();

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

            _notificationTimer = new DispatcherTimer();
            _notificationTimer.Interval = TimeSpan.FromSeconds(5);
            _notificationTimer.Tick += NotificationTimer_Tick;
        }

        private void ToggleNavButton_Click(object sender, RoutedEventArgs e)
        {
            _isNavCollapsed = !_isNavCollapsed;

            if (_isNavCollapsed)
            {
                NavColumn.Width = new GridLength(70);

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
                using(var context = new ApplicationDataContext())
                {
                    _favoriteIds = (await context.Favorites.Select(f => f.KinopoiskId).ToListAsync()).ToHashSet();
                }

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
                ShowNotification($"Ошибка: {ex.Message}");
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
                ShowNotification($"Не удалось загрузить список стран: {ex.Message}");
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
            MovieListView.Visibility = Visibility.Collapsed;
            MovieDetailsView.Visibility = Visibility.Collapsed;
            GenreView.Visibility = Visibility.Visible;

            if (GenreItemsControl.ItemsSource == null)
            {
                LoadGenresAsync();
            }
        }

        private async void FavouriteButton_Click(object sender, RoutedEventArgs e)
        {
            _currentContentType = _favoritesContentType;

            MovieDetailsView.Visibility = Visibility.Collapsed;
            MovieListView.Visibility = Visibility.Visible;

            _movieCollection.Clear();

            using(var context = new ApplicationDataContext())
            {
                var favouriteMovies = await context.Favorites.ToListAsync();

                if (!favouriteMovies.Any())
                {
                    if (e != null && e.OriginalSource != null)
                    {
                        ShowNotification("Ваш список избранного пуст.");
                    }
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

                var genresList = filters["genres"]
                    .Where(g => _allowedGenreIds.Contains((int)g["id"]))
                    .Select(g => {
                        var genreName = g["genre"].ToString();

                        return new GenreViewModel
                        {
                            Id = (int)g["id"],
                            Name = genreName,
                            ImagePath = $"/Pictures/Genres/{genreName.ToLower()}.jpg"
                        };
                    })
                    .OrderBy(g => g.Name)
                    .ToList();

                GenreItemsControl.ItemsSource = genresList;
            }
            catch (Exception ex)
            {
                ShowNotification($"Не удалось загрузить список жанров: {ex.Message}");
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
            if (_currentContentType == _favoritesContentType)
                FavouriteButton_Click(this, new RoutedEventArgs());

            
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
                ShowNotification($"Не удалось загрузить информацию по фильму. Ошибка {ex.Message}");
            }
        }

        private async void MovieListView_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (_currentContentType == _favoritesContentType)
                return;

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
                ShowNotification("Информация о фильме не загружена!");
                return;
            }

            using (var context = new ApplicationDataContext())
            {
                bool dataExists = await context.Favorites.AnyAsync(f => f.KinopoiskId == _currentlyViewiedFilmId);

                if (dataExists)
                {
                    ShowNotification("Фильм уже добавлен в избранное");
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

                ShowNotification($"Фильм {favouriteModel.Title} добавлен в избранное!");
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
                if (_currentlyViewiedFilmId == null) return;

                using (var context = new ApplicationDataContext())
                {
                    var favoriteToRemove = await context.Favorites.FirstOrDefaultAsync(f => f.KinopoiskId == _currentlyViewiedFilmId.Value);

                    if (favoriteToRemove != null)
                    {
                        context.Favorites.Remove(favoriteToRemove);
                        await context.SaveChangesAsync();
                        ShowNotification("Фильм удален из избранного.");

                        AddToFavoritesButton.Visibility = Visibility.Visible;
                        DeleteFavouritesButton.Visibility = Visibility.Collapsed;
                    }
                }
            }
            catch(Exception ex)
            {
                ShowNotification($"Ошибка удаления: {ex.Message}");
            }
        }

        private async void CardFavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            if(sender is Button button && button.DataContext is MovieViewModel movie)
            {
                using(var context = new ApplicationDataContext())
                {
                    var favoriteDb = await context.Favorites.FirstOrDefaultAsync(f => f.KinopoiskId == movie.Id);

                    if (favoriteDb != null)
                    {
                        context.Favorites.Remove(favoriteDb);
                        await context.SaveChangesAsync();

                        button.Content = "☆";
                        button.Foreground = Brushes.White;
                        _favoriteIds.Remove(movie.Id);
                        ShowNotification("Фильм удален из избранного!");
                    }
                    else
                    {
                        var details = await _kinopoiskService.GetFilmByIdAsync(movie.Id);
                        var favorite = new Favourite
                        {
                            KinopoiskId = movie.Id,
                            Title = details["nameRu"]?.ToString() ?? details["nameOriginal"]?.ToString(),
                            PosterUrl = details["posterUrlPreview"].ToString(),
                            Rating = double.TryParse(details["ratingKinopoisk"]?.ToString(), out var r) ? r : 0.0,
                            Year = details["year"].ToObject<int>(),
                            Genres = string.Join(", ", details["genres"].Select(g => g["genre"]))
                        };
                        context.Favorites.Add(favorite);
                        await context.SaveChangesAsync();
                        
                        button.Content = "★";
                        button.Foreground = Brushes.Gold;
                        _favoriteIds.Add(favorite.KinopoiskId);
                        ShowNotification("Фильм добавлен в избранное!");
                    }
                }
            }
        }

        private void CardFavoriteButton_Loaded(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (sender is Button button && button.DataContext is MovieViewModel movie)
            {
                if (_favoriteIds.Contains(movie.Id))
                {
                    button.Content = "★";
                    button.Foreground = Brushes.Gold;
                }
                else
                {
                    button.Content = "☆";
                    button.Foreground = Brushes.White;
                }
            }
        }

        private void ShowNotification(string message)
        {
            NotificationText.Text = message;
            NotificationBorder.Visibility = Visibility.Visible;

            
            _notificationTimer.Stop();
            _notificationTimer.Start();
        }

        // Этот метод сработает, когда таймер закончится
        private void NotificationTimer_Tick(object sender, EventArgs e)
        {
            _notificationTimer.Stop();
            NotificationBorder.Visibility = Visibility.Collapsed;
        }

        private void CloseNotification_Click(object sender, RoutedEventArgs e)
        {
            _notificationTimer.Stop();
            NotificationBorder.Visibility = Visibility.Collapsed;
        }
    }
}