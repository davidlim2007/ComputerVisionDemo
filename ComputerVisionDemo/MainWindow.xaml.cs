using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ComputerVisionDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // The ComputerVisionClient object used to perform calls to the
        // Computer Vision API.
        //
        // ComputerVisionClient is a C# object that serves as a wrapper
        // that simplifies the web-method call process by internally calling
        // the JSON strings and making HTTP POST calls.
        private IComputerVisionClient computerVision;

        // The local filepath of the image being uploaded to the API.
        private string filePath;

        private static readonly List<VisualFeatureTypes> features =
            new List<VisualFeatureTypes>()
        {
            VisualFeatureTypes.Categories, VisualFeatureTypes.Description,
            VisualFeatureTypes.Faces, VisualFeatureTypes.ImageType,
            VisualFeatureTypes.Tags
        };

        private const TextRecognitionMode textRecognitionMode = TextRecognitionMode.Printed;
        private const int numberOfCharsInOperationId = 36;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MSCognitiveServicesLogin_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(CVKey.Text))
            {
                lblError.Content = "Please enter a Computer Vision API Key.";
                lblError.Visibility = Visibility.Visible;
                return;
            }
            
            lblError.Visibility = Visibility.Hidden;
            computerVision = new ComputerVisionClient(
                new ApiKeyServiceClientCredentials(CVKey.Text),
                new System.Net.Http.DelegatingHandler[] { }
            );

            if (computerVision != null)
            {
                CVKey.Text = "";
                MSCognitiveServicesLogin.IsEnabled = false;
                computerVision.Endpoint = "https://westcentralus.api.cognitive.microsoft.com";
                lblError.Content = "Connection successful.";
            }
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            // Get the image file to scan from the user.
            var openDlg = new Microsoft.Win32.OpenFileDialog();

            openDlg.Filter = "JPEG Image(*.jpg)|*.jpg";
            bool? result = openDlg.ShowDialog(this);

            // Return if canceled.
            if (!(bool)result)
            {
                return;
            }

            filePath = openDlg.FileName;

            Uri fileUri = new Uri(filePath);
            BitmapImage bitmapSource = new BitmapImage();

            bitmapSource.BeginInit();
            bitmapSource.CacheOption = BitmapCacheOption.None;
            bitmapSource.UriSource = fileUri;
            bitmapSource.EndInit();

            ImgUpload.Source = bitmapSource;
        }

        private async void btnAnalyzeImage_Click(object sender, RoutedEventArgs e)
        {
            await AnalyzeImageAsync();
        }

        private async Task AnalyzeImageAsync()
        {
            if (String.IsNullOrEmpty(CVKey.Text))
            {
                lblError.Content = "Please enter a Computer Vision API Key.";
                lblError.Visibility = Visibility.Visible;
                return;
            }

            if (String.IsNullOrEmpty(filePath))
            {
                lblError.Content = "Please upload an image.";
                lblError.Visibility = Visibility.Visible;
                return;
            }

            if (!File.Exists(filePath))
            {
                lblError.Content = "Unable to open or read Image Path: " + filePath;
                lblError.Visibility = Visibility.Visible;
                return;
            }

            lblError.Visibility = Visibility.Hidden;

            using (Stream imageStream = File.OpenRead(filePath))
            {
                ImageAnalysis analysis = await computerVision.AnalyzeImageInStreamAsync(
                    imageStream, features);

                imageDescriptionStatusBar.Text = "Description: ";
                imageDescriptionStatusBar.Text += analysis.Description.Captions[0].Text + "\n";
                imageDescriptionStatusBar.Text += "Tags: ";

                for (int i = 0; i < analysis.Description.Tags.Count; i++)
                {
                    imageDescriptionStatusBar.Text += analysis.Description.Tags[i];

                    if (i != analysis.Description.Tags.Count - 1)
                    {
                        imageDescriptionStatusBar.Text += ", ";
                    }
                }
            }
        }

        private async void btnExtractText_Click(object sender, RoutedEventArgs e)
        {
            await GetTextAsync();
        }

        private async Task GetTextAsync()
        {
            if (String.IsNullOrEmpty(CVKey.Text))
            {
                lblError.Content = "Please enter a Computer Vision API Key.";
                lblError.Visibility = Visibility.Visible;
                return;
            }

            if (String.IsNullOrEmpty(filePath))
            {
                lblError.Content = "Please upload an image.";
                lblError.Visibility = Visibility.Visible;
                return;
            }

            if (!File.Exists(filePath))
            {
                lblError.Content = "Unable to open or read Image Path: " + filePath;
                lblError.Visibility = Visibility.Visible;
                return;
            }

            using (Stream imageStream = File.OpenRead(filePath))
            {
                // Start the async process to recognize the text
                RecognizeTextInStreamHeaders textHeaders =
                   await computerVision.RecognizeTextInStreamAsync(
                       imageStream, textRecognitionMode);

                string operationLocation = textHeaders.OperationLocation;

                // Retrieve the URI where the recognized text will be
                // stored from the Operation-Location header
                string operationId = operationLocation.Substring(
                    operationLocation.Length - numberOfCharsInOperationId);

                TextOperationResult result = await computerVision.GetTextOperationResultAsync(operationId);

                // Wait for the operation to complete
                int i = 0;
                int maxRetries = 10;
                while ((result.Status == TextOperationStatusCodes.Running ||
                        result.Status == TextOperationStatusCodes.NotStarted) && i++ < maxRetries)
                {
                    await Task.Delay(1000);

                    result = await computerVision.GetTextOperationResultAsync(operationId);
                }

                // Display the results
                var lines = result.RecognitionResult.Lines;
                imageDescriptionStatusBar.Text = "Text:\n";

                foreach (Line line in lines)
                {
                    imageDescriptionStatusBar.Text += line.Text + "\n";
                }
            }
        }
    }
}
