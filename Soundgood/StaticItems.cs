using Soundgood.Pages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage.Pickers;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Foundation.Collections;
using static System.Net.Mime.MediaTypeNames;
using System.IO.Pipes;
using static System.Net.WebRequestMethods;
using Soundgood.Model;
using Windows.UI.Xaml.Shapes;

namespace Soundgood
{
    internal static class StaticItems
    {
		public static UserGetPrivilages user;

		public static string EndPoint = "http://localhost/soundgood/api/";

		public static readonly List<(string Tag, Type Page)> _pages = new List<(string Tag, Type Page)>
		{
			("home", typeof(HomePage)),
			("library", typeof(LibraryPage)),
		};

        public static NavigationView navigation;
        public static Frame navigationFrame;

		/// <summary>
		/// Производит переход между страницами
		/// </summary>
		/// <param name="navItemTag">Тег элемента панели навигации, соответствующий элементу коллекции _pages</param>
		public static void NavView_Navigate(string navItemTag, Windows.UI.Xaml.Media.Animation.NavigationTransitionInfo transitionInfo, RequestData data = null)
		{
			Type _page = null;
			if (navItemTag == "settings")
			{
				_page = typeof(SettingsPage);
			}
			else if (navItemTag == "playlist")
			{
				navigationFrame.Navigate(typeof(PlaylistPage), data, transitionInfo);
                return;
			}
			else
			{
				var item = _pages.FirstOrDefault(p => p.Tag.Equals(navItemTag));
				_page = item.Page;
			}

			var preNavPageType = navigationFrame.CurrentSourcePageType;

			if (!(_page is null) && !Type.Equals(preNavPageType, _page))
			{
				navigationFrame.Navigate(_page, null, transitionInfo);
			}
		}

		public static MediaPlayerElement _mediaPlayerElement;
		public static MediaPlayer _mediaPlayer;
		public static MediaPlaybackList _mediaPlaybackList;

		public static List<(Composition composition, MediaPlaybackItem item)> _query = new List<(Composition composition, MediaPlaybackItem item)>();

		public static void PlayComposition(List<Composition> compositions, int first)
		{
			_mediaPlayer?.Pause();

			_query.Clear();

			MediaPlaybackItem firstItem = null;
			foreach (Composition composition in compositions)
			{
				var mediaPlaybackItem = new MediaPlaybackItem(MediaSource.CreateFromUri(new Uri("http://localhost/soundgood/materials/music/" + composition.Data.NamePath)));
				_mediaPlaybackList.Items.Add(mediaPlaybackItem);

				_query.Add((composition, mediaPlaybackItem));

				if (compositions.IndexOf(composition) == first)
					firstItem = mediaPlaybackItem;
			}

			if (firstItem != null)
				_mediaPlaybackList.StartingItem = firstItem;

			_mediaPlaybackList.ItemOpened += MediaPlaybackList_ItemOpened;
			_mediaPlaybackList.ItemFailed += MediaPlaybackList_ItemFailed;

			_mediaPlaybackList.MaxPlayedItemsToKeepOpen = 3;

			_mediaPlayer = new MediaPlayer();
			_mediaPlayer.Source = _mediaPlaybackList;
			_mediaPlayerElement.SetMediaPlayer(_mediaPlayer);

			_mediaPlayer.Play();
		}

		private static void MediaPlaybackItem_TimedMetadataTracksChanged(MediaPlaybackItem sender, IVectorChangedEventArgs args)
		{
			Debug.Write("Сработал TimedMetadataTracksChanged");
		}

		private static void PlaybackItem_AudioTracksChanged(MediaPlaybackItem sender, IVectorChangedEventArgs args)
		{
			Debug.Write("дорожка изменилась");
		}

		private static void MediaPlaybackList_ItemFailed(MediaPlaybackList sender, MediaPlaybackItemFailedEventArgs args)
		{
			Debug.Write("не удалось проиграть трек");
		}

		private static void MediaPlaybackList_ItemOpened(MediaPlaybackList sender, MediaPlaybackItemOpenedEventArgs args)
		{
			Debug.Write("трек готов к проигрыванию");
		}

		private static void MediaPlaybackList_CurrentItemChanged(MediaPlaybackList sender, CurrentMediaPlaybackItemChangedEventArgs args)
		{
			Debug.Write("трек изменился");
		}



		public static async void Alert(string title, string content)
        {
            ContentDialog errorDialog = new ContentDialog
            {
                Title = title,
                Content = content,
                CloseButtonText = "Ок",
            };

            ContentDialogResult result = await errorDialog.ShowAsync();
        }

        public static async Task<BitmapImage> Base64ToBitmap(string imageSymbols)
        {
            try
            {
                byte[] ib = Convert.FromBase64String(imageSymbols);

                using (Stream ms = new MemoryStream(ib))
                {
                    using (IRandomAccessStream fileStream = ms.AsRandomAccessStream())
                    {
                        BitmapImage bi = new BitmapImage();
                        bi.DecodePixelHeight = 150;
                        bi.DecodePixelWidth = 150;

                        await bi.SetSourceAsync(fileStream);
                        return bi;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
                return null;
            }
        }
    }

    public class IResponse
    {
        [JsonPropertyName("kind")]
        public string Kind { get; set; }

        [JsonPropertyName("error")]
        public Error Error { get; set; } = null;
    }

    public class Error
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public List<ErrorList> Errors { get; set; }

        public struct ErrorList
        {
            public string message;
            public string domain;
            public string reason;
        }

        public async void DisplayErrorDialog()
        {
            ContentDialog errorDialog = new ContentDialog
            {
                Title = "Ошибка " + this.Code,
                Content = this.Message,
                CloseButtonText = "Ясно.",
            };

            ContentDialogResult result = await errorDialog.ShowAsync();
        }
    }

	public class RequestData
	{
		public string Kind;
		public int Id;

		public RequestData(string kind, int id)
		{
			this.Kind = kind;
			this.Id = id;
		}
	}
}
