using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace YuGiOh_TTS_Deck_Builder
{
    /// <summary>
    /// The Main Page.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // Private fields.
        private Windows.Storage.StorageFile _fileDeckYDK;
        private Windows.Storage.StorageFile _fileDeckOutput;

        // ID and Secret from Enviornment Variables for Imgur.
        private const string _imgur_client_id = "";

        public MainPage()
        {
            this.InitializeComponent();
            ApplicationView.PreferredLaunchViewSize = new Size(480, 200);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
        }

        private async void btnDeckBrowse_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker filePicker = new FileOpenPicker();
            filePicker.SuggestedStartLocation = PickerLocationId.Desktop;
            filePicker.FileTypeFilter.Add(".ydk");

            Windows.Storage.StorageFile file = await filePicker.PickSingleFileAsync();
            if (file != null)
            {
                _fileDeckYDK = file;
                this.txtDeckPath.Text = file.Path;
            }
        }

        private async void btnDeckSave_Click(object sender, RoutedEventArgs e)
        {
            FileSavePicker filePicker = new FileSavePicker();
            filePicker.SuggestedStartLocation = PickerLocationId.Desktop;
            filePicker.FileTypeChoices.Add("Table Top Simulator Deck", new List<String>() { ".json" });

            Windows.Storage.StorageFile file = await filePicker.PickSaveFileAsync();
            if (file != null)
            {
                _fileDeckOutput = file;
                this.txtDeckSavePath.Text = file.Path;
            }
        }

        private async void btnProcess_Click(object sender, RoutedEventArgs e)
        {
            // Ensure YDK File, and output file have been specified
            if (_fileDeckYDK == null || _fileDeckOutput == null)
            {
                ContentDialog fileNotSpecified = new ContentDialog {
                    Title = "File Not Specified",
                    Content = "You must specify both a YDK file and your output filename.",
                    CloseButtonText = "Acknowledge"
                };
                await fileNotSpecified.ShowAsync();
                return;
            }

            //ensure that a network connection is available
            var networkAvailable = NetworkInterface.GetIsNetworkAvailable();
            if (!networkAvailable)
            {
                ContentDialog networkConnectionNotAvailable = new ContentDialog
                {
                    Title = "No Network Connection",
                    Content = "An active network connection is required to process.",
                    CloseButtonText = "Acknowledge"
                };
                await networkConnectionNotAvailable.ShowAsync();
                return;
            }

            // Show working ring
            enableWorkingRing();

            // Read YDK into list of lines.
            IList<string> strYDKLines = await Windows.Storage.FileIO.ReadLinesAsync(_fileDeckYDK);

            // Parse YDK lines into the three possible decks for Yu-Gi-Oh.
            List<string> strMainDeckCards = new List<string>();
            List<string> strExtraDeckCards = new List<string>();
            List<string> strSideDeckCards = new List<string>();

            // Main deck is first in the YDK file.
            YDKParts currentPart = YDKParts.unknown;
            for (int i = 0; i < strYDKLines.Count; i++)
            {
                string strCurrentLine = strYDKLines[i].Trim();
                if (strCurrentLine == "#main")
                {
                    // Set tracking variable to main deck.
                    currentPart = YDKParts.main;
                }
                else if (strCurrentLine == "#extra")
                {
                    // Set tracking variable to extra deck.
                    currentPart = YDKParts.extra;
                }
                else if (strCurrentLine == "!side")
                {
                    // Set tracking variable to side deck.
                    currentPart = YDKParts.side;
                }
                else
                {
                    if (currentPart == YDKParts.main)
                    {
                        if (!string.IsNullOrEmpty(strCurrentLine))
                        {
                            // Add card ID to main deck.
                            strMainDeckCards.Add(strCurrentLine);
                        }
                    }
                    else if (currentPart == YDKParts.extra)
                    {
                        if (!string.IsNullOrEmpty(strCurrentLine))
                        {
                            // Add card ID to extra deck.
                            strExtraDeckCards.Add(strCurrentLine);
                        }
                    }
                    else if (currentPart == YDKParts.side)
                    {
                        if (!string.IsNullOrEmpty(strCurrentLine))
                        {
                            // Add card ID to side deck.
                            strSideDeckCards.Add(strCurrentLine);
                        }
                    }
                }
            }

            // Save file and alert user
            // Url for card front picture: https://ygoprodeck.com/pics/34541863.jpg
            // Url for card title: https://db.ygoprodeck.com/card/?search=34541863
            // Url for card back picture:
            const string strBackCard = "https://i.imgur.com/ubLQ2p2.jpg";

            // Local booleans to hold whether we have parsed cards for each deck
            bool blnHaveMainDeck = strMainDeckCards.Count > 0;
            bool blnHaveExtraDeck = strExtraDeckCards.Count > 0;
            bool blnHaveSideDeck = strSideDeckCards.Count > 0;

            if (!blnHaveMainDeck && !blnHaveExtraDeck && !blnHaveSideDeck)
            {
                ContentDialog noCards = new ContentDialog
                {
                    Title = "No Cards Found",
                    Content = "We could not find any cards with the YGO Pro deck you provided. Please check your file and try again.",
                    CloseButtonText = "Acknowledge"
                };
                await noCards.ShowAsync();

                // Turn off working ring
                disableWorkingRing();
                return;
            }

            // Create new empty TTS object that will be the correct output format for TTS.
            TTS tts = new TTS();

            // Start empty ObjectStates List to hold our decks.
            tts.ObjectStates = new List<ObjectState>();

            if (blnHaveMainDeck)
            {
                // Generate Deck
                ObjectState mainDeckObjectState = await generateDeck(strMainDeckCards, strBackCard);
                
                // Add ObjectState object to TTS object.
                tts.ObjectStates.Add(mainDeckObjectState);
            }
            if (blnHaveExtraDeck)
            {
                // Generate Deck
                ObjectState extraDeckObjectState = await generateDeck(strExtraDeckCards, strBackCard);
                extraDeckObjectState.Transform.posX = -4;

                // Add ObjectState object to TTS object.
                tts.ObjectStates.Add(extraDeckObjectState);
            }
            if (blnHaveSideDeck)
            {
                // Generate Deck
                ObjectState sideDeckObjectState = await generateDeck(strSideDeckCards, strBackCard);
                sideDeckObjectState.Transform.posX = -8;

                // Add ObjectState object to TTS object.
                tts.ObjectStates.Add(sideDeckObjectState);
            }

            // Convert TTS object to JSON and write to the disk.
            await Windows.Storage.FileIO.WriteTextAsync(_fileDeckOutput, JsonConvert.SerializeObject(tts, Newtonsoft.Json.Formatting.None,
                            new JsonSerializerSettings
                            {
                                NullValueHandling = NullValueHandling.Ignore
                            }));

            // Turn off working ring
            disableWorkingRing();
        }

        private async void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            string uriToLaunch = @"https://github.com/kmccmk9/Yu-Gi-Oh-TTS-Deck-Builder";
            Uri uri = new Uri(uriToLaunch);
            await Windows.System.Launcher.LaunchUriAsync(uri);
        }

        /// <summary>
        /// Method to build the ObjectState object for the TTS object.
        /// </summary>
        /// <param name="strDeckIds">List of string representing the card Ids.</param>
        /// <param name="deckSize">Tuple representing the rows,columns of the WriteableBitmap.</param>
        /// <param name="strDeckFaceUrl">Direct image url representing the front of all cards specified in strDeckIds.</param>
        /// <param name="strDeckBackUrl">Direct image url representing the back of the card.</param>
        /// <param name="strCardNames">List of string representing the card names associated with strDeckIds.</param>
        /// <param name="intCustomDeckId">Integer representing the deck ID (important for distinguishing card Ids and decks).</param>
        /// <returns>Populated ObjectState object.</returns>
        private ObjectState buildObjectState(List<string> strDeckIds, Tuple<int,int> deckSize, string strDeckFaceUrl, string strDeckBackUrl, List<string> strCardNames, int intCustomDeckId)
        {
            // List of integers representing the cards in the deck. There can be duplicates in this list, but not in the Deck Face URL.
            List<int> cardIds = new List<int>();

            // This is required per TTS json requirements.
            // Start at Deck number * 100. We must subtract one because we increment before adding to account for duplicates.
            int intCardCounter = (100 * intCustomDeckId) - 1;

            // Iterate of card Ids.
            for (int i = 0; i < strDeckIds.Count; i++)
            {
             	 string cardId = strDeckIds[i];
                if (i > 0 && strDeckIds[i-1] == cardId)
                {
                    // Use the same Card Id without incrementing if the card is already on the list.
                    cardIds.Add(intCardCounter);
                }
                else
                {
                    // Add the Card Id to the list and increment counter.
                    intCardCounter++;
                    cardIds.Add(intCardCounter);
                }
            }

            // Populate ObjectState object
            ObjectState objectState = new ObjectState()
            {
                Name = "DeckCustom",
                Transform = new ObjectStateTransform(),
                CustomDeck = new CustomDeck(),
                DeckIDs = cardIds,
                ContainedObjects = new List<ContainedObject>()
            };
            switch (intCustomDeckId)
            {
                case 1:
                    objectState.CustomDeck.one = new DeckInfo()
                    {
                        NumHeight = deckSize.Item1,
                        NumWidth = deckSize.Item2,
                        FaceURL = strDeckFaceUrl,
                        BackURL = strDeckBackUrl,
                    };
                    break;
                case 2:
                    objectState.CustomDeck.two = new DeckInfo()
                    {
                        NumHeight = deckSize.Item1,
                        NumWidth = deckSize.Item2,
                        FaceURL = strDeckFaceUrl,
                        BackURL = strDeckBackUrl,
                    };
                    objectState.Transform.posX += 5;
                    break;
                case 3:
                    objectState.CustomDeck.three = new DeckInfo()
                    {
                        NumHeight = deckSize.Item1,
                        NumWidth = deckSize.Item2,
                        FaceURL = strDeckFaceUrl,
                        BackURL = strDeckBackUrl,
                    };
                    objectState.Transform.posX += 10;
                    break;
            }

            // Add unique card Ids to the ContainedObjects list.
            for (int i = 0; i < cardIds.Count; i++)
            {
                int cardId = cardIds[i];
                string cardName = "Card";
                string cardNickname = strCardNames[i];
                objectState.ContainedObjects.Add(new ContainedObject() { CardID = cardId, Name = cardName, Nickname = cardNickname, Transform = new ContainedObjectTransform() });
            }

            return objectState;
        }

        /// <summary>
        /// Method to return a Tuple representing the rows and columns represented in the WriteableBitmap. 
        /// If the rows is more than 1, columns are automatically 5. Columns can be less when the number of rows is 1.
        /// </summary>
        /// <param name="strDeckCardIds">List of string representing the card Ids.</param>
        /// <returns>Tuple representing rows,columns of the WriteableBitmap.</returns>
        private Tuple<int, int> getDeckSize(List<string> strDeckCardIds)
        {
            // Get a list of unique card IDs as the final image should only have each card appear once.
            strDeckCardIds = strDeckCardIds.Distinct().ToList();

            int intTotalCards = strDeckCardIds.Count;
            int remainder = 0;
            double cols = 5;
            if (intTotalCards / cols < 1)
            {
                remainder = intTotalCards % (int)cols;
            }
            else
            {
                remainder = (int)cols;
            }
            int rows = (int)Math.Ceiling(intTotalCards / cols);
            return new Tuple<int, int>(rows, remainder);
        }

        /// <summary>
        /// Method to convert WriteableBitmap into JPEG.
        /// </summary>
        /// <param name="bmp">WriteableBitmap to convert.</param>
        /// <returns>List of bytes representing the WriteableBitmap.</returns>
        private async Task<byte[]> EncodeJpeg(WriteableBitmap bmp)
        {
            SoftwareBitmap soft = SoftwareBitmap.CreateCopyFromBuffer(bmp.PixelBuffer, BitmapPixelFormat.Bgra8, bmp.PixelWidth, bmp.PixelHeight);
            byte[] array = null;

            using (var ms = new InMemoryRandomAccessStream())
            {
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, ms);
                encoder.SetSoftwareBitmap(soft);

                try
                {
                    await encoder.FlushAsync();
                }
                catch { }

                array = new byte[ms.Size];
                await ms.ReadAsync(array.AsBuffer(), (uint)ms.Size, InputStreamOptions.None);
            }

            return array;
        }

        /// <summary>
        /// Method to upload provided WriteableBitmap to Imgur.
        /// </summary>
        /// <param name="imageBitmap">Memory Access Stream of the image to upload.</param>
        /// <returns>String representing the direct image url for the uploaded image.</returns>
        private async Task<string> uploadPhotoToImgur(IRandomAccessStream imageBitmap)
        {
            using (var client = new HttpClient())
            {
                // Upload to Imgur as a form post with provided Client-Id
                using (var content =
                    new MultipartFormDataContent("Upload----" + DateTime.Now.ToString(CultureInfo.InvariantCulture)))
                {
                    // Convert to bytes
                    DataReader dr = new DataReader(imageBitmap.GetInputStreamAt(0));
                    byte[] bytes = new byte[imageBitmap.Size];
                    await dr.LoadAsync((uint)imageBitmap.Size);
                    dr.ReadBytes(bytes);

                    content.Add(new ByteArrayContent(bytes), "image", "deck.jpg");

                    client.DefaultRequestHeaders.Add("Authorization", "Client-ID " + _imgur_client_id);

                    // Disable Expect 100 Header because Imgur API chokes on it.
                    client.DefaultRequestHeaders.ExpectContinue = false;

                    using (var message = await client.PostAsync("https://api.imgur.com/3/upload", content))
                    {
                        if (message.IsSuccessStatusCode)
                        {
                            // Read HTTP POST response into the Imgur object.
                            string jsonString = await message.Content.ReadAsStringAsync();
                            Imgur imgurResponse = JsonConvert.DeserializeObject<Imgur>(jsonString, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                            return imgurResponse.data.link;
                        }
                        else
                        {
                            return string.Empty;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Method to generate a list of names for each card Id.
        /// </summary>
        /// <param name="strDeckCardIds">List of strings representing card Ids.</param>
        /// <returns>List of strings representing card names associated with card Ids.</returns>
        private async Task<List<string>> generateCardNamesForDeck(List<string> strDeckCardIds)
        {
            List<string> deckCardNames = new List<string>();

            // Iterate over the list of card Ids.
            foreach (string strDeckCardId in strDeckCardIds)
            {
                using (HttpClient client = new HttpClient())
                {
                    const string strCardInformationBaseUrl = "https://db.ygoprodeck.com/getcardbyid.php?cardid=";
                    using (HttpResponseMessage response = await client.GetAsync(strCardInformationBaseUrl + strDeckCardId))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            // Convert response into a YGOProCardDetails object.
                            YGOProCardDetails cardDetails = JsonConvert.DeserializeObject<YGOProCardDetails>(await response.Content.ReadAsStringAsync());

                            // Add to the list of card names.
                            deckCardNames.Add(cardDetails.name);
                        }
                    }
                }
            }

            return deckCardNames;
        }

        /// <summary>
        /// Method to generate a WriteableBitmap from list of card Ids.
        /// </summary>
        /// <param name="strDeckCardIds">List of strings representing card Ids.</param>
        /// <returns>A WriteableBitmap representing the combined image of cards.</returns>
        private async Task<WriteableBitmap> generateWriteableBitmapForDeck(List<string> strDeckCardIds)
        {
            // Get a list of unique card IDs as the final image should only have each card appear once.
            strDeckCardIds = strDeckCardIds.Distinct().ToList();

            // Calculate final image size based on how many cards we have in our list.
            int intTotalCards = strDeckCardIds.Count;
            double cols = 5;
            int rows = (int)Math.Ceiling(intTotalCards / cols);

            // Create out empty WriteableBitmap.
            var finalWriteableBitmap = new WriteableBitmap((int)cols * 421, rows * 614);

            // Track which column and row we are in.
            int intColumn = 0;
            int intRow = 0;

            // Constant string representing base url of card image.
            const string strCardFrontBaseUrl = "https://ygoprodeck.com/pics/";

            // Iterate over the list of card Ids.
            for (int i = 0; i < strDeckCardIds.Count; i++)
            {
                string strDeckCard = strDeckCardIds[i];

                // Create a WriteableBitmap to hold picture from YGOPro.
                var writeableBitmap = new WriteableBitmap(1, 1);
                using (HttpClient client = new HttpClient())
                {
                    using (HttpResponseMessage response = await client.GetAsync(strCardFrontBaseUrl + strDeckCard + ".jpg"))
                    {
                        var image = await BitmapFactory.FromStream(await response.Content.ReadAsStreamAsync());
                        byte[] pixels;
                        Guid BitmapEncoderGuid = BitmapEncoder.JpegEncoderId;
                        using (Stream stream = image.PixelBuffer.AsStream())
                        {
                            pixels = new byte[(uint)stream.Length];
                            await stream.ReadAsync(pixels, 0, pixels.Length);
                        }

                        // Specify x and y of where to insert into final WriteableImage from column and row.
                        int x = intColumn * 421;
                        int y = intRow * 614;

                        // Combine images.
                        finalWriteableBitmap.Blit(new Rect(x, y, image.PixelWidth, image.PixelHeight), image, new Rect(0, 0, image.PixelWidth, image.PixelHeight), WriteableBitmapExtensions.BlendMode.None);
                        if (++intColumn >= cols)
                        {
                            // Reset column and increment row
                            intColumn = 0;
                            intRow++;
                        }
                    }
                }
            }

            return finalWriteableBitmap;
        }

        /// <summary>
        /// Method that will generate an ObjectState for the provided deck information.
        /// </summary>
        /// <param name="deckCardIds">List of strings representing card Ids.</param>
        /// <param name="strBackCard">Direct image url representing the back of the card.</param>
        /// <returns>A ObjectState representing the deck.</returns>
        private async Task<ObjectState> generateDeck(List<string> deckCardIds, string strBackCard)
        {
            // Generate WriteableBitmaps for deck.
            Guid BitmapEncoderGuid = BitmapEncoder.JpegEncoderId;
            WriteableBitmap deckBitmap = await generateWriteableBitmapForDeck(deckCardIds);
            //var file = await Windows.Storage.ApplicationData.Current.TemporaryFolder.CreateFileAsync("combine.jpg", CreationCollisionOption.ReplaceExisting);
            using (IRandomAccessStream stream = new InMemoryRandomAccessStream())//await file.OpenAsync(FileAccessMode.ReadWrite))
            {
                BitmapPropertySet propertySet = new BitmapPropertySet();
                BitmapTypedValue quality = new BitmapTypedValue(0.8, PropertyType.Single);
                propertySet.Add("ImageQuality", quality);
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoderGuid, stream, propertySet);
                Stream pixelStream = deckBitmap.PixelBuffer.AsStream();
                byte[] pixels = new byte[pixelStream.Length];
                await pixelStream.ReadAsync(pixels, 0, pixels.Length);
                encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)deckBitmap.PixelWidth, (uint)deckBitmap.PixelHeight, 96, 96, pixels);
                await encoder.FlushAsync();

                // Upload WriteableBitmaps to Imgur and save their direct urls (requires Imgur API).
                string deckUrl = await uploadPhotoToImgur(stream);

                // Get list of card names for deck.
                List<string> deckCardNames = await generateCardNamesForDeck(deckCardIds);

                // Get the number of rows and columns that are in the WriteableBitmap.
                Tuple<int, int> deckSize = getDeckSize(deckCardIds);

                // Build out ObjectState objects for deck.
                ObjectState deckObjectState = buildObjectState(deckCardIds, deckSize, deckUrl, strBackCard, deckCardNames, 1);

                return deckObjectState;
            }
        }

        /// <summary>
        /// Shows working ring and prevents user from using the application.
        /// </summary>
        private void enableWorkingRing()
        {
            // Hide MainContent and show Overlay
            MainContent.Visibility = Visibility.Collapsed;
            Overlay.Visibility = Visibility.Visible;
            workingRing.IsActive = true;
        }

        /// <summary>
        /// Hides working ring and allows user to use the application.
        /// </summary>
        private void disableWorkingRing()
        {
            // Show MainContent and hide Overaly
            workingRing.IsActive = false;
            MainContent.Visibility = Visibility.Visible;
            Overlay.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Enum for tracking which part of the YDK we're reading through
        /// </summary>
        private enum YDKParts
        {
            main,
            extra,
            side,
            unknown
        }
    }
}
