using Soundgood.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
    public sealed partial class HomePage : Page
    {
		List<Playlist> sets;
		public HomePage()
        {
            this.InitializeComponent();

			sets = GetEditorsSets(20, 1);
			UpdateSets();

			LoadSetPicture();
			Debug.WriteLine("------1--------");

			this.NavigationCacheMode = NavigationCacheMode.Enabled;
		}

		private async void LoadSetPicture()
		{
			foreach (Playlist item in sets)
			{
				string content = item.PlaylistData.ImageSymbols;

				if (content != null)
				{
					Task<BitmapImage> task = StaticItems.Base64ToBitmap(content);
					await task;
					item.Picture = task.Result;
				}
			}

			Debug.WriteLine("Картинки наборов загружены");
			UpdateSets();
		}

		private void UpdateSets()
		{
			object cvsSets = this.Resources["cvsSets"];
			(cvsSets as CollectionViewSource).Source = sets;
		}

		private List<Playlist> GetEditorsSets(int maxResults, int page)
		{
			try
			{
				using (WebClient webClient = new WebClient())
				{
					webClient.BaseAddress = StaticItems.EndPoint;
					var json = webClient.DownloadString($"sets?&maxResults={maxResults}&page={page}");

					var options = new JsonSerializerOptions
					{
						PropertyNameCaseInsensitive = true
					};
					var response = JsonSerializer.Deserialize<SetList>(json, options);

					Debug.WriteLine("GetEditorsSets: Ответ получен");

					if (response.Error != null)
					{
						response.Error.DisplayErrorDialog();
						return null;
					}

					if (response.Kind == "music#setListResponse")
						return response.Sets;
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

		private void GridView_ItemClick(object sender, ItemClickEventArgs e)
		{
			Playlist set = e.ClickedItem as Playlist;
			RequestData data = new RequestData(set.Kind, set.Id);
			StaticItems.NavView_Navigate("playlist", new Windows.UI.Xaml.Media.Animation.EntranceNavigationTransitionInfo(), data);
		}
	}

	public class SetList : IResponse
	{
		[JsonPropertyName("pageInfo")]
		public ListInfo SetListInfo { get; set; }

		[JsonPropertyName("items")]
		public List<Playlist> Sets { get; set; }

		public struct ListInfo
		{
			public int ResultsPerPage { get; set; }
		}
	}
}
