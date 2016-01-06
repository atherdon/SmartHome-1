using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartHome.VoiceCommandService
{
    public static class Utils
    {
        private static Dictionary<string, string> _roomToDeviceId = new Dictionary<string, string>()
        {
            {"Garden's",  "GardenPi"},
            {"Bed Room's", "BedRoomPi" },
            {"Living Room's", "LivingRoomPi" },
            {"Simulator's", "SimulatedDevice" }
        };

        public static string GetDeviceIdByRoom(string room)
        {
            if (!string.IsNullOrEmpty(room))
            {
                return _roomToDeviceId["room"];
            }
            return null;
        }
    }
}
