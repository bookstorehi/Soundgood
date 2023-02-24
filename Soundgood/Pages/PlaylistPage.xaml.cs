using Soundgood.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Playback;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using static Soundgood.Pages.LibraryPage;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=234238

namespace Soundgood.Pages
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class PlaylistPage : Page
    {
        Playlist playlist;
        public PlaylistPage()
        {
            this.InitializeComponent();
		}

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is RequestData)
            {
                var data = (RequestData)e.Parameter;
				playlist = GetPlaylist(data.Kind, data.Id, 25, 1);
                UpdateCompositions();

                LoadCompsPictures();

				base.OnNavigatedTo(e);
            }
            else
            {
                Debug.WriteLine("OnNavigatedTo: ID плейлиста не был передан");
            }
        }

        private Playlist GetPlaylist(string kind, int id, int maxResults, int page)
        {
            try
            {
                using (WebClient webClient = new WebClient())
                {
                    webClient.BaseAddress = StaticItems.EndPoint;

                    string url = "";
                    if (kind == "music#playlist")
                        url = $"playlist/open?&id={id}&maxResults={maxResults}&page={page}";
                    else if (kind == "music#set")
						url = $"sets/open?&id={id}&maxResults={maxResults}&page={page}";
                    else
                    {
						StaticItems.Alert("Ошибка", "Неизвестный тип запроса\nИзвеняемся за неудобства!");
						return null;
					}

					var json = webClient.DownloadString(url);

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    var response = JsonSerializer.Deserialize<Playlist>(json, options);

                    Debug.WriteLine("GetPlaylist: Ответ получен (" + id + ") ******** --- " + url + " - ******** - " + response.PlaylistData.Title);

                    if (response.Error != null)
                    {
                        response.Error.DisplayErrorDialog();
                        return null;
                    }

                    if (response.Kind == "music#playlist" || response.Kind == "music#set")
                        return response;
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

        private void UpdateCompositions()
        {
            object cvsCompositions = this.Resources["cvsCompositions"];
            (cvsCompositions as CollectionViewSource).Source = playlist.Compositions;
        }

		private async void LoadCompsPictures()
		{
            BitmapImage playlistPreview = null;

			foreach (Composition item in playlist.Compositions)
			{
                string content = item.Data.Album?.ImageSymbols;

				if (content != null)
				{
					Task<BitmapImage> task = StaticItems.Base64ToBitmap(content);
					await task;
					item.Picture = task.Result;

                    if (playlistPreview == null)
                    {
						playlistPreview = task.Result;
					}
				}
			}

			Debug.WriteLine("Картинки для треков загружены");

            if (playlistPreview != null && playlist.Kind == "music#playlist")
            {
				previewImg.Source = playlistPreview;
			}
            else if (playlist.Kind == "music#set")
            {
				Task<BitmapImage> task = StaticItems.Base64ToBitmap(playlist.PlaylistData.ImageSymbols);
				await task;
				previewImg.Source = task.Result;
            }

			UpdateCompositions();
		}

		private void ListView_ItemClick(object sender, ItemClickEventArgs e)
		{
            StaticItems._mediaPlaybackList = new MediaPlaybackList();

			Debug.WriteLine("1");
			Composition composition = e.ClickedItem as Composition;
            PlayComposition(composition);

		}

		public void PlayComposition(Composition composition)
		{
			int index = playlist.Compositions.IndexOf(composition);
			StaticItems.PlayComposition(playlist.Compositions, index);
		}
    }
}
