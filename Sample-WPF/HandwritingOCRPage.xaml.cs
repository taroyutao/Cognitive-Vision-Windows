﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Microsoft.ProjectOxford.Vision;
using Microsoft.ProjectOxford.Vision.Contract;
using System.IO;

namespace VisionAPI_WPF_Samples
{
    /// <summary>
    /// Interaction logic for HandwritingOCRPage.xaml
    /// </summary>
    public partial class HandwritingOCRPage : ImageScenarioPage
    {
        public HandwritingOCRPage()
        {
            InitializeComponent();
            this.PreviewImage = _imagePreview;
            this.URLTextBox = _urlTextBox;
        }

        /// <summary>
        /// Uploads the image to Project Oxford and performs OCR
        /// </summary>
        /// <param name="imageFilePath">The image file path.</param>
        /// <param name="language">The language code to recognize for</param>
        /// <returns></returns>
        private async Task<HandwritingOCROperationResult> UploadAndRecognizeImage(string imageFilePath)
        {
            using (Stream imageFileStream = File.OpenRead(imageFilePath))
            {
                return await Recognize(async (VisionServiceClient VisionServiceClient) => await VisionServiceClient.CreateHandwritingOCROperationAsync(imageFileStream));
            }
        }

        /// <summary>
        /// Sends a url to Project Oxford and performs OCR
        /// </summary>
        /// <param name="imageUrl">The url to perform recognition on</param>
        /// <param name="language">The language code to recognize for</param>
        /// <returns></returns>
        private async Task<HandwritingOCROperationResult> RecognizeUrl(string imageUrl)
        {
            return await Recognize(async (VisionServiceClient VisionServiceClient) => await VisionServiceClient.CreateHandwritingOCROperationAsync(imageUrl));
        }

        private async Task<HandwritingOCROperationResult> Recognize(Func<VisionServiceClient, Task<HandwritingOCROperation>> Func)
        {
            // -----------------------------------------------------------------------
            // KEY SAMPLE CODE STARTS HERE
            // -----------------------------------------------------------------------

            //
            // Create Project Oxford Vision API Service client
            //
            VisionServiceClient VisionServiceClient = new VisionServiceClient(SubscriptionKey);
            Log("VisionServiceClient is created");

            HandwritingOCROperationResult result;
            try
            {
                Log("Calling VisionServiceClient.CreateHandwritingOCROperationAsync()...");
                HandwritingOCROperation operation = await Func(VisionServiceClient);

                Log("Calling VisionServiceClient.GetHandwritingOCROperationResultAsync()...");
                result = await VisionServiceClient.GetHandwritingOCROperationResultAsync(operation);

                int i = 0;
                while ((result.Status == HandwritingOCROperationStatus.Running || result.Status == HandwritingOCROperationStatus.NotStarted) && i++ < MaxRetryTimes)
                {
                    Log(string.Format("Server status: {0}, wait {1} seconds...", result.Status, QueryWaitTimeInSecond));
                    await Task.Delay(QueryWaitTimeInSecond);

                    Log("Calling VisionServiceClient.GetHandwritingOCROperationResultAsync()...");
                    result = await VisionServiceClient.GetHandwritingOCROperationResultAsync(operation);
                }

            }
            catch (ClientException ex)
            {
                result = new HandwritingOCROperationResult() { Status = HandwritingOCROperationStatus.Failed };
                Log(ex.Error.Message);
            }

            return result;

            // -----------------------------------------------------------------------
            // KEY SAMPLE CODE ENDS HERE
            // -----------------------------------------------------------------------
        }

        /// <summary>
        /// Perform the work for this scenario
        /// </summary>
        /// <param name="imageUri">The URI of the image to run against the scenario</param>
        /// <param name="upload">Upload the image to Project Oxford if [true]; submit the Uri as a remote url if [false];</param>
        /// <returns></returns>
        protected override async Task DoWork(Uri imageUri, bool upload)
        {
            _status.Text = "Performing HandwritingOCR...";

            //
            // Either upload an image, or supply a url
            //
            HandwritingOCROperationResult result;
            if (upload)
            {
                result = await UploadAndRecognizeImage(imageUri.LocalPath);
            }
            else
            {
                result = await RecognizeUrl(imageUri.AbsoluteUri);
            }
            _status.Text = "HandwritingOCR Done";

            //
            // Log analysis result in the log window
            //
            LogOneOCRResult(result);
            Log("HandwritingOCR Done!");
        }
    }
}
