using MauiAquariumBLE.View;
namespace MauiAquariumBLE.ViewModels;

public partial class HomePageViewModel : BaseViewModel
{
    BluetoothService BluetoothService;

    public ObservableCollection<BluetoothDevice> BluetoothDevices { get; } = new();

    public IAsyncRelayCommand GoToHeartRatePageAsyncCommand { get; }
    public IAsyncRelayCommand ScanNearbyDevicesAsyncCommand { get; }
    public IAsyncRelayCommand CheckBluetoothAvailabilityAsyncCommand { get; }

    public HomePageViewModel(BluetoothService bluetoothService)
    {
        Title = $"Scan and select device";

        BluetoothService = bluetoothService;

        GoToHeartRatePageAsyncCommand = new AsyncRelayCommand<BluetoothDevice>(async (bluetoothDevice) => await GoToHeartRatePageAsync(bluetoothDevice));

        ScanNearbyDevicesAsyncCommand = new AsyncRelayCommand(ScanDevicesAsync);
        CheckBluetoothAvailabilityAsyncCommand = new AsyncRelayCommand(CheckBluetoothAvailabilityAsync);
    }

    async Task GoToHeartRatePageAsync(BluetoothDevice bluetoothDevice)
    {
        if (IsScanning)
        {
            await BluetoothService.ShowToastAsync($"Bluetooth adapter is scanning. Try again.");
            return;
        }

        if (bluetoothDevice == null)
        {
            return;
        }

        BluetoothService.NewDeviceCandidateFromHomePage = bluetoothDevice;

        Title = $"{bluetoothDevice.Name}";

        await Shell.Current.GoToAsync("//HeartRatePage", true);
    }

    async Task ScanDevicesAsync()
    {
        if (IsScanning)
        {
            return;
        }

        if (!BluetoothService.BluetoothLE.IsAvailable)
        {
            Debug.WriteLine($"Bluetooth is missing.");
            await Shell.Current.DisplayAlert($"Bluetooth", $"Bluetooth is missing.", "OK");
            return;
        }

#if ANDROID
        PermissionStatus permissionStatus = await BluetoothService.CheckBluetoothPermissions();
        if (permissionStatus != PermissionStatus.Granted)
        {
            permissionStatus = await BluetoothService.RequestBluetoothPermissions();
            if (permissionStatus != PermissionStatus.Granted)
            {
                await Shell.Current.DisplayAlert($"Bluetooth LE permissions", $"Bluetooth LE permissions are not granted.", "OK");
                return;
            }
        }
#elif IOS
#elif WINDOWS
#endif

        try
        {
            if (!BluetoothService.BluetoothLE.IsOn)
            {
                await Shell.Current.DisplayAlert($"Bluetooth is not on", $"Please turn Bluetooth on and try again.", "OK");
                return;
            }

            IsScanning = true;

            List<BluetoothDevice> deviceCandidates = await BluetoothService.ScanForDevicesAsync();

            if (deviceCandidates.Count == 0)
            {
                await BluetoothService.ShowToastAsync($"Unable to find nearby Bluetooth LE devices. Try again.");
                return;
            }

            BluetoothDevices.Clear();

            foreach (var deviceCandidate in deviceCandidates)
            {
                BluetoothDevices.Add(deviceCandidate);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Unable to get nearby Bluetooth LE devices: {ex.Message}");
            await Shell.Current.DisplayAlert($"Unable to get nearby Bluetooth LE devices", $"{ex.Message}.", "OK");
        }
        finally
        {
            IsScanning = false;
        }
    }

    async Task CheckBluetoothAvailabilityAsync()
    {
        if (IsScanning)
        {
            return;
        }

        try
        {
            if (!BluetoothService.BluetoothLE.IsAvailable)
            {
                Debug.WriteLine($"Error: Bluetooth is missing.");
                await Shell.Current.DisplayAlert($"Bluetooth", $"Bluetooth is missing.", "OK");
                return;
            }

            if (BluetoothService.BluetoothLE.IsOn)
            {
                await Shell.Current.DisplayAlert($"Bluetooth is on", $"You are good to go.", "OK");
            }
            else
            {
                await Shell.Current.DisplayAlert($"Bluetooth is not on", $"Please turn Bluetooth on and try again.", "OK");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Unable to check Bluetooth availability: {ex.Message}");
            await Shell.Current.DisplayAlert($"Unable to check Bluetooth availability", $"{ex.Message}.", "OK");
        }
    }
}

