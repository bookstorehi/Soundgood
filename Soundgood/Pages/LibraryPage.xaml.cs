using Soundgood.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using static System.Net.Mime.MediaTypeNames;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Soundgood.Pages
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class LibraryPage : Page
    {
        List<Playlist> playlists;
		public LibraryPage()
        {
            this.InitializeComponent();

			playlists = GetUserPlaylists(20, 1);
            UpdatePlaylists();

            LoadPlaylistPicture();
            Debug.WriteLine("------1--------");

			this.NavigationCacheMode = NavigationCacheMode.Enabled;
		}

		private List<Playlist> GetUserPlaylists(int maxResults, int page)
        {
            try
            {
                using (WebClient webClient = new WebClient())
                {
                    webClient.BaseAddress = StaticItems.EndPoint;
                    var json = webClient.DownloadString($"user/playlists?&maxResults={maxResults}&page={page}");

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    var response = JsonSerializer.Deserialize<PlaylistList>(json, options);

                    Debug.WriteLine("GetUserPlaylists: Ответ получен");

                    if (response.Error != null)
                    {
                        response.Error.DisplayErrorDialog();
                        return null;
                    }

                    if (response.Kind == "music#playlistListResponse")
                        return response.Playlists;
                    else
                    {
                        StaticItems.Alert("Ошибка", "Данные получены не верно\nИзвеняемся за неудобства!");
                        return null;
                    }
                }
            }
            catch (WebException e)
            {
                StaticItems.Alert("Ошибка", e.Message);
                return null;
            }
        }

        private void UpdatePlaylists()
        {
            object cvsPlaylists = this.Resources["cvsPlaylists"];
            (cvsPlaylists as CollectionViewSource).Source = playlists;
        }

        private async void LoadPlaylistPicture()
        {
            foreach (Playlist item in playlists)
            {
                string content = item.PlaylistData.ImageSymbols;

                if (content != null)
                {
                    Task<BitmapImage> task = StaticItems.Base64ToBitmap(content);
                    await task;
                    item.Picture = task.Result;
                }
            }

            Debug.WriteLine("Картинки плейлистов загружены");
            UpdatePlaylists();
        }

        public class PlaylistList : IResponse
        {
            [JsonPropertyName("pageInfo")]
            public ListInfo PlaylistListInfo { get; set; }

            [JsonPropertyName("items")]
            public List<Playlist> Playlists { get; set; }

            public struct ListInfo
            {
                public int ResultsPerPage { get; set; }
            }
        }

		private void GridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            Playlist playlist = e.ClickedItem as Playlist;
			RequestData data = new RequestData(playlist.Kind, playlist.Id);
			StaticItems.NavView_Navigate("playlist", new Windows.UI.Xaml.Media.Animation.EntranceNavigationTransitionInfo(), data);
		}
	}
}
