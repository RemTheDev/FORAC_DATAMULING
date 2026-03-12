using System.Text.RegularExpressions;

namespace FORAC.Utility
{
    public static class RandomHelper
    {
        public static string GenerateShortGuidWithoutSpecialChar()
        {
            return Regex.Replace(Convert.ToBase64String(Guid.NewGuid().ToByteArray()), "[/+=]", "");
        }
    }
}
