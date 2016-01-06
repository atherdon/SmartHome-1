using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.VoiceCommands;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Resources.Core;
using Windows.Storage;
using System.Globalization;

namespace SmartHome.VoiceCommandService
{
    public sealed class SmartHomeVoiceCommandService : IBackgroundTask
    {
        VoiceCommandServiceConnection _voiceCommandServiceConnection;

        BackgroundTaskDeferral _serviceDeferrel;

        ResourceMap _cortanaResourceMap;

        ResourceContext _cortanaContext;

        DateTimeFormatInfo _dateTimeFormatInfo;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            _serviceDeferrel = taskInstance.GetDeferral();

            taskInstance.Canceled += OnTaskCancelled;

            var triggerDetails = taskInstance.TriggerDetails as AppServiceTriggerDetails;

            _cortanaResourceMap = ResourceManager.Current.MainResourceMap.GetSubtree("Resources");

            _cortanaContext = ResourceContext.GetForViewIndependentUse();

            _dateTimeFormatInfo = CultureInfo.CurrentCulture.DateTimeFormat;

            if (triggerDetails != null && triggerDetails.Name == "SmartHomeVoiceCommandService")
            {
                try
                {
                    _voiceCommandServiceConnection = VoiceCommandServiceConnection.FromAppServiceTriggerDetails(triggerDetails);

                    _voiceCommandServiceConnection.VoiceCommandCompleted += OnVoiceCommandCompleted;

                    VoiceCommand voiceCommand = await _voiceCommandServiceConnection.GetVoiceCommandAsync();

                    switch (voiceCommand.CommandName)
                    {
                        case "switchDeviceOnOff":
                            {
                                var room = voiceCommand.Properties["room"][0];
                                var device = voiceCommand.Properties["device"][0];
                                var action = voiceCommand.Properties["action"][0];
                                
                                await SendCompletionMessageForDeviceAction(room, device, action);
                            }
                            break;
                        case "whatIsTheDeviceStatus":
                            {
                                var room = voiceCommand.Properties["room"][0];
                                var device = voiceCommand.Properties["device"][0];

                                await SendCompletionMessageForDeviceStatus(room, device);
                            }
                            break;
                        case "whatIsTheStatus":
                            {
                                await SendCompletionMessageForDeviceAction();
                            }
                            break;
                        default:
                            break;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Handling Voice Command failed " + ex.ToString());
                }
            }
        }

        private async Task SendCompletionMessageForDeviceStatus(string room, string device)
        {
            //string status = await DeviceAction.GetDeviceStatusAsync(room, device);

            string message = string.Format("{0} {1} is switched on", room, device);
            
            var userMessage = new VoiceCommandUserMessage();

            var progressMsg = string.Format(_cortanaResourceMap.GetValue("ProcessingDeviceStatus", _cortanaContext).ValueAsString, room, device);

            await ShowProgressScreen(progressMsg);

            //string message = string.Format(_cortanaResourceMap.GetValue("StatusOfDevice", _cortanaContext).ValueAsString, room, device, "on");

            userMessage.DisplayMessage = message;
            userMessage.SpokenMessage = message;
        }

        private Task SendCompletionMessageForDeviceAction(string room, string device, string action)
        {
            throw new NotImplementedException();
        }

        private async Task SendCompletionMessageForDeviceAction()
        {
            string message = "switched on";

            var userMessage = new VoiceCommandUserMessage();

            var progressMsg = string.Format("Getting Status...");

            await ShowProgressScreen(progressMsg);

            //string message = string.Format(_cortanaResourceMap.GetValue("StatusOfDevice", _cortanaContext).ValueAsString, room, device, "on");

            userMessage.DisplayMessage = message;
            userMessage.SpokenMessage = message;
        }

        private async Task ShowProgressScreen(string message)
        {
            var userProgressMessage = new VoiceCommandUserMessage();
            userProgressMessage.DisplayMessage = userProgressMessage.SpokenMessage = message;

            VoiceCommandResponse response = VoiceCommandResponse.CreateResponse(userProgressMessage);
            await _voiceCommandServiceConnection.ReportProgressAsync(response);
        }

        private void OnVoiceCommandCompleted(VoiceCommandServiceConnection sender, VoiceCommandCompletedEventArgs args)
        {
            if (this._serviceDeferrel != null)
            {
                this._serviceDeferrel.Complete();
            }
        }

        private void OnTaskCancelled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            System.Diagnostics.Debug.WriteLine("Task cancelled, clean up");
            if (this._serviceDeferrel != null)
            {
                //Complete the service deferral
                this._serviceDeferrel.Complete();
            }
        }
    }
}
