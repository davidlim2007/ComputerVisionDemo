using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ComputerVisionDemo
{
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

        // A List that specifies the features to return from the Computer
        // Vision API. This only applies to Image Analysis.
        private static readonly List<VisualFeatureTypes> features =
            new List<VisualFeatureTypes>()
        {
            VisualFeatureTypes.Categories, VisualFeatureTypes.Description,
            VisualFeatureTypes.Faces, VisualFeatureTypes.ImageType,
            VisualFeatureTypes.Tags
        };

        // The Text Recognition type to help the Computer Vision API recognize
        // and extract text from an image. Can be toggled between Printed and Handwritten.
        private const TextRecognitionMode textRecognitionMode = TextRecognitionMode.Printed;
        private const int numberOfCharsInOperationId = 36;

        public MainWindow()
        {
            InitializeComponent();
        }

        // MSCognitiveServicesLogin_Click() is the handler for the Click event of the
        // button named MSCognitiveServicesLogin in the xaml.
        //
        // This handler responds by instantiating a new ComputerVisionClient object 
        // based on the Computer Vision API key supplied in the CVKey TextBox.
        private void MSCognitiveServicesLogin_Click(object sender, RoutedEventArgs e)
        {
            // If the user has not entered a key in the CVKey textbox,
            // display an error message prompting the user to enter the
            // key.
            if (String.IsNullOrEmpty(CVKey.Text))
            {
                lblError.Content = "Please enter a Computer Vision API Key.";
                lblError.Visibility = Visibility.Visible;
                return;
            }
            
            lblError.Visibility = Visibility.Hidden;

            // Else, instantiate computerVision using the entered key.
            computerVision = new ComputerVisionClient(
                new ApiKeyServiceClientCredentials(CVKey.Text),
                new System.Net.Http.DelegatingHandler[] { }
            );

            // If computerVision has been successfully instantiated, the connection
            // to the Computer Vision API is deemed successful. We display a success
            // message and disable the CVKey textbox and MSCognitiveServicesLogin button.
            //
            // Else, if computerVision could not be instantiated, we display an error
            // message.
            if (computerVision != null)
            {
                CVKey.Text = "Connection successful.";
                CVKey.IsReadOnly = true;
                MSCognitiveServicesLogin.IsEnabled = false;
                computerVision.Endpoint = "https://westcentralus.api.cognitive.microsoft.com";
            }
            else
            {
                lblError.Content = "Connection failed, please try again!";
                lblError.Visibility = Visibility.Visible;
            }
        }

        // BrowseButton_Click() is the event handler shared by the "Browse" button.
        // When clicked, a file dialog is opened, allowing the user to choose the
        // image file to upload. The user is currently limited to JPEG images
        // (*.jpg or *.jpeg).
        //
        // When the image is selected, the local path to the image is assigned to
        // global variable filePath. This allows the program to know which image
        // to upload to the API when either Analyze Image or Extract Text is
        // clicked.
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

            // Save the local path of the file.
            filePath = openDlg.FileName;

            // If the file path is invalid, display an error message.
            if (!File.Exists(filePath))
            {
                lblError.Content = "Unable to open or read Image Path: " + filePath;
                lblError.Visibility = Visibility.Visible;
                return;
            }

            Uri fileUri = new Uri(filePath);
            BitmapImage bitmapSource = new BitmapImage();

            // Display the image on the Window.
            bitmapSource.BeginInit();
            bitmapSource.CacheOption = BitmapCacheOption.None;
            bitmapSource.UriSource = fileUri;
            bitmapSource.EndInit();

            ImgUpload.Source = bitmapSource;
        }

        // The Click event handler for the Analyze Image button.
        //
        // This method simply calls the async method AnalyzeImageAsync()
        // and waits for it to finish.
        private async void btnAnalyzeImage_Click(object sender, RoutedEventArgs e)
        {
            await AnalyzeImageAsync();
        }

        // This method is adapted from the AnalyzeLocalAsync() method from
        // the following tutorial: 
        // https://docs.microsoft.com/en-us/azure/cognitive-services/Computer-vision/quickstarts-sdk/csharp-analyze-sdk
        //
        // 
        private async Task AnalyzeImageAsync()
        {
            // If the user has not entered a key, display an error message
            // prompting the user to do so.
            if (String.IsNullOrEmpty(CVKey.Text))
            {
                lblError.Content = "Please enter a Computer Vision API Key.";
                lblError.Visibility = Visibility.Visible;
                return;
            }

            // If the user has not uploaded an image, display an error message
            // prompting the user to do so.
            if (String.IsNullOrEmpty(filePath))
            {
                lblError.Content = "Please upload an image.";
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
