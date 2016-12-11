using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Net.Http;
using System.IO;
using Newtonsoft.Json;


namespace ScreenOCR
{
    public partial class Form1 : Form
    {
        private const string SETTINGS_JSON_FILE_NAME = "settings.json";

        HotKey hotKey;

        public string capturedImageFilePath { get; set; }

        private AppSettings appSettings;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            hotKey = new HotKey(MOD_KEY.CONTROL, Keys.F1);
            hotKey.HotKeyPush += new EventHandler(hotKey_HotKeyPush);

            appSettings = new AppSettings();
            appSettings.LanguageIndex = 5;
            appSettings.ApiKey = "";

            if (System.IO.File.Exists(SETTINGS_JSON_FILE_NAME))
            {
                using (FileStream fs = new FileStream(SETTINGS_JSON_FILE_NAME, FileMode.Open))
                {
                    using (StreamReader sr = new StreamReader(fs))
                    {
                        string jsonString = sr.ReadToEnd();
                        AppSettings settings = JsonConvert.DeserializeObject<AppSettings>(jsonString);
                        if (settings.LanguageIndex < 0 || settings.LanguageIndex >= languageComboBox.Items.Count)
                        {
                            settings.LanguageIndex = appSettings.LanguageIndex;
                        }
                        appSettings = settings;
                    }
                }
            }

            languageComboBox.SelectedIndex = appSettings.LanguageIndex;

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            hotKey.Dispose();

            appSettings.LanguageIndex = languageComboBox.SelectedIndex;
            using (FileStream fs = new FileStream(SETTINGS_JSON_FILE_NAME, FileMode.Create))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.Write(JsonConvert.SerializeObject(appSettings));
                }
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            DisposePreviewImage();
            DeleteCapturedImageFile();
        }

        private void captureButton_Click(object sender, EventArgs e)
        {
            CaptureScreen();
        }

        private void clearButton_Click(object sender, EventArgs e)
        {
            resultTextBox.Text = "";
            DisposePreviewImage();
        }

        void hotKey_HotKeyPush(object sender, EventArgs e)
        {
            CaptureScreen();
        }

        public void CaptureBitmap(Bitmap bitmap)
        {
            string tempPath = Path.GetTempPath();

            string path = Path.Combine(tempPath, "temp.jpg");

            SaveBitmapToFile(bitmap, path, 80);

            previewPictureBox.BackgroundImage = Image.FromFile(path);

            capturedImageFilePath = path;
        }

        public async void ParseImage()
        {
            if (string.IsNullOrEmpty(capturedImageFilePath))
            {
                return;
            }

            FileInfo fileInfo = new FileInfo(capturedImageFilePath);
            if (fileInfo.Length > 1 * 1024 * 1024)
            {
                //Size limit depends: Free API 1 MB, PRO API 5 MB and more
                MessageBox.Show("Image file size limit reached (1MB free API)");
                return;
            }

            if (string.IsNullOrEmpty(appSettings.ApiKey))
            {
                //Size limit depends: Free API 1 MB, PRO API 5 MB and more
                MessageBox.Show("Get API key at \"https://ocr.space/OCRAPI\" and write it in \"" + SETTINGS_JSON_FILE_NAME + "\"");
                return;
            }

            try
            {
                HttpClient httpClient = new HttpClient();
                httpClient.Timeout = new TimeSpan(1, 1, 1);

                MultipartFormDataContent form = new MultipartFormDataContent();
                form.Add(new StringContent(appSettings.ApiKey), "apikey");
                form.Add(new StringContent(GetSelectedLanguage()), "language");

                byte[] imageData = File.ReadAllBytes(capturedImageFilePath);
                form.Add(new ByteArrayContent(imageData, 0, imageData.Length), "image", "image.jpg");

                HttpResponseMessage response = await httpClient.PostAsync("https://api.ocr.space/Parse/Image", form);

                string strContent = await response.Content.ReadAsStringAsync();

                Rootobject ocrResult = JsonConvert.DeserializeObject<Rootobject>(strContent);

                if (ocrResult.OCRExitCode == 1)
                {
                    for (int i = 0; i < ocrResult.ParsedResults.Count(); i++)
                    {
                        resultTextBox.Text = resultTextBox.Text + ocrResult.ParsedResults[i].ParsedText;
                    }
                }
                else
                {
                    MessageBox.Show("ERROR: " + strContent);
                }

            }
            catch (Exception exception)
            {
                MessageBox.Show("ERROR: " + exception.ToString());
            }
        }

        private string GetSelectedLanguage()
        {
            // https://ocr.space/OCRAPI#PostParameters
            // Czech = cze; Danish = dan; Dutch = dut; English = eng; Finnish = fin; French = fre; 
            // German = ger; Hungarian = hun; Italian = ita; Norwegian = nor; Polish = pol; Portuguese = por;
            // Spanish = spa; Swedish = swe; ChineseSimplified = chs; Greek = gre; Japanese = jpn; Russian = rus;
            // Turkish = tur; ChineseTraditional = cht; Korean = kor

            string strLang = "";
            switch (languageComboBox.SelectedIndex)
            {
                case 0:
                    strLang = "chs";
                    break;
                case 1:
                    strLang = "cht";
                    break;
                case 2:
                    strLang = "cze";
                    break;
                case 3:
                    strLang = "dan";
                    break;
                case 4:
                    strLang = "dut";
                    break;
                case 5:
                    strLang = "eng";
                    break;
                case 6:
                    strLang = "fin";
                    break;
                case 7:
                    strLang = "fre";
                    break;
                case 8:
                    strLang = "ger";
                    break;
                case 9:
                    strLang = "gre";
                    break;
                case 10:
                    strLang = "hun";
                    break;
                case 11:
                    strLang = "ita";
                    break;
                case 12:
                    strLang = "jpn";
                    break;
                case 13:
                    strLang = "kor";
                    break;
                case 14:
                    strLang = "nor";
                    break;
                case 15:
                    strLang = "pol";
                    break;
                case 16:
                    strLang = "por";
                    break;
                case 17:
                    strLang = "rus";
                    break;
                case 18:
                    strLang = "spa";
                    break;
                case 19:
                    strLang = "swe";
                    break;
                case 20:
                    strLang = "tur";
                    break;

            }
            return strLang;

        }

        private byte[] ImageToBase64(Image image, System.Drawing.Imaging.ImageFormat format)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                // Convert Image to byte[]
                image.Save(ms, format);
                byte[] imageBytes = ms.ToArray();

                return imageBytes;
            }
        }

        private void DisposePreviewImage()
        {
            if (previewPictureBox.BackgroundImage != null)
            {
                previewPictureBox.BackgroundImage.Dispose();
                previewPictureBox.BackgroundImage = null;
            }
        }

        // MimeTypeで指定されたImageCodecInfoを探して返す
        private System.Drawing.Imaging.ImageCodecInfo
            GetEncoderInfo(string mineType)
        {
            //GDI+ に組み込まれたイメージ エンコーダに関する情報をすべて取得
            System.Drawing.Imaging.ImageCodecInfo[] encs =
                System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders();
            //指定されたMimeTypeを探して見つかれば返す
            foreach (System.Drawing.Imaging.ImageCodecInfo enc in encs)
            {
                if (enc.MimeType == mineType)
                {
                    return enc;
                }
            }
            return null;
        }

        /// <summary>
        /// 指定された画像ファイルを、品質を指定してJPEGで保存する
        /// </summary>
        /// <param name="fileName">変換する画像ファイル名</param>
        /// <param name="quality">品質</param>
        private void SaveBitmapToFile(Bitmap srcBitmap, string fileName, int quality)
        {
            //EncoderParameterオブジェクトを1つ格納できる
            //EncoderParametersクラスの新しいインスタンスを初期化
            //ここでは品質のみ指定するため1つだけ用意する
            System.Drawing.Imaging.EncoderParameters eps =
                new System.Drawing.Imaging.EncoderParameters(1);
            //品質を指定
            System.Drawing.Imaging.EncoderParameter ep =
                new System.Drawing.Imaging.EncoderParameter(
                System.Drawing.Imaging.Encoder.Quality, (long)quality);
            //EncoderParametersにセットする
            eps.Param[0] = ep;

            //イメージエンコーダに関する情報を取得する
            System.Drawing.Imaging.ImageCodecInfo ici = GetEncoderInfo("image/jpeg");

            //保存する
            srcBitmap.Save(fileName, ici, eps);

            srcBitmap.Dispose();
            eps.Dispose();
        }

        private void CaptureScreen()
        {
            // 画面キャプチャして作成したファイルがBackgroundImageに使われていると
            // 上書き保存に失敗するので
            // 画面キャプチャの前にここでDisposeする
            DisposePreviewImage();
            DeleteCapturedImageFile();

            //Form2クラスのインスタンスを作成する
            Form2 f = new Form2();

            //Form2を表示する
            //ここではモーダルダイアログボックスとして表示する
            //オーナーウィンドウにthisを指定する
            f.ShowDialog(this);
        }

        private void DeleteCapturedImageFile()
        {
            if (string.IsNullOrEmpty(capturedImageFilePath))
            {
                return;
            }
            System.IO.File.Delete(capturedImageFilePath);
            capturedImageFilePath = "";
        }
    }
}



