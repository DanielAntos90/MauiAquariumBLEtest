
using MauiAquariumBLE.Models;
using System.Text;

namespace MauiAquariumBLE.ViewModels;

public partial class HeartRatePageViewModel : BaseViewModel
{
    public BluetoothService BluetoothService { get; private set; }

    public IAsyncRelayCommand ConnectToDeviceCandidateAsyncCommand { get; }
    public IAsyncRelayCommand DisconnectFromDeviceAsyncCommand { get; }

    public IService HeartRateService { get; private set; }
    public ICharacteristic HeartRateMeasurementCharacteristic { get; private set; }
    private string _fullValue;
    public HeartRatePageViewModel(BluetoothService bluetoothService)
    {
        Title = $"Heart rate";

        BluetoothService = bluetoothService;

        ConnectToDeviceCandidateAsyncCommand = new AsyncRelayCommand(ConnectToDeviceCandidateAsync);

        DisconnectFromDeviceAsyncCommand = new AsyncRelayCommand(DisconnectFromDeviceAsync);
    }

    [ObservableProperty]
    ushort heartRateValue;

    [ObservableProperty]
    DateTimeOffset timestamp;

    private async Task ConnectToDeviceCandidateAsync()
    {
        if (IsBusy)
        {
            return;
        }

        if (BluetoothService.NewDeviceCandidateFromHomePage.Id.Equals(Guid.Empty))
        {
            #region read device id from storage
            var device_name = await SecureStorage.Default.GetAsync("device_name");
            var device_id = await SecureStorage.Default.GetAsync("device_id");
            if (!string.IsNullOrEmpty(device_id))
            {
                BluetoothService.NewDeviceCandidateFromHomePage.Name = device_name;
                BluetoothService.NewDeviceCandidateFromHomePage.Id = Guid.Parse(device_id);
            }
            #endregion read device id from storage
            else
            {
                await BluetoothService.ShowToastAsync($"Select a Bluetooth LE device first. Try again.");
                return;
            }
        }

        if (!BluetoothService.BluetoothLE.IsOn)
        {
            await Shell.Current.DisplayAlert($"Bluetooth is not on", $"Please turn Bluetooth on and try again.", "OK");
            return;
        }

        if (BluetoothService.Adapter.IsScanning)
        {
            await BluetoothService.ShowToastAsync($"Bluetooth adapter is scanning. Try again.");
            return;
        }

        try
        {
            IsBusy = true;

            if (BluetoothService != null)
            {
                if (BluetoothService.Device.State == DeviceState.Connected)
                {
                    if (BluetoothService.Device.Id.Equals(BluetoothService.NewDeviceCandidateFromHomePage.Id))
                    {
                        await BluetoothService.ShowToastAsync($"{BluetoothService.Device.Name} is already connected.");
                        return;
                    }

                    if (BluetoothService.NewDeviceCandidateFromHomePage != null)
                    {
                        #region another device
                        if (!BluetoothService.Device.Id.Equals(BluetoothService.NewDeviceCandidateFromHomePage.Id))
                        {
                            Title = $"{BluetoothService.NewDeviceCandidateFromHomePage.Name}";
                            await DisconnectFromDeviceAsync();
                            await BluetoothService.ShowToastAsync($"{BluetoothService.Device.Name} has been disconnected.");
                        }
                        #endregion another device
                    }
                }
            }

            BluetoothService.Device = await BluetoothService.Adapter.ConnectToKnownDeviceAsync(BluetoothService.NewDeviceCandidateFromHomePage.Id);

            if (BluetoothService.Device.State == DeviceState.Connected)
            {
                HeartRateService = await BluetoothService.Device.GetServiceAsync(Uuids.TISensorTagSmartKeys);
                if (HeartRateService != null)
                {
                    HeartRateMeasurementCharacteristic = await HeartRateService.GetCharacteristicAsync(Uuids.TISensorTagKeysData);
                    if (HeartRateMeasurementCharacteristic != null && HeartRateMeasurementCharacteristic.CanUpdate)
                    {
                                   

                       /*     if (result == CharacteristicDescriptorWriteResult.Successful)
                            {
                                await adapter.UpdateConnectionInterval(device, TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(300));
                                int mtu = await device.RequestMtuAsync(512);

                                // Get the characteristic and subscribe to the ValueUpdated event
                                characteristic = await service.GetCharacteristicAsync(Guid.Parse("YOUR-CHARACTERISTIC-GUID"));
                                characteristic.ValueUpdated += Characteristic_ValueUpdated;
                                await characteristic.StartUpdatesAsync();
                            }*/


                            Title = $"{BluetoothService.Device.Name}";

                         /*   #region save device id to storage
                            await SecureStorage.Default.SetAsync("device_name", $"{BluetoothLEService.Device.Name}");
                            await SecureStorage.Default.SetAsync("device_id", $"{BluetoothLEService.Device.Id}");
                            #endregion save device id to storage
                         */
                            await send();

                            HeartRateMeasurementCharacteristic.ValueUpdated += HeartRateMeasurementCharacteristic_ValueUpdated;
                             /*  {
                                   var bytes = args.Characteristic.Value;
                                   var a = BitConverter.ToString(bytes);
                                   Debug.WriteLine(a);

                               };*/

                             await HeartRateMeasurementCharacteristic.StartUpdatesAsync(); 
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Unable to connect to {BluetoothService.NewDeviceCandidateFromHomePage.Name} {BluetoothService.NewDeviceCandidateFromHomePage.Id}: {ex.Message}.");
            await Shell.Current.DisplayAlert($"{BluetoothService.NewDeviceCandidateFromHomePage.Name}", $"Unable to connect to {BluetoothService.NewDeviceCandidateFromHomePage.Name}.", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void HeartRateMeasurementCharacteristic_ValueUpdated(object sender, CharacteristicUpdatedEventArgs e)
    {
        string message = Encoding.UTF8.GetString(e.Characteristic.Value);
        _fullValue += message;
        
        if (message.Contains("\n"))
        {
            // Full value received, process it

            var a =_fullValue; //    ArduinoOutputs;22:27;30.03.2023;10:00;19:00;42;42;led off\n
            _fullValue = null;
        }
    }
    private async Task send()
    {
        byte[] data = Encoding.UTF8.GetBytes("inputs");
        await HeartRateMeasurementCharacteristic.WriteAsync(data);
    }

    private async Task DisconnectFromDeviceAsync()
    {
        if (IsBusy)
        {
            return;
        }

        if (BluetoothService.Device == null)
        {
            await BluetoothService.ShowToastAsync($"Nothing to do.");
            return;
        }

        if (!BluetoothService.BluetoothLE.IsOn)
        {
            await Shell.Current.DisplayAlert($"Bluetooth is not on", $"Please turn Bluetooth on and try again.", "OK");
            return;
        }

        if (BluetoothService.Adapter.IsScanning)
        {
            await BluetoothService.ShowToastAsync($"Bluetooth adapter is scanning. Try again.");
            return;
        }

        if (BluetoothService.Device.State == DeviceState.Disconnected)
        {
            await BluetoothService.ShowToastAsync($"{BluetoothService.Device.Name} is already disconnected.");
            return;
        }

        try
        {
            IsBusy = true;

            await HeartRateMeasurementCharacteristic.StopUpdatesAsync();

            await BluetoothService.Adapter.DisconnectDeviceAsync(BluetoothService.Device);

            HeartRateMeasurementCharacteristic.ValueUpdated -= HeartRateMeasurementCharacteristic_ValueUpdated;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Unable to disconnect from {BluetoothService.Device.Name} {BluetoothService.Device.Id}: {ex.Message}.");
            await Shell.Current.DisplayAlert($"{BluetoothService.Device.Name}", $"Unable to disconnect from {BluetoothService.Device.Name}.", "OK");
        }
        finally
        {
            Title = "Heart rate";
            HeartRateValue = 0;
            Timestamp = DateTimeOffset.MinValue;
            IsBusy = false;
            BluetoothService.Device?.Dispose();
            BluetoothService.Device = null;
            await Shell.Current.GoToAsync("//HomePage", true);
        }
    }
}
