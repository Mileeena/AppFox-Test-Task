using Avalonia.Platform;
using PropertyChanged;
using ReactiveUI;
using Splat;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using System.Windows.Input;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using AppFoxTT.Models;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using Avalonia.Threading;

namespace AppFoxTT.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public class MainWindowViewModel
    {
        /// <summary>
        /// HttpClient
        /// </summary>
        private HttpClient client = new HttpClient();
        
        /// <summary>
        /// Коллекция скаченных скриншотов
        /// </summary>
        public ObservableCollection<Avalonia.Media.Imaging.Bitmap> Images { get; set; } =
            new ObservableCollection<Avalonia.Media.Imaging.Bitmap>();

        /// <summary>
        /// Дата начала периода
        /// </summary>
        public DateTimeOffset StartDate { get; set; } = DateTimeOffset.Now;

        /// <summary>
        /// Дата окончания периода
        /// </summary>
        public DateTimeOffset EndDate { get; set; } = DateTimeOffset.Now;

        /// <summary>
        /// Команда создания скриншотов
        /// </summary>
        public ICommand ScreenshotCommand { get; set; }

        /// <summary>
        /// Команда загрузки скриншотов
        /// </summary>
        public ICommand GetScreenshotCommand { get; set; }

        /// <summary>
        /// Коллекция дисплеев компьютера
        /// </summary>
        private IReadOnlyList<Screen> screens { get; }

        /// <summary>
        /// Текст ошибки
        /// </summary>
        public string ErrorMessage { get; set; }
        public MainWindowViewModel()
        {
            screens = Locator.Current.GetService<ScreensHelper>().ScreensList;

            ScreenshotCommand = ReactiveCommand.Create(() =>
            {
                TakeScreenshot();
            });

            GetScreenshotCommand = ReactiveCommand.Create(() =>
            {
                GetScreenshots(StartDate, EndDate);
            });
        }

        /// <summary>
        /// Создание и отправка скриншотов
        /// </summary>
        private void TakeScreenshot()
        {
            //Если у пользователя несколько мониторов, скриншоты будут отдельными
            foreach (var erScreen in screens)
            {
                Rectangle rect = new Rectangle(erScreen.Bounds.X, erScreen.Bounds.Y, erScreen.Bounds.Width, erScreen.Bounds.Height);
                Bitmap bmp = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);

                using (var e = Graphics.FromImage(bmp))
                {
                    e.CopyFromScreen(erScreen.Bounds.X, erScreen.Bounds.Y, 0, 0,
                        bmp.Size, CopyPixelOperation.SourceCopy);
                }
                UploadImage(ConvertToByte(bmp));
            }
        }

        /// <summary>
        /// Конвертер из Bitmap в массив Byte
        /// </summary>
        private static Byte[] ConvertToByte(Bitmap bitmap)
        {
            if (bitmap != null)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    bitmap.Save(ms, ImageFormat.Jpeg);
                    return ms.ToArray();
                }
            }
            return null;
        }

        /// <summary>
        /// Загрузка на сервер
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private async void UploadImage(Byte[] byteImage)
        {
            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    MultipartFormDataContent form = new MultipartFormDataContent();

                    var Name = $"sceenshot-{DateTime.Now}";
                    form.Add(new ByteArrayContent(byteImage), "file", Name);
                    HttpResponseMessage response = await httpClient.PostAsync("http://45.84.226.180/UploadScreenshot", form);

                    if (response.StatusCode != HttpStatusCode.NoContent &&
                        response.StatusCode != HttpStatusCode.OK)
                    {
                        ErrorMessage =
                            $"StatusCode: {response.StatusCode}. Скриношот не отправлен.";
                    }
                }
            }
            catch
            {
            }

        }

        /// <summary>
        /// Получить скриншоты за выбранный период
        /// </summary>
        /// <param name="startDate">Дата начала</param>
        /// <param name="endDate">Дата окончания</param>
        private async void GetScreenshots(DateTimeOffset startDate, DateTimeOffset endDate)
        {
            try
            {
                if (startDate > endDate) (startDate, endDate) = (endDate, startDate);

                using (HttpClient httpClient = new HttpClient())
                {
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Get,
                        RequestUri = new Uri($@"http://45.84.226.180/GetScreenshots?startDate={startDate.Date:yyyy.MM.dd}&endDate={endDate.Date:yyyy.MM.dd}")
                    };

                    var response = await client.SendAsync(request).ConfigureAwait(false);
                    
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string jsonContent = response.Content.ReadAsStringAsync().Result;
                        var items = JsonConvert.DeserializeObject<List<ScreenshotEntityModel>>(jsonContent);

                        //Dispatcher.UIThread.InvokeAsync используем для обратного вызова, т.к. находимся не в потоке с интерфейсом
                        if (Images.Count != 0) Dispatcher.UIThread.InvokeAsync(() => Images.Clear());
                        foreach (var e in items)
                        {
                            var bytes = e.screenshot.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => Convert.ToByte(s, 16))
                                .ToArray();

                            Bitmap bmp;
                            using (var ms = new MemoryStream(bytes))
                            {
                                bmp = new Bitmap(ms);
                                Dispatcher.UIThread.InvokeAsync(() => Images.Add(ConvertToAvaloniaBitmap(bmp)));
                            }
                        }
                    }
                    else
                    {
                        ErrorMessage =
                            $"StatusCode: {response.StatusCode}. Скриношоты не получены.";
                    }
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Конвертер Bitmap для авалонии
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        public static Avalonia.Media.Imaging.Bitmap ConvertToAvaloniaBitmap(System.Drawing.Image bitmap)
        {
            if (bitmap == null)
                return null;
            System.Drawing.Bitmap bitmapTmp = new System.Drawing.Bitmap(bitmap);
            var bitmapdata = bitmapTmp.LockBits(new Rectangle(0, 0, bitmapTmp.Width, bitmapTmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            Avalonia.Media.Imaging.Bitmap bitmap1 = new Avalonia.Media.Imaging.Bitmap(Avalonia.Platform.PixelFormat.Bgra8888, Avalonia.Platform.AlphaFormat.Premul,
                bitmapdata.Scan0,
                new Avalonia.PixelSize(bitmapdata.Width, bitmapdata.Height),
                new Avalonia.Vector(96, 96),
                bitmapdata.Stride);
            bitmapTmp.UnlockBits(bitmapdata);
            bitmapTmp.Dispose();
            return bitmap1;
        }
    }
}
