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
            VisualFeatureTypes.Categories,
            VisualFeatureTypes.Color,
            VisualFeatureTypes.Description,
            VisualFeatureTypes.Faces,
            VisualFeatureTypes.ImageType,
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

            // If the user has not entered a URL Endpoint in the CVEndpoint
            // textbox, display an error message prompting the user to enter 
            // the Endpoint.
            //
            // Different users may have obtained their keys from different
            // regions. For example, one user may have obtained their key
            // from the West Central US region, while another has obtained
            // their key from the South-East Asia region, etc.
            //
            // The Endpoint specifies the region where the user has obtained
            // their key, and hence which region that the key is valid for
            // use.
            if (String.IsNullOrEmpty(CVEndpoint.Text))
            {
                lblError.Content = "Please specify an Endpoint.";
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
                computerVision.Endpoint = CVEndpoint.Text;

                CVKey.Text = "Connection successful.";
                CVKey.IsReadOnly = true;
                MSCognitiveServicesLogin.IsEnabled = false;

                CVEndpoint.Text = "";
                CVEndpoint.IsReadOnly = true;
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

            openDlg.Filter = "JPEG Image(*.jpg)|*.jpg|PNG Image(*.png)|*.png|Bitmap Image(*.bmp)|*.bmp|GIF Image(*gif)|*.gif";
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
            // If computerVision is not initialized, display an error message
            // prompting the user to enter the relevant credentials (i.e. API key
            // and/or Endpoint).
            if (computerVision == null)
            {
                lblError.Content = "Computer Vision API Client not initialized. Please enter your API key and/or Endpoint if you haven't already done so.";
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
                // Call the Computer Vision API to perform Image Analysis.
                //
                // The returned object (of ImageAnalysis type) contains the
                // information returned from the Image Analysis operation.
                ImageAnalysis analysis = await computerVision.AnalyzeImageInStreamAsync(
                    imageStream, features);

                imageDescriptionStatusBar.Text = "";

                // Each set of information contained in the ImageAnalysis object
                // is processed and displayed as text.
                DisplayImageTypeInfo(analysis);
                DisplayColorInfo(analysis);
                DisplayCategoryInfo(analysis);
                DisplayDescriptionInfo(analysis);
                DisplayTagInfo(analysis);
                DetectImagesInFace(analysis);
            }
        }

        // Displays Image Type information obtained from the Image Analysis operation.
        //
        // If ImageType is amongst the features specified for the API to return (in the
        // case of this demo program, ImageType is included), the API will analyze the
        // image to determine if it is Clipart or a Line drawing. The ImageAnalysis object
        // returned will hence contain two values - ClipArtType and LineDrawing. Both of
        // these attributes will be explained in the comments below.
        private void DisplayImageTypeInfo(ImageAnalysis analysis)
        {
            imageDescriptionStatusBar.Text += "\n[IMAGE TYPE INFO]";
            imageDescriptionStatusBar.Text += "\nClip Art Type: ";

            // ClipArtType represents whether or not the API has determined that
            // the image is Clipart, and if so, the type of Clipart the image is.
            //
            // Its values include Non-clipart (0), ambiguous (1), 
            // normal (2), and good clipart (3).
            switch (analysis.ImageType.ClipArtType)
            {
                case 1:
                    imageDescriptionStatusBar.Text += "Ambiguous";
                    break;

                case 2:
                    imageDescriptionStatusBar.Text += "Normal";
                    break;

                case 3:
                    imageDescriptionStatusBar.Text += "Good";
                    break;

                default:
                    imageDescriptionStatusBar.Text += "Non-Clipart";
                    break;
            }

            // Similar to the case of ClipArtType, LineDrawingType represents whether
            // or not the API has determined that the image is a Line drawing.
            //
            // Unlike ClipArtType, however, LineDrawingType is a binary value that is
            // either 0 (non-line drawing) or 1 (line drawing).
            imageDescriptionStatusBar.Text += "\nIs Line Drawing: ";

            if (analysis.ImageType.LineDrawingType == 0)
            {
                imageDescriptionStatusBar.Text += "False";
            }
            else
            {
                imageDescriptionStatusBar.Text += "True";
            }

            imageDescriptionStatusBar.Text += "\n";
        }

        // Displays Image Color information obtained from the Image Analysis operation.
        private void DisplayColorInfo(ImageAnalysis analysis)
        {
            imageDescriptionStatusBar.Text += "\n[COLOR INFO]";
            imageDescriptionStatusBar.Text += "\nAccent Color: " + analysis.Color.AccentColor;
            imageDescriptionStatusBar.Text += "\nDominant Color Background: " + analysis.Color.DominantColorBackground;
            imageDescriptionStatusBar.Text += "\nDominant Color Foreground: " + analysis.Color.DominantColorForeground;
            imageDescriptionStatusBar.Text += "\nDominant Colors: ";

            for (int i = 0; i < analysis.Color.DominantColors.Count; i++)
            {
                imageDescriptionStatusBar.Text += analysis.Color.DominantColors[i];

                if (i != analysis.Color.DominantColors.Count - 1)
                {
                    imageDescriptionStatusBar.Text += ", ";
                }
            }

            imageDescriptionStatusBar.Text += "\nIs Black & White image: " + analysis.Color.IsBWImg + "\n";
        }

        // Displays Image Category information obtained from the Image Analysis operation.
        private void DisplayCategoryInfo(ImageAnalysis analysis)
        {
            imageDescriptionStatusBar.Text += "\n[CATEGORIES]\n";

            for (int i = 0; i < analysis.Categories.Count; i++)
            {
                imageDescriptionStatusBar.Text += "Category " + (i + 1) + ": " + analysis.Categories[i].Name;

                imageDescriptionStatusBar.Text += "\n[Details]";
                var catDetails = analysis.Categories[i].Detail;
                
                if (catDetails == null)
                {
                    imageDescriptionStatusBar.Text += "\nNo details.";
                }
                else
                {
                    imageDescriptionStatusBar.Text += "\nCelebrities: ";

                    if (catDetails.Celebrities == null || catDetails.Celebrities.Count == 0)
                    {
                        imageDescriptionStatusBar.Text += "None";
                    }
                    else
                    {
                        for (int j = 0; j < catDetails.Celebrities.Count; j++)
                        {
                            imageDescriptionStatusBar.Text += catDetails.Celebrities[j].Name;

                            if (j != catDetails.Celebrities.Count - 1)
                            {
                                imageDescriptionStatusBar.Text += ", ";
                            }
                        }
                    }

                    imageDescriptionStatusBar.Text += "\nLandmarks: ";

                    if (catDetails.Landmarks == null || catDetails.Landmarks.Count == 0)
                    {
                        imageDescriptionStatusBar.Text += "None";
                    }
                    else
                    {
                        for (int j = 0; j < catDetails.Landmarks.Count; j++)
                        {
                            imageDescriptionStatusBar.Text += catDetails.Landmarks[j].Name;

                            if (j != catDetails.Landmarks.Count - 1)
                            {
                                imageDescriptionStatusBar.Text += ", ";
                            }
                        }
                    }
                }

                imageDescriptionStatusBar.Text += "\n";
            }
        }

        // Displays Image Description information obtained from the Image Analysis operation.
        private void DisplayDescriptionInfo(ImageAnalysis analysis)
        {
            imageDescriptionStatusBar.Text += "\n[DESCRIPTION]";
            imageDescriptionStatusBar.Text += "\nCaptions: ";

            for (int i = 0; i < analysis.Description.Captions.Count; i++)
            {
                imageDescriptionStatusBar.Text += analysis.Description.Captions[i].Text;

                if (i != analysis.Description.Captions.Count - 1)
                {
                    imageDescriptionStatusBar.Text += ", ";
                }
            }            

            imageDescriptionStatusBar.Text += "\nDescription Tags: ";

            for (int i = 0; i < analysis.Description.Tags.Count; i++)
            {
                imageDescriptionStatusBar.Text += analysis.Description.Tags[i];

                if (i != analysis.Description.Tags.Count - 1)
                {
                    imageDescriptionStatusBar.Text += ", ";
                }
            }

            imageDescriptionStatusBar.Text += "\n";
        }

        // Displays Image Tag information obtained from the Image Analysis operation.
        private void DisplayTagInfo(ImageAnalysis analysis)
        {
            imageDescriptionStatusBar.Text += "\n[TAGS]\n";

            for (int i = 0; i < analysis.Tags.Count; i++)
            {
                imageDescriptionStatusBar.Text += analysis.Tags[i].Name;

                if (i != analysis.Tags.Count - 1)
                {
                    imageDescriptionStatusBar.Text += ", ";
                }
            }

            imageDescriptionStatusBar.Text += "\n";
        }

        // The code for this method has been adapted from my FaceRecogDemo program
        // (see https://davidlim2007.wordpress.com/2018/06/03/facial-detection-recognition-with-the-microsoft-face-api/)
        // which in turn uses code adapted from https://docs.microsoft.com/en-us/azure/cognitive-services/Face/Tutorials/FaceAPIinCSharpTutorial
        private void DetectImagesInFace(ImageAnalysis analysis)
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

                // Display the image with the rectangle(s) around the face(s).
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
            // If computerVision is not initialized, display an error message
            // prompting the user to enter the relevant credentials (i.e. API key
            // and/or Endpoint).
            if (computerVision == null)
            {
                lblError.Content = "Computer Vision API Client not initialized. Please enter your API key and/or Endpoint if you haven't already done so.";
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

                DrawTextRectangles(lines);
            }
        }

        // The code for this method has been adapted from my FaceRecogDemo program
        // (see https://davidlim2007.wordpress.com/2018/06/03/facial-detection-recognition-with-the-microsoft-face-api/)
        // which in turn uses code adapted from https://docs.microsoft.com/en-us/azure/cognitive-services/Face/Tutorials/FaceAPIinCSharpTutorial
        //
        // This method searches for each line of text detected in the image and draws a
        // rectangle over them to highlight their presence in the image. When the Text
        // Extraction operation completes, the API returns, for each line of text detected,
        // a set of coordinates called a BoundingBox. Each BoundingBox contains the coordinates
        // of the corresponding line on the image. 
        //
        // In this method, we will interpret these coordinates and use them to draw rectangles
        // around each line in the image, much like we have already done for faces.
        private void DrawTextRectangles(IList<Line> lines)
        {
            double currentResizeFactor;

            if (lines.Count > 0)
            {
                // Prepare to draw rectangles around each text line.
                DrawingVisual visual = new DrawingVisual();
                DrawingContext drawingContext = visual.RenderOpen();
                drawingContext.DrawImage(bitmapSource,
                    new Rect(0, 0, bitmapSource.Width, bitmapSource.Height));

                double dpi = bitmapSource.DpiX;
                currentResizeFactor = 96 / dpi;

                foreach (Line line in lines)
                {
                    // According to https://westus.dev.cognitive.microsoft.com/docs/services/5adf991815e1060e6355ad44/operations/587f2cf1154055056008f201:
                    // The BoundingBox attribute for each line represents the four points 
                    // (x-coordinate, y-coordinate) of the detected Line Rectangle from the
                    // top-left corner and clockwise.
                    //
                    // This means that the first element of BoundingBox contains the x-coordinate
                    // of the top-left corner, the second element contains the y-coordinate of the
                    // top-left corner, the third element contains the x-coordinate of the top-right
                    // corner, and so on.
                    //
                    // Using this, we can discern the necessary information (e.g. the width and height
                    // of each Line Rectangle) in order to draw them on the image.
                    int xcoord = line.BoundingBox[0];
                    int ycoord = line.BoundingBox[1];
                    int width = line.BoundingBox[2] - line.BoundingBox[0];
                    int height = line.BoundingBox[5] - line.BoundingBox[3];
                    
                    // Draw a rectangle on the line.
                    drawingContext.DrawRectangle(
                        Brushes.Transparent,
                        new Pen(Brushes.Red, 2),
                        new Rect(
                            xcoord * currentResizeFactor,
                            ycoord * currentResizeFactor,
                            width * currentResizeFactor,
                            height * currentResizeFactor
                            )                        
                    );
                }

                drawingContext.Close();

                // Display the image with the rectangle(s) around the lines.
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
    }
}
