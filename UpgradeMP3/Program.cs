using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;

using HtmlAgilityPack;

namespace UpgradeMP3
{
    internal static class Program
    {
        private static void Main()
        {
            Console.Write($"взять фото из интернета (Y): ");
            char keyChar = Console.ReadKey().KeyChar.ToString().ToUpper()[0];
            ImageMp3 imageMp3 = new ImageMp3(keyChar is 'Y');
            //mp3.ClearTagAll();
            imageMp3.SetAllMP3();
            Console.WriteLine($"готово");
            _ = Console.ReadLine();
        }
    }

    public class ImageMp3
    {
        private const int COUNT_TRY = 10;
        private static readonly string APPLICATION_NAME = Assembly.GetEntryAssembly()?.GetName().Name;
        private static readonly string APPLICATION_PATH = Assembly.GetEntryAssembly()?.Location;
        private static readonly string PATH = APPLICATION_PATH.Remove(APPLICATION_PATH.Length - APPLICATION_NAME.Length - 4);//4 = количество букв в .exe
        private int _countDone, _countAll;
        private bool _internet;

        public ImageMp3(bool internet)
        {
            _internet = internet;
        }
        private void Status()
        {
            Console.Clear();
            Console.WriteLine(_countAll != 0
                ? $"{_countDone} файлов обработано из {_countAll} файлов: {Math.Round(_countDone / (double)_countAll * 100.0, 2)}%"
                : "нет файлов");
        }
        private void CountFiles()
        {
            _countAll = 0;
            string pathDirectory = PATH;
            if (Directory.Exists(pathDirectory))
                CountFiles(new DirectoryInfo(pathDirectory));
        }

        private void CountFiles(DirectoryInfo directory)
        {
            if (directory.GetFiles().Length == 0)
                return;

            foreach (FileInfo file in directory.GetFiles())
            {
                if (file.Name.EndsWith(".mp3"))
                    _countAll++;
            }

            if (directory.GetDirectories().Length == 0)
                return;

            foreach (DirectoryInfo subDirectory in directory.GetDirectories())
                SetAllMP3(subDirectory);
        }

        private static void SetAlbumArt(string url, TagLib.File file)
        {
            byte[] imageBytes;
            using (WebClient client = new WebClient())
            {
                imageBytes = client.DownloadData(url);
            }

            TagLib.Id3v2.AttachedPictureFrame cover = new TagLib.Id3v2.AttachedPictureFrame
            {
                Type = TagLib.PictureType.LeafletPage,
                Description = "",
                MimeType = System.Net.Mime.MediaTypeNames.Image.Jpeg,
                Data = imageBytes,
                TextEncoding = TagLib.StringType.UTF16
            };
            file.Tag.Pictures = new TagLib.IPicture[] { cover };
        }
        public void ClearTagAll()
        {
            string pathDirectory = PATH;
            if (Directory.Exists(pathDirectory))
                ClearTagAll(new DirectoryInfo(pathDirectory));
        }

        private void ClearTagAll(DirectoryInfo directory)
        {
            if (directory.GetFiles().Length == 0)
                return;

            foreach (FileInfo file in directory.GetFiles())
            {
                if (file.Name.EndsWith(".mp3"))
                    ClearTag(file.FullName);
            }

            if (directory.GetDirectories().Length == 0)
                return;

            foreach (DirectoryInfo subDirectory in directory.GetDirectories())
                SetAllMP3(subDirectory);
        }

        private static void ClearTag(string path)
        {
            //Создаем переменную с информацией о файле. В качестве параметра указываем полный путь.
            using (TagLib.File audioFile = TagLib.File.Create(path))
            {
                audioFile.Tag.Album = null;
                audioFile.Save();
            }
        }
        public void GetTags(string path)
        {
            //Создаем переменную с информацией о файле. В качестве параметра указываем полный путь.
            using (TagLib.File audioFile = TagLib.File.Create(path))
            {
                //Выводим нужную нам информацию на экран
                Console.WriteLine("Альбом: {0}\nИсполнитель: {1}\nНазвание: {2}\nГод: {3}\nДлительность: {4:mm\\:ss}"
                        , audioFile.Tag.Album
                        , string.Join(", ", audioFile.Tag.Performers)
                        , audioFile.Tag.Title
                        , audioFile.Tag.Year, audioFile.Properties.Duration);
            }
        }

        public void AddMp3Tags(string path, string nameMP3/*, string urlImage*/, string songTitle, string[] artists, string album, uint trackNumber, string year)
        {
            TagLib.Id3v2.Tag.DefaultVersion = 3;
            TagLib.Id3v2.Tag.ForceDefaultVersion = true;
            TagLib.File file = TagLib.File.Create(path + nameMP3 + ".mp3");
            //SetAlbumArt(urlImage, file);
            file.Tag.Title = songTitle;
            file.Tag.Performers = artists;
            file.Tag.Album = album;
            file.Tag.Track = trackNumber;
            file.Tag.Year = (uint)Convert.ToInt32(Regex.Match(year, @"(\d)(\d)(\d)(\d)").Value);
            file.RemoveTags(file.TagTypes & ~file.TagTypesOnDisk);
            file.Save();
        }

        public void SetAllMP3()
        {
            CountFiles();
            _countDone = 0;
            Status();
            Console.ForegroundColor = ConsoleColor.White;
            string pathDirectory = PATH;
            if (Directory.Exists(pathDirectory))
                SetAllMP3(new DirectoryInfo(pathDirectory));
        }

        private void SetAllMP3(DirectoryInfo directory)
        {
            if (directory.GetFiles().Length == 0)
                return;

            foreach (FileInfo file in directory.GetFiles())
            {
                if (file.Name.EndsWith(".mp3"))
                {
                    SetMP3(file.Name, directory.FullName);
                }
            }

            if (directory.GetDirectories().Length == 0)
                return;

            foreach (DirectoryInfo subDirectory in directory.GetDirectories())
                SetAllMP3(subDirectory);
        }

        private void SetMP3(string nameMP3, string path)
        {
            TagLib.File file = TagLib.File.Create(path + nameMP3);
            TagLib.Id3v2.Tag.DefaultVersion = 3;
            TagLib.Id3v2.Tag.ForceDefaultVersion = true;
            //file.Tag.Lyrics = "";

            if(_internet)
                SetImageFromInternet(nameMP3, file);
            else
                SetImageFromLocal(nameMP3, file);
        }
        private void SetImageFromLocal(string nameMP3, TagLib.File file)
        {
            string path = nameMP3.Replace(".mp3", "");
            if(File.Exists(path + ".png"))
                path = path + ".png";
            else if(File.Exists(path + ".jpg"))
                path = path + ".jpg";
            byte[] imageBytes = File.ReadAllBytes(path);
            TagLib.Id3v2.AttachedPictureFrame cover = new TagLib.Id3v2.AttachedPictureFrame
            {
                Type = TagLib.PictureType.LeafletPage,
                Description = "",
                MimeType = System.Net.Mime.MediaTypeNames.Image.Jpeg,
                Data = imageBytes,
                TextEncoding = TagLib.StringType.UTF16
            };
            file.Tag.Pictures = new TagLib.IPicture[] { cover };
            file.Save();
            _countDone++;
            Status();
        }
        private void SetImageFromInternet(string nameMP3, TagLib.File file)
        {
            /*string searchQuery = nameMP3 + " обложка"; // ваш поисковый запрос*/
            string url = $"https://www.bing.com/images/search?q={nameMP3}";
            HtmlWeb web = new HtmlWeb();
            HtmlNodeCollection imageNodes = null;
            for (int id = 0; id < COUNT_TRY; id++)
            {
                HtmlDocument doc = web.Load(url);
                // ReSharper disable once StringLiteralTypo
                imageNodes = doc.DocumentNode.SelectNodes("//a[contains(@class, 'iusc')]");
                if (imageNodes != null)
                    break;
            }

            if (imageNodes == null)
                return;

            // Проходим по всем найденным ссылкам на страницы с качественным фото
            foreach (HtmlNode img in imageNodes)
            {
                if (FindArt(file, img.GetAttributeValue("href", "")))
                    break;
            }
        }

        private bool FindArt(TagLib.File file, string previewImageUrl)
        {
            if (string.IsNullOrEmpty(previewImageUrl) || !previewImageUrl.StartsWith(@"/images/search?"))
                return false;
            previewImageUrl = @"https://www.bing.com" + previewImageUrl.Replace(';', '&') + '\n';
            HtmlDocument docFindImage = new HtmlWeb().Load(previewImageUrl);
            HtmlNode findImageNode = docFindImage.DocumentNode.SelectSingleNode("//img");
            if (findImageNode == null)
                return false;
            string imageUrl = findImageNode.GetAttributeValue("src", "").Replace(';', '&');
            if (string.IsNullOrEmpty(imageUrl))
                return false;
            try
            {
                SetAlbumArt(imageUrl, file);
                file.Save();
                _countDone++;
                Status();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}