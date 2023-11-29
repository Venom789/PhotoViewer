using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Windows.Storage.Pickers;
using Microsoft.UI.Composition.SystemBackdrops;
using WinRT;
using Microsoft.ML;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;




// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PhotoViewer
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {

        ImageRepository ImageRepository { get; } = new();
        public object MessageBox { get; private set; }


        public MainWindow()
        {
            this.InitializeComponent();

            TrySetMicaBackdrop();

            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(AppTitleBar);


            string folderPath = "C:\\Users\\akashsuman\\Downloads\\flowersdataset\\test";
            LoadImages(folderPath);








        }

        

        private void LoadImages(string folderPath)
        {
            ImageRepository.GetImages(folderPath);
            var numImages = ImageRepository.Images.Count();
            ImageInfoBar.Message = $"{numImages} have loaded";
            ImageInfoBar.IsOpen = true;
        }

        private async void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            FolderPicker folderPicker = new FolderPicker();
            folderPicker.FileTypeFilter.Add("*");
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);
            var folder = await folderPicker.PickSingleFolderAsync();

            if (folder != null)
            {
                LoadImages(folder.Path);
            }
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var imageInfo = (sender as Button)?.DataContext as ImageInfo;

            if (imageInfo != null)
            {
                // Load the image bytes
                var imageBytes = File.ReadAllBytes(imageInfo.FullName);

                // Create the ML input
                MLModel.ModelInput sampleData = new MLModel.ModelInput()
                {
                    ImageSource = imageBytes,
                };

                // Load the model and predict the output
                var result = MLModel.Predict(sampleData);

                // Create the image control
                Image image = new Image();
                image.Source = new BitmapImage(new Uri(imageInfo.FullName, UriKind.Absolute));

                // Create a stack panel to hold the image and labels
                StackPanel stackPanel = new StackPanel();

                // Add the image to the stack panel
                stackPanel.Children.Add(image);

                // Create a text block for the predicted label
                TextBlock labelBlock = new TextBlock();
                labelBlock.Text = $"This is {result.PredictedLabel}";
                stackPanel.Children.Add(labelBlock);

                

                // Additional information dictionary
                Dictionary<string, string> additionalInfo = new Dictionary<string, string>()
                {
                    { "sunflower", "Sunflowers are known for their bright yellow color and large flower heads." },
                    { "dandelion", "Dandelions are yellow flowers that are commonly considered weeds, but they have several culinary and medicinal uses." },
                    { "rose", "Roses are beautiful and fragrant flowers that come in a variety of colors and are often associated with love and romance." },
                    { "tulip", "Tulips are colorful flowers that come in many different varieties and are often associated with spring." },
                    { "daisy", "Daisies are simple and cheerful flowers with white petals and a yellow center." }
                };

                // Check if additional information is available for the predicted label
                if (additionalInfo.ContainsKey(result.PredictedLabel))
                {
                    // Create a text block for the additional information
                    TextBlock additionalInfoBlock = new TextBlock();
                    additionalInfoBlock.Text = additionalInfo[result.PredictedLabel];
                    stackPanel.Children.Add(additionalInfoBlock);
                }

                // Create the new window and set its content
                Window window = new Window
                {
                    Title = imageInfo.Name,
                    Content = stackPanel
                };

                // Set the window size and show the window
                SetWindowSize(window, 480, 640);
                window.Activate();
            }
        }


        





        private static void SetWindowSize(Window window, int height, int width)
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            var windowsId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowsId);
            appWindow.Resize(new Windows.Graphics.SizeInt32 { Width = 480, Height = 640 });
        }


        private SpringVector3NaturalMotionAnimation _springAnimation;

        private void CreateOrUpdateSpringAnimation(float finalValue)
        {
            if (_springAnimation is null)
            {
                Compositor compositor = this.Compositor;
                if (compositor is not null)
                {
                    _springAnimation = compositor.CreateSpringVector3Animation();
                    _springAnimation.Target = "Scale";
                }

            }

            _springAnimation.FinalValue = new Vector3(finalValue);
        }

        private void element_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            // Scale up to 1.5
            CreateOrUpdateSpringAnimation(1.05f);

            (sender as UIElement).StartAnimation(_springAnimation);
        }

        private void element_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            // Scale back down to 1.0
            CreateOrUpdateSpringAnimation(1.0f);

            (sender as UIElement).StartAnimation(_springAnimation);
        }

        WindowsSystemDispatcherQueueHelper m_wsdqHelper;
        MicaController m_micaController;
        SystemBackdropConfiguration m_configurationSource;


        bool TrySetMicaBackdrop()
        {
            if (MicaController.IsSupported())
            {
                m_wsdqHelper = new WindowsSystemDispatcherQueueHelper();
                m_wsdqHelper.EnsureWindowsSystemDispatcherQueueController();

                // Hooking up the policy object
                m_configurationSource = new SystemBackdropConfiguration();
                this.Activated += Window_Activated;
                this.Closed += Window_Closed;
                ((FrameworkElement)this.Content).ActualThemeChanged += Window_ThemeChanged;

                // Initial configuration state.
                m_configurationSource.IsInputActive = true;
                SetConfigurationSourceTheme();

                m_micaController = new MicaController();

                // Enable the system backdrop.
                // Note: Be sure to have "using WinRT;" to support the Window.As<...>() call.
                m_micaController.AddSystemBackdropTarget(this.As<ICompositionSupportsSystemBackdrop>());
                m_micaController.SetSystemBackdropConfiguration(m_configurationSource);
                return true; // succeeded
            }
            return false; // Mica is not supported on this system
        }
        private void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            m_configurationSource.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            // Make sure any Mica/Acrylic controller is disposed so it doesn't try to
            // use this closed window.
            if (m_micaController != null)
            {
                m_micaController.Dispose();
                m_micaController = null;
            }
            this.Activated -= Window_Activated;
            m_configurationSource = null;
        }

        private void Window_ThemeChanged(FrameworkElement sender, object args)
        {
            if (m_configurationSource != null)
            {
                SetConfigurationSourceTheme();
            }
        }
        private void SetConfigurationSourceTheme()
        {
            switch (((FrameworkElement)this.Content).ActualTheme)
            {
                case ElementTheme.Dark: m_configurationSource.Theme = SystemBackdropTheme.Dark; break;
                case ElementTheme.Light: m_configurationSource.Theme = SystemBackdropTheme.Light; break;
                case ElementTheme.Default: m_configurationSource.Theme = SystemBackdropTheme.Default; break;
            }
        }

        private async void AppBarButton_Click_1(object sender, RoutedEventArgs e)
        {
            ContentDialog dialog = new ContentDialog()
            {
                Title = "//Build 2023",
                CloseButtonText = "Close",
                XamlRoot = (sender as Button)?.XamlRoot,
                Content = "Thank you for joining us !"

            };
            var result = await dialog.ShowAsync();

        }

        private List<byte[]> imageBytesList = new List<byte[]>();

        private void AppBarButton_ClassifyData(object sender, RoutedEventArgs e)
        {
            // Clear the existing image bytes list
            imageBytesList.Clear();

            // Iterate over the images in the ImageRepository
            foreach (var imageInfo in ImageRepository.Images)
            {
                // Load the image as bytes
                var imageData = File.ReadAllBytes(imageInfo.FullName);

                // Add the image bytes to the list
                imageBytesList.Add(imageData);


            }

            // Now you have all the image bytes in the 'imageBytesList' variable

        }

    }


    class WindowsSystemDispatcherQueueHelper
    {
        [StructLayout(LayoutKind.Sequential)]
        struct DispatcherQueueOptions
        {
            internal int dwSize;
            internal int threadType;
            internal int apartmentType;
        }

        [DllImport("CoreMessaging.dll")]
        private static extern int CreateDispatcherQueueController([In] DispatcherQueueOptions options, [In, Out, MarshalAs(UnmanagedType.IUnknown)] ref object dispatcherQueueController);

        object m_dispatcherQueueController = null;
        public void EnsureWindowsSystemDispatcherQueueController()
        {
            if (Windows.System.DispatcherQueue.GetForCurrentThread() != null)
            {
                // one already exists, so we'll just use it.
                return;
            }

            if (m_dispatcherQueueController == null)
            {
                DispatcherQueueOptions options;
                options.dwSize = Marshal.SizeOf(typeof(DispatcherQueueOptions));
                options.threadType = 2;    // DQTYPE_THREAD_CURRENT
                options.apartmentType = 2; // DQTAT_COM_STA

                CreateDispatcherQueueController(options, ref m_dispatcherQueueController);
            }
        }
    }




    public class ImageInfo
    {
        public ImageInfo(string name, string fullName)
        {
            Name = name;
            FullName = fullName;
        }
        public string Name { get; set; }
        public string FullName { get; set; }

    }





    public class ImageRepository
    {
        public ObservableCollection<ImageInfo> Images { get; } = new();

        public void GetImages(string folderPath)
        {
            Images.Clear();
            var di = new DirectoryInfo(folderPath);
            var files = di.GetFiles("*.*").Where(file => IsImageFile(file.Extension));
            foreach (var file in files)
            {
                Images.Add(new ImageInfo(file.Name, file.FullName));
            }
        }

        private bool IsImageFile(string fileExtension)
        {
            string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".jfif" };
            return imageExtensions.Contains(fileExtension, StringComparer.OrdinalIgnoreCase);
        }


    }




    






}
