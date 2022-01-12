using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows.Forms;
using AdonisUI.Controls;
using Anidow.Database;
using Anidow.Database.Models;
using Anidow.Events;
using Anidow.Extensions;
using Anidow.GraphQL;
using Anidow.Pages.Components.AnimeInfo;
using Anidow.Services;
using Anidow.Utils;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.EntityFrameworkCore;
using Notifications.Wpf.Core;
using Serilog;
using Stylet;
using Application = System.Windows.Application;
using MessageBox = AdonisUI.Controls.MessageBox;
using Screen = Stylet.Screen;

namespace Anidow.Pages.Components.Tracked;

public class TrackedAnimeEditContentViewModel : Screen
{
    private readonly IEventAggregator _eventAggregator;

    private readonly GraphQLHttpClient _graphQlClient =
        new("https://graphql.anilist.co", new SystemTextJsonSerializer());

    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly SettingsService _settingsService;
    private readonly TaskbarIcon _taskbarIcon;
    private readonly IWindowManager _windowManager;

    public TrackedAnimeEditContentViewModel(SettingsService settingsService, TaskbarIcon taskbarIcon,
        HttpClient httpClient, IWindowManager windowManager, IEventAggregator eventAggregator, ILogger logger)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _taskbarIcon = taskbarIcon ?? throw new ArgumentNullException(nameof(taskbarIcon));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Anime Anime { get; private set; }
    public string Title => $"Edit - {Anime?.Name ?? "None"}";

    public bool CanSaveAnime { get; set; } = true;

    public BindableCollection<AniListAnime> SearchResults { get; set; }
    public bool SearchAnimeLoading { get; set; }

    public bool CanSearchAnime => !string.IsNullOrWhiteSpace(_settingsService.Settings.AniListJwt)
                                  && !SearchAnimeLoading
                                  && Anime.AniListAnime is null;

    public void SetAnime(Anime anime)
    {
        Anime = anime;
    }

    public async Task Delete()
    {
        if (await Anime.DeleteInDatabase())
        {
            Close();
            _eventAggregator.PublishOnUIThread(new TrackedDeleteAnimeEvent
            {
                Anime = Anime,
            });
            await NotificationUtil.ShowUndoAsync(Anime.Name, "Deleted", async () =>
            {
                await Anime.AddToDatabase(Anime.CoverData);
                _eventAggregator.PublishOnUIThread(new TrackedRefreshEvent());
            }, null, TimeSpan.FromSeconds(10));
        }
    }

    public async Task SaveAnime()
    {
        if (string.IsNullOrWhiteSpace(Anime.Group))
        {
            await NotificationUtil.ShowAsync(
                "Warning",
                "Group can not be empty!",
                NotificationType.Warning,
                area: NotificationUtil.TrackedEditArea);
            return;
        }

        await Anime.UpdateInDatabase();
        if (_settingsService.Settings.Notifications)
        {
            _taskbarIcon.ShowBalloonTip("Saved", Anime.Name, BalloonIcon.Info);
        }
        else
        {
            await NotificationUtil.ShowAsync(Anime.Name,
                "Saved!",
                NotificationType.Success,
                area: NotificationUtil.TrackedEditArea);
        }
    }


    public void OpenFolderBrowserDialog()
    {
        using var dialog = new FolderBrowserDialog
        {
            SelectedPath = Anime.Folder,
        };
        var result = dialog.ShowDialog();
        if (result != DialogResult.OK)
        {
            return;
        }

        Anime.Folder = dialog.SelectedPath;
    }

    public void OpenFolderFilesWindow()
    {
        _windowManager.ShowWindow(new FolderFilesViewModel(Anime, _logger));
    }


    public async Task DownloadCover((object url, object anime) data)
    {
        try
        {
            var anime = (Anime)data.anime;
            var url = (string)data.url;
            Uri.TryCreate(url, UriKind.Absolute, out var uri);
            if (uri == null)
            {
                return;
            }

            anime.Cover = url;
            anime.CoverData = await url.GetCoverData(anime, _httpClient, _logger);
            await SaveAnime();
        }
        catch (Exception e)
        {
            _logger.Error(e, "failed downloading Cover");
        }
    }

    public void Close()
    {
        RequestClose();
    }


    public async Task Watch(Episode episode)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(episode.File))
            {
                await OpenFolder(episode);
                return;
            }

            await ProcessUtil.OpenFile(episode);

            episode.Watched = true;
            episode.WatchedDate = DateTime.UtcNow;
            await episode.UpdateInDatabase();
        }
        catch (Exception e)
        {
            _logger.Error(e, "failed opening file to watch");
            MessageBox.Show($"Failed opening file\nerror: {e.Message}",
                icon: MessageBoxImage.Error);
            await OpenFolder(episode);
        }
    }

    public async Task OpenFolder(Episode episode)
    {
        episode.CanOpen = false;
        _windowManager.ShowWindow(new FolderFilesViewModel(ref episode, _logger));
        await Task.Delay(100);
        episode.CanOpen = true;
    }

    public void ShowImage()
    {
        ImageUtil.ShowImage(Anime.CoverData.FilePath);
    }

    protected override void OnActivate()
    {
        if (Anime is null)
        {
            return;
        }

        _ = SearchAnime();
    }

    private async Task SearchAnime()
    {
        SearchAnimeLoading = true;
        _graphQlClient.HttpClient.DefaultRequestHeaders.Authorization =
            AuthenticationHeaderValue.Parse($"Bearer {_settingsService.Settings.AniListJwt}");
        var name = Anime.Name;

        if (name.Contains(" - TV Series ["))
        {
            name = name[..name.IndexOf(" - TV Series [", StringComparison.InvariantCulture)];
        }
        else if (name.Contains(" - ONA ["))
        {
            name = name[..name.IndexOf(" - ONA [", StringComparison.InvariantCulture)];
        }

        try
        {
            var query = GraphQLQueries.SearchQuery(name);
            var response = await _graphQlClient.SendQueryAsync<AnimeSearchResult>(query);

            SearchResults = new BindableCollection<AniListAnime>(response.Data.Page.Media);
        }
        catch (Exception e)
        {
            _logger.Error(e, "failed searching anime on Anidow\nyour token might be expired");
        }
        SearchAnimeLoading = false;
    }

    public void ShowAniListAnime(AniListAnime anime)
    {
        var window = new AnimeInfoPanelWindow
        {
            DataContext = anime,
            Owner = Application.Current.MainWindow,
        };
        window.ShowDialog();
    }

    public async Task SetAniListAnime(AniListAnime anime)
    {
        try
        {
            await using (var db = new TrackContext())
            {
                var a = await db.AniListAnime.FirstOrDefaultAsync(a => a.Id == anime.Id);
                if (a is null)
                {
                    await db.AniListAnime.AddAsync(anime);
                    await db.SaveChangesAsync();
                }
            }

            Anime.AniListAnime = anime;

            await Anime.UpdateInDatabase();
            _ = NotificationUtil.ShowAsync(
                "Linking", 
                "Successful", 
                NotificationType.Success,
                area: NotificationUtil.TrackedEditArea);

            NotifyOfPropertyChange(nameof(CanSearchAnime));
        }
        catch (Exception e)
        {
            _logger.Error(e, "failed linking AniList Anime");
        }
    }
}