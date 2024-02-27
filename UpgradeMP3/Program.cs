using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;

using HtmlAgilityPack;

namespace UpgradeMP3
{
    internal class Program
    {
        #region Methods

        private static void Main()
        {
            Mp3 mp3 = new Mp3();
            //mp3.ClearTagAll();
            mp3.SetImagesMP3();
            Console.WriteLine($"готово");
            _ = Console.ReadLine();
        }

        #endregion Methods
    }

    public class Mp3
    {
        #region Fields
        private const int COUNT_TRY = 10;
        private static readonly string APPLICATION_NAME = Assembly.GetEntryAssembly().GetName().Name;
        private static readonly string APPLICATION_PATH = Assembly.GetEntryAssembly().Location;
        private static readonly string PATH = APPLICATION_PATH.Remove(APPLICATION_PATH.Length - APPLICATION_NAME.Length - 4);//4 = количество букв в .exe
        private int countDone, countAll;

        #endregion Fields

        #region Methods
        private void Status()
        {
            Console.Clear();
            if (countAll != 0)
                Console.WriteLine($"{countDone} файлов обработано из {countAll} файлов: {Math.Round(countDone / (double)countAll * 100.0, 2)}%");
            else
                Console.WriteLine("нет файлов");
        }
        private void CountFiles()
        {
            countAll = 0;
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
                    countAll++;
            }

            if (directory.GetDirectories().Length == 0)
                return;

            foreach (DirectoryInfo subDirectory in directory.GetDirectories())
                SetImagesMP3(subDirectory);
        }

        private void SetAlbumArt(string url, TagLib.File file)
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
                SetImagesMP3(subDirectory);
        }
        public void ClearTag(string path)
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
                Console.WriteLine("Альбом: {0}\nИсполнитель: {1}\nНазвание: {2}\nГод: {3}\nДлительность: {4}"
                        , audioFile.Tag.Album
                        , string.Join(", ", audioFile.Tag.Performers)
                        , audioFile.Tag.Title
                        , audioFile.Tag.Year
                        , audioFile.Properties.Duration.ToString("mm\\:ss"));
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

        public void SetImagesMP3()
        {
            CountFiles();
            countDone = 0;
            Status();
            Console.ForegroundColor = ConsoleColor.White;
            string pathDirectory = PATH;
            if (Directory.Exists(pathDirectory))
                SetImagesMP3(new DirectoryInfo(pathDirectory));
        }

        public void SetImagesMP3(DirectoryInfo directory)
        {
            if (directory.GetFiles().Length == 0)
                return;

            foreach (FileInfo file in directory.GetFiles())
            {
                if (file.Name.EndsWith(".mp3"))
                    SetImageMP3(file.Name, directory.FullName);
            }

            if (directory.GetDirectories().Length == 0)
                return;

            foreach (DirectoryInfo subDirectory in directory.GetDirectories())
                SetImagesMP3(subDirectory);
        }

        public void SetImageMP3(string nameMP3, string path)
        {
            #region инициализация

            string searchQuery = nameMP3 + " обложка"; // ваш поисковый запрос
            string url = $"https://www.bing.com/images/search?q={searchQuery}";// URL для поискового запроса
            HtmlWeb web = new HtmlWeb();
            HtmlDocument doc;
            HtmlNodeCollection imageNodes = null;
            for (int id = 0; id < COUNT_TRY; id++)
            {
                doc = web.Load(url);
                // Находим все ссылки на страницы с качесвенным фото на странице
                imageNodes = doc.DocumentNode.SelectNodes("//a[contains(@class, 'iusc')]");
                if (imageNodes != null)
                    break;
            }

            if (imageNodes == null)
                return;

            TagLib.File file = TagLib.File.Create(path + nameMP3);
            TagLib.Id3v2.Tag.DefaultVersion = 3;
            TagLib.Id3v2.Tag.ForceDefaultVersion = true;
            string previewImageUrl, imageUrl;
            HtmlDocument docFindImage;
            HtmlNode findImageNode;

            #endregion инициализация

            // Проходим по всем найденным ссылкам на страницы с качесвенным фото
            foreach (HtmlNode img in imageNodes)
            {
                previewImageUrl = img.GetAttributeValue("href", "");
                if (string.IsNullOrEmpty(previewImageUrl) || !previewImageUrl.StartsWith(@"/images/search?"))
                    continue;
                previewImageUrl = @"https://www.bing.com" + previewImageUrl.Replace(';', '&') + '\n';
                docFindImage = new HtmlWeb().Load(previewImageUrl);
                findImageNode = docFindImage.DocumentNode.SelectSingleNode("//img");
                if (findImageNode == null)
                    continue;
                imageUrl = findImageNode.GetAttributeValue("src", "").Replace(';', '&');
                if (string.IsNullOrEmpty(imageUrl))
                    continue;
                try
                {
                    SetAlbumArt(imageUrl, file);
                    file.Save();
                    countDone++;
                    Status();
                    return;
                }
                catch { }
            }
        }

        #endregion Methods
    }
}