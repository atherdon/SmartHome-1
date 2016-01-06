using System;
using System.Globalization;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Resources.Core;
using Windows.ApplicationModel.VoiceCommands;

namespace SmartHome.VoiceCommands
{
    public sealed class SmartHomeVoiceCommandService : IBackgroundTask
    {
        /// <summary>
        /// the service connection is maintained for the lifetime of a cortana session, once a voice command
        /// has been triggered via Cortana.
        /// </summary>
        VoiceCommandServiceConnection voiceServiceConnection;

        /// <summary>
        /// Lifetime of the background service is controlled via the BackgroundTaskDeferral object, including
        /// registering for cancellation events, signalling end of execution, etc. Cortana may terminate the 
        /// background service task if it loses focus, or the background task takes too long to provide.
        /// 
        /// Background tasks can run for a maximum of 30 seconds.
        /// </summary>
        BackgroundTaskDeferral serviceDeferral;

        /// <summary>
        /// ResourceMap containing localized strings for display in Cortana.
        /// </summary>
        ResourceMap cortanaResourceMap;

        /// <summary>
        /// The context for localized strings.
        /// </summary>
        ResourceContext cortanaContext;

        /// <summary>
        /// Get globalization-aware date formats.
        /// </summary>
        DateTimeFormatInfo dateFormatInfo;
       
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            serviceDeferral = taskInstance.GetDeferral();

            // Register to receive an event if Cortana dismisses the background task. This will
            // occur if the task takes too long to respond, or if Cortana's UI is dismissed.
            // Any pending operations should be cancelled or waited on to clean up where possible.
            taskInstance.Canceled += OnTaskCanceled;

            var triggerDetails = taskInstance.TriggerDetails as AppServiceTriggerDetails;

            // Load localized resources for strings sent to Cortana to be displayed to the user.
            cortanaResourceMap = ResourceManager.Current.MainResourceMap.GetSubtree("Resources");

            // Select the system language, which is what Cortana should be running as.
            cortanaContext = ResourceContext.GetForViewIndependentUse();

            // Get the currently used system date format
            dateFormatInfo = CultureInfo.CurrentCulture.DateTimeFormat;

            // This should match the uap:AppService and VoiceCommandService references from the 
            // package manifest and VCD files, respectively. Make sure we've been launched by
            // a Cortana Voice Command.
            if (triggerDetails != null && triggerDetails.Name == "SmartHomeVoiceCommandService")
            {
                try
                {
                    voiceServiceConnection = VoiceCommandServiceConnection.FromAppServiceTriggerDetails(triggerDetails);

                    voiceServiceConnection.VoiceCommandCompleted += OnVoiceCommandCompleted;

                    // GetVoiceCommandAsync establishes initial connection to Cortana, and must be called prior to any 
                    // messages sent to Cortana. Attempting to use ReportSuccessAsync, ReportProgressAsync, etc
                    // prior to calling this will produce undefined behavior.
                    VoiceCommand voiceCommand = await voiceServiceConnection.GetVoiceCommandAsync();

                    // Depending on the operation (defined in SmartHome:SmartHomeCommands.xml)
                    // perform the appropriate command.
                    switch (voiceCommand.CommandName)
                    {     
                        case "switchDeviceOnOff":
                            var area = voiceCommand.Properties["area"][0];
                            var device = voiceCommand.Properties["device"][0];
                            var action = voiceCommand.Properties["action"][0];
                            await SendCompleationMessageForDeviceAction(area, device, action);
                            break;

                        case "whatIsTheDeviceStatus":
                            var area1 = voiceCommand.Properties["area"][0];
                            var device1 = voiceCommand.Properties["device"][0];
                            await SendCompletionMessageForDeviceStatus(area1, device1);
                            break;
                        case "whatIsAreaStatus":
                            var areaa = voiceCommand.Properties["area"][0];
                            await SendCompletionMessageForAreaStatus(areaa);
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

        private Task SendCompletionMessageForAreaStatus(string areaa)
        {
            throw new NotImplementedException();
        }

        private async Task SendCompleationMessageForDeviceAction(string area, string device, string action)
        {
            string processScreenMsg = string.Format(cortanaResourceMap.GetValue("ProcessingDeviceAction", cortanaContext).ValueAsString, area, device, action);
            await ShowProgressScreen(processScreenMsg);

            // TODO : Switch device on/off
            //var result = DeviceManager.SwitchDeviceOnOff(area, device);]
            string result = "off";

            var responseMsg = string.Format(cortanaResourceMap.GetValue("ResponseDeviceAction", cortanaContext).ValueAsString, area, device, result);

            var userMessage = new VoiceCommandUserMessage();
            userMessage.DisplayMessage = responseMsg;
            userMessage.SpokenMessage = responseMsg;

            VoiceCommandResponse response = VoiceCommandResponse.CreateResponse(userMessage);
            await voiceServiceConnection.ReportSuccessAsync(response);
        }

        private async Task SendCompletionMessageForDeviceStatus(string area, string device)
        {
            string progressScreenString = string.Format(
                cortanaResourceMap.GetValue("ProcessingDeviceStatus", cortanaContext).ValueAsString,
                area, device);
            await ShowProgressScreen(progressScreenString);

            //TODO : Get device status by passing area name and device name
            string status = "on";

            string message = string.Format(
                cortanaResourceMap.GetValue("DeviceStatus", cortanaContext).ValueAsString,
                area, device, status);

            var userMessage = new VoiceCommandUserMessage();
            userMessage.DisplayMessage = message;
            userMessage.SpokenMessage = message;

            VoiceCommandResponse response = VoiceCommandResponse.CreateResponse(userMessage);
            await voiceServiceConnection.ReportSuccessAsync(response);
        }

        /// <summary>
        /// Show a progress screen. These should be posted at least every 5 seconds for a 
        /// long-running operation, such as accessing network resources over a mobile 
        /// carrier network.
        /// </summary>
        /// <param name="message">The message to display, relating to the task being performed.</param>
        /// <returns></returns>
        private async Task ShowProgressScreen(string message)
        {
            var userProgressMessage = new VoiceCommandUserMessage();
            userProgressMessage.DisplayMessage = userProgressMessage.SpokenMessage = message;

            VoiceCommandResponse response = VoiceCommandResponse.CreateResponse(userProgressMessage);
            await voiceServiceConnection.ReportProgressAsync(response);
        }

        /// <summary>
        /// Handle the completion of the voice command. Your app may be cancelled
        /// for a variety of reasons, such as user cancellation or not providing 
        /// progress to Cortana in a timely fashion. Clean up any pending long-running
        /// operations (eg, network requests).
        /// </summary>
        /// <param name="sender">The voice connection associated with the command.</param>
        /// <param name="args">Contains an Enumeration indicating why the command was terminated.</param>
        private void OnVoiceCommandCompleted(VoiceCommandServiceConnection sender, VoiceCommandCompletedEventArgs args)
        {
            if (this.serviceDeferral != null)
            {
                this.serviceDeferral.Complete();
            }
        }

        /// <summary>
        /// When the background task is cancelled, clean up/cancel any ongoing long-running operations.
        /// This cancellation notice may not be due to Cortana directly. The voice command connection will
        /// typically already be destroyed by this point and should not be expected to be active.
        /// </summary>
        /// <param name="sender">This background task instance</param>
        /// <param name="reason">Contains an enumeration with the reason for task cancellation</param>
        private void OnTaskCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            System.Diagnostics.Debug.WriteLine("Task cancelled, clean up");
            if (this.serviceDeferral != null)
            {
                //Complete the service deferral
                this.serviceDeferral.Complete();
            }
        }
    }
}
