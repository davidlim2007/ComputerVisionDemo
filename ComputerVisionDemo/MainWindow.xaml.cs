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

        // The bitmap source of the image to be uploaded. Used in drawing face
        // rectangles on the image.
        private BitmapImage bitmapSource;

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
            bitmapSource = new BitmapImage();

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
            imageDescriptionStatusBar.Text = "Analyzing...";

            using (Stream imageStream = File.OpenRead(filePath))
            {
                ImageAnalysis analysis = await computerVision.AnalyzeImageInStreamAsync(
                    imageStream, features);

                imageDescriptionStatusBar.Text = "Categories: ";

                for (int i = 0; i < analysis.Categories.Count; i++)
                {
                    imageDescriptionStatusBar.Text += analysis.Categories[i].Name;

                    if (i != analysis.Categories.Count - 1)
                    {
                        imageDescriptionStatusBar.Text += ", ";
                    }
                }

                imageDescriptionStatusBar.Text += "\nDescription: ";
                imageDescriptionStatusBar.Text += analysis.Description.Captions[0].Text + "\n";
                imageDescriptionStatusBar.Text += "Description Tags: ";

                for (int i = 0; i < analysis.Description.Tags.Count; i++)
                {
                    imageDescriptionStatusBar.Text += analysis.Description.Tags[i];

                    if (i != analysis.Description.Tags.Count - 1)
                    {
                        imageDescriptionStatusBar.Text += ", ";
                    }
                }

                imageDescriptionStatusBar.Text += "\nOther Tags: ";

                for (int i = 0; i < analysis.Tags.Count; i++)
                {
                    imageDescriptionStatusBar.Text += analysis.Tags[i].Name;

                    if (i != analysis.Tags.Count - 1)
                    {
                        imageDescriptionStatusBar.Text += ", ";
                    }
                }

                DetectImagesInFaceAsync(analysis);
            }
        }

        // The code for this method has been adapted from my FaceRecogDemo program
        // (see https://davidlim2007.wordpress.com/2018/06/03/facial-detection-recognition-with-the-microsoft-face-api/)
        // which in turn uses code adapted from https://docs.microsoft.com/en-us/azure/cognitive-services/Face/Tutorials/FaceAPIinCSharpTutorial
        private void DetectImagesInFaceAsync(ImageAnalysis analysis)
        {
            // Note that the Computer Vision API can be configured to return information
            // of faces detected in an image (see the features list). Hence there is no
            // need to make an explicit call to the Face API unless more substantial
            // information on the faces (e.g. Face Descriptions) are required.
            imageDescriptionStatusBar.Text += "\nFaces detected: " + analysis.Faces.Count;

            // Define a double (currentResizeFactor) for the ResizeFactor for the current image.
            // Note that the Resize Factor depends on the resolution of the current image as so 
            // is not a constant value. Another important factor is the XAML image tag "Stretch" 
            // property.
            double currentResizeFactor;

            if (analysis.Faces.Count > 0)
            {
                // Prepare to draw rectangles around the faces.
                DrawingVisual visual = new DrawingVisual();
                DrawingContext drawingContext = visual.RenderOpen();
                drawingContext.DrawImage(bitmapSource,
                    new Rect(0, 0, bitmapSource.Width, bitmapSource.Height));

                double dpi = bitmapSource.DpiX;
                currentResizeFactor = 96 / dpi;

                foreach (FaceDescription face in analysis.Faces)
                {
                    // Draw a rectangle on the face.
                    // Note that the dimensions given in FaceRectangle
                    // is relative to the dimensions of the bitmap which
                    // has been sent for face detection.
                    drawingContext.DrawRectangle(
                        Brushes.Transparent,
                        new Pen(Brushes.Red, 2),
                        new Rect(
                            face.FaceRectangle.Left * currentResizeFactor,
                            face.FaceRectangle.Top * currentResizeFactor,
                            face.FaceRectangle.Width * currentResizeFactor,
                            face.FaceRectangle.Height * currentResizeFactor
                            )
                    );
                }

                drawingContext.Close();

                // Display the image with the rectangle around the face.
                RenderTargetBitmap imgWithRectBitmap = new RenderTargetBitmap(
                    (int)(bitmapSource.PixelWidth * currentResizeFactor),
                    (int)(bitmapSource.PixelHeight * currentResizeFactor),
                    96,
                    96,
                    PixelFormats.Pbgra32);

                imgWithRectBitmap.Render(visual);
                ImgUpload.Source = imgWithRectBitmap;
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

            lblError.Visibility = Visibility.Hidden;
            imageDescriptionStatusBar.Text = "Extracting text...";

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
