using Soundgood.Pages;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Text.Json.Serialization;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Management;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using static Soundgood.Model.Composition;
using static Soundgood.Pages.LibraryPage;

// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x419

namespace Soundgood
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private void GoBtn_Click(object sender, RoutedEventArgs e)
        {
			if (tryLogIn() == true)
				this.Frame.Navigate(typeof(AppPage));
        }

        private bool tryLogIn()
        {
            if (String.IsNullOrWhiteSpace(emailTb.Text))
            {
				StaticItems.Alert("Заполните все поля", "Необходимо ввести логин для входа в приложение.");
				return false;
            }
            if (!(new EmailAddressAttribute().IsValid(emailTb.Text)))
            {
				StaticItems.Alert("Неверные поля", "Логин не в правильной форме.");
				return false;
			}
			if (String.IsNullOrWhiteSpace(passTb.Password))
			{
				StaticItems.Alert("Заполните все поля", "Необходимо ввести пароль для входа в приложение.");
				return false;
			}

			try
			{
				using (WebClient webClient = new WebClient())
				{
					webClient.BaseAddress = StaticItems.EndPoint;
					webClient.Headers[HttpRequestHeader.ContentType] = "application/json";

					Dictionary<string, string> data = new Dictionary<string, string>
					{
						{ "email", emailTb.Text },
						{ "pass", passTb.Password }
					};

					var json = webClient.UploadString("user/login", JsonSerializer.Serialize<Dictionary<string, string>>(data));

					var options = new JsonSerializerOptions
					{
						PropertyNameCaseInsensitive = true
					};
					var response = JsonSerializer.Deserialize<UserGetPrivilages>(json, options);

					Debug.WriteLine("GetUserPlaylists: Ответ получен");

					if (response.Error != null)
					{
						response.Error.DisplayErrorDialog();
						return false;
					}

					if (response.Code == "201")
					{
						StaticItems.user = response;
						return true;
					}
					else if (response.Code == "404")
					{
						StaticItems.Alert("Ошибка входа", response.Message);
						return false;
					}
					else
					{
						StaticItems.Alert("Ошибка", "Данные получены не верно\nИзвеняемся за неудобства!");
						return false;
					}
				}
			}
			catch (WebException e)
			{
				StaticItems.Alert("Ошибка", e.Message);
				return false;
			}
		}
    }

	public class UserGetPrivilages : IResponse
	{
		public string Code { get; set; }
		public string Message { get; set; }

		[JsonPropertyName("snippet")]
		public DataList Data { get; set; }

		public struct DataList
		{
			[JsonPropertyName("userid")]
			public int Id { get; set; }

			[JsonPropertyName("useremail")]
			public string Email { get; set; }

			[JsonPropertyName("userrole")]
			public char Role { get; set; }
		}
	}
}
