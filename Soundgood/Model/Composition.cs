using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml;

namespace Soundgood.Model
{
    public class Composition
    {
        public int Id { get; set; }

        [JsonPropertyName("snippet")]
        public DataList Data { get; set; }

        public struct DataList
        {
            public string Name { get; set; }
            public List<ArtistsList> Artists { get; set; }
            public Album? Album { get; set; }
            public string Date { get; set; }
            public string Text { get; set; }
            public int Auditions { get; set; }
            public bool Explicit { get; set; }

			[JsonPropertyName("namepath")]
			public string NamePath { get; set; }
		}

        public struct ArtistsList
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
        public struct Album
        {
            public int Id { get; set; }
            public string Name { get; set; }

            [JsonPropertyName("picture")]
            public string ImageSymbols { get; set; }
		}

        public BitmapImage Picture { get; set; }

        public Visibility AlbumVisibility
        {
            get
            {
                if (this.Data.Album?.Name != null)
                    return Visibility.Visible;
                return Visibility.Collapsed;
            }
        }

        public MemoryStream MemoryStream { get; set; } = null;
        public bool IsPlaying { get; set; } = false;
    }
}
