using System.Text;
using SimpleWifi.Win32;
using SimpleWifi.Win32.Interop;

namespace DataMulingMachineFileMonitor
{
    public class WifiChecker
    {
        private WlanClient _wLanClient;

        public WifiChecker()
        {
            this._wLanClient = new WlanClient();
        }

        public bool isConnected(string ssid)
        {
            foreach(WlanInterface wlanInterface in this._wLanClient.Interfaces)
            {
                try
                {
                    Dot11Ssid ssidObj = wlanInterface.CurrentConnection.wlanAssociationAttributes.dot11Ssid;
                    string strSSID = new String(Encoding.ASCII.GetChars(ssidObj.SSID, 0, (int)ssidObj.SSIDLength));
                    if (ssid == strSSID)
                        return true;
                }
                catch (Exception e)
                { 
                    //If exception -> wifi not connected on current interface. Skip exception
                }
            }
            return false;
        }
    }
}
