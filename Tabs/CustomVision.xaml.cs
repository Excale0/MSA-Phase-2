﻿
using Newtonsoft.Json;
using Plugin.Geolocator;
using Plugin.Media;
using Plugin.Media.Abstractions;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Tabs.DataModels;
using Tabs.Model;
using Xamarin.Forms;

namespace Tabs
{
    public partial class CustomVision : ContentPage
    {
        public CustomVision()
        {
            InitializeComponent();
        }

        private async void loadCamera(object sender, EventArgs e)
        {
            await CrossMedia.Current.Initialize();

            if (!CrossMedia.Current.IsCameraAvailable || !CrossMedia.Current.IsTakePhotoSupported)
            {
                await DisplayAlert("No Camera", ":( No camera available.", "OK");
                return;
            }

            MediaFile file = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions
            {
                PhotoSize = PhotoSize.Medium,
                Directory = "Sample",
                Name = $"{DateTime.UtcNow}.jpg"
            });

            if (file == null)
                return;

            image.Source = ImageSource.FromStream(() =>
            {
                return file.GetStream();
            });

            //await postLocationAsync();

            await MakePredictionRequest(file);
        }

        static byte[] GetImageAsByteArray(MediaFile file)
        {
            var stream = file.GetStream();
            BinaryReader binaryReader = new BinaryReader(stream);
            return binaryReader.ReadBytes((int)stream.Length);
        }

        async Task MakePredictionRequest(MediaFile file)
        {
            var client = new HttpClient();

            client.DefaultRequestHeaders.Add("Prediction-Key", "81be588befe841cbb91627d809d387e9"); //need to change "Prediction-Key"

            string url = "https://southcentralus.api.cognitive.microsoft.com/customvision/v1.0/Prediction/4da1555c-14ca-4aaf-af01-d6e1e97e5fa6/image?iterationId=7bc76035-3825-4643-917e-98f9d9f79b71"; //Change url

            HttpResponseMessage response;

            byte[] byteData = GetImageAsByteArray(file);

            var content = new ByteArrayContent(byteData);


            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            response = await client.PostAsync(url, content);


            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();

                EvaluationModel responseModel = JsonConvert.DeserializeObject<EvaluationModel>(responseString);

                double max = responseModel.Predictions.Max(m => m.Probability);

                TagLabel.Text = (max >= 0.5) ? "Solved Cube" : "Unsolved Cube";

            }

            //Get rid of file once we have finished using it
            file.Dispose();

        }

        async Task postLocationAsync()
        {

            var locator = CrossGeolocator.Current;
            locator.DesiredAccuracy = 50;

            var position = await locator.GetPositionAsync(10000);

            Phase1Module2table model = new Phase1Module2table()
            {
                Longitude = (float)position.Longitude,
                Latitude = (float)position.Latitude

            };

            await AzureManager.AzureManagerInstance.PostCubeInformation(model);
        }

    }
}
